using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.Interaction;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS.Mobile.PlugIn;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.ComplexCacheJson.Model;
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.ComplexCacheJson.Utils;
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;


namespace HMSX.SCZZ.CJGL.MES
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("移动表单_调拨单列表")]
    public class DBDLBPad:AbstractMobilePlugin
    {
        protected FormCacheModel cacheModel4Save = new FormCacheModel();
        protected bool HasCached;
        public override void OnInitialize(InitializeEventArgs e)
        {
            base.OnInitialize(e);
            this.View.GetControl("FLableUser").SetValue(this.Context.UserName);
        }
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            filldata("");
        }
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (e.Field.Key == "FGLWLDM" || e.Field.Key == "FGLPH")
            {
                var wldm = this.View.Model.GetValue("FGLWLDM");
                var ph = this.View.Model.GetValue("FGLPH");
                ph = ph == null ? "" : ph.ToString();
                wldm = wldm == null ? "" : wldm.ToString();
                string sql = string.Format(" and wl.FNUMBER like '%{0}%' and b.FLOT_TEXT like '%{1}%'", wldm, ph);
                filldata(sql);
            }
        }
        //按钮点击
        public override void ButtonClick(ButtonClickEventArgs e)
        {
            base.ButtonClick(e);
            DynamicObjectCollection entitys = this.Model.DataObject["MobileListViewEntity"] as DynamicObjectCollection;
            switch (e.Key.ToUpper())
            {
                //提交
                case "FBUTTONSUBMIT":
                    int[] SubmitRows = this.View.GetControl<EntryGrid>("FMobileListViewEntity").GetSelectedRows();
                    List<object> Submitlist = new List<object>();
                    foreach (int i in SubmitRows)
                    {
                        DynamicObject entity = entitys[i];
                        Submitlist.Add(entity["FData_FID"]);
                    }
                    List<object> Submitfids = Submitlist.Distinct().ToList();
                    string Submitresult = Submit(Submitfids);
                    this.View.ShowMessage(Submitresult);
                    return;
                //审核
                case "FBUTTONAUDIT":
                    int[] AuditRows = this.View.GetControl<EntryGrid>("FMobileListViewEntity").GetSelectedRows();
                    List<object> Auditlist = new List<object>();
                    foreach (int i in AuditRows)
                    {
                        DynamicObject entity = entitys[i];
                        Auditlist.Add(entity["FData_FID"]);
                    }
                    List<object> Auditfids = Auditlist.Distinct().ToList();
                    string Auditresult = Audit(Auditfids);
                    this.View.ShowMessage(Auditresult);
                    return;
                //返回
                case "FBUTTONCLOSE":
                    JsonCacheUtils.DeleteCache(base.Context, this.cacheModel4Save.DeviceCode, this.HasCached);
                    base.View.Close();
                    return;
                //注销
                case "FBUTTONLOGOUT":
                    JsonCacheUtils.DeleteCache(base.Context, this.cacheModel4Save.DeviceCode, this.HasCached);
                    LoginUtils.LogOut(base.Context, base.View);
                    base.View.Logoff("indexforpad.aspx");
                    return;
            }
        }
        //提交单据
        private string Submit(List<object> fids)
        {
            FormMetadata cachedFormMetaData = FormMetaDataCache.GetCachedFormMetaData(this.Context, "STK_TransferDirect");
            IOperationResult operationResult = BusinessDataServiceHelper.Submit(this.Context, cachedFormMetaData.BusinessInfo, fids.ToArray(), "Submit", null);
            string message="提交失败！";
            if (operationResult.IsSuccess)
            {
                this.View.Refresh();
                message = "提交成功";
            }
            else
            {
                foreach (ValidationErrorInfo error in operationResult.ValidationErrors)
                {
                    message += error.Message + "\r\n";
                }

            }
            return message;
        }
        //审核单据
        private string Audit(List<object> fids)
        {
            FormMetadata cachedFormMetaData = FormMetaDataCache.GetCachedFormMetaData(this.Context, "STK_TransferDirect");
            OperateOption option = OperateOption.Create();
            option.AddInteractionFlag("Kingdee.K3.SCM.App.Core.AppBusinessService.UpdateStockService,Kingdee.K3.SCM.App.Core");
            option.SetIgnoreInteractionFlag(true);
            IOperationResult operationResult = BusinessDataServiceHelper.Audit(this.Context, cachedFormMetaData.BusinessInfo, fids.ToArray(), option);
            string message="审核失败!";
            if (operationResult.IsSuccess)
            {
                this.View.Refresh();
                message = "审核成功";
            }
            else 
            {
                foreach(ValidationErrorInfo error in operationResult.ValidationErrors)
                {
                    message += error.Message+"\r\n";
                }
                
            }
            return message;
        }
        private void filldata(string sql)
        {
            string DBSQL = string.Format(@"/*dialect*/select a.FBILLNO 单据编号,wl.FNUMBER 物料编码,wla.FNAME 物料名称,b.FLOT_TEXT 批号,b.FQTY 调拨数量,
                a.FID,cck.FNAME 调出仓库,rck.FNAME 调入仓库,
                case when a.FDOCUMENTSTATUS='A' then '创建'
                     when a.FDOCUMENTSTATUS='B' then '审核中'
                     else '重新审核' end 单据状态
                from T_STK_STKTRANSFERIN a
                inner join T_STK_STKTRANSFERINENTRY b on a.FID=b.FID
                inner join T_BD_MATERIAL wl on wl.FMATERIALID=b.FSRCMATERIALID
                inner join T_BD_MATERIAL_L wla on wla.FMATERIALID=b.FSRCMATERIALID
                inner join T_BD_STOCK_L cck on cck.FSTOCKID=b.FSRCSTOCKID
                inner join T_BD_STOCK_L rck on rck.FSTOCKID=b.FDESTSTOCKID
                inner join (select distinct bm.FWIPSTOCKID from T_SEC_user yh
                    inner join T_HR_EMPINFO yg on yh.FLINKOBJECT=yg.FPERSONID
                    inner join T_BD_STAFF rg on yg.FID=rg.FEMPINFOID and rg.FFORBIDSTATUS='A'
                    inner join T_BD_DEPARTMENT bm on bm.FDEPTID=rg.FDEPTID
                    where yh.FUSERID={0} and yg.FUSEORGID=100026
                    union all
                    select distinct 27601080 FWIPSTOCKID from T_SEC_user yh
                    inner join T_HR_EMPINFO yg on yh.FLINKOBJECT=yg.FPERSONID
                    inner join T_BD_STAFF rg on yg.FID=rg.FEMPINFOID and rg.FFORBIDSTATUS='A'
                    inner join T_BD_DEPARTMENT bm on bm.FDEPTID=rg.FDEPTID
                    where yh.FUSERID={0} and yg.FUSEORGID=100026 and bm.FNUMBER='000341') 
                WIP on WIP.FWIPSTOCKID=b.FSRCSTOCKID or WIP.FWIPSTOCKID=b.FDESTSTOCKID
                where a.FSTOCKOUTORGID=100026 and a.FDOCUMENTSTATUS in ('A','D','B'){1}
                order by a.FBILLNO,b.FLOT_TEXT", this.Context.UserId,sql);
            DynamicObjectCollection DBDS = DBUtils.ExecuteDynamicObject(this.Context, DBSQL);
            this.View.Model.DeleteEntryData("FMobileListViewEntity");
            this.View.Model.DeleteEntryRow("FMobileListViewEntity", 0);
            double sum = 0;
            for (int i = 0; i < DBDS.Count; i++)
            {
                this.View.Model.CreateNewEntryRow("FMobileListViewEntity");
                this.View.Model.SetValue("FData_XH", i+1, i);
                this.View.Model.SetValue("FData_DJBH", DBDS[i]["单据编号"], i);
                this.View.Model.SetValue("FData_DJZT", DBDS[i]["单据状态"], i);
                this.View.Model.SetValue("FData_WLBM", DBDS[i]["物料编码"], i);
                this.View.Model.SetValue("FData_WLMC", DBDS[i]["物料名称"], i);
                this.View.Model.SetValue("FData_PH", DBDS[i]["批号"], i);
                this.View.Model.SetValue("FData_DBSL", DBDS[i]["调拨数量"], i);
                this.View.Model.SetValue("FData_DCCK", DBDS[i]["调出仓库"], i);
                this.View.Model.SetValue("FData_DRCK", DBDS[i]["调入仓库"], i);
                this.View.Model.SetValue("FData_FID", DBDS[i]["FID"], i);
                sum +=Convert.ToDouble(DBDS[i]["调拨数量"]);
                this.View.UpdateView("FMobileListViewEntity");
            }
            this.View.Model.SetValue("FData_SLHJ", sum, 0);
            this.View.UpdateView("FData_SLHJ", 0);
        }
    }
}
