using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Bill.PlugIn.Args;
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
    }
}
