using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Bill.PlugIn.Args;
using Kingdee.BOS.Core.CommonFilter;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace HMSX.Second.Plugin
{
    [Description("点击选单--补料单")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class BLXDBillPlugin : AbstractBillPlugIn
    {
        /**
        public override void BeforeF7Select(BeforeF7SelectEventArgs e)
        {
            base.BeforeF7Select(e);
            if (e.FieldKey.Equals("F_RUJP_PGID", StringComparison.OrdinalIgnoreCase))
            {

                ListShowParameter listShowParameter = new ListShowParameter();
                //FormId你要调用那个单据的列表,通过打开未扩展的销售订单,找到唯一标识     
                listShowParameter.FormId = "SFC_DispatchDetail";
                //IsLookUp弹出的列表界面是否有“返回数据”按钮
                listShowParameter.IsLookUp = true;
                this.View.ShowForm(listShowParameter, delegate (FormResult result)
                {
                    //读取返回值
                    object returnData = result.ReturnData;

                    if (returnData is ListSelectedRowCollection)
                    {
                        //如果是,执行,转换格式
                        ListSelectedRowCollection listSelectedRowCollection = returnData as ListSelectedRowCollection;

                        //如果不是空值,说明有返回值
                        if (listSelectedRowCollection != null)
                        {
                            //PGMX548694-810532  PGMX548694-810534
                            DynamicObjectDataRow datarow = (DynamicObjectDataRow)listSelectedRowCollection[0].DataRow;

                            var index = datarow.DynamicObject["FBARCODE"].ToString().IndexOf("-") + 1;
                            string str = datarow.DynamicObject["FBARCODE"].ToString().Substring(index);
                            // string obj = datarow.DynamicObject["FBARCODE"].ToString().substring(index + 1, datarow.DynamicObject["FBARCODE"].ToString().length);
                            //datarow.DynamicObject["FBARCODE"].ToString().
                            this.View.Model.SetValue("F_RUJP_PGID", str, e.Row);

                        }
                    }
                });

            }
        }
        **/
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (e.Field.Key == "FMoBillNo")
            {             
                if (Context.CurrentOrganizationInfo.ID== 100026)
                {
                    //b.FNUMBER='01' and 
                    string xmhsql = $@"select F_260_XMH1,F_260_BASEMJYT from T_PRD_MO a
                                    left join ora_t_Cust100049 b on a.F_260_BASEMJYT=b.FID
                                    where a.fbillno='{this.Model.GetValue("FMOBILLNO",e.Row).ToString()}'";
                    var date = DBUtils.ExecuteDynamicObject(Context, xmhsql);
                    if (date.Count > 0)
                    {
                        this.Model.SetItemValueByID("F_260_BASEXMH", Convert.ToInt32(date[0]["F_260_XMH1"]), e.Row);
                        this.Model.SetItemValueByID("F_260_MJYT", Convert.ToInt32(date[0]["F_260_BASEMJYT"]), e.Row);
                    }
                }
            }
        }
        public override void AfterEntryBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterEntryBarItemClick(e);
            if (this.Context.CurrentOrganizationInfo.ID == 100026 && e.BarItemKey.Equals("F_KCCX"))
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
                    string gl = "FSTOCKORGID=100026 AND FBASEQTY>0 and FMATERIALID=" + wl;
                    string flotid = "0,";
                    if (gys != "" && gys != " ")
                    {
                        string gysname = "";
                        foreach (var sup in gys.ToString().Split(';'))
                        {
                            gysname += "'" + sup + "',";
                        }
                        string strsql = $@"select  a.fid,A.FMATERIALID,FLOT,c.FNAME from T_STK_INVENTORY a
                                     left join T_BD_LOTMASTER b on a.FLOT= b.FLOTid
                                     left join T_BD_SUPPLier_l c on c.FSUPPLIERID=b.FSUPPLYID
                                     where FSTOCKORGID=100026 and a.FMATERIALID='{wl}' AND FBASEQTY>0
                                     and (c.Fname in ({gysname.Trim(',')}))";
                        var strs = DBUtils.ExecuteDynamicObject(Context, strsql);

                        foreach (var str in strs)
                        {
                            flotid += str["FLOT"].ToString() + ",";
                        }
                        if (flotid != "")
                        {
                            gl += " AND FLOT IN (" + flotid.Trim(',') + ")";
                        }

                    }
                    ListRegularFilterParameter regularFilterPara = new ListRegularFilterParameter();
                    regularFilterPara.Filter = gl;
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
                            this.View.Model.SetValue("FLOT", ((DynamicObject)list.DataRow["FLot_Ref"])["Id"].ToString(), i);
                            //this.Model.SetItemValueByID("FLOT", ((DynamicObject)list.DataRow["FLot_Ref"])["Number"].ToString(), i);
                            this.Model.SetValue("FQTY", list.DataRow["FBASEQTY"], i);
                            this.Model.SetItemValueByID("FSTOCKID", list.DataRow["FStockId_Id"], i);
                            this.View.Model.SetValue("F_260_DXGYS", gys, i);
                            //this.View.Model.SetValue("F_260_YCLYJDHRQ", dhrq, e.Row);
                            this.View.InvokeFieldUpdateService("FSTOCKID", i);
                            this.View.InvokeFieldUpdateService("FLOT", i);
                            this.View.UpdateView("F_260_DXGYS", i);
                            this.View.UpdateView("FSTOCKID", i);
                            this.View.UpdateView("FLot", i);
                            i++;
                        }
                    }
                });
            }
        }
        public override void AfterDoOperation(AfterDoOperationEventArgs e)
        {
            base.AfterDoOperation(e);
            if (Context.CurrentOrganizationInfo.ID == 100026)
            {
                if (e.Operation.OperationId == 8 && e.Operation.Operation == "Save" && e.OperationResult.IsSuccess)
                {
                    this.View.InvokeFormOperation("Refresh");
                }
            }
        }
    }
}
