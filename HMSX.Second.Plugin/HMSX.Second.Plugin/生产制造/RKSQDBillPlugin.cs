using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.生产制造
{
    [Description("入库申请--项目号过滤")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class RKSQDBillPlugin : AbstractBillPlugIn
    {
        public override void BeforeF7Select(BeforeF7SelectEventArgs e)
        {
            base.BeforeF7Select(e);
            if (this.Context.CurrentOrganizationInfo.ID == 100026)
            {
                if (e.FieldKey.EqualsIgnoreCase("F_260_XMHH"))
                {
                    string wl = this.Model.GetValue("FMATERIALID", e.Row) == null ? "" : ((DynamicObject)this.Model.GetValue("FMATERIALID", e.Row))["Id"].ToString();
                    string cxsql = $@"select 
                                        XMH.F_260_XMH
                                        from T_BD_MATERIAL a
                                        left join t_BD_MaterialPlan c on c.FMATERIALID=a.FMATERIALID
                                        left join T_PLN_MANUFACTUREPOLICY d on c.FMFGPOLICYID=d.FID
                                        LEFT JOIN PAEZ_t_Cust_Entry100355 XMH ON XMH.FMATERIALID=A.FMATERIALID
                                        WHERE 
                                        --D.FNUMBER='ZZCL003_SYS'
                                        --and 
                                        a.FMATERIALID='{wl}'
                                        and FCREATEORGID=100026
                                       and XMH.F_260_XMH is not null";
                    var cxs = DBUtils.ExecuteDynamicObject(Context, cxsql);
                    string str = "";
                    foreach (var cx in cxs)
                    {
                        str += cx["F_260_XMH"].ToString() + ",";
                    }
                    string xmh = "FID=0";
                    if (cxs.Count > 0)
                    {
                        xmh = "FID" + " in (" + str.Trim(',') + " )";
                    }

                    e.ListFilterParameter.Filter = e.ListFilterParameter.Filter.JoinFilterString(xmh);
                    return;
                }
            }
        }
        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);
            if (this.Context.CurrentOrganizationInfo.ID == 100026)
            {
                //if (e.BarItemKey.Equals("tbSplitSave") || e.BarItemKey.Equals("tbSave"))
                //{
                //    this.View.UpdateView("FEntity");
                //    foreach(var date in this.Model.DataObject["BillEntry"] as DynamicObjectCollection)
                //    {
                //        this.View.UpdateView("F_260_XMHH", Convert.ToInt32(date["Seq"])-1);
                //        this.View.UpdateView("F_260_JHGZHBM", Convert.ToInt32(date["Seq"])-1);
                //        this.View.UpdateView("FMtoNo", Convert.ToInt32(date["Seq"])-1);
                //    }                
                //}
            }
        }
        public override void AfterDoOperation(AfterDoOperationEventArgs e)
        {
            base.AfterDoOperation(e);
            string opt = e.Operation.Operation;           
            if (Context.CurrentOrganizationInfo.ID == 100026)
            {
                if (e.Operation.OperationId == 8 && e.Operation.Operation== "Save" && e.OperationResult.IsSuccess)
                {                 
                    this.View.InvokeFormOperation("Refresh");
                    //            // 保存后刷新字段
                    //            var loadKeys = e.Operation.ReLoadKeys == null ? new List<string>() : new List<string>(e.Operation.ReLoadKeys);
                    //            ((IBillModel)this.Model).SynDataFromDB(loadKeys);
                }
            }
        }
    }
}
