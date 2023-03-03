using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
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
    [Description("其他出库单---带出价格")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class QTCKBillPlugin: AbstractBillPlugIn
    {
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (e.Field.Key == "FCustId")
            {
                string kczz = this.Model.GetValue("FSTOCKORGID") == null ? "" : ((DynamicObject)this.Model.GetValue("FSTOCKORGID"))["Id"].ToString();
                if (kczz == "100026")
                {
                    string kh = this.Model.GetValue("FCUSTID") == null ? "" : ((DynamicObject)this.Model.GetValue("FCUSTID"))["Id"].ToString();
                    var dates = this.Model.DataObject["BillEntry"] as DynamicObjectCollection;
                    foreach(var date in dates)
                    {
                        string xsjmbsql = $@"select FPRICE from T_SAL_PRICELIST a
                         inner join T_SAL_PRICELISTENTRY b on a.fid=b.fid
                         inner join T_SAL_APPLYCUSTOMER c on a.fid=c.fid
                         where FCUSTID={kh} and FMATERIALID={date["MaterialId_Id"]}
                         and A.FFORBIDSTATUS='A' AND  B.FFORBIDSTATUS='A'";
                        var xsjmb = DBUtils.ExecuteDynamicObject(Context, xsjmbsql);
                        if (xsjmb.Count > 0)
                        {
                            this.Model.SetValue("FCGPRICE",xsjmb[0]["FPRICE"], Convert.ToInt32(date["Seq"]) - 1);
                        }
                    }
                    this.View.UpdateView("FEntity");
                }
            }
        }
        public override void AfterDoOperation(AfterDoOperationEventArgs e)
        {
            base.AfterDoOperation(e);
            if (Context.CurrentOrganizationInfo.ID == 100026)
            {
                if (e.Operation.OperationId == 8 && e.Operation.Operation == "Save" && e.OperationResult.IsSuccess )
                {
                    this.View.InvokeFormOperation("Refresh");                  
                }
            }
        }
    }
}
