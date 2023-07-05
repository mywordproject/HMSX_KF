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

namespace HMSX.Second.Plugin.SJCJ
{
    [Description("数据采集--结论")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class SJCJServicePlugin : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FSCZ", "FSCZ1", "FSCZ2", "FSCZ3", "FSCZ4", "FSCZ5", "FSC6", "FSCZ7",
                                       "FSCZ8", "FSCZ9" , "FSCZ10", "FSCZ11", "FSCZ12", "FSCZ13", "FSCZ14", "FSCZ15"
                                       , "FSCZ16", "FSCZ17", "FSCZ18", "FSCZ19", "FSCZ20", "FSCZ21", "FSCZ22", "FSCZ23"
                                       , "FSCZ24", "FSCZ25", "FSCZ26", "FSCZ27", "FSCZ28", "FSCZ29", "FSCZ30", "FSCZ31",
                                       "F_LJJH","FLLZ","FSGC","FXGC",
                                       "F_260_SCZ1", "F_SCZ1", "F_SCZ2", "F_SCZ3", "F_SCZ4", "F_SCZ5", "F_SCZ6", "F_SCZ7",
                                         "F_SCZ8",  "F_SCZ9" ,  "F_SCZ10", "F_SCZ11", "F_SCZ12", "F_SCZ13", "F_SCZ14", "F_SCZ15"
                                       , "F_SCZ16", "F_SCZ17", "F_SCZ18", "F_SCZ19", "F_SCZ20", "F_SCZ21", "F_SCZ22", "F_SCZ23"
                                       , "F_SCZ24", "F_SCZ25", "F_SCZ26", "F_SCZ27", "F_SCZ28", "F_SCZ29", "F_SCZ30", "F_SCZ31",
                                        "FLJJH","F_HMD_LLZ1","F_HMD_SGC1","F_HMD_XGC1","F_PAEZ_Remarks"};
            foreach (String property in propertys)
            {
                e.FieldKeys.Add(property);
            }
        }
        public override void BeforeExecuteOperationTransaction(BeforeExecuteOperationTransaction e)
        {
            base.BeforeExecuteOperationTransaction(e);
            if (Context.CurrentOrganizationInfo.ID == 100026)
            {
                if (FormOperation.Operation.Equals("Save", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (ExtendedDataEntity extended in e.SelectedRows)
                    {
                        DynamicObject dates = extended.DataEntity;
                        int i = 0;
                        try
                        {
                            foreach (var date in dates["HMD_Cust_Entry100282"] as DynamicObjectCollection)
                            {
                                if (date["F_LJJH"] != null && date["F_LJJH"].ToString() != "" && date["F_LJJH"].ToString() != " ")
                                {
                                    decimal llz = date["FLLZ"] != null && date["FLLZ"].ToString() != "" && date["FLLZ"].ToString() != " " ? Convert.ToDecimal(date["FLLZ"]) : 0;
                                    decimal sg = date["FSGC"] != null && date["FSGC"].ToString() != "" && date["FSGC"].ToString() != " " ? Convert.ToDecimal(date["FSGC"]) : 0;
                                    decimal xg = date["FXGC"] != null && date["FXGC"].ToString() != "" && date["FXGC"].ToString() != " " ? Convert.ToDecimal(date["FXGC"]) : 0;
                                    decimal sgc = llz + sg;
                                    decimal xgc = llz + xg;
                                    String[] propertys = { "FSCZ", "FSCZ1", "FSCZ2", "FSCZ3", "FSCZ4", "FSCZ5", "FSC6", "FSCZ7",
                                       "FSCZ8", "FSCZ9" , "FSCZ10", "FSCZ11", "FSCZ12", "FSCZ13", "FSCZ14", "FSCZ15"
                                       , "FSCZ16", "FSCZ17", "FSCZ18", "FSCZ19", "FSCZ20", "FSCZ21", "FSCZ22", "FSCZ23"
                                       , "FSCZ24", "FSCZ25", "FSCZ26", "FSCZ27", "FSCZ28", "FSCZ29", "FSCZ30", "FSCZ31"};
                                    foreach (String property in propertys)
                                    {
                                        if (date["F_LJJH"] != null && date["F_LJJH"].ToString() != "" && date["F_LJJH"].ToString() != " " &&
                                            date[property] != null && date[property].ToString() != "" && date[property].ToString() != " ")
                                        {
                                            if (sgc < Convert.ToDecimal(date[property]))
                                            {
                                                i++;
                                            }
                                            else if (xgc > Convert.ToDecimal(date[property]))
                                            {
                                                i++;
                                            }
                                        }
                                    }
                                }
                            }
                            foreach (var date1 in dates["HMD_Cust_Entry100283"] as DynamicObjectCollection)
                            {
                                if (date1["FLJJH"] != null && date1["FLJJH"].ToString() != "" && date1["FLJJH"].ToString() != " ")
                                {
                                    decimal llz = date1["F_HMD_LLZ1"] != null && date1["F_HMD_LLZ1"].ToString() != "" && date1["F_HMD_LLZ1"].ToString() != " " ? Convert.ToDecimal(date1["F_HMD_LLZ1"]) : 0;
                                    decimal sg = date1["F_HMD_SGC1"] != null && date1["F_HMD_SGC1"].ToString() != "" && date1["F_HMD_SGC1"].ToString() != " " ? Convert.ToDecimal(date1["F_HMD_SGC1"]) : 0;
                                    decimal xg = date1["F_HMD_XGC1"] != null && date1["F_HMD_XGC1"].ToString() != "" && date1["F_HMD_XGC1"].ToString() != " " ? Convert.ToDecimal(date1["F_HMD_XGC1"]) : 0;
                                    decimal sgc = llz + sg;
                                    decimal xgc = llz + xg;
                                    String[] propertys = { "F_260_SCZ1", "F_SCZ1", "F_SCZ2", "F_SCZ3", "F_SCZ4", "F_SCZ5", "F_SCZ6", "F_SCZ7",
                                         "F_SCZ8",  "F_SCZ9" ,  "F_SCZ10", "F_SCZ11", "F_SCZ12", "F_SCZ13", "F_SCZ14", "F_SCZ15"
                                       , "F_SCZ16", "F_SCZ17", "F_SCZ18", "F_SCZ19", "F_SCZ20", "F_SCZ21", "F_SCZ22", "F_SCZ23"
                                       , "F_SCZ24", "F_SCZ25", "F_SCZ26", "F_SCZ27", "F_SCZ28", "F_SCZ29", "F_SCZ30", "F_SCZ31"};
                                    foreach (String property in propertys)
                                    {
                                        if (date1["FLJJH"] != null && date1["FLJJH"].ToString() != "" && date1["FLJJH"].ToString() != " " &&
                                            date1[property] != null && date1[property].ToString() != "" && date1[property].ToString() != " ")
                                        {
                                            if (sgc < Convert.ToDecimal(date1[property]))
                                            {
                                                i++;
                                            }
                                            else if (xgc > Convert.ToDecimal(date1[property]))
                                            {
                                                i++;
                                            }
                                            if(date1[property].ToString()!=Convert.ToString(Convert.ToDecimal(date1[property])))
                                            {
                                                i++;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            i++;
                        }
                        if (i == 0)
                        {
                            dates["F_PAEZ_Remarks"] = "OK";
                        }
                        else
                        {
                            dates["F_PAEZ_Remarks"] = "NG";
                        }
                    }
                }
            }
        }
    }
}
