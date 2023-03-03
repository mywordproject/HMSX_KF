using Kingdee.BOS;
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

namespace HMSX.Second.Plugin.PLM
{
    [Description("变更单--状态转换")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class BGDServerPlugincs : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FChargeUserId", "FCreatorId"};
            foreach (String property in propertys)
            {
                e.FieldKeys.Add(property);
            }
        }
        public override void BeforeExecuteOperationTransaction(BeforeExecuteOperationTransaction e)
        {          
            if (FormOperation.Operation.Equals("PLMOP_1054_AJ", StringComparison.OrdinalIgnoreCase) ||
                FormOperation.Operation.Equals("PLMOP_1054_AL", StringComparison.OrdinalIgnoreCase) ||
                FormOperation.Operation.Equals("PLMOP_1054_AK", StringComparison.OrdinalIgnoreCase)||
                FormOperation.Operation.Equals("PLMOP_1054_AI", StringComparison.OrdinalIgnoreCase))
            {
                if (Context.CurrentOrganizationInfo.ID == 100026)
                {
                    foreach(var date in e.DataEntitys)
                    {
                        if (Context.UserId ==Convert.ToInt64(date["CreatorId_Id"]) || Context.UserId == Convert.ToInt64(date["ChargeUserId_Id"]))
                        {
                            throw new KDBusinessException("", "单据仅创建人和负责人才能转换状态！！！");
                        }
                    }                   
                }
            }
            base.BeforeExecuteOperationTransaction(e);
        }
        public override void OnAddValidators(AddValidatorsEventArgs e)
        {
            base.OnAddValidators(e);
        }
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            //if (Context.CurrentOrganizationInfo.ID == 100026)
            //{
            //    if (FormOperation.Operation.Equals("DoNothingS", StringComparison.OrdinalIgnoreCase) && Context.CurrentOrganizationInfo.ID == 100026)
            //    {
            //        foreach (var dates in e.DataEntitys)
            //        {
            //            foreach (var entity in dates["ChangeObjectEntity"] as DynamicObjectCollection)
            //            {
            //                // if (entity["ObjectType"].ToString() == "5" && entity["AssignText"].ToString()==Context.UserName)
            //                // {
            //                //     string cxsql = $@"SELECT  FLIFECIRCLESTAGE FROM T_PLM_PDM_BASE WHERE FCODE='{entity["ObjectCode"]}' and FVERNO='{entity["ObjectNewVerNo"]}'";
            //                //     var cx = DBUtils.ExecuteDynamicObject(Context, cxsql);
            //                //     if (cx.Count > 0)
            //                //     {
            //                //         if(cx[0]["FLIFECIRCLESTAGE"].ToString()!= "AC")
            //                //         {
            //                throw new KDBusinessException("", "当前变更BOM未归档,请归档后再提交！");
            //                //         }
            //                //     }
            //                // }
            //            }
            //        }
            //    }
            //}
        }
    }
}
