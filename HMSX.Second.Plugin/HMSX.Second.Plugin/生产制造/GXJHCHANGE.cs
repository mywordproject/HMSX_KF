using Kingdee.BOS;
using Kingdee.BOS.App;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.Operation;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Interaction;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.ConvertElement.ServiceArgs;
using Kingdee.BOS.Core.Metadata.FormElement;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using Kingdee.K3.MFG.App;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.生产制造
{
    [Description("工序计划--转移单")]
    [Kingdee.BOS.Util.HotUpdate]
    public class GXJHCHANGE : AbstractBillPlugIn
    {
        public override void AfterDoOperation(AfterDoOperationEventArgs e)
        {
            base.AfterDoOperation(e);
            long orgID = this.View.Context.CurrentOrganizationInfo.ID;
            if (orgID == 100026)
            {
                if (e.Operation.Operation == "InsertSubEntry")
                {
                    if (this.View.Model.GetValue("FBillNo") != null)//工序计划单号
                    {
                        string FBillNo = this.View.Model.GetValue("FBillNo").ToString();
                        string SQL;
                        SQL = @"SELECT TOP 1 FENTRYID FROM T_SFC_OPERPLANNINGSEQ T1
                                LEFT JOIN T_SFC_OPERPLANNING T2 ON T1.FID=T2.FID
                                WHERE FPROORGID IN (100026,100027)
                                AND T2.FBILLNO='" + FBillNo + "'";
                        DynamicObjectCollection Dyobj = DBUtils.ExecuteDynamicObject(this.Context, SQL);
                        foreach (DynamicObject objFID in Dyobj)
                        {
                            string FENTRYID;
                            FENTRYID = objFID["FENTRYID"].ToString();
                            DelBill(FENTRYID);
                        }
                        this.View.UpdateView("FSubEntity");
                    }
                }
                else if (e.Operation.Operation == "Audit")
                {
                    if (this.View.Model.GetValue("FBillNo") != null)//工序计划单号
                    {
                        string FBillNo = this.View.Model.GetValue("FBillNo").ToString();
                        string SQL;
                        SQL = @"SELECT TOP 1 FENTRYID FROM T_SFC_OPERPLANNINGSEQ T1
                                LEFT JOIN T_SFC_OPERPLANNING T2 ON T1.FID=T2.FID
                                WHERE FPROORGID IN (100026,100027)
                                AND T2.FBILLNO='" + FBillNo + "'";
                        DynamicObjectCollection Dyobj = DBUtils.ExecuteDynamicObject(this.Context, SQL);

                        foreach (DynamicObject objFID in Dyobj)
                        {
                            string FENTRYID;
                            FENTRYID = objFID["FENTRYID"].ToString();


                            long ORGid = base.Context.CurrentOrganizationInfo.ID;
                            if (ORGid == 100026 || ORGid == 100027)
                            {
                                ZDXT("", FENTRYID);
                                GXZY_SH(FENTRYID);
                            }

                        }

                    }
                }
            }
        }

        private void DelBill(string FENTRYID)
        {
            string SQL;
            SQL = @"SELECT FID FROM T_SFC_OPERATIONTRANSFER 
                    WHERE FPROORGID=100026 AND FSRCOPTPLANOPTID =
                    (SELECT MAX(FDETAILID) FROM T_SFC_OPERPLANNINGDETAIL_B where FREPORTQTY>0 and  FENTRYID=" + FENTRYID + ")";
            DynamicObjectCollection Dyobj = DBUtils.ExecuteDynamicObject(this.Context, SQL);
            int Rows = 0;
            foreach (DynamicObject objFID in Dyobj)
            {
                string FID;
                FID = objFID["FID"].ToString();
                Rows = 1;
                ////提交
                Object[] obj = new object[] { FID };
                FormMetadata meta = MetaDataServiceHelper.Load(base.Context, "SFC_OperationTransfer") as FormMetadata;
                Form form = meta.BusinessInfo.GetForm();
                //AppServiceContext.SubmitService.Submit(base.Context, meta.BusinessInfo, obj, "Submit"); //提交

                //反审核
                List<KeyValuePair<object, object>> pkIds = new List<KeyValuePair<object, object>>();
                foreach (var o in obj)
                {
                    pkIds.Add(new KeyValuePair<object, object>(o, ""));
                }
                List<object> paraUnAudit = new List<object>();
                //反审核
                paraUnAudit.Add("2");
                //审核意见
                paraUnAudit.Add("");
                AppServiceContext.SetStatusService.SetBillStatus(
                                base.Context,
                                meta.BusinessInfo,
                                pkIds, paraUnAudit,
                                "UnAudit",
                                null
                                );

                //删除单据
                IDeleteService delService = Kingdee.BOS.App.ServiceHelper.GetService<IDeleteService>();
                delService.Delete(base.Context, "SFC_OperationTransfer", new object[] { FID });


            }
            if (Rows == 0)
            {
                this.View.ShowMessage("最近工序汇报的转移单已删除,不需要再删除!");
            }
            else
            {
                this.View.ShowMessage("最近工序汇报的转移单删除成功!");
            }


            //string FID = RowSID[0];//单据内码
            //FID = "146359";


            // //审核
            // List<KeyValuePair<object, object>> pkIds = new List<KeyValuePair<object, object>>();
            // foreach (var o in obj)
            // {
            //     pkIds.Add(new KeyValuePair<object, object>(o, ""));
            // }
            // List<object> paraAudit = new List<object>();
            // //1审核通过
            // paraAudit.Add("1");
            // //审核意见
            // paraAudit.Add("");
            // AppServiceContext.SetStatusService.SetBillStatus(base.Context, meta.BusinessInfo, pkIds, paraAudit, "Audit");


        }


        class canshu
        {
            public Int64 fid; //
            public Int64 fentryid; //
            public Int64 fdetailid; //
            public Int64 fqty; //
            //public int FQTYRETURNEDGOODSWRITTENOFF2; //数量
            //public decimal FEXCHANGERATE; //
            //public string FBUSINESSTYPES2; //
        }

        public void ZDXT(string FID, string FENTRYID)
        {
            FID = "119162";
            //var rows = DBUtils.ExecuteDynamicObject(this.Context, string.Format("select FID,FENTRYID from T_SFC_OPERPLANNINGSEQ where FENTRYID={0}", FID));
            string SQL;
            SQL = @"SELECT top 1 FREPORTQTY - FTRANSSELQTY as FQty,T1.FDETAILID FROM T_SFC_OPERPLANNINGDETAIL_B T1
                    LEFT JOIN(SELECT MAX(FDETAILID) AS FDETAILID FROM T_SFC_OPERPLANNINGDETAIL_B where  FENTRYID =" + FENTRYID + @"
            AND FREPORTQTY> 0 GROUP BY FENTRYID) T2 ON T1.FDETAILID = T2.FDETAILID
            WHERE FREPORTQTY -FTRANSSELQTY > 0 AND FENTRYID = " + FENTRYID + " ORDER BY T1.FDETAILID DESC";

            var rows = DBUtils.ExecuteDynamicObject(this.Context, SQL);
            //var rows = DBUtils.ExecuteDynamicObject(this.Context, ("select 119162 FID,119171 FENTRYID "));
            List<canshu> list = new List<canshu>();
            if (rows.Count > 0)
            {
                string FDetailID;
                FDetailID = "";
                for (int i = 0; i < rows.Count; i++)
                {
                    list.Add(new canshu() { fid = Convert.ToInt64(FID), fentryid = Convert.ToInt64(FENTRYID), fdetailid = Convert.ToInt64(rows[i][1]), fqty = Convert.ToInt64(rows[i][0]) });
                    FDetailID = rows[i][1].ToString();
                }

                if (FDetailID != "")
                {
                    this.DoPush("SFC_OperationPlanning", "SFC_OperationTransfer", list, FDetailID);
                    //this.View.ShowMessage(FDetailID)  ;
                    this.View.ShowMessage("转移成功!");

                }


            }
            else
            {
                this.View.ShowMessage("最近汇报已完成转移,无需再转移!");
            }
        }


        /// <summary>
        /// 自动下推并保存
        /// </summary>
        /// <param name="sourceFormId">源单FormId</param>
        /// <param name="targetFormId">目标单FormId</param>
        /// <param name="sourceBillIds">源单内码</param>
        /// <param name="rule">默认下推方案序号</param>
        private void DoPush(string sourceFormId, string targetFormId, List<canshu> sourceBillIds, string FDetailID)
        {
            // 获取源单与目标单的转换规则
            IConvertService convertService = ServiceHelper.GetService<IConvertService>();
            var rules = convertService.GetConvertRules(this.Context, sourceFormId, targetFormId);


            if (rules == null || rules.Count == 0)
            {
                throw new KDBusinessException("", string.Format("未找到{0}到{1}之间，启用的转换规则，无法自动下推！", sourceFormId, targetFormId));
            }
            // 取勾选了默认选项的规则
            var rule = rules.FirstOrDefault(t => t.IsDefault);
            int k = 0;
            foreach (var item in rules)
            {
                string FName = "";
                FName = item.Name.ToString();
                if (FName == "工序转出")
                {
                    rule = rules[k];
                }
                k = k + 1;
            }

            // 如果无默认规则，则取第一个
            if (rule == null)
            {
                rule = rules[0];
            }

            // 开始构建下推参数：
            // 待下推的源单数据行
            List<ListSelectedRow> srcSelectedRows = new List<ListSelectedRow>();
            //srcSelectedRows.Add(LSR);

            foreach (var billId in sourceBillIds)
            {// 把待下推的源单内码，逐个创建ListSelectedRow对象，添加到集合中
             //srcSelectedRows.Add(new ListSelectedRow(billId.ToString(), string.Empty, 0, sourceFormId));
             // 特别说明：上述代码，是整单下推；
             // 如果需要指定待下推的单据体行，请参照下句代码，在ListSelectedRow中，指定EntryEntityKey以及EntryId FSubEntity
                ListSelectedRow LSR_FSubEntity = new ListSelectedRow(billId.fid.ToString(), billId.fentryid.ToString(), 0, sourceFormId) { EntryEntityKey = "FEntity" };
                LSR_FSubEntity.FieldValues.Add("FSubEntity", FDetailID);
                srcSelectedRows.Add(LSR_FSubEntity);
                //srcSelectedRows.Add(new ListSelectedRow(billId.fid.ToString(), billId.fentryid.ToString(), 0, sourceFormId) { EntryEntityKey = "FEntity" });
                //srcSelectedRows.Add(new ListSelectedRow(billId.fid.ToString(), billId.fentryid.ToString(), 0, sourceFormId) { EntryEntityKey = "FSubEntity" });

            }


            IEnumerable<ListSelectedRow> selectedRows = srcSelectedRows;
            if (ObjectUtils.IsNullOrEmpty(selectedRows) || selectedRows.Count<ListSelectedRow>() == 0)
            {
                return;
            }

            if (!selectedRows.FirstOrDefault<ListSelectedRow>().FieldValues.ContainsKey("FSubEntity"))
            {
                long subEntityId2 = Convert.ToInt64(selectedRows.FirstOrDefault<ListSelectedRow>().FieldValues["FSubEntity"]);

                //throw new KDBusinessException("OptPlan2OptTransferConvert", ResManager.LoadKDString("无法定位工序，请在选单列表过滤界面中勾选显示工序列表！", "0151515151833000022852", 7, new object[0]));
            }
            long subEntityId = Convert.ToInt64(selectedRows.FirstOrDefault<ListSelectedRow>().FieldValues["FSubEntity"]);






            // 指定目标单单据类型:情况比较复杂，没有合适的案例做参照，示例代码暂略，直接留空，会下推到默认的单据类型
            string targetBillTypeId = string.Empty;
            // 指定目标单据主业务组织：情况更加复杂，需要涉及到业务委托关系，缺少合适案例，示例代码暂略
            // 建议在转换规则中，配置好主业务组织字段的映射关系：运行时，由系统根据映射关系，自动从上游单据取主业务组织，避免由插件指定
            long targetOrgId = 0;
            // 自定义参数字典：把一些自定义参数，传递到转换插件中；转换插件再根据这些参数，进行特定处理
            Dictionary<string, object> custParams = new Dictionary<string, object>();
            // 组装下推参数对象
            PushArgs pushArgs = new PushArgs(rule, srcSelectedRows.ToArray())
            {

                TargetBillTypeId = targetBillTypeId,
                TargetOrgId = targetOrgId,
                CustomParams = custParams

            };

            // 调用下推服务，生成下游单据数据包
            ConvertOperationResult operationResult = convertService.Push(this.Context, pushArgs, OperateOption.Create());
            // 开始处理下推结果:
            // 获取下推生成的下游单据数据包
            DynamicObject[] targetBillObjs = (from p in operationResult.TargetDataEntities select p.DataEntity).ToArray();
            if (targetBillObjs.Length == 0)
            {
                // 未下推成功目标单，抛出错误，中断审核
                throw new KDBusinessException("", string.Format("由{0}自动下推{1}，没有成功生成数据包，自动下推失败！", sourceFormId, targetFormId));
            }

            // 对下游单据数据包，进行适当的修订，以避免关键字段为空，自动保存失败
            // 示例代码略

            // 获取生成的目标单据数据包
            DynamicObject[] objs = (from p in operationResult.TargetDataEntities
                                    select p.DataEntity).ToArray();
            //MustQty   RealQty  Lot批号  BaseUnitQty基本单位数量
            // ((DynamicObject)((DynamicObjectCollection)((DynamicObject)objs[0])[66])[0])[8] = WeightPlan;
            //对个别自动做特殊赋值-如果有需要
            int i = 0;
            foreach (var billId in sourceBillIds)
            {

                //((DynamicObject)((DynamicObjectCollection)((DynamicObject)objs[0])["SFC_OperationTransfer"])[i])["FOperTransferQty"] = Convert.ToInt32("2");
                //((DynamicObject)((DynamicObjectCollection)((DynamicObject)objs[0])["SFC_OperationTransfer"])[i])["alreadyQty"] = Convert.ToInt32(billId.FQTYRETURNEDGOODSWRITTENOFF2);
                //((DynamicObject)((DynamicObjectCollection)((DynamicObject)objs[0])["SFC_OperationTransfer"])[i])["WaitingQty"] = 0;
                (((DynamicObject)objs[0])["OperApplyQty"]) = Convert.ToString(billId.fqty);
                (((DynamicObject)objs[0])["OperTransferQty"]) = Convert.ToString(billId.fqty);
                (((DynamicObject)objs[0])["TransferQty"]) = Convert.ToString(billId.fqty);
                (((DynamicObject)objs[0])["QualifiedQty"]) = Convert.ToString(billId.fqty);
                (((DynamicObject)objs[0])["OperQualifiedQty"]) = Convert.ToString(billId.fqty);
                (((DynamicObject)objs[0])["TransferBaseQty"]) = Convert.ToString(billId.fqty);
                (((DynamicObject)objs[0])["TransferBaseQty"]) = Convert.ToString(billId.fqty);
                (((DynamicObject)objs[0])["TransferBaseQty"]) = Convert.ToString(billId.fqty);
                (((DynamicObject)objs[0])["QualifiedBaseQty"]) = Convert.ToString(billId.fqty);
                (((DynamicObject)objs[0])["ApplyQty"]) = Convert.ToString(billId.fqty);
                (((DynamicObject)objs[0])["ApplyBaseQty"]) = Convert.ToString(billId.fqty);


                //if (billId.FEXCHANGERATE > 0)
                //{
                //    ((DynamicObject)((DynamicObjectCollection)((DynamicObject)objs[0])["SAL_OUTSTOCKFIN"])[0])["ExchangeRate"] = Convert.ToInt32(billId.FEXCHANGERATE);
                //}
                //else
                //{
                //    ((DynamicObject)((DynamicObjectCollection)((DynamicObject)objs[0])["SAL_OUTSTOCKFIN"])[0])["ExchangeRate"] = 1.0;
                //}
                i++;
            }


            // 读取目标单据元数据
            IMetaDataService metaService = ServiceHelper.GetService<IMetaDataService>();
            var targetBillMeta = metaService.Load(this.Context, targetFormId) as FormMetadata;

            // 构建保存操作参数：设置操作选项值，忽略交互提示
            OperateOption saveOption = OperateOption.Create();

            // 忽略全部需要交互性质的提示，直接保存；
            saveOption.SetIgnoreWarning(true);              // 忽略交互提示
                                                            //saveOption.SetInteractionFlag(this.Option.GetInteractionFlag());        // 如果有交互，传入用户选择的交互结果
                                                            // using Kingdee.BOS.Core.Interaction;
                                                            //saveOption.SetIgnoreInteractionFlag(this.Option.GetIgnoreInteractionFlag());
                                                            //// 如下代码，强制要求忽略交互提示(演示案例不需要，注释掉)
                                                            //saveOption.SetIgnoreWarning(true);
                                                            //// using Kingdee.BOS.Core.Interaction;
                                                            //saveOption.SetIgnoreInteractionFlag(true);

            // 调用保存服务，自动保存
            ISaveService saveService = ServiceHelper.GetService<ISaveService>();
            var saveResult = saveService.Save(this.Context, targetBillMeta.BusinessInfo, targetBillObjs, saveOption, "Save");
            // 判断自动保存结果：只有操作成功，才会继续
            if (this.CheckOpResult(saveResult, saveOption))
            {
                return;
            }


        }

        /// <summary>
        /// 判断操作结果是否成功，如果不成功，则直接抛错中断进程
        /// </summary>
        /// <param name="opResult">操作结果</param>
        /// <param name="opOption">操作参数</param>
        /// <returns></returns>
        ///
        private bool CheckOpResult(IOperationResult opResult, OperateOption opOption)
        {
            bool isSuccess = false;
            if (opResult.IsSuccess == true)
            {
                // 操作成功
                isSuccess = true;
            }
            else
            {
                if (opResult.InteractionContext != null && opResult.InteractionContext.Option.GetInteractionFlag().Count > 0)
                {
                    // 有交互性提示
                    // 传出交互提示完整信息对象
                    //this.OperationResult.InteractionContext = opResult.InteractionContext;
                    // 传出本次交互的标识，
                    // 用户在确认继续后，会重新进入操作；
                    // 将以此标识取本交互是否已经确认过，避免重复交互
                    //this.OperationResult.Sponsor = opResult.Sponsor;
                    // 抛出交互错误，把交互信息传递给前端
                    new KDInteractionException(opOption, opResult.Sponsor);



                }
                else
                {
                    // 操作失败，拼接失败原因，然后抛出中断
                    opResult.MergeValidateErrors();
                    if (opResult.OperateResult == null)
                    {// 未知原因导致提交失败
                        throw new KDBusinessException("", "未知原因导致自动提交、审核失败！");
                    }
                    else
                    {

                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine("自动操作失败：");
                        foreach (var operateResult in opResult.OperateResult)
                        {
                            sb.AppendLine(operateResult.Message);
                        }
                        throw new KDBusinessException("", sb.ToString());
                    }

                }



            }
            return isSuccess;




        }




        private void GXZY_SH(string FENTRYID)
        {
            string SQL;
            SQL = @"SELECT FID FROM T_SFC_OPERATIONTRANSFER 
                    WHERE FPROORGID=100026 AND FSRCOPTPLANOPTID =
                    (SELECT MAX(FDETAILID) FROM T_SFC_OPERPLANNINGDETAIL_B where FREPORTQTY>0 and  FENTRYID=" + FENTRYID + ")";
            DynamicObjectCollection Dyobj = DBUtils.ExecuteDynamicObject(this.Context, SQL);
            int Rows = 0;
            foreach (DynamicObject objFID in Dyobj)
            {
                string FID;
                FID = objFID["FID"].ToString();
                Rows = 1;
                ////提交
                Object[] obj = new object[] { FID };
                FormMetadata meta = MetaDataServiceHelper.Load(base.Context, "SFC_OperationTransfer") as FormMetadata;
                Form form = meta.BusinessInfo.GetForm();
                AppServiceContext.SubmitService.Submit(base.Context, meta.BusinessInfo, obj, "Submit"); //提交



                //审核
                List<KeyValuePair<object, object>> pkIds = new List<KeyValuePair<object, object>>();
                foreach (var o in obj)
                {
                    pkIds.Add(new KeyValuePair<object, object>(o, ""));
                }
                List<object> paraAudit = new List<object>();
                //1审核通过
                paraAudit.Add("1");
                //审核意见
                paraAudit.Add("");
                AppServiceContext.SetStatusService.SetBillStatus(base.Context, meta.BusinessInfo, pkIds, paraAudit, "Audit");


                ////反审核
                //List<KeyValuePair<object, object>> pkIds = new List<KeyValuePair<object, object>>();
                //foreach (var o in obj)
                //{
                //    pkIds.Add(new KeyValuePair<object, object>(o, ""));
                //}
                //List<object> paraUnAudit = new List<object>();
                //paraUnAudit.Add("2");
                ////审核意见
                //paraUnAudit.Add("");
                //AppServiceContext.SetStatusService.SetBillStatus(
                //                base.Context,
                //                meta.BusinessInfo,
                //                pkIds, paraUnAudit,
                //                "UnAudit",
                //                null
                //                );

                ////删除单据
                //IDeleteService delService = Kingdee.BOS.App.ServiceHelper.GetService<IDeleteService>();
                //delService.Delete(base.Context, "SFC_OperationTransfer", new object[] { FID });


            }
            if (Rows == 0)
            {
                this.View.ShowMessage("最近工序汇报的转移单已审核,不需要再审核!");
            }
            else
            {
                this.View.ShowMessage("最近工序汇报的转移单审核成功!");
            }


            //string FID = RowSID[0];//单据内码
            //FID = "146359";

        }
    }
}
