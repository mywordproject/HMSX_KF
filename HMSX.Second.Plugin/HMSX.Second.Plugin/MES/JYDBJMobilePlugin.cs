using System;
using System.Collections.Generic;
using System.ComponentModel;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.Utils;
using Kingdee.BOS.Mobile.PlugIn;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Mobile.PlugIn.ControlModel;
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.ComplexCacheJson.Utils;
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.ComplexCacheJson.Model;
using Kingdee.BOS.JSON;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.WebApi.FormService;
using Newtonsoft.Json.Linq;
using System.Linq;
using Newtonsoft.Json;
using Kingdee.BOS;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Resource;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Mobile;

namespace HMSX.Second.Plugin.MES
{
    [Description("检验单编辑--移动端")]
    [Kingdee.BOS.Util.HotUpdate]
    public class JYDBJMobilePlugin : AbstractMobilePlugin
    {
        protected FormCacheModel cacheModel4Save = new FormCacheModel();
        protected bool HasCached;
        private DynamicObject date;
        private string Name;
        public override void OnInitialize(InitializeEventArgs e)
        {
            base.OnInitialize(e);
            date = e.Paramter.GetCustomParameters()["date"] as DynamicObject;
            Name = e.Paramter.GetCustomParameter("Name").ToString();
        }
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            this.Model.SetValue("FID", date["F_FID"]);
            this.Model.SetValue("FENTRYID", date["F_FENTRYID"]);
            this.Model.SetValue("FDETAILID", date["F_FDETAILID"]);
            this.Model.SetValue("F_MC", Name);
            this.Model.SetValue("F_WLMC", date["F_CPXX"]);
            this.Model.SetValue("F_PH", date["F_PH"]);
            this.Model.SetValue("F_JYSL", date["F_JYSL"]);
            this.Model.SetValue("F_SYJC", date["F_SYJC"]);
            this.Model.SetValue("F_ZT", date["F_ZT"]);
            this.Model.SetValue("F_BLCL", date["F_BLCL"]);
            if (Name == "新增")
            {
                string cxsql = $@"SELECT A.FID,B.FENTRYID,case when (F_260_HBS-sum(FQTY))<0 then 0 else (F_260_HBS-sum(FQTY))end  FQTY
               FROM T_QM_INSPECTBILL A
               INNER JOIN T_QM_INSPECTBILLENTRY B ON A.FID=B.FID
               LEFT JOIN T_QM_IBPOLICYDETAIL C ON B.FENTRYID=C.FENTRYID
               where  (A.FDOCUMENTSTATUS='A' OR A.FDOCUMENTSTATUS='D') AND FINSPECTORGID=100026
               AND A.FID='{date["F_FID"]}' and B.FENTRYID={date["F_FENTRYID"]}
               GROUP BY A.FID,B.FENTRYID,F_260_HBS";
                var cx = DBUtils.ExecuteDynamicObject(Context, cxsql);
                if (cx.Count > 0)
                {
                    this.Model.SetValue("F_SL", cx[0]["FQTY"]);
                }
            }
            else
            {
                this.Model.SetValue("F_SL", date["F_JCSL"]);
            }
            this.Model.SetValue("F_JYMS", date["F_JYMS"]);
            this.View.Model.SetValue("F_ZJY", date["F_ZJY_Id"]);
            this.View.Model.SetValue("F_QRR", date["F_QRR_Id"]);
            this.View.UpdateView("F_WLMC");
            this.View.UpdateView("F_PH");
            this.View.UpdateView("F_JYSL");
            this.View.UpdateView("F_SYJC");
            this.View.UpdateView("F_SL");
            this.View.UpdateView("F_ZT");
            this.View.UpdateView("F_ZJY");
            this.View.UpdateView("F_QRR");
            this.View.UpdateView("F_JYMS");
            this.View.UpdateView("F_BLCL");
            this.View.UpdateView("F_MC");
        }
        public override void ButtonClick(ButtonClickEventArgs e)
        {
            base.ButtonClick(e);
            string key;
            switch (key = e.Key.ToUpper())
            {
                case "F_SUBMIT":
                    if(this.Model.GetValue("F_ZT").ToString() == "2" && this.Model.GetValue("F_QRR")== null) 
                    {
                        base.View.ShowMessage(ResManager.LoadKDString("不合格时，确认人必填！", "015747000026506", SubSystemType.MFG, new object[0]), MessageBoxType.Notice);
                        return;
                    }
                    else
                    {
                        if (Name == "新增")
                        {
                            New();
                            return;
                        }
                        else
                        {
                            Edit();
                            return;
                        }
                    }
                    
                case "FBUTTON_RETURN":
                    JsonCacheUtils.DeleteCache(base.Context, this.cacheModel4Save.DeviceCode, this.HasCached);
                    base.View.Close();
                    return;
                case "FBUTTON_LOGOUT":
                    JsonCacheUtils.DeleteCache(base.Context, this.cacheModel4Save.DeviceCode, this.HasCached);
                    LoginUtils.LogOut(base.Context, base.View);
                    base.View.Logoff("indexforpad.aspx");
                    return;

            }
        }
        public void Edit()
        {
            string FID = this.Model.GetValue("FID").ToString();
            string FENTRYID = this.Model.GetValue("FENTRYID").ToString();
            string FDETAILID = this.Model.GetValue("FDETAILID").ToString();
            string FUSEPOLICY = this.Model.GetValue("F_SYJC").ToString();//使用决策
            string FSTATUS = this.Model.GetValue("F_ZT").ToString();//状态
            decimal FQTY = this.Model.GetValue("F_SL") == null ? 0 : Convert.ToDecimal(this.Model.GetValue("F_SL").ToString());//数量
            string FISDEFECTPROCESS = this.Model.GetValue("F_BLCL").ToString();//不良品处理
            string FINSPECTORID = this.Model.GetValue("F_ZJY") == null ? "" : ((DynamicObject)this.Model.GetValue("F_ZJY"))["Number"].ToString();//质检员
            string RBJSQRR = this.Model.GetValue("F_QRR") == null ? "" : ((DynamicObject)this.Model.GetValue("F_QRR"))["FStaffNumber"].ToString();//确认人
            string FMEMO = this.Model.GetValue("F_JYMS") == null ? "" : this.Model.GetValue("F_JYMS").ToString();//结果描述
            JObject json = new JObject();
            json.Add("IsDeleteEntry", false);
            JObject model = new JObject();
            JArray FEntity = new JArray();
            JObject Entity = new JObject();
            Entity.Add("FENTRYID", FENTRYID);
            JArray FPolicyDetail = new JArray();
            JObject FDetailID = new JObject();
            FDetailID.Add("FDetailID", FDETAILID);
            FDetailID.Add("FPolicyStatus", FSTATUS);//状态
            FDetailID.Add("FUsePolicy", FUSEPOLICY);//使用决策
            FDetailID.Add("FPolicyQty", FQTY);//数量
            FDetailID.Add("FIsDefectProcess", FISDEFECTPROCESS);//不良品处理
            JObject F_260_RBJSQRR = new JObject();
            F_260_RBJSQRR.Add("FSTAFFNUMBER", RBJSQRR);//确认人
            FDetailID.Add("F_260_RBJSQRR", F_260_RBJSQRR);
            FDetailID.Add("FMemo1", FMEMO);//检验描述
            FPolicyDetail.Add(FDetailID);
            Entity.Add("FPolicyDetail", FPolicyDetail);
            FEntity.Add(Entity);
            model.Add("FID", FID);
            JObject FSourceOrgId = new JObject();
            FSourceOrgId.Add("FNumber", 260);//来源组织
            model.Add("FSourceOrgId", FSourceOrgId);
            JObject FInspectOrgId = new JObject();
            FInspectOrgId.Add("FNumber", 260);//质检组织
            model.Add("FInspectOrgId  ", FInspectOrgId);
            JObject FInspectorId = new JObject();
            FInspectorId.Add("FNumber", FINSPECTORID);//质检员
            model.Add("FInspectorId ", FInspectorId);
            model.Add("FEntity", FEntity);
            json.Add("Model", model);
            var result = WebApiServiceCall.Save(this.Context, "QM_InspectBill", json.ToString());
            bool isSuccess = Convert.ToBoolean(JObject.Parse(JsonConvert.SerializeObject(result))["Result"]["ResponseStatus"]["IsSuccess"].ToString());
            string c = KDObjectConverter.SerializeObject(result);
            if (isSuccess)
            {
                this.View.ShowMessage("修改成功，是否退出该界面？",
                                 MessageBoxOptions.YesNo,
                                 new Action<MessageBoxResult>((results) =>
                                 {
                                     if (results == MessageBoxResult.Yes)
                                     {
                                         JsonCacheUtils.DeleteCache(base.Context, this.cacheModel4Save.DeviceCode, this.HasCached);
                                         base.View.Close();
                                         return;
                                     }
                                     else if (results == MessageBoxResult.No)
                                     {

                                     }
                                 }));
               // base.View.ShowMessage(ResManager.LoadKDString("修改成功！", "015747000026506", SubSystemType.MFG, new object[0]), MessageBoxType.Notice);               
            }
            else
            {
                string Errors = JObject.Parse(JsonConvert.SerializeObject(result))["Result"]["ResponseStatus"]["Errors"].ToString();
                var JErrors = JArray.Parse(Errors);
                string message = "";
                foreach (var Error in JErrors)
                {
                    message += ((JObject)Error)["Message"].ToString() + ",";
                }
                throw new KDBusinessException("", message);
            }

        }
        public void New()
        {
            string FID = this.Model.GetValue("FID").ToString();
            string FENTRYID = this.Model.GetValue("FENTRYID").ToString();
            string FUSEPOLICY = this.Model.GetValue("F_SYJC").ToString();//使用决策
            string FSTATUS = this.Model.GetValue("F_ZT").ToString();//状态
            decimal FQTY = this.Model.GetValue("F_SL") == null ? 0 : Convert.ToDecimal(this.Model.GetValue("F_SL").ToString());//数量
            string FISDEFECTPROCESS = this.Model.GetValue("F_BLCL").ToString();//不良品处理
            string RBJSQRR = this.Model.GetValue("F_QRR") == null ? "" : ((DynamicObject)this.Model.GetValue("F_QRR"))["FStaffNumber"].ToString();//确认人
            string FMEMO = this.Model.GetValue("F_JYMS") == null ? "" : this.Model.GetValue("F_JYMS").ToString();//结果描述
            JObject json = new JObject();
            json.Add("IsDeleteEntry", false);
            JObject model = new JObject();
            JArray FEntity = new JArray();
            JObject Entity = new JObject();
            Entity.Add("FENTRYID", FENTRYID);
            JArray FPolicyDetail = new JArray();
            JObject FDetailID = new JObject();
            FDetailID.Add("FDetailID", 0);
            FDetailID.Add("FPolicyStatus", FSTATUS);//状态
            FDetailID.Add("FUsePolicy", FUSEPOLICY);//使用决策
            FDetailID.Add("FPolicyQty", FQTY);//数量
            FDetailID.Add("FIsDefectProcess", FISDEFECTPROCESS);//不良品处理
            JObject F_260_RBJSQRR = new JObject();
            F_260_RBJSQRR.Add("FStaffNumber", RBJSQRR);//确认人
            FDetailID.Add("F_260_RBJSQRR", F_260_RBJSQRR);
            FDetailID.Add("FMemo1", FMEMO);//检验描述
            FPolicyDetail.Add(FDetailID);
            Entity.Add("FPolicyDetail", FPolicyDetail);
            FEntity.Add(Entity);
            model.Add("FID", FID);
            model.Add("FEntity", FEntity);
            json.Add("Model", model);
            var result = WebApiServiceCall.Save(this.Context, "QM_InspectBill", json.ToString());
            bool isSuccess = Convert.ToBoolean(JObject.Parse(JsonConvert.SerializeObject(result))["Result"]["ResponseStatus"]["IsSuccess"].ToString());
            string c = KDObjectConverter.SerializeObject(result);
            if (isSuccess)
            {
                this.View.ShowMessage("新增成功，是否退出该界面？",
                                 MessageBoxOptions.YesNo,
                                 new Action<MessageBoxResult>((results) =>
                                 {
                                     if (results == MessageBoxResult.Yes)
                                     {
                                         JsonCacheUtils.DeleteCache(base.Context, this.cacheModel4Save.DeviceCode, this.HasCached);
                                         base.View.Close();
                                         return;
                                     }
                                     else if (results == MessageBoxResult.No)
                                     {

                                     }
                                 }));
            }
            else
            {
                string Errors = JObject.Parse(JsonConvert.SerializeObject(result))["Result"]["ResponseStatus"]["Errors"].ToString();
                var JErrors = JArray.Parse(Errors);
                string message = "";
                foreach (var Error in JErrors)
                {
                    message += ((JObject)Error)["Message"].ToString() + ",";
                }
                throw new KDBusinessException("", message);
            }

        }
    }
}
