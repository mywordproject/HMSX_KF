using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Report;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Resource;
using Kingdee.BOS.Util;
using Kingdee.K3.FIN.App.Core;
using Kingdee.K3.FIN.App.Core.FINServiceForCN;
using Kingdee.K3.FIN.CN.App.Core;
using Kingdee.K3.FIN.CN.App.Report;
using Kingdee.K3.FIN.Core;
using Kingdee.K3.FIN.Core.FilterCondition;
using Kingdee.K3.FIN.Core.Object.CN;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
namespace HMSX.Second.Plugin.study
{
	[Description("银行日报表服务")]
	[Kingdee.BOS.Util.HotUpdate]
	public class BankReport : DailyReportBase
	{
		public bool IsFilterBySettleOrg
		{
			get;
			set;
		}
		public bool IsFilterByPayOrg
		{
			get;
			set;
		}
		public bool IsFilterByInnerPayOrg
		{
			get;
			set;
		}
		public override void Initialize()
		{
			base.Initialize();
			base.ReportProperty.DetailReportFormIdFieldName = "FDetailReportFormId";
		}
		protected override void AddHeader(ReportHeader header)
		{
			if (this.IsFilterByPayOrg)
			{
				header.AddChild("FBankName", new LocaleValue(ResManager.LoadKDString("银行", "003256000003823", SubSystemType.FIN, new object[0]), base.Context.UserLocale.LCID));
				header.AddChild("FBANKACCTNAME", new LocaleValue(ResManager.LoadKDString("账户名称", "003256000003826", SubSystemType.FIN, new object[0]), base.Context.UserLocale.LCID));
				header.AddChild("FBANKACCTNO", new LocaleValue(ResManager.LoadKDString("银行账号", "003256000003829", SubSystemType.FIN, new object[0]), base.Context.UserLocale.LCID));
			}
			if (this.IsFilterBySettleOrg || this.IsFilterByInnerPayOrg)
			{
				header.AddChild("FINNERACCTNAME", new LocaleValue(ResManager.LoadKDString("内部账号名称", "003256000011685", SubSystemType.FIN, new object[0])));
				header.AddChild("FINNERACCTNO", new LocaleValue(ResManager.LoadKDString("内部账号", "003256000011686", SubSystemType.FIN, new object[0])));
			}
		}
		public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
		{
			if (filter.IsNullOrEmpty() || filter.FilterParameter.CustomFilter.IsNullOrEmpty())
			{
				return;
			}
			using (new SessionScope())
			{
				string text = DBUtils.CreateSessionTemplateTable(base.Context, "TM_CN_DailyReportBase", this.CreateRptTempTable());
				this.InsertTempTable(filter, text);
				if (base.FilterCondition.IsShowMyCurrencySum)
				{
					this.InsertLocalCurrencySubTotal(text);
				}
				this.InsertSubTotalByFPayOrgId(text);
				if (base.FilterCondition.IsShowSum)
				{
					base.InsertSubTotal(text);
					base.UpdateRowTypeName(text);
				}
				this.FillRptTable(tableName, text, filter);
				base.UpdateSubSumTitle(tableName);
				this.UpdateLocalCurrencySumTitle(tableName);
				this.ClearDate(tableName);
				DBUtils.DropSessionTemplateTable(base.Context, "TM_CN_DailyReportBase");
			}
		}
		public override ReportTitles GetReportTitles(IRptParams filter)
		{
			ReportTitles reportTitles = new ReportTitles();
			if (filter == null || filter.FilterParameter.CustomFilter == null)
			{
				return reportTitles;
			}
			DynamicObject customFilter = filter.FilterParameter.CustomFilter;
			string sTitleValue = string.Empty;
			string sTitleValue2 = string.Empty;
			DynamicObjectCollection dynamicObjectCollection = customFilter["FSETTLEORGID"] as DynamicObjectCollection;
			DynamicObjectCollection dynamicObjectCollection2 = customFilter["FINNERACCTID"] as DynamicObjectCollection;
			DynamicObjectCollection dynamicObjectCollection3 = customFilter["BankAccountID"] as DynamicObjectCollection;
			if (!dynamicObjectCollection.IsNullOrEmpty() && dynamicObjectCollection.Count != 0)
			{
				sTitleValue = string.Join(",", dynamicObjectCollection.Select(delegate (DynamicObject a)
				{
					if (a["FSETTLEORGID"] != null)
					{
						return ((DynamicObject)a["FSETTLEORGID"])["Name"].ToString();
					}
					return "";
				}));
				if (!dynamicObjectCollection2.IsNullOrEmpty() && dynamicObjectCollection2.Count != 0)
				{
					sTitleValue2 = string.Join(",", dynamicObjectCollection2.Select(delegate (DynamicObject a)
					{
						if (a["FINNERACCTID"] != null)
						{
							return ((DynamicObject)a["FINNERACCTID"])["Name"].ToString();
						}
						return "";
					}));
				}
			}
			reportTitles.AddTitle("FSETTLEORGNAME", sTitleValue);
			reportTitles.AddTitle("FINNERACCTNAMEs", sTitleValue2);
			string sTitleValue3 = string.Empty;
			string sTitleValue4 = string.Empty;
			string sTitleValue5 = string.Empty;
			string sTitleValue6 = this.SetBankValueByBankAcnt(dynamicObjectCollection3);
			string sTitleValue7 = string.Empty;
			DynamicObjectCollection dynamicObjectCollection4 = customFilter["OrgId"] as DynamicObjectCollection;
			if (dynamicObjectCollection4 != null && dynamicObjectCollection4.Count != 0)
			{
				sTitleValue3 = string.Join(",", dynamicObjectCollection4.Select(delegate (DynamicObject a)
				{
					if (a["OrgId"] != null)
					{
						return ((DynamicObject)a["OrgId"])["Name"].ToString();
					}
					return "";
				}));
				DynamicObject dynamicObject = customFilter["Affiliation"] as DynamicObject;
				if (dynamicObject != null)
				{
					sTitleValue5 = Convert.ToString(dynamicObject["Name"].ToString());
				}
				if (!dynamicObjectCollection3.IsNullOrEmpty() && dynamicObjectCollection3.Count != 0)
				{
					sTitleValue4 = string.Join(",", dynamicObjectCollection3.Select(delegate (DynamicObject a)
					{
						if (a["BankAccountID"] != null)
						{
							return ((DynamicObject)a["BankAccountID"])["Number"].ToString();
						}
						return "";
					}));
					sTitleValue7 = string.Join(",", dynamicObjectCollection3.Select(delegate (DynamicObject a)
					{
						if (a["BankAccountID"] != null)
						{
							return ((DynamicObject)a["BankAccountID"])["NAME"].ToString();
						}
						return "";
					}));
				}
			}
			reportTitles.AddTitle("FAffiliationName", sTitleValue5);
			reportTitles.AddTitle("FBankAcctNames", sTitleValue4);
			reportTitles.AddTitle("FOrgName", sTitleValue3);
			reportTitles.AddTitle("FBankAccountName", sTitleValue7);
			reportTitles.AddTitle("FBankNames", sTitleValue6);
			string sTitleValue8 = string.Empty;
			string sTitleValue9 = string.Empty;
			DynamicObjectCollection dynamicObjectCollection5 = customFilter["FInnerPayOrgId"] as DynamicObjectCollection;
			DynamicObjectCollection dynamicObjectCollection6 = customFilter["FPAYORGINNERACCTID"] as DynamicObjectCollection;
			if (dynamicObjectCollection5 != null && dynamicObjectCollection5.Count > 0)
			{
				sTitleValue8 = string.Join(",", dynamicObjectCollection5.Select(delegate (DynamicObject a)
				{
					if (a["FInnerPayOrgId"] != null)
					{
						return ((DynamicObject)a["FInnerPayOrgId"])["Name"].ToString();
					}
					return "";
				}));
				if (!dynamicObjectCollection6.IsNullOrEmpty() && dynamicObjectCollection6.Count != 0)
				{
					sTitleValue9 = string.Join(",", dynamicObjectCollection6.Select(delegate (DynamicObject a)
					{
						if (a["FPAYORGINNERACCTID"] != null)
						{
							return ((DynamicObject)a["FPAYORGINNERACCTID"])["Name"].ToString();
						}
						return "";
					}));
				}
			}
			reportTitles.AddTitle("FInnerPayOrgName", sTitleValue8);
			reportTitles.AddTitle("FPAYORGINNERACCTNAME", sTitleValue9);
			DateTime dateTime = Convert.ToDateTime(customFilter["StartDate"]);
			DateTime dateTime2 = Convert.ToDateTime(customFilter["EndDate"]);
			reportTitles.AddTitle("FStartDate", dateTime.ToString("yyyy-MM-dd"));
			reportTitles.AddTitle("FEndDate", dateTime2.ToString("yyyy-MM-dd"));
			string currencyNames = ReportCommonFunction.GetCurrencyNames(base.Context, customFilter["CurrencyIds"].ToString());
			reportTitles.AddTitle("FCurrencyNames", currencyNames);
			return reportTitles;
		}
		protected override void InsertTempTable(IRptParams filter, string tempTable)
		{
			DailyReportCondition filterCondition = this.GetFilterCondition(filter);
			if (filterCondition == null)
			{
				return;
			}
			DynamicObject customFilter = filter.FilterParameter.CustomFilter;
			filterCondition.CashBankBizType = CashBankBusinessType.Bank;
			this.IsFilterBySettleOrg = (!customFilter["FSettleOrgBox"].IsNullOrEmpty() && Convert.ToBoolean(customFilter["FSettleOrgBox"]));
			this.IsFilterByPayOrg = (!customFilter["FPayOrgIdBox"].IsNullOrEmpty() && Convert.ToBoolean(customFilter["FPayOrgIdBox"]));
			this.IsFilterByInnerPayOrg = (!customFilter["FInnerPayOrgBox"].IsNullOrEmpty() && Convert.ToBoolean(customFilter["FInnerPayOrgBox"]));
			if (this.IsFilterBySettleOrg)
			{
				filterCondition.CashBankBizType = CashBankBusinessType.SettleOrgInnerAcct;
				filterCondition.OrgIds = ReportCommonFunction.GetOrgIds(customFilter, "FSETTLEORGID", "FSETTLEORGID_ID");
				filterCondition.InnerAcctIds = ReportCommonFunction.GetBankOrInnerAccounts(base.Context, filter, "FINNERACCTID", "FINNERACCTID_Id", true, "CN_INNERACCOUNT");
				this.InsertTmpTableWithCondition(filterCondition, tempTable);
			}
			if (this.IsFilterByPayOrg)
			{
				filterCondition.CashBankBizType = CashBankBusinessType.Bank;
				filterCondition.OrgIds = ReportCommonFunction.GetMulOrgIdsByAffiliation(customFilter, base.Context, base.BusinessInfo.GetForm().Id);
				filterCondition.BankAcctIds = ReportCommonFunction.GetBankOrInnerAccounts(base.Context, filter, "BankAccountID", "BankAccountID_Id", false, "CN_BANKACNT");
				if (filterCondition.IsShowPayOrg)
				{
					this.InsertTmpTableWithConditionForOrg(filterCondition, tempTable);
				}
				else
				{
					this.InsertTmpTableWithCondition(filterCondition, tempTable);
				}
			}
			if (this.IsFilterByInnerPayOrg)
			{
				filterCondition.CashBankBizType = CashBankBusinessType.PayOrgInnerAcct;
				filterCondition.OrgIds = ReportCommonFunction.GetOrgIds(customFilter, "FInnerPayOrgId", "FInnerPayOrgId_Id");
				filterCondition.PayInnerAcctIds = ReportCommonFunction.GetBankOrInnerAccounts(base.Context, filter, "FPAYORGINNERACCTID", "FPAYORGINNERACCTID_Id", true, "CN_INNERACCOUNT");
				this.InsertTmpTableWithCondition(filterCondition, tempTable);
			}
		}
		protected override DailyReportCondition GetFilterCondition(IRptParams filter)
		{
			DailyReportCondition filterCondition = base.GetFilterCondition(filter);
			DynamicObjectCollection dynamicObjectCollection = DBUtils.ExecuteDynamicObject(base.Context, "select FID from T_BD_SETTLETYPE where FTYPE = '2'", null, null, CommandType.Text, new SqlParam[0]);
			if (dynamicObjectCollection == null || dynamicObjectCollection.Count == 0)
			{
				throw new KDException("1", ResManager.LoadKDString("结算方式基础资料未设置", "003256000003832", SubSystemType.FIN, new object[0]));
			}
			filterCondition.SettleType = (
				from a in dynamicObjectCollection
				select Convert.ToInt64(a["FID"])).ToList<long>();
			DynamicObject customFilter = filter.FilterParameter.CustomFilter;
			filterCondition.IsShowPayOrg = Convert.ToBoolean(customFilter["FMyPayOrg"]);
			filterCondition.IsShowSum = Convert.ToBoolean(customFilter["CurrencySubTotal"]);
			return filterCondition;
		}
		protected override string GetOrderBy(IRptParams filter)
		{
			string text = "t0.FDate,t0.FRowTypeName,t0.FRowType,t0.FMasterID,t0.FForCurrencyId";
			if (base.FilterCondition.IsShowPayOrg && base.FilterCondition.IsShowMyCurrencySum)
			{
				text = string.Format("{0},org.FNUMBER ", text);
			}
			return text;
		}
		protected override void FillRptTable(string tableName, string tempTable, IRptParams filter)
		{
			Tuple<string, string> tuple = ReportCommonFunction.GetOrgNamesNIds(filter);
			Tuple<string, string> settleOrgNamesNIds = ReportCommonFunction.GetSettleOrgNamesNIds(filter);
			Tuple<string, string> innerPayOrgNamesNIds = ReportCommonFunction.GetInnerPayOrgNamesNIds(filter);
			Tuple<string, string> affiliationNamesIds = ReportCommonFunction.GetAffiliationNamesIds(filter);
			string value = string.Format(" '{0}' as FORGNAME,\r\n                                '{1}' as FORGID,", tuple.Item2, tuple.Item1);
			if (base.FilterCondition.IsShowPayOrg)
			{
				tuple = new Tuple<string, string>("t0.FPAYORGID", "organization.FNAME");
				value = string.Format(" {0} as FORGNAME,\r\n                                {1} as FORGID,", tuple.Item2, tuple.Item1);
			}
			int lCID = base.Context.UserLocale.LCID;
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("select t0.FROWTYPE,t0.FROWTYPENAME,t0.FDATE,t0.FSTARTDATEHIDE,t0.FENDDATEHIDE,t0.FMasterID,organization.FNAME as FPAYORGNAME,");
			stringBuilder.Append("t0.FBANKACCTID,bankAcct.FNUMBER as FBANKACCTNO,bankAcctL.FName as FBANKACCTNAME,bankL.FName as FBANKNAME,");
			stringBuilder.Append("t0.FInnerAcctId, t0.FInAcctMasterID,InnerAcct.fnumber as FINNERACCTNO,InnerAcctL.FNAME as FINNERACCTNAME,");
			stringBuilder.Append("t0.FFORCURRENCYID,t0.FFORLASTBAL,t0.FFORTODAYIN,t0.FFORTODAYOUT,t0.FFORTODAYBAL,t0.FLOCALCURRENCYID,");
			stringBuilder.Append("t0.FLOCALLASTBAL,t0.FLOCALTODAYIN,t0.FLOCALTODAYOUT,t0.FLOCALTODAYBAL,t0.FINCOUNT,t0.FOUTCOUNT,t0.FFORDIGITS,");
			stringBuilder.Append("t0.FLOCALDIGITS,currencyL1.FName as FFORCURRENCYNAME,currencyL2.FName as FLOCALCURRENCYNAME,t0.FNEEDDATE,'CN_BankDetailReport' as FDetailReportFormId,");
			stringBuilder.Append(value);
			stringBuilder.AppendFormat("'{3}' as FSETTLEORGNAME, '{0}' as FSETTLEORGID,'{1}' as FINNERPAYORGNAME,'{2}' as FINNERPAYORGID,", new object[]
			{
				settleOrgNamesNIds.Item1,
				innerPayOrgNamesNIds.Item2,
				innerPayOrgNamesNIds.Item1,
				settleOrgNamesNIds.Item2
			});
			stringBuilder.AppendFormat("'{0}' as FAFFILIATIONNAME,'{1}' as FAFFILIATIONID,", affiliationNamesIds.Item2, affiliationNamesIds.Item1);
			stringBuilder.AppendFormat("{0}", string.Format(this.KSQL_SEQ, this.GetOrderBy(filter)));
			stringBuilder.AppendFormat("into {0} from {1} t0 ", tableName, tempTable);
			stringBuilder.Append("left join T_CN_BANKACNT bankAcct on bankAcct.fmasterID = t0.fmasterID and bankAcct.FCREATEORGID=bankAcct.FUSEORGID ");
			stringBuilder.Append("left join T_CN_INNERACCOUNT InnerAcct on InnerAcct.fmasterid = t0.FInAcctMasterID and InnerAcct.FCREATEORGID=InnerAcct.FUSEORGID ");
			stringBuilder.AppendFormat("left join T_CN_INNERACCOUNT_L InnerAcctL on InnerAcct.FID = InnerAcctL.FID and InnerAcctL.FLocaleid = {0} ", lCID);
			stringBuilder.AppendFormat("left join T_CN_BankAcnt_L bankAcctL on bankAcct.FBANKACNTID = bankAcctL.FBANKACNTID and bankAcctL.FLocaleid = {0} ", lCID);
			stringBuilder.AppendFormat("left join T_BD_BANK_L bankL on bankL.FBankID = bankAcct.FBankID and bankL.FLocaleid = {0} ", lCID);
			stringBuilder.AppendFormat("left join T_BD_Currency_L currencyL1 on currencyL1.FCurrencyID = t0.FForCurrencyId and currencyL1.FLocaleid = {0} ", lCID);
			stringBuilder.AppendFormat("left join T_BD_Currency_L currencyL2 on currencyL2.FCurrencyID = t0.FLocalCurrencyId and currencyL2.FLocaleid = {0} ", lCID);
			stringBuilder.AppendFormat("left join t_org_organizations_l organization on organization.FORGID = t0.FPAYORGID and organization.FLocaleid = {0} ", lCID);
			if (base.FilterCondition.IsShowPayOrg && base.FilterCondition.IsShowMyCurrencySum)
			{
				stringBuilder.AppendLine(" left join t_org_organizations org on org.FORGID =  t0.FPAYORGID ");
			}
			stringBuilder.AppendFormat("order by {0}", this.GetOrderBy(filter));
			DBUtils.ExecuteWithTime(base.Context, stringBuilder.ToString(), null, CNBaseReport.TimeOutSecond);
		}
		private void InsertTmpTableWithCondition(DailyReportCondition reportCondition, string tempTable)
		{
			List<DetailBillResult> balEntryObject = this.GetBalEntryObject();
			List<DetailBillResult> list = (
				from o in new DailyDataHelper(base.Context)
				{
					IsShowPayOrg = reportCondition.IsShowPayOrg,
					IsShowCancelBankAcnt = true
				}.GetDailyList(reportCondition)
				where ((reportCondition.CashBankBizType == CashBankBusinessType.Bank) ? o.AcctMasterId : o.InAcctMasterId) != 0L
				select o).ToList<DetailBillResult>();
			InsertInfo insertInfo = new InsertInfo();
			long num = (
				from a in list
				select a.LocCurrencyId).FirstOrDefault<long>();
			long num2 = (
				from a in list
				select a.LocCurrencyDecimal).FirstOrDefault<long>();
			InsertInfo arg_ED_0 = insertInfo;
			long arg_ED_1;
			if (num != 0L)
			{
				arg_ED_1 = num;
			}
			else
			{
				arg_ED_1 = (
					from a in balEntryObject
					select a.LocCurrencyId).FirstOrDefault<long>();
			}
			arg_ED_0.LocCurrencyId = arg_ED_1;
			InsertInfo arg_125_0 = insertInfo;
			long arg_125_1;
			if (num2 != 0L)
			{
				arg_125_1 = num2;
			}
			else
			{
				arg_125_1 = (
					from a in balEntryObject
					select a.LocCurrencyDecimal).FirstOrDefault<long>();
			}
			arg_125_0.LocCurrencyDecimal = arg_125_1;
			insertInfo.NeedDateFlag = 1;
			insertInfo.DetailReportFormId = string.Empty;
			insertInfo.StartDate = reportCondition.StartDate;
			insertInfo.EndDate = reportCondition.EndDate;
			switch (reportCondition.CashBankBizType)
			{
				case CashBankBusinessType.Bank:
					insertInfo.RowType = 0;
					break;
				case CashBankBusinessType.SettleOrgInnerAcct:
					insertInfo.RowType = 1;
					break;
				case CashBankBusinessType.PayOrgInnerAcct:
					insertInfo.RowType = 2;
					break;
			}
			BulkInsertAdapter blukInsertAdapter = this.GetBlukInsertAdapter(tempTable);
			Dictionary<long, long> acctIdAndMasterIds = this.GetAcctIdAndMasterIds();
			List<long> allCurrencyIds = this.GetAllCurrencyIds(balEntryObject, list, acctIdAndMasterIds.Values.ToList<long>());
			Dictionary<long, int> currencyDecimals = CommonFunction.GetCurrencyDecimals(base.Context, allCurrencyIds);
			DateTime dateTime = reportCondition.StartDate;
			while (dateTime <= reportCondition.EndDate)
			{
				List<long> masterIds = this.GetMasterIds(balEntryObject, list, dateTime);
				insertInfo.Date = dateTime;
				foreach (long current in masterIds)
				{
					this.SetAcctIds(insertInfo, current, acctIdAndMasterIds);
					List<long> currencyIds = this.GetCurrencyIds(balEntryObject, list, current);
					foreach (long current2 in currencyIds)
					{
						List<DetailBillResult> source = this.GetBalData(balEntryObject, current2, current, 0L).ToList<DetailBillResult>();
						DetailBillResult dailyDataRow = this.GetDailyDataRow(list, dateTime, current2, current, 0L);
						List<DetailBillResult> source2 = this.GetBalToTodayRow(list, dateTime, current2, current, 0L).ToList<DetailBillResult>();
						insertInfo.CurrencyId = current2;
						int num3 = 0;
						currencyDecimals.TryGetValue(current2, out num3);
						insertInfo.CurrencyDecimal = (long)num3;
						decimal d = 0m;
						decimal d2 = 0m;
						decimal d3 = 0m;
						decimal d4 = 0m;
						decimal d5 = 0m;
						decimal d6 = 0m;
						if (source.Any<DetailBillResult>() && source.FirstOrDefault<DetailBillResult>().Date < dateTime)
						{
							d = source.Sum((DetailBillResult a) => a.BalanceAmount);
							d2 = source.Sum((DetailBillResult a) => a.BalanceAmountLocal);
						}
						if (source2.Count<DetailBillResult>() != 0)
						{
							d3 = source2.Sum((DetailBillResult a) => a.DebitAmount);
							d4 = source2.Sum((DetailBillResult a) => a.CreditAmount);
							d5 = source2.Sum((DetailBillResult a) => a.DebitAmountLocal);
							d6 = source2.Sum((DetailBillResult a) => a.CreditAmountLocal);
						}
						insertInfo.LastBal = d + d3 - d4;
						insertInfo.LastBalLoc = d2 + d5 - d6;
						DailyReportCommonFunc.Insert(blukInsertAdapter, dailyDataRow, insertInfo, reportCondition.IsShowStdCurrency);
					}
				}
				dateTime = dateTime.AddDays(1.0);
			}
			blukInsertAdapter.Finish();
		}
		private void InsertTmpTableWithConditionForOrg(DailyReportCondition reportCondition, string tempTable)
		{
			List<DetailBillResult> balEntryObject = this.GetBalEntryObject();
			List<DetailBillResult> list = (
				from o in new DailyDataHelper(base.Context)
				{
					IsShowPayOrg = reportCondition.IsShowPayOrg,
					IsShowCancelBankAcnt = true
				}.GetDailyList(reportCondition)
				where ((reportCondition.CashBankBizType == CashBankBusinessType.Bank) ? o.AcctMasterId : o.InAcctMasterId) != 0L
				select o).ToList<DetailBillResult>();
			InsertInfo insertInfo = new InsertInfo();
			long num = (
				from a in list
				select a.LocCurrencyId).FirstOrDefault<long>();
			long num2 = (
				from a in list
				select a.LocCurrencyDecimal).FirstOrDefault<long>();
			InsertInfo arg_ED_0 = insertInfo;
			long arg_ED_1;
			if (num != 0L)
			{
				arg_ED_1 = num;
			}
			else
			{
				arg_ED_1 = (
					from a in balEntryObject
					select a.LocCurrencyId).FirstOrDefault<long>();
			}
			arg_ED_0.LocCurrencyId = arg_ED_1;
			InsertInfo arg_125_0 = insertInfo;
			long arg_125_1;
			if (num2 != 0L)
			{
				arg_125_1 = num2;
			}
			else
			{
				arg_125_1 = (
					from a in balEntryObject
					select a.LocCurrencyDecimal).FirstOrDefault<long>();
			}
			arg_125_0.LocCurrencyDecimal = arg_125_1;
			insertInfo.NeedDateFlag = 1;
			insertInfo.DetailReportFormId = string.Empty;
			insertInfo.StartDate = reportCondition.StartDate;
			insertInfo.EndDate = reportCondition.EndDate;
			switch (reportCondition.CashBankBizType)
			{
				case CashBankBusinessType.Bank:
					insertInfo.RowType = 0;
					break;
				case CashBankBusinessType.SettleOrgInnerAcct:
					insertInfo.RowType = 1;
					break;
				case CashBankBusinessType.PayOrgInnerAcct:
					insertInfo.RowType = 2;
					break;
			}
			BulkInsertAdapter blukInsertAdapter = this.GetBlukInsertAdapter(tempTable);
			Dictionary<long, long> acctIdAndMasterIds = this.GetAcctIdAndMasterIds();
			List<long> allCurrencyIds = this.GetAllCurrencyIds(balEntryObject, list, acctIdAndMasterIds.Values.ToList<long>());
			Dictionary<long, int> currencyDecimals = CommonFunction.GetCurrencyDecimals(base.Context, allCurrencyIds);
			DateTime dateTime = reportCondition.StartDate;
			while (dateTime <= reportCondition.EndDate)
			{
				List<long> masterIds = this.GetMasterIds(balEntryObject, list, dateTime);
				insertInfo.Date = dateTime;
				foreach (long current in masterIds)
				{
					this.SetAcctIds(insertInfo, current, acctIdAndMasterIds);
					List<long> currencyIds = this.GetCurrencyIds(balEntryObject, list, current);
					foreach (long current2 in currencyIds)
					{
						List<long> orgIds = DailyReportCommonFunc.GetOrgIds(balEntryObject, list, current2, current);
						insertInfo.CurrencyId = current2;
						int num3 = 0;
						currencyDecimals.TryGetValue(current2, out num3);
						insertInfo.CurrencyDecimal = (long)num3;
						foreach (long current3 in orgIds)
						{
							insertInfo.OrgId = current3;
							List<DetailBillResult> source = this.GetBalData(balEntryObject, current2, current, current3).ToList<DetailBillResult>();
							DetailBillResult dailyDataRow = this.GetDailyDataRow(list, dateTime, current2, current, current3);
							List<DetailBillResult> source2 = this.GetBalToTodayRow(list, dateTime, current2, current, current3).ToList<DetailBillResult>();
							decimal d = 0m;
							decimal d2 = 0m;
							decimal d3 = 0m;
							decimal d4 = 0m;
							decimal d5 = 0m;
							decimal d6 = 0m;
							if (source.Any<DetailBillResult>() && source.FirstOrDefault<DetailBillResult>().Date < dateTime)
							{
								d = source.Sum((DetailBillResult a) => a.BalanceAmount);
								d2 = source.Sum((DetailBillResult a) => a.BalanceAmountLocal);
							}
							if (source2.Count<DetailBillResult>() != 0)
							{
								d3 = source2.Sum((DetailBillResult a) => a.DebitAmount);
								d4 = source2.Sum((DetailBillResult a) => a.CreditAmount);
								d5 = source2.Sum((DetailBillResult a) => a.DebitAmountLocal);
								d6 = source2.Sum((DetailBillResult a) => a.CreditAmountLocal);
							}
							insertInfo.LastBal = d + d3 - d4;
							insertInfo.LastBalLoc = d2 + d5 - d6;
							DailyReportCommonFunc.Insert(blukInsertAdapter, dailyDataRow, insertInfo, reportCondition.IsShowStdCurrency);
						}
					}
				}
				dateTime = dateTime.AddDays(1.0);
			}
			blukInsertAdapter.Finish();
		}
		private void UpdateLocalCurrencySumTitle(string tableName)
		{
			SqlParamStringBuilder sqlParamStringBuilder = new SqlParamStringBuilder();
			string obj = ResManager.LoadKDString("本位币合计", "003256000023752", SubSystemType.FIN, new object[0]);
			sqlParamStringBuilder.AppendFormat(" UPDATE {0} SET FBANKACCTNAME = {1} WHERE FROWTYPE =40 ", new object[]
			{
				tableName,
				obj.ToSqlParamItem('@')
			});
			DBUtils.Execute(base.Context, sqlParamStringBuilder.Sql, sqlParamStringBuilder.SqlParams);
		}
		private void ClearDate(string tableName)
		{
			string strSQL = string.Format(" update {0} set FDATE = '' where FNeedDate <> 1", tableName);
			DBUtils.ExecuteWithTime(base.Context, strSQL, null, CNBaseReport.TimeOutSecond);
		}
		private void InsertSubTotalByFPayOrgId(string tempTable)
		{
			if (base.FilterCondition.IsShowPayOrg && base.FilterCondition.IsShowStdCurrency && base.FilterCondition.IsShowMyCurrencySum)
			{
				SqlParamStringBuilder sqlParamStringBuilder = new SqlParamStringBuilder();
				sqlParamStringBuilder.AppendFormat("insert into {0} (FROWTYPENAME,FRowType,FDate,FNeedDate,", new object[]
				{
					tempTable
				});
				sqlParamStringBuilder.AppendFormat("FPayOrgId,FLOCALCURRENCYID,FLOCALLASTBAL,FLOCALTODAYIN,FLOCALTODAYOUT,FLOCALTODAYBAL,FLOCALDIGITS,FInCount,FOutCount)", new object[0]);
				sqlParamStringBuilder.AppendFormat(" select FROWTYPENAME, FRowType,'{0}',0,FPayOrgId,FLOCALCURRENCYID,sum(FLOCALLASTBAL),sum(FLOCALTODAYIN),sum(FLOCALTODAYOUT),\r\n                               sum(FLOCALTODAYBAL),FLOCALDIGITS,sum(FInCount),sum(FOutCount) from (", new object[]
				{
					DateTime.MaxValue.AddDays(-2.0)
				});
				sqlParamStringBuilder.AppendFormat(" select '40' FROWTYPENAME, 40 FRowType,FPayOrgId,FLocalCurrencyId, ", new object[0]);
				sqlParamStringBuilder.AppendFormat(" SUM(FLOCALLASTBAL) FLOCALLASTBAL,0 FLOCALTODAYIN ,0 FLOCALTODAYOUT,0 FLOCALTODAYBAL,FLOCALDIGITS,0 FInCount, 0 FOutCount ", new object[0]);
				sqlParamStringBuilder.AppendFormat(" from {0} where  CONVERT(datetime,FDATE)  = CONVERT(datetime,'{1}')  ", new object[]
				{
					tempTable,
					base.FilterCondition.StartDate.ToString("yyyy-MM-dd")
				});
				sqlParamStringBuilder.AppendFormat(" group by FPayOrgId,FLocalCurrencyId,FLocalDigits", new object[0]);
				sqlParamStringBuilder.Append(" UNION ALL ");
				sqlParamStringBuilder.AppendFormat(" SELECT '40' FROWTYPENAME, 40 FRowType,FPayOrgId,FLocalCurrencyId,0 FLOCALLASTBAL, SUM(FLOCALTODAYIN) FLOCALTODAYIN,SUM(FLOCALTODAYOUT) FLOCALTODAYOUT,0 FLOCALTODAYBAL,FLOCALDIGITS,sum(FInCount), sum(FOutCount) ", new object[0]);
				sqlParamStringBuilder.AppendFormat(" from {0} where FRowType<>40 ", new object[]
				{
					tempTable
				});
				sqlParamStringBuilder.AppendFormat(" group by FPayOrgId,FLocalCurrencyId,FLocalDigits ", new object[0]);
				sqlParamStringBuilder.Append(" UNION ALL ");
				sqlParamStringBuilder.AppendFormat(" SELECT  '40' FROWTYPENAME, 40 FRowType,FPayOrgId,FLocalCurrencyId,0 FLOCALLASTBAL,0 FLOCALTODAYIN,0 FLOCALTODAYOUT, SUM(FLOCALTODAYBAL) FLOCALTODAYBAL ,FLOCALDIGITS,0 FInCount,0 FOutCount ", new object[0]);
				sqlParamStringBuilder.AppendFormat(" from {0} where CONVERT(datetime,FDATE) = CONVERT(datetime,'{1}') ", new object[]
				{
					tempTable,
					base.FilterCondition.EndDate.ToString("yyyy-MM-dd")
				});
				sqlParamStringBuilder.AppendFormat(" group by FPayOrgId,FLocalCurrencyId,FLocalDigits ) a  group by FROWTYPENAME, FROWTYPE,FPayOrgId,FLocalCurrencyId,FLOCALDIGITS ", new object[0]);
				DBUtils.ExecuteWithTime(base.Context, sqlParamStringBuilder.Sql, null, CNBaseReport.TimeOutSecond);
			}
		}
		protected override void InsertLocalCurrencySubTotal(string tempTable)
		{
			SqlParamStringBuilder sqlParamStringBuilder = new SqlParamStringBuilder();
			sqlParamStringBuilder.AppendFormat("insert into {0} (FROWTYPENAME,FRowType,FDate,FNeedDate,", new object[]
			{
				tempTable
			});
			sqlParamStringBuilder.AppendFormat("FLOCALCURRENCYID,FLOCALLASTBAL,FLOCALTODAYIN,FLOCALTODAYOUT,FLOCALTODAYBAL,FLOCALDIGITS,FInCount,FOutCount)", new object[0]);
			sqlParamStringBuilder.AppendFormat(" select FROWTYPENAME, FRowType,'{0}',0,FLOCALCURRENCYID,sum(FLOCALLASTBAL),sum(FLOCALTODAYIN),sum(FLOCALTODAYOUT),\r\n                               sum(FLOCALTODAYBAL),FLOCALDIGITS,sum(FInCount),sum(FOutCount) from (", new object[]
			{
				DateTime.MaxValue.AddDays(-1.0)
			});
			sqlParamStringBuilder.AppendFormat(" select '40' FROWTYPENAME, 40 FRowType,FLocalCurrencyId, ", new object[0]);
			sqlParamStringBuilder.AppendFormat(" SUM(FLOCALLASTBAL) FLOCALLASTBAL,0 FLOCALTODAYIN ,0 FLOCALTODAYOUT,0 FLOCALTODAYBAL,FLOCALDIGITS,0 FInCount, 0 FOutCount ", new object[0]);
			sqlParamStringBuilder.AppendFormat(" from {0} where  CONVERT(datetime,FDATE)  = CONVERT(datetime,'{1}')  ", new object[]
			{
				tempTable,
				base.FilterCondition.StartDate.ToString("yyyy-MM-dd")
			});
			sqlParamStringBuilder.AppendFormat(" group by FLocalCurrencyId,FLocalDigits", new object[0]);
			sqlParamStringBuilder.Append(" UNION ALL ");
			sqlParamStringBuilder.AppendFormat(" SELECT '40' FROWTYPENAME, 40 FRowType,FLocalCurrencyId,0 FLOCALLASTBAL, SUM(FLOCALTODAYIN) FLOCALTODAYIN,SUM(FLOCALTODAYOUT) FLOCALTODAYOUT,0 FLOCALTODAYBAL,FLOCALDIGITS,sum(FInCount), sum(FOutCount) ", new object[0]);
			sqlParamStringBuilder.AppendFormat(" from {0}  ", new object[]
			{
				tempTable
			});
			sqlParamStringBuilder.AppendFormat(" group by FLocalCurrencyId,FLocalDigits ", new object[0]);
			sqlParamStringBuilder.Append(" UNION ALL ");
			sqlParamStringBuilder.AppendFormat(" SELECT  '40' FROWTYPENAME, 40 FRowType,FLocalCurrencyId,0 FLOCALLASTBAL,0 FLOCALTODAYIN,0 FLOCALTODAYOUT, SUM(FLOCALTODAYBAL) FLOCALTODAYBAL ,FLOCALDIGITS,0 FInCount,0 FOutCount ", new object[0]);
			sqlParamStringBuilder.AppendFormat(" from {0} where CONVERT(datetime,FDATE) = CONVERT(datetime,'{1}') ", new object[]
			{
				tempTable,
				base.FilterCondition.EndDate.ToString("yyyy-MM-dd")
			});
			sqlParamStringBuilder.AppendFormat(" group by FLocalCurrencyId,FLocalDigits ) a  group by FROWTYPENAME, FROWTYPE,FLocalCurrencyId,FLOCALDIGITS ", new object[0]);
			DBUtils.ExecuteWithTime(base.Context, sqlParamStringBuilder.Sql, null, CNBaseReport.TimeOutSecond);
		}
		private List<DetailBillResult> GetBalEntryObject()
		{
			List<DetailBillResult> result = new List<DetailBillResult>();
			switch (base.FilterCondition.CashBankBizType)
			{
				case CashBankBusinessType.Bank:
					result = CNCommonFunction.GetBalanceAmount(base.Context, base.FilterCondition.StartDate, base.FilterCondition.OrgIds, (int)base.FilterCondition.CashBankBizType, base.FilterCondition.CurrencyIds, base.FilterCondition.BankAcctIds, base.FilterCondition.IsShowStdCurrency, null, base.FilterCondition.IsNotAudit);
					break;
				case CashBankBusinessType.SettleOrgInnerAcct:
					result = CNCommonFunction.GetBalanceAmountBySettleOrg(base.Context, base.FilterCondition.StartDate, base.FilterCondition.OrgIds, (int)base.FilterCondition.CashBankBizType, base.FilterCondition.CurrencyIds, base.FilterCondition.InnerAcctIds, base.FilterCondition.IsShowStdCurrency, base.FilterCondition.IsNotAudit);
					break;
				case CashBankBusinessType.PayOrgInnerAcct:
					result = CNCommonFunction.GetBalanceInnerAmountByPayOrg(base.Context, base.FilterCondition.StartDate, base.FilterCondition.OrgIds, base.FilterCondition.CurrencyIds, base.FilterCondition.PayInnerAcctIds, base.FilterCondition.IsShowStdCurrency, base.FilterCondition.IsNotAudit);
					break;
			}
			return result;
		}
		private List<long> GetAllCurrencyIds(List<DetailBillResult> balEntryObj, IEnumerable<DetailBillResult> dailyList, List<long> masterIds)
		{
			List<long> list = new List<long>();
			foreach (long current in masterIds)
			{
				List<long> currencyIds = this.GetCurrencyIds(balEntryObj, dailyList, current);
				list.AddRange(currencyIds);
			}
			return list.Distinct<long>().ToList<long>();
		}
		private List<long> GetCurrencyIds(List<DetailBillResult> balEntryObj, IEnumerable<DetailBillResult> dailyList, long masterId)
		{
			List<long> result = new List<long>();
			switch (base.FilterCondition.CashBankBizType)
			{
				case CashBankBusinessType.Bank:
					result = DailyReportCommonFunc.GetCurrencyIds(balEntryObj, dailyList, masterId).ToList<long>();
					break;
				case CashBankBusinessType.SettleOrgInnerAcct:
					result = DailyReportCommonFunc.GetCurrencyIdsByInAcctOrBankAcctMasterid(balEntryObj, dailyList, masterId, 0L).ToList<long>();
					break;
				case CashBankBusinessType.PayOrgInnerAcct:
					result = DailyReportCommonFunc.GetCurrencyIdsByInAcct(balEntryObj, dailyList, masterId).ToList<long>();
					break;
			}
			return result;
		}
		private List<long> GetMasterIds(List<DetailBillResult> balEntryObj, IEnumerable<DetailBillResult> dailyList, DateTime date)
		{
			List<long> result = new List<long>();
			switch (base.FilterCondition.CashBankBizType)
			{
				case CashBankBusinessType.Bank:
					{
						IEnumerable<long> first =
							from a in balEntryObj
							select a.AcctMasterId;
						long[] second = (
							from b in dailyList
							where b.Date <= date
							select b into a
							select a.AcctMasterId).ToArray<long>();
						result = (
							from a in first.Union(second)
							orderby a
							select a).ToList<long>();
						break;
					}
				case CashBankBusinessType.SettleOrgInnerAcct:
				case CashBankBusinessType.PayOrgInnerAcct:
					{
						IEnumerable<long> first2 =
							from a in balEntryObj
							select a.InAcctMasterId;
						long[] second2 = (
							from b in dailyList
							where b.Date <= date
							select b into a
							select a.InAcctMasterId).ToArray<long>();
						result = (
							from a in first2.Union(second2)
							orderby a
							select a).ToList<long>();
						break;
					}
				default:
					result = null;
					break;
			}
			return result;
		}
		private Dictionary<long, long> GetAcctIdAndMasterIds()
		{
			switch (base.FilterCondition.CashBankBizType)
			{
				case CashBankBusinessType.Bank:
					return CNCommonFunction.GetBankAcctIdAndMasterIds(base.Context, base.FilterCondition.BankAcctIds, 1);
				case CashBankBusinessType.SettleOrgInnerAcct:
					return CNCommonFunction.GetInnerAcctIdAndMasterIds(base.Context, base.FilterCondition.InnerAcctIds);
				case CashBankBusinessType.PayOrgInnerAcct:
					return CNCommonFunction.GetInnerAcctIdAndMasterIds(base.Context, base.FilterCondition.PayInnerAcctIds);
				default:
					return new Dictionary<long, long>();
			}
		}
		private void SetAcctIds(InsertInfo info, long masterId, Dictionary<long, long> acctIdAndMasterIds)
		{
			List<long> values = (
				from f in acctIdAndMasterIds
				where f.Value == masterId
				select f.Key).ToList<long>();
			switch (base.FilterCondition.CashBankBizType)
			{
				case CashBankBusinessType.Bank:
					info.AcctMaster = masterId;
					info.BankacctIdStr = string.Join<long>(",", values);
					return;
				case CashBankBusinessType.SettleOrgInnerAcct:
					info.InacctMaster = masterId;
					info.InacctIdStr = string.Join<long>(",", values);
					return;
				case CashBankBusinessType.PayOrgInnerAcct:
					info.InacctMaster = masterId;
					info.InacctIdStr = string.Join<long>(",", values);
					return;
				default:
					return;
			}
		}
		private IEnumerable<DetailBillResult> GetBalToTodayRow(IEnumerable<DetailBillResult> dailyList, DateTime date, long currencyId, long masterId, long payOrgId = 0L)
		{
			switch (base.FilterCondition.CashBankBizType)
			{
				case CashBankBusinessType.Bank:
					if (payOrgId > 0L)
					{
						return
							from a in dailyList
							where a.Date < date && a.CurrencyId == currencyId && a.AcctMasterId == masterId && a.OrgId == payOrgId
							select a;
					}
					return
						from a in dailyList
						where a.Date < date && a.CurrencyId == currencyId && a.AcctMasterId == masterId
						select a;
				case CashBankBusinessType.SettleOrgInnerAcct:
				case CashBankBusinessType.PayOrgInnerAcct:
					return
						from a in dailyList
						where a.Date < date && a.CurrencyId == currencyId && a.InAcctMasterId == masterId
						select a;
				default:
					return null;
			}
		}
		private DetailBillResult GetDailyDataRow(IEnumerable<DetailBillResult> dailyList, DateTime date, long currencyId, long masterId, long payOrgId = 0L)
		{
			switch (base.FilterCondition.CashBankBizType)
			{
				case CashBankBusinessType.Bank:
					if (payOrgId > 0L)
					{
						return dailyList.FirstOrDefault((DetailBillResult a) => a.Date == date && a.CurrencyId == currencyId && a.AcctMasterId == masterId && a.OrgId == payOrgId);
					}
					return dailyList.FirstOrDefault((DetailBillResult a) => a.Date == date && a.CurrencyId == currencyId && a.AcctMasterId == masterId);
				case CashBankBusinessType.SettleOrgInnerAcct:
				case CashBankBusinessType.PayOrgInnerAcct:
					return dailyList.FirstOrDefault((DetailBillResult a) => a.Date == date && a.CurrencyId == currencyId && a.InAcctMasterId == masterId);
				default:
					return null;
			}
		}
		private IEnumerable<DetailBillResult> GetBalData(List<DetailBillResult> balEntryObj, long currencyId, long masterId, long payOrgId = 0L)
		{
			switch (base.FilterCondition.CashBankBizType)
			{
				case CashBankBusinessType.Bank:
					if (payOrgId > 0L)
					{
						return
							from a in balEntryObj
							where a.CurrencyId == currencyId && a.AcctMasterId == masterId && a.OrgId == payOrgId
							select a;
					}
					return
						from a in balEntryObj
						where a.CurrencyId == currencyId && a.AcctMasterId == masterId
						select a;
				case CashBankBusinessType.SettleOrgInnerAcct:
				case CashBankBusinessType.PayOrgInnerAcct:
					return
						from a in balEntryObj
						where a.CurrencyId == currencyId && a.InAcctMasterId == masterId
						select a;
				default:
					return null;
			}
		}
		private string SetBankValueByBankAcnt(DynamicObjectCollection bankAcctObjs)
		{
			if (bankAcctObjs == null || bankAcctObjs.Count <= 0)
			{
				return string.Empty;
			}
			IEnumerable<long> source =
				from a in bankAcctObjs
				select Convert.ToInt64(a["BankAccountID_Id"]);
			SqlParamStringBuilder sqlParamStringBuilder = new SqlParamStringBuilder();
			sqlParamStringBuilder.AppendFormat(" SELECT DISTINCT TBANK.FNAME FROM  T_CN_BANKACNT TACT \r\n                                        LEFT JOIN T_BD_BANK_L TBANK ON TACT.FBANKID=TBANK.FBANKID \r\n                                        WHERE  TACT.FBANKACNTID IN ({0}) AND FLOCALEID={1}", new object[]
			{
				source.ToList<long>().ToSqlParamItem('@'),
				base.Context.UserLocale.LCID.ToSqlParamItem('@')
			});
			DynamicObjectCollection dynamicObjectCollection = DBUtils.ExecuteDynamicObject(base.Context, sqlParamStringBuilder.Sql, null, null, CommandType.Text, sqlParamStringBuilder.SqlParams.ToArray<SqlParam>());
			if (dynamicObjectCollection == null || !dynamicObjectCollection.Any<DynamicObject>())
			{
				return string.Empty;
			}
			List<string> list = (
				from d in dynamicObjectCollection
				where !d["FNAME"].IsNullOrEmptyOrWhiteSpace()
				select d into a
				select Convert.ToString(a["FNAME"])).ToList<string>();
			if (list.Count<string>() <= 0)
			{
				return string.Empty;
			}
			return string.Join(",", list);
		}
	}
}
