using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.K3.PLM.CFG.Business.PlugIn.BaseObject;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace HMSX.PLM.WDGL
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("文档归档更新物料版本号-列表")]
    public class ListGDGXBBH:BaseObjectList
    {
        public override void AfterDoOperation(AfterDoOperationEventArgs e)
        {
            base.AfterDoOperation(e);
            long orid = this.Context.CurrentOrganizationInfo.ID;
            if (e.OperationResult!=null && e.OperationResult.IsSuccess && orid == 100026)
            {               
                //检入
                if (e.Operation.Operation == "OpCheckIn")
                {
                    ListSelectedRowCollection listcoll = this.ListView.SelectedRowsInfo;           
                    var dycoll = listcoll.Select(d => ((DynamicObjectDataRow)d.DataRow).DynamicObject);
                    foreach (DynamicObject obj in dycoll)
                    {
                        long ywlx = Convert.ToInt64(obj["FCategoryID_Id"]);
                        string zt = obj["FLifeCircleStage"].ToString();
                        if (zt == "AC" && (ywlx == 1020115000000000000 || ywlx == 1020101000000000000)) { updatebbh(ywlx, obj); }
                    }
                }
                //归档
                else if (e.Operation.Operation == "PLMOP_1054_AC")
                {
                    ListSelectedRowCollection listcoll = this.ListView.SelectedRowsInfo;                    
                    DynamicObjectCollection dycoll = this.ListModel.GetData(listcoll);
                    foreach (DynamicObject obj in dycoll)
                    {
                        long ywlx = Convert.ToInt64(obj["FCategoryID_Id"]);                        
                        if (ywlx == 1020115000000000000 || ywlx == 1020101000000000000) { updatebbh(ywlx, obj); }
                    }
                }
            }
        }
        private void updatebbh(long ywlx, DynamicObject obj)
        {
            List<string> cps = new List<string>();
            string bbh = obj["FVerNO"].ToString();
            //截取名称产品信息
            string name = obj["FName"].ToString();
            try { cps.Add("260.02." + name.Substring(7, 5)); } catch { }
            try { cps.Add("260.02." + name.Substring(13, 5)); } catch { }
            //截取试用产品信息
            string sycp = obj["F_260_SYCP"].ToString();
            string[] cp = sycp.Split('&');
            foreach (string number in cp)
            {
                string wl;
                try { wl = "260.02." + number.Substring(number.Length - 5); } catch { continue; }
                if (!cps.Contains(wl)) { cps.Add(wl); }
            }
            //获取相关对象产品信息               
            string xgsql = @"/*dialect*/select cp.FCODE from T_PLM_CFG_RELATEDOBJECT xg
                            inner join T_PLM_PDM_BASE cp on xg.FRELATEDOBJECT = cp.FID and cp.FCATEGORYID = 1010100000000000000
                            where xg.FID =" + obj["FID"];
            DynamicObjectCollection xgobjs = DBUtils.ExecuteDynamicObject(this.Context, xgsql);
            foreach (DynamicObject xgobj in xgobjs)
            {
                string wl = xgobj["FCODE"].ToString();
                if (!cps.Contains(wl)) { cps.Add(wl); }
            }
            //客户图更新客户版本号
            if (ywlx == 1020115000000000000 && name.Contains(".pdf"))
            {
                foreach (string cpnumber in cps)
                {
                    string khsql = $"/*dialect*/update T_BD_MATERIAL set F_260_KHWLBB='{bbh}' where FNUMBER='{cpnumber}'";
                    DBUtils.Execute(this.Context, khsql);
                }
            }
            //成品图更新内部版本号
            else if (ywlx == 1020101000000000000)
            {
                foreach (string cpnumber in cps)
                {
                    string nbsql = $"/*dialect*/update T_BD_MATERIAL set F_260_TEXTBBH='{bbh}' where FNUMBER='{cpnumber}'";
                    DBUtils.Execute(this.Context, nbsql);
                }
            }
        }
    }
}
