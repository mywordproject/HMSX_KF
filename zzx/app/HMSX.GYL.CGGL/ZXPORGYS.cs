using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace HMSX.GYL.CGGL
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("双新POR供应商")]
    public class ZXPORGYS: AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            e.FieldKeys.Add("F_keed_PORGYS");
            e.FieldKeys.Add("F_HMD_Basewlbm");
        }
        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);
            long orid = this.Context.CurrentOrganizationInfo.ID;
            if (orid == 100026)
            {
                foreach(DynamicObject entity in e.DataEntitys)
                {
                    long wlid =Convert.ToInt64(entity["F_HMD_Basewlbm_Id"]);
                    DynamicObjectCollection gys =entity["F_keed_PORGYS"] as DynamicObjectCollection;
                    string delsql = $"/*dialect*/delete keed_t_Cust_Entry100353 where FMATERIALID={wlid}";
                    string usql = @"/*dialect*/update T set FPKID=100000+num 
                        from(select FPKID, ROW_NUMBER() over(order by FPKID) num from keed_t_Cust_Entry100353) a
                        inner join keed_t_Cust_Entry100353 T on a.FPKID = T.FPKID";
                    DBUtils.Execute(this.Context, delsql);//清空
                    DBUtils.Execute(this.Context, usql);//更新FPKID
                    string sql = "/*dialect*/select MAX(FPKID) ID from keed_t_Cust_Entry100353";
                    long maxid = DBUtils.ExecuteScalar<long>(this.Context, sql,100000);
                    string values = "";
                    foreach (DynamicObject obj in gys)
                    {
                        var id = obj["F_keed_PORGYS_Id"];                                               
                        maxid++;
                        values += $"({maxid},{wlid},{id}),";
                                             
                    }                   
                    if (values != "")
                    {
                        values = values.Substring(0, values.Length - 1);
                        string isql = "/*dialect*/insert into keed_t_Cust_Entry100353(FPKID,FMATERIALID,F_KEED_PORGYS) values"+values;
                        DBUtils.Execute(this.Context, isql);
                    }
                }
            }
        }
    }
}
