using Kingdee.BOS.App.Core;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Bill.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace HMSX.GYL.CGGL
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("双新供应商变更记录")]
    public class ZXGYSBG : AbstractBillPlugIn
    {
        private string tempTable;        
        //单据打开创建临时表
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            if (this.Context.CurrentOrganizationInfo.ID == 100026 && this.View.Model.GetValue("FDocumentStatus").ToString() == "D"&& tempTable == null)
            {               
                tempTable = TemporaryTableUtil.CreateTemporaryTableName(this.Context);
                string createtable = string.Format("/*dialect*/create table {0}(frow int,fkey nvarchar(20),fname nvarchar(20),fold nvarchar(500),fnew nvarchar(500))", tempTable);
                DBUtils.Execute(this.Context, createtable);
                
            }
        }
        public override void AfterDoOperation(AfterDoOperationEventArgs e)
        {
            base.AfterDoOperation(e);           
            if(this.Context.CurrentOrganizationInfo.ID == 100026 && (e.Operation.Operation == "UnAudit" ||e.Operation.Operation== "CancelAssign") && e.ExecuteResult && tempTable == null)
            {
                tempTable = TemporaryTableUtil.CreateTemporaryTableName(this.Context);
                string createtable = string.Format("/*dialect*/create table {0}(frow int,fkey nvarchar(20),fname nvarchar(20),fold nvarchar(500),fnew nvarchar(500))", tempTable);
                DBUtils.Execute(this.Context, createtable);
            }
        }
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            try
            {
                if (this.Context.CurrentOrganizationInfo.ID == 100026 && this.View.Model.GetValue("FDocumentStatus").ToString() == "D")
                {
                    string[] entity = new string[] { "FBaseInfo", "FFinanceInfo", "FBankInfo" };
                    if (entity.Contains<string>(e.Field.Entity.Key))
                    {
                        string[] fields = new string[] { "FDeptId", "FStaffId", "FSupplierClassify" };
                        if (!fields.Contains<string>(e.Field.Key))
                        {
                            string key = e.Field.Key;//标识                       
                            string name = e.Field.ToString();//名称                                                                              
                            int row = e.Row;
                            string sql = $"/*dialect*/select * from {tempTable} where fkey='{key}' and frow={row}";//判断是否有记录
                            string usql;
                            var newvalue = e.NewValue == null ? "" : this.GetValue(key, e.NewValue.ToString());
                            var oldvalue = e.OldValue == null ? "" : this.GetValue(key, e.OldValue.ToString());
                            if (DBUtils.ExecuteDynamicObject(this.Context, sql).Count > 0)
                            {
                                usql = $"/*dialect*/update {tempTable} set fnew='{newvalue}' where fkey='{key}' and frow={row}";
                            }
                            else
                            {
                                usql = $"/*dialect*/insert into {tempTable}(frow,fkey,fname,fold,fnew) values({row},'{key}','{name}','{oldvalue}','{newvalue}')";
                            }
                            DBUtils.Execute(this.Context, usql);
                        }
                    }
                }
            }
            catch { return; }
        }
        public override void BeforeSave(BeforeSaveEventArgs e)
        {
            base.BeforeSave(e);
            try 
            {
                if (this.Context.CurrentOrganizationInfo.ID == 100026 && this.View.Model.GetValue("FDocumentStatus").ToString() == "D")
                {
                    if (this.View.Model.DataChanged)
                    {
                        this.View.Model.DeleteEntryData("F_PAEZ_Entity");
                        string sql = $"/*dialect*/select * from {tempTable} where fold!=fnew";
                        DynamicObjectCollection objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
                        int row = 0;
                        foreach (DynamicObject obj in objs)
                        {
                            this.View.Model.CreateNewEntryRow("F_PAEZ_Entity");
                            this.View.Model.SetValue("F_260_BGZDM", obj["fname"], row);//变更字段
                            this.View.Model.SetValue("F_260_BGQZ", obj["fold"], row);//变更前
                            this.View.Model.SetValue("F_260_BGHZ", obj["fnew"], row);//变更后
                            row++;
                        }
                    }
                }
            }
            catch { return; }
        }
        public override void BeforeClosed(BeforeClosedEventArgs e)
        {
            base.BeforeClosed(e);
            TemporaryTableUtil.DeleteTemporaryTableName(this.Context, new string[] { tempTable });
        }        
        private string GetValue(string key, string value)
        {
            string[] fzzl = new string[] { "FCountry", "FProvincial", "FLanguage","FTrade", "FSupplierClassify",
                "FSupplierGrade", "FCompanyClassify", "FCompanyNature", "FCompanyScale", "FGender","FBankCountry",
                "FTaxType","FTendType"};//辅助资料
            string val;
            string sql;
            if (fzzl.Contains<string>(key))
            {
                sql = $"/*dialect*/select FDATAVALUE from T_BAS_ASSISTANTDATAENTRY_L where FENTRYID='{value}'";
            }
            else if (key == "FBankTypeRec")//银行类型
            {
                sql = "/*dialect*/select FNAME from T_WB_BankType_L where FID=" + value;
            }
            else if (key == "FCustomerId")//客户
            {
                sql = "/*dialect*/select FNAME from T_BD_CUSTOMER_L where FCUSTID=" + value;
            }
            else if (key == "FPayCurrencyId")//币别
            {
                sql = "/*dialect*/select FNAME from T_BD_CURRENCY_L where FCURRENCYID=" + value;
            }
            else if (key == "FSettleTypeId")//结算方式
            {
                sql = "/*dialect*/select FNAME from T_BD_SETTLETYPE_L where FID=" + value;
            }
            else if (key == "FPayCondition")//付款条件
            {
                sql = "/*dialect*/select FNAME from T_BD_PaymentCondition_L where FID=" + value;
            }
            else if (key == "FSettleId" || key == "FChargeId")//供应商
            {
                sql = "/*dialect*/select FNAME from t_BD_Supplier_L where FSUPPLIERID=" + value;
            }
            else if (key == "FTaxRateId")//税率
            {
                sql = "/*dialect*/select FNAME from T_BD_TAXRATE_L where FID=" + value;
            }
            else if (key == "FBankDetail")//银行网点
            {
                sql = "/*dialect*/select FNAME,* from T_WB_BankDetail_L where FID=" + value;
            }
            else
            {
                sql = "";
            }
            if (sql != "")
            {
                val = DBUtils.ExecuteScalar<string>(this.Context, sql, "");
            }
            else
            {
                val = value;
            }
            return val;
        }
    }
}

