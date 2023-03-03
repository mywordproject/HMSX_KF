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
    [Description("退料单--带出供应商")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class TLServerPlugin: AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FStockOrgId", "FLot", "FMaterialId", "F_RUJP_PgEntryId", "F_260_PGMXID", "FMoBillNo", "FQty", "F_260_PGMXID" };
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
                                string upsql = $@"update T_PRD_RETURNMTRLENTRY set F_260_GYS1='{gys[0]["FSUPPLYID"].ToString()}' where FENTRYID='{entry["Id"].ToString()}'";
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
                            string cxsql = $@"update T_SFC_DISPATCHDETAILENTRY set F_260_LLSL=F_260_LLSL-{Convert.ToDouble(entry["Qty"].ToString())} where FENTRYID='{entry["F_RUJP_PGENTRYID"]}' or FENTRYID='{entry["F_260_PGMXID"]}' ";
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
                            string cxsql = $@"update T_SFC_DISPATCHDETAILENTRY set F_260_LLSL=F_260_LLSL+{Convert.ToDouble(entry["Qty"].ToString())} where FENTRYID='{entry["F_RUJP_PGENTRYID"]}' or FENTRYID='{entry["F_260_PGMXID"]}'";
                            DBUtils.Execute(Context, cxsql);
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
       //                   //  string scddsql = $@"select * from T_PRD_MO where fbillno='{entry["MoBillNo"]}' and FBILLTYPE='00232405fc58a68311e33257e9e17076'";
       //                   //  var scdd = DBUtils.ExecuteDynamicObject(Context, scddsql);
       //                   //  if (scdd.Count > 0)
       //                   //  {
       //                   //      if (entry["F_260_PGMXID"].ToString() == "0" && entry["F_RUJP_PgEntryId"].ToString() == "0")
       //                   //      {
       //                   //          throw new KDBusinessException("", "生产订单类型为工序汇报入库-普通生产,派工明细必录！");
       //                   //      }
       //                   //  }
       //
       //                 }
       //             }
       //         }
       //     }
       // }
    }
}
