using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.List.PlugIn;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.ComponentModel;

namespace HMSX.SCZZ.SCGL
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("生成下级订单校验")]
    public class ZXSCXJDDJY : AbstractListPlugIn
    {
        private bool isqx = true;       
        public override void BarItemClick(BarItemClickEventArgs e)
        {
            base.BarItemClick(e);
            long orid = this.Context.CurrentOrganizationInfo.ID;
            if (e.BarItemKey.Equals("tbExpandMOdddd") && orid==100026)
            {
                ListSelectedRowCollection listcoll = this.ListView.SelectedRowsInfo;
                DynamicObjectCollection dycoll = this.ListModel.GetData(listcoll);
                foreach (DynamicObject item in dycoll)
                {
                    string MOBI = item["FBILLNO"].ToString();
                    if (MOBI.StartsWith("MO"))
                    {
                        string seq = item["t1_FSeq"].ToString();
                        long wlid = Convert.ToInt64(item["FMaterialId_Id"]);
                        double qty = Convert.ToDouble(item["FQTY"]);
                        this.Check(MOBI,seq,wlid,qty*1.1);
                        if (isqx)
                        {
                            e.Cancel = true;
                            throw new KDBusinessException("", "已下推完下级订单！");
                        }
                        
                    }
                    break;
                }
                isqx = true;
            }
        }
        private void Check(string MOBI,string SEQ,long wlid,double QTY)
        {
            string sql = $"/*dialect*/select ZID,FYL,FPSL from SX_MOKCFP where MOBILLNO='{MOBI}' and MOFSEQ={SEQ} and WLID={wlid}";
            DynamicObjectCollection objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
            if (objs.Count > 0)
            {
                foreach(DynamicObject obj in objs)
                {
                    string ddsql = $"/*dialect*/select sum(FQTY) 订单数 from T_PRD_MOENTRY where FSRCBILLNO='{MOBI}' and FSRCBILLENTRYSEQ={SEQ} and FMATERIALID={obj["ZID"]}";
                    DynamicObjectCollection xjobj = DBUtils.ExecuteDynamicObject(this.Context, ddsql);
                    double dds = xjobj.Count > 0 ? Convert.ToDouble(xjobj[0]["订单数"]) : 0;
                    double cys = dds + Convert.ToDouble(obj["FPSL"]) - QTY * Convert.ToDouble(obj["FYL"]);
                    if (cys >= -0.0001)
                    {
                        this.Check(MOBI, SEQ, Convert.ToInt64(obj["ZID"]), dds);
                    }
                    else { isqx = false; }
                }               
            }
            else
            {
                string ylsql = $@"/*dialect*/select b.FMATERIALID 子物料,FBOMID 子BOM,FNUMERATOR/FDENOMINATOR*(1+FSCRAPRATE/100) 用量 from 
                (select top 1 FID from T_ENG_BOM where FDOCUMENTSTATUS='C' and FBOMUSE!='3' and FFORBIDSTATUS = 'A' and FMATERIALID={wlid} order by FCREATEDATE DESC) a 
                inner join T_ENG_BOMCHILD b on a.FID=b.FID inner join T_BD_MATERIAL wl on wl.FMATERIALID=b.FMATERIALID
                where wl.FNUMBER like '260.03%'";
                DynamicObjectCollection ylobj = DBUtils.ExecuteDynamicObject(this.Context, ylsql);
                if (ylobj.Count > 0) { isqx = false; }
            }
        }
    }
}
