
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Bill.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.生产制造
{
    [Description("检验单--状态同步")]
    //热启动,不用重启IIS
    //python
    [Kingdee.BOS.Util.HotUpdate]
    public class JYDBillPlugin : AbstractBillPlugIn
    {
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            //python挂起
            //if (e.Field.Key == "FPolicyStatus" && ((DynamicObject)this.Model.GetValue("FINSPECTORGID"))["Id"].ToString() == "100026")
            //{
            //    int xh = Convert.ToInt32(this.View.Model.GetEntryCurrentRowIndex("FEntity").ToString());
            //    this.Model.SetValue("FINSPECTRESULT", e.NewValue, xh);
            //    this.View.UpdateView("FInspectResult");
            //    this.View.InvokeFieldUpdateService("FInspectResult", xh);
            //}
        }
        public override void BeforeSave(BeforeSaveEventArgs e)
        {
            if (Context.CurrentOrganizationInfo.ID == 100026 && this.Model.GetValue("F_260_JYQF") != null)
            {
                if (this.Model.GetValue("F_260_JYQF").ToString() == "1")
                {
                    var entitys = this.Model.DataObject["Entity"] as DynamicObjectCollection;
                    foreach (var entity in entitys)
                    {
                        int j = 0;
                        foreach (var polic in entity["PolicyDetail"] as DynamicObjectCollection)
                        {
                            if (polic["UsePolicy"].ToString() == "A" || polic["UsePolicy"].ToString() == "B")
                            {
                                j++;
                            }
                        }
                        if (j > 0)
                        {
                            this.Model.SetValue("FINSPECTRESULT", 3, Convert.ToInt32(entity["Seq"].ToString()) - 1);
                            this.View.InvokeFieldUpdateService("FInspectResult", Convert.ToInt32(entity["Seq"].ToString()) - 1);
                            DynamicObjectCollection subEntry = entity["PolicyDetail"] as DynamicObjectCollection;
                            subEntry.Clear();
                            this.View.UpdateView("FPolicyDetail");
                        }
                    }
                }
            }
            base.BeforeSave(e);
        }
    }
}
