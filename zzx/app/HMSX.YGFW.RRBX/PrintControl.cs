using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Const;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.NotePrint;
using Kingdee.BOS.JSON;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace HMSX.YGFW.RRBX
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("费用报销打印控制")]
    public class PrintControl : AbstractBillPlugIn
    {
        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);
            long orid = this.Context.CurrentOrganizationInfo.ID;
            if (e.BarItemKey.Equals("PAEZ_tbBD") && orid==100026)
            {
                string billId = this.View.Model.DataObject["Id"].ToString();//表单ID
                string sqfk = this.View.Model.GetValue("FRequestType").ToString();                
                string wllx = this.View.Model.GetValue("FCONTACTUNITTYPE").ToString();
                int dycs = Convert.ToInt32(this.View.Model.GetValue("F_260_PrintTimes"));
                if(sqfk=="1" && wllx == "BD_Supplier" && dycs == 1)
                {
                    PrintAction(billId);
                }
                else { this.View.ShowErrMessage("不满足补打条件！"); }
            }
        }
        private void PrintAction(string billId)
        {           
            string tdmbId = "81d0f13a-d217-4c83-af32-303fb06c6a72";//套打模板ID
            string typeId = "000ffecf2c6f97f311e32b0998d51004";//单据类型ID
            List<PrintJobItem> printList = new List<PrintJobItem>();
            PrintJobItem newItem = new PrintJobItem(billId, tdmbId, typeId);
            printList.Add(newItem);
            PrintJob pJob = new PrintJob();
            pJob.Id = Guid.NewGuid().ToString();
            pJob.FormId = this.View.BillBusinessInfo.GetForm().Id;
            pJob.PrintJobItems = printList;
            List<PrintJob> jobs = new List<PrintJob> { pJob };
            string key= Guid.NewGuid().ToString();
            this.View.Session[key] = jobs;
            //打印
            JSONObject jsonObj = new JSONObject();
            jsonObj.Put("pageID",this.View.PageId);
            jsonObj.Put("printJobId", key);
            jsonObj.Put("action", "print");
            string action = JSAction.print;
            this.View.AddAction(action, jsonObj);
        }
    }
}
