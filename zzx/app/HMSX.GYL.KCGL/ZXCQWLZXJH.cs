using Kingdee.BOS;
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
    [Description("双新超期物料监控_电镀件")]
    public class ZXCQWLZXJH:IScheduleService
    {
        private string error;
        private int fid;
        List<long> pgids = new List<long>();
        public void Run(Context ctx,Schedule schedule)
        {
            error = "";
            //设置上下文组织用户信息
            ctx.CurrentOrganizationInfo = new OrganizationInfo();
            ctx.CurrentOrganizationInfo.ID = 100026;
            ctx.CurrentOrganizationInfo.Name = "宏明双新科技";
            ctx.UserId = 11011572;
            ctx.UserName = "李斗星";
            this.GetWLPH(ctx);
            string wmssql = @"/*dialect*/select distinct FWL,b.FNUMBER,FKCS,FCK,FCW,ISNULL(FMTONO,'') FMTONO,1 wms仓库
                from keed_t_Cust100353 a inner join T_BD_LOTMASTER b on a.FPH=b.FLOTID
                inner join T_BD_STOCK ck on a.FCK = ck.FSTOCKID
                inner join T_STK_INVENTORY kc on kc.FMATERIALID=a.FWL and kc.FSTOCKID = a.FCK and kc.FLOT=a.FPH and kc.FBASEQTY > 0 and kc.FSTOCKSTATUSID=10000                
                where a.FSFJK=1 and ck.FNUMBER in ('260CK091','260CK092','260CK093','260CK067','260CK057','260CK028')";
            DynamicObjectCollection wms = DBUtils.ExecuteDynamicObject(ctx, wmssql);
            foreach(DynamicObject obj in wms)
            {
                //wms仓,分单创建
                this.Create_KCZH(ctx, new DynamicObjectCollection(wms.DynamicCollectionItemPropertyType) { obj });
            }
            string fwmssql = @"/*dialect*/select distinct FWL,b.FNUMBER,FKCS,FCK,FCW,ISNULL(FMTONO,'') FMTONO,0 wms仓库
                from keed_t_Cust100353 a inner join T_BD_LOTMASTER b on a.FPH=b.FLOTID
                inner join T_BD_STOCK ck on a.FCK = ck.FSTOCKID
                inner join T_STK_INVENTORY kc on kc.FMATERIALID=a.FWL and kc.FSTOCKID = a.FCK and kc.FLOT=a.FPH and kc.FBASEQTY > 0 and kc.FSTOCKSTATUSID=10000               
                where a.FSFJK=1 and ck.FNUMBER not in ('260CK091','260CK092','260CK093','260CK067','260CK057','260CK028')";
            DynamicObjectCollection fwms = DBUtils.ExecuteDynamicObject(ctx, fwmssql);
            if (fwms.Count > 0)
            {
                //非wms仓，整单创建
                this.Create_KCZH(ctx, fwms);
            }
            if (error != "")
            {
                throw new Exception(error);
            }
        }
        //01查找到期的物料批号
        private void GetWLPH(Context ctx)
        {
            string usql = "/*dialect*/update keed_t_Cust100353 set FSFJK=0 where FSFJK=1";
            DBUtils.Execute(ctx, usql);
            string hsql = "/*dialect*/select top 1 FID from keed_t_Cust100353 order by FID DESC";           
            fid = 1+ DBUtils.ExecuteScalar<int>(ctx, hsql, 0);
            pgids.Clear();
            string sql = @"/*dialect*/select 物料ID,批号ID from
                (select ph.FMATERIALID 物料ID, ph.FLOTID 批号ID,F_260_YJTS 预警天数,TRY_CAST(SUBSTRING(REPLACE(ph.FNUMBER,'260','20'), 1, 8) as datetime) 日期
                from T_BD_MATERIAL wl inner join T_BD_LOTMASTER ph on ph.FMATERIALID = wl.FMATERIALID and ph.FCHECKBOX = 0 and FBIZTYPE='1'
                where wl.F_260_WLYJLX = 'B' and wl.FDOCUMENTSTATUS='C' and wl.FFORBIDSTATUS='A' 
                and (TRY_CAST(SUBSTRING(REPLACE(ph.FNUMBER,'260','20'), 1, 4) as int) = 2022 and TRY_CAST(SUBSTRING(REPLACE(ph.FNUMBER,'260','20'), 5, 2) as int)>=7 or TRY_CAST(SUBSTRING(REPLACE(ph.FNUMBER,'260','20'), 1, 4) as int)> 2022)
                )A where 日期 is not null and DATEDIFF(day, 日期, GETDATE()) >= 预警天数";
            DynamicObjectCollection phobjs = DBUtils.ExecuteDynamicObject(ctx, sql);
            string phs = "";
            foreach(DynamicObject phobj in phobjs)
            {
                long wlid = Convert.ToInt64(phobj["物料ID"]);
                long phid = Convert.ToInt64(phobj["批号ID"]);
                this.KCPD(ctx,wlid,phid,wlid,phid);
                this.GetXJWL(ctx, wlid, phid);
                phs += phid.ToString()+",";
            }
            if (phs != "")
            {
                string phsql = $"/*dialect*/update T_BD_LOTMASTER set FCHECKBOX=1 where FLOTID in ({phs.Substring(0, phs.Length - 1)})";
                DBUtils.Execute(ctx, phsql);
            }            
        }
        //查找下级物料及批号
        private void GetXJWL(Context ctx,long wlid,long phid)
        {
            string llsql = $@"/*dialect*/select distinct 派工ID from 
                (select F_RUJP_PGENTRYID 派工ID,FACTUALQTY 数量 from T_PRD_PICKMTRLDATA where FMATERIALID={wlid} and FLOT={phid}
                union all              
                select F_RUJP_PGENTRYID 派工ID,FACTUALQTY 数量 from T_PRD_FEEDMTRLDATA a 
                inner join T_PRD_FEEDMTRLDATA_Q q on a.FENTRYID=q.FENTRYID where FMATERIALID={wlid} and FLOT={phid}
                union all
                select F_RUJP_PGENTRYID 派工ID,-FQTY 数量 from T_PRD_RETURNMTRLENTRY where FMATERIALID={wlid} and FLOT={phid})B where 数量>0";
            DynamicObjectCollection rkobjs = DBUtils.ExecuteDynamicObject(ctx, llsql);
            foreach(DynamicObject rkobj in rkobjs)
            {
                long pgid = Convert.ToInt64(rkobj["派工ID"]);
                if (pgids.Contains(pgid)) { continue; }                
                string rksql = $@"/*dialect*/select FMATERIALID,FLOT from T_PRD_INSTOCKENTRY 
                    where SUBSTRING(F_RUJP_PGBARCODE,CHARINDEX('-',F_RUJP_PGBARCODE)+1,10)= '{pgid}'";
                DynamicObjectCollection objs = DBUtils.ExecuteDynamicObject(ctx, rksql);
                foreach(DynamicObject obj in objs)
                {
                    long wl = Convert.ToInt64(obj["FMATERIALID"]);
                    long ph = Convert.ToInt64(obj["FLOT"]);
                    this.KCPD(ctx, wl, ph,wlid,phid);
                    this.GetXJWL(ctx, wl, ph);
                }
                pgids.Add(pgid);
            }
        }
        //判断是否有库存
        private void KCPD(Context ctx,long wlid,long phid,long cswl,long csph)
        {
            string kcsql = $@"/*dialect*/select kc.FBASEQTY 库存数,ck.FSTOCKID 仓库,kc.FSTOCKLOCID 仓位,kc.FMTONO 计划跟踪号 from T_STK_INVENTORY kc
                inner join T_BD_STOCKSTATUS_L zt on zt.FSTOCKSTATUSID = kc.FSTOCKSTATUSID and zt.FLOCALEID = 2052
                inner join T_BD_STOCK_L ck on kc.FSTOCKID = ck.FSTOCKID and ck.FNAME!='模具设计部金属带料仓库' and ck.FNAME not like '%VMI%' and ck.FNAME not like '%不良%' and ck.FNAME not like '%委外%'
                where kc.FSTOCKORGID = 100026 and kc.FBASEQTY > 0 and zt.FNAME = '可用' and kc.FMATERIALID ={wlid} and kc.FLOT ={phid}";
            DynamicObjectCollection kcs = DBUtils.ExecuteDynamicObject(ctx, kcsql);
            foreach(DynamicObject kc in kcs)
            {
                string sql = $"/*dialect*/insert into keed_t_Cust100353(FID,FWL,FPH,FKCS,FCK,FCSWL,FCSPH,FSFJK,FCW) values({fid},{wlid},{phid},{kc["库存数"]},{kc["仓库"]},{cswl},{csph},1,{kc["仓位"]})";
                DBUtils.Execute(ctx, sql);
                fid++;
            }         
        }
        //02创建状态转换单
        private void Create_KCZH(Context ctx,DynamicObjectCollection objs)
        {        
            IBillView billView = this.CreateBillView("STK_StockConvert",ctx);
            ((IBillViewService)billView).LoadData();
            DynamicFormViewPlugInProxy eventProxy = billView.GetService<DynamicFormViewPlugInProxy>();
            eventProxy.FireOnLoad();
            this.FillPropertys(billView,objs);
            this.SaveBill(billView, ctx);
        }
        private IBillView CreateBillView(String TableName,Context ctx)
        {
            FormMetadata meta = MetaDataServiceHelper.Load(ctx, TableName) as FormMetadata;
            Form form = meta.BusinessInfo.GetForm();
            Type type = Type.GetType("Kingdee.BOS.Web.Import.ImportBillView,Kingdee.BOS.Web");
            var billView = (IDynamicFormViewService)Activator.CreateInstance(type);
            BillOpenParameter openParam = CreateOpenParameter(meta,ctx);
            var provider = form.GetFormServiceProvider();
            billView.Initialize(openParam, provider);
            return billView as IBillView;
        }
        private BillOpenParameter CreateOpenParameter(FormMetadata meta,Context ctx)
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
            dynamicFormView.SetItemValueByID("FStockOrgId",100026, 0);
            dynamicFormView.UpdateValue("FNote", 0, "执行计划生成");
            dynamicFormView.SetItemValueByNumber("FDeptId", "000430", 0);
            dynamicFormView.UpdateValue("F_260_WMSCK", 0, Convert.ToInt32(Dyobj[0]["wms仓库"]));
            ((IDynamicFormView)dynamicFormView).InvokeFieldUpdateService("FStockOrgId", 0);           
            billView.Model.DeleteEntryData("FEntity");                   
            int ROW = 0;
            foreach (DynamicObject obj in Dyobj)
            {
                billView.Model.BatchCreateNewEntryRow("FEntity", 2);
                long wl = Convert.ToInt64(obj["FWL"]);
                string ph = obj["FNUMBER"].ToString();
                long ck = Convert.ToInt64(obj["FCK"]);
                string jhgzh = obj["FMTONO"].ToString();
                double kcs = Convert.ToDouble(obj["FKCS"]);
                long cw = Convert.ToInt64(obj["FCW"]);
                //转换前
                dynamicFormView.UpdateValue("FConvertType", ROW, "A");                
                dynamicFormView.SetItemValueByID("FMaterialId", wl, ROW);
                dynamicFormView.UpdateValue("FLot",ROW, ph);               
                dynamicFormView.SetItemValueByID("FStockId", ck, ROW);
                dynamicFormView.UpdateValue("FConvertQty", ROW, kcs);
                dynamicFormView.UpdateValue("FMTONo", ROW, jhgzh);
                dynamicFormView.SetItemValueByNumber("FStockStatus", "KCZT01_SYS", ROW);
                dynamicFormView.SetItemValueByID("FStockLocId", cw, ROW);
                //转换后
                ROW = ROW + 1;
                dynamicFormView.UpdateValue("FConvertType", ROW, "B");
                dynamicFormView.SetItemValueByID("FMaterialId", wl, ROW);
                dynamicFormView.UpdateValue("FLot", ROW, ph);
                dynamicFormView.SetItemValueByID("FStockId", ck, ROW);
                dynamicFormView.UpdateValue("FConvertQty", ROW, kcs);
                dynamicFormView.UpdateValue("FMTONo", ROW, jhgzh);
                dynamicFormView.SetItemValueByNumber("FStockStatus", "KCZT009", ROW);
                dynamicFormView.SetItemValueByID("FStockLocId", cw, ROW);                
                ROW = ROW + 1;
            }           
        }
        //保存、提交、审核单据
        private void SaveBill(IBillView billView,Context ctx)
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
                foreach (var err in saveResult.ValidationErrors)
                {
                    error += err.Message + ";";
                }
            }
        }
    }
}
