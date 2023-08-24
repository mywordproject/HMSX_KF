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
    [Description("扫码转移确认")]
    public class SMZYQRMobilePlugin : AbstractMobilePlugin
    {
        string GXJH = "";
        string XLH = "";
        string XH = "";
        protected FormCacheModel cacheModel4Save = new FormCacheModel();
        protected bool HasCached;
        public override void OnInitialize(InitializeEventArgs e)
        {
            base.OnInitialize(e);
            GXJH = e.Paramter.GetCustomParameter("GXJH").ToString();
            XLH = e.Paramter.GetCustomParameter("XLH").ToString();
            XH = e.Paramter.GetCustomParameter("GX").ToString();
        }
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            decimal sl = 0;
            string gxhbsql = $@"select a.FENTRYID,FQUAQTY,FLOT,FLOT_Text,FDISPATCHDETAILENTRYID from T_SFC_OPTRPTENTRY a
                                             left join T_SFC_OPTRPTENTRY_b b on a.fentryid=b.fentryid
                                             left join T_SFC_OPTRPTENTRY_A C on a.fentryid=C.fentryid
                                             WHERE FOPTPLANNO='{GXJH}' and   FSEQNUMBER='{XLH}' and FOPERNUMBER='{XH}' and FQUAQTY>0
                                             and FDISPATCHDETAILENTRYID not in (
											 select b.F_260_PGID from T_SFC_OPERATIONTRANSFER_a a
                                             inner join T_260_PGMXEntry b on a.fid=b.fid
                                             where FOUTOPBILLNO='{GXJH}' and FOUTSEQNUMBER='{XLH}' and FOUTOPERNUMBER='{XH}')";
            var gxhbs = DBUtils.ExecuteDynamicObject(Context, gxhbsql);
            foreach (var gxhb in gxhbs)
            {
                this.Model.CreateNewEntryRow("FMobileListViewEntity");
                int x = this.Model.GetEntryRowCount("FMobileListViewEntity");
                this.View.Model.SetValue("FSeq", x, x - 1);
                this.View.Model.SetValue("F_PHID", Convert.ToInt64(gxhb["FLOT"]), x - 1);
                this.View.Model.SetValue("F_PH_TEXT", gxhb["FLOT_Text"].ToString(), x - 1);
                this.View.Model.SetValue("F_ZYQTY", Convert.ToDecimal(gxhb["FQUAQTY"]), x - 1);
                this.View.Model.SetValue("FPGID", gxhb["FDISPATCHDETAILENTRYID"].ToString(), x - 1);
                sl += Convert.ToDecimal(gxhb["FQUAQTY"]);
            }
            this.View.Model.SetValue("F_ZS_QTY", sl);
            this.View.UpdateView("F_ZS_QTY");
            this.View.UpdateView("FMobileListViewEntity");
        }
        public override void ButtonClick(ButtonClickEventArgs e)
        {
            string key;
            switch (key = e.Key.ToUpper())
            {
                case "FBUTTON_OPTPLANNUMBERSCAN":

                    string scanText = this.View.Model.GetValue("FText_OptPlanNumberScan").ToString();
                    //updateEntry(scanText);
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
                //下一页
                case "F_FHSJ":
                    FHSJ();
                    return;
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
        public void updateEntry(string ScanText)
        {
            if (ScanText != "")
            {
                if (ScanText.Contains("PGMX"))
                {
                    string[] text = ScanText.Split('-');
                    if (text.Length > 3)
                    {
                        foreach (var date in this.Model.DataObject["MobileListViewEntity"] as DynamicObjectCollection)
                        {
                            if (date["FPGID"] != null && date["FPGID"].ToString() == text[4])
                            {
                                this.View.Model.SetValue("F_SFSM", "Y", Convert.ToInt32(date["FSeq"]) - 1);
                            }
                        }
                        decimal smsl = 0;
                        foreach (var date in this.Model.DataObject["MobileListViewEntity"] as DynamicObjectCollection)
                        {
                            if (date["F_SFSM"] != null && date["F_SFSM"].ToString() == "Y")
                            {
                                smsl += Convert.ToDecimal(date["F_ZYQTY"]);
                            }
                        }
                        this.Model.SetValue("F_QTY", smsl);
                        this.View.UpdateView("F_QTY");
                        this.View.UpdateView("FMobileListViewEntity");
                    }
                }
            }
        }
        //返回值
        public void FHSJ()
        {
            List<ZYMX> pickinfoList = new List<ZYMX>();
            foreach (var date in this.Model.DataObject["MobileListViewEntity"] as DynamicObjectCollection)
            {
                if (date["F_SFSM"] != null && date["F_SFSM"].ToString() == "Y")
                {
                    ZYMX pInfo = new ZYMX();
                    pInfo.Fpgid = date["FPGID"].ToString();
                    pInfo.Flot = date["F_PHID"].ToString();
                    pInfo.Fsl = Convert.ToDecimal(date["F_ZYQTY"]);
                    pInfo.Flot_text = date["F_PH_TEXT"].ToString();
                    
                    pickinfoList.Add(pInfo);
                }
            }

            this.View.ReturnToParentWindow(pickinfoList);
            base.View.Close();

        }
      
        public class ZYMX
        {
            private string _pgid;
            private string _lot_text;
            private decimal _sl;
            private string _lot;
            /// <summary>
            /// 派工ID
            /// </summary>
            public string Fpgid
            {
                get { return _pgid; }
                set { _pgid = value; }
            }
            /// <summary>
            /// 批号编码
            /// </summary>
            public string Flot_text
            {
                get { return _lot_text; }
                set { _lot_text = value; }
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
            /// 批号ID
            /// </summary>
            public string Flot
            {
                get { return _lot; }
                set { _lot = value; }
            }
        }
    }
}
