using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Mobile.PlugIn;
using Kingdee.BOS.Mobile.PlugIn.ControlModel;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.K3.Core.MFG.EntityHelper;
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.Complex;
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.MES
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("返修报工--带出批号、派工明细")]
    public class FXBGBillPlugin: ComplexOperReworkRptEdit
    {
        string ph = "";

        public override void OnInitialize(InitializeEventArgs e)
        {
            base.OnInitialize(e);
            ph= e.Paramter.GetCustomParameter("F_RUJP_Lot")==null? "" :e.Paramter.GetCustomParameter("F_RUJP_Lot").ToString();
        }
        protected override void CalcRptRestQty()
        {
            if (ph != "")
            {
                string scdd = this.View.BillModel.GetValue("FMONUMBER").ToString();
                string schh = this.View.BillModel.GetValue("FMOROWNUMBER").ToString();
                string op = this.View.BillModel.GetValue("FSOURCEBILLNO").ToString();
                string wl = ((DynamicObject)this.View.BillModel.GetValue("FMATERIALID"))["Id"].ToString();
                string cxsql = $@"select FMONUMBER, FMOROWNUMBER, FSOURCEBILLNO, b.FMATERIALID, F_SBID_BARCODE,FREWORKQTY from T_SFC_OPTRPT a
              inner join T_SFC_OPTRPTENTRY b on a.fid = b.fid
              inner join T_SFC_OPTRPTENTRY_A b1 on b1.FENTRYID=b.FENTRYID
              inner join T_BD_LOTMASTER c on c.FLOTID=b.FLOT 
              where  c.fnumber='{ph}' and
              FMONUMBER = '{scdd}' and FMOROWNUMBER = '{schh}'
              and FSOURCEBILLNO = '{op}' and b.FMATERIALID = '{wl}'";
                var tm = DBUtils.ExecuteDynamicObject(Context, cxsql);
                if (tm.Count > 0)
                {
                    var piha = Tool.Utils.LoadBDData(Context, "BD_BatchMainFile", ph);
                    //this.View.BillModel.SetItemValueByID("FLOT", Convert.ToInt32(piha["Id"].ToString()), 0);
                    //this.View.BillModel.SetValue("FMLot_Text", ph, 0);                  
                    //  this.View.BillModel.SetValue("Lot", ph, 0);
                    this.View.BillModel.SetValue("F_SBID_BARCODE", tm[0]["F_SBID_BARCODE"].ToString(), 0);
                    this.View.BillModel.SetValue("FFINISHQTY", tm[0]["FREWORKQTY"].ToString(), 0);
                    this.View.UpdateView("F_SBID_BARCODE");
                    this.View.UpdateView("FFINISHQTY");
                }
                this.View.BillModel.SetValue("FLOT", ph, 0);
                this.View.UpdateView("FLOT");
            }
        }
    }
}
