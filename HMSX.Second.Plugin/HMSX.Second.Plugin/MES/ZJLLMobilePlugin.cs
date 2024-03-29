﻿using Kingdee.BOS;
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
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.ComplexCacheJson.Model;
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.ComplexCacheJson.Utils;
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.Utils;
using Kingdee.K3.MFG.Mobile.ServiceHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.MES
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("直接领料")]
    public class ZJLLMobilePlugin : AbstractMobilePlugin
    {
        string FEntryId;//派工明细EntryId
        string FYSM;
        string CSTM = "";
        string MoBillNo = "";//生产订单号
        string MoBillEntrySeq = "";//生产订明细行号
        List<pickinfo> pickinfoList = new List<pickinfo>();
        string kczt = "";
        protected FormCacheModel cacheModel4Save = new FormCacheModel();
        protected bool HasCached;
        protected int TotalPageNumber;//总页数
        public override void OnInitialize(InitializeEventArgs e)
        {
            base.OnInitialize(e);
            FEntryId = e.Paramter.GetCustomParameter("FPgEntryId").ToString();
            FYSM = e.Paramter.GetCustomParameter("FYSM").ToString();
            CSTM = e.Paramter.GetCustomParameter("CSTM").ToString();
            //获取生产订单编号，生产订单行号
            string strSql = string.Format(@"SELECT FMOBILLNO,FMOSEQ FROM T_SFC_DISPATCHDETAIL WHERE FID=(SELECT TOP 1 FID FROM T_SFC_DISPATCHDETAILENTRY WHERE FENTRYID IN ({0}))", FEntryId);
            DynamicObjectCollection rs = DBServiceHelper.ExecuteDynamicObject(this.Context, strSql);
            if (rs.Count > 0)
            {
                MoBillNo = rs[0]["FMOBILLNO"].ToString();
                MoBillEntrySeq = rs[0]["FMOSEQ"].ToString();
            }
            this.View.GetControl("FLable_User").SetValue(this.Context.UserName);
        }
        public override void ButtonClick(ButtonClickEventArgs e)
        {
            base.ButtonClick(e);
            string key;
            switch (key = e.Key.ToUpper())
            {
                case "FBUTTON_RETURN":
                    this.View.Close();
                    return;
                case "FBUTTON_LOGOUT":
                    JsonCacheUtils.DeleteCache(base.Context, this.cacheModel4Save.DeviceCode, this.HasCached);
                    LoginUtils.LogOut(base.Context, base.View);
                    base.View.Logoff("indexforpad.aspx");
                    return;
                case "FSUBMIT":
                    this.Confirm();
                    return;
                case "F_BUTTON_SM":
                    string scanText = this.View.Model.GetValue("F_TMSM") == null ? "" : this.View.Model.GetValue("F_TMSM").ToString();
                    FillData1(scanText);
                    this.View.Model.SetValue("F_TMSM", "");
                    this.View.UpdateView("F_TMSM");
                    this.View.GetControl("F_TMSM").SetFocus();
                    this.View.GetControl("F_TMSM").SetCustomPropertyValue("showKeyboard", true);
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
                    if (key == "F_TMSM")
                    {
                        if (!string.IsNullOrEmpty(e.Value.ToString()) && !string.IsNullOrWhiteSpace(e.Value.ToString()))
                        {
                            FillData1(e.Value.ToString());
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
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            this.View.GetControl("F_SBID_MobileListViewEntity").SetCustomPropertyValue("listEditable", true);
            if (!string.IsNullOrEmpty(FYSM) && !string.IsNullOrWhiteSpace(FYSM))
            {
                FillData();
            }

        }
        public void FillData()
        {
            List<DynamicObject> PPBomInfo = this.GetPPBomInfo(MoBillNo, MoBillEntrySeq);
            var ppBominfosum = (from p in PPBomInfo select new { materialid = Convert.ToInt64(p["FMATERIALID"]), stockId = Convert.ToInt64(p["FSTOCKID"]), LOT = Convert.ToInt64(p["F_RUJP_LOT"]), gys = Convert.ToString(p["GYS"]) }).Distinct().ToList();
            string scddsql = $@"/*dialect*/ select  FBILLNO                                            
                         from T_PRD_MO a
                         inner join T_PRD_MOENTRY b on a.fid=b.fid
                         inner join T_BAS_BILLTYPE c on c.FBILLTYPEID=A.FBILLTYPE
                         where
                         (A.FBILLNO like '%MO%' or A.FBILLNO like '%XNY%' or A.FBILLNO like '%YJ%')
                         and FPRDORGID=100026
                         and  C.FNUMBER='SCDD02_SYS' and F_260_YDLX!='' 
                         and FBILLNO='{MoBillNo}' and FSEQ='{MoBillEntrySeq}' ";
            var scdd = DBUtils.ExecuteDynamicObject(Context, scddsql);
            foreach (var pp in ppBominfosum)
            {
                string strSql = "";
                string gysname = "";
                foreach (var sup in pp.gys.ToString().Split(';'))
                {
                    gysname += "'" + sup + "',";
                }
                if (scdd.Count > 0)
                {
                    strSql = string.Format(@"SELECT  t.FSTOCKSTATUSID,T2.FNAME,t.FStockOrgId,t.FStockId,t.FMaterialId,t.FLot,t1.FNUMBER,t.FBASEUNITID,t.FBASEQTY  FROM T_STK_INVENTORY t 
                                                                  LEFT JOIN T_BD_LOTMASTER T1 ON t.FLOT=t1.FLOTID  AND t.FMaterialId=T1.FmaterialId
                                                                  left JOIN t_BD_StockStatus_L T2 ON t.FSTOCKSTATUSID=T2.FStockStatusId 
                                                                  left join T_BD_SUPPLier_l gys on gys.FSUPPLIERID=t1.FSUPPLYID
                                                                  WHERE 
                                                                  t.FSTOCKSTATUSID in (33194113,33797546,5410804)
                                                                  AND  t.FStockId={0} AND  t.FMaterialId={1}  AND t.FBASEQTY>0 
                                                                  and   T1.FNUMBER='{2}'
                                                                  AND((convert(varchar(255),t1.FSUPPLYID)='{3}' or '{3}'='0' or '{3}'='') or (gys.Fname in ({4})) )
                                                                  ORDER BY t.FSTOCKSTATUSID desc,FNUMBER ASC", pp.stockId, pp.materialid, pp.LOT, pp.gys, gysname.Trim(','));
                }
                else
                {
                     strSql = string.Format(@"SELECT  t.FSTOCKSTATUSID,T2.FNAME,t.FStockOrgId,t.FStockId,t.FMaterialId,t.FLot,t1.FNUMBER,t.FBASEUNITID,t.FBASEQTY  FROM T_STK_INVENTORY t 
                                                                  LEFT JOIN T_BD_LOTMASTER T1 ON t.FLOT=t1.FLOTID  AND t.FMaterialId=T1.FmaterialId
                                                                  left JOIN t_BD_StockStatus_L T2 ON t.FSTOCKSTATUSID=T2.FStockStatusId
                                                                  left join T_BD_SUPPLier_l gys on gys.FSUPPLIERID=t1.FSUPPLYID
                                                                  WHERE 
                                                                  t.FSTOCKSTATUSID=case when t.FStockId in (22315406,31786848)  then 27910195 else 10000 end
                                                                  AND  t.FStockId={0} AND  t.FMaterialId={1}  AND t.FBASEQTY>0 
                                                                  and   T1.FNUMBER='{2}'
                                                                  AND((convert(varchar(255),t1.FSUPPLYID)='{3}' or '{3}'='0' or '{3}'='') or (gys.Fname in ({4})) )
                                                                  ORDER BY t.FSTOCKSTATUSID desc,FNUMBER ASC", pp.stockId, pp.materialid, pp.LOT, pp.gys, gysname.Trim(','));
                }
                DynamicObjectCollection stockrs = DBServiceHelper.ExecuteDynamicObject(this.Context, strSql);
                var PPBomInfotmp = (from p in PPBomInfo where Convert.ToInt64(p["FMATERIALID"]) == pp.materialid && Convert.ToInt64(p["F_RUJP_LOT"]) == pp.LOT && Convert.ToString(p["GYS"]) == pp.gys select p);
                DynamicObjectCollection tmp = stockrs;
                foreach (DynamicObject obj in PPBomInfotmp)
                {
                    Decimal mustQty = Convert.ToDecimal(obj["FMustQty"]) - Convert.ToDecimal(obj["FPickQty"]);
                    for (int j = 0; j < pickinfoList.Count; j++)
                    {
                        if(pickinfoList[j].MaterialNumber.ToString()== obj["FNUMBER"].ToString())
                        {
                            mustQty -= pickinfoList[j].Qty;
                        }
                    }
                    DynamicObjectCollection rs = tmp;
                    for (int i = 0; i < rs.Count; i++)
                    {
                        if (Convert.ToDecimal(rs[i]["FBASEQTY"]) <= 0)
                        {
                            continue;
                        }
                        kczt = rs[0]["FSTOCKSTATUSID"].ToString();
                        pickinfo pInfo = new pickinfo();
                        pInfo.MONumber = obj["FMOBillNO"].ToString();
                        pInfo.MaterialNumber = obj["FNUMBER"].ToString();
                        pInfo.MaterialName = obj["FNAME"].ToString();
                        pInfo.Model = obj["FSPECIFICATION"].ToString();
                        pInfo.MustQty = mustQty;
                        pInfo.Kczt = rs[i]["FNAME"].ToString();
                        if (rs[i]["FNUMBER"] != null)
                        {
                            pInfo.lot = rs[i]["FNUMBER"].ToString();
                        }

                        if (rs[i]["FBASEQTY"] != null)
                        {
                            pInfo.stockQty = Convert.ToDecimal(rs[i]["FBASEQTY"]);
                            pInfo.baseUnitId = Convert.ToInt64(rs[i]["FBASEUNITID"]);
                        }
                        else { }
                        if (Convert.ToDecimal(rs[i]["FBASEQTY"]) >= mustQty)
                        {
                            pInfo.Qty = mustQty;
                            tmp[i]["FBASEQTY"] = Convert.ToDecimal(rs[i]["FBASEQTY"]) - mustQty;
                            mustQty = 0;
                        }
                        else
                        {
                            pInfo.Qty = Convert.ToDecimal(rs[i]["FBASEQTY"]);
                            mustQty = mustQty - Convert.ToDecimal(rs[i]["FBASEQTY"]);
                            tmp[i]["FBASEQTY"] = 0;
                            //tmp.RemoveAt(i);
                        }
                        pInfo.pbomEntryId = Convert.ToInt64(obj["FENTRYID"]);
                        pInfo.pgEntryId = Convert.ToInt64(obj["FPgEntryId"]);

                        pickinfoList.Add(pInfo);
                        if (mustQty == 0)
                        {
                            break;
                        }
                    }

                }
            }
            pickinfoList = pickinfoList.OrderBy(p => p.MaterialNumber).ThenBy(p => p.lot).ToList();
            if (pickinfoList.Count > 0)
            {
                string number = pickinfoList[0].MaterialNumber.ToString();
                Decimal allqty = 0;
                for (int i = 0; i < pickinfoList.Count; i++)
                {
                    this.View.Model.CreateNewEntryRow("F_SBID_MobileListViewEntity");
                    int rowCount = this.View.Model.GetEntryRowCount("F_SBID_MobileListViewEntity");
                    int Seq = i + 1;
                    this.View.Model.SetValue("FSeq", Seq, i);
                    this.View.Model.SetValue("FMONumber", pickinfoList[i].MONumber.ToString(), i);
                    this.View.Model.SetValue("FMaterialNumber", pickinfoList[i].MaterialNumber.ToString(), i);
                    this.View.Model.SetValue("FMaterialName", pickinfoList[i].MaterialName.ToString(), i);
                    this.View.Model.SetValue("FModel", pickinfoList[i].Model.ToString(), i);
                    this.View.Model.SetValue("FLot", pickinfoList[i].lot, i);
                    this.View.Model.SetValue("FBaseUnitID", pickinfoList[i].baseUnitId, i);
                    this.View.Model.SetValue("FMustQty", pickinfoList[i].MustQty, i);
                    this.View.Model.SetValue("FQty", pickinfoList[i].Qty, i);
                    allqty = allqty + Convert.ToDecimal(pickinfoList[i].Qty);
                    this.View.Model.SetValue("FStockQty", pickinfoList[i].stockQty, i);
                    this.View.Model.SetValue("FPBomEntryId", pickinfoList[i].pbomEntryId, i);
                    this.View.Model.SetValue("FPgEntryId", pickinfoList[i].pgEntryId, i);
                    this.View.Model.SetItemValueByID("FKCZT", pickinfoList[i].Kczt, i);
                    this.View.Model.SetValue("FISSCAN", "Y", i);
                    if (i == 0)
                    {
                        this.View.Model.SetValue("FIsParent", "1", i);
                    }
                    if (i > 0 && number == pickinfoList[i].MaterialNumber.ToString())
                    {
                        this.View.Model.SetValue("FIsParent", "0", i);
                    }
                    if (i > 0 && number != pickinfoList[i].MaterialNumber.ToString())
                    {
                        this.View.Model.SetValue("FIsParent", "1", i);
                        number = pickinfoList[i].MaterialNumber.ToString();
                    }
                    this.View.UpdateView("F_SBID_MobileListViewEntity");
                }
                this.View.Model.SetValue("FAllQty", allqty+Convert.ToDecimal(this.Model.GetValue("FAllQty")));
                this.View.UpdateView("FAllQty");
            }
        }
        /// <summary>
        ///  获取用料清单信息
        /// </summary>
        /// <param name="MoBillNo"></param>
        /// <param name="MoBillEntrySeq"></param>
        /// <returns></returns>
        private List<DynamicObject> GetPPBomInfo(string MoBillNo, string MoBillEntrySeq)
        {
            string strSql = string.Format(@"SELECT T.FPRDORGID,T.FMOBillNO,T.FMOENTRYSEQ,T1.FSEQ,T1.FID,T1.FENTRYID,T1.FMATERIALID,T1.FMaterialType,T1.FREPLACEGROUP,T3.FMASTERID,T3.FNUMBER,T4.FNAME,
                                            T4.FSPECIFICATION,T2.FPICKEDQTY,T5.FSTOCKID,T1.FNUMERATOR,T1.FDENOMINATOR,T1.FSCRAPRATE,T6.FMustQty,T6.FAvailableQty as FPickQty ,T6.FPgEntryId,
                                            T6.GYS,t6.FPPBOMID,T6.FPPBOMENTRYID,PGMX.F_RUJP_LOT   FROM T_PRD_PPBOM T 
                                            INNER JOIN T_PRD_PPBOMENTRY T1 ON T.FID=T1.FID 
                                            INNER JOIN T_PRD_PPBOMENTRY_Q T2 ON T1.FID=T2.FID AND T1.FENTRYID=T2.FENTRYID  AND T1.FMUSTQTY>(T2.FPICKEDQTY-t2.FGOODRETURNQTY)
                                            INNER JOIN T_PRD_PPBOMENTRY_C T5 ON T1.FID=T5.FID AND T1.FENTRYID=T5.FENTRYID
                                            INNER JOIN T_BD_MATERIAL T3 ON T1.FMATERIALID=T3.FMATERIALID  and T3.FNUMBER!='260.01.13.02.0030' AND T3.FMATERIALID NOT IN (SELECT FMATERIALID FROM T_BD_MATERIALBASE WHERE FErpClsID=5 )
                                            INNER JOIN T_BD_MATERIAL_L T4 ON T1.FMATERIALID=T4.FMATERIALID AND T4.FLOCALEID=2052
                                            INNER JOIN t_PgBomInfo T6 ON T1.FENTRYID=T6.FPPBomEntryId AND T6.FPgEntryId IN ({0})   AND T6.FMustQty-T6.FAvailableQty>0
                                            INNER JOIN 
                                            (SELECT FMATERIALID,F_RUJP_LOT  from T_SFC_DISPATCHDETAIL t 
                                              inner join T_SFC_DISPATCHDETAILENTRY t1 on t.FID=t1.FID  WHERE ({3})) PGMX ON PGMX.FMATERIALID=T1.FMATERIALID
                                              WHERE T.FMOBillNO='{1}' AND T.FMOENTRYSEQ={2} AND T5.FISSUETYPE IN ('1','3') 
                                              ORDER BY T1.FMATERIALID ASC ", FEntryId, MoBillNo, MoBillEntrySeq, FYSM);
            DynamicObjectCollection source = DBServiceHelper.ExecuteDynamicObject(base.Context, strSql);
            return source.ToList<DynamicObject>();
        }

        protected virtual void Confirm()
        {
            //获取已扫描为Y的数据
            List<long> entryIds = new List<long>();
            Entity entity = this.Model.BusinessInfo.GetEntity("F_SBID_MobileListViewEntity");
            DynamicObjectCollection rows = this.View.Model.GetEntityDataObject(entity);
            if (rows.Count > 0)
            {
                foreach (DynamicObject row in rows)
                {
                    if (row["FIsScan"] != null)
                    {
                        entryIds.Add(Convert.ToInt64(row["FPBomEntryId"]));
                    }
                }
            }
            if (entryIds.Count == 0)
            {
                base.View.ShowMessage(ResManager.LoadKDString("没有需要领料的分录！", "015747000026506", SubSystemType.MFG, new object[0]), MessageBoxType.Notice);

            }
            else
            {
                List<DynamicObject> PPBomInfo = this.GetPPBomInfo(entryIds);
                IOperationResult result = this.CreatePickMtrl(PPBomInfo);
                this.PickSetResult(result);
            }
        }
        private List<DynamicObject> GetPPBomInfo(IEnumerable<long> ppBomEntryIds)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(" SELECT ");
            stringBuilder.AppendLine(" P.FID,P.FQTY,P.FBILLNO,PE.FSEQ,PE.FENTRYID,PE.FMUSTQTY,PEQ.FSELPICKEDQTY,PEQ.FGOODRETURNQTY,PEQ.FINCDEFECTRETURNQTY,P.FMOTYPE,AA.FMTONO");
            stringBuilder.AppendLine(" FROM T_PRD_PPBOM P");
            stringBuilder.AppendLine(" INNER JOIN T_PRD_PPBOMENTRY PE  ON P.FID=PE.FID");
            stringBuilder.AppendLine(" INNER JOIN T_PRD_PPBOMENTRY_Q PEQ ON PE.FENTRYID=PEQ.FENTRYID");
            stringBuilder.AppendLine(" INNER JOIN T_PRD_PPBOMENTRY_C PEC ON PEQ.FENTRYID=PEC.FENTRYID");
            stringBuilder.AppendLine(" LEFT  JOIN (select DD.FMTONO,DD.FID,DD.FSEQ from T_PRD_MOENTRY DD " +
                                     " inner JOIN T_PRD_MO CC ON DD.FID = CC.FID and FBILLTYPE = '0e74146732c24bec90178b6fe16a2d1c')AA" +
                                     " ON AA.FID=P.FMOID AND AA.FSEQ=P.FMOENTRYSEQ");
            stringBuilder.AppendLine(" INNER JOIN (select /*+ cardinality(b " + ppBomEntryIds.Distinct<long>().ToArray<long>().Count<long>() + ")*/ FID from table(fn_StrSplit(@EntryId,',',1)) b) EntryId ON EntryId.FID=PE.FENTRYID");
            stringBuilder.AppendLine(" WHERE PEC.FISSUETYPE IN ('1','3')");
            List<SqlParam> list = new List<SqlParam>
    {
        new SqlParam("@EntryId", KDDbType.udt_inttable, ppBomEntryIds.Distinct<long>().ToArray<long>())
    };
            DynamicObjectCollection source = DBServiceHelper.ExecuteDynamicObject(base.Context, stringBuilder.ToString(), null, null, CommandType.Text, list.ToArray());
            return source.ToList<DynamicObject>();
        }
        protected virtual IOperationResult CreatePickMtrl(List<DynamicObject> ppBomInfos)
        {
            Dictionary<long, decimal> dictionary = new Dictionary<long, decimal>();
            List<ListSelectedRow> list = new List<ListSelectedRow>();
            foreach (DynamicObject dynamicObject in ppBomInfos)
            {
                ListSelectedRow item = new ListSelectedRow(Convert.ToString(dynamicObject["FID"]), Convert.ToString(dynamicObject["FENTRYID"]), Convert.ToInt32(dynamicObject["FSEQ"]) - 1, "PRD_PPBOM")
                {
                    EntryEntityKey = "FEntity"

                };
                list.Add(item);

            }
            if (list.Count == 0)
            {
                base.View.ShowMessage(ResManager.LoadKDString("没有需要领料的分录！", "015747000026506", SubSystemType.MFG, new object[0]), MessageBoxType.Notice);
                return null;
            }
            ConvertOperationResult convertOperationResult;
            string convertRuleId = "PRD_PPBOM2PICKMTRL_NORMAL"; //
            var ruleMeta = ConvertServiceHelper.GetConvertRule(this.Context, convertRuleId);
            var rule = ruleMeta.Rule;
            PushArgs args = new PushArgs(rule, list.ToArray())
            {
                TargetBillTypeId = "f4f46eb78a7149b1b7e4de98586acb67",//普通领料

            };

            OperateOption operateOption = OperateOption.Create();
            convertOperationResult = MobileCommonServiceHelper.Push(this.Context, args, operateOption, false);
            DynamicObject[] array = (from p in convertOperationResult.TargetDataEntities
                                     select p.DataEntity).ToArray<DynamicObject>();
            Entity entity = this.Model.BusinessInfo.GetEntity("F_SBID_MobileListViewEntity");
            DynamicObjectCollection rows = this.View.Model.GetEntityDataObject(entity);
            foreach (DynamicObject obj in array)//源单数据
            {
                DynamicObjectCollection dynamicObjectCollection = obj["Entity"] as DynamicObjectCollection;
                int rowcount = dynamicObjectCollection.Count;

                for (int i = 0; i < rowcount; i++)//源单单据体
                {
                    DynamicObject obj1 = dynamicObjectCollection[i];
                    Decimal num = 0L;
                    string lot;
                    foreach (DynamicObject rowObj in rows)//需要领料数据
                    {
                        if (rowObj["FIsScan"] != null && rowObj["FIsScan"].ToString() == "Y")
                        {
                            if (rowObj["FIsParent"].ToString() == "1" && Convert.ToInt64(rowObj["FPBomEntryId"]) == Convert.ToInt64((obj1["PPBomEntryId"])))
                            {

                                num = Convert.ToDecimal(rowObj["FQty"]);
                                string strSql = string.Format(@"select FLOTID from T_BD_LOTMASTER where FNUMBER='{0}'", rowObj["FLot"].ToString());
                                DynamicObjectCollection rslot = DBServiceHelper.ExecuteDynamicObject(base.Context, strSql);
                                obj1["AppQty"] = num;
                                obj1["StockAppQty"] = num;
                                obj1["StockActualQty"] = num;
                                obj1["ActualQty"] = num;
                                obj1["BaseAppQty"] = num;
                                obj1["BaseActualQty"] = num;
                                obj1["BaseStockActualQty"] = num;
                                obj1["Lot_Id"] = Convert.ToInt64(rslot[0]["FLOTID"]);
                                obj1["Lot_Text"] = rowObj["FLot"].ToString();
                                obj1["F_RUJP_PgEntryId"] = rowObj["FPgEntryId"].ToString();
                                obj1["StockStatusId_Id"] = kczt;
                                obj1["MTONO"] = ppBomInfos[0]["FMTONO"];
                            }
                            if (rowObj["FIsParent"].ToString() == "0" && Convert.ToInt64(rowObj["FPBomEntryId"]) == Convert.ToInt64((obj1["PPBomEntryId"])))
                            {
                                DynamicObject newRow = (DynamicObject)obj1.Clone(false, true);
                                num = Convert.ToDecimal(rowObj["FQty"]);
                                string strSql = string.Format(@"select FLOTID from T_BD_LOTMASTER where FNUMBER='{0}'", rowObj["FLot"].ToString());
                                DynamicObjectCollection rslot = DBServiceHelper.ExecuteDynamicObject(base.Context, strSql);
                                newRow["AppQty"] = num;
                                newRow["StockAppQty"] = num;
                                newRow["StockActualQty"] = num;
                                newRow["ActualQty"] = num;
                                newRow["BaseAppQty"] = num;
                                newRow["BaseActualQty"] = num;
                                newRow["BaseStockActualQty"] = num;
                                newRow["Lot_Id"] = Convert.ToInt64(rslot[0]["FLOTID"]);
                                newRow["Lot_Text"] = rowObj["FLot"].ToString();
                                newRow["F_RUJP_PgEntryId"] = rowObj["FPgEntryId"].ToString();
                                newRow["StockStatusId_Id"] = kczt;
                                newRow["MTONO"] = ppBomInfos[0]["FMTONO"];
                                dynamicObjectCollection.Add(newRow);
                            }
                        }
                    }

                }
            }
            FormMetadata cachedFormMetaData = FormMetaDataCache.GetCachedFormMetaData(base.Context, "PRD_PickMtrl");
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
        protected virtual void PickSetResult(IOperationResult result)
        {
            if (result == null)
            {
                return;
            }
            if (result.IsSuccess)
            {
                this.View.ShowStatusBarInfo(ResManager.LoadKDString("领料成功！", "015747000026623", SubSystemType.MFG, new object[0]));
                this.View.Close();
                return;
            }
            else
            {
                string text = string.Join(";", from o in result.OperateResult
                                               select o.Message);
                string text2 = string.Join(";", from o in result.ValidationErrors
                                                select o.Message);
                if (!text.IsNullOrEmptyOrWhiteSpace())
                {
                    if (!text2.IsNullOrEmptyOrWhiteSpace())
                    {
                        text = text + ";" + text2;
                    }
                    this.View.ShowMessage(ResManager.LoadKDString("操作失败！", "015747000026527", SubSystemType.MFG, new object[0]) + text, MessageBoxType.Notice);
                    return;
                }
                this.View.ShowMessage(ResManager.LoadKDString("操作失败！", "015747000026527", SubSystemType.MFG, new object[0]) + text2, MessageBoxType.Notice);
                return;
            }
        }
        internal class pickinfo
        {
            private static string _moNumber;
            private string _MaterialNumber;
            private string _MaterialName;
            private string _Model;
            private string _lot;
            private decimal _MustQty;
            private decimal _Qty;
            private decimal _stockQty;
            private long _pbomEntryId;
            private long _PgEntryId;
            private long _baseUnitId;
            private string _IsParent;
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
            /// 应领数量
            /// </summary>
            public decimal MustQty
            {
                get
                {
                    return _MustQty;
                }
                set
                {
                    _MustQty = value;
                }
            }
            /// <summary>
            /// 领料数量
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
            /// 库存数量
            /// </summary>
            public decimal stockQty
            {
                get { return _stockQty; }
                set { _stockQty = value; }
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

            /// <summary>
            /// 是否父分录
            /// </summary>
            public string IsParent
            {
                get { return _IsParent; }
                set { _IsParent = value; }
            }

        }

        public void FillData1(string scanText)
        {
            if (!string.IsNullOrEmpty(scanText) && !string.IsNullOrWhiteSpace(scanText))
            {
                var jys = this.Model.DataObject["SBID_K156fd801"] as DynamicObjectCollection;
                foreach (var jy in jys)
                {
                    if (jy["F_YSTM"].ToString().Contains(scanText))
                    {
                        throw new KDBusinessException("", "重复扫码！！！");
                    }
                }
                pickinfoList.Clear();
                string ytm = "";
                string strSql1 = string.Format(@"/*dialect*/select FENTRYID,FMOBILLNO,FMOSEQ,F_260_CSTM
                                              from T_SFC_DISPATCHDETAIL t 
                                              inner join T_SFC_DISPATCHDETAILENTRY t1 on t.FID=t1.FID 
                                              where  F_260_CSTM!=''and F_260_CSTM like '%{0}%'", scanText);
                DynamicObjectCollection pgs = DBServiceHelper.ExecuteDynamicObject(this.Context, strSql1);
                foreach (var pg in pgs)
                {
                    SavePgBom(pg["FENTRYID"].ToString());
                    List<DynamicObject> PPBomInfo = this.GetPPBomInfo1(pg["FENTRYID"].ToString(),pg["FMOBILLNO"].ToString(),pg["FMOSEQ"].ToString(), scanText);
                    var ppBominfosum = (from p in PPBomInfo select new { materialid = Convert.ToInt64(p["FMATERIALID"]), stockId = Convert.ToInt64(p["FSTOCKID"]), LOT = Convert.ToInt64(p["F_RUJP_LOT"]), ystm = p["F_260_CSTM"], gys = Convert.ToString(p["GYS"]),pg = Convert.ToString(p["FPgEntryId"]) }).Distinct().ToList();
                    string scddsql = $@"/*dialect*/ select  FBILLNO                                            
                         from T_PRD_MO a
                         inner join T_PRD_MOENTRY b on a.fid=b.fid
                         inner join T_BAS_BILLTYPE c on c.FBILLTYPEID=A.FBILLTYPE
                         where
                         (A.FBILLNO like '%MO%' or A.FBILLNO like '%XNY%' or A.FBILLNO like '%YJ%')
                         and FPRDORGID=100026
                         and  C.FNUMBER='SCDD02_SYS' and F_260_YDLX!='' 
                         and FBILLNO='{MoBillNo}' and FSEQ='{MoBillEntrySeq}' ";
                    var scdd = DBUtils.ExecuteDynamicObject(Context, scddsql);
                    foreach (var pp in ppBominfosum)
                    {
                        ytm = Convert.ToString(pp.ystm);
                        string strSql = "";
                        string gysname = "";
                        foreach (var sup in pp.gys.ToString().Split(';'))
                        {
                            gysname += "'" + sup + "',";
                        }
                        if (scdd.Count > 0)
                        {
                            strSql = string.Format(@"SELECT  t.FSTOCKSTATUSID,T2.FNAME,t.FStockOrgId,t.FStockId,t.FMaterialId,t.FLot,t1.FNUMBER,t.FBASEUNITID,t.FBASEQTY FROM T_STK_INVENTORY t 
                                                                  LEFT JOIN T_BD_LOTMASTER T1 ON t.FLOT=t1.FLOTID  AND t.FMaterialId=T1.FmaterialId
                                                                  left JOIN t_BD_StockStatus_L T2 ON t.FSTOCKSTATUSID=T2.FStockStatusId 
                                                                  left join T_BD_SUPPLier_l gys on gys.FSUPPLIERID=t1.FSUPPLYID
                                                                  WHERE 
                                                                  t.FSTOCKSTATUSID in (33194113,33797546,5410804)
                                                                  AND  t.FStockId={0} AND  t.FMaterialId={1}  AND t.FBASEQTY>0 
                                                                  and   T1.FNUMBER='{2}'
                                                                  AND((convert(varchar(255),t1.FSUPPLYID)='{3}' or '{3}'='0' or '{3}'='') or (gys.Fname in ({4})) )
                                                                  ORDER BY t.FSTOCKSTATUSID desc,FNUMBER ASC", pp.stockId, pp.materialid, pp.LOT, pp.gys, gysname.Trim(','));
                        }
                        else
                        {
                             strSql = string.Format(@"SELECT  t.FSTOCKSTATUSID,T2.FNAME,t.FStockOrgId,t.FStockId,t.FMaterialId,t.FLot,t1.FNUMBER,t.FBASEUNITID,t.FBASEQTY FROM T_STK_INVENTORY t 
                                                                  LEFT JOIN T_BD_LOTMASTER T1 ON t.FLOT=t1.FLOTID  AND t.FMaterialId=T1.FmaterialId
                                                                  left JOIN t_BD_StockStatus_L T2 ON t.FSTOCKSTATUSID=T2.FStockStatusId 
                                                                  left join T_BD_SUPPLier_l gys on gys.FSUPPLIERID=t1.FSUPPLYID
                                                                  WHERE 
                                                                  t.FSTOCKSTATUSID=case when t.FStockId in (22315406,31786848)  then 27910195 else 10000 end
                                                                  AND  t.FStockId={0} AND  t.FMaterialId={1}  AND t.FBASEQTY>0 
                                                                  and   T1.FNUMBER='{2}'
                                                                  AND((convert(varchar(255),t1.FSUPPLYID)='{3}' or '{3}'='0' or '{3}'='') or (gys.Fname in ({4})) )
                                                                  ORDER BY t.FSTOCKSTATUSID desc,FNUMBER ASC", pp.stockId, pp.materialid, pp.LOT, pp.gys, gysname.Trim(','));
                        }
                        DynamicObjectCollection stockrs = DBServiceHelper.ExecuteDynamicObject(this.Context, strSql);
                        var PPBomInfotmp = (from p in PPBomInfo where Convert.ToInt64(p["FMATERIALID"]) == pp.materialid && Convert.ToInt64(p["F_RUJP_LOT"]) == pp.LOT && Convert.ToString(p["GYS"]) == pp.gys && Convert.ToString(p["FPgEntryId"]) == pp.pg select p);
                         DynamicObjectCollection tmp = stockrs;
                        foreach (DynamicObject obj in PPBomInfotmp)
                        {
                            Decimal mustQty = Convert.ToDecimal(obj["FMustQty"]) - Convert.ToDecimal(obj["FPickQty"]);
                            var dates = this.Model.DataObject["SBID_K156fd801"] as DynamicObjectCollection;
                            foreach (var date in dates)
                            { 
                                if(date["FMaterialNumber"].ToString()== obj["FNUMBER"].ToString() && date["FPgEntryId"].ToString()==obj["FPgEntryId"].ToString())
                                {
                                    mustQty -= Convert.ToDecimal(date["FQty"]);
                                }
                            }                            
                            DynamicObjectCollection rs = tmp;
                            for (int i = 0; i < rs.Count; i++)
                            {
                                if (Convert.ToDecimal(rs[i]["FBASEQTY"]) <= 0)
                                {
                                    continue;
                                }
                                kczt = rs[0]["FSTOCKSTATUSID"].ToString();
                                pickinfo pInfo = new pickinfo();
                                pInfo.MONumber = obj["FMOBillNO"].ToString();
                                pInfo.MaterialNumber = obj["FNUMBER"].ToString();
                                pInfo.MaterialName = obj["FNAME"].ToString();
                                pInfo.Model = obj["FSPECIFICATION"].ToString();
                                pInfo.MustQty = mustQty;
                                pInfo.Kczt = rs[i]["FNAME"].ToString();
                                if (rs[i]["FNUMBER"] != null)
                                {
                                    pInfo.lot = rs[i]["FNUMBER"].ToString();
                                }

                                if (rs[i]["FBASEQTY"] != null)
                                {
                                    pInfo.stockQty = Convert.ToDecimal(rs[i]["FBASEQTY"]);
                                    pInfo.baseUnitId = Convert.ToInt64(rs[i]["FBASEUNITID"]);
                                }
                                else { }
                                if (Convert.ToDecimal(rs[i]["FBASEQTY"]) >= mustQty)
                                {
                                    pInfo.Qty = mustQty;
                                    tmp[i]["FBASEQTY"] = Convert.ToDecimal(rs[i]["FBASEQTY"]) - mustQty;
                                    mustQty = 0;
                                }
                                else
                                {
                                    pInfo.Qty = Convert.ToDecimal(rs[i]["FBASEQTY"]);
                                    mustQty = mustQty - Convert.ToDecimal(rs[i]["FBASEQTY"]);
                                    tmp[i]["FBASEQTY"] = 0;
                                    //tmp.RemoveAt(i);
                                }
                                pInfo.pbomEntryId = Convert.ToInt64(obj["FENTRYID"]);
                                pInfo.pgEntryId = Convert.ToInt64(obj["FPgEntryId"]);

                                pickinfoList.Add(pInfo);
                                if (mustQty == 0)
                                {
                                    break;
                                }
                            }

                        }
                    }
                    pickinfoList = pickinfoList.OrderBy(p => p.MaterialNumber).ThenBy(p => p.lot).ToList();
                    if (pickinfoList.Count > 0)
                    {
                        string number = pickinfoList[0].MaterialNumber.ToString();
                        Decimal allqty = 0;
                        for (int i = 0; i < pickinfoList.Count; i++)
                        {
                            var dates = this.Model.DataObject["SBID_K156fd801"] as DynamicObjectCollection;
                            int rowCount = this.View.Model.GetEntryRowCount("F_SBID_MobileListViewEntity");
                            this.View.Model.CreateNewEntryRow("F_SBID_MobileListViewEntity");
                            if (rowCount == 0)
                            {
                                this.View.Model.SetValue("FIsParent", "1", rowCount);
                            }
                            int j = 0;
                            foreach (var date in dates)
                            {
                                if (rowCount > 0 && date["FMaterialNumber"] != null && date["FMaterialNumber"].ToString() == pickinfoList[i].MaterialNumber.ToString())
                                {
                                    this.View.Model.SetValue("FIsParent", "0", rowCount);
                                    break;
                                }
                                if (rowCount > 0 && date["FMaterialNumber"] != null && date["FMaterialNumber"].ToString() != pickinfoList[i].MaterialNumber.ToString())
                                {
                                    
                                    j++;
                                }
                            }
                            if (j == dates.Count-1 && j>0)
                            {
                                this.View.Model.SetValue("FIsParent", "1", rowCount);
                            }
                            int Seq = rowCount + 1;
                            this.View.Model.SetValue("FSeq", Seq, rowCount);
                            this.View.Model.SetValue("FMONumber", pickinfoList[i].MONumber.ToString(), rowCount);
                            this.View.Model.SetValue("FMaterialNumber", pickinfoList[i].MaterialNumber.ToString(), rowCount);
                            this.View.Model.SetValue("FMaterialName", pickinfoList[i].MaterialName.ToString(), rowCount);
                            this.View.Model.SetValue("FModel", pickinfoList[i].Model.ToString(), rowCount);
                            this.View.Model.SetValue("FLot", pickinfoList[i].lot, rowCount);
                            this.View.Model.SetValue("FBaseUnitID", pickinfoList[i].baseUnitId, rowCount);
                            this.View.Model.SetValue("FMustQty", pickinfoList[i].MustQty, rowCount);
                            this.View.Model.SetValue("FQty", pickinfoList[i].Qty, rowCount);
                            allqty = allqty + Convert.ToDecimal(pickinfoList[i].Qty);
                            this.View.Model.SetValue("FStockQty", pickinfoList[i].stockQty, rowCount);
                            this.View.Model.SetValue("FPBomEntryId", pickinfoList[i].pbomEntryId, rowCount);
                            this.View.Model.SetValue("FPgEntryId", pickinfoList[i].pgEntryId, rowCount);
                            this.View.Model.SetItemValueByID("FKCZT", pickinfoList[i].Kczt, rowCount);
                            this.View.Model.SetValue("FISSCAN", "Y", rowCount);
                            this.View.Model.SetValue("F_YSTM", ytm, rowCount);
                            
                            this.View.UpdateView("F_SBID_MobileListViewEntity");
                        }
                        this.View.Model.SetValue("FAllQty", allqty + Convert.ToDecimal(this.Model.GetValue("FAllQty")));
                        this.View.UpdateView("FAllQty");
                    }
                }
            }
        }
        /// <summary>
        ///  获取用料清单信息
        /// </summary>
        /// <param name="MoBillNo"></param>
        /// <param name="MoBillEntrySeq"></param>
        /// <returns></returns>
        private List<DynamicObject> GetPPBomInfo1(string pgid, string BillNo, string MoSeq, string tm)
        {
            string strSql = string.Format(@"SELECT T.FPRDORGID,T.FMOBillNO,T.FMOENTRYSEQ,T1.FSEQ,T1.FID,T1.FENTRYID,T1.FMATERIALID,T1.FMaterialType,T1.FREPLACEGROUP,T3.FMASTERID,T3.FNUMBER,T4.FNAME,
                                            T4.FSPECIFICATION,T2.FPICKEDQTY,T5.FSTOCKID,T1.FNUMERATOR,T1.FDENOMINATOR,T1.FSCRAPRATE,T6.FMustQty,T6.FAvailableQty as FPickQty ,T6.FPgEntryId,
                                            T6.GYS,t6.FPPBOMID,T6.FPPBOMENTRYID,PGMX.F_RUJP_LOT,F_260_CSTM   FROM T_PRD_PPBOM T 
                                            INNER JOIN T_PRD_PPBOMENTRY T1 ON T.FID=T1.FID 
                                            INNER JOIN T_PRD_PPBOMENTRY_Q T2 ON T1.FID=T2.FID AND T1.FENTRYID=T2.FENTRYID  AND T1.FMUSTQTY>(T2.FPICKEDQTY-t2.FGOODRETURNQTY)
                                            INNER JOIN T_PRD_PPBOMENTRY_C T5 ON T1.FID=T5.FID AND T1.FENTRYID=T5.FENTRYID
                                            INNER JOIN T_BD_MATERIAL T3 ON T1.FMATERIALID=T3.FMATERIALID  AND T3.FMATERIALID NOT IN (SELECT FMATERIALID FROM T_BD_MATERIALBASE WHERE FErpClsID=5 )
                                            INNER JOIN T_BD_MATERIAL_L T4 ON T1.FMATERIALID=T4.FMATERIALID AND T4.FLOCALEID=2052
                                            INNER JOIN t_PgBomInfo T6 ON T1.FENTRYID=T6.FPPBomEntryId AND T6.FPgEntryId IN ({0})   AND T6.FMustQty-T6.FAvailableQty>0
                                            INNER JOIN 
                                            (SELECT FMATERIALID,F_RUJP_LOT,F_260_CSTM  from T_SFC_DISPATCHDETAIL t 
                                              inner join T_SFC_DISPATCHDETAILENTRY t1 on t.FID=t1.FID  WHERE F_260_CSTM!=''and F_260_CSTM like '%{3}%' ) PGMX ON PGMX.FMATERIALID=T1.FMATERIALID
                                            WHERE T.FMOBillNO='{1}' AND T.FMOENTRYSEQ={2} AND T5.FISSUETYPE IN ('1','3') 
                                            ORDER BY T1.FMATERIALID ASC ", pgid, BillNo, MoSeq, tm);
            DynamicObjectCollection source = DBServiceHelper.ExecuteDynamicObject(base.Context, strSql);
            return source.ToList<DynamicObject>();
        }
        public void SavePgBom(string entryId)
        {
            string MoBillNo = "";//生产订单号
            string MoBillEntrySeq = "";//生产订明细行号
            string strSql = string.Format(@"SELECT T.FMOBILLNO,T.FMOSEQ,T1.FWORKQTY FROM T_SFC_DISPATCHDETAIL T INNER JOIN T_SFC_DISPATCHDETAILENTRY T1 ON T.FID=T1.FID AND T1.FENTRYID IN({0})", entryId);
            DynamicObjectCollection rs = DBServiceHelper.ExecuteDynamicObject(this.Context, strSql);
            if (rs.Count > 0)
            {
                for (int i = 0; i < rs.Count; i++)
                {
                    MoBillNo = rs[i]["FMOBILLNO"].ToString();
                    MoBillEntrySeq = rs[i]["FMOSEQ"].ToString();
                    List<DynamicObject> PPBomInfo = this.GetPPBomInfo2(MoBillNo, MoBillEntrySeq);
                    foreach (DynamicObject obj in PPBomInfo)
                    {
                        Decimal mustQty = Convert.ToDecimal(obj["FNUMERATOR"]) / Convert.ToDecimal(obj["FDENOMINATOR"]) * Convert.ToDecimal(rs[i]["FWORKQTY"]) * (Convert.ToDecimal(obj["FUSERATE"]) / 100);
                        DynamicObjectCollection rsentrys = DBServiceHelper.ExecuteDynamicObject(this.Context, string.Format("select * from t_PgBomInfo where FPgEntryId={0} AND FPPBomEntryId={1}", entryId, Convert.ToInt64(obj["FENTRYID"])));
                        if (rsentrys.Count == 0)
                        {
                            string Sql = string.Format(@" INSERT INTO t_PgBomInfo(FPgEntryId,FPPBomId,FPPBomEntryId,FMaterialId,FPgQty,FMustQty,FPickQty,FReturnQty,FFeedQty,FAllPickQty,FAvailableQty,GYS)
                       Values({0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},'{11}')", entryId, Convert.ToInt64(obj["FID"]), Convert.ToInt64(obj["FENTRYID"]), Convert.ToInt64(obj["FMATERIALID"]), Convert.ToDecimal(rs[i]["FWORKQTY"]), mustQty, 0, 0, 0, 0, 0, obj["F_260_DXGYS"] == null ? "" : obj["F_260_DXGYS"].ToString());
                            int row = DBServiceHelper.Execute(this.Context, Sql);
                        }
                    }
                }
            }

        }
        private List<DynamicObject> GetPPBomInfo2(string MoBillNo, string MoBillEntrySeq)
        {
            string strSql = string.Format(@"SELECT T.FPRDORGID,T.FMOBillNO,T.FMOENTRYSEQ,T1.FSEQ,T1.FID,T1.FENTRYID,T1.FMATERIALID,T3.FMASTERID,T3.FNUMBER,T4.FNAME,T4.FSPECIFICATION,T2.FPICKEDQTY,T5.FSTOCKID,T1.FNUMERATOR,T1.FDENOMINATOR,T1.FSCRAPRATE,FUSERATE,F_260_DXGYS  FROM T_PRD_PPBOM T 
                                                             INNER JOIN T_PRD_PPBOMENTRY T1 ON T.FID=T1.FID 
                                                             INNER JOIN T_PRD_PPBOMENTRY_Q T2 ON T1.FID=T2.FID AND T1.FENTRYID=T2.FENTRYID  AND( T1.FMUSTQTY>(T2.FPICKEDQTY-t2.FGOODRETURNQTY) or FMUSTQTY=0 and FUSERATE=0)
                                                             INNER JOIN T_PRD_PPBOMENTRY_C T5 ON T1.FID=T5.FID AND T1.FENTRYID=T5.FENTRYID
                                                             INNER JOIN T_BD_MATERIAL T3 ON T1.FMATERIALID=T3.FMATERIALID  AND T3.FMATERIALID NOT IN (SELECT FMATERIALID FROM T_BD_MATERIALBASE WHERE FErpClsID=5 )
                                                             INNER JOIN T_BD_MATERIAL_L T4 ON T1.FMATERIALID=T4.FMATERIALID AND T4.FLOCALEID=2052
                                                             WHERE T.FMOBillNO='{0}' AND T.FMOENTRYSEQ={1} AND T5.FISSUETYPE IN ('1','3')", MoBillNo, MoBillEntrySeq);
            DynamicObjectCollection source = DBServiceHelper.ExecuteDynamicObject(base.Context, strSql);
            return source.ToList<DynamicObject>();
        }
    }
}
