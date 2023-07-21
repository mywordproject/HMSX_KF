using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.财务会计
{
    [Description("应收开票核销记录")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class YSKPHXJLBillPlugin : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FMATCHMETHODID", "FSRCROWID", "FBILLAMOUNTFOR", "FCUROPENAMOUNTFOR", "F_260_Amount", "FSETTLEORGID", "FBASICUNITQTY", "FCUROPENQTY" };
            foreach (String property in propertys)
            {
                e.FieldKeys.Add(property);
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
                        if (Convert.ToInt64(extended["FMATCHMETHODID_Id"])==30)
                        {
                            foreach (var entity in extended["FEntity"] as DynamicObjectCollection)
                            {
                                if (entity["FSRCBILLNO"] != null && Convert.ToInt64(entity["FSETTLEORGID_Id"]) == 100026 && !entity["FSRCBILLNO"].ToString().Contains('-'))
                                {
                                    string ysdsql = $@" select FCOSTAMTSUM from t_AR_receivableEntry where fentryid='{entity["FSRCROWID"]}'";
                                    var ysd = DBUtils.ExecuteDynamicObject(Context, ysdsql);
                                    if (ysd.Count > 0)
                                    {
                                        decimal je = Convert.ToDecimal(entity["FBASICUNITQTY"]) / (Convert.ToDecimal(entity["FCUROPENQTY"]) == 0 ? 1 : Convert.ToDecimal(entity["FCUROPENQTY"]));
                                        entity["F_260_Amount"] = je * Convert.ToDecimal(ysd[0]["FCOSTAMTSUM"]);
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
                if (FormOperation.Operation.Equals("GXCBJE", StringComparison.OrdinalIgnoreCase))
                {
                     string ysdsql = $@"/*dialect*/ UPDATE A SET F_260_AMOUNT=(A.FBASICUNITQTY/CASE WHEN FCUROPENQTY=0 THEN 1 ELSE FCUROPENQTY END)*FCOSTAMTSUM
                                      FROM T_AR_BillingMatchLogENTRY A
                                      LEFT JOIN T_AR_BillingMatchLog B ON A.FID=B.FID
                                      LEFT JOIN t_AR_receivableEntry C ON C.FENTRYID=A.FSRCROWID
                                      WHERE FSETTLEORGID=100026
                                      AND FMATCHMETHODID=30
                                      AND A.FSRCBILLNO NOT LIKE '%-%'";

                    DBUtils.Execute(Context, ysdsql);
                }
            }
        }
    }
}
