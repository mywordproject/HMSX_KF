using Kingdee.BOS;
using Kingdee.BOS.App.Core.Warn.Data;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Mobile.PlugIn;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.ComplexCacheJson.Model;
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.ComplexCacheJson.Utils;
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.MES
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("扫码绑定")]
    public class SMBDMobilePlugin : AbstractMobilePlugin
    {
        string RLSL = "";
        String ZXBZSL = "";
        string WLDM = "";
        string SCDD = "";
        string SEQ = "";
        string ck = "";
        protected FormCacheModel cacheModel4Save = new FormCacheModel();
        protected bool HasCached;
        public override void OnInitialize(InitializeEventArgs e)
        {
            LoginUtils.CheckAppGroupAndConcurrent(base.Context, base.View);
            base.OnInitialize(e);
            try
            {
                this.View.GetControl("FLable_User").SetValue(this.Context.UserName);
            }
            catch (Exception)
            {
            }
            ZXBZSL = e.Paramter.GetCustomParameter("ZXBZSL").ToString();
            RLSL = e.Paramter.GetCustomParameter("RLSL").ToString();
            WLDM = e.Paramter.GetCustomParameter("WLDM").ToString();
            SCDD = e.Paramter.GetCustomParameter("SCDD").ToString();
            SEQ = e.Paramter.GetCustomParameter("SEQ").ToString();
            string cksql = $@"select C.FNAME,FNUMERATOR 
             from T_PRD_PPBOM a
             left join T_PRD_PPBOMENTRY_C b  on b.fid=a.fid
			 left join T_PRD_PPBOMENTRY b1  on b1.fentryid=b.fentryid
             left join T_BD_STOCK_L C ON C.FSTOCKID=B.FSTOCKID
             left join t_BD_Material wl on wl.FMATERIALID=b1.FMATERIALID
			 left join t_BD_MaterialBase c1 ON c1.FMATERIALID=b1.FMATERIALID
             where a.FMOBILLNO='{SCDD}' and a.FMOENTRYSEQ='{SEQ}' and  FNUMERATOR!=0 and substring(wl.fnumber,1,6)!='260.07'";
            var cks = DBUtils.ExecuteDynamicObject(Context, cksql);
            if (cks.Count > 0)
            {
                ck = cks[0]["FNAME"].ToString();
            }
        }
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            this.View.Model.SetValue("F_RLSL", RLSL);
            this.View.UpdateView("F_RLSL");
            this.View.Model.SetValue("FText_OptPlanNumberScan", "");
            this.View.GetControl("FText_OptPlanNumberScan").SetFocus();
            this.View.GetControl("FText_OptPlanNumberScan").SetCustomPropertyValue("showKeyboard", true);
            this.View.UpdateView("FText_OptPlanNumberScan");
            this.View.GetControl("FMobileListViewEntity").SetCustomPropertyValue("listEditable", true);
            autodate();
        }
        public void autodate()
        {
            string ylqdsql = $@"/*dialect*/select FMATERIALTYPE,FERPCLSID,b.FMATERIALID,
                         PGMX.FMoNumber,PGMX.FProcess,PGMX.OptPlanNo,PGMX.FMaterialName,
                         PGMX.F_RUJP_LOT,PGMX.F_260_CSTM,PGMX.FBARCODE,F_260_SYBDSL ,PGMX.FBASEQTY 
                         from T_PRD_PPBOM a
                         left join T_PRD_PPBOMENTRY b on a.fid=b.fid
                         left JOIN t_BD_Material c on a.FMATERIALID=c.FMATERIALID
                         left join T_BD_MATERIAL d on  b.FMATERIALID=d.FMATERIALID
                         left join t_BD_MaterialBase d1 ON b.FMATERIALID=d1.FMATERIALID
                         left JOIN
                         (   select 
                             concat(FMoBillNo,'-',FMOSEQ) as FMoNumber,t3.FName as FProcess,t.FMATERIALID,
                             F_260_CSTM,concat(FOptPlanNo,'-',FSEQNUMBER,'-',FOperNumber) as OptPlanNo,
                             t2.FNAME as FMaterialName,t1.F_RUJP_LOT,t1.FBARCODE,F_260_SYBDSL ,jskc.FBASEQTY 
                             from T_SFC_DISPATCHDETAIL t 
                             left join T_SFC_DISPATCHDETAILENTRY t1 on t.FID=t1.FID 
                             left join T_BD_MATERIAL_L t2 on t.FMATERIALID = t2.FMATERIALID and t2.FLOCALEID = 2052  
                             left join T_ENG_PROCESS_L t3 on t.FPROCESSID=t3.FID and t3.FLOCALEID = 2052
							 left join 
						     (  select A.FMATERIALID, B.FNUMBER,sum(A.FBASEQTY)FBASEQTY FROM T_STK_INVENTORY A
                                left JOIN T_BD_LOTMASTER B ON B.FLOTID = A.FLOT
                                left join T_BD_STOCK_L c on c.FSTOCKID=a.FSTOCKID 
                                WHERE B.FNUMBER!= ''and A.FSTOCKORGID = 100026 and A.FBASEQTY > 0  and c.fname='{ck}'
                                and (A.FStockId=22315406 AND A.FSTOCKSTATUSID IN (27910195,10000) OR  A.FStockId!=22315406 and c.fname not like '%WMS%' AND A.FSTOCKSTATUSID=10000 or c.fname like '%WMS%' AND A.FSTOCKSTATUSID IN (27910195))
                               and B.FNUMBER is not null
							    group by A.FMATERIALID, B.FNUMBER 
							 )jskc on jskc.FMATERIALID=t.FMATERIALID and t1.F_RUJP_LOT=jskc.FNUMBER
                                WHERE F_260_CSTM!=''and F_260_SYBDSL!=0  and jskc.FBASEQTY is not null
                           ) PGMX ON PGMX.FMATERIALID=b.FMATERIALID						  
                         where c.FNUMBER='{WLDM}' and a.FMOBILLNO='{SCDD}' and a.FMOENTRYSEQ='{SEQ}'and FNUMERATOR!=0
                           and FMATERIALTYPE!=3 and FERPCLSID not in(1,3) and PGMX.F_260_CSTM is not null
                           and substring(d.fnumber,1,6)!='260.07'
                        order by b.FMATERIALID,PGMX.F_RUJP_LOT ";
            //校验剩余数量是否为零
            var rs = DBUtils.ExecuteDynamicObject(Context, ylqdsql);
            var PPBomInfos = (from p in rs.ToList<DynamicObject>() select new { materialid = Convert.ToInt64(p["FMATERIALID"]) }).Distinct().ToList();
            int i = 0;
            foreach (var PPBomInfo in PPBomInfos)
            {
                decimal rlsl = Convert.ToDecimal(RLSL);
                var PPBomInfotmp = (from p in rs.ToList<DynamicObject>() where Convert.ToInt64(p["FMATERIALID"]) == PPBomInfo.materialid orderby p["F_RUJP_LOT"] select p);
                foreach (var PPBom in PPBomInfotmp)
                {
                    if (rlsl > 0)
                    {
                        this.View.Model.CreateNewEntryRow("FMobileListViewEntity");
                        this.View.Model.SetValue("FSeq", i + 1, i);
                        this.View.Model.SetValue("FMONumber", PPBom["FMoNumber"].ToString(), i);
                        this.View.Model.SetValue("FOperPlanNo", PPBom["OptPlanNo"].ToString(), i);
                        if (PPBom["FProcess"] != null)
                        {
                            this.View.Model.SetValue("FProcessId", PPBom["FProcess"].ToString(), i);
                        }
                        this.View.Model.SetValue("FPgBarCode", PPBom["FBARCODE"].ToString(), i);
                        this.View.Model.SetValue("FProductId", PPBom["FMaterialName"].ToString(), i);
                        this.View.Model.SetValue("FLot", PPBom["F_RUJP_LOT"].ToString(), i);
                        this.View.Model.SetValue("FQty", PPBom["F_260_SYBDSL"].ToString(), i);
                        this.View.Model.SetValue("FBASEQTY", PPBom["FBASEQTY"].ToString(), i);
                        this.View.Model.SetValue("F_CSTM", PPBom["F_260_CSTM"].ToString(), i);
                        rlsl -= Convert.ToDecimal(PPBom["FBASEQTY"].ToString());
                        i++;
                    }
                    else
                    {
                        break;
                    }
                }
                
            }
            this.View.UpdateView("FMobileListViewEntity");
            JSSL202304();
        }
        public override void ButtonClick(ButtonClickEventArgs e)
        {
            string key;
            switch (key = e.Key.ToUpper())
            {
                case "FBUTTON_OPTPLANNUMBERSCAN":

                    string scanText = this.View.Model.GetValue("FText_OptPlanNumberScan").ToString();
                    /**
                    if (!string.IsNullOrEmpty(scanText) && !string.IsNullOrWhiteSpace(scanText))
                    {
                        if (scanText.Contains("PGMX"))
                        {
                            //校验剩余数量是否为零
                            string syslsql = $@"SELECT top 1 F_260_SYBDSL from T_SFC_DISPATCHDETAILENTRY a
                             inner join T_SFC_DISPATCHDETAIL b on a.FID=b.FID
                             inner join
                             (select A.FMATERIALID, B.FNUMBER FROM T_STK_INVENTORY A
                             INNER JOIN T_BD_LOTMASTER B ON B.FLOTID = A.FLOT
                             inner join T_BD_STOCK_L c on c.FSTOCKID=a.FSTOCKID 
                             WHERE B.FNUMBER!= ''and A.FSTOCKORGID = 100026 and A.FBASEQTY > 0  and c.fname='{ck}'
                             and A.FSTOCKSTATUSID in( 27910195 , 10000 )
                             )jskc ON  jskc.FMATERIALID = b.FMATERIALID and a.F_RUJP_LOT = jskc.FNUMBER
                             WHERE F_260_CSTM!= ''and F_260_CSTM like '%{scanText}%'
                             order by FDISPATCHTIME desc";
                            var sysls = DBUtils.ExecuteDynamicObject(Context, syslsql);
                            if (sysls.Count > 0)
                            {
                                if (Convert.ToDecimal(sysls[0]["F_260_SYBDSL"]) == 0)
                                {
                                    throw new KDBusinessException("", "该条码剩余绑定数量为零或物料在此仓库没有库存！！！");
                                }
                            }
                            FillAllData(scanText);
                        }
                        else
                        {//扫物料条码
                         //校验剩余数量是否为零
                            string[] text = scanText.Split('-');
                            if (text.Length > 1)
                            {
                                string syslsql = $@"SELECT  F_260_CSTM,F_260_SYBDSL 
                            from T_SFC_DISPATCHDETAILENTRY  a 
                            inner join T_SFC_DISPATCHDETAIL b on a.FID=b.FID
                            inner join
                             (select A.FMATERIALID, B.FNUMBER FROM T_STK_INVENTORY A
                             INNER JOIN T_BD_LOTMASTER B ON B.FLOTID = A.FLOT
                             inner join T_BD_STOCK_L c on c.FSTOCKID=a.FSTOCKID
                             WHERE B.FNUMBER!= ''and A.FSTOCKORGID = 100026 and A.FBASEQTY > 0  and c.fname='{ck}'
                             and A.FSTOCKSTATUSID in( 27910195,10000)
                             )jskc ON  jskc.FMATERIALID = b.FMATERIALID and a.F_RUJP_LOT = jskc.FNUMBER
                            left join T_BD_MATERIAL c on c.FMATERIALID=b.FMATERIALID
                            WHERE c.FNUMBER='{text[0]}' and F_RUJP_LOT='{text[1]}'";
                                var sysls = DBUtils.ExecuteDynamicObject(Context, syslsql);
                                if (sysls.Count > 0)
                                {
                                    if (Convert.ToDecimal(sysls[0]["F_260_SYBDSL"]) == 0)
                                    {
                                        throw new KDBusinessException("", "该条码剩余绑定数量为零！！！");
                                    }
                                    string[] csm = sysls[0]["F_260_CSTM"].ToString().Split(',');
                                    if (csm.Length > 0)
                                    {
                                        FillAllData(csm[0]);
                                    }
                                }
                                else
                                {
                                    throw new KDBusinessException("", "该条码物料在此仓库没有库存！！！");
                                }
                            }
                        }
                    }
                    **/
                    updateEntry(scanText);
                    this.View.Model.SetValue("FText_OptPlanNumberScan", "");
                    this.View.UpdateView("FText_OptPlanNumberScan");
                    this.View.GetControl("FText_OptPlanNumberScan").SetFocus();
                    this.View.GetControl("FText_OptPlanNumberScan").SetCustomPropertyValue("showKeyboard", true);
                    return;
                case "FBUTTON_RETURN":
                    JsonCacheUtils.DeleteCache(base.Context, this.cacheModel4Save.DeviceCode, this.HasCached);
                    base.View.Close();
                    return;

                case "FBUTTON_LOGOUT":
                    JsonCacheUtils.DeleteCache(base.Context, this.cacheModel4Save.DeviceCode, this.HasCached);
                    LoginUtils.LogOut(base.Context, base.View);
                    base.View.Logoff("indexforpad.aspx");
                    return;
                //上一页
                case "FBUTTON_PREVIOUS":
                    //   this.TurnPaga(false);
                    return;
                //下一页
                case "FBUTTON_NEXT":
                    //    this.TurnPaga(true);
                    return;
                case "F_FHSJ":
                    //FHSJ();
                    FHSJ202304();
                    return;
                case "F_BUTTON_SC":
                    SC();
                    decimal sl = JSSL();
                    this.Model.SetValue("F_SM_QTY", sl);
                    this.Model.SetValue("F_QTY", sl);
                    this.View.UpdateView("F_SM_QTY");
                    this.View.UpdateView("F_QTY");
                    this.View.UpdateView("FMobileListViewEntity");
                    return;
            }
        }
        public void updateEntry(string ScanText)
        {
            if (ScanText != "")
            {
                if (ScanText.Contains("PGMX"))
                {
                    Y(ScanText);
                }
                else
                {
                    string[] text = ScanText.Split('-');
                    if (text.Length > 1)
                    {
                        string kcmsql = $@"SELECT top 1 F_260_CSTM 
                            from T_SFC_DISPATCHDETAILENTRY  a 
                            inner join T_SFC_DISPATCHDETAIL b on a.FID=b.FID
                            left join T_BD_MATERIAL c on c.FMATERIALID=b.FMATERIALID
                            WHERE c.FNUMBER='{text[0]}' and F_RUJP_LOT='{text[1]}'";
                        var kcm = DBUtils.ExecuteDynamicObject(Context, kcmsql);
                        if (kcm.Count > 0)
                        {
                            Y(kcm[0]["F_260_CSTM"].ToString());
                        }
                        else
                        {
                            throw new KDBusinessException("", "该条码派工明细不存在！！！");
                        }

                    }
                }
            }
        }
        public void Y(String CSM)
        {
            DynamicObjectCollection dates = this.Model.DataObject["MobileListViewEntity"] as DynamicObjectCollection;
            int j = 0;
            for (int i = 0; i < dates.Count; i++)
            {
                if (dates[i]["F_CSTM"].ToString().Contains(CSM))
                {
                    j++;
                    this.View.Model.SetValue("FIsScan", "Y", i);
                    this.View.UpdateView("FMobileListViewEntity");
                }
            }
            if (j == 0)
            {
                throw new KDBusinessException("", "该条码匹配不成功！！！");
            }
        }
        public void SC()
        {
            int rowcount = this.View.Model.GetEntryRowCount("FMobileListViewEntity");
            if (rowcount > 0)
            {
                for (int row = 0; row < rowcount; row++)
                {
                    if (Convert.ToBoolean(this.View.Model.GetValue("FSelect", row)))
                    {
                        this.Model.DeleteEntryRow("FMobileListViewEntity", row);
                        if (this.View.Model.GetEntryRowCount("FMobileListViewEntity") != 0)
                        {
                            SC();
                        }
                        break;
                    }
                }
            }
            else
            {
                throw new KDBusinessException("", "请选择需要删除的行！！！");
            }
        }
        private void FillAllData(string ScanText)
        {
            if (!string.IsNullOrEmpty(ScanText) && !string.IsNullOrWhiteSpace(ScanText))
            {
                var jys = this.Model.DataObject["MobileListViewEntity"] as DynamicObjectCollection;
                foreach (var jy in jys)
                {
                    if (jy["F_CSTM"].ToString().Contains(ScanText))
                    {
                        throw new KDBusinessException("", "重复扫码或者已扫合批内的条码！！！");
                    }
                }
                string ylqdsql = $@"/*dialect*/select FMATERIALTYPE,FERPCLSID,
                         PGMX.FMoNumber,PGMX.FProcess,PGMX.OptPlanNo,PGMX.FMaterialName,
                        PGMX.F_RUJP_LOT,PGMX.F_260_CSTM,PGMX.FBARCODE,F_260_SYBDSL  
                         from T_PRD_PPBOM a
                         inner join T_PRD_PPBOMENTRY b on a.fid=b.fid
                         INNER JOIN t_BD_Material c on a.FMATERIALID=c.FMATERIALID
                         inner join T_BD_MATERIAL d on  b.FMATERIALID=d.FMATERIALID
                         inner join t_BD_MaterialBase d1 ON b.FMATERIALID=d1.FMATERIALID
                         INNER JOIN
                         (select 
                             concat(FMoBillNo,'-',FMOSEQ) as FMoNumber,t3.FName as FProcess,t.FMATERIALID,
                             F_260_CSTM,concat(FOptPlanNo,'-',FSEQNUMBER,'-',FOperNumber) as OptPlanNo,
                             t2.FNAME as FMaterialName,t1.F_RUJP_LOT,t1.FBARCODE,F_260_SYBDSL  
                           from T_SFC_DISPATCHDETAIL t 
                           inner join T_SFC_DISPATCHDETAILENTRY t1 on t.FID=t1.FID 
                           left join T_BD_MATERIAL_L t2 on t.FMATERIALID = t2.FMATERIALID and t2.FLOCALEID = 2052  
                           left join T_ENG_PROCESS_L t3 on t.FPROCESSID=t3.FID and t3.FLOCALEID = 2052
                            WHERE F_260_CSTM!=''and F_260_CSTM like '%{ScanText}%' and F_260_SYBDSL!=0
                           ) PGMX ON PGMX.FMATERIALID=b.FMATERIALID
                         where c.FNUMBER='{WLDM}' and a.FMOBILLNO='{SCDD}' and a.FMOENTRYSEQ='{SEQ}'and FNUMERATOR!=0";
                //校验剩余数量是否为零
                var rs = DBUtils.ExecuteDynamicObject(Context, ylqdsql);
                if (rs.Count > 0)
                {
                    if (rs[0]["FMATERIALTYPE"].ToString() == "3" && (rs[0]["FERPCLSID"].ToString() == "1" || rs[0]["FERPCLSID"].ToString() == "3"))
                    {
                        throw new KDBusinessException("", "替代件外购不需要绑定，走补料！！");
                    }
                    DynamicObjectCollection dates = this.Model.DataObject["MobileListViewEntity"] as DynamicObjectCollection;
                    var material = this.View.Model.GetValue("FMONumber", 0);
                    if (material == null)
                    {
                        this.View.Model.DeleteEntryData("FMobileListViewEntity");
                    }
                    int j = 0;
                    for (int i = 0; i < rs.Count; i++)
                    {
                        if (Convert.ToDecimal(RLSL) < Convert.ToDecimal(this.Model.GetValue("F_SM_QTY")))
                        {
                            throw new KDBusinessException("", "绑定数量不能大于可认领数量！！");
                        }
                        this.View.Model.InsertEntryRow("FMobileListViewEntity", 0);
                        int rowCount = this.View.Model.GetEntryRowCount("FMobileListViewEntity");
                        this.View.Model.SetValue("FSeq", rowCount + 1, i);
                        this.View.Model.SetValue("FMONumber", rs[i]["FMoNumber"].ToString(), 0);
                        this.View.Model.SetValue("FOperPlanNo", rs[i]["OptPlanNo"].ToString(), 0);
                        if (rs[i]["FProcess"] != null)
                        {
                            this.View.Model.SetValue("FProcessId", rs[i]["FProcess"].ToString(), 0);
                        }
                        this.View.Model.SetValue("FPgBarCode", rs[i]["FBARCODE"].ToString(), 0);
                        this.View.Model.SetValue("FProductId", rs[i]["FMaterialName"].ToString(), 0);
                        this.View.Model.SetValue("FLot", rs[i]["F_RUJP_LOT"].ToString(), 0);
                        this.View.Model.SetValue("FQty", rs[i]["F_260_SYBDSL"].ToString(), 0);
                        this.View.Model.SetValue("F_CSTM", rs[i]["F_260_CSTM"].ToString(), 0);
                        this.View.UpdateView("FMobileListViewEntity");
                        decimal sl = JSSL();
                        this.View.Model.SetValue("F_SM_QTY", sl);
                        this.View.UpdateView("F_SM_QTY");
                        this.View.Model.SetValue("F_QTY", sl);
                        this.View.UpdateView("F_QTY");
                        this.View.Model.SetValue("F_ZXSL", sl);
                        this.View.UpdateView("F_ZXSL");
                    }
                }
                else
                {
                    this.View.Model.SetValue("FText_OptPlanNumberScan", "");
                    throw new KDBusinessException("", "条码不是物料" + WLDM + "的子项物料,或者已被绑定，请重新扫码！！！");
                }
            }
        }
        public override void BeforeUpdateValue(BeforeUpdateValueEventArgs e)
        {
            base.BeforeUpdateValue(e);
            this.ScanCodeChanged(e);

        }
        private void ScanCodeChanged(BeforeUpdateValueEventArgs e)
        {
            try
            {
                string key;
                if ((key = e.Key) != null)
                {
                    if (key == "FText_OptPlanNumberScan")
                    {
                        updateEntry(e.Value.ToString());
                        e.Value = string.Empty;
                        /**
                        if (!string.IsNullOrEmpty(e.Value.ToString()) && !string.IsNullOrWhiteSpace(e.Value.ToString()))
                        {
                            if (e.Value.ToString().Contains("PGMX"))
                            {
                                //校验剩余数量是否为零
                                string syslsql = $@"SELECT top 1 F_260_SYBDSL from T_SFC_DISPATCHDETAILENTRY a
                             inner join T_SFC_DISPATCHDETAIL b on a.FID=b.FID
                             inner join
                             (select A.FMATERIALID, B.FNUMBER FROM T_STK_INVENTORY A
                             INNER JOIN T_BD_LOTMASTER B ON B.FLOTID = A.FLOT
                             inner join T_BD_STOCK_L c on c.FSTOCKID=a.FSTOCKID
                             WHERE B.FNUMBER!= ''and A.FSTOCKORGID = 100026 and A.FBASEQTY > 0  and c.fname='{ck}'
                             and A.FSTOCKSTATUSID in(27910195,10000)
                             )jskc ON  jskc.FMATERIALID = b.FMATERIALID and a.F_RUJP_LOT = jskc.FNUMBER
                             WHERE F_260_CSTM!= ''and F_260_CSTM like '%{e.Value.ToString()}%'
                             and (FMOBILLNO like '%MO%' OR FMOBILLNO like '%XNY%')
                             order by FDISPATCHTIME desc";
                                var sysls = DBUtils.ExecuteDynamicObject(Context, syslsql);
                                if (sysls.Count > 0)
                                {
                                    if (Convert.ToDecimal(sysls[0]["F_260_SYBDSL"]) == 0)
                                    {
                                        throw new KDBusinessException("", "该条码剩余绑定数量为零！！！");
                                    }
                                    FillAllData(e.Value.ToString());
                                }
                                else
                                {
                                    throw new KDBusinessException("", "该条码物料在此仓库没有库存！！！");
                                }

                                e.Value = string.Empty;
                            }
                            else
                            {
                                string[] text = e.Value.ToString().Split('-');
                                if (text.Length > 1)
                                {
                                    string syslsql = $@"SELECT  F_260_CSTM,F_260_SYBDSL 
                                       from T_SFC_DISPATCHDETAILENTRY  a 
                                       inner join T_SFC_DISPATCHDETAIL b on a.FID=b.FID
                                       inner join
                                        (select A.FMATERIALID, B.FNUMBER FROM T_STK_INVENTORY A
                                        INNER JOIN T_BD_LOTMASTER B ON B.FLOTID = A.FLOT
                                        inner join T_BD_STOCK_L c on c.FSTOCKID=a.FSTOCKID
                                        WHERE B.FNUMBER!= ''and A.FSTOCKORGID = 100026 and A.FBASEQTY > 0  and c.fname='{ck}'
                                        and A.FSTOCKSTATUSID in( 27910195,10000)
                                        )jskc ON  jskc.FMATERIALID = b.FMATERIALID and a.F_RUJP_LOT = jskc.FNUMBER
                                       left join T_BD_MATERIAL c on c.FMATERIALID=b.FMATERIALID
                                     WHERE c.FNUMBER='{text[0]}' and F_RUJP_LOT='{text[1]}'
                                     and (FMOBILLNO like '%MO%' OR FMOBILLNO like '%XNY%')";
                                    var sysls = DBUtils.ExecuteDynamicObject(Context, syslsql);
                                    if (sysls.Count > 0)
                                    {
                                        if (Convert.ToDecimal(sysls[0]["F_260_SYBDSL"]) == 0)
                                        {
                                            throw new KDBusinessException("", "该物料条码剩余绑定数量为零或物料在此仓库没有库存！！！");
                                        }
                                        string[] csm = sysls[0]["F_260_CSTM"].ToString().Split(',');
                                        if (csm.Length > 0)
                                        {
                                            FillAllData(csm[0]);
                                        }
                                    }
                                }
                                e.Value = string.Empty;
                            }

                        }
                    }
                        **/
                    }
                }
            }
            catch (Exception ex)
            {
                e.Value = string.Empty;
                this.View.ShowStatusBarInfo(ex.Message);
            }

            this.View.GetControl(e.Key).SetFocus();
        }
        //返回数据
        public void FHSJ()
        {
            List<fhz> pickinfoList = new List<fhz>();
            List<object> li = new List<object>();
            decimal krlsl = Convert.ToDecimal(this.Model.GetValue("F_RLSL"));
            decimal smsl = Convert.ToDecimal(this.Model.GetValue("F_SM_QTY"));
            decimal rlsl = Convert.ToDecimal(this.Model.GetValue("F_QTY"));
            decimal zxsl = Convert.ToDecimal(this.Model.GetValue("F_ZXSL"));
            string tm = "";
            string ylqdsql = $@"/*dialect*/select 
                        d.FMATERIALID,d.FNAME,b1.FSTOCKID,FNUMERATOR/FDENOMINATOR bl
                         from T_PRD_PPBOM a
                         inner join T_PRD_PPBOMENTRY b on a.fid=b.fid
                         inner join T_PRD_PPBOMENTRY_C b1 on b1.FENTRYID=b.FENTRYID
                         inner join T_BD_MATERIAL c on  a.FMATERIALID=c.FMATERIALID
                         inner join T_BD_MATERIAL_L d on  b.FMATERIALID=d.FMATERIALID
                         inner join t_BD_MaterialBase e on b.FMATERIALID=e.FMATERIALID
                         where c.FNUMBER='{WLDM}' and a.FMOBILLNO='{SCDD}' and a.FMOENTRYSEQ='{SEQ}' 
                         and FERPCLSID not in (1,3) and a.FPRDORGID=100026 and FMATERIALTYPE=1 and FNUMERATOR!=0";
            var ylqds = DBUtils.ExecuteDynamicObject(Context, ylqdsql);
            DynamicObjectCollection dates = this.Model.DataObject["MobileListViewEntity"] as DynamicObjectCollection;
            foreach (var ylqd in ylqds)
            {
                decimal pgsl = 0;
                string pc = "";//批次
                foreach (var date in dates)
                {
                    if (ylqd["FNAME"].ToString() == date["FProductId"].ToString())
                    {
                        fhz pInfo = new fhz();
                        pInfo.FMaterialNumber = date["FProductId"].ToString();
                        pInfo.Flot = date["FLot"].ToString();
                        pInfo.Fsl = Convert.ToDecimal(date["FQty"]);
                        pInfo.Fck = ck;
                        pInfo.Fpgtm = date["FPgBarCode"].ToString();
                        pickinfoList.Add(pInfo);
                        pgsl = pgsl + Convert.ToDecimal(date["FQty"]);
                        tm = tm + date["F_CSTM"].ToString() + ",";
                        pc += "'" + date["FLot"].ToString() + "'" + ",";
                    }
                }
                if (pc != "")
                {
                    string[] strpc = pc.Trim(',').Split(',');
                    string jskcsql = $@"/*dialect*/select * from 
                                      (select TOP {strpc.Length} A.FMATERIALID,B.FNUMBER,A.FSTOCKID from T_STK_INVENTORY A
                                       INNER JOIN T_BD_LOTMASTER B ON B.FLOTID =A.FLOT 
                                       INNER join T_BD_STOCK_L c on c.FSTOCKID=a.FSTOCKID
                                       INNER JOIN 
                                       (select distinct A.FMATERIALID,B.F_RUJP_LOT from T_SFC_DISPATCHDETAIL A
                                       LEFT JOIN T_SFC_DISPATCHDETAILENTRY  B ON A.FID=B.FID
                                       WHERE F_260_CSTM!='' AND F_260_SYBDSL>0 and A.FMATERIALID='{ylqd["FMATERIALID"]}')PGMX  ON PGMX.FMATERIALID=A.FMATERIALID AND PGMX.F_RUJP_LOT=B.FNUMBER
                                       where A.FMATERIALID='{ylqd["FMATERIALID"]}' and A.FSTOCKID='{ylqd["FSTOCKID"]}' 
                                       AND A.FSTOCKORGID=100026 and A.FBASEQTY>0 
                                      and (A.FStockId=22315406 AND A.FSTOCKSTATUSID IN (27910195,10000) OR  A.FStockId!=22315406 and c.fname not like '%WMS%' AND A.FSTOCKSTATUSID=10000 or c.fname like '%WMS%' AND A.FSTOCKSTATUSID IN (27910195))
                                       ORDER BY B.FNUMBER)jskc
                                      WHERE jskc.FNUMBER IN({pc.Trim(',')}) 
                                      ORDER BY FNUMBER";
                    var jskcs = DBUtils.ExecuteDynamicObject(Context, jskcsql);
                    if (jskcs.Count != strpc.Length)
                    {
                        throw new KDBusinessException("", "绑定条码需按批号先进先出顺序绑定！！");
                    }
                    if (smsl - rlsl > 0)
                    {
                        foreach (var entry in dates)
                        {
                            if (ylqd["FNAME"].ToString() == entry["FProductId"].ToString() &&
                                entry["FLot"].ToString() == jskcs[jskcs.Count - 1]["FNUMBER"].ToString() &&
                                (smsl - rlsl) >= Convert.ToDecimal(entry["FQty"]))
                            {
                                throw new KDBusinessException("", "请删除多余批次！！");
                            }
                        }

                        foreach (var entry in pickinfoList)
                        {
                            if (ylqd["FNAME"].ToString() == entry.FMaterialNumber.ToString() &&
                                entry.Flot.ToString() == jskcs[jskcs.Count - 1]["FNUMBER"].ToString())
                            {
                                entry.Fsl = smsl - rlsl;
                            }
                        }
                    }
                }
            }
            string sfcf = "否";

            if (smsl < rlsl || rlsl == 0 || zxsl == 0)
            {
                throw new KDBusinessException("", "认领数量不能大于扫码数量且认领数量不能等于零！！");
            }

            else if (smsl == rlsl && zxsl >= rlsl)
            {
                sfcf = "否";
            }
            else
            {
                sfcf = "是";
            }
            if (krlsl < rlsl)
            {
                rlsl = krlsl;
            }
            li.Add(pickinfoList);
            if (sfcf == "是")
            {
                this.View.ShowMessage("1.认领数量小于扫码数量会分批\n2.最小包装数小于认领数会分批\n是否确定要分批？",
                                 MessageBoxOptions.YesNo,
                                 new Action<MessageBoxResult>((result) =>
                                 {
                                     if (result == MessageBoxResult.Yes)
                                     {
                                         li.Add(tm.Trim(','));
                                         li.Add(Convert.ToString(rlsl));
                                         li.Add(sfcf);
                                         li.Add(Convert.ToString(zxsl));
                                         this.View.ReturnToParentWindow(li);
                                         base.View.Close();
                                     }
                                     else if (result == MessageBoxResult.No)
                                     {

                                     }
                                 }));
            }
            else
            {
                li.Add(tm.Trim(','));
                li.Add(Convert.ToString(rlsl));
                li.Add(sfcf);
                li.Add(Convert.ToString(zxsl));
                this.View.ReturnToParentWindow(li);
                base.View.Close();
            }

        }
        public void FHSJ202304()
        {
            List<fhz> pickinfoList = new List<fhz>();
            List<object> li = new List<object>();
            decimal krlsl = Convert.ToDecimal(this.Model.GetValue("F_RLSL"));
            decimal smsl = Convert.ToDecimal(this.Model.GetValue("F_SM_QTY"));
            decimal rlsl = Convert.ToDecimal(this.Model.GetValue("F_QTY"));
            decimal zxsl = Convert.ToDecimal(this.Model.GetValue("F_ZXSL"));
            string tm = "";
            DynamicObjectCollection dates = this.Model.DataObject["MobileListViewEntity"] as DynamicObjectCollection;
            string wl = dates[0]["FProductId"].ToString();
            for(int i=0;i<dates.Count;i++)
            {   
                if(wl != dates[i]["FProductId"].ToString())
                {
                    wl= dates[i]["FProductId"].ToString();
                }
                if (dates[i]["FIsScan"]!=null && dates[i]["FIsScan"].ToString().Contains("Y"))
                {
                    fhz pInfo = new fhz();
                    pInfo.FMaterialNumber = dates[i]["FProductId"].ToString();
                    pInfo.Flot = dates[i]["FLot"].ToString();
                    pInfo.Fcstm= dates[i]["F_CSTM"].ToString();
                    if (i ==(dates.Count - 1)||wl != dates[i + 1]["FProductId"].ToString())
                    {
                        if (krlsl < rlsl)
                        {
                            pInfo.Fsl = Convert.ToDecimal(dates[i]["FBASEQTY"])-(rlsl- krlsl);
                        }
                        else
                        {
                            pInfo.Fsl = Convert.ToDecimal(dates[i]["FBASEQTY"]);
                        }
                    }
                    else
                    {
                        pInfo.Fsl = Convert.ToDecimal(dates[i]["FBASEQTY"]);
                    }
                    
                    pInfo.Fck = ck;
                    pInfo.Fpgtm = dates[i]["FPgBarCode"].ToString();
                    pickinfoList.Add(pInfo);
                    tm = tm + dates[i]["F_CSTM"].ToString() + ",";
                }
                else
                {
                    throw new KDBusinessException("", "存在未扫描记录，请扫码！！");
                }
            }
            string tshp = "否";
            //tshp特殊合批
            for (int i = 0; i < dates.Count; i++)
            {
                if (dates[i]["FIsScan"] != null && dates[i]["FIsScan"].ToString().Contains("Y"))
                {                    
                    if ((Convert.ToDecimal(dates[i]["FBASEQTY"])== Convert.ToDecimal(dates[i]["FQty"])
                      && zxsl% Convert.ToDecimal(dates[i]["FBASEQTY"])==0)||
                      krlsl== rlsl  && rlsl < zxsl)
                    {
                        tshp = "是";
                    }
                }
            }
            if (krlsl < rlsl)
            {
                tshp = "否";
            }
            string sfcf = "否";
            if(tshp == "否")
            {
                if (smsl == rlsl && zxsl >= rlsl && rlsl == krlsl)
                {
                    sfcf = "否";
                }
                else
                {
                    sfcf = "是";
                }
            }         
            if (krlsl < rlsl)
            {
                rlsl = krlsl;
            }
            li.Add(pickinfoList);
            if (sfcf == "是" && tshp == "否")
            {
                this.View.ShowMessage("1.认领数量小于扫码数量会分批\n" +
                    "2.最小包装数除以条码库存数不为整数会分批\n" +
                    "是否确定要分批？",
                                 MessageBoxOptions.YesNo,
                                 new Action<MessageBoxResult>((result) =>
                                 {
                                     if (result == MessageBoxResult.Yes)
                                     {
                                         li.Add(tm.Trim(','));
                                         li.Add(Convert.ToString(rlsl));
                                         li.Add(sfcf);
                                         li.Add(Convert.ToString(zxsl));
                                         li.Add(tshp);
                                         this.View.ReturnToParentWindow(li);
                                         base.View.Close();
                                     }
                                     else if (result == MessageBoxResult.No)
                                     {

                                     }
                                 }));
            }
            else
            {
                li.Add(tm.Trim(','));
                li.Add(Convert.ToString(rlsl));
                li.Add(sfcf);
                li.Add(Convert.ToString(zxsl));
                li.Add(tshp);
                this.View.ReturnToParentWindow(li);
                base.View.Close();
            }

        }
        //计算数量
        public void JSSL202304()
        {
            decimal bl = 0;
            decimal kcbl = 0;
            string ylqdsql = $@"/*dialect*/select FROWID,
                        d.FMATERIALID,d.FNAME,b1.FSTOCKID,FNUMERATOR/FDENOMINATOR bl
                         from T_PRD_PPBOM a
                         left join T_PRD_PPBOMENTRY b on a.fid=b.fid
                         left join T_PRD_PPBOMENTRY_C b1 on b1.FENTRYID=b.FENTRYID
                         left join T_BD_MATERIAL c on  a.FMATERIALID=c.FMATERIALID
                         left join T_BD_MATERIAL_L d on  b.FMATERIALID=d.FMATERIALID
                         left join T_BD_MATERIAL d1 on  b.FMATERIALID=d1.FMATERIALID
                         left join t_BD_MaterialBase e on b.FMATERIALID=e.FMATERIALID
                         where c.FNUMBER='{WLDM}' and a.FMOBILLNO='{SCDD}' and a.FMOENTRYSEQ='{SEQ}' 
                          and FERPCLSID not in (1,3) and a.FPRDORGID=100026 and FMATERIALTYPE=1 AND FNUMERATOR!=0
                           and substring(d1.fnumber,1,6)!='260.07'";
            var ylqds = DBUtils.ExecuteDynamicObject(Context, ylqdsql);
            DynamicObjectCollection dates = this.Model.DataObject["MobileListViewEntity"] as DynamicObjectCollection;
            foreach (var ylqd in ylqds)
            {
                decimal pgsl = 0;
                decimal kcsl = 0;
                string pc = "";
                foreach (var date in dates)
                {
                    if (ylqd["FNAME"].ToString() == date["FProductId"].ToString())
                    {
                        pgsl = pgsl + Convert.ToDecimal(date["FQty"]);
                        kcsl=kcsl+ Convert.ToDecimal(date["FBASEQTY"]);
                        pc += "'" + date["FLot"].ToString() + "'" + ",";
                    }
                }
                if (pc != "")
                {
                    string[] strpc = pc.Trim(',').Split(',');
                    string jskcsql = $@"/*dialect*/select distinct FMATERIALID,FNUMBER from 
                                      (select DISTINCT TOP {strpc.Length} A.FMATERIALID,B.FNUMBER,A.FSTOCKID from T_STK_INVENTORY A
                                       left JOIN T_BD_LOTMASTER B ON B.FLOTID =A.FLOT 
                                       left join T_BD_STOCK_L c on c.FSTOCKID=a.FSTOCKID
                                       where A.FMATERIALID='{ylqd["FMATERIALID"]}' and A.FSTOCKID='{ylqd["FSTOCKID"]}' 
                                       AND A.FSTOCKORGID=100026 and A.FBASEQTY>0 and B.FNUMBER is not null 
                                       and (A.FStockId=22315406 AND A.FSTOCKSTATUSID IN (27910195,10000) OR  A.FStockId!=22315406 and c.fname not like '%WMS%' AND A.FSTOCKSTATUSID=10000 or c.fname like '%WMS%' AND A.FSTOCKSTATUSID IN (27910195))
                                       ORDER BY B.FNUMBER)jskc
                                      WHERE jskc.FNUMBER IN({pc.Trim(',')}) 
                                      ORDER BY FNUMBER";
                    var jskcs = DBUtils.ExecuteDynamicObject(Context, jskcsql);
                    if (jskcs.Count != strpc.Length)
                    {
                        throw new KDBusinessException("", "即时库存的物料存在没有派工明细记录！！");
                    }
                }
                if (pgsl == 0 && TDJ(ylqd["FROWID"].ToString()))
                {

                }
                else
                {
                    if (bl == 0)
                    {
                        bl = pgsl / Convert.ToDecimal(ylqd["bl"]);
                        if (bl == 0)
                        {
                            break;
                        }
                    }
                    else
                    {
                        if (bl > (pgsl / Convert.ToDecimal(ylqd["bl"])))
                        {
                            bl = (pgsl / Convert.ToDecimal(ylqd["bl"]));
                        }
                        if (bl == 0)
                        {
                            break;
                        }
                    }
                    if (kcbl == 0)
                    {
                        kcbl = kcsl / Convert.ToDecimal(ylqd["bl"]);
                        if (kcbl == 0)
                        {
                            break;
                        }
                    }
                    else
                    {
                        if (kcbl > (kcsl / Convert.ToDecimal(ylqd["bl"])))
                        {
                            kcbl = (kcsl / Convert.ToDecimal(ylqd["bl"]));
                        }
                        if (kcbl == 0)
                        {
                            break;
                        }
                    }
                }
            }
            this.View.Model.SetValue("F_SM_QTY", bl);
            this.View.UpdateView("F_SM_QTY");
            this.View.Model.SetValue("F_QTY", kcbl);
            this.View.UpdateView("F_QTY");
            this.View.Model.SetValue("F_ZXSL",ZXBZSL);
            this.View.UpdateView("F_ZXSL");
        }
        public decimal JSSL()
        {
            decimal bl = 0;
            string ylqdsql = $@"/*dialect*/select FROWID,
                        d.FMATERIALID,d.FNAME,b1.FSTOCKID,FNUMERATOR/FDENOMINATOR bl
                         from T_PRD_PPBOM a
                         inner join T_PRD_PPBOMENTRY b on a.fid=b.fid
                         inner join T_PRD_PPBOMENTRY_C b1 on b1.FENTRYID=b.FENTRYID
                         inner join T_BD_MATERIAL c on  a.FMATERIALID=c.FMATERIALID
                         inner join T_BD_MATERIAL_L d on  b.FMATERIALID=d.FMATERIALID
                         inner join t_BD_MaterialBase e on b.FMATERIALID=e.FMATERIALID
                         where c.FNUMBER='{WLDM}' and a.FMOBILLNO='{SCDD}' and a.FMOENTRYSEQ='{SEQ}' 
                          and FERPCLSID not in (1,3) and a.FPRDORGID=100026 and FMATERIALTYPE=1 AND FNUMERATOR!=0";
            var ylqds = DBUtils.ExecuteDynamicObject(Context, ylqdsql);
            DynamicObjectCollection dates = this.Model.DataObject["MobileListViewEntity"] as DynamicObjectCollection;
            foreach (var ylqd in ylqds)
            {
                decimal pgsl = 0;
                foreach (var date in dates)
                {
                    if (ylqd["FNAME"].ToString() == date["FProductId"].ToString())
                    {
                        pgsl = pgsl + Convert.ToDecimal(date["FBASEQTY"]);
                    }
                }
                if (pgsl == 0 && TDJ(ylqd["FROWID"].ToString()))
                {

                }
                else
                {
                    if (bl == 0)
                    {
                        bl = pgsl / Convert.ToDecimal(ylqd["bl"]);
                    }
                    else
                    {
                        if (bl > (pgsl / Convert.ToDecimal(ylqd["bl"])))
                        {
                            bl = (pgsl / Convert.ToDecimal(ylqd["bl"]));
                            //throw new KDBusinessException("", "扫描条码的物料数量与BOM不成比例！！！");
                        }
                    }
                }
            }
            return bl;
        }
        //替代件
        public bool TDJ(string FROWID)
        {
            bool sftd;
            string ylqdsql = $@"/*dialect*/select 
                        d.FMATERIALID,d.FNAME,b1.FSTOCKID,FNUMERATOR/FDENOMINATOR bl
                         from T_PRD_PPBOM a
                         left join T_PRD_PPBOMENTRY b on a.fid=b.fid
                         left join T_PRD_PPBOMENTRY_C b1 on b1.FENTRYID=b.FENTRYID
                         left join T_BD_MATERIAL c on  a.FMATERIALID=c.FMATERIALID
                         left join T_BD_MATERIAL_L d on  b.FMATERIALID=d.FMATERIALID
                         left join T_BD_MATERIAL d1 on  b.FMATERIALID=d1.FMATERIALID
                         left join t_BD_MaterialBase e on b.FMATERIALID=e.FMATERIALID
                         where c.FNUMBER='{WLDM}' and a.FMOBILLNO='{SCDD}' and a.FMOENTRYSEQ='{SEQ}' and FPARENTROWID='{FROWID}'
                          and FERPCLSID not in (1,3) and a.FPRDORGID=100026 and FMATERIALTYPE=3
                          and  substring(d1.fnumber,1,6)!='260.07'";
            var ylqds = DBUtils.ExecuteDynamicObject(Context, ylqdsql);
            if (ylqds.Count > 0)
            {
                sftd = true;
            }
            else
            {
                sftd = false;
            }
            return sftd;
        }
        //返回值
        public class fhz
        {
            private string _MaterialNumber;
            private string _lot;
            private decimal _sl;
            private string _ck;
            private string _pgtm;
            private string _cstm;
            /// <summary>
            /// 物料名称
            /// </summary>
            public string FMaterialNumber
            {
                get { return _MaterialNumber; }
                set { _MaterialNumber = value; }
            }
            /// <summary>
            /// 批号
            /// </summary>
            public string Flot
            {
                get { return _lot; }
                set { _lot = value; }
            }
            /// <summary>
            /// 数量
            /// </summary>
            public decimal Fsl
            {
                get { return _sl; }
                set { _sl = value; }
            }
            /// <summary>
            /// 仓库
            /// </summary>
            public string Fck
            {
                get { return _ck; }
                set { _ck = value; }
            }
            /// <summary>
            /// 派工条码
            /// </summary>
            public string Fpgtm
            {
                get { return _pgtm; }
                set { _pgtm = value; }
            }
            /// <summary>
            ///  初始条码
            /// </summary>
            public string Fcstm
            {
                get { return _cstm; }
                set { _cstm = value; }
            }
        }
    }
}
