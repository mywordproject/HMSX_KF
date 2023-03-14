using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List.PlugIn;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Orm.Metadata.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace HMSX.GYL.CGGL
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("采购入库单零件标识卡套打")]
    public class ListCGRKTD: AbstractListPlugIn
    {       
        public override void OnPrepareNotePrintData(PreparePrintDataEventArgs e)
        {
            base.OnPrepareNotePrintData(e);
            string tdid = e.NotePrintTplId;
            if (tdid == "0556f46d-8e66-4525-8083-6876b60253b0" || tdid == "ab40e384-b6df-43f1-aee6-0a8035c8721c")
            {
                if(e.DataSourceId.Equals("FInStockEntry", StringComparison.OrdinalIgnoreCase))
                {
                    List<DynamicObject> printEntryRows = new List<DynamicObject>();
                    DynamicObjectType dot = e.DynamicObjectType;
                    dot.RegisterSimpleProperty("FCW", typeof(object), attributes: new SimplePropertyAttribute() { Alias = "FCW" });
                    foreach (DynamicObject obj in e.DataObjects)
                    {
                        string FID = obj["FID"].ToString();
                        string wlid = obj["FMaterialId_Id"].ToString();
                        string phbm = obj["FLot_FNumber"].ToString();
                        //相同物料、批号数量合计
                        string slsql = $"/*dialect*/select sum(FREALQTY) QTY from T_STK_INSTOCKENTRY where FID={FID} and FMATERIALID={wlid} and FLOT_TEXT='{phbm}'";
                        double qty = DBUtils.ExecuteScalar<double>(this.Context, slsql, 0);
                        //wms仓位 
                        string sql = $@"select F_260_CW from PAEZ_t_Cust_Entry100360 a
                            left join T_BD_LOTMASTER ph on ph.FLOTID=a.F_260_PH
                            where a.FID={FID} and F_260_WLBM={wlid} and ph.FNUMBER='{phbm}'";
                        string cw = DBUtils.ExecuteScalar<string>(this.Context, sql, "");                                                                 
                        DynamicObject printEntryRow = new DynamicObject(dot);
                        foreach (var p in obj.DynamicObjectType.Properties)
                        {
                            printEntryRow[p] = obj[p];
                        }
                        printEntryRow["FCW"] = cw;
                        printEntryRow["FRealQty"] = qty;
                        printEntryRows.Add(printEntryRow);                                                                   
                    }
                    e.DataObjects = printEntryRows.ToArray();                   
                }
            }
        }
    }
}
