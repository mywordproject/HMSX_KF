﻿using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
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

namespace HMSX.GYL.KCGL
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("双新物料复检超期_执行计划")]
    public class ZXWLFJCQJK: IScheduleService
    {
        private string error;
        public void Run(Context ctx, Schedule schedule)
        {
            error = "";
            //设置上下文组织用户信息
            ctx.CurrentOrganizationInfo = new OrganizationInfo();
            ctx.CurrentOrganizationInfo.ID = 100026;
            ctx.CurrentOrganizationInfo.Name = "宏明双新科技";           
            DynamicObjectCollection fwms = DBUtils.ExecuteDynamicObject(ctx, "exec zzx_sxcqwl 0");
            DynamicObjectCollection wms = DBUtils.ExecuteDynamicObject(ctx, "exec zzx_sxcqwl 1");           
            foreach (DynamicObject obj in wms)
            {
                //wms仓,分单创建
                this.Create_KCZH(ctx, new DynamicObjectCollection(wms.DynamicCollectionItemPropertyType) { obj});
            }
            //非wms仓，整单创建
            if (fwms.Count > 0) { this.Create_KCZH(ctx, fwms); }           
            if (error != "")
            {
                throw new Exception(error);
            }
        }
        private void Create_KCZH(Context ctx,DynamicObjectCollection objs)
        {
            ctx.UserId = Convert.ToInt64(objs[0]["用户id"]);
            ctx.UserName = objs[0]["用户"].ToString();
            IBillView billView = this.CreateBillView("STK_StockConvert", ctx);
            ((IBillViewService)billView).LoadData();
            DynamicFormViewPlugInProxy eventProxy = billView.GetService<DynamicFormViewPlugInProxy>();
            eventProxy.FireOnLoad();
            this.FillPropertys(billView,objs);
            this.SaveBill(billView, ctx);
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
        private void FillPropertys(IBillView billView,DynamicObjectCollection Dyobj)
        {
            IDynamicFormViewService dynamicFormView = billView as IDynamicFormViewService;
            dynamicFormView.SetItemValueByID("FStockOrgId", 100026, 0);
            dynamicFormView.UpdateValue("FNote", 0, "执行计划生成");            
            dynamicFormView.UpdateValue("F_260_WMSCK", 0,Convert.ToInt32(Dyobj[0]["wms仓库"]));
            dynamicFormView.SetItemValueByNumber("FDeptId", Dyobj[0]["部门"].ToString(), 0);
            ((IDynamicFormView)dynamicFormView).InvokeFieldUpdateService("FStockOrgId", 0);
            billView.Model.DeleteEntryData("FEntity");                 
            int ROW = 0;
            foreach (DynamicObject obj in Dyobj)
            {
                billView.Model.BatchCreateNewEntryRow("FEntity", 2);
                string wl = obj["物料编码"].ToString();
                string ph = obj["批号"].ToString();
                string ck = obj["仓库编码"].ToString();
                string jhgzh = obj["计划跟踪号"].ToString();                
                double kcs = Convert.ToDouble(obj["库存数"]);
                long cw = Convert.ToInt64(obj["仓位"]);                                
                //转换前
                dynamicFormView.UpdateValue("FConvertType", ROW, "A");
                dynamicFormView.SetItemValueByNumber("FMaterialId", wl, ROW);
                dynamicFormView.UpdateValue("FLot", ROW, ph);
                dynamicFormView.SetItemValueByNumber("FStockId", ck, ROW);
                dynamicFormView.UpdateValue("FConvertQty", ROW, kcs);
                dynamicFormView.UpdateValue("FMTONo", ROW, jhgzh);
                dynamicFormView.SetItemValueByNumber("FStockStatus", "KCZT01_SYS", ROW);
                dynamicFormView.SetItemValueByID("FStockLocId", cw, ROW);
                //转换后
                ROW = ROW + 1;
                dynamicFormView.UpdateValue("FConvertType", ROW, "B");
                dynamicFormView.SetItemValueByNumber("FMaterialId", wl, ROW);
                dynamicFormView.UpdateValue("FLot", ROW, ph);
                dynamicFormView.SetItemValueByNumber("FStockId", ck, ROW);
                dynamicFormView.UpdateValue("FConvertQty", ROW, kcs);
                dynamicFormView.UpdateValue("FMTONo", ROW, jhgzh);
                dynamicFormView.SetItemValueByNumber("FStockStatus", "KCZT009", ROW);
                dynamicFormView.SetItemValueByID("FStockLocId", cw, ROW);
                ROW = ROW + 1;
            }
        }
        //保存、提交、审核单据
        private void SaveBill(IBillView billView, Context ctx)
        {
            IOperationResult saveResult = BusinessDataServiceHelper.Save(
                    ctx,
                    billView.BillBusinessInfo,
                    billView.Model.DataObject,
                    OperateOption.Create(),
                    "Save");
            long fid;
            if (saveResult.IsSuccess)
            {
                foreach (var dataResult in saveResult.SuccessDataEnity)
                {
                    if (dataResult["Id"] != null)
                    {
                        fid = long.Parse(dataResult["Id"].ToString());
                        IOperationResult submitResult = BusinessDataServiceHelper.Submit(ctx, billView.BusinessInfo, new object[] { fid }, "Submit", null);
                        if (submitResult.IsSuccess)
                        {
                            BusinessDataServiceHelper.Audit(ctx, billView.BusinessInfo, new object[] { fid }, null);
                        }
                    }
                }
            }
            else
            {
                foreach(var err in saveResult.ValidationErrors)
                {
                    error += err.Message+";";
                }
            }           
        }
    }
}
