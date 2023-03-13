using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.Complex;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.Utils;
using Kingdee.K3.MFG.ServiceHelper.SFS;
using Kingdee.BOS.Core;
using Kingdee.BOS.Mobile.PlugIn;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Mobile;
using Kingdee.BOS.Mobile.PlugIn.ControlModel;
using Kingdee.BOS.Resource;
using Kingdee.BOS.Mobile.PlugIn.Args;
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.ComplexCacheJson.Utils;
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.ComplexCacheJson.Model;
using Kingdee.BOS.JSON;
using Kingdee.BOS.Util;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.App.Data;

namespace HMSX.MFG.Mobile.Business.PlugIn
{
    [Description("H领料单-表单插件")]
    [Kingdee.BOS.Util.HotUpdate]
    public class ComplexDispatchList : AbstractMobilePlugin

    {
        protected int CurrPageNumber;//当前页
        protected int TotalPageNumber;//总页数
        protected MobileListFormaterManager ListFormaterManager = new MobileListFormaterManager();
        protected FormCacheModel cacheModel4Save = new FormCacheModel();
        protected bool HasCached;
        protected string CurrOptPlanScanCode { get; private set; }
        protected string TerminalNumber;
        
        List<long> list = new List<long>();
        Dictionary<long, long> dic = new Dictionary<long, long>();
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
            this.CurrOptPlanScanCode= Convert.ToString(e.Paramter.GetCustomParameter("CurrScanCode"));
        }

        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
             this.View.Model.SetValue("FText_OptPlanNumberScan", "");
             this.View.GetControl("FText_OptPlanNumberScan").SetFocus();
             this.View.GetControl("FText_OptPlanNumberScan").SetCustomPropertyValue("showKeyboard", true);
            //this.InitFocus();
             this.View.UpdateView("FText_OptPlanNumberScan");
             this.View.GetControl("FMobileListViewEntity").SetCustomPropertyValue("listEditable", true);
          
          
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
            this.View.Model.SetValue("FSelect", true, e.Row - 1);
             this.View.UpdateView("FSelect", e.Row - 1);
            //this.SetHighLight();
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
                    this.View.Model.SetValue("FSelect", true, i - 1);
                    this.View.UpdateView("FSelect", i- 1);
                }
                else
                {
                    this.ListFormaterManager.SetControlProperty("FFlowLayout_Row", i - 1, "255,255,255", MobileFormatConditionPropertyEnums.BackColor);
                    this.View.Model.SetValue("FSelect", false, i - 1);
                    this.View.UpdateView("FSelect", i - 1);
                }
            }
            base.View.GetControl<MobileListViewControl>("FMobileListViewEntity").SetFormat(this.ListFormaterManager);
            base.View.UpdateView("FMobileListViewEntity");
        }
        /// <summary>
        /// 扫码数据改变
        /// </summary>
        /// <param name="e"></param>
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (e.Field.Key.Equals("FSelect"))
            {
                if (e.NewValue.Equals(true))
                {
                    this.View.GetControl<MobileListViewControl>("FMobileListViewEntity").SetSelectRows(new int[] { e.Row});
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
                    string[] scanText1 = this.View.Model.GetValue("FText_OptPlanNumberScan").ToString().Split('-');
                    if (scanText1.Length >2)
                    {
                        string scanText= scanText1[0] + "-" + scanText1[1] + "-" + scanText1[2];
                        FillAllData(scanText);
                        this.View.Model.SetValue("FText_OptPlanNumberScan", "");
                        this.View.UpdateView("FText_OptPlanNumberScan");
                        this.View.GetControl("FText_OptPlanNumberScan").SetFocus();
                        this.View.GetControl("FText_OptPlanNumberScan").SetCustomPropertyValue("showKeyboard", true);

                    }
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

                case "FBUTTON_CONFIRM":

                    PickMaterial();
                    return;
                    //上一页
                case "FBUTTON_PREVIOUS":
                 //   this.TurnPaga(false);
                    return;
                    //下一页
                case "FBUTTON_NEXT":
                //    this.TurnPaga(true);
                    return;
                case "F_CANCELSELECTED":
                    this.CancelBatchSelect();
                    return;
                case "F_BATCHSELECTED":
                    this.BatchSelect();
                    return;
                case "F_GYSLR":
                    GYSLR();
                    return;
                case "F_HBLL":
                    PickMaterial1();
                    return;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void FillAllData(string  ScanText)
        {
            if (ScanText != "")
            {
               
                string strSql = string.Format(@"/*dialect*/select FMoBillNo,FMOSEQ,concat(FMoBillNo,'-',FMOSEQ) as FMoNumber,FOptPlanNo,t3.FName as FProcess,FOperNumber,FSEQNUMBER, 
                                                                concat(FOptPlanNo,'-',FSEQNUMBER,'-',FOperNumber) as OptPlanNo,t.FMaterialId,t2.FNAME as FMaterialName,t1.F_LOT_Text,t1.FWORKQTY,t1.FEntryId,t1.FBARCODE  
                                                                from T_SFC_DISPATCHDETAIL t inner join T_SFC_DISPATCHDETAILENTRY t1 
                                                                on t.FID=t1.FID and t.FOperId=(select FDETAILID from T_SFC_OPERPLANNINGDETAIL where FBarCode='{0}')   
                                                                 AND t1.FENTRYID NOT IN  (select  FPgEntryId from(select FPgEntryId,sum(FMustQty) as FMustQty,sum(FAvailableQty) as FPickQty  from t_PgBomInfo Group by FPgEntryId) a where a.FMustQty-a.FPickQty<=0) 
                                                               AND T1.FSTATUS='B' 
                                                                left join T_BD_MATERIAL_L t2 on t.FMATERIALID = t2.FMATERIALID and t2.FLOCALEID = 2052  
                                                               left join T_ENG_PROCESS_L t3 on t.FPROCESSID=t3.FID and t3.FLOCALEID = 2052", ScanText);
                DynamicObjectCollection rs = DBServiceHelper.ExecuteDynamicObject(this.Context, strSql);
                if (rs.Count > 0)
                {
                    this.View.Model.DeleteEntryData("FMobileListViewEntity");
                    Decimal allqty = 0;
                    for (int i = 0; i < rs.Count; i++)
                    {
                        this.View.Model.CreateNewEntryRow("FMobileListViewEntity");
                        int rowCount = this.View.Model.GetEntryRowCount("FMobileListViewEntity");
                        int Seq = i + 1;
                        this.View.Model.SetValue("FSeq", Seq, i);
                        this.View.Model.SetValue("FMONumber", rs[i]["FMoNumber"].ToString(), i);
                        this.View.Model.SetValue("FOperPlanNo", rs[i]["OptPlanNo"].ToString(), i);
                        if (rs[i]["FProcess"] != null)
                        {
                            this.View.Model.SetValue("FProcessId", rs[i]["FProcess"].ToString(), i);
                        }
                        this.View.Model.SetValue("FPgBarCode", rs[i]["FBARCODE"].ToString(), i);
                        this.View.Model.SetValue("FProductId", rs[i]["FMaterialName"].ToString(), i);
                        this.View.Model.SetValue("FLot", rs[i]["F_LOT_Text"].ToString(), i);
                        this.View.Model.SetValue("FQty", rs[i]["FWORKQTY"].ToString(), i);
                        allqty = allqty + Convert.ToDecimal(rs[i]["FWORKQTY"]);
                        this.View.Model.SetValue("FPgEntryId", rs[i]["FEntryId"].ToString(), i);
                        this.View.UpdateView("FMobileListViewEntity");
                    }
                    this.View.Model.SetValue("FAllQty", allqty);
                    this.View.UpdateView("FAllQty");
                }
            }
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
                    if (Convert.ToBoolean(this.View.Model.GetValue("FSelect", row)))
                    {
                        rowIndex = rowIndex + 1;
                    }
                }

                if (rowIndex == 0)
                {
                    this.View.ShowStatusBarInfo(ResManager.LoadKDString("请选择要领料的行！", "015747000026624", SubSystemType.MFG, new object[0]));
                    return;
                }
                else
                {
                    dic.Clear();
                    for (int i=0;i< rowcount; i++)
                    {                      
                        if (Convert.ToBoolean( this.View.Model.GetValue("FSelect", i)))
                        {
                            entryId = entryId + ',' + this.View.Model.GetValue("FPgEntryId", i).ToString();
                            dic.Add(Convert.ToInt64(this.View.Model.GetValue("FPgEntryId", i).ToString()), Convert.ToInt64(this.View.Model.GetValue("FPgEntryId", i).ToString()));
                            list.Add(Convert.ToInt64(this.View.Model.GetValue("FPgEntryId", i)));
                        }
                       
                    }

                }
                SavePgBom();
                MobileShowParameter param = new MobileShowParameter();
                param.FormId = "kcda126f86b6f4754a6d58570ca2221e3";
                param.ParentPageId = this.View.PageId;
                param.SyncCallBackAction = false;
               // param.CustomComplexParams["PgEntryIds"] = list;
                param.CustomParams.Add("FPgEntryId", entryId);
                this.ShowFrom(param);
            }
        }
        public void PickMaterial1()
        {
            int rowIndex = 0;
            int rowcount = this.View.Model.GetEntryRowCount("FMobileListViewEntity");
            if (rowcount > 0)
            {
                string entryId = "0";
                for (int row = 0; row < rowcount; row++)
                {
                    if (Convert.ToBoolean(this.View.Model.GetValue("FSelect", row)))
                    {
                        rowIndex = rowIndex + 1;
                    }
                }

                if (rowIndex == 0)
                {
                    this.View.ShowStatusBarInfo(ResManager.LoadKDString("请选择要领料的行！", "015747000026624", SubSystemType.MFG, new object[0]));
                    return;
                }
                else if (rowIndex == 1)
                {
                    this.View.ShowStatusBarInfo(ResManager.LoadKDString("请选择两条或两条以上要领料的行！", "015747000026624", SubSystemType.MFG, new object[0]));
                    return;
                }
                else if(rowIndex >1)
                {
                    dic.Clear();
                    for (int i = 0; i < rowcount; i++)
                    {
                        if (Convert.ToBoolean(this.View.Model.GetValue("FSelect", i)))
                        {
                            entryId = entryId + ',' + this.View.Model.GetValue("FPgEntryId", i).ToString();
                            dic.Add(Convert.ToInt64(this.View.Model.GetValue("FPgEntryId", i).ToString()), Convert.ToInt64(this.View.Model.GetValue("FPgEntryId", i).ToString()));
                            list.Add(Convert.ToInt64(this.View.Model.GetValue("FPgEntryId", i)));
                        }

                    }

                }
                SavePgBom();
                MobileShowParameter param = new MobileShowParameter();
                param.FormId = "keed_HBLLSM";
                param.ParentPageId = this.View.PageId;
                param.SyncCallBackAction = false;
                // param.CustomComplexParams["PgEntryIds"] = list;
                param.CustomParams.Add("FPgEntryId", entryId);
                this.ShowFrom(param);
            }
        }

        /// <summary>
        /// 打开领料扫码
        /// </summary>
        private void ShowFrom(MobileShowParameter param)
        {
           
            this.View.ShowForm(param, new Action<FormResult>((res) => {
               // FillAllData(param.CustomParams[]);
                this.View.Refresh();
            }));
        }

        public override void BeforeUpdateValue(BeforeUpdateValueEventArgs e)
        {
            base.BeforeUpdateValue(e);
            this.ScanCodeChanged(e);
            
        }
        private  void ScanCodeChanged(BeforeUpdateValueEventArgs e)
        {
            // base.ClearDicFilterValues();
            try
            {
                string key;
                if ((key = e.Key) != null)
                {
                    if (key == "FText_OptPlanNumberScan")
                    {
                        string[] text1 = Convert.ToString(e.Value).Split('-');
                        if (text1.Length > 2)
                        {
                            string text= text1[0] + "-" + text1[1] + "-" + text1[2];
                            if (!string.IsNullOrEmpty(text) && !string.IsNullOrWhiteSpace(text))
                            {
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
        public override void AfterGetClientCache(MobileClientCacheLoadedEventArgs args)
        {
            base.AfterGetClientCache(args);
            if (args.Key == "DeviceCode")
            {
                if (!(args.Value is JSONObject) || string.IsNullOrWhiteSpace(Convert.ToString(((JSONObject)args.Value).Get("DeviceCode"))))
                {
                    Guid guid = Guid.NewGuid();
                    base.View.SetClientCache("DeviceCode", JsonCacheUtils.SetDeviceCode(guid.ToString()), null);
                    return;
                }
                object value = args.Value;
                this.TerminalNumber = Convert.ToString((args.Value as JSONObject).Get("TerminalNumber"));
            }
        }

        public void SavePgBom( )
        {
            string MoBillNo = "";//生产订单号
            string MoBillEntrySeq = "";//生产订明细行号
            foreach (var entryId in dic)
            {
                string strSql = string.Format(@"SELECT T.FMOBILLNO,T.FMOSEQ,T1.FWORKQTY FROM T_SFC_DISPATCHDETAIL T INNER JOIN T_SFC_DISPATCHDETAILENTRY T1 ON T.FID=T1.FID AND T1.FENTRYID IN({0})", entryId.Key);
                DynamicObjectCollection rs = DBServiceHelper.ExecuteDynamicObject(this.Context, strSql);
                if (rs.Count > 0)
                {
                    for (int i = 0; i < rs.Count; i++)
                    {
                        MoBillNo = rs[i]["FMOBILLNO"].ToString();
                        MoBillEntrySeq = rs[i]["FMOSEQ"].ToString();
                        List<DynamicObject> PPBomInfo = this.GetPPBomInfo(MoBillNo, MoBillEntrySeq);
                        foreach (DynamicObject obj in PPBomInfo)
                        {
                            Decimal mustQty = Convert.ToDecimal(obj["FNUMERATOR"]) / Convert.ToDecimal(obj["FDENOMINATOR"]) * Convert.ToDecimal(rs[i]["FWORKQTY"]) * (Convert.ToDecimal(obj["FUSERATE"]) / 100);
                            DynamicObjectCollection rsentrys = DBServiceHelper.ExecuteDynamicObject(this.Context,string.Format("select * from t_PgBomInfo where FPgEntryId={0} AND FPPBomEntryId={1}", entryId.Key, Convert.ToInt64(obj["FENTRYID"])));
                            if (rsentrys.Count== 0)                         
                            {
                                string Sql = string.Format(@" INSERT INTO t_PgBomInfo(FPgEntryId,FPPBomId,FPPBomEntryId,FMaterialId,FPgQty,FMustQty,FPickQty,FReturnQty,FFeedQty,FAllPickQty,FAvailableQty)
                       Values({0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10})", entryId.Key, Convert.ToInt64(obj["FID"]), Convert.ToInt64(obj["FENTRYID"]), Convert.ToInt64(obj["FMATERIALID"]), Convert.ToDecimal(rs[i]["FWORKQTY"]), mustQty,0,0,0,0,0);
                                int row = DBServiceHelper.Execute(this.Context, Sql);
                            }
                           //else
                           //{
                           //    string upsql = $@"update t_PgBomInfo set GYS={entryId.Value} WHERE FPgEntryId={entryId.Key} AND FPPBomEntryId={Convert.ToInt64(obj["FENTRYID"])}";
                           //    DBUtils.Execute(Context, upsql);
                           //}
                        }
                    }
                }
            }
        }
        private List<DynamicObject> GetPPBomInfo(string MoBillNo, string MoBillEntrySeq)
        {
            string strSql = string.Format(@"SELECT T.FPRDORGID,T.FMOBillNO,T.FMOENTRYSEQ,T1.FSEQ,T1.FID,T1.FENTRYID,T1.FMATERIALID,T3.FMASTERID,T3.FNUMBER,T4.FNAME,T4.FSPECIFICATION,T2.FPICKEDQTY,T5.FSTOCKID,T1.FNUMERATOR,T1.FDENOMINATOR,T1.FSCRAPRATE,FUSERATE  FROM T_PRD_PPBOM T 
                                                             INNER JOIN T_PRD_PPBOMENTRY T1 ON T.FID=T1.FID 
                                                             INNER JOIN T_PRD_PPBOMENTRY_Q T2 ON T1.FID=T2.FID AND T1.FENTRYID=T2.FENTRYID  AND( T1.FMUSTQTY>(T2.FPICKEDQTY-t2.FGOODRETURNQTY) or FMUSTQTY=0 and FUSERATE=0)
                                                             INNER JOIN T_PRD_PPBOMENTRY_C T5 ON T1.FID=T5.FID AND T1.FENTRYID=T5.FENTRYID
                                                             INNER JOIN T_BD_MATERIAL T3 ON T1.FMATERIALID=T3.FMATERIALID  AND T3.FMATERIALID NOT IN (SELECT FMATERIALID FROM T_BD_MATERIALBASE WHERE FErpClsID=5 )
                                                             INNER JOIN T_BD_MATERIAL_L T4 ON T1.FMATERIALID=T4.FMATERIALID AND T4.FLOCALEID=2052
                                                             WHERE T.FMOBillNO='{0}' AND T.FMOENTRYSEQ={1} AND T5.FISSUETYPE IN ('1','3')", MoBillNo, MoBillEntrySeq);
            DynamicObjectCollection source = DBServiceHelper.ExecuteDynamicObject(base.Context, strSql);
            return source.ToList<DynamicObject>();
        }

        public void BatchSelect()
        {
            int rowCount = this.View.Model.GetEntryRowCount("FMobileListViewEntity");
            for (int i = 0; i < rowCount; i++)
            {
                this.View.Model.SetValue("FSelect", true, i);
                this.View.UpdateView("FSelect", i);
            }
        }

        public void CancelBatchSelect()
        {
            int rowCount = this.View.Model.GetEntryRowCount("FMobileListViewEntity");
            for (int i = 0; i < rowCount; i++)
            {
                this.View.Model.SetValue("FSelect", false, i);
                this.View.UpdateView("FSelect", i);
            }
        }
        public void GYSLR()
        {
            int rowIndex = 0;
            int rowcount = this.View.Model.GetEntryRowCount("FMobileListViewEntity");
            if (rowcount > 0)
            {
                string entryId = "0";
                for (int row = 0; row < rowcount; row++)
                {
                    if (Convert.ToBoolean(this.View.Model.GetValue("FSelect", row)))
                    {
                        rowIndex = rowIndex + 1;
                    }
                }
                if (rowIndex == 0)
                {
                    this.View.ShowStatusBarInfo(ResManager.LoadKDString("请选择要录入供应商的行！", "015747000026624", SubSystemType.MFG, new object[0]));
                    return;
                }
                else
                {
                    dic.Clear();
                    for (int i = 0; i < rowcount; i++)
                    {              
                        if (Convert.ToBoolean(this.View.Model.GetValue("FSelect", i)))
                        {
                            entryId = entryId + ',' + this.View.Model.GetValue("FPgEntryId", i).ToString();
                            dic.Add(Convert.ToInt64(this.View.Model.GetValue("FPgEntryId", i).ToString()), Convert.ToInt64(this.View.Model.GetValue("FPgEntryId", i).ToString()));
                            list.Add(Convert.ToInt64(this.View.Model.GetValue("FPgEntryId", i)));
                        }
                    }
                }
                SavePgBom();
                MobileShowParameter param = new MobileShowParameter();
                param.FormId = "keed_GYSLR";
                param.ParentPageId = this.View.PageId;
                param.SyncCallBackAction = false;
                param.CustomParams.Add("FPgEntryId", entryId.Trim(','));
                this.ShowFrom(param);
            }
        }
    }
}
