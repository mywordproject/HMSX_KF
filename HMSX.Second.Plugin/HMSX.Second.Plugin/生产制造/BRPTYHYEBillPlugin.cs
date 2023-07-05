using Kingdee.BOS.Core.Bill.PlugIn;
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
    public class BRPTYHYEBillPlugin : AbstractBillPlugIn
    {
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (Context.CurrentOrganizationInfo.ID == 100026)
            {
                if (e.Field.Key == "F_BB")
                {
                    DateTime rateDate = Convert.ToDateTime(this.View.Model.GetValue("F_PAEZ_DATE"));
                    if (this.View.Model.GetValue("F_BB", e.Row) != null
                        && this.View.Model.GetValue("F_BWB", e.Row) != null
                        && this.View.Model.GetValue("F_HLLX", e.Row) != null)
                    {
                        DateTime firstday= Convert.ToDateTime(rateDate.Year + "-" + rateDate.Month + "-01");
                        DateTime newDate = firstday.AddDays(-1); 
                        long sourceCurId = Convert.ToInt64(((DynamicObject)this.View.Model.GetValue("F_BB", e.Row))["Id"]);
                        long destCurId = Convert.ToInt64(((DynamicObject)this.View.Model.GetValue("F_BWB", e.Row))["Id"]);
                        long HLLX = Convert.ToInt64(((DynamicObject)this.View.Model.GetValue("F_HLLX", e.Row))["Id"]);
                        decimal x = CommonServiceHelper.GetExchangeBusRate(this.Context, sourceCurId, destCurId, HLLX, newDate);
                        this.View.Model.SetValue("F_PAEZ_DECIMAL", x, e.Row);
                    }

                }
            }
        }
    }
}
