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

namespace HMSX.Second.Plugin.供应链
{
    [Description("采购订单--保存时将当前日期反写")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class CGDDServicePlugin : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FBillNo", "FSeq", "FCreateDate" };
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
                if (FormOperation.Operation.Equals("Save", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var date in e.DataEntitys)
                    {
                        foreach (var entity in date["POOrderEntry"] as DynamicObjectCollection)
                        {
                            string cxsql = $@"/*dialect*/select A.FBILLNO,b.FSEQ from T_SUB_REQORDER a
                                            inner join T_SUB_REQORDERENTRY b on a.fid=b.fid
                                            inner join T_PUR_POORDERENTRY_LK c on c.FSBILLID=a.FID and c.FSID=b.FENTRYID
                                            inner join t_PUR_POOrderEntry d on d.fentryid=c.fentryid
                                            inner join t_PUR_POOrder e on e.fid=d.fid
                                            where e.fbillno='{date["BillNo"]}'
                                            and d.FSEQ={entity["Seq"]}";
                            var cx = DBUtils.ExecuteDynamicObject(Context, cxsql);
                            if (cx.Count > 0)
                            {
                                string upsql = $@"/*dialect*/update T_SUB_PPBOM set F_260_CGDDCJRQ='{date["CreateDate"]}' where FSUBBILLNO='{cx[0]["FBILLNO"]}' and FSUBREQENTRYSEQ={cx[0]["FSEQ"]}";
                                DBUtils.Execute(Context, upsql);
                            }
                        }
                    }
                }
               
            }
        }
        public override void BeforeExecuteOperationTransaction(BeforeExecuteOperationTransaction e)
        {
            base.BeforeExecuteOperationTransaction(e);
            if (Context.CurrentOrganizationInfo.ID == 100026)
            {               
                if (FormOperation.Operation.Equals("Delete", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (ExtendedDataEntity extended in e.SelectedRows)
                    {
                        DynamicObject date = extended.DataEntity;
                        foreach (var entity in date["POOrderEntry"] as DynamicObjectCollection)
                        {
                            string upsql = $@"/*dialect*/update T_SUB_PPBOM set F_260_CGDDCJRQ=null where FPURORDERNO='{date["BillNo"]}' and FPURORDERENTRYSEQ={entity["Seq"]}";
                            DBUtils.Execute(Context, upsql);
                        }
                    }
                }
            }
        }
    }
}
