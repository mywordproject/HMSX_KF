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
    [Description("工序汇报--保存时带出客户标签、客户")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class GXHBHZServerPlugin : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FPrdOrgId", "FLot", "FMoNumber", "FDispatchDetailEntryId", "FFinishQty", "FMaterialId", "FBillNo", "FHMSXKHBQYD", "FHMSXKHBQYD", "F_260_SFNPI1" };
            foreach (String property in propertys)
            {
                e.FieldKeys.Add(property);
            }
        }
        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);
            if (FormOperation.Operation.Equals("Save", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var dates in e.DataEntitys)
                {
                    if (dates["PrdOrgId_Id"].ToString() == "100026")
                    {
                        var entrys = dates["OptRptEntry"] as DynamicObjectCollection;
                        foreach (var entry in entrys)
                        {
                            string cxsql = $@"select b.FSHORTNAME  from HMD_t_Cust100150 a
                                                inner join T_BD_CUSTOMER_L b on a.F_HMD_BASEKH=b.FCUSTID 
                                                WHERE a.FID={entry["FHMSXKHBQYD_Id"]}";
                            var cxs = DBUtils.ExecuteDynamicObject(Context, cxsql);
                            if (cxs.Count > 0)
                            {
                                //带出客户标签
                                string khbqsql = $@"/*dialect*/update PAEZ_t_Cust_Entry100320 set FHMSXBZ=aa.FSHORTNAME from
                                                (select a.FID,b.FSHORTNAME  from HMD_t_Cust100150 a
                                                inner join T_BD_CUSTOMER_L b on a.F_HMD_BASEKH=b.FCUSTID 
                                                WHERE a.FID={entry["FHMSXKHBQYD_Id"]}) aa where aa.fid=PAEZ_t_Cust_Entry100320.FHMSXKHBQYD
                                                and FENTRYID={entry["Id"]}";
                                DBUtils.Execute(Context, khbqsql);
                                //带出客户
                                string khsql = $@"/*dialect*/ update PAEZ_t_Cust_Entry100320 set FHMSXKH=bb.F_HMD_BASEKH from
                                                (select FID,F_HMD_BASEKH  from HMD_t_Cust100150                                        
                                                WHERE FID={entry["FHMSXKHBQYD_Id"]}) bb where bb.fid=PAEZ_t_Cust_Entry100320.FHMSXKHBQYD
                                                and FENTRYID={entry["Id"]}";
                                DBUtils.Execute(Context, khsql);

                                long FStockId = 0;
                                if (cxs[0]["FSHORTNAME"].ToString() == "CDFX" && entry["F_260_SFNPI1"].ToString() != "NPI_OLD")
                                {
                                    FStockId = 11784504;
                                }
                                else if (cxs[0]["FSHORTNAME"].ToString() == "LHFX" && entry["F_260_SFNPI1"].ToString() != "NPI_OLD")
                                {
                                    FStockId = 11784506;
                                }
                                else if (cxs[0]["FSHORTNAME"].ToString() == "吉宝" && entry["F_260_SFNPI1"].ToString() != "NPI_OLD")
                                {
                                    FStockId = 11784508;
                                }
                                else if (cxs[0]["FSHORTNAME"].ToString() == "达功" && entry["F_260_SFNPI1"].ToString() != "NPI_OLD")
                                {
                                    FStockId = 11784509;
                                }
                                else if (cxs[0]["FSHORTNAME"].ToString() == "BYD" && entry["F_260_SFNPI1"].ToString() != "NPI_OLD")
                                {
                                    FStockId = 11784511;
                                }
                                else if (cxs[0]["FSHORTNAME"].ToString() == "翊宝" && entry["F_260_SFNPI1"].ToString() != "NPI_OLD")
                                {
                                    FStockId = 11784513;
                                }
                                else if (cxs[0]["FSHORTNAME"].ToString() == "歌尔" && entry["F_260_SFNPI1"].ToString() != "NPI_OLD")
                                {
                                    FStockId = 25856631;
                                }
                                else if (cxs[0]["FSHORTNAME"].ToString() == "鸿富成" && entry["F_260_SFNPI1"].ToString() != "NPI_OLD")
                                {
                                    FStockId = 31116287;
                                }
                                else if (cxs[0]["FSHORTNAME"].ToString() == "VNFX" && entry["F_260_SFNPI1"].ToString() != "NPI_OLD")
                                {
                                    FStockId = 32379391;
                                }
                                if (FStockId != 0)
                                {
                                    string cksql = $@"/*dialect*/update PAEZ_t_Cust_Entry100320 set FSTOCKID={FStockId} where FENTRYID={entry["Id"]}";
                                    DBUtils.Execute(Context, cksql);
                                }

                            }

                        }
                    }
                }
            }
        }
    }
}
