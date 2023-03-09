using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using System;
using System.ComponentModel;

namespace HMSX.SCZZ.MJGL
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("汇报单审核前提示")]
    public class ZXHBDSHQTS: AbstractBillPlugIn
    {
        public override void BarItemClick(BarItemClickEventArgs e)
        {
            base.BarItemClick(e);
            if(this.Context.CurrentOrganizationInfo.ID==100026 &&(e.BarItemKey== "tbApprove" || e.BarItemKey== "tbSplitApprove"))
            {
                string fbillno = this.View.Model.GetValue("FBillNo").ToString();
                string ddh = this.View.Model.GetValue("FMoNumber",0).ToString();
                int ww = Convert.ToInt32(this.View.Model.GetValue("F_260_CheckBoxWW", 0));
                string fs = this.View.Model.GetValue("FCheckType", 0).ToString();
                if (!ddh.StartsWith("MO") && ww==0 && fs!="1")
                {
                    string sql = $@"/*dailect*/select DATEDIFF(DAY,ISNULL(hbb.FINSPECTTIME,GETDATE()),GETDATE()) 天数
                        from T_SFC_OPTRPT ha
                        inner join T_SFC_OPTRPTENTRY hb on ha.FID = hb.FID
                        left join T_SFC_OPTRPTENTRY_B hbb on hb.FENTRYID = hbb.FENTRYID                       
                        where ha.FBILLNO = '{fbillno}'";
                    int day = DBUtils.ExecuteScalar<int>(this.Context, sql, 0);
                    if (day > 0)
                    {
                        e.Cancel = true;
                        this.View.ShowMessage("审核日期与检验时间不在同一天，将影响完工工时统计，是否继续审核？", MessageBoxOptions.OKCancel, result =>
                        {
                            if (result == MessageBoxResult.OK)
                            {
                                this.View.InvokeFormOperation("Audit");
                            }
                        });                       
                    }
                }                           
            }
        }
    }
}
