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
    [Description("库存形态转换单审核时--反写单号")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class KCXTZHServerPlugin: AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FStockOrgId", "F_260_BGDBM" , "FBillNo", "FMaterialId" };
            foreach (String property in propertys)
            {
                e.FieldKeys.Add(property);
            }
        }

        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            if (FormOperation.Operation.Equals("Audit", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var dates in e.DataEntitys)
                {
                    if (dates["StockOrgId_Id"].ToString() == "100026")
                    {
                        if (dates["F_260_BGDBM"] == null ? false : dates["F_260_BGDBM"].ToString() == "" ? false : true)
                        {
                            string number = "";
                            foreach (var date in dates["StockConvertEntry"] as DynamicObjectCollection)
                            {
                                number = ((DynamicObject)date["MaterialId"])["Number"].ToString();
                            }
                            string upsql = $@"update T_PLM_STD_EC_ITEM set F_260_KCZHD='{dates["FBillNo"]}' 
                            where FPARENTROWID='' and FOBJECTCODE='{number}'
                            and FID in (select FID from T_PLM_PDM_BASE where FCODE='{dates["F_260_BGDBM"]}') ";
                            DBUtils.Execute(Context, upsql);
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
                        if (dates["F_260_BGDBM"] == null ? false : dates["F_260_BGDBM"].ToString() == "" ? false : true)
                        {
                            string upsql = $@"update T_PLM_STD_EC_ITEM set F_260_KCZHD='' 
                            where F_260_KCZHD='{dates["FBillNo"]}'";
                            DBUtils.Execute(Context, upsql);
                        }
                    }
                }
            }
        }
    }
}
