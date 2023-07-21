using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.供应链
{
    [Description("库存形态转换单审核时--反写单号")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class KCXTZHServerPlugin : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FStockOrgId", "F_260_BGDBM", "FBillNo", "FMaterialId", "FConvertQty", "FLot", "FStockStatus", "FConvertType" };
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
                if (FormOperation.Operation.Equals("Audit", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var dates in e.DataEntitys)
                    {
                        if (dates["F_260_BGDBM"] == null ? false : dates["F_260_BGDBM"].ToString() == "" ? false : true)
                        {
                            string number = "";
                            foreach (var date in dates["StockConvertEntry"] as DynamicObjectCollection)
                            {
                                if (date["MaterialId"] != null )
                                {
                                    number = ((DynamicObject)date["MaterialId"])["Number"].ToString();
                                }
                            }
                            string upsql = $@"update T_PLM_STD_EC_ITEM set F_260_KCZHD='{dates["FBillNo"]}' 
                            where FPARENTROWID='' and FOBJECTCODE='{number}'
                            and FID in (select FID from T_PLM_PDM_BASE where FCODE='{dates["F_260_BGDBM"]}') ";
                            DBUtils.Execute(Context, upsql);
                        }
                        foreach (var date in dates["StockConvertEntry"] as DynamicObjectCollection)
                        {
                            if (date["MaterialId"] != null && date["Lot"] != null)
                            {
                                if (date["ConvertType"].ToString() == "A" &&
                                    (date["StockStatus_Id"].ToString() == "10000" || date["StockStatus_Id"].ToString() == "27910195"))
                                {
                                    string upsql2 = $@"/*dialect*/UPDATE T_SFC_DISPATCHDETAILENTRY SET F_260_SYBDSL-={Convert.ToDecimal(date["ConvertQty"])}
                                              where FENTRYID in(
                                              SELECT FENTRYID FROM T_SFC_DISPATCHDETAIL t 
                                              inner join T_SFC_DISPATCHDETAILENTRY t1 on t.FID=t1.FID 
                                              where  F_RUJP_LOT!='' 
                                               AND (FMOBILLNO  LIKE '%MO%' OR FMOBILLNO  LIKE '%XNY%' OR FMOBILLNO  LIKE '%YJ%')
                                               and FMATERIALID='{date["MaterialId_Id"]}'
                                               and F_RUJP_LOT='{date["Lot_Text"]}'
                                               )";
                                    DBUtils.Execute(Context, upsql2);
                                }
                                else if (date["ConvertType"].ToString() == "B" &&
                                        (date["StockStatus_Id"].ToString() == "10000" || date["StockStatus_Id"].ToString() == "27910195"))
                                {
                                    string upsql2 = $@"/*dialect*/UPDATE T_SFC_DISPATCHDETAILENTRY SET F_260_SYBDSL+={Convert.ToDecimal(date["ConvertQty"])}
                                              where FENTRYID in(
                                              SELECT FENTRYID FROM T_SFC_DISPATCHDETAIL t 
                                              inner join T_SFC_DISPATCHDETAILENTRY t1 on t.FID=t1.FID 
                                              where  F_RUJP_LOT!='' 
                                               AND (FMOBILLNO  LIKE '%MO%' OR FMOBILLNO  LIKE '%XNY%' OR FMOBILLNO  LIKE '%YJ%')
                                               and FMATERIALID='{date["MaterialId_Id"]}'
                                               and F_RUJP_LOT='{date["Lot_Text"]}'
                                               )";
                                    DBUtils.Execute(Context, upsql2);
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
                            foreach (var date in dates["StockConvertEntry"] as DynamicObjectCollection)
                            {
                                if (date["MaterialId"] != null && date["Lot"] != null)
                                {
                                    if (date["ConvertType"].ToString() == "A" &&
                                        (date["StockStatus_Id"].ToString() == "10000" || date["StockStatus_Id"].ToString() == "27910195"))
                                    {
                                        string upsql2 = $@"/*dialect*/UPDATE T_SFC_DISPATCHDETAILENTRY SET F_260_SYBDSL+={Convert.ToDecimal(date["ConvertQty"])}
                                              where FENTRYID in(
                                              SELECT FENTRYID FROM T_SFC_DISPATCHDETAIL t 
                                              inner join T_SFC_DISPATCHDETAILENTRY t1 on t.FID=t1.FID 
                                              where  F_RUJP_LOT!='' 
                                               AND (FMOBILLNO  LIKE '%MO%' OR FMOBILLNO  LIKE '%XNY%' OR FMOBILLNO  LIKE '%YJ%')
                                               and FMATERIALID='{date["MaterialId_Id"]}'
                                               and F_RUJP_LOT='{date["Lot_Text"]}'
                                               )";
                                        DBUtils.Execute(Context, upsql2);
                                    }
                                    else if (date["ConvertType"].ToString() == "B" &&
                                            (date["StockStatus_Id"].ToString() == "10000" || date["StockStatus_Id"].ToString() == "27910195"))
                                    {
                                        string upsql2 = $@"/*dialect*/UPDATE T_SFC_DISPATCHDETAILENTRY SET F_260_SYBDSL-={Convert.ToDecimal(date["ConvertQty"])}
                                              where FENTRYID in(
                                              SELECT FENTRYID FROM T_SFC_DISPATCHDETAIL t 
                                              inner join T_SFC_DISPATCHDETAILENTRY t1 on t.FID=t1.FID 
                                              where  F_RUJP_LOT!='' 
                                               AND (FMOBILLNO  LIKE '%MO%' OR FMOBILLNO  LIKE '%XNY%' OR FMOBILLNO  LIKE '%YJ%')
                                               and FMATERIALID='{date["MaterialId_Id"]}'
                                               and F_RUJP_LOT='{date["Lot_Text"]}'
                                               )";
                                        DBUtils.Execute(Context, upsql2);
                                    }
                                }
                            }
                            if (dates["F_260_BGDBM"] == null ? false : dates["F_260_BGDBM"].ToString() == "" ? false : true)
                            {
                                string upsql = $@"update T_PLM_STD_EC_ITEM set F_260_KCZHD='' 
                            where F_260_KCZHD='{dates["FBillNo"]}'";
                                DBUtils.Execute(Context, upsql);
                            }
                        }
                    }
                }
            }
        }
    }
}
