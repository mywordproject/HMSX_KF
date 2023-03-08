using Kingdee.BOS;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.生产制造
{
    [Description("工艺路线批量修改——动态表单")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class GYLXPLXGFormPlugin : AbstractDynamicFormPlugIn
    {
        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            if (e.Key.Equals("F_QD"))
            {
                String zd = this.Model.GetValue("F_260_PGZD") == null ? "" : this.Model.GetValue("F_260_PGZD").ToString();
                string F_260_HBSXBL = this.Model.GetValue("F_260_HBSXBL") == null ? "" : this.Model.GetValue("F_260_HBSXBL").ToString();
                string F_260_GYLXFZ = this.Model.GetValue("F_260_GYLXFZ") == null ? "" :((DynamicObject)this.Model.GetValue("F_260_GYLXFZ"))["Id"].ToString();
                if (F_260_GYLXFZ == "" && zd=="2")
                {
                    throw new KDBusinessException("", "分组字段为空不能做批改！");
                }
                string[] rs = new string[3];
                if (zd != "")
                {
                    rs[0] = zd;
                    rs[1] = F_260_HBSXBL;
                    rs[2] = F_260_GYLXFZ;               
                    this.View.ReturnToParentWindow(rs);
                    this.View.Close();
                }              
            }

        }
    }
}
