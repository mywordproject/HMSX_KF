using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Workflow.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS.App;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Util;
using Kingdee.BOS.Workflow.Models.EnumStatus;
using Kingdee.BOS.Workflow.Models.Template;


namespace HMSX.Second.Plugin.生产制造
{
    [Description("补料单--带出供应商")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class BLServerPlugin : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FStockOrgId", "FLot", "FMaterialId", "F_RUJP_PgEntryId", "F_260_PGMXID", "FMoBillNo", "FPPBomEntryId" };
            foreach (String property in propertys)
            {
                e.FieldKeys.Add(property);
            }
        }
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            if (this.Context.CurrentOrganizationInfo.ID == 100026)
            {
                if (FormOperation.Operation.Equals("Save", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var dates in e.DataEntitys)
                    {
                        var entrys = dates["Entity"] as DynamicObjectCollection;
                        foreach (var entry in entrys)
                        {
                            string gyssql = $@"select FSUPPLYID from T_BD_LOTMASTER where FLOTID='{entry["Lot_Id"].ToString()}' and FMATERIALID='{entry["MaterialId_Id"].ToString()}'";
                            var gys = DBUtils.ExecuteDynamicObject(Context, gyssql);
                            if (gys.Count > 0)
                            {
                                string upsql = $@"update T_PRD_FEEDMTRLDATA set F_260_GYS1='{gys[0]["FSUPPLYID"].ToString()}' where FENTRYID='{entry["Id"].ToString()}'";
                                DBUtils.Execute(Context, upsql);
                            }
                        }
                    }
                }
             /**
                if (FormOperation.Operation.Equals("Save", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var dates in e.DataEntitys)
                    {
                        this.OperationResult.IsShowMessage = true;
                        // 本演示只提交一张单据，批量处理，请自行修改代码实现         
                        string formId = this.BusinessInfo.GetForm().Id;
                        string billId = Convert.ToString(dates["Id"]);
                        // 首先判断单据是否已经有未完成的工作流           
                        IProcInstService procInstService = Kingdee.BOS.Workflow.Contracts.ServiceFactory.GetProcInstService(this.Context);
                        bool isExist = procInstService.CheckUnCompletePrcInstExsit(this.Context, formId, billId);
                        if (isExist == false)
                        {
                            // 读取单据的工作流配置模板          
                            IWorkflowTemplateService wfTemplateService = Kingdee.BOS.Workflow.Contracts.ServiceFactory.GetWorkflowTemplateService(this.Context);
                            List<FindPrcResult> findProcResultList = wfTemplateService.GetPrcListByFormID(formId, new string[] { billId }, this.Context);
                            if (findProcResultList == null || findProcResultList.Count == 0)
                            {
                                throw new KDBusinessException("AutoSubmit-002", "查找单据适用的流程模板失败，不允许提交工作流！");
                            }
                            // 设置提交参数：忽略操作过程中的警告，避免与用户交互        
                            OperateOption submitOption = OperateOption.Create();
                            submitOption.SetIgnoreWarning(true);
                            IOperationResult submitResult = null;
                            FindPrcResult findProcResult = findProcResultList[0];
                            if (findProcResult.Result == TemplateResultType.Error)
                            {
                                throw new KDBusinessException("AutoSubmit-003", "单据不符合流程启动条件，不允许提交工作流！");
                            }
                            else if (findProcResult.Result != TemplateResultType.Normal)
                            {
                                // 本单无适用的流程图，直接走传统审批               
                                ISubmitService submitService = ServiceHelper.GetService<ISubmitService>();
                                submitResult = submitService.Submit(this.Context, this.BusinessInfo,
                                    new object[] { billId }, "Submit", submitOption);
                            }
                            else
                            {
                                // 走工作流               
                                IBOSWorkflowService wfService = Kingdee.BOS.Workflow.Contracts.ServiceFactory.GetBOSWorkflowService(this.Context);
                                submitResult = wfService.ListSubmit(this.Context, this.BusinessInfo, 0, new object[] { billId }, findProcResultList, submitOption);
                            }
                            // 判断提交结果          
                            if (submitResult.IsSuccess == true)
                            {
                                this.OperationResult.MergeResult(submitResult);
                            }
                            else
                            {
                                submitResult.MergeValidateErrors();
                                if (submitResult.OperateResult == null)
                                {
                                    throw new KDBusinessException("AutoSubmit-004", "未知原因导致自动提交失败！");
                                }
                                else
                                {
                                    StringBuilder sb = new StringBuilder();
                                    sb.AppendLine("自动提交失败，失败原因：");
                                    foreach (var operateResult in submitResult.OperateResult)
                                    {
                                        sb.AppendLine(operateResult.Message);
                                    }
                                    throw new KDBusinessException("AutoSubmit-005", sb.ToString());
                                }
                            }
                        }
                        
                    }
                }
                **/
            }
        }
        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);
            if (FormOperation.Operation.Equals("Audit", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var dates in e.DataEntitys)
                {
                    if (dates["StockOrgId_Id"].ToString() == "100026")
                    {
                        var entrys = dates["Entity"] as DynamicObjectCollection;
                        foreach (var entry in entrys)
                        {
                            string cxsql = $@"update T_SFC_DISPATCHDETAILENTRY set F_260_LLSL=F_260_LLSL+{Convert.ToDouble(entry["ActualQty"].ToString())} where FENTRYID='{entry["F_RUJP_PGENTRYID"]}' or FENTRYID='{entry["F_260_PGMXID"]}'";
                            DBUtils.Execute(Context, cxsql);
                            string TDJSQL = $@"SELECT T.FPgEntryId,T.FPPBomEntryId,T.FMaterialId,T.FMustQty,FREPLACEGROUP,T2.FMATERIALID,FAvailableQty
                            FROM t_PgBomInfo T
                            INNER JOIN T_PRD_PPBOM T1 ON T.FPPBomId = T1.FID
                            INNER JOIN T_PRD_PPBOMENTRY T2 ON T.FPPBomEntryId = T2.FENTRYID
                            INNER JOIN T_PRD_PPBOMENTRY_C T3 ON T.FPPBomId = T3.FID AND T.FPPBomEntryId = T3.FENTRYID AND T3.FISSUETYPE IN ('1', '3')
                            WHERE 
                            T.FPgEntryId ={entry["F_RUJP_PGENTRYID"]}
                            and T.FPPBomEntryId= {entry["PPBomEntryId"]}
                            and T.FMustQty=0";
                            var TDJ = DBUtils.ExecuteDynamicObject(Context, TDJSQL);
                            if (TDJ.Count > 0)
                            {
                                string UPSQL = $@"/*dialect*/update t_PgBomInfo set FAvailableQty=FAvailableQty+{Convert.ToDouble(entry["ActualQty"].ToString())}
                                         FROM
                                         (SELECT T.FPgEntryId,T.FPPBomEntryId,T.FMaterialId,T.FMustQty,FREPLACEGROUP
                                         FROM t_PgBomInfo T
                                         INNER JOIN T_PRD_PPBOM T1 ON T.FPPBomId = T1.FID
                                         INNER JOIN T_PRD_PPBOMENTRY T2 ON T.FPPBomEntryId = T2.FENTRYID
                                         INNER JOIN T_PRD_PPBOMENTRY_C T3 ON T.FPPBomId = T3.FID AND T.FPPBomEntryId = T3.FENTRYID AND T3.FISSUETYPE IN ('1', '3')
                                         WHERE 
                                         T.FPgEntryId ={entry["F_RUJP_PGENTRYID"]}
                                         and T1.FMoBillNo='{entry["MoBillNo"]}'                                        
                                         and T.FMustQty!=0
                                         AND T2.FREPLACEGROUP={TDJ[0]["FREPLACEGROUP"]} )AA WHERE 
                                         AA.FPgEntryId=t_PgBomInfo.FPgEntryId AND
                                         AA.FPPBomEntryId=t_PgBomInfo.FPPBomEntryId
                                         ";
                                DBUtils.Execute(Context, UPSQL);
                            }
                        }
                    }
                }
            }
            else if (FormOperation.Operation.Equals("UnAudit", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var dates in e.DataEntitys)
                {
                    if (dates["StockOrgId_Id"].ToString() == "100026")
                    {
                        var entrys = dates["Entity"] as DynamicObjectCollection;
                        foreach (var entry in entrys)
                        {
                            string cxsql = $@"update T_SFC_DISPATCHDETAILENTRY set F_260_LLSL=F_260_LLSL-{Convert.ToDouble(entry["ActualQty"].ToString())} where FENTRYID='{entry["F_RUJP_PGENTRYID"]}' or  FENTRYID='{entry["F_260_PGMXID"]}'";
                            DBUtils.Execute(Context, cxsql);


                            string TDJSQL = $@"SELECT T.FPgEntryId,T.FPPBomEntryId,T.FMaterialId,T.FMustQty,FREPLACEGROUP,T2.FMATERIALID,FAvailableQty
                            FROM t_PgBomInfo T
                            INNER JOIN T_PRD_PPBOM T1 ON T.FPPBomId = T1.FID
                            INNER JOIN T_PRD_PPBOMENTRY T2 ON T.FPPBomEntryId = T2.FENTRYID
                            INNER JOIN T_PRD_PPBOMENTRY_C T3 ON T.FPPBomId = T3.FID AND T.FPPBomEntryId = T3.FENTRYID AND T3.FISSUETYPE IN ('1', '3')
                            WHERE 
                            T.FPgEntryId ={entry["F_RUJP_PGENTRYID"]}
                            and T.FPPBomEntryId= {entry["PPBomEntryId"]}
                            and T.FMustQty=0";
                            var TDJ = DBUtils.ExecuteDynamicObject(Context, TDJSQL);
                            if (TDJ.Count > 0)
                            {
                                string UPSQL = $@"/*dialect*/update t_PgBomInfo set FAvailableQty=FAvailableQty-{Convert.ToDouble(entry["ActualQty"].ToString())}
                                         FROM
                                         (SELECT T.FPgEntryId,T.FPPBomEntryId,T.FMaterialId,T.FMustQty,FREPLACEGROUP
                                         FROM t_PgBomInfo T
                                         INNER JOIN T_PRD_PPBOM T1 ON T.FPPBomId = T1.FID
                                         INNER JOIN T_PRD_PPBOMENTRY T2 ON T.FPPBomEntryId = T2.FENTRYID
                                         INNER JOIN T_PRD_PPBOMENTRY_C T3 ON T.FPPBomId = T3.FID AND T.FPPBomEntryId = T3.FENTRYID AND T3.FISSUETYPE IN ('1', '3')
                                         WHERE 
                                         T.FPgEntryId ={entry["F_RUJP_PGENTRYID"]}
                                         and T1.FMoBillNo='{entry["MoBillNo"]}'                                        
                                         and T.FMustQty!=0
                                         AND T2.FREPLACEGROUP={TDJ[0]["FREPLACEGROUP"]} )AA WHERE 
                                         AA.FPgEntryId=t_PgBomInfo.FPgEntryId AND
                                         AA.FPPBomEntryId=t_PgBomInfo.FPPBomEntryId
                                         ";
                                DBUtils.Execute(Context, UPSQL);
                            }
                        }
                    }
                }
            }
        }
    }
}
