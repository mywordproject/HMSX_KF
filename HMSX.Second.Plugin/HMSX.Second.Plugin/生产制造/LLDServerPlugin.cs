using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.生产制造
{
    [Description("领料单单--校验领料数量，反写数量;领退补--带出供应商")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class LLDServerPlugin : AbstractOperationServicePlugIn
    {

        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FStockOrgId", "FActualQty", "F_RUJP_PGENTRYID", "FMaterialId", "FLot", "FMoBillNo", "F_260_PGMXID" };
            foreach (String property in propertys)
            {
                e.FieldKeys.Add(property);
            }
        }
        public override void BeforeExecuteOperationTransaction(BeforeExecuteOperationTransaction e)
        {
            base.BeforeExecuteOperationTransaction(e);
            if (FormOperation.Operation.Equals("Save", StringComparison.OrdinalIgnoreCase))
            {
                foreach (ExtendedDataEntity extended in e.SelectedRows)
                {
                    DynamicObject dy = extended.DataEntity;

                    if (dy["StockOrgId_Id"].ToString() == "100026")
                    {
                        DynamicObjectCollection docPriceEntity = dy["Entity"] as DynamicObjectCollection;
                        //string pgid = "";
                        List<long> pgids = new List<long>();
                        foreach (var entry in docPriceEntity)
                        {
                            pgids.Add(Convert.ToInt64(entry["F_RUJP_PGENTRYID"]));
                        }

                        foreach (var pgid in pgids.Distinct().ToList())
                        {
                            string MoBillNo = "";//生产订单号
                            string MoBillEntrySeq = "";//生产订明细行号                
                            string strSql = string.Format(@"SELECT T.FMOBILLNO,T.FMOSEQ,T1.FWORKQTY FROM T_SFC_DISPATCHDETAIL T INNER JOIN T_SFC_DISPATCHDETAILENTRY T1 ON T.FID=T1.FID AND T1.FENTRYID IN({0})", pgid);
                            DynamicObjectCollection rs = DBUtils.ExecuteDynamicObject(this.Context, strSql);
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
                                        DynamicObjectCollection rsentrys = DBUtils.ExecuteDynamicObject(this.Context, string.Format("select * from t_PgBomInfo where FPgEntryId={0} AND FPPBomEntryId={1}", pgid, Convert.ToInt64(obj["FENTRYID"])));
                                        if (rsentrys.Count == 0)
                                        {
                                            string Sql = string.Format(@" INSERT INTO t_PgBomInfo(FPgEntryId,FPPBomId,FPPBomEntryId,FMaterialId,FPgQty,FMustQty,FPickQty,FReturnQty,FFeedQty,FAllPickQty,FAvailableQty)
                                   Values({0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10})", pgid, Convert.ToInt64(obj["FID"]), Convert.ToInt64(obj["FENTRYID"]), Convert.ToInt64(obj["FMATERIALID"]), Convert.ToDecimal(rs[i]["FWORKQTY"]), mustQty, 0, 0, 0, 0, 0);
                                            int row = DBUtils.Execute(this.Context, Sql);
                                        }
                                    }
                                }
                            }
                        }
                        foreach (var entry in docPriceEntity)
                        {
                            if((entry["StockId"]as DynamicObject)["Number"].ToString() != "260CK067")
                            {
                                string cxsql = $@"select FMustQty-FAvailableQty as QTY from t_PgBomInfo where FPgEntryId='{entry["F_RUJP_PGENTRYID"].ToString()}' and FMaterialId='{entry["MaterialId_Id"].ToString()}'";
                                var cx = DBUtils.ExecuteDynamicObject(Context, cxsql);
                                if (cx.Count > 0)
                                {
                                    if ((Convert.ToDouble(cx[0]["QTY"].ToString()) - Convert.ToDouble(entry["ActualQty"].ToString())) < -0.01)
                                    {
                                        throw new KDBusinessException("", "超额领料！");
                                    }
                                }
                            }                         
                        }
                    }
                }
            }
        }
        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);
            if (FormOperation.Operation.Equals("Audit", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var dates in e.DataEntitys)
                {
                    if (dates["StockOrgId_Id"].ToString() == "100026")
                    {
                        var entrys = dates["Entity"] as DynamicObjectCollection;
                        foreach (var entry in entrys)
                        {
                            //string gyssql = $@"select FSUPPLYID from T_BD_LOTMASTER where FLOTID='{entry["Lot_Id"].ToString()}' and FMATERIALID='{entry["MaterialId_Id"].ToString()}'";
                            //var gys = DBUtils.ExecuteDynamicObject(Context, gyssql);
                            //if (gys.Count > 0)
                            //{
                            //    string upsql = $@"update T_PRD_PICKMTRLDATA set F_260_GYS='{gys[0]["FSUPPLYID"].ToString()}' where FENTRYID='{entry["Id"].ToString()}'";
                            //    DBUtils.Execute(Context, upsql);
                            //}

                            string cxsql = $@"update T_SFC_DISPATCHDETAILENTRY set F_260_LLSL=F_260_LLSL+{Convert.ToDouble(entry["ActualQty"].ToString())} where FENTRYID='{entry["F_RUJP_PGENTRYID"]}' or FENTRYID='{entry["F_260_PGMXID"]}'";
                            DBUtils.Execute(Context, cxsql);
                        }
                    }
                }
            }
            else if (FormOperation.Operation.Equals("UnAudit", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var dates in e.DataEntitys)
                {
                    if (dates["StockOrgId_Id"].ToString() == "100026")
                    {
                        var entrys = dates["Entity"] as DynamicObjectCollection;
                        foreach (var entry in entrys)
                        {
                            string cxsql = $@"update T_SFC_DISPATCHDETAILENTRY set F_260_LLSL=F_260_LLSL-{Convert.ToDouble(entry["ActualQty"].ToString())} where FENTRYID='{entry["F_RUJP_PGENTRYID"]}' or FENTRYID='{entry["F_260_PGMXID"]}'";
                            DBUtils.Execute(Context, cxsql);
                        }
                    }
                }
            }
        }
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            if (FormOperation.Operation.Equals("Save", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var dates in e.DataEntitys)
                {
                    if (dates["StockOrgId_Id"].ToString() == "100026")
                    {
                        var entrys = dates["Entity"] as DynamicObjectCollection;
                        foreach (var entry in entrys)
                        {
                            string gyssql = $@"select FSUPPLYID from T_BD_LOTMASTER where FLOTID='{entry["Lot_Id"].ToString()}' and FMATERIALID='{entry["MaterialId_Id"].ToString()}'";
                            var gys = DBUtils.ExecuteDynamicObject(Context, gyssql);
                            if (gys.Count > 0)
                            {
                                string upsql = $@"update T_PRD_PICKMTRLDATA set F_260_GYS1='{gys[0]["FSUPPLYID"].ToString()}' where FENTRYID='{entry["Id"].ToString()}'";
                                DBUtils.Execute(Context, upsql);
                            }
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
                                                             INNER JOIN T_BD_MATERIAL T3 ON T1.FMATERIALID=T3.FMATERIALID  and T3.FNUMBER!='260.01.13.02.0030' AND T3.FMATERIALID NOT IN (SELECT FMATERIALID FROM T_BD_MATERIALBASE WHERE FErpClsID=5 )
                                                             INNER JOIN T_BD_MATERIAL_L T4 ON T1.FMATERIALID=T4.FMATERIALID AND T4.FLOCALEID=2052
                                                             WHERE T.FMOBillNO='{0}' AND T.FMOENTRYSEQ={1} AND T5.FISSUETYPE IN ('1','3')", MoBillNo, MoBillEntrySeq);
            DynamicObjectCollection source = DBUtils.ExecuteDynamicObject(Context, strSql);
            return source.ToList<DynamicObject>();
        }
    }
}

