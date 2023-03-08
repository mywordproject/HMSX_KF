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
    [Description("生产订单变更单--反写状态")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class SCDDBGDServicePlugin: AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FDocumentStatus", "FMoNo", "FMoEntrySeq", "FChangeType" };
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
                if (FormOperation.Operation.Equals("Save", StringComparison.OrdinalIgnoreCase) ||
                    FormOperation.Operation.Equals("Submit", StringComparison.OrdinalIgnoreCase)||
                    FormOperation.Operation.Equals("Revoke", StringComparison.OrdinalIgnoreCase) ||
                    FormOperation.Operation.Equals("Audit", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var dates in e.DataEntitys)
                    {
                        foreach(var entry in dates["Entity"] as DynamicObjectCollection)
                        {
                            if (entry["ChangeType"].ToString() == "2")
                            {
                                string zt = dates["DocumentStatus"].ToString() == "A" ? "创建" : dates["DocumentStatus"].ToString() == "B" ? "审核中" : dates["DocumentStatus"].ToString() == "C" ? "已审核" : "暂存";
                                string upsql = $@"/*dialect*/update A set F_260_SSDDBGZT='{zt}' 
                  from T_PRD_MOENTRY A,T_PRD_MO B where B.FID=A.FID AND A.FSEQ='{entry["MoEntrySeq"]}' AND B.FBILLNO='{entry["MoNo"]}'";
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
                        DynamicObject dy = extended.DataEntity;

                        if (dy["PrdOrgId_Id"].ToString() == "100026")
                        {
                            DynamicObjectCollection docPriceEntity = dy["Entity"] as DynamicObjectCollection;
                            foreach (var entry in docPriceEntity)
                            {
                                if (entry["ChangeType"].ToString() == "2")
                                {
                                    string upsql = $@"/*dialect*/update A set F_260_SSDDBGZT='' 
                  from T_PRD_MOENTRY A,T_PRD_MO B where B.FID=A.FID AND A.FSEQ='{entry["MoEntrySeq"]}' AND B.FBILLNO='{entry["MoNo"]}'";
                                    DBUtils.Execute(Context, upsql);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
