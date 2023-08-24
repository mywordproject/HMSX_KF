using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Mobile;
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.Complex;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HMSX.Second.Plugin.MES.SMZYQRMobilePlugin;

namespace HMSX.Second.Plugin.MES
{
	[Kingdee.BOS.Util.HotUpdate]
	[Description("转移申请录入")]
	public class ZYSQLRMobilePlugin: ComplexTransBillEdit
    {
        public override void ButtonClick(ButtonClickEventArgs e)
        {
            base.ButtonClick(e);
			string a;
			if ((a = e.Key.ToUpper()) != null)
			{
				if (a == "F_SMQR")
				{
                    SMZYQR();
                    return;
				}

			}
		}
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
			//this.View.BillModel.InsertEntryRow("F_keed_Entity", 0);
			////this.View.BillModel.CreateNewEntryRow("F_keed_Entity");
			//this.View.BillModel.SetValue("F_260_PGID", "147258",0);
			//this.View.UpdateView("F_260_PGID");
			//this.View.UpdateView("F_keed_Entity");
		}
        public void SMZYQR()
        {
            string gxjh = this.View.BillModel.GetValue("FOUTOPBILLNO") == null ? "" : this.View.BillModel.GetValue("FOUTOPBILLNO").ToString();
            string xlh = this.View.BillModel.GetValue("FOUTSEQNUMBER") == null ? "" : this.View.BillModel.GetValue("FOUTSEQNUMBER").ToString();
            string gx = this.View.BillModel.GetValue("FOUTOPERNUMBER") == null ? "" : this.View.BillModel.GetValue("FOUTOPERNUMBER").ToString();
            MobileShowParameter param = new MobileShowParameter();
            param.FormId = "SLSB_ZYSMQR";
            param.ParentPageId = this.View.PageId;
            param.SyncCallBackAction = false;
            param.CustomParams.Add("GXJH", gxjh);
            param.CustomParams.Add("XLH", xlh);
            param.CustomParams.Add("GX", gx);
            this.View.ShowForm(param, delegate (FormResult result)
            {
                List<ZYMX> dates = (List<ZYMX>)result.ReturnData;
                if (dates != null)
                {
                    this.View.BillModel.DeleteEntryData("F_keed_Entity");
                    decimal sl = 0;
                    foreach(var date in dates)
                    {
                        
                        this.View.BillModel.CreateNewEntryRow("F_keed_Entity");
                        int x = this.View.BillModel.GetEntryRowCount("F_keed_Entity");
                        this.View.BillModel.SetValue("F_260_PGID", date.Fpgid, x-1);
                        this.View.BillModel.SetValue("F_260_PC", date.Flot, x - 1);
                        this.View.BillModel.SetValue("F_260_SL", date.Fsl, x - 1);
                        //this.View.BillModel.SetValue("F_260_PGID", "147258", 0);
                        sl += date.Fsl;
                    }
                    this.View.BillModel.SetValue("FOPERAPPLYQTY", sl);
                    this.View.UpdateView("FOPERAPPLYQTY");
                    this.View.UpdateView("F_keed_Entity");
                }
            });
        }
    }
}
