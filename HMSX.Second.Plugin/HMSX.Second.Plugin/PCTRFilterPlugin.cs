
using Kingdee.BOS;
using Kingdee.BOS.Core.CommonFilter;
using Kingdee.BOS.Core.CommonFilter.PlugIn;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Permission;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace HMSX.Second.Plugin
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("生产批次投入产出过滤界面")]
    public class PCTRFilterPlugin : AbstractCommonFilterPlugIn
    {
        public override void TreeNodeClick(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.TreeNodeArgs e)
        {
            base.TreeNodeClick(e);

        }
        public override void AfterCreateNewData(EventArgs e)
        {
            base.AfterCreateNewData(e);
            DateTime dt = DateTime.Now;
            dt = dt.AddMonths(-1);
            this.Model.SetValue("F_PAEZ_SD", dt);
            this.View.UpdateView("F_PAEZ_SD");
        }
        public override void BeforeF7Select(BeforeF7SelectEventArgs e)
        {
            base.BeforeF7Select(e);
            if (e.FieldKey.Equals("F_PAEZ_SCDD", StringComparison.OrdinalIgnoreCase))
            {
                ListShowParameter listShowParameter = new ListShowParameter();
                //FormId你要调用那个单据的列表,通过打开未扩展的销售订单,找到唯一标识     
                listShowParameter.FormId = "PRD_MO";
                //IsLookUp弹出的列表界面是否有“返回数据”按钮
                listShowParameter.IsLookUp = true;
                var wl = this.Model.GetValue("F_PAEZ_WL", e.Row) == null ? "" : ((DynamicObject)this.Model.GetValue("F_PAEZ_WL", e.Row))["Id"].ToString();
                if (wl != "")
                {
                    ListRegularFilterParameter regularFilterPara = new ListRegularFilterParameter();
                    regularFilterPara.Filter = "FMATERIALID=" + wl; ;
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
                        if (listSelectedRowCollection != null)
                        {
                            DynamicObjectDataRow datarow = (DynamicObjectDataRow)listSelectedRowCollection[0].DataRow;
                            var fbillno = datarow.DynamicObject["FBILLNO"].ToString();
                            this.View.Model.SetValue("F_PAEZ_SCDD", fbillno, e.Row);
                        }
                    }
                });

            }
            else if (e.FieldKey.Equals("F_PAEZ_PGID", StringComparison.OrdinalIgnoreCase))
            {
                ListShowParameter listShowParameter = new ListShowParameter();
                //FormId你要调用那个单据的列表,通过打开未扩展的销售订单,找到唯一标识     
                listShowParameter.FormId = "SFC_DispatchDetail";
                //IsLookUp弹出的列表界面是否有“返回数据”按钮
                listShowParameter.IsLookUp = true;
                var wl = this.Model.GetValue("F_PAEZ_WL", e.Row) == null ? "" : ((DynamicObject)this.Model.GetValue("F_PAEZ_WL", e.Row))["Id"].ToString();
                if (wl != "")
                {
                    ListRegularFilterParameter regularFilterPara = new ListRegularFilterParameter();
                    regularFilterPara.Filter = "FMATERIALID=" + wl; ;
                    listShowParameter.ListFilterParameter = regularFilterPara;
                }
                this.View.ShowForm(listShowParameter, delegate (FormResult result)
                {
                    object returnData = result.ReturnData;
                    if (returnData is ListSelectedRowCollection)
                    {
                        //如果是,执行,转换格式
                        ListSelectedRowCollection listSelectedRowCollection = returnData as ListSelectedRowCollection;
                        //如果不是空值,说明有返回值
                        if (listSelectedRowCollection != null)
                        {
                            DynamicObjectDataRow datarow = (DynamicObjectDataRow)listSelectedRowCollection[0].DataRow;
                            var fbillno = datarow.DynamicObject["t1_FENTRYID"].ToString();
                            this.View.Model.SetValue("F_PAEZ_PGID", fbillno, e.Row);
                        }
                    }
                });

            }
            else if (e.FieldKey.EqualsIgnoreCase("F_PAEZ_PC"))
            {
                string a = this.Model.GetValue("F_PAEZ_WL") == null ? null : ((DynamicObject)this.Model.GetValue("F_PAEZ_WL"))["Id"].ToString();
                if (a != null)
                {
                    string FMA = "FMATERIALID" + "=" + Convert.ToInt32(a) + "and FINSTOCKDATE >'2022-04-01'";
                    e.ListFilterParameter.Filter = e.ListFilterParameter.Filter.JoinFilterString(FMA);
                    return;
                }
            }
            else if (e.FieldKey.Equals("F_PAEZ_GXJH", StringComparison.OrdinalIgnoreCase))
            {
                ListShowParameter listShowParameter = new ListShowParameter();
                //FormId你要调用那个单据的列表,通过打开未扩展的销售订单,找到唯一标识     
                listShowParameter.FormId = "SFC_OperationPlanning";
                //IsLookUp弹出的列表界面是否有“返回数据”按钮
                listShowParameter.IsLookUp = true;
                var wl = this.Model.GetValue("F_PAEZ_WL", e.Row) == null ? "" : ((DynamicObject)this.Model.GetValue("F_PAEZ_WL", e.Row))["Id"].ToString();
                if (wl != "")
                {
                    ListRegularFilterParameter regularFilterPara = new ListRegularFilterParameter();
                    regularFilterPara.Filter = "FPRODUCTID=" + wl; ;
                    listShowParameter.ListFilterParameter = regularFilterPara;
                }
                this.View.ShowForm(listShowParameter, delegate (FormResult result)
                {
                    object returnData = result.ReturnData;
                    if (returnData is ListSelectedRowCollection)
                    {
                        //如果是,执行,转换格式
                        ListSelectedRowCollection listSelectedRowCollection = returnData as ListSelectedRowCollection;
                        //如果不是空值,说明有返回值
                        if (listSelectedRowCollection != null)
                        {
                            DynamicObjectDataRow datarow = (DynamicObjectDataRow)listSelectedRowCollection[0].DataRow;
                            var fbillno = datarow.DynamicObject["FBILLNO"].ToString();
                            this.View.Model.SetValue("F_PAEZ_GXJH", fbillno, e.Row);
                        }
                    }
                });

            }

        }
    }
}

