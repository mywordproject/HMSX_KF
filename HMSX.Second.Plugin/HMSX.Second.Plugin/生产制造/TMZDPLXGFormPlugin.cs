using Kingdee.BOS;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.生产制造
{
    [Description("条码主档批量修改——动态表单---返回数据")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class TMZDPLXGFormPlugin: AbstractDynamicFormPlugIn
    {
        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            if (e.Key.Equals("F_260_QD"))
            {
                String zd = this.Model.GetValue("F_260_ZD") == null ? "" : this.Model.GetValue("F_260_ZD").ToString();
                String JHZGH = this.Model.GetValue("F_260_JHGZH") == null ? "" : this.Model.GetValue("F_260_JHGZH").ToString();
                string JHZGHBM = this.Model.GetValue("F_260_JHGZHBM") == null ? "" : this.Model.GetValue("F_260_JHGZHBM").ToString();
                string[] rs = new string[3];
                if (zd != "")
                {
                    rs[0] = zd;
                    rs[1] = JHZGH;
                    rs[2] = JHZGHBM;                    
                    this.View.ReturnToParentWindow(rs);
                    this.View.Close();
                }
                else
                {
                    throw new KDBusinessException("", "字段不能为空！");
                }

            }

        }
    }
}
