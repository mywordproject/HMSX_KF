using HMSX.Second.Plugin.Tool;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Contracts.Report;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Report;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Orm.DataEntity;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace HMSX.Second.Plugin.考勤报表
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("考勤报表")]
    public class KQBBReportPlugin : SysReportBaseService
    {
        String tempTableName;
        /**
         * 初始化
         * */
        public override void Initialize()
        {
            base.Initialize();
            //简单账表类型
            this.ReportProperty.ReportType = ReportType.REPORTTYPE_NORMAL;
            //是否创建零时表
            this.IsCreateTempTableByPlugin = true;
            //是否分组汇总
            this.ReportProperty.IsGroupSummary = true;
            this.ReportProperty.IdentityFieldName = "XH";
        }

        /**
         * 获取过滤条件信息(构造单据信息)
         * */
        public override ReportTitles GetReportTitles(IRptParams filter)
        {
            ReportTitles reprotTitles = new ReportTitles();
            DynamicObject customFilter = filter.FilterParameter.CustomFilter;
            if (customFilter != null)
            {
                String F_260_KQFW = customFilter["F_260_KQFW"] == null ? String.Empty : Convert.ToString(customFilter["F_260_KQFW"]);
                String F_260_CX = customFilter["F_260_CX"] == null ? String.Empty : Convert.ToString(customFilter["F_260_CX"]);
                String F_260_RYFW = customFilter["F_260_RYFW"] == null ? String.Empty : Convert.ToString(customFilter["F_260_RYFW"]);
                //String F_260_RQ = customFilter["F_260_RQ"] == null ? String.Empty : Convert.ToString(customFilter["F_260_RQ"]);
                String F_260_KQKSRQ = customFilter["F_260_KQKSRQ"] == null ? String.Empty : Convert.ToString(customFilter["F_260_KQKSRQ"]);
                String F_260_KQJZRQ = customFilter["F_260_KQJZRQ"] == null ? String.Empty : Convert.ToString(customFilter["F_260_KQJZRQ"]);               
                reprotTitles.AddTitle("F_260_KQFW", F_260_KQFW);
                reprotTitles.AddTitle("F_260_CX", F_260_CX);
                reprotTitles.AddTitle("F_260_RYFW", F_260_RYFW);
               // reprotTitles.AddTitle("F_260_RQ", F_260_RQ);
                reprotTitles.AddTitle("F_260_KQKSRQ", F_260_KQKSRQ);
                reprotTitles.AddTitle("F_260_KQJZRQ", F_260_KQJZRQ);
            }
            return reprotTitles;
        }

        /**
         * 设置单据列
         **/
        public override ReportHeader GetReportHeaders(IRptParams filter)
        {
            DynamicObject customFilter = filter.FilterParameter.CustomFilter;
            ReportHeader reportHeader = new ReportHeader();
            //设置列
            if(Convert.ToString(customFilter["F_260_KQFW"]) == "1")
            {
                reportHeader.AddChild("FJobNumber", new LocaleValue("工号", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FUserName", new LocaleValue("姓名", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FEmployeeType", new LocaleValue("人员类型", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FDeptName", new LocaleValue("部门", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FDate", new LocaleValue("日期", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FGroupName", new LocaleValue("所属考勤组", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FTitle", new LocaleValue("职位名称", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FClassName", new LocaleValue("班次名称", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FStartWorkTime", new LocaleValue("班次的应上班时间", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FEndWorkTime", new LocaleValue("班次的应下班时间", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FOnDutyTime", new LocaleValue("上班打卡时间", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FOffDutyTime", new LocaleValue("下班打卡时间", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FAttendanceResult", new LocaleValue("考勤结果", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FWorkTime", new LocaleValue("工作时长", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FRealWorkTime", new LocaleValue("实际出勤时长", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FWorkCount", new LocaleValue("应出勤天数", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FRealWorkCount", new LocaleValue("实际出勤天数", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FLateTime", new LocaleValue("迟到时长(小时)", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FAbsentCount", new LocaleValue("旷工次数", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FAbsentTime", new LocaleValue("旷工时长(小时)", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FLeaveEarly", new LocaleValue("早退时长(小时)", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FLeaveTime", new LocaleValue("请假时长", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FLeaveCount", new LocaleValue("请假次数", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FOvertime", new LocaleValue("加班时长", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FOverClockTime", new LocaleValue("加班打卡时长", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FDDWorkDayOverTime", new LocaleValue("钉钉工作日加班", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FDDWeekEndOverTime", new LocaleValue("钉钉休息日加班", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FDDHolidayOverTime", new LocaleValue("钉钉节假日加班", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FWorkDayOverTime", new LocaleValue("工作日_加班", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FWeekEndOverTime", new LocaleValue("休息日_加班", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FHolidayOverTime", new LocaleValue("节假日_加班", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FShortRestTime", new LocaleValue("短休", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FFuneralLeaveTime", new LocaleValue("丧假", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FMarriageLeaveTime", new LocaleValue("婚假", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FInjuryLeaveTime", new LocaleValue("工伤假", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FPublicLeaveTime", new LocaleValue("公假", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FExchangeLeaveTime", new LocaleValue("调休假", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FPersonalLeaveTime", new LocaleValue("事假", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FSickLeaveTime", new LocaleValue("病假", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FPaternityLeaveTime", new LocaleValue("陪产假", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FMaternityLeaveTime", new LocaleValue("产假", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FInspectionLeaveTime", new LocaleValue("产检假", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FBreastLeaveTime", new LocaleValue("哺乳假", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FYearLeaveTime", new LocaleValue("年休假", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FSmallMaternityLeave", new LocaleValue("小产假", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FLateCount", new LocaleValue("迟到次数", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FLeaveEarlyCount", new LocaleValue("早退次数", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);

                //reportHeader.AddChild("FDateType",             new LocaleValue("日期类型",                 this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                //reportHeader.AddChild("FWeekType",         new LocaleValue("星期类型",         this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                //reportHeader.AddChild("FAcross",           new LocaleValue("班次是否跨天",         this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                //reportHeader.AddChild("FDDOverTime",         new LocaleValue("钉钉加班时长",                 this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);                         
                //reportHeader.AddChild("FTSReturnTime", new LocaleValue("特殊调休换班总时长", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                //reportHeader.AddChild("FTSDeduTime", new LocaleValue("特殊年休扣减时长", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                //reportHeader.AddChild("FTSAddTime", new LocaleValue("特殊调休增加时长", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                //reportHeader.AddChild("FTSShortRestTime", new LocaleValue("特殊短休合计时长", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                //reportHeader.AddChild("FBatchShortRestTime", new LocaleValue("批量调休短休时长", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                //reportHeader.AddChild("FEdtTime", new LocaleValue("更新时间", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                //reportHeader.AddChild("FCrtTime", new LocaleValue("创建时间", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                //reportHeader.AddChild("FIsUpload", new LocaleValue("是否更新到金蝶云", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                //reportHeader.AddChild("FUploadTime", new LocaleValue("更新到金蝶云的时间", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
            }
            else
            {
                reportHeader.AddChild("FJobNumber", new LocaleValue("工号", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FUserName", new LocaleValue("姓名", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FEmployeeType", new LocaleValue("人员类型", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FDeptName", new LocaleValue("部门", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FGroupName", new LocaleValue("所属考勤组", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FTitle", new LocaleValue("职位", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FWorkTime", new LocaleValue("打卡时长(小时)", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FRealWorkTime", new LocaleValue("实际出勤时长(小时)", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FWorkCount", new LocaleValue("应出勤天数", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FRealWorkCount", new LocaleValue("实际出勤天数", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FLateTime", new LocaleValue("迟到时长(小时)", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FLateCount", new LocaleValue("迟到次数", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FAbsentTime", new LocaleValue("旷工时长(小时)", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FAbsentCount", new LocaleValue("旷工次数", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FLeaveEarly", new LocaleValue("早退时长(小时)", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FLeaveEarlyCount", new LocaleValue("早退次数", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FLeaveTime", new LocaleValue("请假时长(小时)", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FLeaveCount", new LocaleValue("请假次数", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FOverClockTime", new LocaleValue("休息打卡时长(小时)", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FOvertime", new LocaleValue("加班时长(小时)", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FWorkDayOverTime", new LocaleValue("工作日加班(小时)", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FWeekEndOverTime", new LocaleValue("休息日加班(小时)", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FHolidayOverTime", new LocaleValue("节假日加班(小时)", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FPaidLeaveBalance", new LocaleValue("休息日加班余额(小时)", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FShortRestTime", new LocaleValue("短休(小时)", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FFuneralLeaveTime", new LocaleValue("丧假(小时)", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FMarriageLeaveTime", new LocaleValue("婚假(小时)", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FInjuryLeaveTime", new LocaleValue("工伤假(小时)", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FPublicLeaveTime", new LocaleValue("公假(小时)", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FExchangeLeaveTime", new LocaleValue("调休假(小时)", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FPersonalLeaveTime", new LocaleValue("事假(小时)", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FSickLeaveTime", new LocaleValue("病假(小时)", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FPaternityLeaveTime", new LocaleValue("陪产假(小时)", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FMaternityLeaveTime", new LocaleValue("产假(小时)", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FInspectionLeaveTime", new LocaleValue("产检假(小时)", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FBreastLeaveTime", new LocaleValue("哺乳假(小时)", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FYearLeaveTime", new LocaleValue("年休假(小时)", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FSmallMaternityLeave", new LocaleValue("小产假(小时)", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                reportHeader.AddChild("FYearBalance", new LocaleValue("当前年假余额(小时)",                 this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                //reportHeader.AddChild("FWeekType", new LocaleValue("工作日休息打卡时长(小时)",         this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                //reportHeader.AddChild("FAcross",  new LocaleValue("休息日休息打卡时长(小时)",         this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                //reportHeader.AddChild("FDDOverTime", new LocaleValue("节假日休息打卡时长(小时)",                 this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);                         
                //reportHeader.AddChild("FTSReturnTime", new LocaleValue("特殊调休换班总时长", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                //reportHeader.AddChild("FTSDeduTime", new LocaleValue("特殊年休扣减时长", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                //reportHeader.AddChild("FTSAddTime", new LocaleValue("特殊调休增加时长", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                //reportHeader.AddChild("FTSShortRestTime", new LocaleValue("特殊短休合计时长", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                //reportHeader.AddChild("FBatchShortRestTime", new LocaleValue("批量调休短休时长", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                //reportHeader.AddChild("FEdtTime", new LocaleValue("更新时间", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                //reportHeader.AddChild("FCrtTime", new LocaleValue("创建时间", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                //reportHeader.AddChild("FIsUpload", new LocaleValue("是否更新到金蝶云", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
                //reportHeader.AddChild("FUploadTime", new LocaleValue("更新到金蝶云的时间", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
            }
            return reportHeader;
        }

        /**
         * 构造取数sql
         * */
        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            DynamicObject customFilter = filter.FilterParameter.CustomFilter;
            //考勤范围
            if (Convert.ToString(customFilter["F_260_KQFW"]) == "1")
            {
                //日报接口
                //查询
                String F_260_CX = "";
                if (customFilter["F_260_CX"] != null && !String.IsNullOrEmpty(customFilter["F_260_CX"].ToString()))
                {
                    F_260_CX = Convert.ToString(customFilter["F_260_CX"]);
                }
                JObject rb = new JObject();
                rb.Add("PageIndex", 1);
                rb.Add("PageSize", int.MaxValue);
                JObject datas = new JObject();
                datas.Add("StartTime", Convert.ToString(customFilter["F_260_KQKSRQ"]));
                datas.Add("EndTime", Convert.ToString(customFilter["F_260_KQKSRQ"]));
                datas.Add("FDept", "");
                datas.Add("FScope", Convert.ToString(customFilter["F_260_RYFW"]));
                datas.Add("KeyWord", Convert.ToString(customFilter["F_260_CX"]));
                rb.Add("Data", datas);

                Results.GetToken message = POST.HttpPost(rb.ToString(), "Http://222.210.102.21:8888/Api/UserAttendByDay");
                if (message.Code == 5000)
                {
                    string cresql = $@"create table {tableName}(
                        XH int, FAttendanceState VARCHAR(100), FDateType VARCHAR(100), FWeekType VARCHAR(100)           
                 , FDeptID VARCHAR(100), FTitle VARCHAR(100) , FDate VARCHAR(100) , FStartWorkTime VARCHAR(100)
                 , FEndWorkTime VARCHAR(100), FAcross int, FDDOverTime VARCHAR(100), FDDWorkDayOverTime VARCHAR(100)
                 , FDDWeekEndOverTime VARCHAR(100), FDDHolidayOverTime VARCHAR(100), FOnDutyTime VARCHAR(100), FOffDutyTime VARCHAR(100)
                 , FAttendanceResult VARCHAR(100), FOverClockTime VARCHAR(100), FWorkTime VARCHAR(100), FRealWorkTime VARCHAR(100), FWorkCount VARCHAR(100)
                 , FRealWorkCount VARCHAR(100), FLateTime VARCHAR(100), FLateCount VARCHAR(100), FAbsentCount VARCHAR(100), FAbsentTime VARCHAR(100)
                 , FLeaveEarly VARCHAR(100), FLeaveEarlyCount VARCHAR(100), FLeaveTime VARCHAR(100), FLeaveCount VARCHAR(100), FOvertime VARCHAR(100)
                 , FWorkDayOverTime VARCHAR(100), FWeekEndOverTime VARCHAR(100), FHolidayOverTime VARCHAR(100), FShortRestTime VARCHAR(100)
                 , FFuneralLeaveTime VARCHAR(100), FMarriageLeaveTime VARCHAR(100), FInjuryLeaveTime VARCHAR(100), FPublicLeaveTime VARCHAR(100)
                 , FExchangeLeaveTime VARCHAR(100), FPersonalLeaveTime VARCHAR(100), FSickLeaveTime VARCHAR(100), FPaternityLeaveTime VARCHAR(100)
                 , FMaternityLeaveTime VARCHAR(100), FInspectionLeaveTime VARCHAR(100), FBreastLeaveTime VARCHAR(100), FYearLeaveTime VARCHAR(100), FTSReturnTime VARCHAR(100)
                 , FTSDeduTime VARCHAR(100), FTSAddTime VARCHAR(100), FTSShortRestTime VARCHAR(100), FBatchShortRestTime VARCHAR(100), FEdtTime VARCHAR(100)
                 , FCrtTime VARCHAR(100), FIsUpload VARCHAR(100), FUploadTime VARCHAR(100)  , FSmallMaternityLeave VARCHAR(100) , FUserName VARCHAR(100)
                 , FJobNumber VARCHAR(100), FDeptName VARCHAR(100), FGroupName VARCHAR(100), FClassName VARCHAR(100)
                 , FEmployeeType VARCHAR(100))";
                    DBUtils.Execute(Context, cresql);
                    List<Rootobject> date = message.Data.DataList;
                    int XH = 1;
                    foreach (var aa in date)
                    {
                       String sql = $@"/*dialect*/ 
                           insert into  {tableName} values ('{XH}','{aa.FAttendanceState}','{aa.FDateType}','{aa.FWeekType}','{aa.FDeptID}','{aa.FTitle}','{aa.FDate}'
                          ,'{aa.FStartWorkTime}','{aa.FEndWorkTime}','{aa.FAcross}','{aa.FDDOverTime}','{aa.FDDWorkDayOverTime}','{aa.FDDWeekEndOverTime}','{aa.FDDHolidayOverTime}'
                          ,'{aa.FOnDutyTime}','{aa.FOffDutyTime}','{aa.FAttendanceResult}','{aa.FOverClockTime}','{aa.FWorkTime}','{aa.FRealWorkTime}','{aa.FWorkCount}','{aa.FRealWorkCount}','{aa.FLateTime}','{aa.FLateCount}'
                         ,'{aa.FAbsentCount}','{aa.FAbsentTime}','{aa.FLeaveEarly}','{aa.FLeaveEarlyCount}','{aa.FLeaveTime}','{aa.FLeaveCount}','{aa.FOvertime}','{aa.FWorkDayOverTime}'
                         ,'{aa.FWeekEndOverTime}','{aa.FHolidayOverTime}','{aa.FShortRestTime}','{aa.FFuneralLeaveTime}','{aa.FMarriageLeaveTime}','{aa.FInjuryLeaveTime}','{aa.FPublicLeaveTime}'
                         ,'{aa.FExchangeLeaveTime}','{aa.FPersonalLeaveTime}','{aa.FSickLeaveTime}','{aa.FPaternityLeaveTime}','{aa.FMaternityLeaveTime}','{aa.FInspectionLeaveTime}'
                         ,'{aa.FBreastLeaveTime}','{aa.FYearLeaveTime}','{aa.FTSReturnTime}','{aa.FTSDeduTime}','{aa.FTSAddTime}','{aa.FTSShortRestTime}','{aa.FBatchShortRestTime}'
                         ,'{aa.FEdtTime}','{aa.FCrtTime}','{aa.FIsUpload}','{aa.FUploadTime}','{aa.FSmallMaternityLeave}','{aa.FUserName}','{aa.FJobNumber}','{aa.FDeptName}'
                         ,'{aa.FGroupName}','{aa.FClassName}','{aa.FEmployeeType}')";
                          DBUtils.ExecuteDynamicObject(this.Context, sql);
                        XH++;
                    }
                }
                else
                {
                    throw new KDBusinessException("", "" + message.Msg + "");
                }             
            }
            else
            {
                //月报接口
                //查询
                String F_260_CX = "";
                if (customFilter["F_260_CX"] != null && !String.IsNullOrEmpty(customFilter["F_260_CX"].ToString()))
                {
                    F_260_CX = Convert.ToString(customFilter["F_260_CX"]);
                }
                JObject rb = new JObject();
                rb.Add("PageIndex", 1);
                rb.Add("PageSize", int.MaxValue);
                JObject datas = new JObject();
                datas.Add("StartTime", Convert.ToString(customFilter["F_260_KQKSRQ"]));
                datas.Add("EndTime", Convert.ToString(customFilter["F_260_KQJZRQ"]));
                datas.Add("FDept", "");
                datas.Add("FScope", Convert.ToString(customFilter["F_260_RYFW"]));
                datas.Add("KeyWord", Convert.ToString(customFilter["F_260_CX"]));
                rb.Add("Data", datas);

                Results.GetToken1 message = POST.HttpPost1(rb.ToString(), "Http://222.210.102.21:8888/Api/UserAttendByMonth");
                if (message.Code == 5000)
                {
                    string cresql = $@"create table {tableName}(
                        XH int, FJobNumber varchar(100)
                    ,FUserName varchar(100),FEmployeeType  varchar(100),FDeptName varchar(100),FGroupName varchar(100),
                    FTitle varchar(100),FWorkTime varchar(100),FRealWorkTime varchar(100),FWorkCount varchar(100),
                    FRealWorkCount varchar(100),FLateTime varchar(100),FLateCount varchar(100),FAbsentTime varchar(100),
                    FAbsentCount varchar(100),FLeaveEarly varchar(100),FLeaveEarlyCount varchar(100),FLeaveTime varchar(100),
                    FLeaveCount varchar(100),FOverClockTime varchar(100),FOvertime varchar(100),FWorkDayOverTime varchar(100),
                    FWeekEndOverTime varchar(100),FHolidayOverTime varchar(100),FPaidLeaveBalance varchar(100),FShortRestTime varchar(100),
                    FFuneralLeaveTime varchar(100),FMarriageLeaveTime varchar(100),FInjuryLeaveTime varchar(100),FPublicLeaveTime varchar(100),
                    FExchangeLeaveTime varchar(100),FPersonalLeaveTime varchar(100),FSickLeaveTime varchar(100),FPaternityLeaveTime varchar(100),
                    FMaternityLeaveTime varchar(100),FInspectionLeaveTime varchar(100),FBreastLeaveTime varchar(100),FYearLeaveTime varchar(100),
                    FYearBalance varchar(100),FSmallMaternityLeave varchar(100))";
                    DBUtils.Execute(Context, cresql);
                    List<Rootobject1> date = message.Data.DataList;
                    int XH = 1;
                    foreach (var aa in date)
                    {
                        String sql = $@"/*dialect*/ 
                           insert into  {tableName} values ('{XH}','{aa.FJobNumber}'
                          ,'{aa.FUserName}','{aa.FEmployeeType}','{aa.FDeptName}','{aa.FGroupName}','{aa.FTitle}'
                          ,'{aa.FWorkTime}','{aa.FRealWorkTime}','{aa.FWorkCount}','{aa.FRealWorkCount}','{aa.FLateTime}','{aa.FLateCount}'
                          ,'{aa.FAbsentTime}','{aa.FAbsentCount}','{aa.FLeaveEarly}','{aa.FLeaveEarlyCount}'
                          ,'{aa.FLeaveTime}','{aa.FLeaveCount}','{aa.FOverClockTime}','{aa.FOvertime}','{aa.FWorkDayOverTime}'
                          ,'{aa.FWeekEndOverTime}','{aa.FHolidayOverTime}','{aa.FPaidLeaveBalance}','{aa.FShortRestTime}','{aa.FFuneralLeaveTime}'
                          ,'{aa.FMarriageLeaveTime}','{aa.FInjuryLeaveTime}','{aa.FPublicLeaveTime}','{aa.FExchangeLeaveTime}'
                          ,'{aa.FPersonalLeaveTime}','{aa.FSickLeaveTime}','{aa.FPaternityLeaveTime}','{aa.FMaternityLeaveTime}'
                          ,'{aa.FInspectionLeaveTime}','{aa.FBreastLeaveTime}','{aa.FYearLeaveTime}','{aa.FYearBalance}','{aa.FSmallMaternityLeave}')";
                        DBUtils.ExecuteDynamicObject(this.Context, sql);
                        XH++;
                    }
                }
                else
                {
                    throw new KDBusinessException("", ""+ message.Msg +"");
                }              
            }
            tempTableName = tableName;
        }

        /**
         * 设置汇总列信息
         * */
        public override List<SummaryField> GetSummaryColumnInfo(IRptParams filter)
        {
            var result = base.GetSummaryColumnInfo(filter);
          //  result.Add(new SummaryField("FTAXAMOUNT", Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM));
            return result;
        }

        /**
        * 删除临时表
        * */
        public override void CloseReport()
        {
            base.CloseReport();
            Boolean flag = DBUtils.IsExistTable(this.Context, tempTableName);
            String[] tempName = { tempTableName };
            IDBService dbService = Kingdee.BOS.App.ServiceHelper.GetService<Kingdee.BOS.Contracts.IDBService>();
            if (flag)
            {
                dbService.DeleteTemporaryTableName(this.Context, tempName);
            }
        }
    }
}

