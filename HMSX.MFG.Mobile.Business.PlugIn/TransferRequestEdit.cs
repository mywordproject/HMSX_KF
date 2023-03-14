using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Mobile.PlugIn;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Resource;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS.Core.Bill.PlugIn.Args;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Core.Interaction;
using Kingdee.BOS.Core.Metadata.EntityElement;

namespace HMSX.MFG.Mobile.Business.PlugIn
{
    [Description("调拨单申请-表单插件")]
    public   class TransferRequestEdit : AbstractMobileBillPlugin
    {
        //调出仓库
        protected long FSrcStockId = 0L;
        Decimal allqty = 0;
        public override void OnInitialize(InitializeEventArgs e)
        {
            base.OnInitialize(e);
            this.View.GetControl("FLable_User").SetValue(this.Context.UserName);
            this.View.GetControl("F_RUJP_MobileProxyEntryEntity").SetCustomPropertyValue("defaultRows", 0);
            this.View.BillModel.BillBusinessInfo.GetEntity("FEntity").DefaultRows = 0;

        }

        private long GetDeptId(long userId)
        {
            long DeptId = 0;
            string sql = string.Format(@" SELECT E.FID,DPL.FDEPTID AS DEPTID,POST.FPOSTID AS POSTID FROM T_SEC_USER U    
                                                          INNER JOIN T_HR_EMPINFO E ON (U.FLINKOBJECT = E.FPERSONID)    
                                                          INNER JOIN T_HR_EMPINFO_L EL ON (EL.FID = E.FID AND EL.FLOCALEID = 2052)    
                                                          INNER JOIN T_BD_STAFF ST ON ST.FEMPINFOID = EL.FID    
                                                          INNER JOIN T_BD_STAFFPOSTINFO STPP ON STPP.FSTAFFID = ST.FSTAFFID    
                                                          INNER JOIN T_BD_DEPARTMENT_L DPL ON DPL.FDEPTID = ST.FDEPTID     
                                                          INNER JOIN T_ORG_POST POST ON POST.FPOSTID = ST.FPOSTID WHERE STPP.FISFIRSTPOST=1
                                                          AND U.FUserId={0}", userId);
            DynamicObjectCollection rs = DBServiceHelper.ExecuteDynamicObject(this.Context, sql);
            if (rs.Count == 1)
            {
                DeptId = Convert.ToInt64(rs[0]["DEPTID"]);
            }
            return DeptId;

        }
        public override void AfterCreateNewData(EventArgs e)
        {
            base.AfterCreateNewData(e);
           
        }

        private long GetStockId(string userName)
        {
            long stockId = 0;
            string strSql = string.Format(@"SELECT T4.FWIPSTOCKID FROM T_SEC_USER U  
                                                                            INNER JOIN T_BD_PERSON T1 ON U.FLINKOBJECT = T1.FPERSONID 
                                                                            INNER JOIN T_BD_STAFF T2 ON T1.FPERSONID=T2.FPERSONID 
                                                                            LEFT JOIN T_BD_DEPARTMENT T4 ON T2.FDEPTID=T4.FDEPTID 
                                                                            WHERE T4.FWIPSTOCKID<>0 AND U.FNAME='{0}'", userName);
            DynamicObjectCollection rs= DBServiceHelper.ExecuteDynamicObject(this.Context, strSql);
            if (rs.Count > 0)
            {
                stockId = Convert.ToInt64(rs[0]["FWIPSTOCKID"]);
            }
            return stockId;
        }
        public override void BeforeBindData(EventArgs e)
        {
            base.BeforeBindData(e);
            string strSql = string.Format(@"select FBILLTYPEID from T_BAS_BILLTYPE where Fnumber='DBSQD_CJDB'");
            DynamicObjectCollection rs= DBServiceHelper.ExecuteDynamicObject(this.Context, strSql);
            this.View.BillModel.SetValue("FBillTypeID", rs[0]["FBILLTYPEID"].ToString());
            this.View.UpdateView("F_RUJP_BillTypeID");

            this.View.BillModel.SetValue("F_220_BaseSQBM", this.GetDeptId(this.Context.UserId));
        }
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            this.View.GetControl("F_RUJP_MobileProxyEntryEntity").SetCustomPropertyValue("listEditable", true);
            int rowcount = this.View.BillModel.GetEntryRowCount("FEntity");
            //this.View.BillModel.DeleteEntryData("FEntity");
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
                        if (scanText.Contains("PGMX"))
                        {
                            string[] scanText1 = scanText.Split('-');
                            if (scanText1.Length > 2)
                            {
                                string scanText2 = scanText1[3] + "-" + scanText1[4];
                                updateEntry(scanText2);
                            }
                            else
                            {
                                updateEntry(scanText);
                            }
                        }
                        else
                        {
                            updateEntry(scanText);
                        }
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
                case "FBUTTON_LOGOUT":
                    LoginUtils.LogOut(base.Context, base.View);
                    base.View.Logoff("indexforpad.aspx");
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
                    strSql = string.Format(@"SELECT T.FMATERIALID,T1.FMASTERID,t2.FLOT,T3.FNUMBER,t2.FSTOCKID,t2.FSTOCKLOCID,t2.FBASEQTY,t2.FSTOCKUNITID,t2.FBASEUNITID,t2.FSTOCKSTATUSID FROM T_PRD_INSTOCKENTRY T 
                                                                        INNER JOIN T_BD_MATERIAL T1 ON T.FMATERIALID=T1.FMATERIALID  
	                                                                    INNER JOIN  T_STK_INVENTORY t2 on t1.FMASTERID=t2.FMATERIALID AND t.FLOT=t2.FLOT  AND T2.FBASEQTY>0  
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
                   
                      DynamicObject  material= this.View.BillModel.GetValue("FMATERIALID", 0) as DynamicObject;
                    if (material == null)
                   {
                       this.View.BillModel.DeleteEntryData("FEntity");
                    }
                   
                    for (int i = 0; i < rs.Count; i++)
                    {
                       // this.View.BillModel.CreateNewEntryRow("FEntity");
                       this.View.BillModel.InsertEntryRow("FEntity", 0);
                        int rowCount = this.View.BillModel.GetEntryRowCount("FEntity");
                        this.View.BillModel.SetValue("FSeq", rowCount + 1, 0);
                        this.View.BillModel.SetValue("FMATERIALID", Convert.ToInt64(rs[i]["FMATERIALID"]), 0);
                        this.View.InvokeFieldUpdateService("FMATERIALID", 0);
                        this.View.BillModel.SetValue("FUNITID", Convert.ToInt64(rs[i]["FSTOCKUNITID"]), 0);
                        this.View.BillModel.SetValue("FBaseUnitID", Convert.ToInt64(rs[i]["FBASEUNITID"]), 0);
                        this.View.BillModel.SetValue("FQty", Convert.ToDecimal(rs[i]["FBASEQTY"]), 0);
                        this.View.InvokeFieldUpdateService("FQty", 0);
                        allqty = allqty + Convert.ToDecimal(rs[i]["FBASEQTY"]);
                       if (!rs[i]["FNUMBER"].Equals("") || rs[i]["FNUMBER"] != null)
                        {
                          this.View.BillModel.SetValue("FLot", rs[i]["FNUMBER"].ToString(), 0);
                       }
                        this.View.BillModel.SetValue("FStockId", Convert.ToInt64(rs[i]["FSTOCKID"]), 0);
                        this.View.BillModel.SetValue("FStockStatusId", Convert.ToInt64(rs[i]["FSTOCKSTATUSID"]), 0);
                        this.View.BillModel.SetValue("FStockStatusInId", Convert.ToInt64(rs[i]["FSTOCKSTATUSID"]), 0);
                        this.View.UpdateView("FMaterialId");
                        this.View.UpdateView("FLot");
                        this.View.UpdateView("F_RUJP_MobileProxyEntryEntity");
                        
                    }
                    this.View.GetControl("FAllQty").SetValue(allqty);
                }
                else
                {
                    this.View.ShowStatusBarInfo(ResManager.LoadKDString("扫描条码不正确！", "015747000026624", SubSystemType.MFG, new object[0]));
                    this.View.Model.SetValue("FText_BarcodeScan", "");
                    this.View.GetControl("FText_BarcodeScan").SetFocus();
                    return;
                }
            }
        }

