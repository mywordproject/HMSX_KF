using Kingdee.BOS.App.Data;
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
    [Description("其他出库单")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class QTCKServicePlugin : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "F_260_MFYPSE", "F_260_MFYPJE", "F_260_MFYPJSHJ", "FDate", "FBillTypeID", "F_260_HLLX" , "F_260_BWB" , "F_260_JSBB" };
            foreach (String property in propertys)
            {
                e.FieldKeys.Add(property);
            }
        }
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            if (Context.CurrentOrganizationInfo.ID == 100026)
            {
                if (FormOperation.Operation.Equals("Save", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var date in e.DataEntitys)
                    {
                        if (date["BillTypeID"] != null && ((DynamicObject)date["BillTypeID"])["Number"].ToString() == "05MFYP")
                        {
                            double se = 0;
                            double je = 0;
                            double jshj = 0;
                            double hl = 1;
                            foreach (var entry in date["BillEntry"] as DynamicObjectCollection)
                            {
                                se += Convert.ToDouble(entry["F_260_MFYPSE"]);
                                je += Convert.ToDouble(entry["F_260_MFYPJE"]);
                                jshj += Convert.ToDouble(entry["F_260_MFYPJSHJ"]);
                            }
                            string cxsql = $@"select FEXCHANGERATE from T_BD_Rate
                                               where FRATETYPEID='{date["F_260_HLLX_Id"]}'
                                               and FCYTOID='{date["F_260_BWB_Id"]}'
                                               and FCYFORID='{date["F_260_JSBB_Id"]}'
                                               and FBEGDATE<='{date["Date"]}'
                                               and FENDDATE>='{date["Date"]}'";
                            var cx = DBUtils.ExecuteDynamicObject(Context, cxsql);
                            if (cx.Count > 0)
                            {
                                hl = Convert.ToDouble(cx[0]["FEXCHANGERATE"]);
                            }
                            string upsql = $@"/*dialect*/update T_STK_MISDELIVERY set F_260_HL={hl},F_260_ZSE={se},F_260_ZJE={je},F_260_ZJSHJ={jshj},
                                                  F_260_ZSEBWB={hl * se},F_260_ZJEBWB={je * hl},F_260_JSHJBWB={jshj * hl} where FID={date["Id"]}";
                            DBUtils.Execute(Context, upsql);

                        }

                    }

                }
            }
        }
    }
}
