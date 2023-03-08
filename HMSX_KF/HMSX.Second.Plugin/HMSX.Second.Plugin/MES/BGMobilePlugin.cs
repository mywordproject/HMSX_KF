using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.Complex;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.MES
{
    [Description("报工--移动端")]
    [Kingdee.BOS.Util.HotUpdate]
    public class BGMobilePlugin: ComplexDispatchReportEdit
    {
        public override void ButtonClick(ButtonClickEventArgs e)
        {
			if (e.Key.ToUpper() == "FBUTTON_SUBMIT")
			{
				Kingdee.BOS.Orm.DataEntity.DynamicObjectCollection dynamicObjectCollection = (Kingdee.BOS.Orm.DataEntity.DynamicObjectCollection)base.View.BillModel.DataObject["OptRptEntry"];		
			}
			base.ButtonClick(e);
        }

    }
}
