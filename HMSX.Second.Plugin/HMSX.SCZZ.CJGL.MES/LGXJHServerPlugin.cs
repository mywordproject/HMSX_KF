using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace HMSX.SCZZ.CJGL.MES
{
    [Description("工序计划--保存时将订单是否npi携带下来")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class LGXJHServerPlugin : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FMONumber", "FMOEntrySeq" , "FProOrgId" };
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
                    if (dates["ProOrgId_Id"].ToString() == "100026")
                    {
                        string cxsql = $@"select 
                         CASE WHEN C.FNUMBER='SCDD05_SYS' THEN '普通'  WHEN C.FNUMBER='SCDD02_SYS' THEN '返工' else '其他' END LX,
                         FBILLNO,FSEQ,F_260_SFNPI,FSTOCKID
                         from T_PRD_MO a
                         inner join T_PRD_MOENTRY b on a.fid=b.fid
                         inner join T_BAS_BILLTYPE c on c.FBILLTYPEID=A.FBILLTYPE
                         where
                         substring(A.FBILLNO,1,2)='MO'
                         and FPRDORGID=100026
                         and FBILLNO='{dates["MONumber"]}' and FSEQ={dates["MOEntrySeq"]}";
                        var cx = DBUtils.ExecuteDynamicObject(Context, cxsql);
                        if (cx.Count > 0)
                        {
                            string upsql = $@"/*dialect*/update T_SFC_OPERPLANNING set 
                                           F_260_SFNPI='{cx[0]["F_260_SFNPI"]}',F_260_CK={cx[0]["FSTOCKID"]},F_260_DDLX='{cx[0]["LX"]}'
                                           where FID={dates["Id"]}";
                            DBUtils.Execute(Context, upsql);
                        }
                    }
                }
            }
        }
    }
}