        private DynamicObjectCollection GetStockInfobyBarCode(string barcode)
        {
            long materialId = 0;
            long lot = 0;
            string lotText = string.Empty;
            string strSql = string.Format(@"SELECT FMATERIALID,FLOT,FLOT_TEXT  FROM T_BD_BARCODEMAIN WHERE FBARCODE='{0}'", barcode);
            DynamicObjectCollection rs= DBServiceHelper.ExecuteDynamicObject(this.Context, strSql);
            if (rs.Count > 0)
            {
                materialId = Convert.ToInt64(rs[0]["FMATERIALID"]);
                lot= Convert.ToInt64(rs[0]["FLOT"]);
                if (rs[0]["FLOT_TEXT"] != null)
                {
                    lotText = rs[0]["FLOT_TEXT"].ToString();
                } 
            }
            return rs;

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
                        if (text.Contains("PGMX"))
                        {
                            string[] text1 = text.Split('-');
                            if (text1.Length > 2)
                            {
                                string scanText2 = text1[3] + "-" + text1[4];
                                updateEntry(scanText2);
                                e.Value = string.Empty;
                            }
                            else
                            {
                                updateEntry(text);
                                e.Value = string.Empty;
                            }
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(text) && !string.IsNullOrWhiteSpace(text))
                            {
                                updateEntry(text);
                                e.Value = string.Empty;
                            }
                        }
                    }
                    if (key == "F_StockId")
                    {
                        DynamicObject stockId = e.Value as DynamicObject;
                        long _stockId = Convert.ToInt64(stockId["Id"]);
                        int rowCount = this.View.BillModel.GetEntryRowCount("FEntity");
                        if (rowCount > 1)
                        {
                            for (int i = 0; i < rowCount; i++)
                            {
                                this.View.BillModel.SetValue("FStockInId", _stockId, i);
                            }
                            this.View.UpdateView("F_RUJP_MobileProxyEntryEntity");
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
            object id = View.BillModel.GetPKValue();
            list.Add(id);
            if (e.OperationResult.IsSuccess)
            {
                string strSql = string.Format(@"update T_STK_STKTRANSFERAPP set FDESCRIPTION='PAD' where FID={0}", Convert.ToInt64(View.BillModel.GetPKValue()));
                DBServiceHelper.Execute(this.Context, strSql);
                FormMetadata cachedFormMetaData = FormMetaDataCache.GetCachedFormMetaData(base.Context, "STK_TRANSFERAPPLY");
                OperateOption option = OperateOption.Create();
                option.AddInteractionFlag("Kingdee.K3.SCM.App.Core.AppBusinessService.UpdateStockService,Kingdee.K3.SCM.App.Core");
                option.SetIgnoreInteractionFlag(true);
                BusinessDataServiceHelper.Submit(this.Context, cachedFormMetaData.BusinessInfo, list.ToArray(), "Submit", null);
                BusinessDataServiceHelper.Audit(this.Context, cachedFormMetaData.BusinessInfo, list.ToArray(), option);
            }
            this.View.Close();
        }
    }
}
