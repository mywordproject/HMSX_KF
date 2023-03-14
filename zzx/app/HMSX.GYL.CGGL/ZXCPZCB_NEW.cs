using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.ComponentModel;

namespace HMSX.GYL.CGGL
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("产品总成本_新版_版本号管理")]
    public class ZXCPZCB_NEW: AbstractBillPlugIn
    {
        //产品合格率
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (e.Field.Key == "FHGL")
            {
                double zll = 1.0;
                int jghs = this.View.Model.GetEntryRowCount("F_HMD_Entity");
                for(int i = 0; i < jghs; i++)
                {
                    double hgl = Convert.ToDouble(this.View.Model.GetValue("FHGL", i));
                    if (hgl > 0) { zll *= hgl; }
                }
                this.View.Model.SetValue("FCPHGL",zll,0);
            }
        }
        //升版
        public override void BarItemClick(BarItemClickEventArgs e)
        {
            base.BarItemClick(e);
            if (e.BarItemKey == "HMD_tbButton_2")
            {
                this.View.OpenParameter.SetCustomParameter("FLX", "升版");
            }
        }
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            if (this.View.OpenParameter.CreateFrom == CreateFrom.Copy)
            {
                var lx=this.View.OpenParameter.GetCustomParameter("FLX");
                string sb = lx == null ? "" : lx.ToString();
                if (sb == "升版")
                {
                    string bbh = this.View.Model.GetValue("FBBH", 0).ToString();
                    if (this.View.Model.GetValue("FBillNo", 0) == null && bbh != "")
                    {
                        string cpbh = this.View.Model.GetValue("FCPBH", 0).ToString();
                        string sql = $"/*dialect*/select distinct FBBH from PAEZ_t_Cust100369 where FCPBH='{cpbh}'";
                        DynamicObjectCollection objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
                        if (objs.Count > 0)
                        {
                            this.View.Model.SetValue("FBBH", "V" + (objs.Count + 1).ToString(), 0);
                        }
                    }
                } 
            }
        }
    }
}
