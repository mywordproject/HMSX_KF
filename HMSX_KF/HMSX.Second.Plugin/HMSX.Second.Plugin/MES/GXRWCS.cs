using HMSX.MFG.Mobile.Business.PlugIn;
using HMSX.Second.Plugin.供应链;
using Kingdee.BOS;
using Kingdee.BOS.App.Core.Utils;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.NetworkCtrl;
using Kingdee.BOS.Mobile.PlugIn.ControlModel;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Resource;
using Kingdee.BOS.ServiceHelper;
using Kingdee.K3.Core.Mobile.Utils;
using Kingdee.K3.MFG.Common.BusinessEntity.SFC.SFCDymObjManager.SFC.Bill;
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace HMSX.Second.Plugin.MES
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("工序任务超市--领料数大于零，不允许关闭")]
    public class GXRWCS : MobileComplexTaskPoolListEdit
    {
        protected Dictionary<int, int> SelectedDataIndex = new Dictionary<int, int>();
        public override void ButtonClick(ButtonClickEventArgs e)
        {
            base.ButtonClick(e);
            string a = e.Key.ToUpper();
            switch (a)
            {
                case "F_260_QX":
                    AllSelect("FMobileListViewEntity_Detail");
                    break;
                case "F_260_PLGB":
                    this.CloseRow1(false);
                    break;
                case "F_DYBQ":
                    this.printLable1();
                    break;
            }
        }
        protected void AllSelect(string entityKey)
        {
            int entryRowCount = this.Model.GetEntryRowCount(entityKey);
            List<int> list = new List<int>();
            for (int i = 0; i < entryRowCount; i++)
            {
                list.Add(i);
                this.ListFormaterManager.SetControlProperty("FFlowLayout_Detail", i, "255,234,199", MobileFormatConditionPropertyEnums.BackColor);
            }
            base.View.GetControl<MobileListViewControl>(entityKey).SetSelectRows(list.ToArray());
            base.View.GetControl<MobileListViewControl>(entityKey).SetFormat(this.ListFormaterManager);
            this.View.UpdateView(entityKey);
        }
        protected void CloseRow1(bool flag)
        {
            int[] selectedRows = base.View.GetControl<MobileListViewControl>("FMobileListViewEntity_Detail").GetSelectedRows();
            if (!selectedRows.Any<int>())
            {
                base.View.ShowStatusBarInfo(Kingdee.BOS.Resource.ResManager.LoadKDString("未选择分录！", "015747000028217", Kingdee.BOS.Resource.SubSystemType.MFG, new object[0]));
            }
            else
            {
                List<Dictionary<string, object>> dictionarys = new List<Dictionary<string, object>>();
                for (int i = 0; i < selectedRows.Length; i++)
                {
                    int num = selectedRows[i] + this.RowCountPerPage * (this.detailCurrPageIndex - 1);
                    System.Collections.Generic.Dictionary<string, object> dictionary = this.detailTableData[num];
                    bool flag3 = dictionary != null;
                    if (flag3)
                    {
                        bool flag4 = Convert.ToDouble(dictionary["F_260_LLSL"].ToString()) > 0.0;
                        if (flag4)
                        {
                            throw new KDBusinessException("", "领料数量大于0不允许关闭！");
                        }
                    }
                    dictionarys.Add(dictionary);
                }            
                foreach (var dictionary in dictionarys)
                {
                    //try
                    //{
                    string fsjysql = $@"select FNUMBER,FNAME from  T_BAS_PREBDFIVE a
                                           inner join T_BAS_PREBDFIVE_L b on a.FID=b.FID
                                           WHERE FNAME='关闭校验'";
                    var fsjy = DBUtils.ExecuteDynamicObject(Context, fsjysql);
                    if (fsjy.Count > 0)
                    {
                        Encoding encoding = Encoding.UTF8;
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(fsjy[0]["FNUMBER"].ToString());
                        request.Method = "POST";
                        request.ContentType = "application/json; charset=UTF-8";
                        request.Headers["Accept-Encoding"] = "gzip, deflate";
                        request.AutomaticDecompression = DecompressionMethods.GZip;
                        JObject jsonRoot = new JObject();
                        jsonRoot.Add("fbillno", dictionary["FBarCode"].ToString());
                        jsonRoot.Add("fbilltype", "派工明细");
                        byte[] buffer = encoding.GetBytes(jsonRoot.ToString());
                        request.ContentLength = buffer.Length;
                        request.GetRequestStream().Write(buffer, 0, buffer.Length);
                        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                        using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                        {
                            供应链.WMSclass jsonDatas = JsonConvert.DeserializeObject<供应链.WMSclass>(reader.ReadToEnd());
                            if (jsonDatas.Code != "0")
                            {
                                throw new KDBusinessException("", jsonDatas.Message);
                            }
                        }
                    }
                    string fsjysql1 = $@"select FNUMBER,FNAME from  T_BAS_PREBDFIVE a
                                           inner join T_BAS_PREBDFIVE_L b on a.FID=b.FID
                                           WHERE FNAME='执行关闭'";
                    var fsjy1 = DBUtils.ExecuteDynamicObject(Context, fsjysql1);
                    if (fsjy1.Count > 0)
                    {
                        Encoding encoding1 = Encoding.UTF8;
                        HttpWebRequest request1 = (HttpWebRequest)WebRequest.Create(fsjy1[0]["FNUMBER"].ToString());
                        request1.Method = "POST";
                        request1.ContentType = "application/json; charset=UTF-8";
                        request1.Headers["Accept-Encoding"] = "gzip, deflate";
                        request1.AutomaticDecompression = DecompressionMethods.GZip;
                        JObject jsonRoot1 = new JObject();
                        jsonRoot1.Add("fbillno", dictionary["FBarCode"].ToString());
                        jsonRoot1.Add("fbilltype", "派工明细");
                        byte[] buffer1 = encoding1.GetBytes(jsonRoot1.ToString());
                        request1.ContentLength = buffer1.Length;
                        request1.GetRequestStream().Write(buffer1, 0, buffer1.Length);
                        HttpWebResponse response1 = (HttpWebResponse)request1.GetResponse();
                        using (StreamReader reader = new StreamReader(response1.GetResponseStream(), Encoding.UTF8))
                        {
                            供应链.WMSclass jsonDatas = JsonConvert.DeserializeObject<供应链.WMSclass>(reader.ReadToEnd());
                            if (jsonDatas.Code != "0")
                            {
                                throw new KDBusinessException("", jsonDatas.Message);
                            }
                        }
                    }

                    //}
                    //catch
                    //{
                    //    throw new KDBusinessException("", "访问WMS接口异常");
                    //}
                    System.Collections.Generic.List<string> lstDisPatchIds = new System.Collections.Generic.List<string>
                    {
                    dictionary["PkId"].ToString()
                    };
                    System.Collections.Generic.List<Kingdee.BOS.Core.NetworkCtrl.NetworkCtrlResult> netCtrlDispatchIds = this.GetNetCtrlDispatchIds(lstDisPatchIds);
                    if (netCtrlDispatchIds.Count > 0)
                    {
                        NetworkCtrlServiceHelper.BatchCommitNetCtrl(base.Context, netCtrlDispatchIds);
                        System.Collections.Generic.List<string> list = (
                            from o in netCtrlDispatchIds
                            select o.InterID).ToList<string>();
                        DynamicObject dynamicObject = SFCDispatchManager.Instance.Load(base.Context, list.ToArray()).FirstOrDefault<Kingdee.BOS.Orm.DataEntity.DynamicObject>();
                        string text = System.Convert.ToString(dictionary["PkId"]);
                        object entryId = dictionary["EntryPkId"];
                        DynamicObjectCollection dynamicObjectItemValue = dynamicObject.GetDynamicObjectItemValue<DynamicObjectCollection>("DispatchDetailEntry", null);
                        DynamicObject dynamicObject2 = (
                            from o in dynamicObjectItemValue
                            where entryId.Equals(o["Id"])
                            select o).FirstOrDefault<Kingdee.BOS.Orm.DataEntity.DynamicObject>();
                        if (dynamicObject2 != null)
                        {
                            if (System.Convert.ToDecimal(dynamicObject2["FinishSelQty"]) == 0m)
                            {
                                dynamicObjectItemValue.Remove(dynamicObject2);
                            }
                            else
                            {
                                dynamicObject2["BaseWorkQty"] = dynamicObject2["BaseFinishSelQty"];
                                dynamicObject2["WorkQty"] = dynamicObject2["FinishSelQty"];
                                dynamicObject2["WorkHeadQty"] = dynamicObject2["FinishSelHeadQty"];
                                dynamicObject2["Status"] = "D";
                            }
                        }
                        Kingdee.BOS.Orm.OperateOption operateOption = Kingdee.BOS.Orm.OperateOption.Create();
                        operateOption.SetVariableValue("IsMobileInvoke", true);
                        Kingdee.BOS.Core.DynamicForm.IOperationResult operationResult = SFCDispatchManager.Instance.Save(base.Context, new Kingdee.BOS.Orm.DataEntity.DynamicObject[]
                        {
                        dynamicObject
                        }, operateOption);
                        if (operationResult.IsSuccess)
                        {
                            this.BindDispatchDetailList("");
                            if (!flag)
                            {
                                base.View.ShowStatusBarInfo(Kingdee.BOS.Resource.ResManager.LoadKDString("批量关闭成功！", "015747000026594", Kingdee.BOS.Resource.SubSystemType.MFG, new object[0]));
                            }
                            else
                            {
                                base.View.ShowStatusBarInfo(Kingdee.BOS.Resource.ResManager.LoadKDString("批量删除成功！", "015747000026594", Kingdee.BOS.Resource.SubSystemType.MFG, new object[0]));
                            }
                        }

                    }
                }
            }

        }
        public override void EntityRowDoubleClick(EntityRowClickEventArgs e)
        {
            base.EntityRowDoubleClick(e);
            int entryRowCount = this.Model.GetEntryRowCount("FMobileListViewEntity_Detail");
            int[] selectedRows = base.View.GetControl<MobileListViewControl>("FMobileListViewEntity_Detail").GetSelectedRows();
            this.View.GetControl<MobileListViewControl>("FMobileListViewEntity_Detail").SetSelectRows(selectedRows);
            for (int i = 0; i < entryRowCount; i++)
            {
                if(Array.IndexOf(selectedRows,i)==-1)
                {
                    this.ListFormaterManager.SetControlProperty("FFlowLayout_Detail", i, "255,255,255", MobileFormatConditionPropertyEnums.BackColor);
                }
                else
                {
                    this.ListFormaterManager.SetControlProperty("FFlowLayout_Detail", i, "255,234,199", MobileFormatConditionPropertyEnums.BackColor);
                }
            }
            this.View.GetControl<MobileListViewControl>("FMobileListViewEntity_Detail").SetFormat(this.ListFormaterManager);
            this.View.UpdateView("FMobileListViewEntity_Detail");

        }
        public void printLable1()
        {
            int[] selectedRows = base.View.GetControl<MobileListViewControl>("FMobileListViewEntity_Detail").GetSelectedRows();
            int num = selectedRows.FirstOrDefault<int>() + this.RowCountPerPage * (this.detailCurrPageIndex - 1);
            if (!selectedRows.Any<int>())
            {
                base.View.ShowStatusBarInfo(ResManager.LoadKDString("未选择分录！", "015747000028217", SubSystemType.MFG, new object[0]));
            }
            else
            {
                Dictionary<string, object> dictionary = this.detailTableData[num];
                string billBarCode = dictionary["FBarCode"].ToString();
                this.Print(billBarCode, false);
            }
        }

        /**
        protected void ClaimedModity()
        {
            int[] selectedRows = base.View.GetControl<MobileListViewControl>("FMobileListViewEntity_Detail").GetSelectedRows();
            if (!selectedRows.Any<int>())
            {
                base.View.ShowStatusBarInfo(ResManager.LoadKDString("未选择分录！", "015747000028217", SubSystemType.MFG, new object[0]));
            }
            else
            {
                int num = selectedRows.FirstOrDefault<int>() + this.RowCountPerPage * (this.detailCurrPageIndex - 1);
                Dictionary<string, object> dictionary = this.detailTableData[num - 1];
                decimal dispatchQtyByPK = this.GetDispatchQtyByPK(Convert.ToInt64(dictionary["EntryPkId"]));
                if (Convert.ToDecimal(this.Model.DataObject["ClaimedQty"]) > dispatchQtyByPK || Convert.ToDecimal(this.Model.DataObject["ClaimedQty"]) <= 0m)
                {
                    base.View.ShowErrMessage(ResManager.LoadKDString("数量必须大于0且不能超过选中行的“可认领”数量！", "0151515153512030033830", SubSystemType.MFG, new object[0]), "", MessageBoxType.Notice);
                }
                else
                {
                    if (Convert.ToDateTime(this.Model.DataObject["PlanBeginedTime"]) < Convert.ToDateTime(this.Model.DataObject["PlanBeginedTime"]))
                    {
                        base.View.ShowErrMessage(ResManager.LoadKDString("计划结束时间不能早于计划开始时间！", "0151515153512030033831", SubSystemType.MFG, new object[0]), "", MessageBoxType.Notice);
                    }
                    List<string> lstDisPatchIds = new List<string>
            {
                dictionary["PkId"].ToString()
            };
                    List<NetworkCtrlResult> netCtrlDispatchIds = this.GetNetCtrlDispatchIds(lstDisPatchIds);
                    if (netCtrlDispatchIds.Count > 0)
                    {
                        NetworkCtrlServiceHelper.BatchCommitNetCtrl(base.Context, netCtrlDispatchIds);
                        List<string> list = (
                            from o in netCtrlDispatchIds
                            select o.InterID).ToList<string>();
                        string text = Convert.ToString(dictionary["PkId"]);
                        object entryId = dictionary["EntryPkId"];
                        DynamicObject dynamicObject = SFCDispatchManager.Instance.Load(base.Context, list.ToArray()).FirstOrDefault<DynamicObject>();
                        DynamicObjectCollection dynamicObjectItemValue = dynamicObject.GetDynamicObjectItemValue<DynamicObjectCollection>("DispatchDetailEntry", null);
                        DynamicObject dynamicObject2 = (
                            from o in dynamicObjectItemValue
                            where entryId.Equals(o["Id"])
                            select o).FirstOrDefault<DynamicObject>();
                        if (dynamicObject2 != null)
                        {
                            if (this.curUnitConvert == null)
                            {
                                this.curUnitConvert = this.GetUnitConvert(Convert.ToInt64(dynamicObject["MaterialId_Id"]), Convert.ToInt64(dynamicObject["FUnitID_Id"]), Convert.ToInt64(dynamicObject["BaseUnitID_Id"]));
                            }
                            decimal num2 = Convert.ToDecimal(this.Model.DataObject["ClaimedQty"]) * Convert.ToDecimal(dynamicObject["UnitTransHeadQty"]) / Convert.ToDecimal(dynamicObject["UnitTransOperQty"]);
                            dynamicObject2["BaseWorkQty"] = this.curUnitConvert.ConvertQty(num2, "");
                            dynamicObject2["WorkQty"] = this.Model.DataObject["ClaimedQty"];
                            dynamicObject2["WorkHeadQty"] = num2;
                            dynamicObject2["PlanBeginTime"] = this.Model.DataObject["PlanBeginedTime"];
                            dynamicObject2["PlanEndTime"] = this.Model.DataObject["PlanEndedTime"];
                        }
                        OperateOption operateOption = OperateOption.Create();
                        operateOption.SetVariableValue("IsMobileInvoke", true);
                        IOperationResult operationResult = SFCDispatchManager.Instance.Save(base.Context, new DynamicObject[]
                        {
                    dynamicObject
                        }, operateOption);
                        if (operationResult.IsSuccess)
                        {
                            base.View.ShowStatusBarInfo(ResManager.LoadKDString("成功！", "015747000028224", SubSystemType.MFG, new object[0]));
                            this.Model.DataObject["ClaimedQty"] = 0;
                            base.View.SetControlProperty("FClaimedQty", 0);
                            base.View.UpdateView("FClaimedQty");
                            this.Model.DataObject["PlanBeginedTime"] = null;
                            this.Model.DataObject["PlanEndedTime"] = null;
                            base.View.UpdateView("FPlanBeginedTime");
                            base.View.UpdateView("FPlanEndedTime");
                            base.View.GetControl("FBtn_ClaimModify").Enabled = false;
                            base.View.UpdateView("FBtn_ClaimModify");
                            this.IsClaim = true;
                            this.CurrDataFilterType = MobileEnums.DataFilterType.UnFinishedDetail;
                        }
                        else
                        {
                            base.View.ShowErrMessage(MobBusinessUtils.GetErrMsgFromOperationResult("", operationResult), "", MessageBoxType.Notice);
                        }
                    }
                }
            }
        }
        **/
    }
}

