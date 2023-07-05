using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.K3.FIN.ServiceHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.生产制造
{
    [Description("保融平台银行存款")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class BRPTYHServicePlugin : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "F_PAEZ_Date", "F_BB", "F_BWB", "F_HLLX", "F_PAEZ_DECIMAL"};
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
                    foreach (ExtendedDataEntity extended in e.SelectedRows)
                    {
                        DynamicObject dates = extended.DataEntity;
                        DateTime rateDate = Convert.ToDateTime(dates["F_PAEZ_DATE"]);
                        DateTime firstday = Convert.ToDateTime(rateDate.Year + "-" + rateDate.Month + "-01");
                        foreach (var date in dates["FEntity"] as DynamicObjectCollection)
                        {
                            DateTime newDate = firstday.AddDays(-1);
                            long sourceCurId = Convert.ToInt64(date["F_BB_Id"]);
                            long destCurId = Convert.ToInt64(date["F_BWB_Id"]);
                            long HLLX = Convert.ToInt64(date["F_HLLX_Id"]);
                            decimal x = CommonServiceHelper.GetExchangeBusRate(this.Context, sourceCurId, destCurId, HLLX, newDate);
                            date["F_PAEZ_DECIMAL"] = x;
                        }
                    }
                }
            }
        }
    }
}
