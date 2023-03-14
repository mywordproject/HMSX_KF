using Kingdee.BOS;
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

namespace HMSX.SCZZ.MJGL
{
    [Description("工序汇报--保存、提交、审核时是否关账")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class LGXHBServerPlugin: AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FMaterialId", "FDate" };
            foreach (String property in propertys)
            {
                e.FieldKeys.Add(property);
            }
        }
        public override void BeforeExecuteOperationTransaction(BeforeExecuteOperationTransaction e)
        {
            base.BeforeExecuteOperationTransaction(e);
            if (FormOperation.Operation.Equals("Save", StringComparison.OrdinalIgnoreCase)||
                FormOperation.Operation.Equals("Submit", StringComparison.OrdinalIgnoreCase)||
                FormOperation.Operation.Equals("Audit", StringComparison.OrdinalIgnoreCase))
            {
                foreach (ExtendedDataEntity extended in e.SelectedRows)
                {
                    DynamicObject dy = extended.DataEntity;
                    DateTime djrq=Convert.ToDateTime(dy["FDate"].ToString());
                    string year = djrq.Year.ToString();
                    string month = djrq.Month.ToString();
                    if (dy["PrdOrgId_Id"].ToString() == "100026")
                    {
                        DynamicObjectCollection docPriceEntity = dy["OptRptEntry"] as DynamicObjectCollection;
                        foreach (var entry in docPriceEntity)
                        {
                            if (((DynamicObject)entry["MaterialId"])["Number"].ToString().Substring(0, 6) == "260.07")
                            {
                                //关账日期
                                string gzrqsql = $@"SELECT top 1 FCLOSEDATE FROM T_STK_CLOSEPROFILE SCP 
                                WHERE SCP.FCATEGORY = 'HS' AND SCP.FORGID =100026 order by FCLOSEDATE DESC";
                                var gzrq = DBUtils.ExecuteDynamicObject(Context, gzrqsql);
                                if (gzrq.Count > 0)
                                {
                                    if(Convert.ToDateTime(gzrq[0]["FCLOSEDATE"].ToString()).Year.ToString()==year &&
                                       Convert.ToDateTime(gzrq[0]["FCLOSEDATE"].ToString()).Month.ToString() == month)
                                    {
                                        throw new KDBusinessException("", "已关账，不允许操作！");
                                    }
                                }                                                                     
                            }
                        }
                    }
                }
            }
        }
    }
}
