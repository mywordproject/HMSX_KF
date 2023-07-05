using Kingdee.BOS;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.K3.MFG.QM.App.BillConvertServicePlugIn;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.生产制造
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("检验单-数据填充")]
    public class JYDConvertPlugin: BaseInspectConvert
    {
        public override void AfterConvert(AfterConvertEventArgs e)
        {
            base.AfterConvert(e);           
        }
    }
}
