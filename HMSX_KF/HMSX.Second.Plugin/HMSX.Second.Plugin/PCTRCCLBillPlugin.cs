using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.CommonFilter;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin
{
    [Description("批次投入产出率报表")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class PCTRCCLBillPlugin : AbstractDynamicFormPlugIn
    {
        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            if (e.Key.Equals("F_260_CX"))
            {
                string wl = this.Model.GetValue("F_260_WL") == null ? "" : ((DynamicObject)this.Model.GetValue("F_260_WL"))["Id"].ToString();
                string ph = this.Model.GetValue("F_260_PH") == null ? "" : ((DynamicObject)this.Model.GetValue("F_260_PH"))["Id"].ToString();
                string sccj = this.Model.GetValue("F_260_SCCJ") == null ? "" : ((DynamicObject)this.Model.GetValue("F_260_SCCJ"))["Id"].ToString();
                string scdd = this.Model.GetValue("F_260_SCDD") == null ? "" : this.Model.GetValue("F_260_SCDD").ToString();
                string gxjh = this.Model.GetValue("F_GXJH") == null ? "" : this.Model.GetValue("F_GXJH").ToString();
                string pgid = this.Model.GetValue("F_260_PGID") == null ? "" : this.Model.GetValue("F_260_PGID").ToString();
                DateTime kssj = Convert.ToDateTime(this.Model.GetValue("F_260_KSSJ").ToString());
                DateTime jssj = Convert.ToDateTime(this.Model.GetValue("F_260_JSSJ").ToString());
                if(wl=="" & ph=="" && sccj=="" && gxjh=="" && pgid=="" && scdd == "")
                {
                    throw new KDBusinessException("", "请输入一个或多个查询条件！");
                }
                else
                {
                    string cxssql = $@"exec HMSX_260_PHTRCCL '{wl}','{ph}','{sccj}','{scdd}','{gxjh}','{pgid}','{kssj}','{jssj}'";
                    var cxs = DBUtils.ExecuteDynamicObject(Context, cxssql);
                    this.Model.DeleteEntryData("F_260_Entity");
                    int hs = 0;
                    if (cxs.Count > 0)
                    {
                        foreach (var cx in cxs)
                        {
                            this.Model.CreateNewEntryRow("F_260_Entity");
                            this.View.Model.SetValue("F_260_DDBH", cx["FBILLNO"].ToString(), hs);
                            this.View.Model.SetValue("F_260_HH", cx["FSEQ"].ToString(), hs);
                            this.View.Model.SetValue("F_260_GXJH", cx["GXJH"].ToString(), hs);

                            this.View.Model.SetValue("F_BOMFWL", cx["BOMFNUMBER"].ToString(), hs);
                            this.View.Model.SetValue("F_BOMFWLNAME", cx["BOMFNAME"].ToString(), hs);
                            this.View.Model.SetValue("F_BOMWL", cx["BOMWLFNUMBER"].ToString(), hs);
                            this.View.Model.SetValue("F_BOMWLNAME", cx["BOMWLFNAME"].ToString(), hs);
                            this.View.Model.SetValue("F_LLWL", cx["WLFNUMBER"].ToString(), hs);
                            this.View.Model.SetValue("F_LLWLNAME", cx["WLFNAME"].ToString(), hs);
                            this.View.Model.SetValue("F_CJ", cx["CJFNAME"]==null?"": cx["CJFNAME"].ToString(), hs);
                            //this.View.Model.SetItemValueByID("F_260_BOMFWL", Convert.ToInt32(cx["BOMFXWL"].ToString()), hs);
                            //this.View.Model.SetItemValueByID("F_260_BOMWL", Convert.ToInt32(cx["BOMWL"] == null ? "0" : cx["BOMWL"].ToString()), hs);
                            // this.View.Model.SetItemValueByID("F_260_LLWL", Convert.ToInt32(cx["FMATERIALID"].ToString()), hs);
                            //this.View.Model.SetItemValueByID("F_260_DW", Convert.ToInt32(cx["FUNITID"].ToString()), hs);
                            //this.View.Model.SetItemValueByID("F_SCCJ", Convert.ToInt32(cx["FWORKSHOPID"].ToString()), hs);
                            this.View.Model.SetItemValueByID("F_260_PC", Convert.ToInt32(cx["FLOT"].ToString()), hs);
                            this.View.Model.SetValue("F_260_RKSL", cx["RKSL"] == null ? "" : cx["RKSL"].ToString(), hs);
                            this.View.Model.SetValue("F_260_LLSL", cx["LLSL"].ToString(), hs);
                            this.View.Model.SetValue("F_260_CCL", cx["CCL"] == null ? "" : cx["CCL"].ToString(), hs);
                            this.View.Model.SetValue("F_260_PG", cx["PGID"].ToString(), hs);
                            this.View.Model.SetValue("F_260_FZ", cx["FNUMERATOR"] == null ? "" : cx["FNUMERATOR"].ToString(), hs);
                            this.View.Model.SetValue("F_260_FM", cx["FDENOMINATOR"] == null ? "" : cx["FDENOMINATOR"].ToString(), hs);
                            this.View.Model.SetValue("F_260_BZSL", cx["BZSL"] == null ? "" : cx["BZSL"].ToString(), hs);
                            this.View.Model.SetValue("F_260_BZYLSL", cx["BZYLSL"] == null ? "" : cx["BZYLSL"].ToString(), hs);
                            this.View.Model.SetValue("F_260_HBSL", cx["HBSL"] == null ? "" : cx["HBSL"].ToString(), hs);
                            this.View.Model.SetValue("F_260_PGSL", cx["PGSL"].ToString(), hs);
                            this.View.Model.SetValue("F_260_LLPC", cx["FLOT_TEXT"] == null ? "" : cx["FLOT_TEXT"].ToString().Trim(','), hs);
                            this.View.Model.SetValue("F_260_LLRQ", cx["FDATE"].ToString().Trim(','), hs);
                            this.View.Model.SetValue("F_260_RKRQ", cx["RKFDATE"] == null ? "" : cx["RKFDATE"].ToString().Trim(','), hs);
                            hs++;
                        }
                    }
                    this.View.UpdateView("F_260_Entity");
                }
            }
        }
        public override void EntryButtonCellClick(EntryButtonCellClickEventArgs e)
        {
            base.EntryButtonCellClick(e);
            if (e.FieldKey.EqualsIgnoreCase("F_260_DDBH") || e.FieldKey.EqualsIgnoreCase("F_260_GXJH"))
            {
                if (e.Row < 0)
                {
                    return;
                }
                String ID = "";
                if (e.FieldKey.EqualsIgnoreCase("F_260_DDBH"))
                {
                    ID = "PRD_MO";
                }
                else
                {
                    ID = "SFC_OperationPlanning";
                }
                var requisitionMetadata = (FormMetadata)MetaDataServiceHelper.Load(this.Context, ID);
                var billNo = this.Model.GetValue(e.FieldKey, e.Row);
                var objs = BusinessDataServiceHelper.Load(this.Context, requisitionMetadata.BusinessInfo,
                    new List<SelectorItemInfo>(new[] { new SelectorItemInfo("FID") }), OQLFilter.CreateHeadEntityFilter("FBillNo='" + billNo + "'"));
                if (objs == null || objs.Length == 0) { return; }
                var pkId = objs[0]["Id"].ToString(); var showParameter = new BillShowParameter
                {
                    FormId = ID, // 业务对象标识              
                    PKey = pkId, // 单据内码                
                    Status = OperationStatus.VIEW // 查看模式打开                                                            
                };
                this.View.ShowForm(showParameter);
            }
        }
        public override void BeforeF7Select(BeforeF7SelectEventArgs e)
        {
            base.BeforeF7Select(e);
            if (e.FieldKey.Equals("F_260_SCDD", StringComparison.OrdinalIgnoreCase))
            {
                ListShowParameter listShowParameter = new ListShowParameter();
                //FormId你要调用那个单据的列表,通过打开未扩展的销售订单,找到唯一标识     
                listShowParameter.FormId = "PRD_MO";
                //IsLookUp弹出的列表界面是否有“返回数据”按钮
                listShowParameter.IsLookUp = true;
                var wl = this.Model.GetValue("F_260_WL", e.Row) == null ? "" : ((DynamicObject)this.Model.GetValue("F_260_WL", e.Row))["Id"].ToString();
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
                            this.View.Model.SetValue("F_260_SCDD", fbillno, e.Row);
                        }
                    }
                });

            }
            else if (e.FieldKey.Equals("F_260_PGID", StringComparison.OrdinalIgnoreCase))
            {
                ListShowParameter listShowParameter = new ListShowParameter();
                //FormId你要调用那个单据的列表,通过打开未扩展的销售订单,找到唯一标识     
                listShowParameter.FormId = "SFC_DispatchDetail";
                //IsLookUp弹出的列表界面是否有“返回数据”按钮
                listShowParameter.IsLookUp = true;
                var wl = this.Model.GetValue("F_260_WL", e.Row) == null ? "" : ((DynamicObject)this.Model.GetValue("F_260_WL", e.Row))["Id"].ToString();
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
                            this.View.Model.SetValue("F_260_PGID", fbillno, e.Row);
                        }
                    }
                });

            }
            else if (e.FieldKey.EqualsIgnoreCase("F_260_PH"))
            {
                string a = this.Model.GetValue("F_260_WL") == null ? null : ((DynamicObject)this.Model.GetValue("F_260_WL"))["Id"].ToString();
                if (a != null)
                {
                    string FMA = "FMATERIALID" + "=" + Convert.ToInt32(a)+ "and FINSTOCKDATE >'2022-04-01'";
                    e.ListFilterParameter.Filter = e.ListFilterParameter.Filter.JoinFilterString(FMA);
                    return;
                }
            }
            else if (e.FieldKey.Equals("F_GXJH", StringComparison.OrdinalIgnoreCase))
            {
                ListShowParameter listShowParameter = new ListShowParameter();
                //FormId你要调用那个单据的列表,通过打开未扩展的销售订单,找到唯一标识     
                listShowParameter.FormId = "SFC_OperationPlanning";
                //IsLookUp弹出的列表界面是否有“返回数据”按钮
                listShowParameter.IsLookUp = true;
                var wl = this.Model.GetValue("F_260_WL", e.Row) == null ? "" : ((DynamicObject)this.Model.GetValue("F_260_WL", e.Row))["Id"].ToString();
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
                            this.View.Model.SetValue("F_GXJH", fbillno, e.Row);
                        }
                    }
                });

            }
            
        }
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            DateTime dt = DateTime.Now;
            dt = dt.AddMonths(-1);
            this.Model.SetValue("F_260_KSSJ", dt);
            this.View.UpdateView("F_260_KSSJ");
        }   
    }
}
