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
    [Description("BOM--移动端")]
    [Kingdee.BOS.Util.HotUpdate]
    public class BOMMobilePlugin : ComplexBOMListEdit
    {
        public override void BeforeUpdateValue(BeforeUpdateValueEventArgs e)
        {
            if (e.Key== "FText_OptPlanNumberScan")
            {
                if (e.Value.ToString().Contains("PGMX"))
                {
                    String[] strs = e.Value.ToString().Split('-');
                    if (strs.Length > 4)
                    {
                        e.Value = strs[0] + "-" + strs[1]+"-"+strs[2];
                    }
                }
            }
            base.BeforeUpdateValue(e);
        }
    }
}
