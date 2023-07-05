using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.Operation;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Interaction;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.ConvertElement.ServiceArgs;
using Kingdee.BOS.JSON;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.K3.MFG.Mobile.ServiceHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.供应链
{
    [Description("采购入库单--计算有限期限")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class CGRKDServerPlugin : AbstractOperationServicePlugIn
    {
        readonly string[] reloadKeys = new string[] { "F_260_YXQX", "FMtoNo" };
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FPurchaseOrgId", "F_260_YXQX", "FMaterialId", "FMtoNo", "FPOORDERENTRYID", "FPOOrderNo" };
            foreach (String property in propertys)
            {
                e.FieldKeys.Add(property);
            }
        }

        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            if (Context.CurrentOrganizationInfo.ID == 100026)
            {
                if (FormOperation.Operation.Equals("Save", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var dates in e.DataEntitys)
                    {
                        var entitys = dates["InStockEntry"] as DynamicObjectCollection;
                        foreach (var entity in entitys)
                        {
                            if (entity["F_260_YXQX"] == null)
                            {
                                if (entity["Lot_Text"] != null && entity["Lot_Text"].ToString() != " " && entity["Lot_Text"].ToString() != "")
                                {
                                    string wlsql = $@"select F_260_YJTS from T_BD_MATERIAL where FMATERIALID={entity["MaterialId_Id"]}";
                                    var wl = DBUtils.ExecuteDynamicObject(Context, wlsql);
                                    if (entity["Lot_Text"].ToString().Substring(0, 3) == "260")
                                    {
                                        DateTime rq = DateTime.ParseExact("20" + entity["Lot_Text"].ToString().Substring(3, 6), "yyyyMMdd", System.Globalization.CultureInfo.CurrentCulture);
                                        if (wl.Count > 0)
                                        {
                                            if (Convert.ToInt64(wl[0]["F_260_YJTS"]) > 0)
                                            {
                                                rq = rq.AddDays(Convert.ToInt32(wl[0]["F_260_YJTS"]));
                                                string upsql = $@"update T_STK_INSTOCKENTRY set F_260_YXQX='{rq}' where FENTRYID={entity["Id"]}";
                                                DBUtils.Execute(Context, upsql);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        DateTime rq = DateTime.ParseExact(entity["Lot_Text"].ToString().Substring(0, 8), "yyyyMMdd", System.Globalization.CultureInfo.CurrentCulture);
                                        if (wl.Count > 0)
                                        {
                                            if (Convert.ToInt64(wl[0]["F_260_YJTS"]) > 0)
                                            {
                                                rq = rq.AddDays(Convert.ToInt32(wl[0]["F_260_YJTS"]));
                                                string upsql = $@"update T_STK_INSTOCKENTRY set F_260_YXQX='{rq}' where FENTRYID={entity["Id"]}";
                                                DBUtils.Execute(Context, upsql);
                                            }
                                        }
                                    }
                                }
                            }
                            if (((DynamicObject)entity["MaterialID"])["Number"].ToString().Substring(0, 6) == "260.02" &&
                              (entity["MtoNo"] == null || entity["MtoNo"].ToString() == "" || entity["MtoNo"].ToString() == " "))
                            {
                                string str = entity["FHMSXBZ"].ToString();
                                string jysql = $@"select XMH.F_260_XMH,XMH.FPKID,FNAME
                                    from T_BD_MATERIAL a
                                    left join t_BD_MaterialPlan c on c.FMATERIALID=a.FMATERIALID
                                    left join T_PLN_MANUFACTUREPOLICY d on c.FMFGPOLICYID=d.FID
                                    LEFT JOIN PAEZ_t_Cust_Entry100355 XMH ON XMH.FMATERIALID=A.FMATERIALID
                                    left join ora_t_Cust100045_L x on XMH.F_260_XMH=x.FID
                                    WHERE 
                                    --D.FNUMBER='ZZCL003_SYS'
                                    --and 
                                    a.FMATERIALID={entity["MaterialId_Id"]}
                                    and FCREATEORGID=100026
                                    and XMH.F_260_XMH is not null
                                    and x.FNAME is not null
                                    order by XMH.FPKID desc";
                                var jy = DBUtils.ExecuteDynamicObject(Context, jysql);
                                if (jy.Count > 0)
                                {
                                    str += "_" + jy[0]["FNAME"].ToString();
                                }
                                string upsql = $@"/*dialect*/ update T_STK_INSTOCKENTRY set FMTONO='{str}' where FENTRYID={entity["Id"]}";
                                DBUtils.Execute(Context, upsql);
                            }
                        }
                    }
                }
                else if (FormOperation.Operation.Equals("Audit", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var dates in e.DataEntitys)
                    {
                        foreach (var entity in dates["InStockEntry"] as DynamicObjectCollection)
                        {
                            try
                            {
                                string upsql = $@"/*dialect*/
                             UPDATE T_PUR_ReqEntry SET F_260_WRKS=FREQQTY-FSTOCKQTY
                             FROM 
                             (
                             SELECT T.FENTRYID,FSTOCKQTY FROM T_PUR_ReqEntry_R T
                             INNER JOIN (
                             select lk.FSID,fbillno,B.FENTRYID from t_PUR_POOrder a
                             left join t_PUR_POOrderEntry b on a.fid=b.fid
                             left join T_PUR_POORDERENTRY_R c on c.fentryid=b.fentryid
                             left join T_PUR_POORDERENTRY_LK lk on lk.fentryid=b.fentryid
                             where FPURCHASEORGID=100026 AND fbillno='{entity["POOrderNo"]}' and B.FENTRYID='{entity["POORDERENTRYID"]}'
                             )T1 ON T1.FSID=T.FENTRYID
                             )K WHERE K.FENTRYID=T_PUR_ReqEntry.FENTRYID";
                                DBUtils.Execute(Context, upsql);
                            }
                            catch
                            {

                            }
                            finally
                            {

                            }
                        }
                    }
                }
                else if (FormOperation.Operation.Equals("UnAudit", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var dates in e.DataEntitys)
                    {
                        foreach (var entity in dates["InStockEntry"] as DynamicObjectCollection)
                        {
                            try
                            {
                                string upsql = $@"/*dialect*/
                             UPDATE T_PUR_ReqEntry SET F_260_WRKS=FREQQTY-FSTOCKQTY
                             FROM 
                             (
                             SELECT T.FENTRYID,FSTOCKQTY FROM T_PUR_ReqEntry_R T
                             INNER JOIN (
                             select lk.FSID,fbillno,B.FENTRYID from t_PUR_POOrder a
                             left join t_PUR_POOrderEntry b on a.fid=b.fid
                             left join T_PUR_POORDERENTRY_R c on c.fentryid=b.fentryid
                             left join T_PUR_POORDERENTRY_LK lk on lk.fentryid=b.fentryid
                             where FPURCHASEORGID=100026 AND fbillno='{entity["POOrderNo"]}' and B.FENTRYID='{entity["POORDERENTRYID"]}'
                             )T1 ON T1.FSID=T.FENTRYID
                             )K WHERE K.FENTRYID=T_PUR_ReqEntry.FENTRYID";
                            DBUtils.Execute(Context, upsql);
                            }
                            catch
                            {

                            }
                            finally
                            {

                            }
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
        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);
            if (Context.CurrentOrganizationInfo.ID == 100026)
            {
                if (FormOperation.Operation.Equals("Audit", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var date in e.DataEntitys)
                    {
                        string sql = $@"select a.FID,FENTRYID,FSEQ FROM t_STK_InStock a
                                            inner join T_STK_INSTOCKENTRY b on a.fid=b.fid 
                                            left join t_bd_material c on c.FMATERIALID=b.FMATERIALID
                                            where a.fid={date["Id"]} and c.fnumber like '260.02.%' 
                                             and FMTONO like '%SI'";
                        DynamicObjectCollection source = DBServiceHelper.ExecuteDynamicObject(base.Context, sql);
                        if (source.Count > 0)
                        {
                            CreatePickMtrl(source.ToList<DynamicObject>());
                        }
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
            string convertRuleId = "HMSXCGRK-ZJDB"; //
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
    }
}
