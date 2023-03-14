using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Bill.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.ComponentModel;

namespace HMSX.SCZZ.SCGL
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("生产订单10%超额控制01")]
    public class ZXSCDDKZ: AbstractBillPlugIn
    {
        private bool isget = false;
        private long sjid = 0;
        private double xqdd=0;
        public override void BeforeSave(BeforeSaveEventArgs e)
        {
            base.BeforeSave(e);
            long orid = this.Context.CurrentOrganizationInfo.ID;
            if (orid == 100026)
            {
                try
                {
                    int hs = this.View.Model.GetEntryRowCount("FTreeEntity");
                    var billno = this.View.Model.GetValue("FBillNo");
                    string filter;
                    if (billno == null)
                    {
                        filter = "";
                    }
                    else { filter = $" and FBILLNO!='{billno}'"; }
                    string ErrMessage = "";
                    for (int i = 0; i < hs; i++)
                    {
                        DynamicObject wl = (DynamicObject)this.View.Model.GetValue("FMaterialId", i);
                        long wlid = Convert.ToInt64(wl["Id"]);
                        string wldm = wl["Number"].ToString();
                        double sl = Convert.ToDouble(this.View.Model.GetValue("FQty", i));
                        string srcBillno = this.View.Model.GetValue("FSrcBillNo", i).ToString();
                        string srcSeq = this.View.Model.GetValue("FSrcBillEntrySeq", i).ToString();
                        if (srcBillno.StartsWith("MO") && wldm.StartsWith("260.03"))
                        {
                            this.GetDDS(srcBillno, srcSeq, wlid);
                            string ddsql = $@"/*dialect*/select sum(FQTY) 数量 from T_PRD_MOENTRY b inner join T_PRD_MO a on a.FID=B.FID
                                            where b.FSRCBILLNO='{srcBillno}' and b.FSRCBILLENTRYSEQ={srcSeq} and b.FMATERIALID={wlid}{filter}";
                            DynamicObjectCollection ddObj = DBUtils.ExecuteDynamicObject(this.Context, ddsql);
                            double dds = ddObj.Count > 0 ? Convert.ToDouble(ddObj[0]["数量"]) : 0;                            
                            if (sl > Math.Round(xqdd, 4) - dds)
                            {                               
                                ErrMessage += $"第{i + 1}分录数量超出成品订单10%限制;\n";
                            }
                        }
                        xqdd = 0;
                    }
                    if (ErrMessage != "")
                    {
                        e.Cancel = true;
                        this.View.ShowErrMessage(ErrMessage);
                    }
                }
                catch { return; }
            }
        }
        public override void AfterCreateNewData(EventArgs e)
        {
            base.AfterCreateNewData(e);
            long orid = this.Context.CurrentOrganizationInfo.ID;
            if (orid == 100026)
            {
                try
                {
                    int hs = this.View.Model.GetEntryRowCount("FTreeEntity");
                    var billno = this.View.Model.GetValue("FBillNo");                   
                    string filter;
                    if (billno == null)
                    {
                        filter = "";
                    }
                    else { filter = $" and FBILLNO!='{billno}'"; }
                    for (int i = 0; i < hs; i++)
                    {
                        DynamicObject wl = (DynamicObject)this.View.Model.GetValue("FMaterialId", i);
                        long wlid = Convert.ToInt64(wl["Id"]);
                        string wldm = wl["Number"].ToString();
                        string srcBillno = this.View.Model.GetValue("FSrcBillNo", i).ToString();
                        string srcSeq = this.View.Model.GetValue("FSrcBillEntrySeq", i).ToString();                       
                        if (srcBillno.StartsWith("MO") && wldm.StartsWith("260.03"))
                        {
                            this.GetDDS(srcBillno, srcSeq, wlid);
                            string ddsql = $@"/*dialect*/select sum(FQTY) 数量 from T_PRD_MOENTRY b inner join T_PRD_MO a on a.FID=B.FID
                                            where b.FSRCBILLNO='{srcBillno}' and b.FSRCBILLENTRYSEQ={srcSeq} and b.FMATERIALID={wlid}{filter}";
                            DynamicObjectCollection ddObj = DBUtils.ExecuteDynamicObject(this.Context, ddsql);
                            double dds = ddObj.Count > 0 ? Convert.ToDouble(ddObj[0]["数量"]) : 0;
                            double QTY = Math.Round(xqdd, 4) - dds;
                            QTY = QTY > 0 ? QTY : 0;
                            this.View.Model.SetValue("FQty", QTY, i);
                            this.View.Model.SetValue("FStockInLimitH", QTY, i);
                            this.View.Model.SetValue("FBaseStockInLimitH", QTY, i);
                            this.View.Model.SetValue("FStockInLimitL", QTY, i);
                            this.View.Model.SetValue("FBaseStockInLimitL", QTY, i);
                            this.View.Model.SetValue("FBaseUnitQty", QTY, i);
                            this.View.Model.SetValue("FNoStockInQty", QTY, i);
                        }
                        xqdd = 0;
                    }
                }
                catch { return; }
            }
        }
        //获取需求订单数
        private void GetDDS(string srcBillno, string srcSeq,long zid)
        {
            string cpSQL = $"/*dialect*/select FMATERIALID,FBOMID,FQTY from T_PRD_MO a inner join T_PRD_MOENTRY b on a.FID=b.FID where FBILLNO='{srcBillno}' and FSEQ={srcSeq}";
            DynamicObjectCollection cpobj = DBUtils.ExecuteDynamicObject(this.Context, cpSQL);
            double cpQTY = Convert.ToDouble(cpobj[0]["FQTY"]);
            long cpid = Convert.ToInt64(cpobj[0]["FMATERIALID"]);
            this.Bom(srcBillno, srcSeq, Convert.ToInt64(cpobj[0]["FBOMID"]), cpid, zid);            
            this.FPKC(srcBillno, srcSeq, cpQTY * 1.1, cpid, zid);
            isget = false;
        }
        //分配各级库存
        private void FPKC(string MOBI,string SEQ,double sjxq,long fid,long zid)
        {
            string fpsql = $@"/*dialect*/select ZID,
                case when {sjxq} * FYL - FPSL > ISNULL(kc.SYKC,0) then ISNULL(kc.SYKC,0) else {sjxq} * FYL - FPSL end 分配数,
                case when {sjxq} * FYL - FPSL > ISNULL(kc.SYKC,0) then {sjxq} * FYL - FPSL - ISNULL(kc.SYKC,0) else 0 end 需求数,
                case when {sjxq} * FYL - FPSL > ISNULL(kc.SYKC,0) then 0 else ISNULL(kc.SYKC,0) - ({sjxq} * FYL - FPSL) end 库存剩余
                from SX_MOKCFP a left join SX_WLKCJY kc on a.ZID = kc.WLID
                where MOBILLNO = '{MOBI}' and MOFSEQ = {SEQ} and a.WLID = {fid}";
            if (sjxq > 0)
            {               
                DynamicObjectCollection obj = DBUtils.ExecuteDynamicObject(this.Context, fpsql);
                foreach (DynamicObject zx in obj)
                {
                    long zxid = Convert.ToInt64(zx["ZID"]);
                    string upFPsql = $"/*dialect*/update SX_MOKCFP set FPSL=FPSL+{Convert.ToDouble(zx["分配数"])} where MOBILLNO = '{MOBI}' and MOFSEQ = {SEQ} and ZID = {zxid}";
                    string upKCsql = $"/*dialect*/update SX_WLKCJY set SYKC = {Convert.ToDouble(zx["库存剩余"])} where WLID = {zxid}";
                    DBUtils.Execute(this.Context, upFPsql);
                    DBUtils.Execute(this.Context, upKCsql);
                    if (zxid != zid)
                    {
                        this.FPKC(MOBI, SEQ, Convert.ToDouble(zx["需求数"]), zxid, zid);
                    }
                    else
                    {                       
                        xqdd = Convert.ToDouble(zx["需求数"]);                     
                    }                    
                }                
            }
        }
        //获取各级物料及用料比例
        private void Bom(string MOBI,string FSEQ,long bomid, long fid, long zid)
        {            
            string bomsql;
            if (bomid > 0)
            {
                bomsql = $@"/*dialect*/select b.FMATERIALID 子物料,FBOMID 子BOM,FNUMERATOR/FDENOMINATOR*(1+FSCRAPRATE/100) 用量
                from T_ENG_BOM a inner join T_ENG_BOMCHILD b on a.FID=b.FID
                inner join T_BD_MATERIAL wl on wl.FMATERIALID=b.FMATERIALID
                where a.FDOCUMENTSTATUS='C' and a.FFORBIDSTATUS = 'A' and wl.FNUMBER like '260.03%' and a.FID={bomid}";
            }
            else
            {
                bomsql = $@"/*dialect*/select b.FMATERIALID 子物料,FBOMID 子BOM,FNUMERATOR/FDENOMINATOR*(1+FSCRAPRATE/100) 用量 from 
                (select top 1 FID from T_ENG_BOM where FDOCUMENTSTATUS='C' and FBOMUSE!='3' and FFORBIDSTATUS = 'A' and FMATERIALID={fid} order by FCREATEDATE DESC) a 
                inner join T_ENG_BOMCHILD b on a.FID=b.FID inner join T_BD_MATERIAL wl on wl.FMATERIALID=b.FMATERIALID
                where wl.FNUMBER like '260.03%'";
            }
            DynamicObjectCollection objects = DBUtils.ExecuteDynamicObject(this.Context, bomsql);
            foreach (DynamicObject obj in objects)
            {
                long zwl = Convert.ToInt64(obj["子物料"]);
                long zbom = Convert.ToInt64(obj["子BOM"]);
                double yl = Convert.ToDouble(obj["用量"]);
                if (zid == zwl)
                {
                    isget = true;
                    sjid = fid;
                    this.GetKC(zwl);
                    this.LYJL(MOBI, FSEQ, fid,zwl,yl);
                    break;
                }
                else
                {
                    this.Bom(MOBI,FSEQ,zbom, zwl, zid);
                }
                if (isget) 
                {
                    this.GetKC(zwl);
                    this.LYJL(MOBI, FSEQ, fid,zwl, yl);
                    break; 
                }
            }            
        }
        //更新库存信息
        private void GetKC(long wlid)
        {
            string kcjl = $"select SYKC from SX_WLKCJY where WLID={wlid}";
            DynamicObjectCollection jlobj = DBUtils.ExecuteDynamicObject(this.Context, kcjl);
            if (jlobj.Count == 0)
            {
                string jskc=$@"/*dialect*/select sum(数量) 数量 from
                    (select sum(FBASEQTY) 数量 from T_STK_INVENTORY kc
                    inner join T_BD_STOCK_L ck on kc.FSTOCKID = ck.FSTOCKID
					inner join T_BD_STOCK cka on kc.FSTOCKID = cka.FSTOCKID
                    inner join T_BD_STOCKSTATUS_L zt on zt.FSTOCKSTATUSID = kc.FSTOCKSTATUSID
                    where FMATERIALID = {wlid} and FBASEQTY> 0 and ck.FNAME not like '%不良%' and zt.FNAME = '可用' and cka.FSTOCKPROPERTY in ('1','2')
                    union all
                    select -FSAFESTOCK 数量  from t_BD_MaterialStock where FMATERIALID={wlid})A";
                DynamicObjectCollection kcobj = DBUtils.ExecuteDynamicObject(this.Context, jskc);
                if (kcobj.Count > 0)
                {
                    double kc = Convert.ToDouble(kcobj[0]["数量"]);                   
                    string isql = $"/*dialect*/insert into SX_WLKCJY(WLID,CSKC,SYKC) values ({wlid},{kc},{kc})";
                    DBUtils.Execute(this.Context, isql);                  
                }
            }
        }
        //更新应领记录表
        private void LYJL(string MOBI,string SEQ,long WLID,long ZID,double yl)
        {
            string jlsql = $"/*dialect*/select * from SX_MOKCFP where MOBILLNO='{MOBI}' and MOFSEQ={SEQ} and WLID={WLID} and ZID={ZID}";
            DynamicObjectCollection jlobj = DBUtils.ExecuteDynamicObject(this.Context, jlsql);
            if (jlobj.Count == 0)
            {
                string isql = $"/*dialect*/insert into SX_MOKCFP(MOBILLNO,MOFSEQ,WLID,ZID,FYL,FPSL,SYSL) values ('{MOBI}',{SEQ},{WLID},{ZID},{yl},0,0)";
                DBUtils.Execute(this.Context, isql);
            }
        }
        //获取上级已领料数
        private double GetZZS(string MOBI,string SEQ,long WLID,long cpid)
        {
            string sjSQL;
            if (sjid == cpid)
            {
                sjSQL = $@"/*dialect*/select sum(FWIPQTY) WIPQTY from T_PRD_PPBOM a
                    inner join T_PRD_PPBOMENTRY b on a.FID = b.FID
                    inner join T_PRD_PPBOMENTRY_Q c on c.FENTRYID = b.FENTRYID
                    where a.FMOBILLNO ='{MOBI}' and a.FMOENTRYSEQ ={SEQ} and b.FMATERIALID ={WLID}
                    group by a.FMOBILLNO,a.FMOENTRYSEQ,b.FMATERIALID";
            }
            else
            {
                sjSQL = $@"select sum(FWIPQTY) WIPQTY from T_PRD_MOENTRY b
                    inner join T_PRD_MO a on a.FID = b.FID
                    inner join T_PRD_PPBOM ya on ya.FMOBILLNO = a.FBILLNO and ya.FMOENTRYSEQ = b.FSEQ
                    inner join T_PRD_PPBOMENTRY yb on ya.FID = yb.FID
                    inner join T_PRD_PPBOMENTRY_Q yc on yc.FENTRYID = yb.FENTRYID
                    where b.FSRCBILLNO = '{MOBI}' and b.FMATERIALID={sjid} and b.FSRCBILLENTRYSEQ ={SEQ} and yb.FMATERIALID ={WLID}
                    group by b.FSRCBILLNO,b.FSRCBILLENTRYSEQ,yb.FMATERIALID";
            }
            DynamicObjectCollection sjobj = DBUtils.ExecuteDynamicObject(this.Context, sjSQL);
            double zzs = sjobj.Count == 0 ? 0 : Convert.ToDouble(sjobj[0]["WIPQTY"]);
            return zzs;
        }
    }
}
