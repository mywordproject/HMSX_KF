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
    [Description("补料单--带出供应商")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class BLServerPlugin: AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FStockOrgId", "FLot", "FMaterialId" , "F_RUJP_PgEntryId", "F_260_PGMXID" , "FMoBillNo", "FPPBomEntryId" };
            foreach (String property in propertys)
            {
                e.FieldKeys.Add(property);
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
                                string upsql = $@"update T_PRD_FEEDMTRLDATA set F_260_GYS1='{gys[0]["FSUPPLYID"].ToString()}' where FENTRYID='{entry["Id"].ToString()}'";
                                DBUtils.Execute(Context, upsql);
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
                            string cxsql = $@"update T_SFC_DISPATCHDETAILENTRY set F_260_LLSL=F_260_LLSL+{Convert.ToDouble(entry["ActualQty"].ToString())} where FENTRYID='{entry["F_RUJP_PGENTRYID"]}' or FENTRYID='{entry["F_260_PGMXID"]}'";
                            DBUtils.Execute(Context, cxsql);
                            string TDJSQL = $@"SELECT T.FPgEntryId,T.FPPBomEntryId,T.FMaterialId,T.FMustQty,FREPLACEGROUP,T2.FMATERIALID,FAvailableQty
                            FROM t_PgBomInfo T
                            INNER JOIN T_PRD_PPBOM T1 ON T.FPPBomId = T1.FID
                            INNER JOIN T_PRD_PPBOMENTRY T2 ON T.FPPBomEntryId = T2.FENTRYID
                            INNER JOIN T_PRD_PPBOMENTRY_C T3 ON T.FPPBomId = T3.FID AND T.FPPBomEntryId = T3.FENTRYID AND T3.FISSUETYPE IN ('1', '3')
                            WHERE 
                            T.FPgEntryId ={entry["F_RUJP_PGENTRYID"]}
                            and T.FPPBomEntryId= {entry["PPBomEntryId"]}
                            and T.FMustQty=0";
                            var TDJ = DBUtils.ExecuteDynamicObject(Context, TDJSQL);
                            if (TDJ.Count > 0)
                            {
                                string UPSQL = $@"/*dialect*/update t_PgBomInfo set FAvailableQty=FAvailableQty+{Convert.ToDouble(entry["ActualQty"].ToString())}
                                         FROM
                                         (SELECT T.FPgEntryId,T.FPPBomEntryId,T.FMaterialId,T.FMustQty,FREPLACEGROUP
                                         FROM t_PgBomInfo T
                                         INNER JOIN T_PRD_PPBOM T1 ON T.FPPBomId = T1.FID
                                         INNER JOIN T_PRD_PPBOMENTRY T2 ON T.FPPBomEntryId = T2.FENTRYID
                                         INNER JOIN T_PRD_PPBOMENTRY_C T3 ON T.FPPBomId = T3.FID AND T.FPPBomEntryId = T3.FENTRYID AND T3.FISSUETYPE IN ('1', '3')
                                         WHERE 
                                         T.FPgEntryId ={entry["F_RUJP_PGENTRYID"]}
                                         and T1.FMoBillNo='{entry["MoBillNo"]}'                                        
                                         and T.FMustQty!=0
                                         AND T2.FREPLACEGROUP={TDJ[0]["FREPLACEGROUP"]} )AA WHERE 
                                         AA.FPgEntryId=t_PgBomInfo.FPgEntryId AND
                                         AA.FPPBomEntryId=t_PgBomInfo.FPPBomEntryId
                                         ";
                                DBUtils.Execute(Context, UPSQL);
                            }
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
                            string cxsql = $@"update T_SFC_DISPATCHDETAILENTRY set F_260_LLSL=F_260_LLSL-{Convert.ToDouble(entry["ActualQty"].ToString())} where FENTRYID='{entry["F_RUJP_PGENTRYID"]}' or  FENTRYID='{entry["F_260_PGMXID"]}'";
                            DBUtils.Execute(Context, cxsql);

                           
                             string TDJSQL = $@"SELECT T.FPgEntryId,T.FPPBomEntryId,T.FMaterialId,T.FMustQty,FREPLACEGROUP,T2.FMATERIALID,FAvailableQty
                            FROM t_PgBomInfo T
                            INNER JOIN T_PRD_PPBOM T1 ON T.FPPBomId = T1.FID
                            INNER JOIN T_PRD_PPBOMENTRY T2 ON T.FPPBomEntryId = T2.FENTRYID
                            INNER JOIN T_PRD_PPBOMENTRY_C T3 ON T.FPPBomId = T3.FID AND T.FPPBomEntryId = T3.FENTRYID AND T3.FISSUETYPE IN ('1', '3')
                            WHERE 
                            T.FPgEntryId ={entry["F_RUJP_PGENTRYID"]}
                            and T.FPPBomEntryId= {entry["PPBomEntryId"]}
                            and T.FMustQty=0";
                            var TDJ = DBUtils.ExecuteDynamicObject(Context, TDJSQL);
                            if (TDJ.Count > 0)
                            {
                                string UPSQL = $@"/*dialect*/update t_PgBomInfo set FAvailableQty=FAvailableQty-{Convert.ToDouble(entry["ActualQty"].ToString())}
                                         FROM
                                         (SELECT T.FPgEntryId,T.FPPBomEntryId,T.FMaterialId,T.FMustQty,FREPLACEGROUP
                                         FROM t_PgBomInfo T
                                         INNER JOIN T_PRD_PPBOM T1 ON T.FPPBomId = T1.FID
                                         INNER JOIN T_PRD_PPBOMENTRY T2 ON T.FPPBomEntryId = T2.FENTRYID
                                         INNER JOIN T_PRD_PPBOMENTRY_C T3 ON T.FPPBomId = T3.FID AND T.FPPBomEntryId = T3.FENTRYID AND T3.FISSUETYPE IN ('1', '3')
                                         WHERE 
                                         T.FPgEntryId ={entry["F_RUJP_PGENTRYID"]}
                                         and T1.FMoBillNo='{entry["MoBillNo"]}'                                        
                                         and T.FMustQty!=0
                                         AND T2.FREPLACEGROUP={TDJ[0]["FREPLACEGROUP"]} )AA WHERE 
                                         AA.FPgEntryId=t_PgBomInfo.FPgEntryId AND
                                         AA.FPPBomEntryId=t_PgBomInfo.FPPBomEntryId
                                         ";
                                DBUtils.Execute(Context, UPSQL);
                            }
                        }
                    }
                }
            }
        }
       // public override void BeforeExecuteOperationTransaction(BeforeExecuteOperationTransaction e)
       // {
       //     base.BeforeExecuteOperationTransaction(e);
       //     if (FormOperation.Operation.Equals("Save", StringComparison.OrdinalIgnoreCase))
       //     {
       //         foreach (ExtendedDataEntity extended in e.SelectedRows)
       //         {
       //             DynamicObject dy = extended.DataEntity;
       //
       //             if (dy["StockOrgId_Id"].ToString() == "100026")
       //             {
       //                 DynamicObjectCollection docPriceEntity = dy["Entity"] as DynamicObjectCollection;
       //                 foreach (var entry in docPriceEntity)
       //                 {
       //                     string scddsql = $@"select * from T_PRD_MO where fbillno='{entry["MoBillNo"]}' and FBILLTYPE='00232405fc58a68311e33257e9e17076'";
       //                     var scdd = DBUtils.ExecuteDynamicObject(Context, scddsql);
       //                     if (scdd.Count > 0)
       //                     {
       //                         if (entry["F_260_PGMXID"].ToString() == "0" && entry["F_RUJP_PgEntryId"].ToString() == "0")
       //                         {
       //                             throw new KDBusinessException("", "生产订单类型为工序汇报入库-普通生产,派工明细必录！");
       //                         }
       //                     }
       //
       //                 }
       //             }
       //         }
       //     }
       // }
    }
}
