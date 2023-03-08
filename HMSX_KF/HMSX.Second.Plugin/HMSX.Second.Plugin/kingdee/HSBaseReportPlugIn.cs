using Kingdee.BOS;
using Kingdee.BOS.Core.CommonFilter;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Report;
using Kingdee.BOS.Core.Report.PlugIn;
using Kingdee.BOS.Resource;
using Kingdee.K3.FIN.Core;
using Kingdee.K3.FIN.HS.Report.PlugIn;
using Kingdee.K3.FIN.ServiceHelper;
using System;
namespace HMSX.Second.Plugin.kingdee
{
	public class HSBaseReportPlugIn : AbstractSysReportPlugIn
	{
		protected long AcctgSystemId
		{
			get;
			set;
		}
		protected long AcctgOrgId
		{
			get;
			set;
		}
		protected long AcctPolicyId
		{
			get;
			set;
		}
		protected int Year
		{
			get;
			set;
		}
		protected int Period
		{
			get;
			set;
		}
		protected long AcctgID
		{
			get;
			set;
		}
		public override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			FilterParameter filterParameter = ((ISysReportModel)this.Model).FilterParameter;
			CommonFunction.FilterConvert(this.View.Context, this.View.OpenParameter.GetCustomParameters(), filterParameter, new Action<FilterParameter>(this.FilterAddCondition));
			if ((filterParameter == null || filterParameter.CustomFilter == null || Convert.ToInt64(filterParameter.CustomFilter["AcctgSystemId_Id"]) == 0L) && this.SysReportModel.RptParams.GetCustomParameter<FilterParameter>("ParentReportFilter", null) == null)
			{
			throw new KDBusinessException("HSRePort", ResManager.LoadKDString("报表缺少必要参数，请先在过滤界面中设置参数", "003206000033569", SubSystemType.FIN, new object[0]));
			}
		}
		public override void BeforeDoOperation(BeforeDoOperationEventArgs e)
		{
			base.BeforeDoOperation(e);
			if (e.Operation.FormOperation.Operation.ToUpperInvariant() == "REFRESH")
			{
				FilterParameter filterParameter = ((ISysReportModel)this.Model).FilterParameter;
				if ((filterParameter == null || filterParameter.CustomFilter == null || Convert.ToInt64(filterParameter.CustomFilter["AcctgSystemId_Id"]) == 0L) && this.SysReportModel.RptParams.GetCustomParameter<FilterParameter>("ParentReportFilter", null) == null)
				{
					this.View.ShowErrMessage(ResManager.LoadKDString("报表缺少必要参数，请先在过滤界面中设置参数", "003206000033569", SubSystemType.FIN, new object[0]), "", MessageBoxType.Notice);
					e.Cancel = true;
				}
			}
		}
		protected virtual void FilterAddCondition(FilterParameter filterObject)
		{
		}
		public virtual void BuildCustomParams()
		{
			
			FilterParameter customParameter = this.SysReportModel.RptParams.GetCustomParameter<FilterParameter>("ParentReportFilter", null);
			if (customParameter == null)
			{
				this.AcctgSystemId = this.SysReportModel.RptParams.FilterParameter.CustomFilter.GetValue("ACCTGSYSTEMID_Id", 0L);
				this.AcctgOrgId = this.SysReportModel.RptParams.FilterParameter.CustomFilter.GetValue("ACCTGORGID_Id", 0L);
				this.AcctPolicyId = this.SysReportModel.RptParams.FilterParameter.CustomFilter.GetValue("ACCTPOLICYID_Id", 0L);
				this.Year = this.SysReportModel.RptParams.FilterParameter.CustomFilter.GetValue("Year", 0);
				this.Period = this.SysReportModel.RptParams.FilterParameter.CustomFilter.GetValue("Period", 0);
			}
			else
			{
				this.AcctgSystemId = customParameter.CustomFilter.GetValue("ACCTGSYSTEMID_Id", 0L);
				this.AcctgOrgId = customParameter.CustomFilter.GetValue("ACCTGORGID_Id", 0L);
				this.AcctPolicyId = customParameter.CustomFilter.GetValue("ACCTPOLICYID_Id", 0L);
				this.Year = customParameter.CustomFilter.GetValue("Year", 0);
				this.Period = customParameter.CustomFilter.GetValue("Period", 0);
			}
			this.AcctgID = CommonServiceHelper.GetAcctgID(base.Context, this.AcctgSystemId, this.AcctgOrgId, this.AcctPolicyId, this.Year, this.Period);
		}
	}
}
