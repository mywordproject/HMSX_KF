using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Mobile;
using Kingdee.BOS.Mobile.PlugIn;
using Kingdee.BOS.Mobile.PlugIn.ControlModel;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Resource;
using Kingdee.BOS.ServiceHelper;
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.MFG.Mobile.Business.PlugIn
{
    [Description("退料列表-表单插件")]

    public class ReturnList : AbstractMobilePlugin
    {
        protected MobileListFormaterManager ListFormaterManager = new MobileListFormaterManager();
        public static DynamicObjectCollection dispatchDetailInfo;
        protected string _scanText;
        public override void OnInitialize(InitializeEventArgs e)
        {
            base.OnInitialize(e);
            this.View.GetControl("FLable_User").SetValue(this.Context.UserName);
        }

        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            this.View.GetControl("FMobileListViewEntity").SetCustomPropertyValue("listEditable", true);
            this.InitFocus();

        }

        protected virtual void InitFocus()
        {
            if (this.View.BusinessInfo.ContainsKey("FText_OptPlanNumberScan"))
            {
                this.View.GetControl("FText_OptPlanNumberScan").SetFocus();
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
                        string text = Convert.ToString(e.Value);
                        _scanText = text;
                        if (!string.IsNullOrEmpty(text) && !string.IsNullOrWhiteSpace(text))
                        {
                            if (Convert.ToBoolean(this.Model.GetValue("FIsCompleted")))
                            {
                                this.ReloadData(this.GetDispatchDetail(text), true);
                                e.Value = string.Empty;
                            }
                            else
                            {
                                dispatchDetailInfo = this.GetDispatchDetail(text);
                                FillAllData(text);
                                e.Value = string.Empty;

                            }
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

        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (e.Field.Key.Equals("FSelect"))
            {
                if (e.NewValue.Equals(true))
                {
                    this.View.GetControl<MobileListViewControl>("FMobileListViewEntity").SetSelectRows(new int[] { e.Row });
                    this.View.UpdateView("FMobileListViewEntity");
                }
            }
            if (e.Field.Key == "FIsCompleted")
            {
                if (_scanText != null)
                {
                    if (Convert.ToBoolean(e.NewValue))
                    {
                        this.ReloadData(this.GetDispatchDetail(_scanText), true);
                    }
                    else { FillAllData(_scanText); }
                }
            }
        }
        public override void EntityRowDoubleClick(EntityRowClickEventArgs e)
        {
            base.EntityRowDoubleClick(e);
            SetHighLight();
        }
        protected virtual void SetHighLight()
        {

            int entryRowCount = this.Model.GetEntryRowCount("FMobileListViewEntity");
            int[] selectedRows = base.View.GetControl<MobileListViewControl>("FMobileListViewEntity").GetSelectedRows();
            for (int i = 1; i <= entryRowCount; i++)
            {
                if (selectedRows != null && selectedRows.Contains(i))
                {
                    this.ListFormaterManager.SetControlProperty("FFlowLayout_Row", i - 1, "255,234,199", MobileFormatConditionPropertyEnums.BackColor);
                }
                else
                {
                    this.ListFormaterManager.SetControlProperty("FFlowLayout_Row", i - 1, "255,255,255", MobileFormatConditionPropertyEnums.BackColor);

                }
            }
            base.View.GetControl<MobileListViewControl>("FMobileListViewEntity").SetFormat(this.ListFormaterManager);
            base.View.UpdateView("FMobileListViewEntity");
        }
      
        public override void ButtonClick(ButtonClickEventArgs e)
        {
            string key;
            switch (key = e.Key.ToUpper())
            {
                case "FBUTTON_OPTPLANNUMBERSCAN":
                    string scanText = this.View.Model.GetValue("FText_OptPlanNumberScan").ToString();

                    if (Convert.ToBoolean(this.Model.GetValue("FIsCompleted")))
                    {

                        this.ReloadData(this.GetDispatchDetail(scanText), true);

                    }
                    else
                    {

                        dispatchDetailInfo = this.GetDispatchDetail(scanText);
                        FillAllData(scanText);

                    }
                    return;

                case "FBUTTON_RETURN":
                    this.View.Close();
                    return;

                case "FBUTTON_LOGOUT":
                    LoginUtils.LogOut(base.Context, base.View);
                    this.View.Logoff("indexforpad.aspx");
                    return;
                case "FBUTTON_CONFIRM":

                    PickMaterial();
                    return;
               
            }
        }

        private void FillAllData(string ScanText)
        {
            if (ScanText != "")
            {
                //获取已领料的派工明细
                string strSql = string.Format(@"/*dialect*/select FMoBillNo,FMOSEQ,concat(FMoBillNo,'-',FMOSEQ) as FMoNumber,FOptPlanNo,t3.FName as FProcess,FOperNumber,FSEQNUMBER, 
                                                                concat(FOptPlanNo,'-',FSEQNUMBER,'-',FOperNumber) as OptPlanNo,t.FMaterialId,t2.FNAME as FMaterialName,t1.F_LOT_Text,t1.FWORKQTY,t1.FEntryId,t1.FBARCODE  
                                                                from T_SFC_DISPATCHDETAIL t inner join T_SFC_DISPATCHDETAILENTRY t1 
                                                                on t.FID=t1.FID and t.FOperId=(select FDETAILID from T_SFC_OPERPLANNINGDETAIL where FBarCode='{0}')   
                                                                 AND t1.FENTRYID  IN  (select  FPgEntryId from(select FPgEntryId,sum(FAvailableQty) as FPickQty  from t_PgBomInfo Group by FPgEntryId) a where a.FPickQty>0)  
                                                                 AND t1.FSTATUS='B' 
                                                               left join T_BD_MATERIAL_L t2 on t.FMATERIALID = t2.FMATERIALID and t2.FLOCALEID = 2052  
                                                               left join T_ENG_PROCESS_L t3 on t.FPROCESSID=t3.FID and t3.FLOCALEID = 2052", ScanText);
                DynamicObjectCollection rs = DBServiceHelper.ExecuteDynamicObject(this.Context, strSql);
                if (rs.Count > 0)
                {
                    this.View.Model.DeleteEntryData("FMobileListViewEntity");
                    for (int i = 0; i < rs.Count; i++)
                    {
                        this.View.Model.CreateNewEntryRow("FMobileListViewEntity");
                        int rowCount = this.View.Model.GetEntryRowCount("FMobileListViewEntity");
                        int Seq = i + 1;
                        this.View.Model.SetValue("FSeq", Seq, i);
                        this.View.Model.SetValue("FMONumber", rs[i]["FMoNumber"].ToString(), i);
                        this.View.Model.SetValue("FOperPlanNo", rs[i]["OptPlanNo"].ToString(), i);
                        this.View.Model.SetValue("FProcessId", rs[i]["FProcess"].ToString(), i);
                        this.View.Model.SetValue("FPgBarCode", rs[i]["FBARCODE"].ToString(), i);
                        this.View.Model.SetValue("FProductId", rs[i]["FMaterialName"].ToString(), i);
                        this.View.Model.SetValue("FLot", rs[i]["F_LOT_Text"].ToString(), i);
                        this.View.Model.SetValue("FQty", rs[i]["FWORKQTY"].ToString(), i);
                        this.View.Model.SetValue("FPgEntryId", rs[i]["FEntryId"].ToString(), i);
                        this.View.UpdateView("FMobileListViewEntity");
                    }
                }
            }
        }

        private void ReloadData(DynamicObjectCollection dispatchDetailInfo, bool flag)
        {
            DynamicObjectCollection dispatchDetailTmp = null;
            if (dispatchDetailInfo != null)
            {
                if (flag)
                {
                    dispatchDetailTmp = dispatchDetailInfo;
                }
                else
                {
                    dispatchDetailTmp = (DynamicObjectCollection)(from p in dispatchDetailInfo where p["FSTATUS"].ToString() == "B" select p);
                }
                this.View.Model.DeleteEntryData("FMobileListViewEntity");
                int i = 0;
                foreach (DynamicObject obj in dispatchDetailTmp)
                {
                    this.View.Model.CreateNewEntryRow("FMobileListViewEntity");
                    int rowCount = this.View.Model.GetEntryRowCount("FMobileListViewEntity");
                    int Seq = i + 1;
                    this.View.Model.SetValue("FSeq", Seq, i);
                    this.View.Model.SetValue("FMONumber", obj["FMoNumber"].ToString(), i);
                    this.View.Model.SetValue("FOperPlanNo", obj["OptPlanNo"].ToString(), i);
                    this.View.Model.SetValue("FProcessId", obj["FProcess"].ToString(), i);
                    this.View.Model.SetValue("FPgBarCode", obj["FBARCODE"].ToString(), i);
                    this.View.Model.SetValue("FProductId", obj["FMaterialName"].ToString(), i);
                    this.View.Model.SetValue("FLot", obj["F_LOT_Text"].ToString(), i);
                    this.View.Model.SetValue("FQty", obj["FWORKQTY"].ToString(), i);
                    this.View.Model.SetValue("FPgEntryId", obj["FEntryId"].ToString(), i);
                    this.View.UpdateView("FMobileListViewEntity");
                    i++;
                }
            }
        }
        public DynamicObjectCollection GetDispatchDetail(string ScanText)
        {
            DynamicObjectCollection rs = null;
            if (ScanText != "")
            {
                //获取已领料的派工明细
                string strSql = string.Format(@"/*dialect*/select FMoBillNo,FMOSEQ,concat(FMoBillNo,'-',FMOSEQ) as FMoNumber,FOptPlanNo,t3.FName as FProcess,FOperNumber,FSEQNUMBER, 
                                                                concat(FOptPlanNo,'-',FSEQNUMBER,'-',FOperNumber) as OptPlanNo,t.FMaterialId,t2.FNAME as FMaterialName,t1.F_LOT_Text,t1.FWORKQTY,t1.FEntryId,t1.FBARCODE ,t1. FSTATUS 
                                                                from T_SFC_DISPATCHDETAIL t inner join T_SFC_DISPATCHDETAILENTRY t1 
                                                                on t.FID=t1.FID and t.FOperId=(select FDETAILID from T_SFC_OPERPLANNINGDETAIL where FBarCode='{0}')   
                                                                 AND t1.FENTRYID  IN  (select  FPgEntryId from(select FPgEntryId,sum(FAvailableQty) as FPickQty  from t_PgBomInfo Group by FPgEntryId) a where a.FPickQty>0)  
                                                                 AND t1.FSTATUS  IN ('B' ,'D')
                                                               left join T_BD_MATERIAL_L t2 on t.FMATERIALID = t2.FMATERIALID and t2.FLOCALEID = 2052  
                                                               left join T_ENG_PROCESS_L t3 on t.FPROCESSID=t3.FID and t3.FLOCALEID = 2052", ScanText);
                rs = DBServiceHelper.ExecuteDynamicObject(this.Context, strSql);

            }
            return rs;
        }
        public void PickMaterial()
        {

            int[] selectedRowIndexs = this.View.GetControl<MobileListViewControl>("FMobileListViewEntity").GetSelectedRows();
            int rowIndex = 0;
            int rowcount = this.View.Model.GetEntryRowCount("FMobileListViewEntity");
            if (rowcount > 0)
            {
                string entryId = "0";
                for (int row = 0; row < rowcount; row++)
                {
                    if (Convert.ToBoolean(this.View.Model.GetValue("FSelcet", row)))
                    {
                        rowIndex = rowIndex + 1;
                        entryId = entryId + ',' + this.View.Model.GetValue("FPgEntryId", row).ToString();
                    }
                }
                if (rowIndex == 0)
                {
                    this.View.ShowStatusBarInfo(ResManager.LoadKDString("请选择要退料的行！", "015747000026624", SubSystemType.MFG, new object[0]));
                    return;
                }

                MobileShowParameter param = new MobileShowParameter();
                param.FormId = "k5383faabccc548ccb8273ea83b5d7818";
                param.ParentPageId = this.View.PageId;
                param.SyncCallBackAction = false;
                param.CustomParams.Add("FPgEntryId", entryId);

                this.ShowFrom(param);
            }
        }

        private void ShowFrom(MobileShowParameter param)
        {
            this.View.ShowForm(param);
        }
    }
}
