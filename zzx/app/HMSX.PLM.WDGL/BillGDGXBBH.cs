using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.K3.PLM.CFG.Business.PlugIn.BaseObject;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace HMSX.PLM.WDGL
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("文档归档更新物料版本号-表单")]
    public class BillGDGXBBH:BaseObjectBill
    {
        public override void AfterDoOperation(AfterDoOperationEventArgs e)
        {
            base.AfterDoOperation(e);
            long orid = this.Context.CurrentOrganizationInfo.ID;
            if(e.OperationResult != null && e.OperationResult.IsSuccess && orid == 100026)
            {
                //检入
                if (e.Operation.Operation == "OpCheckIn")
                {
                    string bbh = this.View.Model.GetValue("FVerNO").ToString();
                    long ywlx = Convert.ToInt64(this.View.Model.DataObject["CategoryID_Id"]);
                    string zt = this.View.Model.GetValue("FLifeCircleStage").ToString();
                    if (zt == "AC" && (ywlx== 1020115000000000000|| ywlx == 1020101000000000000)) { updatebbh(ywlx, bbh); }                   
                }
                //归档
                else if(e.Operation.Operation == "PLMOP_1054_AC")
                {
                    string bbh = this.View.Model.GetValue("FVerNO").ToString();
                    long ywlx = Convert.ToInt64(this.View.Model.DataObject["CategoryID_Id"]);
                    if (ywlx == 1020115000000000000 || ywlx == 1020101000000000000) { updatebbh(ywlx, bbh); }
                }
            }
        }
        private void updatebbh(long ywlx,string bbh)
        {
            List<string> cps = new List<string>();
            //截取名称产品信息
            string name = this.View.Model.GetValue("FName").ToString();
            try { cps.Add("260.02." + name.Substring(7, 5)); } catch { }
            try { cps.Add("260.02." + name.Substring(13, 5)); } catch { }
            //截取试用产品信息
            string sycp = this.View.Model.GetValue("F_260_SYCP").ToString();
            string[] cp = sycp.Split('&');
            foreach (string number in cp)
            {
                string wl;
                try{wl = "260.02." + number.Substring(number.Length - 5);}catch { continue; }
                if (!cps.Contains(wl)){cps.Add(wl);}
            }
            //获取相关对象产品信息               
            string xgsql = @"/*dialect*/select cp.FCODE from T_PLM_CFG_RELATEDOBJECT xg
                            inner join T_PLM_PDM_BASE cp on xg.FRELATEDOBJECT = cp.FID and cp.FCATEGORYID = 1010100000000000000
                            where xg.FID =" + this.View.Model.DataObject["Id"];
            DynamicObjectCollection xgobjs = DBUtils.ExecuteDynamicObject(this.Context, xgsql);
            foreach (DynamicObject xgobj in xgobjs)
            {
                string wl = xgobj["FCODE"].ToString();
                if (!cps.Contains(wl)){cps.Add(wl);}
            }
            //客户图更新客户版本号
            if (ywlx == 1020115000000000000 && (name.Contains(".pdf") || name.Contains(".PDF")))
            {
                foreach (string cpnumber in cps)
                {
                    string khsql = $"/*dialect*/update T_BD_MATERIAL set F_260_KHWLBB='{bbh}' where FNUMBER like '{cpnumber}%'";
                    DBUtils.Execute(this.Context, khsql);
                }
            }
            //成品图更新内部版本号
            else if (ywlx == 1020101000000000000)
            {
                foreach (string cpnumber in cps)
                {
                    string nbsql = $"/*dialect*/update T_BD_MATERIAL set F_260_TEXTBBH='{bbh}' where FNUMBER like '{cpnumber}%'";
                    DBUtils.Execute(this.Context, nbsql);
                }
            }
        }
    }
}
