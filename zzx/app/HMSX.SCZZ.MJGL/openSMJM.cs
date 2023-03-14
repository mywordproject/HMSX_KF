using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List.PlugIn;
using System.ComponentModel;

namespace HMSX.SCZZ.MJGL
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("工序汇报打开扫描领用界面")]
    public class openSMJM:AbstractListPlugIn
    {
        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);
            long orid = this.Context.CurrentOrganizationInfo.ID;
            if (e.BarItemKey.Equals("PAEZ_260_SMLY")&& orid==100026)
            {
                DynamicFormShowParameter showParam = new DynamicFormShowParameter();          
                showParam.FormId = "PAEZ_260_SMLY";                
                this.View.ShowForm(showParam, null);
            }
        }
    }
}
