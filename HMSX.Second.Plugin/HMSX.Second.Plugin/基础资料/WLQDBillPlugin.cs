using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.基础资料
{
    [Description("物料清单--刷新")]
    [Kingdee.BOS.Util.HotUpdate]
    public class WLQDBillPlugin:AbstractBillPlugIn
    {
        public override void AfterDoOperation(AfterDoOperationEventArgs e)
        {
            base.AfterDoOperation(e);
            if (Context.CurrentOrganizationInfo.ID == 100026)
            {
                if (e.Operation.OperationId == 8 && e.Operation.Operation == "Save" && e.OperationResult.IsSuccess)
                {
                    this.View.InvokeFormOperation("Refresh");
                    //        // 保存后刷新字段
                }
            }
        }
    }
}
