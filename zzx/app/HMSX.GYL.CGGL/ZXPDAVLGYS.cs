using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.ComponentModel;

namespace HMSX.GYL.CGGL
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("双新供应商评分表_是否合格供应商更新")]
    public class ZXPDAVLGYS: AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            e.FieldKeys.Add("F_260_HGGYS");
            e.FieldKeys.Add("F_keed_Combo");
        }
        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);
            long orid = this.Context.CurrentOrganizationInfo.ID;
            if (orid == 100026)
            {
                foreach(DynamicObject entity in e.DataEntitys)
                {
                    DynamicObjectCollection entrys =(DynamicObjectCollection)entity["HMD_HMSX_SYSQEntry"];
                    foreach(DynamicObject entry in entrys)
                    {
                        long gys =Convert.ToInt64(entry["F_260_HGGYS_Id"]);
                        string sf = entry["F_keed_Combo"].ToString();
                        if (sf != " ")
                        {
                            string sql = $"/*dialect*/update t_BD_Supplier set F_260_SFAVL='{sf}' where FSUPPLIERID={gys}";
                            DBUtils.Execute(this.Context, sql);
                        }                       
                    }
                }
            }
        }
    }
}
