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

namespace HMSX.Second.Plugin.生产制造
{
    [Description("发货通知单--其他出库")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class QTCKConvertPlugin : AbstractConvertPlugIn
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
                    string cxsql = $@"select FEXCHANGERATE from T_BD_Rate
                                               where FRATETYPEID='{billData["F_260_HLLX_Id"]}'
                                               and FCYTOID='{billData["F_260_BWB_Id"]}'
                                               and FCYFORID='{billData["F_260_JSBB_Id"]}'
                                               and FBEGDATE<='{billData["Date"]}'
                                               and FENDDATE>='{billData["Date"]}'";
                    var cx = DBUtils.ExecuteDynamicObject(Context, cxsql);
                    if (cx.Count > 0)
                    {
                        billData["F_260_HL"] =Convert.ToDouble(cx[0]["FEXCHANGERATE"]);
                    }
                }
            }
        }
    }
}
