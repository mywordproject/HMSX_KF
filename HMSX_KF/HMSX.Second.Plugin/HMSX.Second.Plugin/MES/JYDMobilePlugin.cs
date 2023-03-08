using System;
using System.Collections.Generic;
using System.ComponentModel;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.Utils;
using Kingdee.BOS.Mobile.PlugIn;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Mobile.PlugIn.ControlModel;
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.ComplexCacheJson.Utils;
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.ComplexCacheJson.Model;
using Kingdee.BOS.JSON;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.WebApi.FormService;
using Newtonsoft.Json.Linq;
using System.Linq;
using Newtonsoft.Json;
using Kingdee.BOS;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Resource;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Mobile;

namespace HMSX.Second.Plugin.MES
{
    [Description("检验单--移动端")]
    [Kingdee.BOS.Util.HotUpdate]
    public class JYDMobilePlugin : AbstractMobilePlugin
    {
        protected FormCacheModel cacheModel4Save = new FormCacheModel();
        protected bool HasCached;
        string scanText = "";
        public override void ButtonClick(ButtonClickEventArgs e)
        {
            string key;
            scanText = this.View.Model.GetValue("F_TMSM") == null ? "" : this.View.Model.GetValue("F_TMSM").ToString();
            switch (key = e.Key.ToUpper())
            {
                case "F_TMSM_BUTTON":                   
                    FillAllData(scanText, "");
                    //this.View.Model.SetValue("F_TMSM", "");
                    //this.View.UpdateView("F_TMSM");
                    this.View.GetControl("F_TMSM").SetFocus();
                    this.View.GetControl("F_TMSM").SetCustomPropertyValue("showKeyboard", true);
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
                case "F_QX":
                    SelectAll();
                    return;
                case "F_SUBMIT":
                    Submit();
                    FillAllData(scanText, "");
                    return;
                case "F_XG":
                    PickMaterial("修改");
                    return;
                case "F_XZ":
                    PickMaterial("新增");
                    return;
                case "F_SX":
                    FillAllData("", "");
                    this.View.Model.SetValue("F_TMSM", "");
                    this.View.UpdateView("F_TMSM");
                    return;
                case "F_CX":
                    string Number = this.View.Model.GetValue("F_FNUMBER").ToString();
                    FillAllData("", Number);
                    return;
                case "F_PLXG":
                    PLXG();
                    FillAllData(scanText, "");
                    return;
                case "F_SH":
                    Audit();
                    FillAllData("", "");
                    this.View.Model.SetValue("F_TMSM", "");
                    this.View.UpdateView("F_TMSM");
                    return;
            }
        }
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            //this.View.Model.SetValue("F_TMSM", "");
            this.View.GetControl("F_TMSM").SetFocus();
            this.View.GetControl("F_TMSM").SetCustomPropertyValue("showKeyboard", true);
            //this.InitFocus();
            //this.View.UpdateView("F_TMSM");
            this.View.GetControl("F_SLSB_MobileListViewEntity").SetCustomPropertyValue("listEditable", true);
            FillAllData("", "");
        }
        private void FillAllData(string OPH, string WL)
        {
            if (OPH != "" && OPH.IndexOf('-')>0)
            {
                OPH = (OPH.Split('-'))[0];
            }
            string strSql = $@"/*dialect*/SELECT row_number() over(order by A.FBILLNO desc) as XH,A.FID,B.FENTRYID,C.FDETAILID,
                                   A.FBILLNO,FDATE,FINSPECTORID,CASE WHEN A.FDOCUMENTSTATUS='A' THEN '创建' when A.FDOCUMENTSTATUS='B'then '审核中'ELSE '重新审核' end FDOCUMENTSTATUS,
                                   C.FMATERIALID,E.FNUMBER+'-'+F.FNAME FNAME,
                                   F_260_HBS FINSPECTQTY,F_101_TEXTLZKH,F_260_PGMXTM,
                                   FLOT,G.FNUMBER,FSTATUS,FQTY,FUSEPOLICY,
                                   FISDEFECTPROCESS,FMEMO,F_260_RBJSQRR
                                   FROM T_QM_INSPECTBILL A
                                   INNER JOIN T_QM_INSPECTBILLENTRY B ON A.FID=B.FID
                                   INNER JOIN T_QM_INSPECTBILLENTRY_A B1 ON B.FENTRYID=B1.FENTRYID
                                   inner JOIN T_BD_MATERIAL E ON E.FMATERIALID=B1.FMATERIALID
                                   inner JOIN T_BD_MATERIAL_L F ON E.FMATERIALID=F.FMATERIALID
                                   LEFT JOIN T_QM_IBPOLICYDETAIL C ON B.FENTRYID=C.FENTRYID
                                   left join T_QM_IBPOLICYDETAIL_L D ON  D.FDetailID=C.FDetailID
                                   left join T_BD_LOTMASTER G ON G.FLOTID=B.FLOT
                                   where  (A.FDOCUMENTSTATUS='A' OR A.FDOCUMENTSTATUS='D' OR A.FDOCUMENTSTATUS='B') AND FINSPECTORGID=100026
                                   AND A.FBILLTYPEID='005056945db184ed11e3af2dcda7ee49' AND (F_101_TEXTLZKH like'{OPH}%' or '{OPH}'='')
                                   and (E.FNUMBER='{WL}' OR '{WL}'='')";
            var rs = DBUtils.ExecuteDynamicObject(this.Context, strSql);
            if (rs.Count > 0)
            {
                this.View.Model.DeleteEntryData("F_SLSB_MobileListViewEntity");
                for (int i = 0; i < rs.Count; i++)
                {
                    this.View.Model.CreateNewEntryRow("F_SLSB_MobileListViewEntity");
                    this.View.Model.SetValue("F_XH", rs[i]["XH"].ToString(), i);
                    this.View.Model.SetValue("F_DJBH", rs[i]["FBILLNO"].ToString(), i);
                    this.View.Model.SetValue("F_RQ", rs[i]["FDATE"].ToString(), i);
                    this.View.Model.SetValue("F_GXJH", rs[i]["F_101_TEXTLZKH"].ToString(), i);
                    this.View.Model.SetValue("F_PGTM", rs[i]["F_260_PGMXTM"].ToString(), i);
                    this.View.Model.SetValue("F_CPXX", rs[i]["FNAME"].ToString(), i);
                    this.View.Model.SetValue("F_JYSL", rs[i]["FINSPECTQTY"].ToString(), i);
                    this.View.Model.SetValue("F_PH", rs[i]["FNUMBER"].ToString(), i);
                    this.View.Model.SetValue("F_FID", rs[i]["FID"].ToString(), i);
                    this.View.Model.SetValue("F_FENTRYID", rs[i]["FENTRYID"].ToString(), i);
                    this.View.Model.SetValue("F_FDETAILID", rs[i]["FDETAILID"].ToString(), i);
                    this.View.Model.SetValue("F_SYJC", rs[i]["FUSEPOLICY"],i);
                    this.View.Model.SetValue("F_JCSL", rs[i]["FQTY"],i);
                    this.View.Model.SetValue("F_JYMS", rs[i]["FMEMO"], i);
                    this.View.Model.SetValue("F_ZT", rs[i]["FSTATUS"], i);
                    this.View.Model.SetItemValueByID("F_ZJY", rs[i]["FINSPECTORID"], i);
                    this.View.Model.SetItemValueByID("F_QRR", rs[i]["F_260_RBJSQRR"], i);
                    this.View.Model.SetValue("F_BLCL", rs[i]["FISDEFECTPROCESS"], i);
                    this.View.Model.SetValue("F_DJZT", rs[i]["FDOCUMENTSTATUS"], i);
                }
                this.View.UpdateView("F_SLSB_MobileListViewEntity");
            }
        }
        public override void EntityRowDoubleClick(EntityRowClickEventArgs e)
        {
            base.EntityRowDoubleClick(e);                    
        }
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (e.Field.Key == "F_SYJC")
            {
                if (this.Model.GetValue("F_SYJC").ToString() == "A")
                {
                    this.Model.SetValue("F_ZT", 1);
                }
                else if (this.Model.GetValue("F_SYJC").ToString() == "I")
                {
                    this.Model.SetValue("F_ZT", 2);
                    this.Model.SetValue("F_BLCL", true);
                }
            }
            else if (e.Field.Key == "F_ZT")
            {
                if (this.Model.GetValue("F_ZT").ToString() == "1")
                {
                    this.Model.SetValue("F_SYJC", "A");
                }
                else if (this.Model.GetValue("F_ZT").ToString() == "2")
                {
                    this.Model.SetValue("F_SYJC", "I");
                    this.Model.SetValue("F_BLCL", false);
                }
            }
        }
        public void SelectAll()
        {
            int entryRowCount = this.Model.GetEntryRowCount("F_SLSB_MobileListViewEntity");
            List<int> list = new List<int>();
            for (int i = 0; i < entryRowCount; i++)
            {
                list.Add(i);
            }
            base.View.GetControl<MobileListViewControl>("F_SLSB_MobileListViewEntity").SetSelectRows(list.ToArray());
            this.View.UpdateView("F_SLSB_MobileListViewEntity");
        }
        public void Submit()
        {
            int[] selectedRows = base.View.GetControl<MobileListViewControl>("F_SLSB_MobileListViewEntity").GetSelectedRows();
            if (!selectedRows.Any<int>())
            {
                base.View.ShowStatusBarInfo(Kingdee.BOS.Resource.ResManager.LoadKDString("未选择分录！", "015747000028217", Kingdee.BOS.Resource.SubSystemType.MFG, new object[0]));
            }
            else
            {
                string[] strs = new string[selectedRows.Length];
                for (int i = 0; i < selectedRows.Length; i++)
                {
                    if (this.Model.GetValue("F_DJZT", selectedRows[i]).ToString() == "审核中")
                    {
                        throw new KDBusinessException("", "单据审核中不能提交");
                    }
                    strs[i] = this.Model.GetValue("F_DJBH", selectedRows[i]).ToString();
                }
                strs = strs.GroupBy(p => p).Select(p => p.Key).ToArray();
                foreach (var str in strs)
                {
                    JObject json = new JObject();
                    json.Add("Numbers", str);
                    var result = WebApiServiceCall.Submit(this.Context, "QM_InspectBill", json.ToString());
                    bool isSuccess = Convert.ToBoolean(JObject.Parse(JsonConvert.SerializeObject(result))["Result"]["ResponseStatus"]["IsSuccess"].ToString());
                    string c = KDObjectConverter.SerializeObject(result);
                    if (isSuccess)
                    {
                        base.View.ShowMessage(ResManager.LoadKDString("提交成功！", "015747000026506", SubSystemType.MFG, new object[0]), MessageBoxType.Notice);
                        
                    }
                    else
                    {
                        string Errors = JObject.Parse(JsonConvert.SerializeObject(result))["Result"]["ResponseStatus"]["Errors"].ToString();
                        var JErrors = JArray.Parse(Errors);
                        string message = "";
                        foreach (var Error in JErrors)
                        {
                            message += ((JObject)Error)["Message"].ToString() + ",";
                        }
                        throw new KDBusinessException("", message);
                    }
                }
            }
        }
        public void PickMaterial(string an)
        {

            int[] selectedRowIndexs = this.View.GetControl<MobileListViewControl>("F_SLSB_MobileListViewEntity").GetSelectedRows();
            if (selectedRowIndexs.Length == 1)
            {
                var dates = this.View.Model.DataObject["SLSB_Kb8d20421"] as DynamicObjectCollection;
                if(dates.Count > 0)
                {
                    DynamicObject da=null;
                     foreach( var date in dates)
                     {
                        if (Convert.ToInt32( date["F_XH"])== selectedRowIndexs[0]+1)
                        {
                            if (date["F_DJZT"].ToString() == "审核中")
                            {
                                throw new KDBusinessException("", "单据审核中不能修改/新增");
                            }
                            da = date;
                        }
                     }                 
                    MobileShowParameter param = new MobileShowParameter();
                    param.FormId = "keed_JYDBJ";
                    param.ParentPageId = this.View.PageId;
                    param.SyncCallBackAction = false;
                    param.CustomComplexParams.Add("date", da);
                    param.CustomParams.Add("Name", an);
                    this.View.ShowForm(param,delegate(FormResult returnValue)
                     {
                         FillAllData("", "");
                     });
                }
            }
            else if (selectedRowIndexs.Length == 0)
            {
                this.View.ShowStatusBarInfo(ResManager.LoadKDString("请选择行！", "015747000026624", SubSystemType.MFG, new object[0]));
                return;
            }
            else if (selectedRowIndexs.Length > 1)
            {
                this.View.ShowStatusBarInfo(ResManager.LoadKDString("新增或修改只能选择一行！", "015747000026624", SubSystemType.MFG, new object[0]));
                return;
            }

        }
        public void PLXG()
        {
            string FINSPECTORID = this.Model.GetValue("F_260_ZJY") == null ? "" : ((DynamicObject)this.Model.GetValue("F_260_ZJY"))["Id"].ToString();//质检员
            if (FINSPECTORID == "")
            {
                throw new KDBusinessException("", "质检员不能为空");
            }
            int[] selectedRows = base.View.GetControl<MobileListViewControl>("F_SLSB_MobileListViewEntity").GetSelectedRows();
            if (!selectedRows.Any<int>())
            {
                base.View.ShowStatusBarInfo(Kingdee.BOS.Resource.ResManager.LoadKDString("未选择分录！", "015747000028217", Kingdee.BOS.Resource.SubSystemType.MFG, new object[0]));
            }
            else
            {
                string strs = "";
                for (int i = 0; i < selectedRows.Length; i++)
                {
                    if(this.Model.GetValue("F_DJZT", selectedRows[i]).ToString() == "审核中")
                    {
                        throw new KDBusinessException("", "单据审核中不能修改");
                    }
                    strs+= this.Model.GetValue("F_FID", selectedRows[i]).ToString()+",";
                }                        
                string upsql1 = $@"update T_QM_INSPECTBILL set FINSPECTORID={FINSPECTORID} where FID in ({strs.Trim(',')})";
                DBUtils.ExecuteDynamicObject(Context, upsql1);
                this.View.ShowMessage("批量修改质检员成功");

            }

        }
        public void Audit()
        {
            int[] selectedRows = base.View.GetControl<MobileListViewControl>("F_SLSB_MobileListViewEntity").GetSelectedRows();
            if (!selectedRows.Any<int>())
            {
                base.View.ShowStatusBarInfo(Kingdee.BOS.Resource.ResManager.LoadKDString("未选择分录！", "015747000028217", Kingdee.BOS.Resource.SubSystemType.MFG, new object[0]));
            }
            else
            {
                string[] strs = new string[selectedRows.Length];
                for (int i = 0; i < selectedRows.Length; i++)
                {
                    if (this.Model.GetValue("F_DJZT", selectedRows[i]).ToString() != "审核中")
                    {
                        throw new KDBusinessException("", "单据状态为审核中才能审核");
                    }
                    strs[i] = this.Model.GetValue("F_DJBH", selectedRows[i]).ToString();
                }
                strs = strs.GroupBy(p => p).Select(p => p.Key).ToArray();
                foreach (var str in strs)
                {
                    JObject json = new JObject();
                    json.Add("Numbers", str);
                    var result = WebApiServiceCall.Audit(this.Context, "QM_InspectBill", json.ToString());
                    bool isSuccess = Convert.ToBoolean(JObject.Parse(JsonConvert.SerializeObject(result))["Result"]["ResponseStatus"]["IsSuccess"].ToString());
                    string c = KDObjectConverter.SerializeObject(result);
                    if (isSuccess)
                    {
                        base.View.ShowMessage(ResManager.LoadKDString("审核成功！", "015747000026506", SubSystemType.MFG, new object[0]), MessageBoxType.Notice);                  
                    }
                    else
                    {
                        string Errors = JObject.Parse(JsonConvert.SerializeObject(result))["Result"]["ResponseStatus"]["Errors"].ToString();
                        var JErrors = JArray.Parse(Errors);
                        string message = "";
                        foreach (var Error in JErrors)
                        {
                            message += ((JObject)Error)["Message"].ToString() + ",";
                        }
                        throw new KDBusinessException("", message);
                    }
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
                    if (key == "F_TMSM")
                    {
                        string text = Convert.ToString(e.Value);
                        if (!string.IsNullOrEmpty(text) && !string.IsNullOrWhiteSpace(text))
                        {
                            FillAllData(text,"");
                            //e.Value = string.Empty;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                e.Value = string.Empty;
                //this.CurrOptPlanScanCode = string.Empty;
                this.View.ShowStatusBarInfo(ex.Message);
            }
            this.View.GetControl(e.Key).SetFocus();
        }
    }
}
