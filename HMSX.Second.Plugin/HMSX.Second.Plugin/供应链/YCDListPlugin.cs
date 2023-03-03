using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List.PlugIn;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.供应链
{
    [Description("预测单--更新出库数量")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class YCDListPlugin: AbstractListPlugIn
    {
        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);
            if (e.BarItemKey.Equals("KEEP_GX"))
            {
                if (Context.CurrentOrganizationInfo.ID == 100026)
                {
                    string upsql = $@"/*dialect*/update T_PLN_FORECASTENTRY set F_260_SETTLEMENTQTY=FSTOCKOUTQTY from (
                     select FSRCBILLNO,FSCRENTRYID,sum(FSTOCKOUTQTY)FSTOCKOUTQTY,FSRCTYPE from T_SAL_ORDER  a
                     left join T_SAL_ORDERENTRY b on b.fid=a.FID
                     left join T_SAL_ORDERENTRY_R c on c.fentryid=b.fentryid 
                     WHERE FSRCBILLNO like 'FO%' and FSALEORGID=100026 
                     and FSRCTYPE='PLN_FORECAST' and  FDATE>='2020-10-01'
                     group by FSRCBILLNO,FSCRENTRYID,FSRCTYPE
                     )aa where T_PLN_FORECASTENTRY.fentryid=aa.FSCRENTRYID";
                    DBUtils.Execute(Context, upsql);
                    this.View.ShowMessage("批量更新成功！");
                }
            }
        }
    }
}
