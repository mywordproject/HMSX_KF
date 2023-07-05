using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Mobile.PlugIn.ControlModel;
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.Complex;
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.MES
{
    [Description("工序转移申请--移动端")]
    [Kingdee.BOS.Util.HotUpdate]
    public class GXZYMobilePlugin : ComplexTransListEdit
    {
        public override void EntityRowDoubleClick(EntityRowClickEventArgs e)
        {
            base.EntityRowDoubleClick(e);
            int entryRowCount = this.Model.GetEntryRowCount("FMobileListViewEntity");
            int[] selectedRows = base.View.GetControl<MobileListViewControl>("FMobileListViewEntity").GetSelectedRows();
            this.View.GetControl<MobileListViewControl>("FMobileListViewEntity").SetSelectRows(selectedRows);
            for (int i = 0; i < entryRowCount; i++)
            {
                if (Array.IndexOf(selectedRows, i) == -1)
                {
                    this.ListFormaterManager.SetControlProperty("FFlowLayout_Row", i, "255,255,255", MobileFormatConditionPropertyEnums.BackColor);
                }
                else
                {
                    this.ListFormaterManager.SetControlProperty("FFlowLayout_Row", i, "255,234,199", MobileFormatConditionPropertyEnums.BackColor);
                }
            }
            this.View.GetControl<MobileListViewControl>("FMobileListViewEntity").SetFormat(this.ListFormaterManager);
            this.View.UpdateView("FMobileListViewEntity");

        }

        public override void ButtonClick(ButtonClickEventArgs e)
        {
            base.ButtonClick(e);
            if(e.Key.ToUpper()== "F_QX")
            {
                AllSelect("FMobileListViewEntity");
            }
        }
        protected void AllSelect(string entityKey)
        {
            var dictionary = this.dicFieldLabelKeys;
            List<int> list = new List<int>();
            for (int i = 0; i < dictionary.Count; i++)
            {
                list.Add(i);
                this.ListFormaterManager.SetControlProperty("FFlowLayout_Row", i, "255,234,199", MobileFormatConditionPropertyEnums.BackColor);
            }
            base.View.GetControl<MobileListViewControl>(entityKey).SetSelectRows(list.ToArray());
            base.View.GetControl<MobileListViewControl>(entityKey).SetFormat(this.ListFormaterManager);
            this.View.UpdateView(entityKey);
        }
    }
}
