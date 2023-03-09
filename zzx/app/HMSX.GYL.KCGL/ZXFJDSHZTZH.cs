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

namespace HMSX.GYL.KCGL
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("双新物料复检单审核生成状态转换单")]
    public class ZXFJDSHZTZH: AbstractOperationServicePlugIn
    {
        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);
            if (this.Context.CurrentOrganizationInfo.ID == 100026)
            {
                foreach(DynamicObject entity in e.DataEntitys)
                {
                    string fid = entity["Id"].ToString();
                    string fjsql = $@"/*dialect*/select fj.FMATERIALID 物料,fj.FSTOCKID 仓库,fj.FSTOCKLOCID 仓位,ph.FNUMBER 批号,bm.FNUMBER 复检部门,fjb.FBILLNO 单号
                        from HMD_t_Cust_Entry100241 fj inner join HMD_t_Cust100253 fjb on fj.FID=fjb.FID
                        inner join T_BD_LOTMASTER ph on ph.FLOTID=fj.FLOT
                        left join T_BD_DEPARTMENT bm on fjb.FHMSXDEPT=bm.FDEPTID
                        where F_HMD_FJJG='A' and fj.FID={fid}";
                    DynamicObjectCollection objs = DBUtils.ExecuteDynamicObject(this.Context, fjsql);
                    foreach(DynamicObject obj in objs)
                    {
                        string kcsql = $@"/*dialect*/select wl.FNUMBER 物料编码,cka.FNUMBER 仓库编码,kc.FSTOCKLOCID 仓位,ph.FNUMBER 批号,kc.FBASEQTY 库存数,'{obj["复检部门"]}' 部门,kc.FMTONO 计划跟踪号,
                            '{obj["单号"]}' 复检单号,case when cka.FNUMBER in ('260CK091','260CK092','260CK093','260CK067','260CK057','260CK028') then 1 else 0 end wms仓库
                            from T_STK_INVENTORY kc
                            inner join T_BD_MATERIAL wl on kc.FMATERIALID=wl.FMATERIALID
                            inner join T_BD_LOTMASTER ph on ph.FLOTID=kc.FLOT
                            inner join T_BD_STOCKSTATUS_L zt on zt.FSTOCKSTATUSID = kc.FSTOCKSTATUSID and zt.FLOCALEID = 2052
                            inner join T_BD_STOCK cka on kc.FSTOCKID = cka.FSTOCKID
                            where kc.FSTOCKORGID = 100026 and kc.FBASEQTY > 0 and zt.FNAME = '超期' 
                            and kc.FSTOCKID={obj["仓库"]} and kc.FMATERIALID={obj["物料"]} and kc.FSTOCKLOCID={obj["仓位"]} and ph.FNUMBER='{obj["批号"]}'";
                        DynamicObjectCollection kcobjs = DBUtils.ExecuteDynamicObject(this.Context, kcsql);
                        if (kcobjs.Count > 0)
                        {
                            this.Create_KCZH(this.Context, kcobjs);
                        } 
                    }
                }
            }
        }
        private void Create_KCZH(Context ctx, DynamicObjectCollection objs)
        {
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
            dynamicFormView.UpdateValue("FNote", 0, Dyobj[0]["复检单号"].ToString()+"生成");
            dynamicFormView.UpdateValue("F_260_WMSCK", 0, Convert.ToInt32(Dyobj[0]["wms仓库"]));
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
                dynamicFormView.SetItemValueByNumber("FStockStatus", "KCZT009", ROW);
                dynamicFormView.SetItemValueByID("FStockLocId", cw, ROW);
                //转换后
                ROW = ROW + 1;
                dynamicFormView.UpdateValue("FConvertType", ROW, "B");
                dynamicFormView.SetItemValueByNumber("FMaterialId", wl, ROW);
                dynamicFormView.UpdateValue("FLot", ROW, ph);
                dynamicFormView.SetItemValueByNumber("FStockId", ck, ROW);
                dynamicFormView.UpdateValue("FConvertQty", ROW, kcs);
                dynamicFormView.UpdateValue("FMTONo", ROW, jhgzh);
                dynamicFormView.SetItemValueByNumber("FStockStatus", "KCZT01_SYS", ROW);
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
        }
    }
}
