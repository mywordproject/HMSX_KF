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
    [Description("其他入库单")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class QTRKServicePlugin : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FQty", "FMATERIALID", "FLOT" };
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
                    foreach (var date in e.DataEntitys)
                    {
                        foreach (var entry in date["STK_MISCELLANEOUSENTRY"] as DynamicObjectCollection)
                        {
                            if (entry["MATERIALID"] != null && entry["Lot"] != null)
                            {
                                string upsql = $@"/*dialect*/UPDATE T_SFC_DISPATCHDETAILENTRY SET F_260_SYBDSL+={Convert.ToDecimal(entry["FQty"])}
                                              where FENTRYID in(
                                              SELECT FENTRYID FROM T_SFC_DISPATCHDETAIL t 
                                              inner join T_SFC_DISPATCHDETAILENTRY t1 on t.FID=t1.FID 
                                              where  F_RUJP_LOT!='' 
                                               AND (FMOBILLNO  LIKE '%MO%' OR FMOBILLNO  LIKE '%XNY%')
                                               and FMATERIALID='{entry["MATERIALID_Id"]}'
                                               and F_RUJP_LOT='{entry["Lot_Text"]}'
                                               )";
                                DBUtils.Execute(Context, upsql);
                            }

                        }
                    }
                }
                if (FormOperation.Operation.Equals("UnAudit", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var date in e.DataEntitys)
                    {
                        foreach (var entry in date["STK_MISCELLANEOUSENTRY"] as DynamicObjectCollection)
                        {
                            if (entry["MATERIALID"] != null && entry["Lot"] != null)
                            {
                                string upsql = $@"/*dialect*/UPDATE T_SFC_DISPATCHDETAILENTRY SET F_260_SYBDSL-={Convert.ToDecimal(entry["FQty"])}
                                              where FENTRYID in(
                                              SELECT FENTRYID FROM T_SFC_DISPATCHDETAIL t 
                                              inner join T_SFC_DISPATCHDETAILENTRY t1 on t.FID=t1.FID 
                                              where  F_RUJP_LOT!='' 
                                               AND (FMOBILLNO  LIKE '%MO%' OR FMOBILLNO  LIKE '%XNY%')
                                               and FMATERIALID='{entry["MATERIALID_Id"]}'
                                               and F_RUJP_LOT='{entry["Lot_Text"]}'
                                               )";
                                DBUtils.Execute(Context, upsql);
                            }
                        }

                    }
                }
            }
        }
    }
}
