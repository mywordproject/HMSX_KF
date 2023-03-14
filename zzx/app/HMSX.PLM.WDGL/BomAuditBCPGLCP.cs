using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.ComponentModel;

namespace HMSX.PLM.WDGL
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("物料清单审核半成品关联成品")]
    public class BomAuditBCPGLCP:AbstractOperationServicePlugIn
    {
        private long sid;
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            e.FieldKeys.Add("FMATERIALID");
        }
        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);
            long orid = this.Context.CurrentOrganizationInfo.ID;
            if (orid == 100026)
            {
                foreach (DynamicObject entity in e.DataEntitys)
                {
                    if (entity != null)
                    {
                        DynamicObject FWL = (DynamicObject)entity["MATERIALID"];
                        long FID = Convert.ToInt64(FWL["Id"]);
                        string FBM = FWL["Number"].ToString();
                        if (FBM.Contains("260.02")|| FBM.Contains("260.03"))
                        {
                            string cpsql;
                            if (FBM.Contains("260.02"))
                            {
                                cpsql = $@"/*dialect*/select distinct a.FMATERIALID 成品,b.FMATERIALID 半成品 from T_ENG_BOM a
                                inner join T_ENG_BOMCHILD b on a.FID=b.FID
                                inner join T_BD_MATERIAL zwl on zwl.FMATERIALID = b.FMATERIALID and zwl.FNUMBER like '260.03%'
                                where a.FDOCUMENTSTATUS = 'C' and a.FFORBIDSTATUS = 'A' and a.FUSEORGID=100026 and a.FMATERIALID={FID}";                               
                            }
                            else
                            {
                                cpsql = $@"/*dialect*/select distinct cp.F_260_FXCP 成品,b.FMATERIALID 半成品 from T_ENG_BOM a
                                inner join T_ENG_BOMCHILD b on a.FID = b.FID
                                inner join T_BD_MATERIAL zwl on zwl.FMATERIALID = b.FMATERIALID and zwl.FNUMBER like '260.03%'
                                left join PAEZ_t_Cust_Entry100301 cp on cp.FMATERIALID=a.FMATERIALID
                                where a.FDOCUMENTSTATUS = 'C' and a.FFORBIDSTATUS = 'A' and a.FUSEORGID = 100026 and a.FMATERIALID ={ FID}";
                            }
                            DynamicObjectCollection cps = DBUtils.ExecuteDynamicObject(this.Context, cpsql);
                            //获取最大流水号
                            string idsql = "/*dialect*/select top 1 FPKID from PAEZ_t_Cust_Entry100301 order by FPKID DESC";
                            DynamicObjectCollection ids = DBUtils.ExecuteDynamicObject(this.Context, idsql);
                            if (ids.Count > 0) { sid = Convert.ToInt64(ids[0]["FPKID"]); }
                            else { sid = 100000; }
                            sid++;
                            //添加父项产品
                            foreach (DynamicObject cp in cps)
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
                }
            }
        }
        private void setBCP(long fid, long cpid)
        {
            string bcpsql = @"/*dialect*/select distinct b.FMATERIALID 子物料 from T_ENG_BOM a
                    inner join T_BD_MATERIAL fwl on fwl.FMATERIALID = a.FMATERIALID and fwl.FNUMBER like '260.03%'
                    inner join T_ENG_BOMCHILD b on a.FID=b.FID
                    inner join T_BD_MATERIAL zwl on zwl.FMATERIALID = b.FMATERIALID and zwl.FNUMBER like '260.03%'
                    where a.FDOCUMENTSTATUS = 'C' and a.FFORBIDSTATUS = 'A' and a.FUSEORGID=100026 and a.FMATERIALID=" + fid;
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
