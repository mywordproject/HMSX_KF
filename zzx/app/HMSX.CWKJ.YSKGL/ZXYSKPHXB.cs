using Kingdee.BOS.App;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.Report;
using Kingdee.BOS.Util;
using Kingdee.K3.FIN.AR.App.Report;
using System.ComponentModel;

namespace HMSX.CWKJ.YSKGL
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("双新应收单开票核销明细表添加创建人")]
    public class ZXYSKPHXB: ReceivableOpenDetailService
    {
        private string[] tempTables;
        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            IDBService dBService = ServiceHelper.GetService<IDBService>();
            tempTables=dBService.CreateTemporaryTableName(this.Context,1);
            string tempTable = tempTables[0];
            base.BuilderReportSqlAndTempTable(filter, tempTable);
            string sql = $@"/*dialect*/select t1.*,yh.FNAME F_260_CJR into {tableName} from {tempTable} t1
                left join t_AR_receivable ys on ys.FBILLNO = t1.FBILLNO
                left join T_SEC_user yh on yh.FUSERID = ys.FCREATORID";
            DBUtils.Execute(this.Context, sql);
        }
        public override void CloseReport()
        {           
            if (tempTables.IsNullOrEmptyOrWhiteSpace())
            {
                return;
            }
            IDBService dBService = ServiceHelper.GetService<IDBService>();
            dBService.DeleteTemporaryTableName(this.Context, tempTables);
            base.CloseReport();
        }       
    }
}
