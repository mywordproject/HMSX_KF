using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.CommonFilter;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List;
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
    [Description("调拨申请单")]
    [HotUpdate]
    public class DBSQBillPlugin : AbstractBillPlugIn
    {
        public override void AfterEntryBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterEntryBarItemClick(e);
            if (e.BarItemKey.Equals("F_KCCX"))
            {
                ListShowParameter listShowParameter = new ListShowParameter();
                //FormId你要调用那个单据的列表,通过打开未扩展的销售订单,找到唯一标识     
                listShowParameter.FormId = "STK_Inventory";
                //IsLookUp弹出的列表界面是否有“返回数据”按钮
                listShowParameter.IsLookUp = true;
                listShowParameter.CustomParams.Add("NeedReturnData", "1");
                listShowParameter.CustomParams.Add("IsFromQuery", "True");
                listShowParameter.CustomParams.Add("QueryMode", "1");
                listShowParameter.CustomParams.Add("QueryPage", "50");
                listShowParameter.CustomParams.Add("DBSQ", "1");
                int hs = this.Model.GetEntryCurrentRowIndex("FEntity");
                var wl = this.Model.GetValue("FMATERIALID", hs) == null ? "" : ((DynamicObject)this.Model.GetValue("FMATERIALID", hs))["Id"].ToString();
                var ck = this.Model.GetValue("FSTOCKID", hs) == null ? "" : ((DynamicObject)this.Model.GetValue("FSTOCKID", hs))["Id"].ToString();
                string gys = this.Model.GetValue("F_260_DXGYS", hs) == null ? "" : this.Model.GetValue("F_260_DXGYS", hs).ToString();
                if (wl != "")
                {
                    string gl = "FSTOCKORGID=100026 and FMATERIALID=" + wl;
                    string flotid = "";
                    if (gys != "")
                    {
                        string gysname = "";
                        foreach (var sup in gys.ToString().Split(','))
                        {
                            gysname += "'" + sup + "',";
                        }
                        string strsql = $@"select  a.fid,A.FMATERIALID,FLOT,c.FNAME from T_STK_INVENTORY a
                                     left join T_BD_LOTMASTER b on a.FLOT= b.FLOTid
                                     left join T_BD_SUPPLier_l c on c.FSUPPLIERID=b.FSUPPLYID
                                     where FSTOCKORGID=100026 and a.FMATERIALID='{wl}'
                                     and (c.Fname in ({gysname.Trim(',')}))";
                        var strs = DBUtils.ExecuteDynamicObject(Context, strsql);
                        
                        foreach (var str in strs)
                        {
                            flotid += str["FLOT"].ToString() + ",";
                        }
                        gl += " AND FLOT IN (" + flotid.Trim(',') + ")";
                    }
                    ListRegularFilterParameter regularFilterPara = new ListRegularFilterParameter();
                    regularFilterPara.Filter =gl;
                    listShowParameter.ListFilterParameter = regularFilterPara;
                }

                this.View.ShowForm(listShowParameter, delegate (FormResult result)
                {
                    //读取返回值
                    object returnData = result.ReturnData;
                    if (returnData is ListSelectedRowCollection)
                    {
                        //如果是,执行,转换格式
                        ListSelectedRowCollection listSelectedRowCollection = returnData as ListSelectedRowCollection;
                        //如果不是空值,说明有返回值
                        int row = this.Model.GetEntryCurrentRowIndex("FEntity");
                        int i = row;
                        foreach (var list in listSelectedRowCollection)
                        {
                            int newrow = this.Model.GetEntryRowCount("FEntity");
                            if (i != row)
                            {
                                this.Model.CopyEntryRow("FEntity", row, newrow);
                                i = newrow;
                            }
                            this.Model.SetItemValueByID("FLOT", ((DynamicObject)list.DataRow["FLot_Ref"])["Number"].ToString(), i);
                            this.Model.SetValue("FQTY", list.DataRow["FBASEQTY"], i);

                            //this.View.Model.SetValue("FCGSQDDH", fbillno, e.Row);
                            //this.View.Model.SetValue("F_260_YCLYJDHRQ", dhrq, e.Row);

                            this.View.UpdateView("FLot", i);
                            i++;
                        }
                    }
                });
            }
        }
    }
}
