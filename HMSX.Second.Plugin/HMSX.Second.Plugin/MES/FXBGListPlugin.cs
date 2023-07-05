using Kingdee.BOS;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.Operation;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Metadata.ConvertElement;
using Kingdee.BOS.Core.Metadata.ConvertElement.ServiceArgs;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS.Mobile;
using Kingdee.BOS.Mobile.PlugIn.ControlModel;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Resource;
using Kingdee.BOS.ServiceHelper;
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.Complex;
using Kingdee.K3.MFG.Mobile.ServiceHelper;
using Kingdee.K3.MFG.ServiceHelper.SFS;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.MES
{
	[Kingdee.BOS.Util.HotUpdate]
	[Description("返修报工列表")]
	public class FXBGListPlugin : ComplexOperReworkRptList
	{
		private bool isDispatch;
		public override void OnInitialize(InitializeEventArgs e)
		{
			this.isDispatch = (base.View.BusinessInfo.GetForm().Id == "SFC_MobileComplexDispReRptList");
			base.OnInitialize(e);
		}
		public override void ButtonClick(ButtonClickEventArgs e)
		{
			base.ButtonClick(e);
			string a;
			if ((a = e.Key.ToUpper()) != null)
			{
				if (a == "F_FX")
				{
					if (this.isDispatch)
					{
						this.DispatchReworkRpt();
						return;
					}
					this.ReworkReport();
					return;
				}
			}
		}
		/// <summary>
		/// 派工返工汇报
		/// </summary>
		private void DispatchReworkRpt()
		{
			Dictionary<string, object> currentRowData = base.GetCurrentRowData();
			if (currentRowData == null)
			{
				base.View.ShowMessage(ResManager.LoadKDString("当前未选中行！", "015747000028226", SubSystemType.MFG, new object[0]), MessageBoxType.Notice);
				return;
			}
			ListSelectedRow listSelectedRow = new ListSelectedRow(Convert.ToString(currentRowData["FOptPlanId"]), Convert.ToString(currentRowData["FOptPlanOptId"]), Convert.ToInt32(currentRowData["FOptPlanOptSeq"]), "SFC_OperationPlanning")
			{
				EntryEntityKey = "FSubEntity"
			};
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			dictionary["FSubEntity"] = Convert.ToString(currentRowData["FOptPlanOptId"]);
			listSelectedRow.FieldValues = dictionary;
			ConvertRuleElement rule = ConvertServiceHelper.GetConvertRules(base.Context, "SFC_OperationPlanning", "SFC_OperationReport").Find((ConvertRuleElement f) => f.IsDefault);
			PushArgs serviceArgs = new PushArgs(rule, new ListSelectedRow[]
			{
		listSelectedRow
			});
			OperateOption operateOption = OperateOption.Create();
			operateOption.SetVariableValue("IsMobileInvoke", true);
			operateOption.SetVariableValue("MobileBizType", "Dispatch");
			operateOption.SetVariableValue("DispatchEntryId", Convert.ToInt64(currentRowData["EntryPkId"]));
			try
			{
				ConvertOperationResult convertOperationResult = MobileCommonServiceHelper.Push(base.Context, serviceArgs, operateOption, false);
				if (convertOperationResult.IsSuccess)
				{
					DynamicObject dataEntity = convertOperationResult.TargetDataEntities.FirstOrDefault<ExtendedDataEntity>().DataEntity;
					dataEntity["BillGenType"] = "C";
					MobileShowParameter mobileShowParameter = new MobileShowParameter();
					mobileShowParameter.FormId = "SFC_MobileComplexOpReworkEdit";
					mobileShowParameter.ParentPageId = base.View.PageId;
					mobileShowParameter.CustomComplexParams["DataPacket"] = dataEntity;
					mobileShowParameter.CustomComplexParams["CurrScanCode"] = this.CurrScanCode;
					mobileShowParameter.CustomComplexParams["ListCurrPage"] = this.CurrPageNumber;
					mobileShowParameter.CustomComplexParams["IsDispatch"] = this.isDispatch;
					mobileShowParameter.CustomComplexParams["FOperQty"] = "";
					mobileShowParameter.CustomComplexParams["FReworkQty"] = currentRowData["FReworkQty"];
					mobileShowParameter.CustomComplexParams["FWorkQty"] = currentRowData["FWorkQty"];
					mobileShowParameter.CustomComplexParams["ToReportQty"] = currentRowData["ToReportQty"];
					mobileShowParameter.CustomComplexParams["ToReportBaseQty"] = currentRowData["ToReportBaseQty"];
					mobileShowParameter.CustomComplexParams["FOperPlanNo"] = currentRowData["FOpSeqNumber"];
					mobileShowParameter.CustomComplexParams["InspectStatus"] = currentRowData["FInspectStatus"];
					mobileShowParameter.CustomComplexParams["PkId"] = currentRowData["FOptPlanId"];
					mobileShowParameter.CustomComplexParams["EntryPkId"] = currentRowData["FOptPlanOptId"];
					mobileShowParameter.CustomComplexParams["EntryRowIndex"] = currentRowData["FOptPlanOptSeq"];
					mobileShowParameter.CustomComplexParams["FIsReturnList"] = SFSDiscreteServiceHelper.GetUserRecentData(base.Context, "YMCS_0002_SYS", this.BillModelFormId);
					mobileShowParameter.CustomComplexParams["FIsFirstPieceInspect"] = currentRowData["FIsFirstPieceInspect"];
					mobileShowParameter.CustomComplexParams["FID"] = currentRowData["PkId"];
					mobileShowParameter.CustomComplexParams["FENTRYID"] = currentRowData["EntryPkId"];
					base.View.ShowForm(mobileShowParameter, delegate (FormResult returnValue)
					{
						base.View.GetControl<MobileListViewControl>("FMobileListViewEntity").SetSelectRows(new int[0]);
						base.View.OpenParameter.SetCustomParameter("ListCurrPage", this.CurrPageNumber);
						this.ReloadListData(null, true);
					});
				}
				else
				{
					StringBuilder stringBuilder = new StringBuilder();
					stringBuilder.AppendLine(ResManager.LoadKDString("报工失败！", "015747000015462", SubSystemType.MFG, new object[0]));
					if (convertOperationResult.ValidationErrors.Count > 0)
					{
						stringBuilder.AppendLine();
						foreach (ValidationErrorInfo current in convertOperationResult.ValidationErrors)
						{
							stringBuilder.AppendLine(current.Message);
						}
					}
					base.View.ShowStatusBarInfo(stringBuilder.ToString());
				}
			}
			catch (KDBusinessException ex)
			{
				base.View.ShowStatusBarInfo(new StringBuilder().AppendLine(ResManager.LoadKDString("报工失败！", "015747000015462", SubSystemType.MFG, new object[0])).AppendLine().AppendLine(ex.Message).ToString());
			}
		}
		/// <summary>
		/// 返工汇报
		/// </summary>
		private void ReworkReport()
		{
			Dictionary<string, object> currentRowData = base.GetCurrentRowData();
			if (currentRowData == null)
			{
				base.View.ShowMessage(ResManager.LoadKDString("当前未选中行！", "015747000028226", SubSystemType.MFG, new object[0]), MessageBoxType.Notice);
				return;
			}
			ListSelectedRow listSelectedRow = new ListSelectedRow(Convert.ToString(currentRowData["PkId"]), Convert.ToString(currentRowData["EntryPkId"]), Convert.ToInt32(currentRowData["EntryRowIndex"]), this.SrcBillModelFormId)
			{
				EntryEntityKey = "FSubEntity"
			};
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			dictionary["FSubEntity"] = Convert.ToString(currentRowData["EntryPkId"]);
			listSelectedRow.FieldValues = dictionary;
			ConvertRuleElement rule = ConvertServiceHelper.GetConvertRules(base.Context, this.SrcBillModelFormId, "SFC_OperationReport").Find((ConvertRuleElement f) => f.IsDefault);
			PushArgs serviceArgs = new PushArgs(rule, new ListSelectedRow[]
			{
		listSelectedRow
			});
			OperateOption operateOption = OperateOption.Create();
			operateOption.SetVariableValue("IsMobileInvoke", true);
			try
			{
				ConvertOperationResult convertOperationResult = MobileCommonServiceHelper.Push(base.Context, serviceArgs, operateOption, false);
				if (convertOperationResult.IsSuccess)
				{
					DynamicObject dataEntity = convertOperationResult.TargetDataEntities.FirstOrDefault<ExtendedDataEntity>().DataEntity;
					dataEntity["BillGenType"] = "C";
					MobileShowParameter mobileShowParameter = new MobileShowParameter();
					mobileShowParameter.FormId = "SFC_MobileComplexOpReworkEdit";
					mobileShowParameter.ParentPageId = base.View.PageId;
					mobileShowParameter.CustomComplexParams["DataPacket"] = dataEntity;
					mobileShowParameter.CustomComplexParams["CurrScanCode"] = this.CurrScanCode;
					mobileShowParameter.CustomComplexParams["ListCurrPage"] = this.CurrPageNumber;
					mobileShowParameter.CustomComplexParams["IsDispatch"] = this.isDispatch;
					mobileShowParameter.CustomComplexParams["FOperQty"] = "";
					mobileShowParameter.CustomComplexParams["FReworkQty"] = currentRowData["FReworkQty"];
					mobileShowParameter.CustomComplexParams["ToReportQty"] = currentRowData["ToReportQty"];
					mobileShowParameter.CustomComplexParams["ToReportBaseQty"] = currentRowData["ToReportBaseQty"];
					mobileShowParameter.CustomComplexParams["FOperPlanNo"] = currentRowData["FOpSeqNumber"];
					mobileShowParameter.CustomComplexParams["InspectStatus"] = currentRowData["FInspectStatus"];
					mobileShowParameter.CustomComplexParams["PkId"] = currentRowData["PkId"];
					mobileShowParameter.CustomComplexParams["EntryPkId"] = currentRowData["EntryPkId"];
					mobileShowParameter.CustomComplexParams["EntryRowIndex"] = currentRowData["EntryRowIndex"];
					mobileShowParameter.CustomComplexParams["FIsReturnList"] = SFSDiscreteServiceHelper.GetUserRecentData(base.Context, "YMCS_0002_SYS", this.BillModelFormId);
					mobileShowParameter.CustomComplexParams["FID"] = currentRowData["PkId"];
					mobileShowParameter.CustomComplexParams["FENTRYID"] = currentRowData["EntryPkId"];
					base.View.ShowForm(mobileShowParameter, delegate (FormResult r)
					{
						base.View.GetControl<MobileListViewControl>("FMobileListViewEntity").SetSelectRows(new int[0]);
						base.View.OpenParameter.SetCustomParameter("ListCurrPage", this.CurrPageNumber);
						this.ReloadListData(null, true);
					});
				}
				else
				{
					StringBuilder stringBuilder = new StringBuilder();
					stringBuilder.AppendLine(ResManager.LoadKDString("报工失败！", "015747000015462", SubSystemType.MFG, new object[0]));
					if (convertOperationResult.ValidationErrors.Count > 0)
					{
						stringBuilder.AppendLine();
						foreach (ValidationErrorInfo current in convertOperationResult.ValidationErrors)
						{
							stringBuilder.AppendLine(current.Message);
						}
					}
					base.View.ShowStatusBarInfo(stringBuilder.ToString());
				}
			}
			catch (KDBusinessException ex)
			{
				base.View.ShowStatusBarInfo(new StringBuilder().AppendLine(ResManager.LoadKDString("报工失败！", "015747000015462", SubSystemType.MFG, new object[0])).AppendLine().AppendLine(ex.Message).ToString());
			}
		}
	}
}
