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
    [Description("采购申请单---采购订单")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class CGDDConvertPlugin: AbstractConvertPlugIn
    {
        public override void AfterConvert(AfterConvertEventArgs e)
        {
            base.AfterConvert(e);
            if (Context.CurrentOrganizationInfo.ID == 100026)
            {
                var targetForm = e.TargetBusinessInfo.GetForm();
                if (targetForm.LinkSet == null
                || targetForm.LinkSet.LinkEntitys == null
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
                    DynamicObjectCollection entryDataObjColl = billData["POOrderEntry"] as DynamicObjectCollection;
                    foreach (var entryRow in entryDataObjColl)
                    {
                       string wlqdsql = $@"/*dialect*/select distinct top 1 a.fnumber,F_260_DXGYSWB,b.FMATERIALID 
                                       from  T_ENG_BOM  a
                                       left join T_ENG_BOMCHILD b on a.fid=b.fid
                                       left join t_260_WLEntry zsjWL on zsjWL.FID=a.FID
                                       left join t_BD_Material wl on wl.FMATERIALID=zsjWL.F_260_ZSJWL
                                       left join t_BD_MaterialBase c on b.FMATERIALID=c.FMATERIALID
                                       where wl.fnumber='{entryRow["F_260_TCPDM"]}' and b.FMATERIALID='{entryRow["MaterialId_Id"]}'
                                       order by a.fnumber desc";
                       var wlqd = DBUtils.ExecuteDynamicObject(Context, wlqdsql);
                        if (wlqd.Count > 0)
                        {
                            entryRow["F_260_PORGYS2"] = wlqd[0]["F_260_DXGYSWB"];
                        }                     
                    }
                }
            }
        }
    }
}
