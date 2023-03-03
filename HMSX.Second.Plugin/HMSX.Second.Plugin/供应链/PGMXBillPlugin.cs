using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Bill.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.供应链
{
    [Description("派工明细--清空系统来源")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class PGMXBillPlugin: AbstractBillPlugIn
    {
        public override void AfterSave(AfterSaveEventArgs e)
        {
            base.AfterSave(e);
            foreach(var date in this.Model.DataObject["DispatchDetailEntry"] as DynamicObjectCollection)
            {
                this.Model.SetValue("F_260_LY", "", Convert.ToInt32(date["Seq"])-1);
                this.View.UpdateView("F_260_LY", Convert.ToInt32(date["Seq"])-1);
            }
        }
    }
}
