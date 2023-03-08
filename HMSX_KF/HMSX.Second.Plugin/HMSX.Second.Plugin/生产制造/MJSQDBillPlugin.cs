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
    [Description("模具申请单--选单采购")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class MJSQDBillPlugin : AbstractBillPlugIn
    {
        public override void BeforeF7Select(BeforeF7SelectEventArgs e)
        {
            base.BeforeF7Select(e);
            if (this.Context.CurrentOrganizationInfo.ID == 100026)
            {
                if (e.FieldKey.Equals("FCGSQDDH", StringComparison.OrdinalIgnoreCase))
                {
                    ListShowParameter listShowParameter = new ListShowParameter();
                    //FormId你要调用那个单据的列表,通过打开未扩展的销售订单,找到唯一标识     
                    listShowParameter.FormId = "PUR_Requisition";
                    //IsLookUp弹出的列表界面是否有“返回数据”按钮
                    listShowParameter.IsLookUp = true;
                    var wl = this.Model.GetValue("FYCLMC", e.Row) == null ? "" : ((DynamicObject)this.Model.GetValue("FYCLMC", e.Row))["Id"].ToString();
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
                                string cxsql = $@"select FARRIVALDATE from T_PUR_ReqEntry where FENTRYID={datarow.DynamicObject["t1_FENTRYID"]}";
                                var cx = DBUtils.ExecuteDynamicObject(Context, cxsql);
                                DateTime dhrq = Convert.ToDateTime(cx[0]["FARRIVALDATE"].ToString());
                                this.View.Model.SetValue("FCGSQDDH", fbillno, e.Row);
                                this.View.Model.SetValue("F_260_YCLYJDHRQ", dhrq, e.Row);

                            }
                        }
                    });

                }
            }
        }
    }
}
