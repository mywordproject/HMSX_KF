using Kingdee.BOS.Mobile.PlugIn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.K3.Core.MFG.SFS.ParamOption;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Util;
using Kingdee.BOS.Resource;
using Kingdee.K3.MFG.ServiceHelper.SFS;
using Kingdee.BOS.Mobile.PlugIn.ControlModel;
using Kingdee.BOS.Core.Metadata.EntityElement;
using System.ComponentModel;
using Kingdee.K3.MFG.ServiceHelper;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.DynamicForm.Operation;
using Kingdee.BOS.Core.Metadata.ConvertElement.ServiceArgs;
using Kingdee.BOS.Orm;
using Kingdee.K3.MFG.Mobile.ServiceHelper;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Interaction;
using Kingdee.K3.MFG.SFS.Common.Core.ParamValue;

namespace HMSX.MFG.Mobile.Business.PlugIn
{
    [Description("退料编辑-表单插件")]
    [Kingdee.BOS.Util.HotUpdate]
    public class ReturnListEdit : AbstractMobilePlugin
    {
        string DispatchDetailEntryId;//派工明细Id
        DynamicObjectCollection ppBomInfo;
        public override void OnInitialize(InitializeEventArgs e)
        {
            base.OnInitialize(e);
            DispatchDetailEntryId = e.Paramter.GetCustomParameter("FPgEntryId").ToString();
            this.View.GetControl("FLable_User").SetValue(this.Context.UserName);
            //获取已领料数据
            string strSql = string.Format(@"SELECT T1.FPRDORGID,T1.FMOBillNO,T1.FMOENTRYSEQ,T2.FSEQ,T2.FID,T2.FENTRYID,T2.FMATERIALID,T4.FNUMBER,T5.FNAME,T5.FSPECIFICATION,T2.FBASEUNITID,T3.FSTOCKID,T.FAvailableQty AS FPickQty,T.FPgEntryId,PA.FACTUALQTY,PA.FLOT,PA.FLOT_TEXT FROM t_PgBomInfo T
                                                                INNER JOIN T_PRD_PPBOM T1 ON T.FPPBomId = T1.FID
                                                                INNER JOIN T_PRD_PPBOMENTRY T2 ON T.FPPBomEntryId = T2.FENTRYID
                                                                INNER JOIN T_PRD_PPBOMENTRY_C T3 ON T.FPPBomId = T3.FID AND T.FPPBomEntryId = T3.FENTRYID AND T3.FISSUETYPE IN ('1', '3')
                                                                INNER JOIN T_PRD_PICKMTRLDATA PA ON T.FPPBomEntryId = PA.FPPBOMENTRYID AND T.FPgEntryId = PA.F_RUJP_PGENTRYID
                                                                INNER JOIN T_BD_MATERIAL T4 ON T.FMATERIALID = T4.FMATERIALID
                                                                INNER JOIN T_BD_MATERIAL_L T5 ON T.FMATERIALID = T5.FMATERIALID AND T5.FLOCALEID = 2052
                                                                WHERE T.FPgEntryId IN ({0})", DispatchDetailEntryId);
            ppBomInfo = DBServiceHelper.ExecuteDynamicObject(this.Context, strSql);
        }

        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            this.View.GetControl("F_SBID_MobileListViewEntity").SetCustomPropertyValue("listEditable", true);
            this.InitFocus();
        }

