using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.PLM
{
    [Description("PLM变更对象")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class ECNBillPlugin: AbstractBillPlugIn
    {
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            this.Model.GetValue("");
        }
    }
}
