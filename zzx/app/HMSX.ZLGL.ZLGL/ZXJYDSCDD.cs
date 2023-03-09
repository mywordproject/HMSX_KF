using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.FormElement;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace HMSX.ZLGL.ZLGL
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("双新检验单审核生成返修订单")]
    public class ZXJYDSCDD: AbstractOperationServicePlugIn
    {       
        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {           
            base.AfterExecuteOperationTransaction(e);
            if (this.Context.CurrentOrganizationInfo.ID == 100026)
            {
                string fids = "";
                foreach (DynamicObject entity in e.DataEntitys)
                {
                    fids += $"{entity["Id"]},";
                }
                string fid = fids.Substring(0, fids.Length - 1);
                string sql = $@"/*dialect*/select jyc.FMATERIALID wl,sum(jyc.FQTY) sl 
                    from T_QM_INSPECTBILLENTRY jyb inner join T_QM_IBPOLICYDETAIL jyc on jyc.FENTRYID=jyb.FENTRYID
                    left join T_PRD_MO dd on dd.FBILLNO=jyb.F_260_FGDD
                    where jyc.FUSEPOLICY='C' and dd.FBILLNO is null and jyb.FID in ({fid}) group by jyc.FMATERIALID";
                DynamicObjectCollection objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
                if (objs.Count > 0)
                {
                    this.Create_SCDD(this.Context,objs,fid);
                }
            }
        }
        //创建生产订单
        private void Create_SCDD(Context ctx,DynamicObjectCollection objs,string fid)
        {
            ctx.UserId = 16394;
            ctx.UserName = "Administrator";
            IBillView billView = this.CreateBillView("PRD_MO", ctx);
            ((IBillViewService)billView).LoadData();
            DynamicFormViewPlugInProxy eventProxy = billView.GetService<DynamicFormViewPlugInProxy>();
            eventProxy.FireOnLoad();
            this.FillPropertys(billView,objs);
            this.SaveBill(billView, ctx,fid);
        }
        private IBillView CreateBillView(String TableName, Context ctx)
        {
            FormMetadata meta = MetaDataServiceHelper.Load(ctx, TableName) as FormMetadata;
            Form form = meta.BusinessInfo.GetForm();
            Type type = Type.GetType("Kingdee.BOS.Web.Import.ImportBillView,Kingdee.BOS.Web");
            var billView = (IDynamicFormViewService)Activator.CreateInstance(type);
            BillOpenParameter openParam = CreateOpenParameter(meta, ctx);
            var provider = form.GetFormServiceProvider();
            billView.Initialize(openParam, provider);
            return billView as IBillView;
        }
        private BillOpenParameter CreateOpenParameter(FormMetadata meta, Context ctx)
        {
            Form form = meta.BusinessInfo.GetForm();
            BillOpenParameter openParam = new BillOpenParameter(form.Id, meta.GetLayoutInfo().Id);
            openParam.Context = ctx;
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
            PreOpenFormEventArgs args = new PreOpenFormEventArgs(ctx, openParam);
            foreach (var plug in plugs)
            {
                plug.PreOpenForm(args);
            }
            return openParam;
        }
        //填写表单数据
        private void FillPropertys(IBillView billView,DynamicObjectCollection objs)
        {
            IDynamicFormViewService dynamicFormView = billView as IDynamicFormViewService;            
            dynamicFormView.SetItemValueByNumber("FBillType", "SCDD02_SYS", 0);
            billView.Model.DeleteEntryData("FTreeEntity");
            int row = 0;
            foreach (DynamicObject obj in objs)
            {
                billView.Model.CreateNewEntryRow("FTreeEntity");
                int wl = Convert.ToInt32(obj["wl"]);               
                float sl = Convert.ToSingle(obj["sl"]);               
                dynamicFormView.SetItemValueByID("FMaterialId", wl, row);                
                dynamicFormView.UpdateValue("FQty", row, sl);
                row++;
            }
        }
        private void SaveBill(IBillView billView, Context ctx,string fid)
        {
            IOperationResult saveResult=BusinessDataServiceHelper.Save(ctx,billView.BillBusinessInfo,
                                billView.Model.DataObject,OperateOption.Create(),"Save");
            if (saveResult.IsSuccess)
            {
                string MOBI = saveResult.OperateResult[0].Number;
                string usql = $@"/*dialect*/update a set F_260_FGDD='{MOBI}',F_260_FGDDHH=ddb.FSEQ from T_QM_INSPECTBILLENTRY a
                inner join T_QM_INSPECTBILLENTRY_A b on a.FENTRYID = b.FENTRYID
                inner join T_QM_IBPOLICYDETAIL jyc on jyc.FENTRYID=a.FENTRYID               
                inner join T_PRD_MOENTRY ddb on ddb.FID=(select FID from T_PRD_MO where FBILLNO='{MOBI}') and ddb.FMATERIALID = b.FMATERIALID
                left join T_PRD_MO dd on dd.FBILLNO=a.F_260_FGDD
                where dd.FBILLNO is null and jyc.FUSEPOLICY='C' and a.FID in ({fid})";
                DBUtils.Execute(ctx, usql);
            }
        }
    }
}
