using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.供应链
{
    [Description("采购申请--带出项目号")]
    [Kingdee.BOS.Util.HotUpdate]
    public class CGSQBillPlugin: AbstractBillPlugIn
    {
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (e.Field.Key == "F_260_Base_CP")
            {
                string cp = this.Model.GetValue("F_260_BASE_CP", e.Row) == null ? "" : ((DynamicObject)this.Model.GetValue("F_260_BASE_CP", e.Row))["Id"].ToString();
                string cxsql = $@"select 
                                        XMH.F_260_XMH,XMH.FPKID
                                        from T_BD_MATERIAL a
                                        left join t_BD_MaterialPlan c on c.FMATERIALID=a.FMATERIALID
                                        left join T_PLN_MANUFACTUREPOLICY d on c.FMFGPOLICYID=d.FID
                                        LEFT JOIN PAEZ_t_Cust_Entry100355 XMH ON XMH.FMATERIALID=A.FMATERIALID
                                        WHERE 
                                        --D.FNUMBER='ZZCL003_SYS'
                                        --and 
                                        a.FMATERIALID='{cp}'
                                        and FCREATEORGID=100026
                                        and XMH.F_260_XMH is not null
                                        order by XMH.FPKID DESC";
                var cxs = DBUtils.ExecuteDynamicObject(Context, cxsql);
                if (cxs.Count > 0)
                {
                    this.Model.SetItemValueByID("F_260_BASE_CPXMDM", Convert.ToInt64(cxs[0]["F_260_XMH"]), e.Row);
                }
            }
        }
    }
}
