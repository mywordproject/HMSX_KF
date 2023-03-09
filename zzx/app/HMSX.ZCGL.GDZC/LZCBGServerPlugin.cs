using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace HMSX.ZCGL.GDZC
{
    [Description("资产变更--审核是反写单据编号")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class LZCBGServerPlugin: AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FAssetOrgID", "F_260_BGLX", "F_260_ZCZLDDH", "FAlterId" };
            foreach (String property in propertys)
            {
                e.FieldKeys.Add(property);
            }
        }
        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);
            if (FormOperation.Operation.Equals("Audit", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var dates in e.DataEntitys)
                {
                    if (dates["AssetOrgID_Id"].ToString() == "100026" && dates["F_260_BGLX"].ToString()== "资产借出")
                    {
                        string billsql = $@"update PAEZ_t_Cust100334 set FZCJCBGDDH='{dates["BillNo"].ToString()}' where FBILLNO='{dates["F_260_ZCZLDDH"].ToString()}'";
                        DBUtils.Execute(Context, billsql);

                        string gxsql = $@"update PAEZ_t_Cust_Entry100315 SET FISRETURN=1 WHERE FENTRYID IN (
                        SELECT FENTRYID FROM  PAEZ_t_Cust_Entry100315 a
                        inner join PAEZ_t_Cust100334 b on a.FID=b.FID
                        WHERE FBILLNO='{dates["F_260_ZCZLDDH"].ToString()}' AND FISRETURN=0
                        AND FALTERID='{dates["AlterId_Id"].ToString()}')";
                        DBUtils.Execute(Context, gxsql);
                    }
                    else if(dates["AssetOrgID_Id"].ToString() == "100026" && dates["F_260_BGLX"].ToString() == "资产回收")
                    {
                        string billsql = $@"update PAEZ_t_Cust100334 set FZCHSBGDDH='{dates["BillNo"].ToString()}' where FBILLNO='{dates["F_260_ZCZLDDH"].ToString()}'";
                        DBUtils.Execute(Context, billsql);

                        string gxsql = $@"update PAEZ_t_Cust_Entry100315 SET FISRECEIVE=1 WHERE FENTRYID IN (
                        SELECT FENTRYID FROM  PAEZ_t_Cust_Entry100315 a
                        inner join PAEZ_t_Cust100334 b on a.FID=b.FID
                        WHERE FBILLNO='{dates["F_260_ZCZLDDH"].ToString()}' AND FISRECEIVE=0
                        AND FALTERID='{dates["AlterId_Id"].ToString()}')";
                        DBUtils.Execute(Context, gxsql);
                    }
                }
            }
            else if (FormOperation.Operation.Equals("UnAudit", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var dates in e.DataEntitys)
                {
                    if (dates["AssetOrgID_Id"].ToString() == "100026" && dates["F_260_BGLX"].ToString() == "资产借出")
                    {
                        string billsql = $@"update PAEZ_t_Cust100334 set FZCJCBGDDH='' where FBILLNO='{dates["F_260_ZCZLDDH"].ToString()}'";
                        DBUtils.Execute(Context, billsql);

                        string gxsql = $@"update PAEZ_t_Cust_Entry100315 SET FISRETURN=0 WHERE FENTRYID IN (
                        SELECT FENTRYID FROM  PAEZ_t_Cust_Entry100315 a
                        inner join PAEZ_t_Cust100334 b on a.FID=b.FID
                        WHERE FBILLNO='{dates["F_260_ZCZLDDH"].ToString()}' AND FISRETURN=1
                        AND FALTERID='{dates["AlterId_Id"].ToString()}')";
                        DBUtils.Execute(Context, gxsql);
                    }
                    else if (dates["AssetOrgID_Id"].ToString() == "100026" && dates["F_260_BGLX"].ToString() == "资产回收")
                    {
                        string billsql = $@"update PAEZ_t_Cust100334 set FZCHSBGDDH='' where FBILLNO='{dates["F_260_ZCZLDDH"].ToString()}'";
                        DBUtils.Execute(Context, billsql);

                        string gxsql = $@"update PAEZ_t_Cust_Entry100315 SET FISRECEIVE=0 WHERE FENTRYID IN (
                        SELECT FENTRYID FROM  PAEZ_t_Cust_Entry100315 a
                        inner join PAEZ_t_Cust100334 b on a.FID=b.FID
                        WHERE FBILLNO='{dates["F_260_ZCZLDDH"].ToString()}' AND FISRECEIVE=1
                        AND FALTERID='{dates["AlterId_Id"].ToString()}')";
                        DBUtils.Execute(Context, gxsql);
                    }
                }
            }
        }
    }
}
