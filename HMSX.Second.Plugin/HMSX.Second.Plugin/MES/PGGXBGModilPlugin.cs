using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.Operation;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Metadata.ConvertElement;
using Kingdee.BOS.Core.Metadata.ConvertElement.ServiceArgs;
using Kingdee.BOS.Mobile;
using Kingdee.BOS.Mobile.PlugIn;
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.Complex;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using System.ComponentModel;
using Kingdee.BOS.Resource;
using Kingdee.K3.MFG.ServiceHelper.SFS;
using Kingdee.BOS.Mobile.PlugIn.ControlModel;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS;
using Kingdee.K3.MFG.Mobile.ServiceHelper;
using Kingdee.BOS.Orm;
using Kingdee.K3.MFG.Common.BusinessEntity.SFC.SFCEntity;
using HMSX.MFG.Mobile.Business.PlugIn.Second;

namespace HMSX.Second.Plugin.MES
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("派工工序报工")]
    public class PGGXBGModilPlugin: NComplexDispatchReportList
	{
		string F_RUJP_Lot = "";
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
				base.View.GetControl("F_260_FX").Visible = false;
				base.View.UpdateView("F_260_FX");
		}
		public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            if (e.Key.Equals("F_260_FX"))
            {
				int rowIndex = this.Model.GetEntryCurrentRowIndex("FMobileListViewEntity");
				Dictionary<string, object> currentRowData = base.GetCurrentRowData(rowIndex);
                F_RUJP_Lot = currentRowData["F_RUJP_Lot"]==null?"":currentRowData["F_RUJP_Lot"].ToString();
				decimal canRewrokQty = SFCOperationPlanningEntity.Instance.GetCanRewrokQty(base.Context, Convert.ToInt64(currentRowData["FOptPlanOptId"]));
				if (canRewrokQty <= 0m)
				{
					base.View.ShowMessage(ResManager.LoadKDString("不满足 “返工数量-返工选单数量>0” 条件，不能做返修报工", "015747000018208", SubSystemType.MFG, new object[0]), MessageBoxType.Notice);
					return;
				}
				this.ReworkReport(canRewrokQty);
			}
            else if(e.Key.Equals("FBUTTON_FINISHED"))
			{
				base.View.GetControl("F_260_FX").Visible = true;
				base.View.UpdateView("F_260_FX");
			}
			else if (e.Key.Equals("FBUTTON_UNFINISH"))
			{
				base.View.GetControl("F_260_FX").Visible = false;
				base.View.UpdateView("F_260_FX");
			}

		}
		private void ReworkReport(decimal toReportQty)
		{
			Dictionary<string, object> currentRowData = base.GetCurrentRowData();
			//if (currentRowData==null)
			//{
			//	return;
			//}
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
					mobileShowParameter.CustomComplexParams["FOperQty"] = "";
					mobileShowParameter.CustomComplexParams["FFinishSelQty"] = currentRowData["FFinishSelQty"];
					mobileShowParameter.CustomComplexParams["FOperPlanNo"] = currentRowData["FOperPlanNo"];
					mobileShowParameter.CustomComplexParams["InspectStatus"] = currentRowData["InspectStatus"];
					mobileShowParameter.CustomComplexParams["ToReportQty"] = toReportQty;
					mobileShowParameter.CustomComplexParams["IsDispatch"] = true;
					mobileShowParameter.CustomComplexParams["PkId"] = currentRowData["TransPkId"];
					mobileShowParameter.CustomComplexParams["SeqId"] = currentRowData["FOpSeqId"];
					mobileShowParameter.CustomComplexParams["EntryPkId"] = currentRowData["TransEntryPkId"];
					mobileShowParameter.CustomComplexParams["EntryRowIndex"] = currentRowData["TransEntryRowIndex"];
					mobileShowParameter.CustomComplexParams["FIsReturnList"] = SFSDiscreteServiceHelper.GetUserRecentData(base.Context, "YMCS_0002_SYS", this.BillModelFormId);
					mobileShowParameter.CustomComplexParams["IsUseRealTimeCalc"] = currentRowData["IsUseRealTimeCalc"];
					mobileShowParameter.CustomParams.Add("F_RUJP_Lot", F_RUJP_Lot);
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
