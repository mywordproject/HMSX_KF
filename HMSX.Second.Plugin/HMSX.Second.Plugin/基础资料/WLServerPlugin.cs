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

namespace HMSX.Second.Plugin
{
    [Description("物料--审核时，如果07，更新生产类型")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class WLServerPlugin : AbstractOperationServicePlugIn
    {

        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FNumber", "FUseOrgId", "FWorkShopId"};
            foreach (String property in propertys)
            {
                e.FieldKeys.Add(property);
            }
        }
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            foreach (var date in e.DataEntitys)
            {
                if (date["UseOrgId_Id"].ToString() == "100026")
                {
                    DynamicObjectCollection entrydates = (DynamicObjectCollection)date["MaterialProduce"];
                    foreach (var entrydate in entrydates)
                    {
                        //生产车间=模具车间，物料名称206.07
                        // string x = date["Number"].ToString().Substring(0,6);
                        //bool y = (date["Number"].ToString()).Contains("260.07");
                        string number = entrydate["WorkShopId"] == null ? null : ((DynamicObject)entrydate["WorkShopId"])["Number"].ToString();
                        if(date["Number"].ToString().Contains("260.07") && number=="000362")                           
                        {                                              
                           string upsql = $@"update t_BD_MaterialProduce set FPRODUCEBILLTYPE=(select FBILLTYPEID from T_BAS_BILLTYPE 
                           WHERE FNUMBER='SCDD09_SYS')  where FMATERIALID='{date["Id"]}'";
                            DBUtils.Execute(Context, upsql);                     
                        }
                    }
                }
            }
        }

    }
}
