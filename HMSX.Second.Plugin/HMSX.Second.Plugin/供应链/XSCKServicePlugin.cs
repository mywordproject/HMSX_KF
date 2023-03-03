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
    [Description("销售出库--反写出库数量")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class XSCKServicePlugin: AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FMustQty", "FSoorDerno", "FSOEntryId"};
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
                        foreach(var entry in date["SAL_OUTSTOCKENTRY"] as DynamicObjectCollection)
                        {
                            try
                            {
                                string upsql = $@"/*dialect*/
                            update T_PLN_FORECASTENTRY set F_260_SETTLEMENTQTY=F_260_SETTLEMENTQTY+{Convert.ToDouble(entry["MustQty"])}
                            where fentryid in(
                            select FSCRENTRYID from T_SAL_ORDER  a
                            left join T_SAL_ORDERENTRY b on b.fid=a.FID
                            left join T_SAL_ORDERENTRY_R c on c.fentryid=b.fentryid 
                            WHERE FSRCBILLNO like 'FO%' and FSALEORGID=100026 
                            and FBILLNO='{entry["SoorDerno"]}' and b.fentryid='{entry["SOEntryId"]}'
                            and FSRCTYPE='PLN_FORECAST' )";
                                DBUtils.Execute(Context, upsql);
                            }
                            catch
                            {

                            }
                            finally
                            {

                            }
                            
                        }
                    }
                }
                else if (FormOperation.Operation.Equals("UnAudit", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var date in e.DataEntitys)
                    {
                        foreach (var entry in date["SAL_OUTSTOCKENTRY"] as DynamicObjectCollection)
                        {
                            try
                            {
                                string upsql = $@"/*dialect*/
                                update T_PLN_FORECASTENTRY set F_260_SETTLEMENTQTY=F_260_SETTLEMENTQTY-{Convert.ToDouble(entry["MustQty"])}
                                where fentryid in(
                                select FSCRENTRYID from T_SAL_ORDER  a
                                left join T_SAL_ORDERENTRY b on b.fid=a.FID
                                left join T_SAL_ORDERENTRY_R c on c.fentryid=b.fentryid 
                                WHERE FSRCBILLNO like 'FO%' and FSALEORGID=100026 
                                and FBILLNO='{entry["SoorDerno"]}' and b.fentryid='{entry["SOEntryId"]}'
                                and FSRCTYPE='PLN_FORECAST' )";
                                DBUtils.Execute(Context, upsql);
                            }
                            catch
                            {

                            }
                            finally
                            {

                            }

                        }
                    }
                }
            }           
        }
    }
}
