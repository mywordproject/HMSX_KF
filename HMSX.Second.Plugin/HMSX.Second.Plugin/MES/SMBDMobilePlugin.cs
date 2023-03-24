using Kingdee.BOS;
using Kingdee.BOS.App.Data;
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
                    FillAllData(scanText);
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
            }
        }
        private void FillAllData(string ScanText)
        {
            if (ScanText != "")
            {

                string ylqdsql = $@"/*dialect*/select 
                         PGMX.FMoBillNo,PGMX.FMOSEQ,PGMX.FMoNumber,PGMX.FOptPlanNo,PGMX.FProcess,PGMX.FOperNumber,PGMX.FSEQNUMBER, 
                         PGMX.OptPlanNo,PGMX.FMaterialId,PGMX.FMaterialName,PGMX.F_LOT_Text,PGMX.FWORKQTY,PGMX.F_260_CSTM,PGMX.FBARCODE  
                         from T_PRD_PPBOM a
                         inner join T_PRD_PPBOMENTRY b on a.fid=b.fid
                         inner join T_BD_MATERIAL c on  a.FMATERIALID=c.FMATERIALID
                         inner join T_BD_MATERIAL d on  b.FMATERIALID=d.FMATERIALID
                         INNER JOIN
                         (select FMoBillNo,FMOSEQ,concat(FMoBillNo,'-',FMOSEQ) as FMoNumber,FOptPlanNo,t3.FName as FProcess,FOperNumber,FSEQNUMBER,F_260_CSTM,
                           concat(FOptPlanNo,'-',FSEQNUMBER,'-',FOperNumber) as OptPlanNo,t.FMaterialId,t2.FNAME as FMaterialName,t1.F_LOT_Text,t1.FWORKQTY,t1.FEntryId,t1.FBARCODE  
                           from T_SFC_DISPATCHDETAIL t 
                           inner join T_SFC_DISPATCHDETAILENTRY t1 on t.FID=t1.FID 
                           left join T_BD_MATERIAL_L t2 on t.FMATERIALID = t2.FMATERIALID and t2.FLOCALEID = 2052  
                           left join T_ENG_PROCESS_L t3 on t.FPROCESSID=t3.FID and t3.FLOCALEID = 2052
                            WHERE F_260_CSTM!=''and F_260_CSTM like '%{ScanText}%'
                           ) PGMX ON PGMX.FMATERIALID=b.FMATERIALID
                         where c.FNUMBER='{WLDM}' and a.FMOBILLNO='{SCDD}' and a.FMOENTRYSEQ='{SEQ}'";
                var rs = DBUtils.ExecuteDynamicObject(Context, ylqdsql);
                if (rs.Count > 0)
                {
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
                            this.View.Model.InsertEntryRow("FMobileListViewEntity", 0);
                            int Seq = i + 1;
                            this.View.Model.SetValue("FSeq", Seq + 1, i);
                            this.View.Model.SetValue("FMONumber", rs[i]["FMoNumber"].ToString(), 0);
                            this.View.Model.SetValue("FOperPlanNo", rs[i]["OptPlanNo"].ToString(), 0);
                            if (rs[i]["FProcess"] != null)
                            {
                                this.View.Model.SetValue("FProcessId", rs[i]["FProcess"].ToString(), 0);
                            }
                            this.View.Model.SetValue("FPgBarCode", rs[i]["FBARCODE"].ToString(), 0);
                            this.View.Model.SetValue("FProductId", rs[i]["FMaterialName"].ToString(), 0);
                            this.View.Model.SetValue("FLot", rs[i]["F_LOT_Text"].ToString(), 0);
                            this.View.Model.SetValue("FQty", rs[i]["FWORKQTY"].ToString(), 0);
                            this.View.Model.SetValue("F_CSTM", rs[i]["F_260_CSTM"].ToString(), 0);
                            this.View.UpdateView("FMobileListViewEntity");
                        }
                    }
                }
                else
                {
                    this.View.Model.SetValue("FText_OptPlanNumberScan", "");
                    throw new KDBusinessException("", "条码不是物料" + WLDM + "的子项物料,请重新扫码！！！");
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
                            FillAllData(e.Value.ToString());
                            e.Value = string.Empty;
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
        public void FHSJ()
        {
            decimal bl = 0;
            string tm = "";
            string ylqdsql = $@"/*dialect*/select 
                         d.FNAME,FNUMERATOR/FDENOMINATOR bl
                         from T_PRD_PPBOM a
                         inner join T_PRD_PPBOMENTRY b on a.fid=b.fid
                         inner join T_BD_MATERIAL c on  a.FMATERIALID=c.FMATERIALID
                         inner join T_BD_MATERIAL_L d on  b.FMATERIALID=d.FMATERIALID
                         where c.FNUMBER='{WLDM}' and a.FMOBILLNO='{SCDD}' and a.FMOENTRYSEQ='{SEQ}'";
            var ylqds = DBUtils.ExecuteDynamicObject(Context, ylqdsql);
            DynamicObjectCollection dates = this.Model.DataObject["MobileListViewEntity"] as DynamicObjectCollection;
            foreach(var ylqd in ylqds)
            {
                decimal pgsl = 0;
                foreach (var date in dates)
                {
                    if (ylqd["FNAME"].ToString() == date["FProductId"].ToString())
                    {
                        pgsl = pgsl + Convert.ToDecimal(date["FQty"]);
                        tm = tm + date["F_CSTM"].ToString()+",";
                    }                  
                }
                if (bl == 0)
                {
                    bl = pgsl / Convert.ToDecimal(ylqd["bl"]);
                }
                else
                {
                    if(bl!= (pgsl / Convert.ToDecimal(ylqd["bl"])))
                    {
                        throw new KDBusinessException("", "扫描条码的物料数量与BOM不成比例！！！");
                    }
                }
            }
            if (Convert.ToDecimal(RLSL) < bl)
            {
                throw new KDBusinessException("", "绑定数量不能大于可认领数量！！");
            }
            string[] rs = new string[2];
            rs[0] = tm.Trim(',');
            rs[1] =Convert.ToString(bl);
            this.View.ReturnToParentWindow(rs);
            base.View.Close();
        }
    }
}
