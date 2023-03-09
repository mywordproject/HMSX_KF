using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.GYL.KCGL
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("双新表单字段选择")]
    public class ZXSXBDZDXZ:AbstractBillPlugIn
    {
        public override void BeforeF7Select(BeforeF7SelectEventArgs e)
        {
            base.BeforeF7Select(e);
            if (this.Context.CurrentOrganizationInfo.ID == 100026)
            {
                if (e.FieldKey == "FZDM")
                {
                    int row = this.View.Model.GetEntryCurrentRowIndex("F_PAEZ_Entity1");
                    string formid = ((DynamicObject)this.View.Model.GetValue("FDJM", row))["Id"].ToString();
                    string lx = this.View.Model.GetValue("FSJLX").ToString();
                    DynamicFormShowParameter showParam = new DynamicFormShowParameter();
                    showParam.FormId = "SLSB_ZDXX";
                    showParam.CustomParams.Add("formid",formid);
                    showParam.CustomParams.Add("lx", lx);
                    int hs = 0;
                    this.View.ShowForm(showParam, delegate (FormResult result) {
                        if (result.ReturnData != null)
                        {
                            foreach (var obj in (List<Dictionary<string, string>>)result.ReturnData)
                            {
                                this.View.Model.SetValue("FSTM", obj["F_ST"], hs);
                                this.View.Model.SetValue("FZDM", obj["F_ZDMC"], hs);
                                this.View.Model.SetValue("FZDBS", obj["F_ZDBS"], hs);
                                this.View.Model.SetValue("FSTSX", obj["F_ZDST"], hs);
                                this.View.Model.SetValue("FORMM", obj["F_ORM"], hs);
                                this.View.Model.CreateNewEntryRow("F_PAEZ_SubEntity1");
                                hs++;
                            }
                        }    
                    });
                }
            }
        }
    }
}
