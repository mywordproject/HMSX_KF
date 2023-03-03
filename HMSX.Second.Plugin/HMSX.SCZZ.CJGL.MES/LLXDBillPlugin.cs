using Kingdee.BOS;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List;
using System;
using System.ComponentModel;


namespace HMSX.SCZZ.CJGL.MES
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("生产领料单-选单派工明细")]
    public class LLXDBillPlugin:AbstractBillPlugIn
    {
        public override void BeforeF7Select(BeforeF7SelectEventArgs e)
        {
            base.BeforeF7Select(e);           
            if (e.FieldKey.Equals("F_260_PGMXID", StringComparison.OrdinalIgnoreCase))
            {
                string MOBILLNO = this.View.Model.GetValue("FMoBillNo", e.Row).ToString();
                string MOSEQ = this.View.Model.GetValue("FMoEntrySeq", e.Row).ToString();
                ListShowParameter listShowParameter = new ListShowParameter();         
                listShowParameter.FormId = "SFC_DispatchDetail";              
                listShowParameter.IsLookUp = true;
                listShowParameter.ListFilterParameter.Filter += $"FMOBILLNO='{MOBILLNO}'and FMOSEQ={MOSEQ}";
                this.View.ShowForm(listShowParameter, delegate (FormResult result)
                {
                    //读取返回值
                    object returnData = result.ReturnData;
                    if (returnData is ListSelectedRowCollection)
                    {                
                        ListSelectedRowCollection listSelectedRowCollection = returnData as ListSelectedRowCollection;           
                        if (listSelectedRowCollection != null)
                        {
                            DynamicObjectDataRow datarow = (DynamicObjectDataRow)listSelectedRowCollection[0].DataRow;
                            var index = datarow.DynamicObject["FBARCODE"].ToString().IndexOf("-") + 1;
                            string str = datarow.DynamicObject["FBARCODE"].ToString().Substring(index);
                            this.View.Model.SetValue("F_260_PGMXID", str, e.Row);
                        }
                    }
                });

            }
        }
    }
}
