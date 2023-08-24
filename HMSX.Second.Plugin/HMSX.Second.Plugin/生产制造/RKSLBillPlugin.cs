using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.Operation;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Interaction;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.List.PlugIn;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.ConvertElement.ServiceArgs;
using Kingdee.BOS.Core.Metadata.FormElement;
using Kingdee.BOS.JSON;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using Kingdee.K3.MFG.App;
using Kingdee.K3.MFG.Mobile.ServiceHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin
{
    [Description("入库单---入库数量反写到检验单")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class RKSLBillPlugin : AbstractOperationServicePlugIn
    {
        readonly string[] reloadKeys = new string[] { "FMtoNo", "F_260_FLDDLX" };
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FMaterialId", "FLot", "FSrcBillNo", "FRealQty", "FSrcEntrySeq",
                "F_RUJP_PgBARCODE", "FHMSXBZ", "FMtoNo", "FSrcBillNo" , "FDate","FApproveDate","F_260_OptPlanNo" };
            foreach (String property in propertys)
            {
                e.FieldKeys.Add(property);
            }
        }
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            if (FormOperation.Operation.Equals("Audit", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var date in e.DataEntitys)
                {
                    if (Context.CurrentOrganizationInfo.ID == 100026)
                    {
                        var entrys = date["Entity"] as DynamicObjectCollection;
                        foreach (var entry in entrys)
                        {
                            string upsql = $@"update T_QM_INSPECTBILLENTRY
                        set  F_260_RKSL=F_260_RKSL+{Convert.ToDouble(entry["RealQty"].ToString())}
                        where FENTRYID =(select a.FENTRYID from T_QM_INSPECTBILLENTRY_A a
                        inner join T_QM_INSPECTBILLENTRY b on a.fentryid=b.fentryid 
                        inner join T_BD_LOTMASTER c on b.FLOT=c.FLOTID
                        where FSRCBILLNO='{entry["SrcBillNo"]}'
                        and a.FMATERIALID='{ entry["MaterialId_Id"]}' and c.FNUMBER='{entry["Lot_text"]}' and FSRCENTRYSEQ={entry["SrcEntrySeq"]}
                        and F_260_PGMXTM='{entry["F_RUJP_PgBARCODE"]}' 
                        )";
                            DBUtils.Execute(Context, upsql);

                            string cxsql = $@"select a.FENTRYID from T_QM_INSPECTBILLENTRY_A a
                             inner join T_QM_INSPECTBILLENTRY b on a.fentryid=b.fentryid 
                             inner join T_BD_LOTMASTER c on b.FLOT=c.FLOTID
                             inner join (
                             select a.FENTRYID,a1.FBILLNO,FSEQ,a.FMATERIALID,c.FNUMBER from T_QM_INSPECTBILLENTRY_A a
                            inner join T_QM_INSPECTBILL a1 on a1.fid=a.fid
                            inner join T_QM_INSPECTBILLENTRY b on a.fentryid=b.fentryid 
                            inner join T_BD_LOTMASTER c on b.FLOT=c.FLOTID
                           where FSRCBILLNO='{entry["SrcBillNo"]}'
                           and a.FMATERIALID='{ entry["MaterialId_Id"]}' and c.FNUMBER='{entry["Lot_text"]}' and FSRCENTRYSEQ={entry["SrcEntrySeq"]}
                           )aa on aa.FBILLNO=a.FSRCBILLNO and aa.FSEQ=a.FSRCENTRYSEQ and aa.FMATERIALID=a.FMATERIALID and c.FNUMBER=aa.FNUMBER";
                            var cx = DBUtils.ExecuteDynamicObject(Context, cxsql);
                            if (cx.Count > 0)
                            {
                                string up1sql = $@"update T_QM_INSPECTBILLENTRY
                              set  F_260_RKSL=F_260_RKSL+{Convert.ToDouble(entry["RealQty"].ToString())}
                              where FENTRYID =(select a.FENTRYID from T_QM_INSPECTBILLENTRY_A a
                               inner join T_QM_INSPECTBILLENTRY b on a.fentryid=b.fentryid 
                               inner join T_BD_LOTMASTER c on b.FLOT=c.FLOTID
                               inner join (
                               select a.FENTRYID,a1.FBILLNO,FSEQ,a.FMATERIALID,c.FNUMBER from T_QM_INSPECTBILLENTRY_A a
                                inner join T_QM_INSPECTBILL a1 on a1.fid=a.fid
                              inner join T_QM_INSPECTBILLENTRY b on a.fentryid=b.fentryid 
                              inner join T_BD_LOTMASTER c on b.FLOT=c.FLOTID
                             where FSRCBILLNO='{entry["SrcBillNo"]}'
                             and a.FMATERIALID='{ entry["MaterialId_Id"]}' and c.FNUMBER='{entry["Lot_text"]}' and FSRCENTRYSEQ={entry["SrcEntrySeq"]}
                             and F_260_PGMXTM='{entry["F_RUJP_PgBARCODE"]}' 
                             )aa on aa.FBILLNO=a.FSRCBILLNO and aa.FSEQ=a.FSRCENTRYSEQ and aa.FMATERIALID=a.FMATERIALID and c.FNUMBER=aa.FNUMBER)";
                                DBUtils.Execute(Context, up1sql);
                            }
                            if (entry["MaterialId"] != null && ((DynamicObject)entry["MaterialId"])["Number"].ToString().Substring(0, 6) == "260.08")
                            {
                                string upsql1 = $@"update HMD_t_Cust_Entry100111 set
                                  F_260_SJWCRQWCZZJTS ='{date["Date"]}'
                                  where F_260_SBBM='{entry["MaterialId_Id"]}' ";
                                DBUtils.Execute(Context, upsql1);
                            }
                            //更新派工明细剩余绑定数
                            if (entry["F_260_OptPlanNo"] != null && entry["F_260_OptPlanNo"].ToString() != "" && entry["F_260_OptPlanNo"].ToString() != " ")
                            {
                                String syslsql = $@"update T_SFC_DISPATCHDETAILENTRY set                        
                            F_260_SYBDSL=F_260_SYBDSL+{Convert.ToDecimal(entry["RealQty"])}
                            where FBARCODE='{entry["F_RUJP_PgBARCODE"]}'";
                                DBUtils.Execute(Context, syslsql);
                            }
                        }
                    }
                }
            }
            else if (FormOperation.Operation.Equals("UnAudit", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var date in e.DataEntitys)
                {
                    if (Context.CurrentOrganizationInfo.ID == 100026)
                    {
                        var entrys = date["Entity"] as DynamicObjectCollection;
                        foreach (var entry in entrys)
                        {
                            //更新派工明细剩余绑定数
                            if (entry["F_260_OptPlanNo"] != null && entry["F_260_OptPlanNo"].ToString() != "" && entry["F_260_OptPlanNo"].ToString() != " ")
                            {
                                String syslsql = $@"update T_SFC_DISPATCHDETAILENTRY set                        
                                F_260_SYBDSL=F_260_SYBDSL-{Convert.ToDecimal(entry["RealQty"])}
                                where FBARCODE='{entry["F_RUJP_PgBARCODE"]}'";
                                DBUtils.Execute(Context, syslsql);
                            }

                            string upsql = $@"update T_QM_INSPECTBILLENTRY
                        set  F_260_RKSL=F_260_RKSL-{Convert.ToDouble(entry["RealQty"].ToString())}
                        where FENTRYID =(select a.FENTRYID from T_QM_INSPECTBILLENTRY_A a
                        inner join T_QM_INSPECTBILLENTRY b on a.fentryid=b.fentryid 
                        inner join T_BD_LOTMASTER c on b.FLOT=c.FLOTID
                        where FSRCBILLNO='{entry["SrcBillNo"]}'
                        and a.FMATERIALID='{ entry["MaterialId_Id"]}' and c.FNUMBER='{entry["Lot_text"]}' and FSRCENTRYSEQ={entry["SrcEntrySeq"]}
                        and F_260_PGMXTM='{entry["F_RUJP_PgBARCODE"]}' )";
                            DBUtils.Execute(Context, upsql);

                            string cxsql = $@"select a.FENTRYID from T_QM_INSPECTBILLENTRY_A a
                             inner join T_QM_INSPECTBILLENTRY b on a.fentryid=b.fentryid 
                             inner join T_BD_LOTMASTER c on b.FLOT=c.FLOTID
                             inner join (
                             select a.FENTRYID,a1.FBILLNO,FSEQ,a.FMATERIALID,c.FNUMBER from T_QM_INSPECTBILLENTRY_A a
                            inner join T_QM_INSPECTBILL a1 on a1.fid=a.fid
                            inner join T_QM_INSPECTBILLENTRY b on a.fentryid=b.fentryid 
                            inner join T_BD_LOTMASTER c on b.FLOT=c.FLOTID
                           where FSRCBILLNO='{entry["SrcBillNo"]}'
                           and a.FMATERIALID='{ entry["MaterialId_Id"]}' and c.FNUMBER='{entry["Lot_text"]}' and FSRCENTRYSEQ={entry["SrcEntrySeq"]}
                           )aa on aa.FBILLNO=a.FSRCBILLNO and aa.FSEQ=a.FSRCENTRYSEQ and aa.FMATERIALID=a.FMATERIALID and c.FNUMBER=aa.FNUMBER";
                            var cx = DBUtils.ExecuteDynamicObject(Context, cxsql);
                            if (cx.Count > 0)
                            {
                                string up1sql = $@"update T_QM_INSPECTBILLENTRY
                              set  F_260_RKSL=F_260_RKSL-{Convert.ToDouble(entry["RealQty"].ToString())}
                              where FENTRYID =(select a.FENTRYID from T_QM_INSPECTBILLENTRY_A a
                               inner join T_QM_INSPECTBILLENTRY b on a.fentryid=b.fentryid 
                               inner join T_BD_LOTMASTER c on b.FLOT=c.FLOTID
                               inner join (
                               select a.FENTRYID,a1.FBILLNO,FSEQ,a.FMATERIALID,c.FNUMBER from T_QM_INSPECTBILLENTRY_A a
                                inner join T_QM_INSPECTBILL a1 on a1.fid=a.fid
                              inner join T_QM_INSPECTBILLENTRY b on a.fentryid=b.fentryid 
                              inner join T_BD_LOTMASTER c on b.FLOT=c.FLOTID
                             where FSRCBILLNO='{entry["SrcBillNo"]}'
                             and a.FMATERIALID='{ entry["MaterialId_Id"]}' and c.FNUMBER='{entry["Lot_text"]}' and FSRCENTRYSEQ={entry["SrcEntrySeq"]}
                             and F_260_PGMXTM='{entry["F_RUJP_PgBARCODE"]}' 
                             )aa on aa.FBILLNO=a.FSRCBILLNO and aa.FSEQ=a.FSRCENTRYSEQ and aa.FMATERIALID=a.FMATERIALID and c.FNUMBER=aa.FNUMBER)";
                                DBUtils.Execute(Context, up1sql);
                            }

                        }
                    }
                }
            }
            else if (FormOperation.Operation.Equals("Save", StringComparison.OrdinalIgnoreCase))
            {
                if (Context.CurrentOrganizationInfo.ID == 100026)
                {
                    foreach (var date in e.DataEntitys)
                    {
                        foreach (var entry in date["Entity"] as DynamicObjectCollection)
                        {

                            if (((DynamicObject)entry["MaterialID"])["Number"].ToString().Substring(0, 6) == "260.02" &&
                               (entry["MtoNo"] == null || entry["MtoNo"].ToString() == "" || entry["MtoNo"].ToString() == " "))
                            {
                                string str = entry["FHMSXBZ"].ToString();
                                string jysql = $@"select XMH.F_260_XMH,XMH.FPKID,FNAME
                                    from T_BD_MATERIAL a
                                    left join t_BD_MaterialPlan c on c.FMATERIALID=a.FMATERIALID
                                    left join T_PLN_MANUFACTUREPOLICY d on c.FMFGPOLICYID=d.FID
                                    LEFT JOIN PAEZ_t_Cust_Entry100355 XMH ON XMH.FMATERIALID=A.FMATERIALID
                                    left join ora_t_Cust100045_L x on XMH.F_260_XMH=x.FID
                                    WHERE 
                                    --D.FNUMBER='ZZCL003_SYS'
                                    --and 
                                    a.FMATERIALID={entry["MaterialId_Id"]}
                                    and FCREATEORGID=100026
                                    and XMH.F_260_XMH is not null
                                    and x.FNAME is not null
                                    order by XMH.FPKID desc";
                                var jy = DBUtils.ExecuteDynamicObject(Context, jysql);
                                if (jy.Count > 0)
                                {
                                    str += "_" + jy[0]["FNAME"].ToString();
                                }
                                string upsql = $@"/*dialect*/ update T_PRD_INSTOCKENTRY set FMTONO='{str}' where FENTRYID={entry["Id"]}";
                                DBUtils.Execute(Context, upsql);
                            }

                            string gxhbsql = $@"select F_260_DDLX from T_SFC_OPTRPT where FBILLNO='{entry["SrcBillNo"]}' and F_260_DDLX<>''";
                            var gxhb = DBUtils.ExecuteDynamicObject(Context, gxhbsql);
                            if (gxhb.Count > 0)
                            {
                                string ddlxupsql = $@"/*dialect*/update T_PRD_INSTOCKENTRY set F_260_FLDDLX='{gxhb[0]["F_260_DDLX"]}' where FENTRYID={entry["Id"]}";
                                DBUtils.Execute(Context, ddlxupsql);
                            }

                        }
                    }
                }
                /**
                string id = base.BusinessInfo.GetForm().Id;
                long ORGid = base.Context.CurrentOrganizationInfo.ID;
                string FID;
                if (ORGid != 0)
                {
                    FID = "";
                    StringBuilder _updateSql = new StringBuilder();
                    _updateSql.Clear();//可以添加多个SQL语句批量执行
                                       //单据存在且有数据
                    if ((e.DataEntitys != null) && (e.DataEntitys.Count<DynamicObject>() > 0))
                    {
                        int rows = e.DataEntitys.Count<DynamicObject>();//列表多选审核时,数量大于1
                        for (int i = 0; i < rows; i++) //多张单据同时操作,循环处理每张单据
                        {
                            DynamicObject obj2 = e.DataEntitys[i];
                            FID = Convert.ToString(obj2["Id"]);//单据内码
                            string FZDSH = Convert.ToString(obj2["FZDSH"]);//单据内码
                            string formID = this.BusinessInfo.GetForm().Id;
                            if (FZDSH == "True")
                            {
                                Object[] obj_BH = new object[] { FID };
                                FormMetadata meta = MetaDataServiceHelper.Load(this.Context, formID) as FormMetadata;
                                Form form = meta.BusinessInfo.GetForm();
                                AppServiceContext.SubmitService.Submit(this.Context, meta.BusinessInfo, obj_BH, "Submit");
                                //审核
                                List<KeyValuePair<object, object>> pkIds = new List<KeyValuePair<object, object>>();
                                foreach (var o in obj_BH)
                                {
                                    pkIds.Add(new KeyValuePair<object, object>(o, ""));
                                }
                                List<object> paraAudit = new List<object>();
                                //1审核通过
                                paraAudit.Add("1");
                                //审核意见
                                paraAudit.Add("");
                                AppServiceContext.SetStatusService.SetBillStatus(this.Context, meta.BusinessInfo, pkIds, paraAudit, "Audit");
                            }                          
                        }

                    }

                }
                **/
            }

        }
        public override void BeforeExecuteOperationTransaction(BeforeExecuteOperationTransaction e)
        {
            base.BeforeExecuteOperationTransaction(e);
            if (Context.CurrentOrganizationInfo.ID == 100026)
            {
                if (FormOperation.Operation.Equals("Save", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (ExtendedDataEntity extended in e.SelectedRows)
                    {
                        DynamicObject dy = extended.DataEntity;
                        DynamicObjectCollection docPriceEntity = dy["Entity"] as DynamicObjectCollection;
                        foreach (var entry in docPriceEntity)
                        {
                            if (entry["F_260_FLDDLX"] != null && entry["F_260_FLDDLX"].ToString() == "车间返工前工序")
                            {
                                entry["StockStatusId_Id"] = 33797546;
                            }
                        }
                    }
                }
                else if (FormOperation.Operation.Equals("UnAudit", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (ExtendedDataEntity extended in e.SelectedRows)
                    {
                        DynamicObject dy = extended.DataEntity;
                        DynamicObjectCollection docPriceEntity = dy["Entity"] as DynamicObjectCollection;
                        foreach (var entry in docPriceEntity)
                        {
                            string jskcsql = $@"/*dialect*/select sum(A.FBASEQTY)FBASEQTY FROM T_STK_INVENTORY A
                                                           left JOIN T_BD_LOTMASTER B ON B.FLOTID = A.FLOT
                                                           where A.FMATERIALID='{entry["MaterialId_Id"]}'
                                                           and B.FNUMBER='{entry["Lot_text"]}'";
                            var jskc = DBUtils.ExecuteDynamicObject(Context, jskcsql);
                            if (jskc.Count > 0 && Convert.ToDecimal(jskc[0]["FBASEQTY"])< Convert.ToDecimal(entry["RealQty"]))
                            {
                                //直接调拨单
                                string zjdbsql = $@"select FLOT, c.fnumber,FSRCMATERIALID from T_STK_STKTRANSFERIN a
                                               inner join T_STK_STKTRANSFERINENTRY b on a.FID = B.FID
                                               left join T_BD_LOTMASTER c on c.FLOTID = B.FLOT
                                               WHERE FSTOCKOUTORGID=100026 and
                                               FSRCMATERIALID ='{entry["MaterialId_Id"]}'
                                               AND c.FNUMBER='{entry["Lot_text"]}'";
                                var zjdb = DBUtils.ExecuteDynamicObject(Context, zjdbsql);
                                if (zjdb.Count > 0)
                                {
                                    throw new KDBusinessException("", "有调拨单无法反审核！");
                                }
                            }
                        }
                    }
                }
            }
        }
        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);
            if (Context.CurrentOrganizationInfo.ID == 100026)
            {
                if (FormOperation.Operation.Equals("Audit", StringComparison.OrdinalIgnoreCase) || FormOperation.Operation.Equals("UnAudit", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var date in e.DataEntitys)
                    {
                        foreach (var entry in date["Entity"] as DynamicObjectCollection)
                        {
                            string upsql = $@"/*dialect*/update T_SFC_OPTRPTENTRY set F_260_CYS=FFINISHQTY-FSTOCKINQUAAUXQTY from T_SFC_OPTRPTENTRY_A
                                where T_SFC_OPTRPTENTRY.FENTRYID=T_SFC_OPTRPTENTRY_A.FENTRYID AND T_SFC_OPTRPTENTRY.FID IN (SELECT FID FROM T_SFC_OPTRPT where FBILLNO='{entry["SrcBillNo"].ToString()}') 
						        and T_SFC_OPTRPTENTRY.FSEQ='{entry["SrcEntrySeq"].ToString()}'";
                            DBUtils.Execute(Context, upsql);
                        }
                        if (FormOperation.Operation.Equals("Audit", StringComparison.OrdinalIgnoreCase))
                        {
                            string sql = $@"select a.FID,FENTRYID,FSEQ FROM T_PRD_INSTOCK a
                                            inner join T_PRD_INSTOCKENTRY b on a.fid=b.fid 
                                            left join t_bd_material c on c.FMATERIALID=b.FMATERIALID
                                            where a.fid={date["Id"]} and c.fnumber like '260.02.%' 
                                             and FMTONO like '%SI'";
                            DynamicObjectCollection source = DBServiceHelper.ExecuteDynamicObject(base.Context, sql);
                            if (source.Count > 0)
                            {
                                CreatePickMtrl(source.ToList<DynamicObject>());
                            }
                            //this.PickSetResult(result);
                        }
                    }
                }
            }
        }

        public override void OnPrepareOperationServiceOption(OnPrepareOperationServiceEventArgs e)
        {
            base.OnPrepareOperationServiceOption(e);
            if (Context.CurrentOrganizationInfo.ID == 100026)
            {
                if (this.FormOperation.OperationId == 8)
                {
                    if (!string.IsNullOrWhiteSpace(this.FormOperation.LoadKeys) && this.FormOperation.LoadKeys != "null")
                    {
                        // 设置操作完后刷新字段
                        var loadKeys = KDObjectConverter.DeserializeObject<List<string>>(this.FormOperation.LoadKeys);
                        if (loadKeys == null)
                        {
                            loadKeys = new List<string>();
                        }
                        foreach (var reloadKey in reloadKeys)
                        {
                            if (!loadKeys.Contains(reloadKey))
                            {
                                loadKeys.Add(reloadKey);
                            }
                        }
                        this.FormOperation.LoadKeys = KDObjectConverter.SerializeObject(loadKeys);
                    }
                }
            }
        }

        protected virtual IOperationResult CreatePickMtrl(List<DynamicObject> ppBomInfos)
        {
            List<ListSelectedRow> list = new List<ListSelectedRow>();
            foreach (DynamicObject dynamicObject in ppBomInfos)
            {
                ListSelectedRow item = new ListSelectedRow(Convert.ToString(dynamicObject["FID"]), Convert.ToString(dynamicObject["FENTRYID"]), Convert.ToInt32(dynamicObject["FSEQ"]) - 1, "daf67cbb-53ae-4250-8047-50a695e73996")
                {
                    EntryEntityKey = "FEntity"

                };
                list.Add(item);

            }
            ConvertOperationResult convertOperationResult;
            string convertRuleId = "SCRK_ZJDB"; //
            var ruleMeta = ConvertServiceHelper.GetConvertRule(this.Context, convertRuleId);
            var rule = ruleMeta.Rule;
            PushArgs args = new PushArgs(rule, list.ToArray())
            {
                TargetBillTypeId = "ce8f49055c5c4782b65463a3f863bb4a",

            };
            IOperationResult operationResult = null;
            try
            {


                OperateOption operateOption = OperateOption.Create();
                convertOperationResult = MobileCommonServiceHelper.Push(this.Context, args, operateOption, false);
                DynamicObject[] array = (from p in convertOperationResult.TargetDataEntities
                                         select p.DataEntity).ToArray<DynamicObject>();
                foreach (DynamicObject obj in array)//源单数据
                {
                    //DynamicObjectCollection dynamicObjectCollection = obj["Entity"] as DynamicObjectCollection;
                    //int rowcount = dynamicObjectCollection.Count;
                }
                FormMetadata cachedFormMetaData = FormMetaDataCache.GetCachedFormMetaData(base.Context, "STK_TransferDirect");
                OperateOption option = OperateOption.Create();
                option.AddInteractionFlag("Kingdee.K3.SCM.App.Core.AppBusinessService.UpdateStockService,Kingdee.K3.SCM.App.Core");
                option.SetIgnoreInteractionFlag(true);
                operationResult = BusinessDataServiceHelper.Save(base.Context, cachedFormMetaData.BusinessInfo, array, option, "");
                if (operationResult.IsSuccess)
                {
                }
                return operationResult;
            }
            catch
            {
                return operationResult;
            }
        }


        protected virtual void PickSetResult(IOperationResult result)
        {
            if (result == null)
            {
                return;
            }
            if (result.IsSuccess)
            {
                return;
            }
            else
            {
                string text = string.Join(";", from o in result.OperateResult
                                               select o.Message);
                string text2 = string.Join(";", from o in result.ValidationErrors
                                                select o.Message);
                if (!text.IsNullOrEmptyOrWhiteSpace())
                {
                    if (!text2.IsNullOrEmptyOrWhiteSpace())
                    {
                        text = text + ";" + text2;
                    }
                    return;
                }
                return;
            }
        }


    }
    internal class pickinfo
    {
        private long _id;
        private long _entryid;
        private long _seq;
        /// <summary>
        /// FID
        /// </summary>
        public long FID
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
            }
        }
        /// <summary>
        /// _entryid
        /// </summary>
        public long FENTRYID
        {
            get
            {
                return _entryid;
            }
            set
            {
                _entryid = value;
            }
        }
        /// <summary>
        /// 物料编码
        /// </summary>
        public long FSEQ
        {
            get
            {
                return _seq;
            }
            set
            {
                _seq = value;
            }
        }
    }
}
