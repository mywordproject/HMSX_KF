using Kingdee.BOS;
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
            RLSL = e.Paramter.GetCustomParameter("RLSL").ToString();
            WLDM = e.Paramter.GetCustomParameter("WLDM").ToString();
            SCDD = e.Paramter.GetCustomParameter("SCDD").ToString();
            SEQ = e.Paramter.GetCustomParameter("SEQ").ToString();
            string cksql = $@"select C.FNAME,FNUMERATOR 
             from T_PRD_PPBOM a
             inner join T_PRD_PPBOMENTRY_C b  on b.fid=a.fid
			 inner join T_PRD_PPBOMENTRY b1  on b1.fentryid=b.fentryid
             inner join T_BD_STOCK_L C ON C.FSTOCKID=B.FSTOCKID
			 inner join t_BD_MaterialBase c1 ON c1.FMATERIALID=b1.FMATERIALID
             where a.FMOBILLNO='{SCDD}' and a.FMOENTRYSEQ='{SEQ}' and  FNUMERATOR!=0";
            var cks = DBUtils.ExecuteDynamicObject(Context,cksql);
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
        }
        public override void ButtonClick(ButtonClickEventArgs e)
        {
            string key;
            switch (key = e.Key.ToUpper())
            {
                case "FBUTTON_OPTPLANNUMBERSCAN":

                    string scanText = this.View.Model.GetValue("FText_OptPlanNumberScan").ToString();
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
                             and A.FSTOCKSTATUSID =case when A.FSTOCKID = 22315406 then 27910195 else 10000 end
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
                                string syslsql = $@"/*dialect*/SELECT  F_260_CSTM,F_260_SYBDSL 
                            from T_SFC_DISPATCHDETAILENTRY  a 
                            inner join T_SFC_DISPATCHDETAIL b on a.FID=b.FID
                            inner join
                             (select A.FMATERIALID, B.FNUMBER FROM T_STK_INVENTORY A
                             INNER JOIN T_BD_LOTMASTER B ON B.FLOTID = A.FLOT
                             inner join T_BD_STOCK_L c on c.FSTOCKID=a.FSTOCKID
                             WHERE B.FNUMBER!= ''and A.FSTOCKORGID = 100026 and A.FBASEQTY > 0  and c.fname='{ck}'
                             and A.FSTOCKSTATUSID =case when A.FSTOCKID = 22315406 then 27910195 else 10000 end
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
                    FHSJ();
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
                         PGMX.FMoBillNo,PGMX.FMOSEQ,PGMX.FMoNumber,PGMX.FOptPlanNo,PGMX.FProcess,PGMX.FOperNumber,PGMX.FSEQNUMBER, 
                         PGMX.OptPlanNo,PGMX.FMaterialId,PGMX.FMaterialName,PGMX.F_RUJP_LOT,PGMX.FWORKQTY,PGMX.F_260_CSTM,PGMX.FBARCODE,F_260_SYBDSL  
                         from T_PRD_PPBOM a
                         inner join T_PRD_PPBOMENTRY b on a.fid=b.fid
                         inner join T_BD_MATERIAL c on  a.FMATERIALID=c.FMATERIALID
                         inner join T_BD_MATERIAL d on  b.FMATERIALID=d.FMATERIALID
                         inner join t_BD_MaterialBase d1 ON b.FMATERIALID=d1.FMATERIALID
                         INNER JOIN
                         (select FMoBillNo,FMOSEQ,concat(FMoBillNo,'-',FMOSEQ) as FMoNumber,FOptPlanNo,t3.FName as FProcess,FOperNumber,FSEQNUMBER,F_260_CSTM,
                           concat(FOptPlanNo,'-',FSEQNUMBER,'-',FOperNumber) as OptPlanNo,t.FMaterialId,t2.FNAME as FMaterialName,t1.F_RUJP_LOT,t1.FWORKQTY,t1.FEntryId,t1.FBARCODE,F_260_SYBDSL  
                           from T_SFC_DISPATCHDETAIL t 
                           inner join T_SFC_DISPATCHDETAILENTRY t1 on t.FID=t1.FID 
                           left join T_BD_MATERIAL_L t2 on t.FMATERIALID = t2.FMATERIALID and t2.FLOCALEID = 2052  
                           left join T_ENG_PROCESS_L t3 on t.FPROCESSID=t3.FID and t3.FLOCALEID = 2052
                            WHERE F_260_CSTM!=''and F_260_CSTM like '%{ScanText}%' and F_260_SYBDSL!=0
                           ) PGMX ON PGMX.FMATERIALID=b.FMATERIALID
                         where c.FNUMBER='{WLDM}' and a.FMOBILLNO='{SCDD}' and a.FMOENTRYSEQ='{SEQ}'";
                //校验剩余数量是否为零
                var rs = DBUtils.ExecuteDynamicObject(Context, ylqdsql);
                if (rs.Count > 0)
                {
                    if (rs[0]["FMATERIALTYPE"].ToString() == "3" &&( rs[0]["FERPCLSID"].ToString() == "1" || rs[0]["FERPCLSID"].ToString() == "3"))
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
                        foreach (var date in dates)
                        {
                            if (date["FPgBarCode"].ToString() == rs[i]["FBARCODE"].ToString())
                            {
                                j++;
                            }
                        }
                        if (j == 0)
                        {
                            if (Convert.ToDecimal(RLSL) < Convert.ToDecimal(this.Model.GetValue("F_SM_QTY")))
                            {
                                throw new KDBusinessException("", "绑定数量不能大于可认领数量！！");
                            }
                            this.View.Model.InsertEntryRow("FMobileListViewEntity", 0);
                            int rowCount = this.View.Model.GetEntryRowCount("FMobileListViewEntity");
                            int Seq = i + 1;
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
                             and A.FSTOCKSTATUSID =case when A.FSTOCKID = 22315406 then 27910195 else 10000 end
                             )jskc ON  jskc.FMATERIALID = b.FMATERIALID and a.F_RUJP_LOT = jskc.FNUMBER
                             WHERE F_260_CSTM!= ''and F_260_CSTM like '%{e.Value.ToString()}%'
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
                                    string syslsql = $@"/*dialect*/SELECT  F_260_CSTM,F_260_SYBDSL 
                                       from T_SFC_DISPATCHDETAILENTRY  a 
                                       inner join T_SFC_DISPATCHDETAIL b on a.FID=b.FID
                                       inner join
                                        (select A.FMATERIALID, B.FNUMBER FROM T_STK_INVENTORY A
                                        INNER JOIN T_BD_LOTMASTER B ON B.FLOTID = A.FLOT
                                        inner join T_BD_STOCK_L c on c.FSTOCKID=a.FSTOCKID
                                        WHERE B.FNUMBER!= ''and A.FSTOCKORGID = 100026 and A.FBASEQTY > 0  and c.fname='{ck}'
                                        and A.FSTOCKSTATUSID =case when A.FSTOCKID = 22315406 then 27910195 else 10000 end
                                        )jskc ON  jskc.FMATERIALID = b.FMATERIALID and a.F_RUJP_LOT = jskc.FNUMBER
                                       left join T_BD_MATERIAL c on c.FMATERIALID=b.FMATERIALID
                                     WHERE c.FNUMBER='{text[0]}' and F_RUJP_LOT='{text[1]}'";
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
                         and FERPCLSID not in (1,3) and a.FPRDORGID=100026 and FMATERIALTYPE=1";
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
                        pInfo.Fpgtm= date["FPgBarCode"].ToString();
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
                                       INNER JOIN 
                                       (select A.FMATERIALID,B.F_RUJP_LOT from T_SFC_DISPATCHDETAIL A
                                       LEFT JOIN T_SFC_DISPATCHDETAILENTRY  B ON A.FID=B.FID
                                       WHERE F_260_CSTM!='' AND F_260_SYBDSL>0 and A.FMATERIALID='{ylqd["FMATERIALID"]}')PGMX  ON PGMX.FMATERIALID=A.FMATERIALID AND PGMX.F_RUJP_LOT=B.FNUMBER
                                       where A.FMATERIALID='{ylqd["FMATERIALID"]}' and A.FSTOCKID='{ylqd["FSTOCKID"]}' AND A.FSTOCKORGID=100026 and A.FBASEQTY>0 and A.FSTOCKSTATUSID=case when A.FSTOCKID=22315406 then 27910195 else 10000 end
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
        //计算数量
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
                        pgsl = pgsl + Convert.ToDecimal(date["FQty"]);
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
                         inner join T_PRD_PPBOMENTRY b on a.fid=b.fid
                         inner join T_PRD_PPBOMENTRY_C b1 on b1.FENTRYID=b.FENTRYID
                         inner join T_BD_MATERIAL c on  a.FMATERIALID=c.FMATERIALID
                         inner join T_BD_MATERIAL_L d on  b.FMATERIALID=d.FMATERIALID
                         inner join t_BD_MaterialBase e on b.FMATERIALID=e.FMATERIALID
                         where c.FNUMBER='{WLDM}' and a.FMOBILLNO='{SCDD}' and a.FMOENTRYSEQ='{SEQ}' and FPARENTROWID='{FROWID}'
                          and FERPCLSID not in (1,3) and a.FPRDORGID=100026 and FMATERIALTYPE=3";
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
        }
    }
}
