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

namespace HMSX.Second.Plugin
{
    [Description("付款申请单--带出联行号")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class FKSQDBillPlugin : AbstractBillPlugIn
    {
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (e.Field.Key == "FRECTUNIT")
            {
                if (Context.CurrentOrganizationInfo.ID == 100026)
                {
                    var dates = this.Model.DataObject["FPAYAPPLYENTRY"] as DynamicObjectCollection;
                    foreach (var date in dates)
                    {
                        string fkdw = this.Model.GetValue("FRECTUNIT") == null ? "" : ((DynamicObject)this.Model.GetValue("FRECTUNIT"))["Id"].ToString();
                        string gyssql = $@"select a.FSupplierId,FBANKCODE,FBANKHOLDER,FCNAPS from t_BD_SupplierBank a
                                 inner join t_BD_Supplier b on a.FSupplierId = b.FSupplierId where FUSEORGID = 100026 and 
                                 a.FSupplierId='{fkdw}' ";
                        var gys = DBUtils.ExecuteDynamicObject(Context, gyssql);
                        if (gys.Count > 0)
                        {
                            this.Model.SetValue("F_260_LHH", gys[0]["FCNAPS"], Convert.ToInt32(date["Seq"].ToString()) - 1);

                        }

                    }
                    this.View.UpdateView("F_260_LHH");
                }
            }
        }
        public override void AfterEntryBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterEntryBarItemClick(e);
            if (e.BarItemKey.Equals("tbSplitButton") || e.BarItemKey.Equals("tbAdd") || e.BarItemKey.Equals("tbInsert"))
            {
                if (((DynamicObject)this.Model.GetValue("FAPPLYORGID"))["Id"].ToString() == "100026")
                {
                    var dates = this.Model.DataObject["FPAYAPPLYENTRY"] as DynamicObjectCollection;
                    foreach (var date in dates)
                    {
                        string fkdw = this.Model.GetValue("FRECTUNIT") == null ? "" : ((DynamicObject)this.Model.GetValue("FRECTUNIT"))["Id"].ToString();
                        string gyssql = $@"select a.FSupplierId,FBANKCODE,FBANKHOLDER,FCNAPS from t_BD_SupplierBank a
                                 inner join t_BD_Supplier b on a.FSupplierId = b.FSupplierId where FUSEORGID = 100026 and 
                                 a.FSupplierId='{fkdw}' ";
                        var gys = DBUtils.ExecuteDynamicObject(Context, gyssql);
                        if (gys.Count > 0)
                        {
                            this.Model.SetValue("F_260_LHH", gys[0]["FCNAPS"], Convert.ToInt32(date["Seq"].ToString()) - 1);

                        }

                    }
                    this.View.UpdateView("F_260_LHH");
                }
            }
        }

        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            if (((DynamicObject)this.Model.GetValue("FAPPLYORGID"))["Id"].ToString() == "100026")
            {
                var dates = this.Model.DataObject["FPAYAPPLYENTRY"] as DynamicObjectCollection;
                foreach (var date in dates)
                {
                    string fkdw = this.Model.GetValue("FRECTUNIT") == null ? "" : ((DynamicObject)this.Model.GetValue("FRECTUNIT"))["Id"].ToString();
                    string gyssql = $@"select a.FSupplierId,FBANKCODE,FBANKHOLDER,FCNAPS from t_BD_SupplierBank a
                                 inner join t_BD_Supplier b on a.FSupplierId = b.FSupplierId where FUSEORGID = 100026 and 
                                 a.FSupplierId='{fkdw}' ";
                    var gys = DBUtils.ExecuteDynamicObject(Context, gyssql);
                    if (gys.Count > 0)
                    {
                        this.Model.SetValue("F_260_LHH", gys[0]["FCNAPS"], Convert.ToInt32(date["Seq"].ToString()) - 1);

                    }

                }
                this.View.UpdateView("F_260_LHH");
            }
        }
    }
}
