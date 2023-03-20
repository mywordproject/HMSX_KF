using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Mobile.PlugIn;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.ComplexCacheJson.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.MES
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("直接领料")]
    public class ZJLLMobilePlugin: AbstractMobilePlugin
    {
        string FEntryId;//派工明细EntryId
        string FYSM;
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
            FYSM= e.Paramter.GetCustomParameter("FYSM").ToString();
            //获取生产订单编号，生产订单行号
            string strSql = string.Format(@"SELECT FMOBILLNO,FMOSEQ FROM T_SFC_DISPATCHDETAIL WHERE FID=(SELECT TOP 1 FID FROM T_SFC_DISPATCHDETAILENTRY WHERE FENTRYID IN ({0}))", FEntryId);
            DynamicObjectCollection rs = DBServiceHelper.ExecuteDynamicObject(this.Context, strSql);
            if (rs.Count > 0)
            {
                MoBillNo = rs[0]["FMOBILLNO"].ToString();
                MoBillEntrySeq = rs[0]["FMOSEQ"].ToString();
            }
            this.View.GetControl("FText_MaterialNumberScan").SetCustomPropertyValue("showKeyboard", true);
            this.View.GetControl("FLable_User").SetValue(this.Context.UserName);
        }
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            this.View.GetControl("F_SBID_MobileListViewEntity").SetCustomPropertyValue("listEditable", true);
            FillData();
        }
        public void FillData()
        {
            List<DynamicObject> PPBomInfo = this.GetPPBomInfo(MoBillNo, MoBillEntrySeq);
            var ppBominfosum = (from p in PPBomInfo select new { materialid = Convert.ToInt64(p["FMATERIALID"]), stockId = Convert.ToInt64(p["FSTOCKID"]), LOT = Convert.ToInt64(p["F_LOT_TEXT"]) }).Distinct().ToList();
            foreach (var pp in ppBominfosum)
            {

                string strSql = string.Format(@"SELECT  t.FSTOCKSTATUSID,T2.FNAME,t.FStockOrgId,t.FStockId,t.FMaterialId,t.FLot,t1.FNUMBER,t.FBASEUNITID,t.FBASEQTY FROM T_STK_INVENTORY t 
                                                                  LEFT JOIN T_BD_LOTMASTER T1 ON t.FLOT=t1.FLOTID  AND t.FMaterialId=T1.FmaterialId
                                                                  left JOIN t_BD_StockStatus_L T2 ON t.FSTOCKSTATUSID=T2.FStockStatusId
                                                                  WHERE 
                                                                  t.FSTOCKSTATUSID=case when t.FStockId=22315406 then 27910195 else 10000 end
                                                                  AND  t.FStockId={0} AND  t.FMaterialId={1}  AND t.FBASEQTY>0 
                                                                  and   T1.FNUMBER='{2}'
                                                                  ORDER BY t.FSTOCKSTATUSID desc,FNUMBER ASC", pp.stockId, pp.materialid, pp.LOT);
                DynamicObjectCollection stockrs = DBServiceHelper.ExecuteDynamicObject(this.Context, strSql);
                var PPBomInfotmp = (from p in PPBomInfo where Convert.ToInt64(p["FMATERIALID"]) == pp.materialid && Convert.ToInt64(p["FNUMBER"]) == pp.LOT select p);
                DynamicObjectCollection tmp = stockrs;
                foreach (DynamicObject obj in PPBomInfotmp)
                {
                    Decimal mustQty = Convert.ToDecimal(obj["FMustQty"]) - Convert.ToDecimal(obj["FPickQty"]);
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
                this.View.Model.SetValue("FAllQty", allqty);
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
                                            T6.GYS,t6.FPPBOMID,T6.FPPBOMENTRYID,PGMX.F_LOT_TEXT   FROM T_PRD_PPBOM T 
                                            INNER JOIN T_PRD_PPBOMENTRY T1 ON T.FID=T1.FID 
                                            INNER JOIN T_PRD_PPBOMENTRY_Q T2 ON T1.FID=T2.FID AND T1.FENTRYID=T2.FENTRYID  AND T1.FMUSTQTY>(T2.FPICKEDQTY-t2.FGOODRETURNQTY)
                                            INNER JOIN T_PRD_PPBOMENTRY_C T5 ON T1.FID=T5.FID AND T1.FENTRYID=T5.FENTRYID
                                            INNER JOIN T_BD_MATERIAL T3 ON T1.FMATERIALID=T3.FMATERIALID  AND T3.FMATERIALID NOT IN (SELECT FMATERIALID FROM T_BD_MATERIALBASE WHERE FErpClsID=5 )
                                            INNER JOIN T_BD_MATERIAL_L T4 ON T1.FMATERIALID=T4.FMATERIALID AND T4.FLOCALEID=2052
                                            INNER JOIN t_PgBomInfo T6 ON T1.FENTRYID=T6.FPPBomEntryId AND T6.FPgEntryId IN ({0})   AND T6.FMustQty-T6.FAvailableQty>0
                                            INNER JOIN 
                                            (SELECT FMATERIALID,F_LOT_TEXT  from T_SFC_DISPATCHDETAIL t 
                                              inner join T_SFC_DISPATCHDETAILENTRY t1 on t.FID=t1.FID  WHERE F_260_CSTM IN({3})) PGMX ON PGMX.FMATERIALID=T1.FMATERIALID
                                            WHERE T.FMOBillNO='{1}' AND T.FMOENTRYSEQ={2} AND T5.FISSUETYPE IN ('1','3') 
                                            ORDER BY T1.FMATERIALID ASC ", FEntryId, MoBillNo, MoBillEntrySeq, FYSM);
            DynamicObjectCollection source = DBServiceHelper.ExecuteDynamicObject(base.Context, strSql);
            return source.ToList<DynamicObject>();
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
    }
}
