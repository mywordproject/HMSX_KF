using Kingdee.BOS.Mobile.PlugIn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using System.ComponentModel;
using Kingdee.BOS.Resource;
using Kingdee.BOS.Core.Bill.PlugIn.Args;
using Kingdee.BOS.App;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Core.Interaction;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Util;

namespace HMSX.MFG.Mobile.Business.PlugIn
{
    [Description("调拨单-表单插件")]
    public class TransferEdit: AbstractMobileBillPlugin
    {
        Decimal allqty = 0;
        public override void OnInitialize(InitializeEventArgs e)
        {
            base.OnInitialize(e);
            this.View.GetControl("FLable_User").SetValue(this.Context.UserName);
            this.View.GetControl("F_SBID_MobileProxyEntryEntity").SetCustomPropertyValue("defaultRows", 0);
            this.View.BillModel.BillBusinessInfo.GetEntity("FBillEntry").DefaultRows = 0;
        }
     
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            this.View.GetControl("F_SBID_MobileProxyEntryEntity").SetCustomPropertyValue("listEditable",true);
            int rowcount = this.View.BillModel.GetEntryRowCount("FBillEntry");
            this.View.BillModel.DeleteEntryData("FBillEntry");
            this.InitFocus();
        }
        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            string key;
            switch (key = e.Key.ToUpper())
            {
                case "FBARCODEBUTTON":
                    if (this.View.BillModel.GetValue("FText_BarcodeScan") != null)
                    {
                        string scanText = this.View.BillModel.GetValue("FText_BarcodeScan").ToString();
                        updateEntry(scanText);
                        this.View.BillModel.SetValue("FText_BarcodeScan", " ");
                        this.View.UpdateView("FText_BarcodeScan");
                        this.InitFocus();
                    }
                    return;
                case "FBUTTON_RETURN":
                    this.View.Close();
                    return;
                case "FSUBMIT":
                   // this.Confirm();
                    return;
            }
        }
        public void updateEntry(string scanText)
        {
            if (scanText != "")
            {
                string strSql = "";
                if (scanText.Substring(0, 2) == "PG")
                {
                    strSql = string.Format(@"SELECT T.FMATERIALID,T1.FMASTERID,t2.FLOT,T3.FNUMBER,t2.FSTOCKID,t2.FSTOCKLOCID,t2.FBASEQTY,t2.FSTOCKUNITID,t2.FBASEUNITID FROM T_PRD_INSTOCKENTRY T 
                                                                        INNER JOIN T_BD_MATERIAL T1 ON T.FMATERIALID=T1.FMATERIALID  
	                                                                    INNER JOIN  T_STK_INVENTORY t2 on t1.FMASTERID=t2.FMATERIALID AND t.FLOT=t2.FLOT   AND T2.FBASEQTY>0 
                                                                        INNER JOIN  T_BD_LOTMASTER t3 ON t2.FLOT=t3.FLOTID 
                                                                        WHERE T.F_RUJP_PGBARCODE='{0}'", scanText);
                }
                else
                {
                    strSql = string.Format(@"SELECT t.FMATERIALID,t1.FMASTERID,t2.FLOT,T3.FNUMBER,t2.FSTOCKID,t2.FSTOCKLOCID,t2.FBASEQTY,t2.FSTOCKUNITID,t2.FBASEUNITID,t2.FSTOCKSTATUSID FROM T_BD_BARCODEMAIN t  
                                                                 INNER JOIN  T_BD_MATERIAL t1 on t.FMATERIALID=t1.FMATERIALID 
                                                                 INNER JOIN  T_STK_INVENTORY t2 on t1.FMASTERID=t2.FMATERIALID AND t.FLOT=t2.FLOT  AND T2.FBASEQTY>0 AND t2.FSTOCKSTATUSID=10000
                                                                 LEFT JOIN  T_BD_LOTMASTER t3 ON t2.FLOT=t3.FLOTID AND T3.FLOTId<>0
                                                                 WHERE  t.FBARCODE='{0}'", scanText);
                }

                DynamicObjectCollection rs = DBServiceHelper.ExecuteDynamicObject(this.Context, strSql);
                if (rs.Count > 0)
                {
                    DynamicObject material = this.View.BillModel.GetValue("FMaterialId", 0) as DynamicObject;
                    if (material == null)
                    {
                        this.View.BillModel.DeleteEntryData("FBillEntry");
                    }
                   
                    for (int i = 0; i < rs.Count; i++)
                    {
                        //this.View.BillModel.CreateNewEntryRow("FBillEntry");
                        this.View.BillModel.InsertEntryRow("FBillEntry", 0);
                        int rowCount = this.View.BillModel.GetEntryRowCount("FBillEntry");
                        this.View.BillModel.SetValue("FSeq", rowCount + 1,0);
                        this.View.BillModel.SetValue("FMaterialId", Convert.ToInt64(rs[i]["FMATERIALID"]), 0);
                        this.View.InvokeFieldUpdateService("FMaterialId", 0);
                        this.View.BillModel.SetValue("FUnitID", Convert.ToInt64(rs[i]["FSTOCKUNITID"]), 0);
                        this.View.BillModel.SetValue("FBaseUnitId", Convert.ToInt64(rs[i]["FBASEUNITID"]), 0);
                        this.View.BillModel.SetValue("FQty", Convert.ToDecimal(rs[i]["FBASEQTY"]), 0);
                        this.View.InvokeFieldUpdateService("FQty", 0);
                        allqty = allqty + Convert.ToDecimal(rs[i]["FBASEQTY"]);
                        this.View.BillModel.SetValue("FBaseQty", Convert.ToDecimal(rs[i]["FBASEQTY"]), 0);
                        this.View.BillModel.SetValue("FLot", rs[i]["FNUMBER"].ToString(), 0);
                        this.View.BillModel.SetValue("FSrcStockId", Convert.ToInt64(rs[i]["FSTOCKID"]), 0);
                        this.View.UpdateView("FMaterialId");
                        this.View.UpdateView("FLot");
                        this.View.UpdateView("F_SBID_MobileProxyEntryEntity");
                    }
                    this.View.GetControl("FAllQty").SetValue(allqty);
                }
                else
                {
                    this.View.ShowStatusBarInfo(ResManager.LoadKDString("扫描条码不正确！", "015747000026624", SubSystemType.MFG, new object[0]));
                    this.View.BillModel.SetValue("FText_BarcodeScan", "");
                    this.View.GetControl("FText_BarcodeScan").SetFocus();
                    return;
                }
            }
        }
        protected virtual void InitFocus()
        {
            if (this.View.BusinessInfo.ContainsKey("FText_BarcodeScan"))
            {
                this.View.GetControl("FText_BarcodeScan").SetFocus();
                this.View.GetControl("FText_BarcodeScan").SetCustomPropertyValue("showKeyboard", true);
            }
        }
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (e.Field.Key == "FQty")
            {
                int rowCount = this.View.BillModel.GetEntryRowCount("FEntity");
                Decimal allqty = 0;
                for (int i = 0; i < rowCount; i++)
                {
                    allqty = allqty + Convert.ToDecimal(this.View.BillModel.GetValue("FQty", i));
                }
                this.View.GetControl("FAllQty").SetValue(allqty);
            }
           
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
                    if (key == "FText_BarcodeScan")
                    {
                        string text = Convert.ToString(e.Value);
                        if (!string.IsNullOrEmpty(text) && !string.IsNullOrWhiteSpace(text))
                        {
                            updateEntry(text);
                            e.Value = string.Empty;
                        }
                    }
                    if (key == "FStockId")
                    {
                        DynamicObject stockId = e.Value as DynamicObject;
                        long _stockId = Convert.ToInt64(stockId["Id"]);
                        int rowCount = this.View.BillModel.GetEntryRowCount("FBillEntry");
                        if (rowCount >1)
                        {
                            for (int i = 0; i < rowCount; i++)
                            {
                                this.View.BillModel.SetValue("FDestStockId", _stockId, i);
                            }
                            this.View.UpdateView("F_SBID_MobileProxyEntryEntity");
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
        public override void BeforeSave(BeforeSaveEventArgs e)
        {
            base.BeforeSave(e);
            this.View.GetControl("FButton_Save").Enabled = false;
           
        }
        public override void AfterSave(AfterSaveEventArgs e)
        {
            base.AfterSave(e);
            List<object> list = new List<object>();
           object  id=View.BillModel.GetPKValue();
            list.Add(id);
            if (e.OperationResult.IsSuccess)
            {
                string strSql = string.Format(@"update T_STK_STKTRANSFERIN set FNote='PAD' where FID={0}", Convert.ToInt64(View.BillModel.GetPKValue()));
                DBServiceHelper.Execute(this.Context, strSql);
                FormMetadata cachedFormMetaData = FormMetaDataCache.GetCachedFormMetaData(base.Context, "STK_TransferDirect");
                OperateOption option = OperateOption.Create();
                option.AddInteractionFlag("Kingdee.K3.SCM.App.Core.AppBusinessService.UpdateStockService,Kingdee.K3.SCM.App.Core");
                option.SetIgnoreInteractionFlag(true);
                BusinessDataServiceHelper.Submit(this.Context, cachedFormMetaData.BusinessInfo, list.ToArray(), "Submit", null);
                //   BusinessDataServiceHelper.Audit(this.Context, cachedFormMetaData.BusinessInfo, list.ToArray(), option);
                this.View.Close();
            }
           
        }

        public override void BeforeF7Select(BeforeF7SelectEventArgs e)
        {
            base.BeforeF7Select(e);
            if (e.FieldKey.EqualsIgnoreCase("FDestStockId"))
            {
                e.ListFilterParameter.Filter = e.ListFilterParameter.Filter.JoinFilterString(" FSTOCKPROPERTY='2' ");
                return;
            }

        }
        }
}
