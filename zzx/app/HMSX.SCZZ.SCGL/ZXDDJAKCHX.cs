using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.K3.Core.MFG.EntityHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace HMSX.SCZZ.SCGL
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("成品订单结案对应半成品多余库存回写库存分配表")]
    public class ZXDDJAKCHX: AbstractOperationServicePlugIn
    {
        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);
            OperateResultCollection results = this.OperationResult.OperateResult;
            foreach(OperateResult result in results)
            {
                long enid =Convert.ToInt64(result.PKValue);
                string sql = $@"/*dialect*/select FBILLNO,FSEQ from T_PRD_MO a
                    inner join T_PRD_MOENTRY b on a.FID=b.FID
                    inner join T_BD_MATERIAL wl on wl.FMATERIALID=b.FMATERIALID
                    where wl.FNUMBER like '260.02%' and FENTRYID={enid}";
                DynamicObjectCollection cpobjs = DBUtils.ExecuteDynamicObject(this.Context, sql);
                if (cpobjs.Count > 0)
                {
                    string MOBI = cpobjs[0]["FBILLNO"].ToString();
                    long SEQ = Convert.ToInt64(cpobjs[0]["FSEQ"]);
                    string cpyl = $@"/*dialect*/select b.FMATERIALID 物料ID,q.FPICKEDQTY+q.FREPICKEDQTY-q.FGOODRETURNQTY 领料数 
                        from T_PRD_PPBOM a
                        inner join T_PRD_PPBOMENTRY b on a.FID=b.FID
                        inner join T_PRD_PPBOMENTRY_Q q on q.FENTRYID=b.FENTRYID
                        inner join T_BD_MATERIAL wl on b.FMATERIALID=wl.FMATERIALID                    
                        where a.FPRDORGID=100026 and wl.FNUMBER like '260.03%' and a.FMOBILLNO='{MOBI}' and a.FMOENTRYSEQ={SEQ}";
                    DynamicObjectCollection objs = DBUtils.ExecuteDynamicObject(this.Context, cpyl);
                    foreach(DynamicObject obj in objs)
                    {
                        long wlid = Convert.ToInt64(obj["物料ID"]);                       
                    }
                }                
            }
        }
        private void getdd(long wlid,string MOBI,int SEQ,double lls)
        {
            string sql = $@"/*dialect*/select sum(FQTY) 订单数 from T_PRD_MO a
                inner join T_PRD_MOENTRY b on a.FID = b.FID
                where FSRCBILLNO = '{MOBI}' and FSRCBILLENTRYSEQ = { SEQ } and b.FMATERIALID ={ wlid}";
            DynamicObjectCollection dd = DBUtils.ExecuteDynamicObject(this.Context, sql);
            if (dd.Count > 0)
            {
                double jys = Convert.ToDouble(dd[0]["订单数"])-lls;
                if (jys > 0)
                {
                    string usql = $"/*dialect*/UPDATE SX_WLKCJY set SYKC=SYKC+{jys} where WLID={wlid}";
                }
            }
        }
        private void DdStart(string MOBI,int SEQ,long wlid)
        {
            string ylsql = $@"/*dialect*/select b.FMATERIALID 物料ID,q.FPICKEDQTY+q.FREPICKEDQTY-q.FGOODRETURNQTY 领料数 
                from T_PRD_PPBOM a
                inner join T_PRD_PPBOMENTRY b on a.FID=b.FID
                inner join T_PRD_PPBOMENTRY_Q q on q.FENTRYID=b.FENTRYID
                inner join T_BD_MATERIAL wl on b.FMATERIALID=wl.FMATERIALID
                inner join T_PRD_MO da on da.FBILLNO=a.FMOBILLNO
                inner join T_PRD_MOENTRY db on da.FID=db.FID and db.FSEQ=a.FMOENTRYSEQ and db.FSRCBILLNO='{MOBI}' and db.FSRCBILLENTRYSEQ={SEQ}
                where a.FPRDORGID=100026 and wl.FNUMBER like '260.03%'";

            string ddsql = $@"/*dialect*/select sum(FQTY) 订单数 from T_PRD_MO a
                inner join T_PRD_MOENTRY b on a.FID=b.FID
                where FSRCBILLNO='{MOBI}' and FSRCBILLENTRYSEQ={SEQ} and b.FMATERIALID={wlid}";
        }
    }
}