        protected virtual void InitFocus()
        {
            if (this.View.BusinessInfo.ContainsKey("FText_MaterialNumberScan"))
            {
                this.View.GetControl("FText_MaterialNumberScan").SetFocus();
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
                    if (key == "FText_MaterialNumberScan")
                    {
                        string text = Convert.ToString(e.Value);
                        if (text.Contains("PGMX"))
                        {
                            string[] scanText1 = text.Split('-');
                            if (scanText1.Length > 2)
                            {
                                string scanText2 = scanText1[3] + "-" + scanText1[4];
                                UpdateEntry(scanText2);
                                e.Value = string.Empty;
                            }
                            else
                            {
                                UpdateEntry(text);
                                e.Value = string.Empty;
                            }
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(text) && !string.IsNullOrWhiteSpace(text))
                            {
                                UpdateEntry(text);
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
        public override void ButtonClick(ButtonClickEventArgs e)
        {
            base.ButtonClick(e);
            string key;
            switch (key = e.Key.ToUpper())
            {
                case "FBUTTON_MATERIALNUMBERSCAN":
                    string scanText = this.View.Model.GetValue("FText_MaterialNumberScan").ToString();
                    if (scanText.Contains("PGMX"))
                    {
                        string[] scanText1 = scanText.Split('-');
                        if (scanText1.Length > 2)
                        {
                            string scanText2 = scanText1[3] + "-" + scanText1[4];
                            this.UpdateEntry(scanText2);
                        }
                        else
                        {
                            this.UpdateEntry(scanText);
                        }
                    }
                    else
                    {
                        this.UpdateEntry(scanText);
                    }                    
                    return;
                case "FBUTTON_RETURN":
                    this.View.Close();
                    return;
                case "FSUBMIT":
                    this.Confirm();
                    return;
            }
        }
        public void FillDate(DynamicObjectCollection ppBomInfo)
        {

            if (ppBomInfo.Count > 0)
            {
                for (int i = 0; i < ppBomInfo.Count; i++)
                {
                    this.View.Model.CreateNewEntryRow("F_SBID_MobileListViewEntity");
                    int rowCount = this.View.Model.GetEntryRowCount("F_SBID_MobileListViewEntity");
                    int Seq = i+1 ;
                    this.View.Model.SetValue("FSeq", Seq, i);
                    this.View.Model.SetValue("FMONumber", ppBomInfo[i]["FMOBillNO"].ToString(), i);
                    this.View.Model.SetValue("FMaterialNumber", ppBomInfo[i]["FNUMBER"].ToString(), i);
                    this.View.Model.SetValue("FMaterialName", ppBomInfo[i]["FNAME"].ToString(), i);
                    this.View.Model.SetValue("FLot", ppBomInfo[i]["FLOT_TEXT"].ToString(), i);
                    this.View.Model.SetValue("FMustQty", ppBomInfo[i]["FPickQty"].ToString(), i);
                    this.View.Model.SetValue("FQty", ppBomInfo[i]["FPickQty"].ToString(), i);
                    this.View.Model.SetValue("FBaseUnitID", ppBomInfo[i]["FBASEUNITID"], i);
                    this.View.Model.SetValue("FPpBomEntryId", ppBomInfo[i]["FENTRYID"].ToString(), i);
                    this.View.Model.SetValue("FStockId", ppBomInfo[i]["FSTOCKID"].ToString(), i);
                    this.View.Model.SetValue("FOrgId", ppBomInfo[i]["FPRDORGID"].ToString(), i);
                    //  this.View.Model.SetValue("FQty", rs[i]["Fqty"].ToString(), i);

                    this.View.UpdateView("F_SBID_MobileListViewEntity");
                }
            }
        }

        public void UpdateEntry(string scanText)
        {
            //根据条码获取物料信息
            string materialNumber = "";
            string lot_txt = "";
            List<ReturnInfo> listReturninfo = new List<ReturnInfo>();
            if (scanText != "")
            {
                string strSql = "";
                if (scanText.Substring(0, 2) == "PG")
                {
                    strSql = string.Format(@"SELECT T.FMATERIALID,T1.FNUMBER,T.FLOT,T.FLOT_TEXT FROM T_SFC_OPTRPTENTRY T INNER JOIN T_BD_MATERIAL T1 ON T.FMATERIALID=T1.FMATERIALID  WHERE F_SBID_BARCODE='{0}'", scanText);
                }
                else
                {
                    strSql = string.Format("SELECT T.FMATERIALID,T1.FNUMBER,T.FLOT,T.FLOT_TEXT FROM T_BD_BARCODEMAIN T INNER JOIN T_BD_MATERIAL  T1 ON T.FMATERIALID=T1.FMATERIALID  WHERE FBARCODE='{0}'", scanText);
                }
                //string strSql = string.Format(@"SELECT t.FMATERIALID,t1.FNUMBER,t.FLOT,t.FLOT_TEXT FROM T_BD_BARCODEMAIN t INNER JOIN T_BD_MATERIAL t1 ON t.FMATERIALID=t1.FMATERIALID WHERE FBARCODE='{0}'", scanText);
                DynamicObjectCollection rs = DBServiceHelper.ExecuteDynamicObject(this.Context, strSql);
                if (rs.Count > 0)
                {
                    materialNumber = rs[0]["FNUMBER"].ToString();
                    lot_txt = rs[0]["FLOT_TEXT"].ToString();
                }
                foreach (DynamicObject obj in ppBomInfo)
                {
                    if (obj["FNUMBER"].ToString() == materialNumber && obj["FLOT_TEXT"].ToString() == lot_txt)
                    {
                        ReturnInfo returninfo = new ReturnInfo();
                        returninfo.MONumber = obj["FMOBillNO"].ToString();
                        returninfo.MaterialNumber = obj["FNUMBER"].ToString();
                        returninfo.MaterialName = obj["FNAME"].ToString();
                        returninfo.Model= obj["FSPECIFICATION"].ToString();
                        returninfo.lot = obj["FLOT_TEXT"].ToString();
                        returninfo.PickQty = Convert.ToDecimal(obj["FPickQty"]);
                        returninfo.baseUnitId= Convert.ToInt64(obj["FBASEUNITID"]);
                        returninfo.stockId = Convert.ToInt64(obj["FSTOCKID"]);
                        returninfo.OrgId = Convert.ToInt64(obj["FPRDORGID"]);
                        returninfo.pbomEntryId= Convert.ToInt64(obj["FENTRYID"]);
                        returninfo.pgEntryId= Convert.ToInt64(obj["FPgEntryId"]);
                        listReturninfo.Add(returninfo);
                    }
                }

                if (listReturninfo != null)
                {
                    //this.View.Model.DeleteEntryData("F_SBID_MobileListViewEntity");
                    for (int i = 0; i < listReturninfo.Count; i++)
                    {
                        this.View.Model.CreateNewEntryRow("F_SBID_MobileListViewEntity");
                        int rowCount = this.View.Model.GetEntryRowCount("F_SBID_MobileListViewEntity");
                        int Seq = rowCount;
                        this.View.Model.SetValue("FSeq", Seq, rowCount-1);
                        this.View.Model.SetValue("FMONumber", listReturninfo[i].MONumber, rowCount - 1);
                        this.View.Model.SetValue("FMaterialNumber", listReturninfo[i].MaterialNumber, rowCount - 1);
                        this.View.Model.SetValue("FMaterialName", listReturninfo[i].MaterialName, rowCount - 1);
                        this.View.Model.SetValue("FModel", listReturninfo[i].Model, rowCount - 1);
                        this.View.Model.SetValue("FLot", listReturninfo[i].lot, rowCount - 1);
                        this.View.Model.SetValue("FMustQty", listReturninfo[i].PickQty, rowCount - 1);
                        this.View.Model.SetValue("FQty", listReturninfo[i].PickQty, rowCount - 1); 
                        this.View.Model.SetValue("FBaseUnitID", listReturninfo[i].baseUnitId, rowCount - 1);
                        this.View.Model.SetValue("FPpBomEntryId", listReturninfo[i].pbomEntryId, rowCount - 1);
                        this.View.Model.SetValue("FPgEntryId", listReturninfo[i].pgEntryId, rowCount - 1);
                        this.View.Model.SetValue("FStockId", listReturninfo[i].stockId, rowCount - 1);
                        this.View.Model.SetValue("FOrgId", listReturninfo[i].OrgId, rowCount - 1); 
                        this.View.UpdateView("F_SBID_MobileListViewEntity");
                    }
                }
            }
        }

        public void Confirm()
        {
          
            Entity entity = this.Model.BusinessInfo.GetEntity("F_SBID_MobileListViewEntity");
            DynamicObjectCollection rows = this.View.Model.GetEntityDataObject(entity);
            foreach (DynamicObject rowData in rows)
            {
                if (Convert.ToDecimal(rowData["FQty"]) > 0)
                {
                    IOperationResult result = this.CreatePickMtrl(rowData);
                    this.HandleResult(result);
                }
            }
        }
        protected virtual IOperationResult CreatePickMtrl(DynamicObject rowData)
        {
            string strsql = string.Format("SELECT T.FID,T.FBILLNO,T1.FENTRYID,T1.FSEQ,AA.FMTONO FROM T_PRD_PPBOM T " +
                "INNER JOIN  T_PRD_PPBOMENTRY T1 ON T.FID=T1.FID AND T1.FENTRYID={0}" +
                "left JOIN (select DD.FMTONO,DD.FID,DD.FSEQ from T_PRD_MOENTRY DD " +
                           "inner JOIN T_PRD_MO CC ON DD.FID = CC.FID and FBILLTYPE = '0e74146732c24bec90178b6fe16a2d1c')AA " +
                           " ON AA.FID=T.FMOID AND AA.FSEQ=T.FMOENTRYSEQ", Convert.ToInt64(rowData["FPpBomEntryId"]));
            DynamicObjectCollection rs= DBServiceHelper.ExecuteDynamicObject(this.Context, strsql);
            List<SFSCreateReturnMtrlParam> listreturn = new List<SFSCreateReturnMtrlParam>();
            SFSCreateReturnMtrlParam returninfo = new SFSCreateReturnMtrlParam()
            {
                Lot_Text = rowData["FLot"].ToString(),
                PPBomBillNo = rs[0]["FBILLNO"].ToString(),
                PPBomEntrySeq = Convert.ToInt32(rs[0]["FSEQ"]) - 1,
                StockId= Convert.ToInt64(rowData["FStockId"]),
                Qty = Convert.ToDecimal(rowData["FQty"]),
                WorkCenterId = Convert.ToInt64(rowData["FOrgId"]), 
            };
            listreturn.Add(returninfo);
            List<ListSelectedRow> list = new List<ListSelectedRow>();
                ListSelectedRow item = new ListSelectedRow(Convert.ToString(rs[0]["FID"]), Convert.ToString(rs[0]["FENTRYID"]), Convert.ToInt32(rs[0]["FSEQ"]) - 1, "PRD_PPBOM")
                {
                    EntryEntityKey = "FEntity"

                };
                list.Add(item);
            if (list.Count == 0)
            {
                base.View.ShowMessage(ResManager.LoadKDString("没有需要领料的分录！", "015747000026506", SubSystemType.MFG, new object[0]), MessageBoxType.Notice);
                return null;
            }
            ConvertOperationResult convertOperationResult;
            string convertRuleId = "PRD_PPBOM2RETURNMTRL_LOT"; //
            var ruleMeta = ConvertServiceHelper.GetConvertRule(this.Context, convertRuleId);
            var rule = ruleMeta.Rule;
            PushArgs args = new PushArgs(rule, list.ToArray())
            {
                TargetBillTypeId = "c4e4cef46c844a2bb2a7faf0cf6dc2cb",//普通退料
            };

            OperateOption operateOption = OperateOption.Create();
            operateOption.AddInteractionFlag("Kingdee.K3.SCM.App.Core.AppBusinessService.UpdateStockService,Kingdee.K3.SCM.App.Core");
            operateOption.SetIgnoreInteractionFlag(true);
            operateOption.SetVariableValue("LstParam", listreturn);
            operateOption.SetVariableValue("DicLot",null);
           convertOperationResult = MobileCommonServiceHelper.Push(this.Context, args, operateOption, false);
            DynamicObject[] array = (from p in convertOperationResult.TargetDataEntities
                                     select p.DataEntity).ToArray<DynamicObject>();
           
            foreach (DynamicObject obj in array)
            {
                DynamicObjectCollection dynamicObjectCollection = obj["Entity"] as DynamicObjectCollection;
               
                foreach (DynamicObject obj1 in dynamicObjectCollection)
                {
                            decimal   num = Convert.ToDecimal(rowData["FQty"]);
                                string strSql = string.Format(@"select FLOTID from T_BD_LOTMASTER where FNUMBER='{0}'", rowData["FLot"].ToString());
                                DynamicObjectCollection rslot = DBServiceHelper.ExecuteDynamicObject(base.Context, strSql);
                                obj1["AppQty"] = num;
                                obj1["StockAppQty"] = num;
                                obj1["StockQty"] = num;
                                obj1["BaseStockQty"] = num;
                                obj1["Qty"] = num;
                                obj1["BaseAppQty"] = num;
                                obj1["BaseQty"] = num;
                                obj1["Lot_Id"] = Convert.ToInt64(rslot[0]["FLOTID"]);
                                obj1["Lot_Text"] = rowData["FLot"].ToString();
                                obj1["MTONO"] = rs[0]["FMTONO"];
                                if (Convert.ToInt64(rowData["FStockId"])== 22315406)
                                {
                                  obj1["StockStatusId_Id"] = 27910195;
                                }
                                obj1["F_RUJP_PgEntryId"] = rowData["FPgEntryId"];
                        }
                    }
            FormMetadata cachedFormMetaData = FormMetaDataCache.GetCachedFormMetaData(base.Context, "PRD_ReturnMtrl");
            OperateOption option = OperateOption.Create();
            option.AddInteractionFlag("Kingdee.K3.SCM.App.Core.AppBusinessService.UpdateStockService,Kingdee.K3.SCM.App.Core");
            option.SetIgnoreInteractionFlag(true);
            IOperationResult operationResult = BusinessDataServiceHelper.Save(base.Context, cachedFormMetaData.BusinessInfo, array, option, "");
            if (operationResult.IsSuccess)
            {
                operationResult = BusinessDataServiceHelper.Submit(base.Context, cachedFormMetaData.BusinessInfo, (from o in array
                                                                                                                   select o["Id"]).ToArray<object>(), "Submit", null);
                operationResult = BusinessDataServiceHelper.Audit(base.Context, cachedFormMetaData.BusinessInfo, (from o in array
                                                                                                                  select o["Id"]).ToArray<object>(), option);
            }
            return operationResult;
        }

        protected virtual void HandleResult(IOperationResult result)
        {
            string text = string.Join(";", from o in result.ValidationErrors
                                           select o.Message);
           
            if (!result.IsSuccess)
            {
                base.View.ShowMessage(text, MessageBoxType.Notice);
                return;
            }
            if (text.IsNullOrEmptyOrWhiteSpace())
            {
                base.View.ShowMessage(ResManager.LoadKDString("退料成功！", "015747000026617", SubSystemType.MFG, new object[0]), MessageBoxOptions.OK, delegate (MessageBoxResult r)
                {
                    if (r == MessageBoxResult.OK)
                    {
                        this.View.Close();
                    }
                }, "", MessageBoxType.Notice);
                return;
            }
            base.View.ReturnToParentWindow(text);
            base.View.Close();
        }

        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (e.Field.Key.Equals("FQty"))
            {
                Decimal pickQty = Convert.ToDecimal(this.View.Model.GetValue("FMustQty", e.Row));
                if (Convert.ToDecimal(e.NewValue) > pickQty)
                {
                    this.View.ShowStatusBarInfo(ResManager.LoadKDString("退料数量不能大于领料数量！", "015747000026624", SubSystemType.MFG, new object[0]));
                    return;
                }
            }

        }


        internal class ReturnInfo
        {
            private static string _moNumber;
            private string _MaterialNumber;
            private string _MaterialName;
            private string _Model;
            private string _lot;
            private decimal _PickQty;
            private decimal _Qty;
            private long _OrgId;
            private long _stockId;
            private long _pbomEntryId;
            private long _PgEntryId;
            private long _baseUnitId;


            /// <summary>
            /// 生产订单号
            /// </summary>
            public string MONumber
            {
                get
                {
                    return _moNumber;
                }
                set
                {
                    _moNumber = value;
                }
            }
            /// <summary>
            /// 物料编码
            /// </summary>
            public string MaterialNumber
            {
                get
                {
                    return _MaterialNumber;
                }
                set
                {
                    _MaterialNumber = value;
                }
            }

            /// <summary>
            /// 物料名称
            /// </summary>
            public string MaterialName
            {
                get
                {
                    return _MaterialName;
                }
                set
                {
                    _MaterialName = value;
                }
            }

            /// <summary>
            /// 规格型号
            /// </summary>
            public string Model
            {
                get { return _Model; }
                set { _Model = value; }
            }

            /// <summary>
            /// 批号
            /// </summary>
            public string lot
            {
                get
                {
                    return _lot;
                }
                set
                {
                    _lot = value;
                }
            }

            /// <summary>
            /// 领料数量
            /// </summary>
            public decimal PickQty
            {
                get
                {
                    return _PickQty;
                }
                set
                {
                    _PickQty = value;
                }
            }
            /// <summary>
            /// 退料数量
            /// </summary>
            public decimal Qty
            {
                get
                {
                    return _Qty;
                }
                set
                {
                    _Qty = value;
                }
            }
            /// <summary>
            /// 组织Id
            /// </summary>
            public long OrgId
            {
                get { return _OrgId; }
                set { _OrgId = value; }
            }
            /// <summary>
            ///仓库Id
            /// </summary>
            public long stockId
            {
                get { return _stockId; }
                set { _stockId = value; }
            }
            /// <summary>
            /// 用料清单分录Id
            /// </summary>
            public long pbomEntryId
            {
                get
                {
                    return _pbomEntryId;
                }
                set
                {
                    _pbomEntryId = value;
                }
            }
            /// <summary>
            /// 派工明细分录Id
            /// </summary>
            public long pgEntryId
            {
                get { return _PgEntryId; }
                set { _PgEntryId = value; }
            }
            /// <summary>
            /// 基本计量单位
            /// </summary>
            public long baseUnitId
            {
                get { return _baseUnitId; }
                set { _baseUnitId = value; }
            }

        }
    }
    }

    

