using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.供应链
{
    [Description("字段选择")]
    [Kingdee.BOS.Util.HotUpdate]
    public class ZDXZFormPlugin: AbstractDynamicFormPlugIn
    {
        string djbs = "PRD_MO";//单据标识
        public override void OnInitialize(InitializeEventArgs e)
        {
            base.OnInitialize(e);
            //djbs = e.Paramter.GetCustomParameters()["DJBS"].ToString();
        }
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            FormMetadata meta = MetaDataServiceHelper.Load(this.Context, djbs) as FormMetadata;
            var forms = meta.BusinessInfo.GetFieldList();
            this.Model.DeleteEntryData("F_SLSB_Entity");
            int hs = 0;
            foreach (var form in forms)
            {
                this.Model.CreateNewEntryRow("F_SLSB_Entity");
                this.View.Model.SetValue("F_ST", form.Entity.Name.ToString(), hs);
                this.View.Model.SetValue("F_ZDMC", form.Name.ToString(), hs);
                this.View.Model.SetValue("F_ZDBS", form.FieldName.ToString(), hs);
                this.View.Model.SetValue("F_ZDST", form.PropertyName.ToString(), hs);
                hs++;
            }
            this.View.UpdateView("F_SLSB_Entity");
        }
        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);
            List<List<KeyValuePair<string, string>>> zdxxs=new List<List<KeyValuePair<string, string>>>() ;
            var dates = this.Model.DataObject["SLSB_K9b8a3fd6"] as DynamicObjectCollection;
            foreach(var date in dates)
            {
                List<KeyValuePair<string, string>> zdxx= new List<KeyValuePair<string, string>>();
                if (Convert.ToBoolean(date["F_XZ"]) == true)
                {
                    zdxx.Add(new KeyValuePair<string, string>("F_ST", date["F_ST"].ToString()));
                    zdxx.Add(new KeyValuePair<string, string>("F_ZDMC", date["F_ZDMC"].ToString()));
                    zdxx.Add(new KeyValuePair<string, string>("F_ZDBS", date["F_ZDBS"].ToString()));
                    zdxx.Add(new KeyValuePair<string, string>("F_ZDST", date["F_ZDST"].ToString()));
                    zdxxs.Add(zdxx);
                }
            }
            this.View.ReturnToParentWindow(zdxxs);
            this.View.Close();
        }
    }
}
