using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.FormElement;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace HMSX.GYL.CGGL
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("双新供应商审核记录变更信息")]
    public class ZXGYSBGJL : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            e.FieldKeys.Add("FModifierId");
        }
        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);
            try
            {
                if (this.Context.CurrentOrganizationInfo.ID == 100026)
                {
                    foreach (DynamicObject entity in e.DataEntitys)
                    {
                        long id = Convert.ToInt64(entity["Id"]);
                        string sql = $"/*dialect*/select * from PAEZ_t_Cust_Entry100415 where F_260_SFJL=0 and FSupplierId={id}";
                        DynamicObjectCollection objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
                        if (objs.Count > 0)
                        {
                            long bgrid = Convert.ToInt64(entity["ModifierId_Id"]);
                            this.Context.UserId = bgrid;
                            this.CreateBill(objs, id);
                            string usql = $"/*dialect*/update PAEZ_t_Cust_Entry100415 set F_260_SFJL=1 where F_260_SFJL=0 and FSupplierId={id}";
                            DBUtils.Execute(this.Context, usql);
                        }
                    }
                }
            }
            catch { return; }
        }
        private void CreateBill(DynamicObjectCollection objs, long id)
        {
            IBillView billView = this.CreateBillView("PAEZ_SXGYSBGJL");
            ((IBillViewService)billView).LoadData();
            DynamicFormViewPlugInProxy eventProxy = billView.GetService<DynamicFormViewPlugInProxy>();
            eventProxy.FireOnLoad();
            this.FillPropertys(billView, objs, id);
            Form form = billView.BillBusinessInfo.GetForm();
            if (form.FormIdDynamicProperty != null)
            {
                form.FormIdDynamicProperty.SetValue(billView.Model.DataObject, form.Id);
            }
            billView.Model.Save();
        }
        private IBillView CreateBillView(string TableName)
        {
            FormMetadata meta = MetaDataServiceHelper.Load(this.Context, TableName) as FormMetadata;
            Form form = meta.BusinessInfo.GetForm();
            Type type = Type.GetType("Kingdee.BOS.Web.Import.ImportBillView,Kingdee.BOS.Web");
            var billView = (IDynamicFormViewService)Activator.CreateInstance(type);
            BillOpenParameter openParam = CreateOpenParam(meta);
            var provider = form.GetFormServiceProvider();
            billView.Initialize(openParam, provider);
            return billView as IBillView;
        }
        private BillOpenParameter CreateOpenParam(FormMetadata meta)
        {
            Form form = meta.BusinessInfo.GetForm();
            BillOpenParameter openParam = new BillOpenParameter(form.Id, meta.GetLayoutInfo().Id);
            openParam.Context = this.Context;
            openParam.ServiceName = form.FormServiceName;
            openParam.PageId = Guid.NewGuid().ToString();
            openParam.FormMetaData = meta;
            openParam.Status = OperationStatus.ADDNEW;
            openParam.PkValue = null;
            openParam.CreateFrom = CreateFrom.Default;
            openParam.GroupId = "";
            openParam.ParentId = 0;
            openParam.DefaultBillTypeId = "";
            openParam.DefaultBusinessFlowId = "";
            openParam.SetCustomParameter("ShowConfirmDialogWhenChangeOrg", false);
            List<AbstractDynamicFormPlugIn> plugs = form.CreateFormPlugIns();
            openParam.SetCustomParameter(FormConst.PlugIns, plugs);
            PreOpenFormEventArgs args = new PreOpenFormEventArgs(this.Context, openParam);
            foreach (var plug in plugs)
            {
                plug.PreOpenForm(args);
            }
            return openParam;
        }
        private void FillPropertys(IBillView billView, DynamicObjectCollection objs, long id)
        {
            IDynamicFormViewService dynamicFormView = billView as IDynamicFormViewService;
            dynamicFormView.SetItemValueByID("FGYS", id, 0);
            int row = 0;
            foreach (DynamicObject obj in objs)
            {
                billView.Model.CreateNewEntryRow("FEntity");
                dynamicFormView.UpdateValue("FNAME", row, obj["F_260_BGZDM"]);//变更字段
                dynamicFormView.UpdateValue("FOLD", row, obj["F_260_BGQZ"]);//变更前
                dynamicFormView.UpdateValue("FBGH", row, obj["F_260_BGHZ"]);//变更后
                row++;
            }
        }
    }
}
