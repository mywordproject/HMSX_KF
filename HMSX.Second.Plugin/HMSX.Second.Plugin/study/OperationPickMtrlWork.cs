using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Log;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS.Log;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Resource;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using Kingdee.BOS.VerificationHelper;
using Kingdee.K3.Core.MFG.Common;
using Kingdee.K3.Core.MFG.EntityHelper;
using Kingdee.K3.Core.MFG.EnumConst;
using Kingdee.K3.Core.MFG.Utils;
using Kingdee.K3.MFG.App;
using Kingdee.K3.MFG.ServiceHelper.SFC;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace HMSX.Second.Plugin.study
{
	[Description("工序领料平台")]
	[Kingdee.BOS.Util.HotUpdate]
	public class OperationPickMtrlWork : AbstractDynamicFormPlugIn
	{
		private string strMsg0 = ResManager.LoadKDString("没有符合工序领料条件的工序！", "015649000008807", SubSystemType.MFG, new object[0]);
		private string strMsg1 = ResManager.LoadKDString("请选择一条工序明细记录！", "015649000008808", SubSystemType.MFG, new object[0]);
		private string strMsg2 = ResManager.LoadKDString("请选择一条领料明细记录！", "015649000008809", SubSystemType.MFG, new object[0]);
		private string strMsg3 = ResManager.LoadKDString("选中的领料明细实发数量为0，不能生成生产领料单，请重新选择！", "015649000008810", SubSystemType.MFG, new object[0]);
		private string strMsg4 = ResManager.LoadKDString("未生成生产领料单，请重新选择符合生成条件的领料明细！", "015649000008811", SubSystemType.MFG, new object[0]);
		private string strMsg5 = ResManager.LoadKDString("物料[{0}]不允许超发，实发数量不能大于申请数量", "015649000008800", SubSystemType.MFG, new object[0]);
		private string strMsg6 = ResManager.LoadKDString("领料套数不能大于工序数量", "015649000008801", SubSystemType.MFG, new object[0]);
		private string strMsg7 = ResManager.LoadKDString("领料套数不能大于订单数量", "015649000011514", SubSystemType.MFG, new object[0]);
		private string strTaskTitle = ResManager.LoadKDString("正在生成单据", "015649000008826", SubSystemType.MFG, new object[0]);
		private string strMsg8 = ResManager.LoadKDString("生成领料单选项业务参数页签【允许多次超发】参数没有勾选，选中分录序号为（{0}）的分录不能生成领料单", "015649000022874", SubSystemType.MFG, new object[0]);
		private string strMsg9 = ResManager.LoadKDString("仅生产订单勾选生产发料才可以点击", "015649000026012", SubSystemType.MFG, new object[0]);
		public int inFlag;
		public DynamicObject optConsistencyLogObject;
		private bool needMapping;
		private bool isViewChanging;
		private IOperationResult result;
		public override void OnInitialize(InitializeEventArgs e)
		{
			base.OnInitialize(e);
			this.InitCustomParameters();
		}
		public override void BeforeBindData(System.EventArgs e)
		{
			base.BeforeBindData(e);
			object customParameter = this.View.OpenParameter.GetCustomParameter("optIds");
			if (customParameter == null)
			{
				return;
			}
			System.Collections.Generic.List<long> list = customParameter as System.Collections.Generic.List<long>;
			System.Collections.Generic.List<long> optIdList = list ?? new System.Collections.Generic.List<long>();
			this.LoadByBizBill(optIdList);
		}
		public override void AfterBindData(System.EventArgs e)
		{
			base.AfterBindData(e);
			this.ChangeView();
			this.LoadData();
		}
		public override void BarItemClick(BarItemClickEventArgs e)
		{
			base.BarItemClick(e);
			string barItemKey;
			switch (barItemKey = e.BarItemKey)
			{
				case "tbGenPickMtrl"://生成领料单
					var entrys1 = this.View.Model.DataObject["PickMatEntity"] as DynamicObjectCollection;
					string str1 = "";
					foreach (var entry in entrys1)
					{
						this.Model.SetValue("FBaseActualQty", Convert.ToDecimal(entry["ActualQty"]), Convert.ToInt32(entry["Seq"]) - 1);
						string jskcsql = $@"/*dialect*/select sum(A.FBASEQTY)FBASEQTY FROM T_STK_INVENTORY A
                                WHERE FSTOCKORGID = 100026 and FBASEQTY > 0  and FSTOCKID='{entry["StockId_Id"]}'
                                and FSTOCKSTATUSID in(27910195 , 10000) and FMATERIALID='{entry["ChiMaterialId_Id"]}'";
						var jskc = DBUtils.ExecuteDynamicObject(Context, jskcsql);
						if (jskc.Count > 0)
						{
							if (Convert.ToDecimal(entry["ActualQty"]) > Convert.ToDecimal(jskc[0]["FBASEQTY"]))
							{
								str1 += "该物料" + ((DynamicObject)entry["ChiMaterialId"])["Number"].ToString() + "库存不足,库存剩余数量" + Convert.ToDecimal(jskc[0]["FBASEQTY"]) + "!!!\n";

							}
						}
						else
						{
							str1 += "该物料" + ((DynamicObject)entry["ChiMaterialId"])["Number"].ToString() + "库存不足,库存剩余数量0!!!\n";
						}
					}
					this.View.ShowMessage(str1);
					LicenseVerifier.CheckViewOnlyOperation(base.Context, ResManager.LoadKDString("生成领料单", "015649000014200", SubSystemType.MFG, new object[0]));
					this.GenPickMtrlData();
					return;
				case "tbPickMtrlView":
					this.ShowPickMtrlBillData();
					return;
				case "tbReinforcePickingMtrl":
					LicenseVerifier.CheckViewOnlyOperation(base.Context, ResManager.LoadKDString("补齐领料", "015649000014201", SubSystemType.MFG, new object[0]));
					this.ReinforceOptPickMtrl();
					return;
				case "tbSuitPickingMtrl"://配套
					LicenseVerifier.CheckViewOnlyOperation(base.Context, ResManager.LoadKDString("配套领料", "015649000014202", SubSystemType.MFG, new object[0]));
					this.SuitOptPickMtrl();
					EntryEntity entryEntity = this.View.BusinessInfo.GetEntryEntity("FPickMatEntity");
					var entrys = this.View.Model.DataObject["PickMatEntity"] as DynamicObjectCollection;
					string str = "";
					foreach (var entry in entrys)
					{
                        this.Model.SetValue("FBaseActualQty", Convert.ToDecimal(entry["ActualQty"]), Convert.ToInt32(entry["Seq"]) - 1);
						string jskcsql = $@"/*dialect*/select sum(A.FBASEQTY)FBASEQTY FROM T_STK_INVENTORY A
                                WHERE FSTOCKORGID = 100026 and FBASEQTY > 0  and FSTOCKID='{entry["StockId_Id"]}'
                                and FSTOCKSTATUSID in(27910195 , 10000) and FMATERIALID='{entry["ChiMaterialId_Id"]}'";
						var jskc = DBUtils.ExecuteDynamicObject(Context,jskcsql);
						if (jskc.Count > 0)
						{
                            if (Convert.ToDecimal(entry["ActualQty"])> Convert.ToDecimal(jskc[0]["FBASEQTY"]))
                            {
								str += "该物料" + ((DynamicObject)entry["ChiMaterialId"])["Number"].ToString() + "库存不足,库存剩余数量"+ Convert.ToDecimal(jskc[0]["FBASEQTY"]) + "!!!\n";
							}
						}
						else
						{
							str += "该物料" + ((DynamicObject)entry["ChiMaterialId"])["Number"].ToString() + "库存不足,库存剩余数量0!!!\n";
                        }
                    }
					this.View.UpdateView("FPickMatEntity");
					this.View.ShowMessage(str);
					return;
				case "tbMovingMtrl":
					LicenseVerifier.CheckViewOnlyOperation(base.Context, ResManager.LoadKDString("工序挪料", "015649000014199", SubSystemType.MFG, new object[0]));
					this.MovingMtrl();
					return;
				case "tbOptConsistencyLogView":
					this.ShowOptConsistencyLog(2);
					return;
				case "tbClose":
					this.View.Close();
					return;
				case "tbGenMtrlNotice":
					this.GenMtrlNotice();
					return;
				case "tbMtrlNoticeView":
					this.ShowMtrlNoticeBillData();
					break;

					return;
			}
		}
		private void ShowMtrlNoticeBillData()
		{
			System.Collections.Generic.IEnumerable<DynamicObject> enumerable = this.GetSelectedEntrys("FPickMatEntity", "IsSelectPickMat");
			if (enumerable.IsNullOrEmpty() || !enumerable.Any<DynamicObject>())
			{
				this.View.ShowErrMessage(this.strMsg1, Consts.ERROR_TITLE, MessageBoxType.Notice);
				return;
			}
			enumerable =
				from item in enumerable
				where System.Convert.ToBoolean(item["IssueMtrl"])
				select item;
			if (enumerable.IsNullOrEmpty() || !enumerable.Any<DynamicObject>())
			{
				this.View.ShowErrMessage(this.strMsg9, "", MessageBoxType.Notice);
				return;
			}
			System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
			System.Collections.Generic.List<SqlParam> list = new System.Collections.Generic.List<SqlParam>();
			System.Collections.Generic.List<long> list2 = new System.Collections.Generic.List<long>();
			foreach (DynamicObject current in enumerable)
			{
				long dynamicObjectItemValue = current.GetDynamicObjectItemValue("PmOperationID", 0L);
				list2.Add(dynamicObjectItemValue);
			}
			if (!list2.IsEmpty<long>())
			{
				stringBuilder.Append(string.Format(" AND (EXISTS (SELECT 1 FROM TABLE(fn_StrSplit(@operIdList,',',1)) Q WHERE Q.FID = FPlanEntryID))", new object[0]));
				list.Add(new SqlParam("@operIdList", KDDbType.udt_inttable, list2.Distinct<long>().ToArray<long>()));
			}
			ListShowParameter listShowParameter = new ListShowParameter();
			listShowParameter.FormId = "PRD_ISSUEMTRNOTICE";
			listShowParameter.ParentPageId = this.View.PageId;
			listShowParameter.IsLookUp = false;
			listShowParameter.IsIsolationOrg = false;
			listShowParameter.IsShowUsed = false;
			listShowParameter.OpenStyle.ShowType = ShowType.Modal;
			listShowParameter.SqlParams = list;
			listShowParameter.ListFilterParameter.Filter = string.Format("1 = 1 ", new object[0]);
			if (!stringBuilder.IsNullOrEmptyOrWhiteSpace())
			{
				listShowParameter.ListFilterParameter.Filter = listShowParameter.ListFilterParameter.Filter + stringBuilder.ToString();
			}
			this.View.ShowForm(listShowParameter);
		}
		private void GenMtrlNotice()
		{
			System.Collections.Generic.IEnumerable<DynamicObject> enumerable = this.GetSelectedEntrys("FPickMatEntity", "IsSelectPickMat");
			if (enumerable.IsNullOrEmpty() || !enumerable.Any<DynamicObject>())
			{
				this.View.ShowErrMessage(this.strMsg2, Consts.ERROR_TITLE, MessageBoxType.Notice);
				return;
			}
			enumerable =
				from item in enumerable
				where System.Convert.ToBoolean(item["IssueMtrl"])
				select item;
			if (enumerable.IsNullOrEmpty() || !enumerable.Any<DynamicObject>())
			{
				this.View.ShowErrMessage(this.strMsg9, "", MessageBoxType.Notice);
				return;
			}
			System.Collections.Generic.List<DynamicObject> list = new System.Collections.Generic.List<DynamicObject>();
			list.AddRange(enumerable);
			string text = string.Format(ResManager.LoadKDString("工序领料平台-生成发料通知开始, 共选中{0}行：{1}", "015649000026008", SubSystemType.MFG, new object[0]), list.Count, this.GetSelectBillInfo(enumerable, "FPickMatEntity"));
			Logger.Info("ShowMtrlNoticeBillData", text);
			LogObject logObject = new LogObject
			{
				Description = text,
				Environment = OperatingEnvironment.BizOperate,
				OperateName = ResManager.LoadKDString("工序领料平台-生成发料通知", "015649000026009", SubSystemType.MFG, new object[0]),
				ObjectTypeId = "SFC_OperationPickMtrlWork",
				SubSystemId = this.View.Model.BillBusinessInfo.GetForm().SubsysId
			};
			LogServiceHelper.WriteLog(base.Context, logObject);
			TaskProxyItem taskProxyItem = new TaskProxyItem();
			taskProxyItem.Parameters = new System.Collections.Generic.List<object>
			{
				base.Context,
				list,
				taskProxyItem.TaskId
			}.ToArray();
			taskProxyItem.ClassName = "Kingdee.K3.MFG.SFC.App.Core.OperationPickMtrlService,Kingdee.K3.MFG.SFC.App.Core";
			taskProxyItem.MethodName = "GetMtrlNoticeData";
			taskProxyItem.Title = this.strTaskTitle;
			this.View.ShowLoadingForm(taskProxyItem, null, true, delegate (IOperationResult result)
			{
				this.ShowMtrlNoticeGenerateResult(result);
			});
		}
		private void ShowMtrlNoticeGenerateResult(IOperationResult result)
		{
			string text = "PRD_ISSUEMTRNOTICE";
			BillShowParameter billShowParameter = new BillShowParameter();
			billShowParameter.ParentPageId = this.View.PageId;
			if ((result == null || !result.IsSuccess) && result.ValidationErrors != null && result.ValidationErrors.Count > 0)
			{
				System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
				foreach (ValidationErrorInfo current in result.ValidationErrors)
				{
					stringBuilder.AppendLine(current.Message);
				}
				Logger.Info("GenMtrlNotice-Error：", stringBuilder.ToString());
				LogObject logObject = new LogObject
				{
					Description = stringBuilder.ToString(),
					Environment = OperatingEnvironment.BizOperate,
					OperateName = ResManager.LoadKDString("生成发料通知单出错", "015649000026010", SubSystemType.MFG, new object[0]),
					ObjectTypeId = "SFC_OperationPickMtrlWork",
					SubSystemId = this.View.Model.BillBusinessInfo.GetForm().SubsysId
				};
				LogServiceHelper.WriteLog(base.Context, logObject);
				return;
			}
			System.Collections.Generic.List<DynamicObject> list = result.SuccessDataEnity as System.Collections.Generic.List<DynamicObject>;
			if (list == null || list.Count < 1)
			{
				this.View.ShowErrMessage(this.strMsg4, Consts.ERROR_TITLE, MessageBoxType.Notice);
				return;
			}
			if (list.Count == 1)
			{
				billShowParameter.Status = OperationStatus.ADDNEW;
				string key = "_ConvertSessionKey";
				string text2 = "ConverOneResult";
				billShowParameter.CustomParams.Add(key, text2);
				this.View.Session[text2] = list[0];
				billShowParameter.FormId = text;
			}
			else
			{
				billShowParameter.FormId = "BOS_ConvertResultForm";
				string key2 = "ConvertResults";
				this.View.Session[key2] = list.ToArray();
				billShowParameter.CustomParams.Add("_ConvertResultFormId", text);
			}
			if ("bosidetest".Equals(this.View.Context.UserToken.ToLowerInvariant()))
			{
				billShowParameter.OpenStyle.ShowType = ShowType.Default;
			}
			else
			{
				billShowParameter.OpenStyle.ShowType = ShowType.MainNewTabPage;
			}
			billShowParameter.CreateFrom = CreateFrom.Push;
			this.View.ShowForm(billShowParameter, delegate (FormResult res)
			{
				if (res != null && res.ReturnData != null && this.View.Model != null)
				{
					Logger.Info("GenMtrlNotice：", ResManager.LoadKDString("工序领料平台-生成发料通知单成功！", "015649000026011", SubSystemType.MFG, new object[0]));
					this.RefreshOptPickMtrl();
				}
			});
		}
		private void GenPickMtrlData()
		{
			System.Collections.Generic.IEnumerable<DynamicObject> enumerable = this.GetSelectedEntrys("FPickMatEntity", "IsSelectPickMat");
			if (enumerable.IsNullOrEmpty() || !enumerable.Any<DynamicObject>())
			{
				this.View.ShowErrMessage(this.strMsg2, Consts.ERROR_TITLE, MessageBoxType.Notice);
				return;
			}
			enumerable =
				from item in enumerable
				where !System.Convert.ToBoolean(item["IssueMtrl"])
				select item;
			if (enumerable.IsNullOrEmpty() || !enumerable.Any<DynamicObject>())
			{
				this.View.ShowErrMessage(ResManager.LoadKDString("勾选了生产发料，无法直接领料，只能通过生产发料通知单领料。", "015649000025990", SubSystemType.MFG, new object[0]), "", MessageBoxType.Notice);
				return;
			}
			System.Collections.Generic.Dictionary<string, Tuple<decimal, decimal>> dictionary = new System.Collections.Generic.Dictionary<string, Tuple<decimal, decimal>>();
			foreach (DynamicObject current in enumerable)
			{
				long num = System.Convert.ToInt64(current["PmOperationID"]);
				long num2 = System.Convert.ToInt64(current["PPBomEntryID"]);
				decimal item4 = System.Convert.ToDecimal(current["ActualQty"]);
				decimal item2 = System.Convert.ToDecimal(current["BaseActualQty"]);
				string key = string.Format("{0}_{1}", num, num2);
				if (!dictionary.ContainsKey(key))
				{
					dictionary.Add(key, new Tuple<decimal, decimal>(item4, item2));
				}
			}
			object customParameter = this.View.OpenParameter.GetCustomParameter("optIds");
			if (customParameter == null)
			{
				return;
			}
			System.Collections.Generic.List<long> list = customParameter as System.Collections.Generic.List<long>;
			System.Collections.Generic.List<long> optIdList = list ?? new System.Collections.Generic.List<long>();
			this.inFlag = 0;
			this.LoadByBizBill(optIdList);
			enumerable = this.GetSelectedEntrys("FPickMatEntity", "IsSelectPickMat");
			DynamicObject dynamicObject = AppServiceContext.ParamService.TryGetUserParam(base.Context, "PRD_PickMtrl", "PRD_PICK_BILLPARAM");
			bool flag = false;
			if (!dynamicObject.IsNullOrEmpty())
			{
				flag = dynamicObject.GetDynamicObjectItemValue("IsAllowMultipul", false);
			}
			System.Collections.Generic.List<int> list2 = new System.Collections.Generic.List<int>();
			foreach (DynamicObject current2 in enumerable)
			{
				long num3 = System.Convert.ToInt64(current2["PmOperationID"]);
				long num4 = System.Convert.ToInt64(current2["PPBomEntryID"]);
				string key2 = string.Format("{0}_{1}", num3, num4);
				if (dictionary.ContainsKey(key2))
				{
					Tuple<decimal, decimal> tuple = dictionary[key2];
					if (System.Convert.ToDecimal(current2["ActualQty"]) > 0m)
					{
						current2["ActualQty"] = tuple.Item1;
						current2["BaseActualQty"] = tuple.Item2;
						current2["IsSelectPickMat"] = true;
					}
					else
					{
						string text = System.Convert.ToString(current2["OverControlMode"]);
						if (text.IsNullOrEmptyOrWhiteSpace())
						{
							text = "3";
						}
						if (text == "3")
						{
							current2["IsSelectPickMat"] = false;
						}
						else
						{
							if (flag)
							{
								current2["ActualQty"] = tuple.Item1;
								current2["BaseActualQty"] = tuple.Item2;
								current2["IsSelectPickMat"] = true;
							}
							else
							{
								current2["IsSelectPickMat"] = false;
								int item3 = System.Convert.ToInt32(current2["Seq"]);
								list2.Add(item3);
							}
						}
					}
				}
				else
				{
					current2["IsSelectPickMat"] = false;
				}
			}
			enumerable = this.GetSelectedEntrys("FPickMatEntity", "IsSelectPickMat");
			System.Collections.Generic.List<DynamicObject> list3 = new System.Collections.Generic.List<DynamicObject>();
			this.View.BusinessInfo.GetEntryEntity("FPickMatEntity");
			enumerable =
				from e in enumerable
				where System.Convert.ToDecimal(e["ActualQty"]) > 0m
				select e;
			if (!enumerable.IsNullOrEmpty() && enumerable.Any<DynamicObject>())
			{
				if (list2.Count > 0)
				{
					string sInfo = string.Format(this.strMsg8, string.Join<int>(",", list2.ToArray()));
					this.View.ShowStatusBarInfo(sInfo);
				}
				list3.AddRange(enumerable);
				string text2 = string.Format(ResManager.LoadKDString("工序领料平台-生成领料单开始, 共选中{0}行：{1}", "015649030034862", SubSystemType.MFG, new object[0]), list3.Count, this.GetSelectBillInfo(enumerable, "FPickMatEntity"));
				Logger.Info("GenPickMtrlData", text2);
				LogObject logObject = new LogObject
				{
					Description = text2,
					Environment = OperatingEnvironment.BizOperate,
					OperateName = ResManager.LoadKDString("工序领料平台-生成领料单", "015649030034863", SubSystemType.MFG, new object[0]),
					ObjectTypeId = "SFC_OperationPickMtrlWork",
					SubSystemId = this.View.Model.BillBusinessInfo.GetForm().SubsysId
				};
				LogServiceHelper.WriteLog(base.Context, logObject);
				TaskProxyItem taskProxyItem = new TaskProxyItem();
				taskProxyItem.Parameters = new System.Collections.Generic.List<object>
				{
					base.Context,
					list3,
					taskProxyItem.TaskId
				}.ToArray();
				taskProxyItem.ClassName = "Kingdee.K3.MFG.SFC.App.Core.OperationPickMtrlService,Kingdee.K3.MFG.SFC.App.Core";
				taskProxyItem.MethodName = "GetPickMtrlData";
				taskProxyItem.Title = this.strTaskTitle;
				this.View.ShowLoadingForm(taskProxyItem, null, true, delegate (IOperationResult result)
				{
					this.ShowPickMtrlGenerateResult(result);
				});
				return;
			}
			if (list2.Count > 0)
			{
				string msg = string.Format(this.strMsg8, string.Join<int>(",", list2.ToArray()));
				this.View.ShowErrMessage(msg, "", MessageBoxType.Notice);
				return;
			}
			this.View.ShowErrMessage(this.strMsg3, Consts.ERROR_TITLE, MessageBoxType.Notice);
		}
		private void ShowPickMtrlGenerateResult(IOperationResult result)
		{
			string text = "PRD_PickMtrl";
			BillShowParameter billShowParameter = new BillShowParameter();
			billShowParameter.ParentPageId = this.View.PageId;
			if ((result == null || !result.IsSuccess) && result.ValidationErrors != null && result.ValidationErrors.Count > 0)
			{
				System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
				foreach (ValidationErrorInfo current in result.ValidationErrors)
				{
					stringBuilder.AppendLine(current.Message);
				}
				Logger.Info("GenPickMtrlData-Error：", stringBuilder.ToString());
				LogObject logObject = new LogObject
				{
					Description = stringBuilder.ToString(),
					Environment = OperatingEnvironment.BizOperate,
					OperateName = ResManager.LoadKDString("生成领料单出错", "015649030034864", SubSystemType.MFG, new object[0]),
					ObjectTypeId = "SFC_OperationPickMtrlWork",
					SubSystemId = this.View.Model.BillBusinessInfo.GetForm().SubsysId
				};
				LogServiceHelper.WriteLog(base.Context, logObject);
				return;
			}
			System.Collections.Generic.List<DynamicObject> list = result.SuccessDataEnity as System.Collections.Generic.List<DynamicObject>;
			if (list == null || list.Count < 1)
			{
				this.View.ShowErrMessage(this.strMsg4, Consts.ERROR_TITLE, MessageBoxType.Notice);
				return;
			}
			if (list.Count == 1)
			{
				billShowParameter.Status = OperationStatus.ADDNEW;
				string key = "_ConvertSessionKey";
				string text2 = "ConverOneResult";
				billShowParameter.CustomParams.Add(key, text2);
				this.View.Session[text2] = list[0];
				billShowParameter.FormId = text;
			}
			else
			{
				billShowParameter.FormId = "BOS_ConvertResultForm";
				string key2 = "ConvertResults";
				this.View.Session[key2] = list.ToArray();
				billShowParameter.CustomParams.Add("_ConvertResultFormId", text);
			}
			if (this.View.Context.UserToken.ToLowerInvariant().Equals("bosidetest"))
			{
				billShowParameter.OpenStyle.ShowType = ShowType.Default;
			}
			else
			{
				billShowParameter.OpenStyle.ShowType = ShowType.MainNewTabPage;
			}
			this.View.ShowForm(billShowParameter, delegate (FormResult res)
			{
				if (res != null && res.ReturnData != null && this.View.Model != null)
				{
					Logger.Info("GenPickMtrlData：", ResManager.LoadKDString("工序领料平台-生成领料单成功！", "015649030034865", SubSystemType.MFG, new object[0]));
					this.RefreshOptPickMtrl();
				}
			});
		}
		private void ReinforceOptPickMtrl()
		{
			System.Collections.Generic.IEnumerable<DynamicObject> selectedEntrys = this.GetSelectedEntrys("FOptPlanEntity", "IsSelectOper");
			if (selectedEntrys.IsNullOrEmpty() || !selectedEntrys.Any<DynamicObject>())
			{
				this.View.ShowErrMessage(this.strMsg1, Consts.ERROR_TITLE, MessageBoxType.Notice);
				return;
			}
			string text = string.Format(ResManager.LoadKDString("工序领料平台-补齐领料开始, 共选中{0}行：{1}", "015649030034866", SubSystemType.MFG, new object[0]), selectedEntrys.Count<DynamicObject>(), this.GetSelectBillInfo(selectedEntrys, "FOptPlanEntity"));
			Logger.Info("ReinforceOptPickMtrl", text);
			this.inFlag = 1;
			this.LoadByBizBill(null);
			Logger.Info("ReinforceOptPickMtrl：", ResManager.LoadKDString("工序领料平台-补齐领料结束！", "015649030034867", SubSystemType.MFG, new object[0]));
			LogObject logObject = new LogObject
			{
				Description = text,
				Environment = OperatingEnvironment.BizOperate,
				OperateName = ResManager.LoadKDString("工序领料平台-补齐领料", "015649030034868", SubSystemType.MFG, new object[0]),
				ObjectTypeId = "SFC_OperationPickMtrlWork",
				SubSystemId = this.View.Model.BillBusinessInfo.GetForm().SubsysId
			};
			LogServiceHelper.WriteLog(base.Context, logObject);
			this.View.UpdateView("FOptPlanEntity");
			this.View.UpdateView("FPickMatEntity");
		}
		private void SuitOptPickMtrl()
		{
			System.Collections.Generic.IEnumerable<DynamicObject> selectedEntrys = this.GetSelectedEntrys("FOptPlanEntity", "IsSelectOper");
			if (selectedEntrys.IsNullOrEmpty() || !selectedEntrys.Any<DynamicObject>())
			{
				this.View.ShowErrMessage(this.strMsg1, Consts.ERROR_TITLE, MessageBoxType.Notice);
				return;
			}
			string text = string.Format(ResManager.LoadKDString("工序领料平台-配套领料开始, 共选中{0}行：{1}", "015649030034869", SubSystemType.MFG, new object[0]), selectedEntrys.Count<DynamicObject>(), this.GetSelectBillInfo(selectedEntrys, "FOptPlanEntity"));
			Logger.Info("SuitOptPickMtrl", text);
			this.inFlag = 2;
			this.LoadByBizBill(null);
			Logger.Info("SuitOptPickMtrl：", ResManager.LoadKDString("工序领料平台-配套领料结束！", "015649030034870", SubSystemType.MFG, new object[0]));
			LogObject logObject = new LogObject
			{
				Description = text,
				Environment = OperatingEnvironment.BizOperate,
				OperateName = ResManager.LoadKDString("工序领料平台-配套领料", "015649030034871", SubSystemType.MFG, new object[0]),
				ObjectTypeId = "SFC_OperationPickMtrlWork",
				SubSystemId = this.View.Model.BillBusinessInfo.GetForm().SubsysId
			};
			LogServiceHelper.WriteLog(base.Context, logObject);
			this.View.UpdateView("FOptPlanEntity");
			this.View.UpdateView("FPickMatEntity");
		}
		private void MovingMtrl()
		{
			System.Collections.Generic.IEnumerable<DynamicObject> selectedEntrys = this.GetSelectedEntrys("FPickMatEntity", "IsSelectPickMat");
			if (selectedEntrys.IsNullOrEmpty() || !selectedEntrys.Any<DynamicObject>())
			{
				this.View.ShowErrMessage(this.strMsg1, Consts.ERROR_TITLE, MessageBoxType.Notice);
				return;
			}
			if (!this.ValidatePPBomAndOper(selectedEntrys))
			{
				return;
			}
			if (!this.ValidatePickedQty(selectedEntrys))
			{
				return;
			}
			DynamicFormShowParameter dynamicFormShowParameter = new DynamicFormShowParameter();
			dynamicFormShowParameter.FormId = "SFC_OperationMovingMtrl";
			dynamicFormShowParameter.OpenStyle.ShowType = ShowType.Floating;
			dynamicFormShowParameter.ParentPageId = this.View.PageId;
			dynamicFormShowParameter.CustomComplexParams.Add("selectItems", selectedEntrys);
			this.View.ShowForm(dynamicFormShowParameter, delegate (FormResult result)
			{
				if (result.ReturnData != null && System.Convert.ToBoolean(result.ReturnData))
				{
					this.View.ShowMessage(ResManager.LoadKDString("挪料成功！", "015649000021363", SubSystemType.MFG, new object[0]), MessageBoxType.Notice);
					object customParameter = this.View.OpenParameter.GetCustomParameter("optIds");
					if (customParameter == null)
					{
						return;
					}
					System.Collections.Generic.List<long> list = customParameter as System.Collections.Generic.List<long>;
					System.Collections.Generic.List<long> optIdList = list ?? new System.Collections.Generic.List<long>();
					this.LoadByBizBill(optIdList);
					this.View.UpdateView("FOptPlanEntity");
					this.View.UpdateView("FPickMatEntity");
				}
			});
		}
		private bool ValidatePPBomAndOper(System.Collections.Generic.IEnumerable<DynamicObject> selectItems)
		{
			bool flag = true;
			if (selectItems.Count<DynamicObject>() > 1)
			{
				string msg = ResManager.LoadKDString("所选子项物料不在同一用料清单中，不能进行挪料操作！", "015649000021364", SubSystemType.MFG, new object[0]);
				string msg2 = ResManager.LoadKDString("所选子项物料发料工序不同，不能进行挪料操作！", "015649000021365", SubSystemType.MFG, new object[0]);
				DynamicObject dynamicObject = selectItems.FirstOrDefault<DynamicObject>();
				long dynamicObjectItemValue =Convert.ToInt64( dynamicObject["PPBomID"]);
				string dynamicObjectItemValue2 =dynamicObject["PmSeqNumber"].ToString();
				int dynamicObjectItemValue3 =Convert.ToInt32( dynamicObject["PmOperNumber"]);
				foreach (DynamicObject current in selectItems)
				{
					long dynamicObjectItemValue4 =Convert.ToInt64( current["PPBomID"]);
					string dynamicObjectItemValue5 = current["PmSeqNumber"].ToString();
					int dynamicObjectItemValue6 =Convert.ToInt32( current["PmOperNumber"]);
					if (dynamicObjectItemValue != dynamicObjectItemValue4)
					{
						flag = false;
						this.View.ShowErrMessage(msg, "", MessageBoxType.Notice);
						break;
					}
					if (!dynamicObjectItemValue5.Equals(dynamicObjectItemValue2) || dynamicObjectItemValue6 != dynamicObjectItemValue3)
					{
						flag = false;
						this.View.ShowErrMessage(msg2, "", MessageBoxType.Notice);
						break;
					}
				}
			}
			return flag;
		}
		private bool ValidatePickedQty(System.Collections.Generic.IEnumerable<DynamicObject> selectItems)
		{
			bool flag = false;
			string text = ResManager.LoadKDString("右侧第{0}行分录，子项物料不满足挪料条件：已领数量 > 退料数量。", "015649000021366", SubSystemType.MFG, new object[0]);
			System.Collections.Generic.List<int> list = new System.Collections.Generic.List<int>();
			int num = 1;
			foreach (DynamicObject current in selectItems)
			{
				decimal dynamicObjectItemValue = current.GetDynamicObjectItemValue("PickedQty", 0m);
				decimal dynamicObjectItemValue2 = current.GetDynamicObjectItemValue("ReturnedQty", 0m);
				if (!(dynamicObjectItemValue > dynamicObjectItemValue2))
				{
					list.Add(num);
					flag = false;
					break;
				}
				flag = true;
				num++;
			}
			if (!flag)
			{
				text = string.Format(text, string.Join<int>(",", list));
				this.View.ShowErrMessage(text, "", MessageBoxType.Notice);
			}
			return flag;
		}
		private void RefreshOptPickMtrl()
		{
			this.inFlag = 3;
			this.LoadByBizBill(null);
			this.View.UpdateView("FOptPlanEntity");
			this.View.UpdateView("FPickMatEntity");
		}
		private void LoadByBizBill(System.Collections.Generic.List<long> optIdList)
		{
			switch (this.inFlag)
			{
				case 0:
					this.result = OperationPickMtrlServiceHelper.CreateOptPickMtrlData(base.Context, optIdList);
					break;
				case 1:
					this.result = OperationPickMtrlServiceHelper.GetReinforceOptPickMtrlData(base.Context, this.View.Model.DataObject);
					break;
				case 2:
					this.result = OperationPickMtrlServiceHelper.GetSuitOptPickMtrlData(base.Context, this.View.Model.DataObject);
					break;
				case 3:
					this.result = OperationPickMtrlServiceHelper.RefreshPickMtrlData(base.Context, this.View.Model.DataObject);
					break;
			}
			System.Collections.Generic.IEnumerable<DynamicObject> successDataEnity = this.result.SuccessDataEnity;
			if (!successDataEnity.IsNullOrEmpty() && successDataEnity.IsCountGreaterThan(0))
			{
				this.View.Model.DataObject = successDataEnity.FirstOrDefault<DynamicObject>();
			}
		}
		private System.Collections.Generic.IEnumerable<DynamicObject> GetSelectedEntrys(string entityKey, string selectKey)
		{
			EntryEntity entryEntity = this.View.BusinessInfo.GetEntryEntity(entityKey);
			return
				from e in this.View.Model.GetEntityDataObject(entryEntity)
				where System.Convert.ToBoolean(e[selectKey])
				select e;
		}
		private void ShowPickMtrlBillData()
		{
			System.Collections.Generic.IEnumerable<DynamicObject> selectedEntrys = this.GetSelectedEntrys("FOptPlanEntity", "IsSelectOper");
			if (selectedEntrys.IsNullOrEmpty() || !selectedEntrys.Any<DynamicObject>())
			{
				this.View.ShowErrMessage(this.strMsg1, Consts.ERROR_TITLE, MessageBoxType.Notice);
				return;
			}
			System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
			System.Collections.Generic.List<SqlParam> list = new System.Collections.Generic.List<SqlParam>();
			if ("A".Equals(this.Model.DataObject["PickType"].ToString()))
			{
				System.Collections.Generic.List<long> list2 = new System.Collections.Generic.List<long>();
				foreach (DynamicObject current in selectedEntrys)
				{
					long dynamicObjectItemValue = current.GetDynamicObjectItemValue("OperId", 0L);
					list2.Add(dynamicObjectItemValue);
				}
				stringBuilder.Append(string.Format(" AND (EXISTS (SELECT 1 FROM TABLE(fn_StrSplit(@OptIdList,',',1)) Q WHERE Q.FID = FOptDetailId))", new object[0]));
				list.Add(new SqlParam("@OptIdList", KDDbType.udt_inttable, list2.Distinct<long>().ToArray<long>()));
			}
			else
			{
				if ("B".Equals(this.Model.DataObject["PickType"].ToString()))
				{
					System.Collections.Generic.List<string> list3 = new System.Collections.Generic.List<string>();
					foreach (DynamicObject current2 in selectedEntrys)
					{
						list3.Add(string.Format(" FMoEntryId={0} AND FProcessId={1} ", current2["MOEntryId"], current2["ProcessId_Id"]));
					}
					stringBuilder.Append(string.Format(" AND ({0})", string.Join<string>("OR", list3)));
				}
			}
			ListShowParameter listShowParameter = new ListShowParameter();
			listShowParameter.FormId = "PRD_PickMtrl";
			listShowParameter.ParentPageId = this.View.PageId;
			listShowParameter.IsLookUp = false;
			listShowParameter.IsIsolationOrg = false;
			listShowParameter.IsShowUsed = false;
			listShowParameter.OpenStyle.ShowType = ShowType.Modal;
			listShowParameter.SqlParams = list;
			listShowParameter.ListFilterParameter.Filter = string.Format("FSRCBIZBILLTYPE = '{0}'", "SFC_OperationPlanning");
			if (!stringBuilder.IsNullOrEmptyOrWhiteSpace())
			{
				listShowParameter.ListFilterParameter.Filter = listShowParameter.ListFilterParameter.Filter + stringBuilder.ToString();
			}
			this.View.ShowForm(listShowParameter);
		}
		private void ShowOptConsistencyLog(int flag)
		{
			if (flag != 2)
			{
				if (flag == 1)
				{
					this.View.ShowMessage(ResManager.LoadKDString("工序一致性检查结果存在异常信息，详情点击工序一致性结果查看按钮进行查看。", "015649000016409", SubSystemType.MFG, new object[0]), MessageBoxType.Notice);
				}
				return;
			}
			DynamicFormShowParameter dynamicFormShowParameter = new DynamicFormShowParameter();
			dynamicFormShowParameter.FormId = "SFC_OptConsistencyLog";
			dynamicFormShowParameter.ParentPageId = this.View.PageId;
			dynamicFormShowParameter.OpenStyle.ShowType = ShowType.Floating;
			dynamicFormShowParameter.CustomComplexParams.Add("DataObject", this.optConsistencyLogObject);
			if (this.optConsistencyLogObject == null || this.optConsistencyLogObject.IsNullOrEmpty())
			{
				return;
			}
			this.View.ShowForm(dynamicFormShowParameter);
		}
		public override void AfterDoOperation(AfterDoOperationEventArgs e)
		{
			base.AfterDoOperation(e);
			string operation;
			if ((operation = e.Operation.Operation) != null)
			{
				if (!(operation == "ChangeSelectedOper"))
				{
					return;
				}
				this.ChangeSelectedOper();
			}
		}
		public override void BeforeUpdateValue(BeforeUpdateValueEventArgs e)
		{
			base.BeforeUpdateValue(e);
			string key;
			if ((key = e.Key) != null)
			{
				if (!(key == "FActualQty"))
				{
					return;
				}
				this.ActualQtyBeforeUpdateValue(e);
			}
		}
		private void ChangeSelectedOper()
		{
			EntryEntity entryEntity = this.View.BusinessInfo.GetEntryEntity("FOptPlanEntity");
			System.Collections.Generic.IEnumerable<DynamicObject> source =
				from o in this.Model.GetEntityDataObject(entryEntity)
				where o.GetDynamicObjectItemValue("IsSelectOper", false)
				select o;
			Field field = this.View.BusinessInfo.GetField("FIsSelectPickMat");
			EntryEntity entryEntity2 = this.View.BusinessInfo.GetEntryEntity("FPickMatEntity");
			DynamicObjectCollection entityDataObject = this.Model.GetEntityDataObject(entryEntity2);
			if ("A".Equals(this.Model.DataObject["PickType"].ToString()))
			{
				System.Collections.Generic.List<long> list = (
					from o in source
					select o.GetDynamicObjectItemValue("OperId", 0L)).ToList<long>();
				using (System.Collections.Generic.IEnumerator<DynamicObject> enumerator = entityDataObject.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						DynamicObject current = enumerator.Current;
						long dynamicObjectItemValue = current.GetDynamicObjectItemValue("PmOperationID", 0L);
						if (list.Contains(dynamicObjectItemValue))
						{
							this.Model.SetValue(field, current, true);
						}
						else
						{
							this.Model.SetValue(field, current, false);
						}
					}
					return;
				}
			}
			if ("B".Equals(this.Model.DataObject["PickType"].ToString()))
			{
				System.Collections.Generic.List<string> list2 = (
					from o in source
					select string.Format("{0}{1}", o["MOEntryId"].ToString(), o["ProcessId_Id"].ToString())).ToList<string>();
				foreach (DynamicObject current2 in entityDataObject)
				{
					string item = string.Format("{0}{1}", current2["MOEntryId"].ToString(), current2["ProcessId_Id"]);
					if (list2.Contains(item))
					{
						this.Model.SetValue(field, current2, true);
					}
					else
					{
						this.Model.SetValue(field, current2, false);
					}
				}
			}
		}
		private void ActualQtyBeforeUpdateValue(BeforeUpdateValueEventArgs e)
		{
			if (e.Value.IsNullOrEmptyOrWhiteSpace())
			{
				return;
			}
			EntryEntity entryEntity = this.View.BusinessInfo.GetEntryEntity("FPickMatEntity");
			DynamicObject entityDataObject = this.Model.GetEntityDataObject(entryEntity, e.Row);
			if ("3".Equals(entityDataObject["OverControlMode"].ToString()) && decimal.Compare(e.Value.ConvertTo(0m),Convert.ToDecimal( entityDataObject["AppQty"])) > 0)
			{
				this.View.ShowErrMessage(string.Format(this.strMsg5, ((DynamicObject)entityDataObject["ChiMaterialId"])["Number"].ToString()), "", MessageBoxType.Notice);
				e.Cancel = true;
			}
		}
		private void PickingQtyBeforeUpdateValue(BeforeUpdateValueEventArgs e)
		{
			if (e.Value.IsNullOrEmptyOrWhiteSpace())
			{
				return;
			}
			EntryEntity entryEntity = this.View.BusinessInfo.GetEntryEntity("FOptPlanEntity");
			DynamicObject entityDataObject = this.Model.GetEntityDataObject(entryEntity, e.Row);
			if (decimal.Compare(e.Value.ConvertTo(0m), entityDataObject.GetDynamicObjectItemValue("OperMoQty", 0m)) > 0)
			{
				string format = this.strMsg6;
				if ("A".Equals(this.Model.DataObject["PickType"].ToString()))
				{
					format = this.strMsg6;
				}
				else
				{
					if ("B".Equals(this.Model.DataObject["PickType"].ToString()))
					{
						format = this.strMsg7;
					}
				}
				this.View.ShowErrMessage(string.Format(format, new object[0]), "", MessageBoxType.Notice);
				e.Cancel = true;
			}
		}
		private void InitCustomParameters()
		{
			System.Collections.Generic.Dictionary<string, object> customParameters = this.View.OpenParameter.GetCustomParameters();
			if (customParameters.ContainsKey("NeedMapping"))
			{
				this.needMapping = customParameters["NeedMapping"].ConvertTo(false);
			}
			if (customParameters.ContainsKey("DataCache"))
			{
				this.result = (customParameters["DataCache"] as IOperationResult);
			}
		}
		private void LoadData()
		{
			if (this.isViewChanging)
			{
				return;
			}
			if (this.result != null && this.result.ValidationErrors != null && this.result.ValidationErrors.Count > 0)
			{
				string title = ResManager.LoadKDString("数据校验不通过！", "015649000028085", SubSystemType.MFG, new object[0]);
				this.View.ShowErrMessage(string.Join(";",
					from o in this.result.ValidationErrors
					select o.Message), title, MessageBoxType.Notice);
				return;
			}
			if (this.inFlag == 0 && !this.needMapping)
			{
				DynamicObject dynamicObject = this.result.FuncResult as DynamicObject;
				if (dynamicObject != null)
				{
					this.optConsistencyLogObject = dynamicObject;
					this.ShowOptConsistencyLog(1);
				}
			}
			System.Collections.Generic.IEnumerable<DynamicObject> successDataEnity = this.result.SuccessDataEnity;
			if (successDataEnity != null && successDataEnity.Any<DynamicObject>())
			{
				if (this.needMapping)
				{
					this.View.Model.DataObject = this.result.SuccessDataEnity.FirstOrDefault<DynamicObject>();
					this.needMapping = false;
					this.View.UpdateView();
				}
				return;
			}
			if (!this.result.IsSuccess && this.result.FuncResult != null)
			{
				DynamicObject dynamicObject2 = this.result.FuncResult as DynamicObject;
				DynamicObjectCollection dynamicObjectCollection = dynamicObject2["Entity"] as DynamicObjectCollection;
				System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(this.strMsg0);
				foreach (DynamicObject current in dynamicObjectCollection)
				{
					stringBuilder.AppendLine(System.Convert.ToString(current["Description"]));
				}
				this.View.ShowErrMessage(stringBuilder.ToString(), Consts.ERROR_TITLE, MessageBoxType.Notice);
				return;
			}
			this.View.ShowErrMessage(this.strMsg0, Consts.ERROR_TITLE, MessageBoxType.Notice);
		}
		private void ChangeView()
		{
			string layoutId;
			if (this.IsNeedChangeView(out layoutId))
			{
				object customParameter = this.View.OpenParameter.GetCustomParameter("optIds");
				DynamicFormShowParameter dynamicFormShowParameter = new DynamicFormShowParameter();
				this.CopyCustomeParameter(dynamicFormShowParameter);
				dynamicFormShowParameter.OpenStyle.ShowType = ShowType.InCurrentForm;
				dynamicFormShowParameter.FormId = this.View.OpenParameter.FormId;
				dynamicFormShowParameter.LayoutId = layoutId;
				dynamicFormShowParameter.ParentPageId = this.View.OpenParameter.ParentPageId;
				dynamicFormShowParameter.PageId = this.View.PageId;
				dynamicFormShowParameter.CustomComplexParams.Add("NeedMapping", true);
				dynamicFormShowParameter.CustomComplexParams.Add("DataCache", this.result);
				dynamicFormShowParameter.CustomComplexParams.Add("optIds", customParameter);
				this.View.OpenParameter.IsOutOfTime = true;
				this.isViewChanging = true;
				this.View.ShowForm(dynamicFormShowParameter);
			}
		}
		private bool IsNeedChangeView(out string newLayoutId)
		{
			newLayoutId = this.GetLayoutId();
			if (this.result != null && !this.result.ValidationErrors.IsNullOrEmpty() && this.result.ValidationErrors.IsCountGreaterThan(0))
			{
				return false;
			}
			string text = this.View.OpenParameter.LayoutId ?? "";
			return !text.Equals(newLayoutId);
		}
		private void CopyCustomeParameter(DynamicFormShowParameter para)
		{
			System.Type type = this.View.OpenParameter.GetType();
			System.Reflection.PropertyInfo[] properties = type.GetProperties();
			System.Collections.Generic.List<string> list = new System.Collections.Generic.List<string>
			{
				"pk",
				"billType"
			};
			System.Collections.Generic.Dictionary<string, object> customParameters = this.View.OpenParameter.GetCustomParameters();
			if (customParameters != null && customParameters.Count > 0)
			{
				using (System.Collections.Generic.Dictionary<string, object>.Enumerator enumerator = customParameters.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						System.Collections.Generic.KeyValuePair<string, object> item = enumerator.Current;
						System.Collections.Generic.List<string> arg_93_0 = list;
						System.Collections.Generic.KeyValuePair<string, object> item6 = item;
						if (!arg_93_0.Contains(item6.Key))
						{
							System.Collections.Generic.KeyValuePair<string, object> item2 = item;
							if (item2.Value is string)
							{
								System.Reflection.PropertyInfo left = properties.FirstOrDefault(delegate (System.Reflection.PropertyInfo p)
								{
									string arg_14_0 = p.Name;
									System.Collections.Generic.KeyValuePair<string, object> item5 = item;
									return arg_14_0.Equals(item5.Key);
								});
								if (!(left != null))
								{
									System.Collections.Generic.Dictionary<string, string> arg_103_0 = para.CustomParams;
									System.Collections.Generic.KeyValuePair<string, object> item3 = item;
									string arg_103_1 = item3.Key;
									System.Collections.Generic.KeyValuePair<string, object> item4 = item;
									arg_103_0[arg_103_1] = System.Convert.ToString(item4.Value);
								}
							}
						}
					}
				}
			}
		}
		private string GetLayoutId()
		{
			string text = "";
			if (this.result != null)
			{
				string text2 = this.result.Sponsor;
				if (this.result.SuccessDataEnity != null && this.result.SuccessDataEnity.IsCountGreaterThan(0))
				{
					text2 = this.result.SuccessDataEnity.FirstOrDefault<DynamicObject>()["PickType"].ToString();
				}
				string a;
				if ((a = text2) != null)
				{
					if (!(a == "A"))
					{
						if (a == "B")
						{
							text = "6b7ae74b-7681-4426-b9ad-33a7f6cf1bf7";
						}
					}
					else
					{
						text = "";
					}
				}
			}
			return text;
		}
		private string GetSelectBillInfo(System.Collections.Generic.IEnumerable<DynamicObject> selectItems, string entityKey)
		{
			System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
			string format = ResManager.LoadKDString("工序计划[{0}]-序列[{1}]-工序[{2}]-子项物料[{3}]", "015649030034872", SubSystemType.MFG, new object[0]);
			string format2 = ResManager.LoadKDString("工序计划[{0}]-序列[{1}]-工序[{2}]", "015165030034476", SubSystemType.MFG, new object[0]);
			foreach (DynamicObject current in selectItems)
			{
				if (current.DynamicObjectType.ExtendName == "PickMatEntity")
				{
					stringBuilder.AppendFormat(format, new object[]
					{
						current["PmBillNo"],
						current["PmSeqNumber"],
						current["PmOperNumber"],
						(current["ChiMaterialId"] as DynamicObject)["Name"]
					});
				}
				else
				{
					if (current.DynamicObjectType.ExtendName == "OptPlanEntity")
					{
						stringBuilder.AppendFormat(format2, current["BillNo"], current["SeqNumber"], current["OperNumber"]);
					}
				}
			}
			return stringBuilder.ToString();
		}
	}
}
