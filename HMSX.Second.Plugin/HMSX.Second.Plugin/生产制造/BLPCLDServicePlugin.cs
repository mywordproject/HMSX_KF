using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core;
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
using Kingdee.BOS.Util;
using Kingdee.BOS.WebApi.FormService;
using Kingdee.K3.MFG.Mobile.ServiceHelper;
using Kingdee.K3.SCM.App;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicObject = Kingdee.BOS.Orm.DataEntity.DynamicObject;

namespace HMSX.Second.Plugin.生产制造
{
    [Description("不良品处理单")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class BLPCLDServicePlugin : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FMateridlId", "FSrcEntryId0", "FSrcInterId0", "FSourceBillEntryId",
                "FSourceBillId" ,"F_260_ComboCZZT2","FDefectiveQty","FUsePolicy","FWorkShopId1"};
            foreach (String property in propertys)
            {
                e.FieldKeys.Add(property);
            }
        }
        public override void BeforeExecuteOperationTransaction(BeforeExecuteOperationTransaction e)
        {
            base.BeforeExecuteOperationTransaction(e);
            if (FormOperation.Operation.Equals("Save", StringComparison.OrdinalIgnoreCase))
            {
                if (Context.CurrentOrganizationInfo.ID == 100026)
                {
                    long i = 0;
                    long id = 0;
                    //PAEZ_t_Cust_Entry100501
                    string xmhsql = $@"select MIN(FDetailID)FDetailID FROM PAEZ_t_Cust_Entry100501";
                    var xmh = DBUtils.ExecuteDynamicObject(Context, xmhsql);
                    if (xmh.Count > 0)
                    {
                        id += Convert.ToInt64(xmh[0]["FDetailID"]);
                    }
                    foreach (ExtendedDataEntity extended in e.SelectedRows)
                    {
                        Kingdee.BOS.Orm.DataEntity.DynamicObject dates = extended.DataEntity;
                        if (dates["FBILLTYPEID"] != null && ((DynamicObject)dates["FBILLTYPEID"])["Number"].ToString() == "BLPCL004_SYS")
                        {
                            foreach (var date in dates["Entity"] as DynamicObjectCollection)
                            {//keed_Cust_Entry100452 测试
                             //PAEZ_Cust_Entry100501
                                if ((date["PAEZ_Cust_Entry100501"] as DynamicObjectCollection).Count == 0)
                                {
                                    string strsql = $@"/*dialect*/IF OBJECT_ID('tempdb.dbo.#WLQD')IS NOT NULL DROP TABLE #WLQD 
                                              select a.FMATERIALID PWL,b.FMATERIALID WL 
                                              INTO #WLQD from
                                              (select FMATERIALID,FNUMBER,FID,row_number() over (partition by FMATERIALID order by FNUMBER desc) rn 
                                              from T_ENG_BOM where FUSEORGID=100026 ) a
                                              left join T_ENG_BOMCHILD b on a.fid=b.fid
                                              left join t_BD_MaterialBase c on b.FMATERIALID=c.FMATERIALID
                                              where a.rn=1 and FERPCLSID=2;                                            
                                              with cte as
                                              (
                                              select PWL,WL,2 as CJ from #WLQD 
                                              where PWL='{date["MateridlId_Id"]}'
                                              union all
                                              select a.PWL,a.WL,cte.CJ+1 cj from #WLQD a
                                              inner join cte on cte.WL=a.PWL
                                              )
                                              select * from(
                                              select * from cte
                                              union all
                                              select FMATERIALID PWL,FMATERIALID WL,1 as CJ from T_BD_MATERIAL 
                                              where FMATERIALID='{date["MateridlId_Id"]}' )aa order by CJ";
                                    var wlqd = DBUtils.ExecuteDynamicObject(Context, strsql);
                                    foreach (var wl in wlqd)
                                    {
                                        i++;
                                        var dyc = new DynamicObject((date["PAEZ_Cust_Entry100501"] as DynamicObjectCollection).DynamicCollectionItemPropertyType);
                                        //给子单据体赋值
                                        if ((id - i) == 0)
                                        {
                                            i++;
                                        }
                                        dyc["Id"] = id - i;
                                        if (wl["CJ"].ToString() == "1")
                                        {
                                            dyc["F_260_SFFG"] = true;
                                        }
                                        dyc["F_260_FGWLBM_Id"] = wl["WL"];
                                        dyc["F_260_CC"] = wl["CJ"];
                                        (date["PAEZ_Cust_Entry100501"] as DynamicObjectCollection).Add(dyc);
                                    }
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
                if (FormOperation.Operation.Equals("Audit", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var date in e.DataEntitys)
                    {
                        //date[""]
                        string ydnm = "";
                        string ydflnm = "";
                        string lx = "";
                        string czzt = "";
                        if (date["F_260_ComboCZZT2"] != null)
                        {
                            czzt = date["F_260_ComboCZZT2"].ToString();
                        }
                        foreach (var entry in date["Entity"] as DynamicObjectCollection)
                        {
                            ydnm += "'" + entry["SourceBillId"] + "',";
                            ydflnm += "'" + entry["SourceBillEntryId"] + "',";
                            if (((DynamicObject)entry["MateridlId"])["Number"].ToString().Contains("260.03."))
                            {
                                lx = "de29f16214744c21b374044d629595f2";
                            }
                            else if (((DynamicObject)entry["MateridlId"])["Number"].ToString().Contains("260.02."))
                            {
                                lx = "23f62df80a644d05bce25d9d22c69d8f";
                            }

                            if (entry["UsePolicy"] != null && entry["UsePolicy"].ToString() == "K" &&
                               entry["SourceBillId"] != null && entry["SourceBillEntryId"] != null && date["F_260_ComboCZZT2"] != null)
                            {
                                Edit(entry["SourceBillId"].ToString(), entry["SourceBillEntryId"].ToString(), date["F_260_ComboCZZT2"].ToString(), Convert.ToDecimal(entry["DefectiveQty"]), 1);
                            }

                        }
                        if (lx == "")
                        {
                            continue;
                        }
                        string jydsql = $@"/*dialect*/
                         select distinct F_260_HBBH,F_260_HBNM FID,F_260_HBFLNM FENTRYID,F_260_HBHH FSEQ from T_QM_INSPECTBILL a
                         left join T_QM_INSPECTBILLENTRY b on a.fid=b.fid
                         left join T_BAS_BILLTYPE c on c.FBILLTYPEID=a.FBILLTYPEID
                         where FINSPECTORGID=100026 and c.FNUMBER='JYD004_SYS'
                         and A.FID IN ({ydnm.Trim(',')}) AND b.FENTRYID IN ({ydflnm.Trim(',')})";
                        DynamicObjectCollection jyd = DBUtils.ExecuteDynamicObject(Context, jydsql);
                        if (jyd.Count > 0)
                        {
                            IOperationResult result = CreateInstock(jyd.ToList<DynamicObject>(), lx, czzt);
                            this.PickSetResult(result);
                        }
                    }
                }
                if (FormOperation.Operation.Equals("UnAudit", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var date in e.DataEntitys)
                    {
                        foreach (var entry in date["Entity"] as DynamicObjectCollection)
                        {

                            if (entry["UsePolicy"] != null && entry["UsePolicy"].ToString() == "K" &&
                               entry["SourceBillId"] != null && entry["SourceBillEntryId"] != null && date["F_260_ComboCZZT2"] != null)
                            {
                                Edit(entry["SourceBillId"].ToString(), entry["SourceBillEntryId"].ToString(), date["F_260_ComboCZZT2"].ToString(), Convert.ToDecimal(entry["DefectiveQty"]), 0);
                            }

                        }
                    }
                }
            }
        }
        public void Edit(string ydfid, string ydfentryid, string czzt, decimal sl, int zt)
        {
            string jydsql = $@"/*dialect*/
                         select distinct F_260_HBBH,F_260_HBNM FID,F_260_HBFLNM FENTRYID,F_260_HBHH FSEQ from T_QM_INSPECTBILL a
                         left join T_QM_INSPECTBILLENTRY b on a.fid=b.fid
                         where FINSPECTORGID=100026 and A.FID IN ({ydfid}) AND b.FENTRYID IN ({ydfentryid})";
            DynamicObjectCollection jyd = DBUtils.ExecuteDynamicObject(Context, jydsql);
            if (jyd.Count > 0)
            {
                JObject json = new JObject();
                json.Add("IsDeleteEntry", false);
                JObject model = new JObject();
                JArray FEntity = new JArray();
                JObject Entity = new JObject();
                Entity.Add("FENTRYID", jyd[0]["FENTRYID"].ToString());
                if (zt == 1)
                {
                    if (czzt == "让步接收")
                    {
                        Entity.Add("FQuaQty", sl);//合格数量
                    }
                    else
                    {
                        Entity.Add("FProcessFailQty", sl); //工废数量
                    }
                }
                else
                {
                    if (czzt == "让步接收")
                    {
                        Entity.Add("FQuaQty", 0);//合格数量
                    }
                    else
                    {
                        Entity.Add("FProcessFailQty", 0); //工废数量
                    }
                }
                FEntity.Add(Entity);
                model.Add("FID", jyd[0]["FID"].ToString());
                model.Add("FEntity", FEntity);
                json.Add("Model", model);
                var results = WebApiServiceCall.Save(this.Context, "SFC_OperationReport", json.ToString());
                bool isSuccess = Convert.ToBoolean(JObject.Parse(JsonConvert.SerializeObject(results))["Result"]["ResponseStatus"]["IsSuccess"].ToString());
                string c = KDObjectConverter.SerializeObject(results);
                if (isSuccess)
                {

                }
                else
                {
                    string Errors = JObject.Parse(JsonConvert.SerializeObject(results))["Result"]["ResponseStatus"]["Errors"].ToString();
                    var JErrors = JArray.Parse(Errors);
                    string message = "";
                    foreach (var Error in JErrors)
                    {
                        message += ((JObject)Error)["Message"].ToString() + ",";
                    }
                    throw new KDBusinessException("", message);
                }
            }
        }
        protected virtual IOperationResult CreateInstock(List<DynamicObject> ppBomInfos, string lx, string czzt)
        {
            List<ListSelectedRow> list = new List<ListSelectedRow>();
            foreach (DynamicObject dynamicObject in ppBomInfos)
            {
                ListSelectedRow item = new ListSelectedRow(Convert.ToString(dynamicObject["FID"]), Convert.ToString(dynamicObject["FENTRYID"]), Convert.ToInt32(dynamicObject["FSEQ"]) - 1, "c0f3d17c-960a-4104-a5f1-4ea74ad29a66")
                {
                    EntryEntityKey = "FEntity"

                };
                list.Add(item);

            }
            IOperationResult operationResult = null;
            ConvertOperationResult convertOperationResult;
            string convertRuleId = "SFC_OPTRPT2INSTOCK"; //SFC_OPTRPT2INSTOCK
            var ruleMeta = ConvertServiceHelper.GetConvertRule(this.Context, convertRuleId);
            var rule = ruleMeta.Rule;
            PushArgs args = new PushArgs(rule, list.ToArray())
            {
                TargetBillTypeId = lx,

            };
            OperateOption operateOption = OperateOption.Create();
            convertOperationResult = MobileCommonServiceHelper.Push(this.Context, args, operateOption, false);
            DynamicObject[] array = (from p in convertOperationResult.TargetDataEntities
                                     select p.DataEntity).ToArray<DynamicObject>();
            foreach (DynamicObject obj in array)//源单数据
            {
                obj["FZDSH"] = true;
                DynamicObjectCollection dynamicObject = obj["Entity"] as DynamicObjectCollection;
                foreach (var dy in dynamicObject)
                {
                    if (czzt == "报废")
                    {
                        dy["StockId_Id"] = 33814858;//正式33814858//测试33207959
                    }
                    else if (czzt == "隔离")
                    {
                        dy["StockStatusId_Id"] = 3025546;
                    }
                    else if (czzt == "返修" && lx == "23f62df80a644d05bce25d9d22c69d8f")
                    {
                        string cksql = $@"select FWIPSTOCKID from T_BD_DEPARTMENT where FUSEORGID=100026 and FWIPSTOCKID!=0 and FDEPTID={dy["WorkShopId_Id"]}";
                        var ck = DBUtils.ExecuteDynamicObject(Context, cksql);
                        if (ck.Count > 0)
                        {
                            dy["StockId_Id"] = Convert.ToInt64(ck[0]["FWIPSTOCKID"]);
                        }
                    }
                }

                //int rowcount = dynamicObjectCollection.Count;
            }
            FormMetadata cachedFormMetaData = FormMetaDataCache.GetCachedFormMetaData(base.Context, "PRD_INSTOCK");
            OperateOption option = OperateOption.Create();
            option.AddInteractionFlag("Kingdee.K3.SCM.App.Core.AppBusinessService.UpdateStockService,Kingdee.K3.SCM.App.Core");
            option.SetIgnoreInteractionFlag(true);
            operationResult = BusinessDataServiceHelper.Save(base.Context, cachedFormMetaData.BusinessInfo, array, option, "");
            if (operationResult.IsSuccess)
            {
            }
            return operationResult;

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
                    throw new KDBusinessException("", text);
                }
                throw new KDBusinessException("", text2);
            }
        }
    }
}
