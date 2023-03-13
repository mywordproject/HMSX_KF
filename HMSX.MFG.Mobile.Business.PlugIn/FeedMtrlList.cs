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

namespace HMSX.MFG.Mobile.Business.PlugIn
{
    [Description("补料列表-表单插件")]
    [Kingdee.BOS.Util.HotUpdate]
    public  class FeedMtrlList: AbstractMobilePlugin
    {
        protected MobileListFormaterManager ListFormaterManager = new MobileListFormaterManager();
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
        public void SetHighLightS()
        {
            int entryRowCount = this.Model.GetEntryRowCount("FMobileListViewEntity");
           // List<int> list = new List<int>();
            for (int i = 0; i < entryRowCount; i++)
            {
                this.View.Model.SetValue("FSelcet", true, i);
              //  list.Add(i);
              //  this.ListFormaterManager.SetControlProperty("FFlowLayout_Row", i, "255,234,199", MobileFormatConditionPropertyEnums.BackColor);
            }
            //base.View.GetControl<MobileListViewControl>("FMobileListViewEntity").SetSelectRows(list.ToArray());
            //base.View.GetControl<MobileListViewControl>("FMobileListViewEntity").SetFormat(this.ListFormaterManager);
            base.View.UpdateView("FMobileListViewEntity");
        }

        public override void BeforeUpdateValue(BeforeUpdateValueEventArgs e)
        {
            base.BeforeUpdateValue(e);
            this.ScanCodeChanged(e);

        }
        private void ScanCodeChanged(BeforeUpdateValueEventArgs e)
        {
            // base.ClearDicFilterValues();
            try
            {
                string key;
                if ((key = e.Key) != null)
                {
                    if (key == "FText_OptPlanNumberScan")
                    {
                        string text = Convert.ToString(e.Value);
                        if (!string.IsNullOrEmpty(text) && !string.IsNullOrWhiteSpace(text))
                        {
                            string[] scText = text.Split('-');
                            if (scText.Length> 2)
                            {
                                string scanText = scText[0] + "-" + scText[1] + "-" + scText[2];
                                FillAllData(scanText);
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
        }
        public override void ButtonClick(ButtonClickEventArgs e)
        {
            string key;
            switch (key = e.Key.ToUpper())
            {
                case "FBUTTON_OPTPLANNUMBERSCAN":
                    string[] scText = this.View.Model.GetValue("FText_OptPlanNumberScan").ToString().Split('-');
                    //op-0-10-pgmx-id
                    if (scText.Length > 2)
                    {
                        string scanText = scText[0] + "-" + scText[1] + "-" + scText[2];
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

                    FeedMaterial();
                    return;
                case "F_QX":
                    SetHighLightS();
                    return;
            }
        }

        private void FillAllData(string ScanText)
        {
            if (ScanText != "")
            {
                string strSql = string.Format(@"/*dialect*/select FMoBillNo,FMOSEQ,concat(FMoBillNo,'-',FMOSEQ) as FMoNumber,FOptPlanNo,t3.FName as FProcess,FOperNumber,FSEQNUMBER, 
                             concat(FOptPlanNo,'-',FSEQNUMBER,'-',FOperNumber) as OptPlanNo,t.FMaterialId,t2.FNAME as FMaterialName,t1.F_LOT_Text,t1.FWORKQTY,t1.FEntryId,t1.FBARCODE  
                             from T_SFC_DISPATCHDETAIL t inner join T_SFC_DISPATCHDETAILENTRY t1 
                             on t.FID=t1.FID and t.FOperId=(select FDETAILID from T_SFC_OPERPLANNINGDETAIL where FBarCode='{0}')  
                             AND (t1.FENTRYID  IN  (select  FPgEntryId from(select FPgEntryId,sum(FAvailableQty) as FPickQty  from t_PgBomInfo where FMustQty!=0 Group by FPgEntryId) a where a.FPickQty>0 )  
                              or  t1.FENTRYID  IN  (select  FPgEntryId from(select FPgEntryId,sum(FAvailableQty) as FPickQty  from t_PgBomInfo where FMustQty=0 Group by FPgEntryId) a ) 
                             or t1.FENTRYID  IN  (select  FPgEntryId from(
                             select FPgEntryId,sum(FAvailableQty) as FPickQty  from t_PgBomInfo a
                             INNER JOIN T_PRD_PPBOM b ON a.FPPBomId = b.FID
                             INNER JOIN T_BAS_BILLTYPE c ON b.FMOTYPE=c.FBILLTYPEID
                             where FMustQty!=0 and c.FNUMBER='SCDD02_SYS'
                             Group by FPgEntryId
                             ) a where a.FPickQty=0 )  ) 
                            
                              AND t1.FSTATUS='B'  
                             left join T_BD_MATERIAL_L t2 on t.FMATERIALID = t2.FMATERIALID and t2.FLOCALEID = 2052  
                            left join T_ENG_PROCESS_L t3 on t.FPROCESSID=t3.FID and t3.FLOCALEID = 2052", ScanText);
                DynamicObjectCollection rs = DBServiceHelper.ExecuteDynamicObject(this.Context, strSql);
                //and  t1.FENTRYID  not IN  (select  FPgEntryId from(select FPgEntryId,sum(FAvailableQty) as FPickQty  from t_PgBomInfo where FMustQty!=0 and FMustQty=FAvailableQty Group by FPgEntryId) a )
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

        public void FeedMaterial()
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
                    this.View.ShowStatusBarInfo(ResManager.LoadKDString("请选择要补料的行！", "015747000026624", SubSystemType.MFG, new object[0]));
                    return;
                }

            
                MobileShowParameter param = new MobileShowParameter();
                param.FormId = "k03c07a0df5bd441abb7b39e452d95c6a";
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
