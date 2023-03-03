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
    [Description("批量修改——动态表单---返回数据")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class PLXGBillPlugin: AbstractDynamicFormPlugIn
    {
        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            if (e.Key.Equals("F_260_QD"))
            {
                String zd = this.Model.GetValue("F_260_ZD")==null?"":this.Model.GetValue("F_260_ZD").ToString();
                String syjc = this.Model.GetValue("F_260_SYJC") == null ? "" : this.Model.GetValue("F_260_SYJC").ToString();
                string bhg = this.Model.GetValue("F_260_BHG") == null ? "" : this.Model.GetValue("F_260_BHG").ToString();
                string jyjgms = this.Model.GetValue("F_260_JYJG") == null ? "" : this.Model.GetValue("F_260_JYJG").ToString();
                string zt = this.Model.GetValue("F_260_ZT") == null ? "" : this.Model.GetValue("F_260_ZT").ToString();
                string zjy = this.Model.GetValue("F_260_ZJY") == null ? "" :((DynamicObject)this.Model.GetValue("F_260_ZJY"))["Id"].ToString();
                string rbjsycms = this.Model.GetValue("F_260_RBJSYCMS") == null ? "" : this.Model.GetValue("F_260_RBJSYCMS").ToString();
                string jyjg = this.Model.GetValue("F_JYJG") == null ? "" : this.Model.GetValue("F_JYJG").ToString();
                string[] rs = new string[8];
                if (zd != "" )
                {
                    rs[0] = zd;
                    rs[1] = syjc;
                    rs[2] = bhg;
                    rs[3] = jyjgms;
                    rs[4] = zt;
                    rs[5] = zjy;
                    rs[6] = rbjsycms;
                    rs[7] = jyjg;
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
