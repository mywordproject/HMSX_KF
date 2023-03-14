using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List.PlugIn;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.ComponentModel;

namespace HMSX.PLM.WDGL
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("物料列表半成品关联成品")]
    public class BCPGLCP:AbstractListPlugIn
    {
        private long sid;
        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);
            long orid = this.Context.CurrentOrganizationInfo.ID;
            if (e.BarItemKey.Equals("PAEZ_260BCPGLCP") && orid==100026)
            {
                //半成品关联成品
                string cpsql = @"/*dialect*/select distinct a.FMATERIALID 成品,b.FMATERIALID 半成品 from T_ENG_BOM a
                    inner join T_BD_MATERIAL fwl on fwl.FMATERIALID = a.FMATERIALID and fwl.FNUMBER like '260.02%'
                    inner join T_ENG_BOMCHILD b on a.FID=b.FID
                    inner join T_BD_MATERIAL zwl on zwl.FMATERIALID = b.FMATERIALID and zwl.FNUMBER like '260.03%'
                    where a.FDOCUMENTSTATUS = 'C' and a.FFORBIDSTATUS = 'A' and a.FUSEORGID=100026";
                DynamicObjectCollection cps = DBUtils.ExecuteDynamicObject(this.Context, cpsql);
                //获取最大流水号
                string idsql = "/*dialect*/select top 1 FPKID from PAEZ_t_Cust_Entry100301 order by FPKID DESC";
                DynamicObjectCollection ids = DBUtils.ExecuteDynamicObject(this.Context, idsql);
                if (ids.Count > 0){sid =Convert.ToInt64(ids[0]["FPKID"]);}
                else { sid = 100000; }
                sid++;
                //添加父项产品
                foreach(DynamicObject cp in cps)
                {
                    long cpid = Convert.ToInt64(cp["成品"]);
                    long bcpid = Convert.ToInt64(cp["半成品"]);
                    string jysql = $"/*dialect*/select * from PAEZ_t_Cust_Entry100301 where FMATERIALID={bcpid} and F_260_FXCP={cpid} ";
                    DynamicObjectCollection jl = DBUtils.ExecuteDynamicObject(this.Context, jysql);
                    if (jl.Count > 0) { setBCP(bcpid, cpid); }
                    else
                    {
                        string upsql = $"/*dialect*/insert into PAEZ_t_Cust_Entry100301(FPKID,FMATERIALID,F_260_FXCP) values({sid},{bcpid},{cpid})";
                        DBUtils.Execute(this.Context, upsql);
                        sid++;
                        setBCP(bcpid, cpid);
                    }                   
                }
            }
        }
        private void setBCP(long fid,long cpid)
        {
            string bcpsql = @"/*dialect*/select distinct b.FMATERIALID 子物料 from T_ENG_BOM a
                    inner join T_BD_MATERIAL fwl on fwl.FMATERIALID = a.FMATERIALID and fwl.FNUMBER like '260.03%'
                    inner join T_ENG_BOMCHILD b on a.FID=b.FID
                    inner join T_BD_MATERIAL zwl on zwl.FMATERIALID = b.FMATERIALID and zwl.FNUMBER like '260.03%'
                    where a.FDOCUMENTSTATUS = 'C' and a.FFORBIDSTATUS = 'A' and a.FUSEORGID=100026 and a.FMATERIALID="+fid;
            DynamicObjectCollection zwls = DBUtils.ExecuteDynamicObject(this.Context, bcpsql);
            //添加父项产品
            foreach (DynamicObject zwl in zwls)
            {
                string jysql = $"/*dialect*/select * from PAEZ_t_Cust_Entry100301 where FMATERIALID={zwl["子物料"]} and F_260_FXCP={cpid} ";
                DynamicObjectCollection jl = DBUtils.ExecuteDynamicObject(this.Context, jysql);
                if (jl.Count > 0) { setBCP(Convert.ToInt64(zwl["子物料"]), cpid); }
                else
                {
                    string upsql = $"/*dialect*/insert into PAEZ_t_Cust_Entry100301(FPKID,FMATERIALID,F_260_FXCP) values({sid},{zwl["子物料"]},{cpid})";
                    DBUtils.Execute(this.Context, upsql);
                    sid++;
                    setBCP(Convert.ToInt64(zwl["子物料"]), cpid);
                }
            }
        }
    }
}
