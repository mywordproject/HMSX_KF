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
    [Description("直接调拨单--审核时计算调拨申请差异数")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class ZJDBServerPlugin : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FSrcBillNo", "FSrcSeq", "FStockOutOrgId" , "FDestStockId" };
            foreach (String property in propertys)
            {
                e.FieldKeys.Add(property);
            }
        }
        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);
            if (Context.CurrentOrganizationInfo.ID == 100026)
            {
                if (FormOperation.Operation.Equals("Audit", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var date in e.DataEntitys)
                    {
                        int i = 0;
                        foreach (var entry in date["TransferDirectEntry"] as DynamicObjectCollection)
                        {
                            string upsql = $@"/*dialect*/update T_STK_STKTRANSFERAPPENTRY set F_260_CYS=AA.CYS FROM 
                                      (select FQTY-FNOTRANSOUTBASEQTY CYS,a.FENTRYID
                                      from T_STK_STKTRANSFERAPPENTRY a
                                      inner join T_STK_STKTRANSFERAPPENTRY_E c on c.fentryid=a.fentryid 
                                      inner join T_STK_STKTRANSFERAPP b on a.fid=b.fid
                                      WHERE a.FSEQ='{entry["SrcSeq"].ToString()}' AND b.FBILLNO='{entry["SrcBillNo"].ToString()}'
                                      )AA WHERE AA.FENTRYID=T_STK_STKTRANSFERAPPENTRY.FENTRYID";
                            DBUtils.Execute(Context, upsql);
                            if (entry["SrcStockId"] != null)
                            {
                                if (((DynamicObject)entry["SrcStockId"])["Name"].ToString().Contains("WMS")||
                                    ((DynamicObject)entry["DestStockId"])["Name"].ToString().Contains("WMS"))
                                    //((DynamicObject)entry["SrcStockId"])["Number"].ToString() == "260CK091" ||
                                    //((DynamicObject)entry["SrcStockId"])["Number"].ToString() == "260CK092" ||
                                    //((DynamicObject)entry["SrcStockId"])["Number"].ToString() == "260CK093" ||
                                    //((DynamicObject)entry["SrcStockId"])["Number"].ToString() == "260CK067" ||
                                    //((DynamicObject)entry["SrcStockId"])["Number"].ToString() == "260CK057" ||
                                    //((DynamicObject)entry["SrcStockId"])["Number"].ToString() == "260CK028"  ||
                                    //
                                    //((DynamicObject)entry["DestStockId"])["Number"].ToString() == "260CK091" ||
                                    //((DynamicObject)entry["DestStockId"])["Number"].ToString() == "260CK092" ||
                                    //((DynamicObject)entry["DestStockId"])["Number"].ToString() == "260CK093" ||
                                    //((DynamicObject)entry["DestStockId"])["Number"].ToString() == "260CK067" ||
                                    //((DynamicObject)entry["DestStockId"])["Number"].ToString() == "260CK057" ||
                                    //((DynamicObject)entry["DestStockId"])["Number"].ToString() == "260CK028")
                                {
                                    i = 1;
                                }
                            }
                        }
                        if (i == 1)
                        {
                            string upsql1 = $@"/*dialect*/update T_STK_STKTRANSFERIN set F_260_SFWMSCK=1 where FID={date["Id"]}";
                            DBUtils.Execute(Context, upsql1);
                        }
                    }
                }
                if (FormOperation.Operation.Equals("UnAudit", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var date in e.DataEntitys)
                    {
                        foreach (var entry in date["TransferDirectEntry"] as DynamicObjectCollection)
                        {
                            string upsql = $@"/*dialect*/update T_STK_STKTRANSFERAPPENTRY set F_260_CYS=AA.CYS FROM 
                                      (select FQTY-FNOTRANSOUTBASEQTY CYS,a.FENTRYID
                                      from T_STK_STKTRANSFERAPPENTRY a
                                      inner join T_STK_STKTRANSFERAPPENTRY_E c on c.fentryid=a.fentryid 
                                      inner join T_STK_STKTRANSFERAPP b on a.fid=b.fid
                                      WHERE a.FSEQ='{entry["SrcSeq"].ToString()}' AND b.FBILLNO='{entry["SrcBillNo"].ToString()}'
                                      )AA WHERE AA.FENTRYID=T_STK_STKTRANSFERAPPENTRY.FENTRYID";
                            DBUtils.Execute(Context, upsql);
                        }
                    }
                }
            }
        }
    }
}
