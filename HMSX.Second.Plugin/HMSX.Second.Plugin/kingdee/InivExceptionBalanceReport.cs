using Kingdee.BOS;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.NetworkCtrl;
using Kingdee.BOS.Core.Report;
using Kingdee.BOS.Core.Report.PlugIn.Args;
using Kingdee.BOS.JSON;
using Kingdee.BOS.Resource;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using Kingdee.K3.Core.MFG.Common;
using Kingdee.K3.Core.MFG.Utils;
using Kingdee.K3.FIN.Business.PlugIn;
using Kingdee.K3.FIN.Core;
using Kingdee.K3.FIN.HS.ServiceHelper;
using Kingdee.K3.FIN.ServiceHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
namespace HMSX.Second.Plugin.kingdee
{
	[Description("期末库存维度余额调整客户端插件")]
	[Kingdee.BOS.Util.HotUpdate]
	public class InivExceptionBalanceReport : HSBaseReportPlugIn
	{
		private bool ChxExpense
		{
			get;
			set;
		}
		private Dictionary<long, long> AllDimeAdj
		{
			get;
			set;
		}
		public override void AfterBindData(EventArgs e)
		{
			base.AfterBindData(e);
			base.BuildCustomParams();
			this.ChxExpense = Convert.ToBoolean(this.SysReportModel.RptParams.FilterParameter.CustomFilter.GetValue("CHXEXPENSE", false));
			this.AllDimeAdj = new Dictionary<long, long>();
			this.SetBarDimeAdjEnable();
			this.View.GetControl<EntryGrid>("FList").SetCustomPropertyValue("AllowDefaultSelected", false);
		}
		public override void AfterDoOperation(AfterDoOperationEventArgs e)
		{
			base.AfterDoOperation(e);
			if (e.Operation.Operation.ToUpperInvariant() == "CREATEADJUSTMENTBILL" || e.Operation.Operation.ToUpperInvariant() == "DELETEADJUSTMENTBILL")
			{
				if (base.AcctgSystemId == 0L)
				{
					this.View.ShowErrMessage(ResManager.LoadKDString("报表缺少必要参数，请先在过滤界面中设置参数", "003206000033569", SubSystemType.FIN, new object[0]), "", MessageBoxType.Notice);
					return;
				}
				if (this.CheckOutStatus())
				{
					this.DoAction(e.Operation.Operation.ToUpperInvariant(), e.Operation.OperationName);
				}
			}
		}
		public override void BarItemClick(BarItemClickEventArgs e)
		{
			base.BarItemClick(e);
			if (e.BarItemKey.Equals("btnRptConnBill"))
			{
				int currentRowValue = this.SysReportView.GetCurrentRowValue("FADJUSTBILLID", 0);
				if (currentRowValue != 0)
				{
					BaseFunction.ShowBill(this.View, "HS_AdjustmentBill", (long)currentRowValue);
				}
			}
		}
		private bool CheckOutStatus()
		{
			bool flag = BaseFunction.CheckIsNowCheckOut(base.Context, base.AcctgSystemId, base.AcctgOrgId, base.AcctPolicyId, base.Year, base.Period);
			if (flag)
			{
				string msg = ResManager.LoadKDString("当前维度正在进行存货期末结账或反结账，不能进行期末库存维度余额调整！", "0034863000022963", SubSystemType.FIN, new object[0]);
				this.View.ShowWarnningMessage(msg, "", MessageBoxOptions.OK, null, MessageBoxType.Advise);
				return false;
			}
			JSONObject systemCurYearPeriod = CommonServiceHelper.GetSystemCurYearPeriod(base.Context, base.AcctgSystemId, base.AcctgOrgId, base.AcctPolicyId, "HS");
			if (base.Year != Convert.ToInt32(systemCurYearPeriod["CurYear"]) || base.Period != Convert.ToInt32(systemCurYearPeriod["CurPeriod"]))
			{
				string msg2 = ResManager.LoadKDString("当前会计期间已发生改变，不能进行期末库存维度余额调整！", "0034863000022964", SubSystemType.FIN, new object[0]);
				this.View.ShowWarnningMessage(msg2, "", MessageBoxOptions.OK, null, MessageBoxType.Advise);
				return false;
			}
			return true;
		}
		public override void OnFormatRowConditions(ReportFormatConditionArgs args)
		{
			if (!(args.DataRow["FEXPENSEID"] is DBNull) && Convert.ToInt32(args.DataRow["FEXPENSEID"]) == 0)
			{
				long num = (args.DataRow["FACCTGDIMEENTRYID"] is DBNull) ? 0L : Convert.ToInt64(args.DataRow["FACCTGDIMEENTRYID"]);
				long num2 = (args.DataRow["FADJUSTBILLID"] is DBNull) ? 0L : Convert.ToInt64(args.DataRow["FADJUSTBILLID"]);
				if (num != 0L && num2 != 0L)
				{
					this.AllDimeAdj.AddWithoutExists(num, num2);
				}
				if (this.ChxExpense)
				{
					FormatCondition formatCondition = new FormatCondition();
					formatCondition.ApplayRow = true;
					formatCondition.BackColor = "#C9DBE0";
					args.FormatConditions.Add(formatCondition);
				}
			}
			DataTableDataRow x=args.DataRow;    //args.DataRow._dr	
			//string y = x._dr.Table;
		}
		private Dictionary<long, List<long>> GetAdjIdToDimeId()
		{
			DataRow[] selectedDataRows = this.SysReportView.SelectedDataRows;
			Dictionary<long, List<long>> dictionary = new Dictionary<long, List<long>>();
			for (int i = 0; i < selectedDataRows.Length; i++)
			{
				long num = string.IsNullOrWhiteSpace(selectedDataRows[i]["FADJUSTBILLID"].ToString()) ? 0L : Convert.ToInt64(selectedDataRows[i]["FADJUSTBILLID"]);
				long num2 = (selectedDataRows[i]["FACCTGDIMEENTRYID"] is DBNull) ? 0L : Convert.ToInt64(selectedDataRows[i]["FACCTGDIMEENTRYID"]);
				if (num2 != 0L)
				{
					if (this.AllDimeAdj.ContainsKey(num2) && num == 0L)
					{
						num = this.AllDimeAdj[num2];
					}
					if (dictionary.ContainsKey(num))
					{
						dictionary[num].Add(num2);
					}
					else
					{
						dictionary.Add(num, new List<long>
						{
							num2
						});
					}
				}
			}
			return dictionary;
		}
		private void DoAction(string operationCode, LocaleValue operationName)
		{
			DataRow[] selectedDataRows = this.SysReportView.SelectedDataRows;
			if (selectedDataRows == null || selectedDataRows.Length == 0)
			{
				this.View.ShowWarnningMessage(ResManager.LoadKDString("没有选择任何数据，请先选择数据！", "003043000017583", SubSystemType.FIN, new object[0]), "", MessageBoxOptions.OK, null, MessageBoxType.Advise);
				return;
			}
			if (selectedDataRows.FirstOrDefault((DataRow i) => i["FACCTGDIMEENTRYID"] != DBNull.Value && Convert.ToInt64(i["FACCTGDIMEENTRYID"]) != 0L) == null)
			{
				this.View.ShowWarnningMessage(string.Format(ResManager.LoadKDString("分组汇总的小计或合计行不能选择{0}，请重新选择数据!", "0034863000022965", SubSystemType.FIN, new object[0]), operationName.ToString()), "", MessageBoxOptions.OK, null, MessageBoxType.Advise);
				return;
			}
			StringBuilder stringBuilder = new StringBuilder();
			string title = string.Empty;
			if (operationCode == "CREATEADJUSTMENTBILL")
			{
				title = string.Format(ResManager.LoadKDString("您正准备调整期末库存维度余额，本次共选择{0}条数据，请确认是否继续？\n点击展开详情可查看出单规则", "0034863000022966", SubSystemType.FIN, new object[0]), selectedDataRows.Length);
				stringBuilder.AppendLine(ResManager.LoadKDString("出单规则：", "0034863000022967", SubSystemType.FIN, new object[0]));
				stringBuilder.AppendLine(ResManager.LoadKDString(" 1、点击出单会同时调整同一个核算维度下的所有库存维度数据；", "0034863000022968", SubSystemType.FIN, new object[0]));
				stringBuilder.AppendLine(ResManager.LoadKDString(" 2、当物料核算维度期末加权价等于零，但是库存维度数量不全部为零时，不进行出单，用户可通过成本调整单自行调整期末余额；", "0034863000022969", SubSystemType.FIN, new object[0]));
				stringBuilder.AppendLine(ResManager.LoadKDString(" 3、当物料核算维度期末加权价小于零时，不进行出单，用户可通过成本调整单自行调整期末余额；", "0034863000022970", SubSystemType.FIN, new object[0]));
				stringBuilder.AppendLine(ResManager.LoadKDString(" 4、当启用费用项目核算，物料核算维度期末加权价大于零，但存在异常费用项目金额（费用项目金额和数量方向不一致）时，异常费用项目将均摊调整到其他费用项目；", "0034863000022971", SubSystemType.FIN, new object[0]));
				stringBuilder.AppendLine(ResManager.LoadKDString(" 5、当核算计价方法为先进先出时，不进行出单。", "0034863000022972", SubSystemType.FIN, new object[0]));
			}
			else
			{
				title = string.Format(ResManager.LoadKDString("您正准备取消已调整的期末库存维度余额，本次共选择{0}条数据，请确认是否继续？", "0034863000022973", SubSystemType.FIN, new object[0]), selectedDataRows.Length);
			}
			this.View.ShowMessage(stringBuilder.ToString(), MessageBoxOptions.YesNo, delegate (MessageBoxResult result)
			{
				if (result == MessageBoxResult.Yes)
				{
					Dictionary<long, List<long>> adjIdToDimeId = this.GetAdjIdToDimeId();
					if (operationCode == "CREATEADJUSTMENTBILL")
					{
						this.CreateAdjustBillByReflect(adjIdToDimeId, operationName);
						return;
					}
					if (operationCode == "DELETEADJUSTMENTBILL")
					{
						this.DeleteAdjustBillByReflect(adjIdToDimeId, operationName);
					}
				}
			}, title, MessageBoxType.Notice);
		}
		private void DeleteAdjustBillByReflect(Dictionary<long, List<long>> billIdDimeId, LocaleValue operationName)
		{
			if (billIdDimeId == null)
			{
				this.View.ShowMessage(ResManager.LoadKDString("没有符合反出单条件的数据，请重新选择！", "003206000033774", SubSystemType.FIN, new object[0]), MessageBoxType.Notice);
				return;
			}
			billIdDimeId.Remove(0L);
			if (billIdDimeId.Count == 0)
			{
				this.View.ShowMessage(ResManager.LoadKDString("没有符合反出单条件的数据，请重新选择！", "003206000033774", SubSystemType.FIN, new object[0]), MessageBoxType.Notice);
				return;
			}
			NetworkCtrlResult netControl = this.GetNetMutex(operationName);
			if (!netControl.StartSuccess)
			{
				this.View.ShowWarnningMessage(netControl.Message, "", MessageBoxOptions.OK, null, MessageBoxType.Advise);
				return;
			}
			TaskProxyItem taskProxyItem = new TaskProxyItem();
			taskProxyItem.Parameters = new List<object>
			{
				base.Context,
				billIdDimeId,
				taskProxyItem.TaskId
			}.ToArray();
			taskProxyItem.ClassName = "Kingdee.K3.FIN.HS.App.Core.AdjustBillService.InivExceptionBalAdjustmentService,Kingdee.K3.FIN.HS.App.Core";
			taskProxyItem.MethodName = "DeleteStockDimeAdjBillByReflect";
			this.View.ShowLoadingForm(taskProxyItem, null, true, delegate (IOperationResult result)
			{
				NetworkCtrlServiceHelper.CommitNetCtrl(this.Context, netControl);
				IOperationResult operationResult = result.FuncResult as IOperationResult;
				this.View.ShowOperateResult(operationResult.OperateResult, "BOS_BatchTips");
				this.View.Refresh();
			});
		}
		private void CreateAdjustBillByReflect(Dictionary<long, List<long>> billIdDimeId, LocaleValue operationName)
		{
			if (billIdDimeId == null && billIdDimeId.Count == 0)
			{
				this.View.ShowMessage(ResManager.LoadKDString("没有符合出单条件的数据，请重新选择！", "003206000033773", SubSystemType.FIN, new object[0]), MessageBoxType.Notice);
				return;
			}
			long num = (
				from i in billIdDimeId.Keys
				where i != 0L
				select i).FirstOrDefault<long>();
			if (num != 0L)
			{
				this.View.ShowWarnningMessage(ResManager.LoadKDString("当前所选数据已有关联的成本调整单，不能重复出单，如需重新出单，请先反出单操作！", "003206000022606", SubSystemType.FIN, new object[0]), "", MessageBoxOptions.OK, null, MessageBoxType.Advise);
				return;
			}
			List<int> list = (
				from i in this.SysReportView.SelectedDataRows
				where Convert.ToString(i["FACCTGDIMEHASADJ"]) == "1"
				select Convert.ToInt32(i["FIDENTITYID"])).ToList<int>();
			if (!list.IsEmpty<int>())
			{
				this.View.ShowWarnningMessage(string.Format(ResManager.LoadKDString("当前所选数据核算维度已有关联的成本调整单，不能重复出单，如需重新出单，请先反出单操作！\n所选序号：{0}", "0034863000023826", SubSystemType.FIN, new object[0]), string.Join<int>(",", list)), "", MessageBoxOptions.OK, null, MessageBoxType.Advise);
				return;
			}
			NetworkCtrlResult netControl = this.GetNetMutex(operationName);
			if (!netControl.StartSuccess)
			{
				this.View.ShowWarnningMessage(netControl.Message, "", MessageBoxOptions.OK, null, MessageBoxType.Advise);
				return;
			}
			if (!InivExceptionBalAdjustmentServiceHelper.CheckIsLastUpdateAdjust(base.Context, this.SysReportModel.DataSource.TableName, base.AcctgID))
			{
				this.View.ShowWarnningMessage(ResManager.LoadKDString("当前报表数据已经过时，请刷新后再操作！", "003206000022447", SubSystemType.FIN, new object[0]), "", MessageBoxOptions.OK, null, MessageBoxType.Advise);
				NetworkCtrlServiceHelper.CommitNetCtrl(base.Context, netControl);
				return;
			}
			TaskProxyItem taskProxyItem = new TaskProxyItem();
			List<object> list2 = new List<object>();
			list2.Add(base.Context);
			list2.Add(billIdDimeId);
			JSONObject periodDate = CommonServiceHelper.GetPeriodDate(base.Context, base.AcctPolicyId, base.Year, base.Period);
			DateTime dateTime = Convert.ToDateTime(periodDate["endDate"]);
			list2.Add(dateTime);
			list2.Add(base.AcctgID);
			list2.Add(taskProxyItem.TaskId);
			taskProxyItem.Parameters = list2.ToArray();
			taskProxyItem.ClassName = "Kingdee.K3.FIN.HS.App.Core.AdjustBillService.InivExceptionBalAdjustmentService,Kingdee.K3.FIN.HS.App.Core";
			taskProxyItem.MethodName = "GenerateStockDimeAdjBillByReflect";
			this.View.ShowLoadingForm(taskProxyItem, null, true, delegate (IOperationResult result)
			{
				NetworkCtrlServiceHelper.CommitNetCtrl(this.Context, netControl);
				IOperationResult operationResult = result.FuncResult as IOperationResult;
				this.View.ShowOperateResult(operationResult.OperateResult, "BOS_BatchTips");
				if (operationResult.IsSuccess)
				{
					this.View.Refresh();
				}
			});
		}
		private NetworkCtrlResult GetNetMutex(LocaleValue operationName)
		{
			LocaleValue name = this.View.BusinessInfo.GetForm().Name;
			NetworkCtrlObject networkCtrlObject = NetworkCtrlServiceHelper.AddNetCtrlObj(base.Context, name, base.GetType().Name, base.GetType().Name + base.AcctgID, NetworkCtrlType.BusinessObjOperateMutex, null, " ", true, true);
			NetworkCtrlServiceHelper.AddMutexNetCtrlObj(base.Context, networkCtrlObject.Id, networkCtrlObject.Id);
			NetWorkRunTimeParam netWorkRunTimeParam = new NetWorkRunTimeParam();
			netWorkRunTimeParam.BillName = name;
			netWorkRunTimeParam.OperationName = operationName;
			netWorkRunTimeParam.OperationDesc = operationName.ToString();
			return NetworkCtrlServiceHelper.BeginNetCtrl(base.Context, networkCtrlObject, netWorkRunTimeParam);
		}
		private void SetBarDimeAdjEnable()
		{
			JSONObject systemCurYearPeriod = CommonServiceHelper.GetSystemCurYearPeriod(base.Context, base.AcctgSystemId, base.AcctgOrgId, base.AcctPolicyId, "HS");
			if (base.Year != Convert.ToInt32(systemCurYearPeriod["CurYear"]) || base.Period != Convert.ToInt32(systemCurYearPeriod["CurPeriod"]))
			{
				this.SysReportView.GetMainBarItem("tbCreateStockDimeAdj").Enabled = false;
				this.SysReportView.GetMainBarItem("tbDeleteStockDimeAdj").Enabled = false;
			}
		}
	}
}
