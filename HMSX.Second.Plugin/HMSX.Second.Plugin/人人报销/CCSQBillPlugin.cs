using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Bill.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.人人报销
{
    [Description("出差申请，金额控制")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class CCSQBillPlugin : AbstractBillPlugIn
    {
        public override void BeforeSave(BeforeSaveEventArgs e)
        {
            base.BeforeSave(e);
            if (((DynamicObject)this.Model.GetValue("FORGID"))["Id"].ToString() == "100026")
            {
                string xc = this.Model.GetValue("F_HMD_XC") == null ? "" : this.Model.GetValue("F_HMD_XC").ToString();
                string fbill = this.Model.GetValue("FBILLNO") == null ? "" : this.Model.GetValue("FBILLNO").ToString();
                string sql = fbill == "" ? "1=1" : "FBILLNO!='" + fbill + "'";
                var dates = this.Model.DataObject["FEntity"] as DynamicObjectCollection;
                double zje = 0;
                foreach (var date in dates)
                {
                    string fyxm = date["ExpenseItemID"] == null ? "" : ((DynamicObject)date["ExpenseItemID"])["Number"].ToString();
                    if (date["F_260_CLFLB"].ToString() == "50" &&
                      (date["F_260_GWDJ"].ToString() == "10" || date["F_260_GWDJ"].ToString() == "20")&& xc=="2")
                    {
                        zje += Convert.ToDouble(date["LocalCost"].ToString());
                    }
                }
                DateTime sqsj = Convert.ToDateTime(this.Model.GetValue("FDATE").ToString());
                string sqrid = this.Model.GetValue("FSTAFFID") == null ? "" : ((DynamicObject)this.Model.GetValue("FSTAFFID"))["Id"].ToString();
                string ccsqsql = $@"select sum(b.FLOCALCOST) JE from T_ER_ExpenseRequest a
                             inner join T_ER_ExpenseRequestEntry b on a.fid=b.fid
                             inner join T_BD_EXPENSE c on c.FEXPID=b.FEXPENSEITEMID
                             inner join T_HR_EMPINFO d on d.fid=a.FSTAFFID
                             where F_260_CLFLB=50 
                             and F_260_GWDJ in(10,20)                             
                             and year(FDATE)='{sqsj.Year.ToString()}'
                             and month(FDATE)='{sqsj.Month.ToString()}'
                             and d.FID='{sqrid}'
                             and F_HMD_XC=2
                             and {sql} ";
                var ccsp = DBUtils.ExecuteDynamicObject(Context, ccsqsql);
                if (ccsp.Count > 0)
                {
                    zje += Convert.ToDouble(ccsp[0]["JE"].ToString());
                }
                if (zje > 600)
                {
                    throw new KDBusinessException("", "一个月金额不能超过600！");
                }
            }
        }
    }
}
