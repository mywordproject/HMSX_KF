using Kingdee.BOS;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace HMSX.Second.Plugin.批号追溯
{
    [Description("批号追溯过滤——动态表单---返回数据")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class DTBillPlugin : AbstractDynamicFormPlugIn
    {
        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            if (e.Key.Equals("F_260_QD"))
            {                             
                 String wl = this.Model.GetValue("F_RUJP_WL")==null?"":((DynamicObject)this.Model.GetValue("F_RUJP_WL"))["Id"].ToString();
                 String ph = this.Model.GetValue("F_260_PH") == null ? "" : ((DynamicObject)this.Model.GetValue("F_260_PH"))["Id"].ToString();              
                 string KSRQ = this.Model.GetValue("F_260_KSRQ") == null ? "" :  this.Model.GetValue("F_260_KSRQ").ToString();
                string JSRQ = this.Model.GetValue("F_260_JSRQ") == null ? "" : this.Model.GetValue("F_260_JSRQ").ToString();
                 string[] rs = new string[5];
                 if (wl != null && ph!=null && KSRQ!=null && JSRQ != null)
                 {
                    rs[0] = wl;
                    rs[1] = ph;
                    rs[2] = KSRQ;
                    rs[3] = JSRQ;
                    rs[4] = this.Model.GetValue("F_260_FX").ToString();
                    this.View.ReturnToParentWindow(rs);
                    this.View.Close();
                }
                 else
                {
                    throw new KDBusinessException("", "物料和批号和日期不能为空！");
                }
               
            }

        }
        public override void BeforeF7Select(BeforeF7SelectEventArgs e)
        {
            base.BeforeF7Select(e);        
            if (e.FieldKey.EqualsIgnoreCase("F_260_PH"))
            {
                 string a = this.Model.GetValue("F_RUJP_WL")==null?null:((DynamicObject)this.Model.GetValue("F_RUJP_WL"))["Id"].ToString();
                 string FMA ="FMATERIALID" + "=" +Convert.ToInt32(a);
                e.ListFilterParameter.Filter = e.ListFilterParameter.Filter.JoinFilterString(FMA);
                return;
            }
        }
    }
}
