using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.供应链
{
    [Description("预测单")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class YCDConvertPlugin: AbstractConvertPlugIn
    {
        public override void AfterConvert(AfterConvertEventArgs e)
        {
            base.AfterConvert(e);
            if (Context.CurrentOrganizationInfo.ID == 100026)
            {
                var targetForm = e.TargetBusinessInfo.GetForm();
                if (targetForm.LinkSet == null || targetForm.LinkSet.LinkEntitys == null
                || targetForm.LinkSet.LinkEntitys.Count == 0)
                {
                    //目标单未设置关联主实体，无法获取目标单的源单信息，携带不了
                    throw new KDBusinessException("", "未设置关联实体请设置");
                }
                //单据整体数据包
                var billDataObjs = e.Result.FindByEntityKey("FBillHead");
                foreach (var billObj in billDataObjs)
                {
                    DynamicObject billData = billObj.DataEntity;
                    //明细数据包
                    DynamicObjectCollection entryDataObjColl = billData["SaleOrderEntry"] as DynamicObjectCollection;
                    foreach (var entryRow in entryDataObjColl)
                    {
                        var x = entryRow["FSCREntryID"];
                        var y = entryRow["SrcBillNo"];
                        string ycdsql = $@"select 
                         case when C.FNUMBER='SCHC03_YDJH' and F_AI_FORECASTTYPE='ProductSpare' then 0 else 1 end APS 
                         from T_PLN_FORECAST a
                         INNER JOIN T_BAS_BILLTYPE C ON a.FBILLTYPEID=C.FBILLTYPEID
                         WHERE FNUMBER IN('YCD01_SYS','SCHC03_YDJH')
                         and FBILLNO='{entryRow["SrcBillNo"]}'";
                        var ycd = DBUtils.ExecuteDynamicObject(Context, ycdsql);
                        if (ycd.Count > 0)
                        {
                            entryRow["F_AI_ReadToAPS"] = ycd[0]["APS"];
                        }
                    }
                }
            }
        }
    }
}
