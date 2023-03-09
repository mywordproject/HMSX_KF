using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List;
using Kingdee.BOS;
using Kingdee.BOS.Core.DynamicForm;

namespace HMSX.SCZZ.CJGL.MES
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("生产补料单-选单派工明细")]
    public class BLXDBillPlugin: AbstractBillPlugIn
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
