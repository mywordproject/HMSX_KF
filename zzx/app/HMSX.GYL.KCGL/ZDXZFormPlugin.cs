using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace HMSX.GYL.KCGL
{
    [Description("字段选择")]
    [Kingdee.BOS.Util.HotUpdate]
    public class ZDXZFormPlugin: AbstractDynamicFormPlugIn
    {        
        public override void AfterBindData(EventArgs e)
        {           
            base.AfterBindData(e);
            string formid = this.View.OpenParameter.GetCustomParameter("formid").ToString();
            string lx = this.View.OpenParameter.GetCustomParameter("lx").ToString();
            FormMetadata meta = MetaDataServiceHelper.Load(this.Context, formid) as FormMetadata;
            var forms = meta.BusinessInfo.GetFieldList();
            this.Model.DeleteEntryData("F_SLSB_Entity");
            int hs = 0;
            foreach (Field form in forms)
            {   
                if(form is BaseDataField)
                {
                    var zl = ((BaseDataField)form).LookUpObject==null?"": ((BaseDataField)form).LookUpObject.FormId;
                    if (zl == lx)
                    {
                        this.Model.CreateNewEntryRow("F_SLSB_Entity");
                        this.View.Model.SetValue("F_ST", form.Entity.Name.ToString(), hs);
                        this.View.Model.SetValue("F_ZDMC", form.Name.ToString(), hs);
                        this.View.Model.SetValue("F_ZDBS", form.Key.ToString(), hs);
                        this.View.Model.SetValue("F_ZDST", form.PropertyName.ToString(), hs);
                        this.View.Model.SetValue("F_ORM", form.Entity.EntryName, hs);
                        hs++;
                    }                                        
                }               
            }
            this.View.UpdateView("F_SLSB_Entity");
        }
        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);           
            List<Dictionary<string,string>> zds = new List<Dictionary<string, string>>();
            var dates = this.Model.DataObject["SLSB_K9b8a3fd6"] as DynamicObjectCollection;
            foreach(var date in dates)
            {                
                Dictionary<string, string> zd = new Dictionary<string, string>();
                if (Convert.ToBoolean(date["F_XZ"]) == true)
                {                   
                    zd.Add("F_ST", date["F_ST"].ToString());
                    zd.Add("F_ZDMC", date["F_ZDMC"].ToString());
                    zd.Add("F_ZDBS", date["F_ZDBS"].ToString());
                    zd.Add("F_ZDST", date["F_ZDST"].ToString());
                    zd.Add("F_ORM", date["F_ORM"].ToString());
                    zds.Add(zd);
                }
            }
            this.View.ReturnToParentWindow(zds);
            this.View.Close();
        }
    }
}
