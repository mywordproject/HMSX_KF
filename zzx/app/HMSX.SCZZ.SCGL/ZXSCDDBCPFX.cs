using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.ComponentModel;

namespace HMSX.SCZZ.SCGL
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("半成品生产订单信息反写成品订单")]
    public class ZXSCDDBCPFX: AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            e.FieldKeys.Add("FMaterialId");
            e.FieldKeys.Add("FQty");
            e.FieldKeys.Add("FSrcBillNo");
            e.FieldKeys.Add("FSrcBillEntrySeq");
        }
        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            long orid = this.Context.CurrentOrganizationInfo.ID;
            base.AfterExecuteOperationTransaction(e);
            if (orid == 100026)
            {
                foreach(DynamicObject entity in e.DataEntitys)
                {
                    DynamicObjectCollection entrys = (DynamicObjectCollection)entity["TreeEntity"];
                    foreach(DynamicObject entry in entrys)
                    {
                        string srcbi = entry["SrcBillNo"].ToString();
                        string srcseq= entry["SrcBillEntrySeq"].ToString();
                        DynamicObject wl = (DynamicObject)entry["MaterialId"];
                        if (srcbi.StartsWith("MO"))
                        {
                            string number = wl["Number"].ToString();
                            string name = wl["Name"].ToString();
                            double qty =Convert.ToDouble(entry["Qty"]);
                            string info = $"{number}({name})-{Math.Round(qty,5)};";
                            string sql = $"/*dialect*/update T_PRD_MOENTRY set F_260_BCPXX=F_260_BCPXX+'{info}' from T_PRD_MO b where T_PRD_MOENTRY.FID=b.FID and b.FBILLNO='{srcbi}' and T_PRD_MOENTRY.FSEQ={srcseq};";
                            DBUtils.Execute(this.Context, sql);
                        }
                    }
                }
            }           
        }
    }
}
