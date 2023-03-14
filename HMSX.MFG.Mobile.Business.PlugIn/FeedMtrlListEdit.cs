using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.Operation;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Interaction;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.ConvertElement.ServiceArgs;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Mobile.PlugIn;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Resource;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using Kingdee.K3.Core.MFG.SFS.ParamOption;
using Kingdee.K3.MFG.Mobile.ServiceHelper;
using Kingdee.K3.MFG.ServiceHelper.SFS;
using Kingdee.K3.MFG.SFS.Common.Core.ParamValue;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace HMSX.MFG.Mobile.Business.PlugIn
{
    [Description("补料编辑-表单插件")]
    [Kingdee.BOS.Util.HotUpdate]
    public class FeedMtrlListEdit : AbstractMobilePlugin
    {
        string DispatchDetailEntryId;//派工明细Id
        string MoBillNo = "";
        string MoBillEntrySeq = "";
        DynamicObjectCollection ppBomInfo;
        string kczt = "";

        public override void OnInitialize(InitializeEventArgs e)
        {
            base.OnInitialize(e);
            DispatchDetailEntryId = e.Paramter.GetCustomParameter("FPgEntryId").ToString();
            this.View.GetControl("FLable_User").SetValue(this.Context.UserName);
            //获取已领料数据+替代件
            string strSql = string.Format(@"SELECT distinct T1.FPRDORGID,T1.FMOBillNO,T1.FMOENTRYSEQ,T2.FSEQ,T2.FID,T2.FENTRYID,T2.FMATERIALID,T4.FNUMBER,T5.FNAME,T5.FSPECIFICATION,
                                                             T2.FBASEUNITID,T3.FSTOCKID,T.FAvailableQty as FPickQty,T.FPgEntryId--,PA.FACTUALQTY,PA.FLOT,PA.FLOT_TEXT
                                                             ,FREPLACEGROUP 
                                                             FROM t_PgBomInfo T
                                                             INNER JOIN T_PRD_PPBOM T1 ON T.FPPBomId = T1.FID
                                                             INNER JOIN T_PRD_PPBOMENTRY T2 ON T.FPPBomEntryId = T2.FENTRYID
                                                             INNER JOIN T_PRD_PPBOMENTRY_C T3 ON T.FPPBomId = T3.FID AND T.FPPBomEntryId = T3.FENTRYID AND T3.FISSUETYPE IN ('1', '3')
                                                             INNER JOIN T_PRD_PICKMTRLDATA PA ON T.FPPBomEntryId = PA.FPPBOMENTRYID AND T.FPgEntryId = PA.F_RUJP_PGENTRYID
                                                             INNER JOIN T_BD_MATERIAL T4 ON T.FMATERIALID = T4.FMATERIALID
                                                             INNER JOIN T_BD_MATERIAL_L T5 ON T.FMATERIALID = T5.FMATERIALID AND T5.FLOCALEID = 2052
                                                             WHERE T.FPgEntryId IN ({0})
                                                             union all
                                                             SELECT distinct T1.FPRDORGID,T1.FMOBillNO,T1.FMOENTRYSEQ,T2.FSEQ,T2.FID,T2.FENTRYID,T2.FMATERIALID,T4.FNUMBER,T5.FNAME,T5.FSPECIFICATION,
                                                             T2.FBASEUNITID,T3.FSTOCKID,T.FAvailableQty as FPickQty,T.FPgEntryId--,'0' FACTUALQTY,jskc.FLOT,ph.fnumber  FLOT_TEXT
                                                             ,FREPLACEGROUP
                                                             FROM t_PgBomInfo T
                                                             INNER JOIN T_PRD_PPBOM T1 ON T.FPPBomId = T1.FID
                                                             INNER JOIN T_PRD_PPBOMENTRY T2 ON T.FPPBomEntryId = T2.FENTRYID
                                                             INNER JOIN T_PRD_PPBOMENTRY_C T3 ON T.FPPBomId = T3.FID AND T.FPPBomEntryId = T3.FENTRYID AND T3.FISSUETYPE IN ('1', '3')
                                                             --inner join T_STK_INVENTORY jskc on T2.FMATERIALID=jskc.FMATERIALID and jskc.FSTOCKID=T3.FSTOCKID and jskc.FSTOCKSTATUSID=10000
                                                             --inner join T_BD_LOTMASTER ph on jskc.FLOT=ph.FLOTID
                                                             INNER JOIN T_BD_MATERIAL T4 ON T.FMATERIALID = T4.FMATERIALID
                                                             INNER JOIN T_BD_MATERIAL_L T5 ON T.FMATERIALID = T5.FMATERIALID AND T5.FLOCALEID = 2052
                                                             INNER JOIN T_BAS_BILLTYPE LX ON LX.FBILLTYPEID=T1.FMOTYPE
                                                             WHERE T.FPgEntryId IN ({1}) and (T.FMustQty=0 OR LX.FNUMBER='SCDD02_SYS' AND T.FPickQty=0 )                                                        
                                                                ", DispatchDetailEntryId, DispatchDetailEntryId);
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
                        if (scanText1.Length>2)
                        {
                            string scanText2= scanText1[3] + "-" + scanText1[4];
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
        public void UpdateEntry(string scanText)
        {
            //根据条码获取物料信息
            string materialNumber = "";
            string lot_txt = "";
            List<FeedInfo> listFeedinfo = new List<FeedInfo>();
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
               // string strSql = string.Format(@"SELECT t.FMATERIALID,t1.FNUMBER,t.FLOT,t.FLOT_TEXT,t.FSTOCKID FROM T_BD_BARCODEMAIN t INNER JOIN T_BD_MATERIAL t1 ON t.FMATERIALID=t1.FMATERIALID WHERE FBARCODE='{0}'", scanText);
                DynamicObjectCollection rs = DBServiceHelper.ExecuteDynamicObject(this.Context, strSql);
                if (rs.Count > 0)
                {
                    materialNumber = rs[0]["FNUMBER"].ToString();

                    lot_txt = rs[0]["FLOT_TEXT"].ToString();                   
                }
                
                foreach (DynamicObject obj in ppBomInfo)
                {

                    if (obj["FNUMBER"].ToString() == materialNumber) //&& obj["FLOT_TEXT"].ToString() == lot_txt)
                    {
                        string pcSql = $@"SELECT top 1 t.FSTOCKSTATUSID,t.FMATERIALID,t.FSTOCKID,t.FLOT,t1.FNUMBER FROM  T_STK_INVENTORY t
                                      LEFT JOIN T_BD_LOTMASTER T1 ON t.FLOT=t1.FLOTID  AND t.FMaterialId=T1.FmaterialId
                                      where t.FMATERIALID={obj["FMATERIALID"]}  and t.FSTOCKID='{obj["FSTOCKID"]}'
                                      and t.FBASEQTY>0 and t.FSTOCKSTATUSID=case when t.FSTOCKID=22315406 then 27910195 else 10000 end
                                      order by t.FSTOCKSTATUSID desc,t1.FNUMBER ASC";
                        DynamicObjectCollection pc = DBUtils.ExecuteDynamicObject(this.Context, pcSql);
                        if (pc.Count > 0)
                        {
                            if (pc[0]["FNUMBER"].ToString() != rs[0]["FLOT_TEXT"].ToString())
                            {
                                throw new KDBusinessException("", "补料的批次需遵循先进先出原则！");
                            }
                           
                        }
                        string jskcsql = $@"select b.FSTOCKSTATUSID,FBASEQTY,b.FNAME from T_STK_INVENTORY a
                                    left JOIN t_BD_StockStatus_L b ON a.FSTOCKSTATUSID=b.FStockStatusId
                                     where FMATERIALID={obj["FMATERIALID"]}  
                                        and FSTOCKID='{obj["FSTOCKID"]}'
                                     and FLOT={rs[0]["FLOT"]} and FBASEQTY> 0 
                                     and a.FSTOCKSTATUSID=case when FSTOCKID=22315406 then 27910195 else 10000 end
                                     ORDER BY a.FSTOCKSTATUSID desc";
                        var jskc = DBUtils.ExecuteDynamicObject(Context, jskcsql);
                        kczt = jskc.Count > 0 ? jskc[0]["FSTOCKSTATUSID"].ToString():"";
                        decimal kcsl = jskc.Count > 0 ? Convert.ToDecimal(jskc[0]["FBASEQTY"]) : 0;
                        string kcztname = jskc.Count > 0 ? jskc[0]["FNAME"].ToString() : "";
                        //已领数量、剩余数量
                        string ylslsql = $@"SELECT T1.FPRDORGID,T1.FMOBillNO,T1.FMOENTRYSEQ,T.FMustQty,T.FAvailableQty,
                        case when(T.FMustQty-T.FAvailableQty)<0 then 0 else T.FMustQty-T.FAvailableQty end SYSL
                        FROM t_PgBomInfo T
                        INNER JOIN T_PRD_PPBOM T1 ON T.FPPBomId = T1.FID
                        INNER JOIN T_PRD_PPBOMENTRY T2 ON T.FPPBomEntryId = T2.FENTRYID
                        INNER JOIN T_PRD_PPBOMENTRY_C T3 ON T.FPPBomId = T3.FID AND T.FPPBomEntryId = T3.FENTRYID AND T3.FISSUETYPE IN('1', '3')
                        WHERE T.FPgEntryId IN({obj["FPgEntryId"]}) 
                        and T1.FMOBillNO='{obj["FMOBillNO"]}'
                        AND T.FMustQty != 0 and FREPLACEGROUP = '{obj["FREPLACEGROUP"]}'";
                        var ylsl = DBUtils.ExecuteDynamicObject(Context, ylslsql);
                        FeedInfo feedinfo = new FeedInfo();
                        feedinfo.MONumber = obj["FMOBillNO"].ToString();
                        feedinfo.MaterialNumber = obj["FNUMBER"].ToString();
                        feedinfo.MaterialName = obj["FNAME"].ToString();
                        feedinfo.Model = obj["FSPECIFICATION"].ToString();
                        feedinfo.lot = lot_txt;
                        feedinfo.PickQty = ylsl.Count > 0 ? Convert.ToDecimal(ylsl[0]["FAvailableQty"].ToString()) : 0;
                        // feedinfo.PickQty = Convert.ToDecimal(obj["FPickQty"]);
                        feedinfo.baseUnitId = Convert.ToInt64(obj["FBASEUNITID"]);
                        feedinfo.stockId = Convert.ToInt64(obj["FSTOCKID"]);
                        feedinfo.OrgId = Convert.ToInt64(obj["FPRDORGID"]);
                        feedinfo.pbomEntryId = Convert.ToInt64(obj["FENTRYID"]);
                        feedinfo.pgEntryId = Convert.ToInt64(obj["FPgEntryId"]);
                        feedinfo.JSKC = kcsl;
                        feedinfo.Kczt = kcztname;
                        feedinfo.WLSL = ylsl.Count > 0 ? Convert.ToDecimal(ylsl[0]["SYSL"].ToString()) : 0;
                        feedinfo.BLSL = (ylsl.Count > 0 ? Convert.ToDecimal(ylsl[0]["SYSL"].ToString()) : 0) >= kcsl ?
                                  kcsl : (ylsl.Count > 0 ? Convert.ToDecimal(ylsl[0]["SYSL"].ToString()) : 0);
                        listFeedinfo.Add(feedinfo);
                        //kcsl = (ylsl.Count > 0 ? Convert.ToDecimal(ylsl[0]["SYSL"].ToString()) : 0) >= kcsl ?
                        //        0 : (kcsl - (ylsl.Count > 0 ? Convert.ToDecimal(ylsl[0]["SYSL"].ToString()) : 0));
                    }
                }
                if (listFeedinfo != null)
                {
                    // this.View.Model.DeleteEntryData("F_SBID_MobileListViewEntity");
                    for (int i = 0; i < listFeedinfo.Count; i++)
                    {
                        this.View.Model.CreateNewEntryRow("F_SBID_MobileListViewEntity");
                        int rowCount = this.View.Model.GetEntryRowCount("F_SBID_MobileListViewEntity");
                        int Seq = rowCount;
                        this.View.Model.SetValue("FSeq", Seq, rowCount - 1);
                        this.View.Model.SetValue("FMONumber", listFeedinfo[i].MONumber, rowCount - 1);
                        this.View.Model.SetValue("FMaterialNumber", listFeedinfo[i].MaterialNumber, rowCount - 1);
                        this.View.Model.SetValue("FMaterialName", listFeedinfo[i].MaterialName, rowCount - 1);
                        this.View.Model.SetValue("FModel", listFeedinfo[i].Model, rowCount - 1);
                        this.View.Model.SetValue("FLot", listFeedinfo[i].lot, rowCount - 1);
                        this.View.Model.SetValue("FMustQty", listFeedinfo[i].PickQty, rowCount - 1);
                        this.View.Model.SetValue("FBaseUnitID", listFeedinfo[i].baseUnitId, rowCount - 1);
                        this.View.Model.SetValue("FPpBomEntryId", listFeedinfo[i].pbomEntryId, rowCount - 1);
                        this.View.Model.SetValue("FPgEntryId", listFeedinfo[i].pgEntryId, rowCount - 1);
                        this.View.Model.SetValue("FStockId", listFeedinfo[i].stockId, rowCount - 1);
                        this.View.Model.SetValue("FOrgId", listFeedinfo[i].OrgId, rowCount - 1);
                        this.View.Model.SetValue("F_260_KCSL", listFeedinfo[i].JSKC, rowCount - 1);
                        this.View.Model.SetValue("F_260_WLSL", listFeedinfo[i].WLSL, rowCount - 1);
                        this.View.Model.SetValue("FQty", listFeedinfo[i].BLSL, rowCount - 1);
                        this.View.Model.SetValue("F_KCZT", listFeedinfo[i].Kczt, rowCount - 1);
                        this.View.UpdateView("F_SBID_MobileListViewEntity");
                    }
                }
            }
        }

        public void Confirm()
        {
            Entity entity = this.Model.BusinessInfo.GetEntity("F_SBID_MobileListViewEntity");
            DynamicObjectCollection rows = this.View.Model.GetEntityDataObject(entity);
            // foreach (DynamicObject rowData in rows)
            // { 
            //     if(Convert.ToDecimal(rowData["FQty"])> Convert.ToDecimal(rowData["F_260_WLSL"]))
            //     {
            //         throw new KDBusinessException("", "补料数量超过未领料数量！");
            //     }
            // }
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
                "INNER JOIN  T_PRD_PPBOMENTRY T1 ON T.FID=T1.FID AND T1.FENTRYID={0} " +
                "left JOIN (select DD.FMTONO,DD.FID,DD.FSEQ from T_PRD_MOENTRY DD " +
                           "inner JOIN T_PRD_MO CC ON DD.FID = CC.FID and FBILLTYPE = '0e74146732c24bec90178b6fe16a2d1c')AA " +
                           " ON AA.FID=T.FMOID AND AA.FSEQ=T.FMOENTRYSEQ", Convert.ToInt64(rowData["FPpBomEntryId"]));
            DynamicObjectCollection rs = DBServiceHelper.ExecuteDynamicObject(this.Context, strsql);
            List<SFSCreateFeedMtrlParam> listFeed = new List<SFSCreateFeedMtrlParam>();
            SFSCreateFeedMtrlParam Feedinfo = new SFSCreateFeedMtrlParam()
            {
                Lot_Text = rowData["FLot"].ToString(),
                PPBomBillNo = rs[0]["FBILLNO"].ToString(),
                PPBomEntrySeq = Convert.ToInt32(rs[0]["FSEQ"]) - 1,
                AppQty = Convert.ToDecimal(rowData["FQty"]),
            };
            listFeed.Add(Feedinfo);
            List<ListSelectedRow> list = new List<ListSelectedRow>();
            ListSelectedRow item = new ListSelectedRow(Convert.ToString(rs[0]["FID"]), Convert.ToString(rs[0]["FENTRYID"]), Convert.ToInt32(rs[0]["FSEQ"]) - 1, "PRD_PPBOM")
            {
                EntryEntityKey = "FEntity"

            };
            list.Add(item);
            if (list.Count == 0)
            {
                base.View.ShowMessage(ResManager.LoadKDString("没有需要补料的分录！", "015747000026506", SubSystemType.MFG, new object[0]), MessageBoxType.Notice);
                return null;
            }
            ConvertOperationResult convertOperationResult;
            string convertRuleId = "PRD_PPBOM2FEEDMTRL_LOT"; //
            var ruleMeta = ConvertServiceHelper.GetConvertRule(this.Context, convertRuleId);
            var rule = ruleMeta.Rule;
            PushArgs args = new PushArgs(rule, list.ToArray())
            {
                TargetBillTypeId = "ca103ed148d0492dbe81d89f54d9ef85",//普通补料
            };

            OperateOption operateOption = OperateOption.Create();
            operateOption.AddInteractionFlag("Kingdee.K3.SCM.App.Core.AppBusinessService.UpdateStockService,Kingdee.K3.SCM.App.Core");
            operateOption.SetIgnoreInteractionFlag(true);
            operateOption.SetVariableValue("LstParam", listFeed);
            operateOption.SetVariableValue("DicLot", null);
            convertOperationResult = MobileCommonServiceHelper.Push(this.Context, args, operateOption, false);
            DynamicObject[] array = (from p in convertOperationResult.TargetDataEntities
                                     select p.DataEntity).ToArray<DynamicObject>();

            foreach (DynamicObject obj in array)
            {
                DynamicObjectCollection dynamicObjectCollection = obj["Entity"] as DynamicObjectCollection;

                foreach (DynamicObject obj1 in dynamicObjectCollection)
                {
                    decimal num = Convert.ToDecimal(rowData["FQty"]);
                    string strSql = string.Format(@"select FLOTID from T_BD_LOTMASTER where FNUMBER='{0}'", rowData["FLot"].ToString());
                    DynamicObjectCollection rslot = DBServiceHelper.ExecuteDynamicObject(base.Context, strSql);
                    obj1["AppQty"] = num;
                    obj1["StockAppQty"] = num;
                    obj1["StockActualQty"] = num;
                    obj1["BaseStockActualQty"] = num;
                    obj1["ActualQty"] = num;
                    obj1["BaseAppQty"] = num;
                    obj1["BaseActualQty"] = num;
                    obj1["Lot_Id"] = Convert.ToInt64(rslot[0]["FLOTID"]);
                    obj1["Lot_Text"] = rowData["FLot"].ToString();
                    obj1["F_RUJP_PgEntryId"] = rowData["FPgEntryId"];
                    obj1["MTONO"] = rs[0]["FMTONO"];
                    if (kczt != "")
                    {
                        obj1["StockStatusId_Id"] = kczt;
                    }

                }
            }
            FormMetadata cachedFormMetaData = FormMetaDataCache.GetCachedFormMetaData(base.Context, "PRD_FeedMtrl");
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
                base.View.ShowMessage(ResManager.LoadKDString("补料成功！", "015747000026617", SubSystemType.MFG, new object[0]), MessageBoxOptions.OK, delegate (MessageBoxResult r)
                {
                    if (r == MessageBoxResult.OK)
                    {
                        base.View.Close();
                    }
                }, "", MessageBoxType.Notice);
                return;
            }
            base.View.ReturnToParentWindow(text);
            base.View.Close();
        }

        internal class FeedInfo
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
            private decimal _jskc;
            private decimal _wlsl;
            private decimal _blsl;
            private string _kczt;
            /// <summary>
            /// 库存状态
            /// </summary>
            public string Kczt
            {
                get
                {
                    return _kczt;
                }
                set
                {
                    _kczt = value;
                }
            }
            /// <summary>
            /// 补料数量
            /// </summary>
            public decimal BLSL
            {
                get
                {
                    return _blsl;
                }
                set
                {
                    _blsl = value;
                }
            }
            /// <summary>
            /// 即时库存
            /// </summary>
            public decimal JSKC
            {
                get
                {
                    return _jskc;
                }
                set
                {
                    _jskc = value;
                }
            }
            /// <summary>
            /// 未领数量
            /// </summary>
            public decimal WLSL
            {
                get
                {
                    return _wlsl;
                }
                set
                {
                    _wlsl = value;
                }
            }
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
