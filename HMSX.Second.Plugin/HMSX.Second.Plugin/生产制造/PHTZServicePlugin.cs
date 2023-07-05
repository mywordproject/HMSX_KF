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

namespace HMSX.Second.Plugin.生产制造
{
    [Description("批号调整单")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class PHTZServicePlugin : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FStockStatusId", "FMATERIALID", "FQty", "FLot" , "FConvertType" };
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
                        foreach (var date in dates["STK_LOTADJUSTENTRY"] as DynamicObjectCollection)
                        {
                            if (date["MaterialId"] != null && date["Lot"] != null)
                            {
                                if (date["ConvertType"].ToString() == "A" &&
                                    (date["StockStatusId_Id"].ToString() == "10000" || date["StockStatusId_Id"].ToString() == "27910195"))
                                {
                                    string upsql2 = $@"/*dialect*/UPDATE T_SFC_DISPATCHDETAILENTRY SET F_260_SYBDSL-={Convert.ToDecimal(date["Qty"])}
                                              where FENTRYID in(
                                              SELECT FENTRYID FROM T_SFC_DISPATCHDETAIL t 
                                              inner join T_SFC_DISPATCHDETAILENTRY t1 on t.FID=t1.FID 
                                              where  F_RUJP_LOT!='' 
                                               AND (FMOBILLNO  LIKE '%MO%' OR FMOBILLNO  LIKE '%XNY%')
                                               and FMATERIALID='{date["MaterialId_Id"]}'
                                               and F_RUJP_LOT='{date["Lot_Text"]}'
                                               )";
                                    DBUtils.Execute(Context, upsql2);
                                }
                                else if (date["ConvertType"].ToString() == "B" &&
                                    (date["StockStatusId_Id"].ToString() == "10000" || date["StockStatusId_Id"].ToString() == "27910195"))
                                {
                                    string upsql2 = $@"/*dialect*/UPDATE T_SFC_DISPATCHDETAILENTRY SET F_260_SYBDSL+={Convert.ToDecimal(date["Qty"])}
                                              where FENTRYID in(
                                              SELECT FENTRYID FROM T_SFC_DISPATCHDETAIL t 
                                              inner join T_SFC_DISPATCHDETAILENTRY t1 on t.FID=t1.FID 
                                              where  F_RUJP_LOT!='' 
                                               AND (FMOBILLNO  LIKE '%MO%' OR FMOBILLNO  LIKE '%XNY%')
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
                        foreach (var date in dates["STK_LOTADJUSTENTRY"] as DynamicObjectCollection)
                        {
                            if (date["MaterialId"] != null && date["Lot"] != null)
                            {
                                if (date["ConvertType"].ToString() == "A" &&
                                    (date["StockStatusId_Id"].ToString() == "10000" || date["StockStatusId_Id"].ToString() == "27910195"))
                                {
                                    string upsql2 = $@"/*dialect*/UPDATE T_SFC_DISPATCHDETAILENTRY SET F_260_SYBDSL+={Convert.ToDecimal(date["Qty"])}
                                              where FENTRYID in(
                                              SELECT FENTRYID FROM T_SFC_DISPATCHDETAIL t 
                                              inner join T_SFC_DISPATCHDETAILENTRY t1 on t.FID=t1.FID 
                                              where  F_RUJP_LOT!='' 
                                               AND (FMOBILLNO  LIKE '%MO%' OR FMOBILLNO  LIKE '%XNY%')
                                               and FMATERIALID='{date["MaterialId_Id"]}'
                                               and F_RUJP_LOT='{date["Lot_Text"]}'
                                               )";
                                    DBUtils.Execute(Context, upsql2);
                                }
                                else if (date["ConvertType"].ToString() == "B" &&
                                    (date["StockStatusId_Id"].ToString() == "10000" || date["StockStatusId_Id"].ToString() == "27910195"))
                                {
                                    string upsql2 = $@"/*dialect*/UPDATE T_SFC_DISPATCHDETAILENTRY SET F_260_SYBDSL-={Convert.ToDecimal(date["Qty"])}
                                              where FENTRYID in(
                                              SELECT FENTRYID FROM T_SFC_DISPATCHDETAIL t 
                                              inner join T_SFC_DISPATCHDETAILENTRY t1 on t.FID=t1.FID 
                                              where  F_RUJP_LOT!='' 
                                               AND (FMOBILLNO  LIKE '%MO%' OR FMOBILLNO  LIKE '%XNY%')
                                               and FMATERIALID='{date["MaterialId_Id"]}'
                                               and F_RUJP_LOT='{date["Lot_Text"]}'
                                               )";
                                    DBUtils.Execute(Context, upsql2);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
