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
        string FID = "";
        string FENTRYID = "";

        public override void OnInitialize(InitializeEventArgs e)
        {
            base.OnInitialize(e);
            FID = e.Paramter.GetCustomParameter("FID") ==null? "" :e.Paramter.GetCustomParameter("FID").ToString();
            FENTRYID = e.Paramter.GetCustomParameter("FENTRYID") == null ? "" : e.Paramter.GetCustomParameter("FENTRYID").ToString();
        }
        protected override void CalcRptRestQty()
        {
            if (FID != "" && FENTRYID!="")
            {
                string cxsql = $@"select F_RUJP_LOT,FBARCODE from  T_SFC_DISPATCHDETAIL t 
                                 left join T_SFC_DISPATCHDETAILENTRY t1 on t.FID=t1.FID 
                                 where T.FID='{FID}'AND t1.FENTRYID='{FENTRYID}'";
                var tm = DBUtils.ExecuteDynamicObject(Context, cxsql);
                if (tm.Count > 0)
                {
                    this.View.BillModel.SetValue("F_SBID_BARCODE", tm[0]["FBARCODE"].ToString(), 0);
                    //this.View.BillModel.SetValue("FFINISHQTY", tm[0]["FREWORKQTY"].ToString(), 0);
                    this.View.UpdateView("F_SBID_BARCODE");
                    // this.View.UpdateView("FFINISHQTY");
                    this.View.BillModel.SetValue("FLOT", tm[0]["F_RUJP_LOT"], 0);
                    this.View.UpdateView("FLOT");
                }
                
            }
        }
    }
}
