using Kingdee.BOS.App.Data;
using Kingdee.BOS.BusinessEntity.BusinessFlow;
using Kingdee.BOS.Core.BusinessFlow.PlugIn;
using Kingdee.BOS.Core.BusinessFlow.PlugIn.Args;
using System.ComponentModel;

namespace HMSX.GYL.CGGL
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("260入库单审核日期反写收料单到货日期")]
    public class ZXDHRQFX:AbstractBusinessFlowServicePlugIn
    {
        public override void AfterCommitAmount(AfterCommitAmountEventArgs e)
        {
            base.AfterCommitAmount(e);
            try
            {
                if (e.Rule.Id == "5826e1fe-d86d-4ba0-86ee-264eabec6d2e")
                {
                    var sid = e.SourceActiveRow["Id"];
                    string ssql = $"/*dialect*/select F_260_SHRQ from T_PUR_ReceiveEntry where FENTRYID={sid}";
                    string DHdate = DBUtils.ExecuteScalar<string>(this.Context, ssql, "");
                    WRule<Id> rk = (WRule<Id>)e.WriteBackRuleRow;
                    string FXdate = e.SourceActiveRow["F_260_SHRQ"] == null ? "" : e.SourceActiveRow["F_260_SHRQ"].ToString();
                    var xid = rk.TargetDataEntity["Id"];
                    string xsql = $"/*dialect*/select FAPPROVEDATE from T_STK_INSTOCKENTRY a inner join t_STK_InStock b on a.FID=b.FID where FENTRYID={xid}";
                    string SHdate = DBUtils.ExecuteScalar<string>(this.Context, xsql, "");
                    if (DHdate == "" || DHdate == SHdate){return;}
                    else{e.SourceActiveRow["F_260_SHRQ"] = DHdate;}
                }               
            }
            catch{return;}
        }
    }
}
