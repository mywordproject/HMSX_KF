using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.ComponentModel;

namespace HMSX.SCZZ.SCGL
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("双新检验单判断是否有例外采购领料")]
    public class ZXJYDLWLLPD : AbstractOperationServicePlugIn
    {       
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            e.FieldKeys.Add("F_260_PGMXTM");
        }
        public override void BeginOperationTransaction(BeginOperationTransactionArgs e)
        {
            base.BeginOperationTransaction(e);
            if (this.Context.CurrentOrganizationInfo.ID==100026)
            {
                foreach (DynamicObject entity in e.DataEntitys)
                {
                    try
                    {
                        DynamicObjectCollection entrys = (DynamicObjectCollection)entity["Entity"];
                        string lx = entity["BusinessType"].ToString();
                        string zt = entity["DocumentStatus"].ToString();
                        if (zt == "Z" && lx == "5")
                        {
                            foreach (DynamicObject entry in entrys)
                            {
                                string pgtm = entry["F_260_PGMXTM"].ToString();
                                int pgid = Convert.ToInt32(pgtm.Split('-')[1]);
                                string sql = $@"/*dialect*/select F_260_LWCG from (select 物料ID,批号ID
                                     from(select FMATERIALID 物料ID,FLOT 批号ID,FACTUALQTY 数量 from T_PRD_PICKMTRLDATA where F_RUJP_PGENTRYID={pgid}
                                     union all              
                                     select FMATERIALID 物料ID,FLOT 批号ID,FACTUALQTY 数量 from T_PRD_FEEDMTRLDATA a 
                                     inner join T_PRD_FEEDMTRLDATA_Q q on a.FENTRYID=q.FENTRYID where F_RUJP_PGENTRYID={pgid}
                                     union all
                                     select FMATERIALID 物料ID,FLOT 批号ID,-FQTY 数量 from T_PRD_RETURNMTRLENTRY where F_RUJP_PGENTRYID={pgid})A
                                     group by 物料ID,批号ID
                                     having SUM(数量)>0)B
                                     inner join T_STK_INSTOCKENTRY rk on rk.FMATERIALID=B.物料ID and rk.FLOT=B.批号ID
                                     inner join t_PUR_POOrder dd on rk.FPOORDERNO=dd.FBILLNO and F_260_LWCG=1";
                                DynamicObjectCollection objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
                                if (objs.Count > 0)
                                {
                                    entry["F_260_LWCGLL"] = 1;
                                }
                            }
                        }
                    }
                    catch { return; }
                }
            }           
        }
    }
}
