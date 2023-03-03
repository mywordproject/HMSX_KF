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

namespace HMSX.Second.Plugin.供应链
{
    [Description("采购退料单--校验、重置")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class CGTLServicePlugin: AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = {"FSrcBillNo", "FSRCBillTypeId" };
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
                if (FormOperation.Operation.Equals("Audit", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var date in e.DataEntitys)
                    {
                        foreach(var entry in date["PUR_MRBENTRY"]as DynamicObjectCollection)
                        {
                            if (entry["SRCBillTypeId"].ToString() == "PUR_ReceiveBill")
                            {
                                string upsql2 = $@"/*dialect*/update T_PUR_Receive set F_260_XTLY='' where FBILLNO='{entry["SrcBillNo"]}'";
                                DBUtils.Execute(Context, upsql2);
                            }
                        }
                    }
                }
                else if (FormOperation.Operation.Equals("UnAudit", StringComparison.OrdinalIgnoreCase))
                {              
                    foreach (var date in e.DataEntitys)
                    {
                        foreach (var entry in date["PUR_MRBENTRY"] as DynamicObjectCollection)
                        {
                            if (entry["SRCBillTypeId"].ToString() == "PUR_ReceiveBill")
                            {
                                string upsql2 = $@"/*dialect*/update T_PUR_Receive set F_260_XTLY='' where FBILLNO='{entry["SrcBillNo"]}'";
                                DBUtils.Execute(Context, upsql2);
                            }
                        }
                    }
                }
            }
        }
    }
}
