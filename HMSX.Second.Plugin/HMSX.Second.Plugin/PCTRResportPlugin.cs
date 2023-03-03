using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Contracts.Report;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Report;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace HMSX.Second.Plugin
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("批次投入产出率")]
    public class PCTRResportPlugin : SysReportBaseService
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
            this.ReportProperty.IdentityFieldName = "FIDENTITYID";
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
                String WL = customFilter["F_PAEZ_WL"] == null ? String.Empty : Convert.ToString(((DynamicObject)customFilter["F_PAEZ_WL"])["Id"]);
                String PC = customFilter["F_PAEZ_PC"] == null ? String.Empty : Convert.ToString(((DynamicObject)customFilter["F_PAEZ_PC"])["Number"]);
                String CJ = customFilter["F_PAEZ_SCCJ"] == null ? String.Empty : Convert.ToString(((DynamicObject)customFilter["F_PAEZ_SCCJ"])["Id"]);
                String SCDD = customFilter["F_PAEZ_SCDD"] == null ? String.Empty : Convert.ToString(customFilter["F_PAEZ_SCDD"]);
                String PGID = customFilter["F_PAEZ_PGID"] == null ? String.Empty : Convert.ToString(customFilter["F_PAEZ_PGID"]);
                String GXJH = customFilter["F_PAEZ_GXJH"] == null ? String.Empty : Convert.ToString(customFilter["F_PAEZ_GXJH"]);
                String KS = customFilter["F_PAEZ_SD"] == null ? String.Empty : Convert.ToString(customFilter["F_PAEZ_SD"]);
                String JS = customFilter["F_PAEZ_ED"] == null ? String.Empty : Convert.ToString(customFilter["F_PAEZ_ED"]);
               //reprotTitles.AddTitle("WL", WL);
               //reprotTitles.AddTitle("PC", PC);
               //reprotTitles.AddTitle("CJ", CJ);
               //reprotTitles.AddTitle("SCDD", SCDD);
               //reprotTitles.AddTitle("PGID", PGID);
               //reprotTitles.AddTitle("GXJH", GXJH);
               //reprotTitles.AddTitle("KS", KS);
               //reprotTitles.AddTitle("JS", JS);
            }
            return reprotTitles;
        }

        /**
         * 设置单据列
         **/
        public override ReportHeader GetReportHeaders(IRptParams filter)
        {
            ReportHeader reportHeader = new ReportHeader();
            //设置列
            reportHeader.AddChild("FBILLNO", new LocaleValue("生产订单", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
            reportHeader.AddChild("FSEQ", new LocaleValue("行号", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
            reportHeader.AddChild("CJFNAME", new LocaleValue("生产车间", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
            reportHeader.AddChild("BZSL", new LocaleValue("订单数量", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
            reportHeader.AddChild("GXJH", new LocaleValue("工序计划", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
            reportHeader.AddChild("BOMFNUMBER", new LocaleValue("BOM父项编码", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
            reportHeader.AddChild("BOMFNAME", new LocaleValue("BOM父项名称", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
            reportHeader.AddChild("FDENOMINATOR", new LocaleValue("分母", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
            reportHeader.AddChild("BOMWLFNUMBER", new LocaleValue("BOM物料编码", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
            reportHeader.AddChild("BOMWLFNAME", new LocaleValue("BOM物料名称", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
            reportHeader.AddChild("FNUMERATOR", new LocaleValue("分子", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
            reportHeader.AddChild("WLFNUMBER", new LocaleValue("领料单物料编码", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
            reportHeader.AddChild("WLFNAME", new LocaleValue("领料单物料名称", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
            reportHeader.AddChild("LLSL", new LocaleValue("领料数", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
            reportHeader.AddChild("BZYLSL", new LocaleValue("标准用料数", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
            reportHeader.AddChild("FLOT", new LocaleValue("汇报批次", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
            reportHeader.AddChild("PGID", new LocaleValue("派工ID", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
            reportHeader.AddChild("RKSL", new LocaleValue("入库数量", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
            reportHeader.AddChild("CCL", new LocaleValue("产出率", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
            reportHeader.AddChild("HBSL", new LocaleValue("汇报数量", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
            reportHeader.AddChild("DWFNAME", new LocaleValue("单位", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
            reportHeader.AddChild("PGSL", new LocaleValue("派工数量", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
            reportHeader.AddChild("FLOT_TEXT", new LocaleValue("领料批次", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
            reportHeader.AddChild("FDATE", new LocaleValue("领料日期", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
            reportHeader.AddChild("RKFDATE", new LocaleValue("入库日期", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);          
            return reportHeader;
        }

        /**
         * 构造取数sql
         * */
        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            DynamicObject customFilter = filter.FilterParameter.CustomFilter;
            String wl = "";
            if (customFilter["F_PAEZ_WL"] != null && !String.IsNullOrEmpty(customFilter["F_PAEZ_WL"].ToString()))
            {
                wl = Convert.ToString(((DynamicObject)customFilter["F_PAEZ_WL"])["Id"]);
            }
            String pc = "";
            if (customFilter["F_PAEZ_PC"] != null && ((DynamicObject)customFilter["F_PAEZ_PC"])["Number"] != null)
            {
                pc = Convert.ToString(((DynamicObject)customFilter["F_PAEZ_PC"])["Id"]);
            }
            String sccj = "";
            if (customFilter["F_PAEZ_SCCJ"] != null && ((DynamicObject)customFilter["F_PAEZ_SCCJ"])["NUMBER"] != null)
            {
                sccj = Convert.ToString(((DynamicObject)customFilter["F_PAEZ_SCCJ"])["Id"]);
            }
            String scdd = "";
            if (customFilter["F_PAEZ_SCDD"] != null && customFilter["F_PAEZ_SCDD"] != null)
            {
                scdd = Convert.ToString(customFilter["F_PAEZ_SCDD"]);
            }
            String gxjh = "";
            if (customFilter["F_PAEZ_GXJH"] != null && customFilter["F_PAEZ_GXJH"] != null)
            {
                gxjh = Convert.ToString(customFilter["F_PAEZ_GXJH"]);
            }
            String pgid = "";
            if (customFilter["F_PAEZ_PGID"] != null && customFilter["F_PAEZ_PGID"] != null)
            {
                pgid = Convert.ToString(customFilter["F_PAEZ_PGID"]);
            }
            DateTime kssj = Convert.ToDateTime(customFilter["F_PAEZ_SD"]);
            DateTime jssj = Convert.ToDateTime(customFilter["F_PAEZ_ED"]);



            String sql = $@"/*dialect*/ 
                       exec HMSX_260_PHTRCCLBB '{wl}','{pc}','{sccj}','{scdd}','{gxjh}','{pgid}','{kssj}','{jssj}','{tableName}'
                    ";
            DBUtils.ExecuteDynamicObject(this.Context, sql);
            tempTableName = tableName;
        }

        /**
         * 设置汇总列信息
         * */
        public override List<SummaryField> GetSummaryColumnInfo(IRptParams filter)
        {
            var result = base.GetSummaryColumnInfo(filter);
            //  result.Add(new SummaryField("FTAXAMOUNT", Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM));
           // result.Add(new SummaryField("FAMOUNT", Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM));
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

