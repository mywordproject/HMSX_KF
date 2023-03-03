using HMSX.Second.Plugin.Tool;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.供应链
{
    [Description("生产订单--返工订单加载")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class KCZTZHConvertPlugin : AbstractConvertPlugIn
    {
        string str = "";
        public override void OnCreateTarget(CreateTargetEventArgs e)
        {
            base.OnCreateTarget(e);
        }
        public override void OnInSelectedRow(InSelectedRowEventArgs e)
        {
            base.OnInSelectedRow(e);
            if (Context.CurrentOrganizationInfo.ID == 100026)
            {
                str = e.InSelectedRowsSQL;
                int x = str.IndexOf("(");
                int z = str.Length;
                str = str.Substring(x, z - x);
            }
        }
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
                    DynamicObjectCollection entryDataObjColl = billData["TreeEntity"] as DynamicObjectCollection;
                    foreach (var entryRow in entryDataObjColl)
                    {
                        string kczhsql = $@"/*dialect*/select b.FMATERIALID,
                          (SELECT distinct convert(varchar(255),FSEQ,111)+',' FROM T_STK_StockConvertEntry 
                          WHERE 
                          T_STK_StockConvertEntry.FMATERIALID=B.FMATERIALID and b.FMATERIALID ={entryRow["MaterialId_Id"]} 
                          and fentryid in {str}  and  FCONVERTTYPE='B'
                          for xml path('')) as HH
                           from T_STK_StockConvert a
                          inner join T_STK_StockConvertEntry b on a.fid=b.FID
                          left join  SLSB_t_Cust_Entry100350 c on b.fentryid=c.fentryid
                          where 
                          FCONVERTTYPE='B'
                          and b.FMATERIALID ={entryRow["MaterialId_Id"]} 
                          and b.fentryid in {str}
                          group by b.FMATERIALID";
                        var kczhs = DBUtils.ExecuteDynamicObject(Context, kczhsql);
                        entryRow["Group"] = entryRow["Seq"];
                        entryRow["RowId"] = SequentialGuid.NewGuid().ToString();
                        entryRow["F_260_HH"] = kczhs[0]["HH"].ToString().Trim(',');
                    }
                }
            }
        }
    }
}
