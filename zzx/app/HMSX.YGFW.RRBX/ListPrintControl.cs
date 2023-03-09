using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Const;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.List.PlugIn;
using Kingdee.BOS.Core.NotePrint;
using Kingdee.BOS.JSON;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace HMSX.YGFW.RRBX
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("费用报销列表打印控制")]
    public class ListPrintControl : AbstractListPlugIn
    {
        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);
            long orid = this.Context.CurrentOrganizationInfo.ID;
            if (e.BarItemKey.Equals("PAEZ_260BD") && orid == 100026)
            {
                ListSelectedRowCollection listcoll = this.ListView.SelectedRowsInfo;
                DynamicObjectCollection dycoll = this.ListModel.GetData(listcoll);
                string errmes = "";
                List<string> billIds = new List<string>();
                foreach (DynamicObject item in dycoll)
                {
                    string billId = item["FID"].ToString();
                    string sqfk = item["FRequestType"].ToString();
                    string wllx = item["FCONTACTUNITTYPE"].ToString();
                    int dycs = Convert.ToInt32(item["F_260_PrintTimes"]);
                    if (sqfk == "1" && wllx == "BD_Supplier" && dycs == 1)
                    {
                        if (!billIds.Contains(billId)) { billIds.Add(billId); }
                    }
                    else { errmes += item["FBillNo"] + "不满足补打条件！\n"; }
                }
                if (billIds.Count > 0) { PrintAction(billIds); }
                if (errmes != "") { this.View.ShowErrMessage(errmes); }

            }
            else if(e.BarItemKey.Equals("PAEZ_260DYCSGX") && orid == 100026)
            {
                ListSelectedRowCollection listcoll = this.ListView.SelectedRowsInfo;
                DynamicObjectCollection dycoll = this.ListModel.GetData(listcoll);
                string fids = "";
                foreach (DynamicObject item in dycoll)
                {
                    string billId = item["FID"].ToString();
                    int dycs = Convert.ToInt32(item["F_260_PrintTimes"]);
                    if (fids.Contains(billId) || dycs<=1) { continue; }
                    fids += billId + ',';
                }
                if (fids != "")
                {
                    string sql= $"/*dialect*/update t_ER_ExpenseReimb set F_260_PRINTTIMES=1 where FID in ({fids.Substring(0, fids.Length - 1)})";
                    DBUtils.Execute(this.Context, sql);
                }
                else
                {
                    this.View.ShowErrMessage("未选择表单，请选择需更新的表单");
                }
            }

        }
        private void PrintAction(List<string> billIds)
        {
            string tdmbId = "81d0f13a-d217-4c83-af32-303fb06c6a72";//套打模板ID
            string typeId = "000ffecf2c6f97f311e32b0998d51004";//单据类型ID
            List<PrintJobItem> printList = new List<PrintJobItem>();
            foreach(string billId in billIds)
            {
                PrintJobItem newItem = new PrintJobItem(billId, tdmbId, typeId);
                printList.Add(newItem);
            }           
            PrintJob pJob = new PrintJob();
            pJob.Id = Guid.NewGuid().ToString();
            pJob.FormId = this.View.BillBusinessInfo.GetForm().Id;
            pJob.PrintJobItems = printList;
            List<PrintJob> jobs = new List<PrintJob> { pJob };
            string key = Guid.NewGuid().ToString();
            this.View.Session[key] = jobs;
            //打印
            JSONObject jsonObj = new JSONObject();
            jsonObj.Put("pageID", this.View.PageId);
            jsonObj.Put("printJobId", key);
            jsonObj.Put("action", "print");
            string action = JSAction.print;
            this.View.AddAction(action, jsonObj);
        }
    }
}
