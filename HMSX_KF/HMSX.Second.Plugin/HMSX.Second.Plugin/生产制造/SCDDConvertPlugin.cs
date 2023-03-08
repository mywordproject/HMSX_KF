using System;
using System.Collections.Generic;
using System.ComponentModel;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn.Args;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.App;
using Kingdee.BOS.Core.Metadata.FieldElement;
using System.Linq;
using Kingdee.BOS.App.Data;

namespace HMSX.Second.Plugin.生产制造
{
    [Description("生产订单下推过滤")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class SCDDConvertPlugin : AbstractConvertPlugIn
    {
        string str = "";
        public override void OnParseFilter(ParseFilterEventArgs e)
        {
            base.OnParseFilter(e);
            if (this.Context.CurrentOrganizationInfo.ID == 100026)
            {
                string cxsql = $@"select FMATERIALID,FSTOCKID,FBOMID from  T_PRD_MOENTRY a
                inner join T_PRD_MOENTRY_A b on a.fentryid=b.FENTRYID 
                INNER join T_PRD_MO c on a.fid=c.fid
                where FSTATUS=3 and substring(FBILLNO,1,2)='MO' and FPRDORGID=100026 and a.FENTRYID in {str}
                group by FMATERIALID,FSTOCKID,FBOMID
                having count(*)<2";
                var cxs = DBUtils.ExecuteDynamicObject(Context, cxsql);
                if (cxs.Count > 0)
                {
                    string FMATERIALID = "";
                    foreach (var cx in cxs)
                    {
                        FMATERIALID += cx["FMATERIALID"] + ",";
                    }
                    e.FilterPolicySQL = e.FilterPolicySQL + "and" + string.Format(" FMATERIALID not in ({0})", FMATERIALID.Trim(','));
                }
            }

        }
        public override void OnParseFilterOptions(ParseFilterOptionsEventArgs e)
        {
            base.OnParseFilterOptions(e);
        }
        public override void OnInSelectedRow(InSelectedRowEventArgs e)
        {
            base.OnInSelectedRow(e);
            str = e.InSelectedRowsSQL;
            int x = str.IndexOf("(");
            int z = str.Length;
            str = str.Substring(x, z - x);
        }
        public override void OnGetSourceData(GetSourceDataEventArgs e)
        {
            base.OnGetSourceData(e);
        }
        public override void OnBeforeGetSourceData(BeforeGetSourceDataEventArgs e)
        {
            base.OnBeforeGetSourceData(e);
        }
    }
}

