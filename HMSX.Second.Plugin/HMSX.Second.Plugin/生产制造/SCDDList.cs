using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.List.PlugIn;
using Kingdee.BOS.Core.List.PlugIn.Args;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.生产制造
{
    [Description("生产订单--已结案不允许生成下级订单")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class SCDDList : AbstractListPlugIn
    {
        public override void BarItemClick(BarItemClickEventArgs e)
        {          
            if (e.BarItemKey.Equals("tbExpandMOdddd"))
            {
                ListSelectedRowCollection listcoll = this.ListView.SelectedRowsInfo;
                foreach (var list in listcoll)
                {
                    if (list.DataRow["FPrdOrgId_Id"].ToString() == "100026")
                    {
                        string ztsql = $@"select * from T_PRD_MO a
                           inner join T_PRD_MOENTRY b on a.FID=b.FID
                           INNER JOIN T_PRD_MOENTRY_A C ON C.FENTRYID=B.FENTRYID
                           inner join T_BD_MATERIAL D ON D.FMATERIALID=B.FMATERIALID
                           WHERE a.FID={list.DataRow["FID"]} AND b.FENTRYID={list.DataRow["t1_FENTRYID"]} AND 
                           (C.FSTATUS not in (3,4)  or (SUBSTRING(D.FNUMBER,1,6)='260.03' and {Context.UserId} not in 
                           (9409480,1231235,3850136,1218222,100115,228417,1231234,1226615,1231233,1231236,5984309,1231237,1323538,15545461,3403812,1296625)))";
                        var zt = DBUtils.ExecuteDynamicObject(Context, ztsql);
                        if (zt.Count > 0)
                        {
                            throw new KDBusinessException("", "已结案或半成品不允许生成下级订单！");
                        }
                    }
                }
            }
            base.BarItemClick(e);
        }
    }
}
