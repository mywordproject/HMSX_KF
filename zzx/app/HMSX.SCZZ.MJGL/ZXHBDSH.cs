using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.ComponentModel;

namespace HMSX.SCZZ.MJGL
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("汇报单审核记录实际完工日期")]
    public class ZXHBDSH: AbstractOperationServicePlugIn
    {
        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);
            long orid=this.Context.CurrentOrganizationInfo.ID;            
            if (orid == 100026)
            {
                foreach(DynamicObject entity in e.DataEntitys)
                {
                    long id = Convert.ToInt64(entity["Id"]);
                    string sql = @"/*dialect*/select case when F_260_CHECKBOXWW=1 then ha.FDATE 
	                    when F_260_CHECKBOXWW=0 and hbc.FCHECKTYPE=1 then ha.FDATE
			            else isnull(hbb.FINSPECTTIME,ha.FAPPROVEDATE) end 日期
                        from T_SFC_OPTRPT ha 
                        inner join T_SFC_OPTRPTENTRY hb on ha.FID=hb.FID
                        left join T_SFC_OPTRPTENTRY_B hbb on hb.FENTRYID=hbb.FENTRYID
                        left join T_SFC_OPTRPTENTRY_C hbc on hb.FENTRYID=hbc.FENTRYID
                        where hb.FMONUMBER not like 'MO%' and ha.FID=" + id;
                    string date = DBUtils.ExecuteScalar<string>(this.Context, sql, "");
                    if (date != "")
                    {
                        string usql = $"/*dialect*/update T_SFC_OPTRPT set F_260_SJWGRQ='{date}' where FID={id}";
                        DBUtils.Execute(this.Context, usql);
                    }
                }
            }
        }
    }
}
