using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace HMSX.PLM.WDGL
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("文件归档更新物料版本号")]
    public class GDGXBBH:AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            e.FieldKeys.Add("FBaseCode");
        }
        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);
            foreach(DynamicObject entity in e.DataEntitys)
            {
                if (entity != null)
                {
                    DynamicObjectCollection entrys = (DynamicObjectCollection)entity["PLMWFBaseObjectBillEntry"];
                    foreach(DynamicObject entry in entrys)
                    {
                        DynamicObject doc = (DynamicObject)entry["BaseCode"];
                        string bbh = doc["VerNO"].ToString();
                        long ywlx = Convert.ToInt64(doc["CategoryID_Id"]);
                        if(ywlx == 1020115000000000000 || ywlx == 1020101000000000000)
                        {
                            List<string> cps = new List<string>();
                            //截取名称产品信息
                            string name = doc["Name"].ToString();
                            try { cps.Add("260.02." + name.Substring(7, 5)); } catch { }
                            try { cps.Add("260.02." + name.Substring(13, 5)); } catch { }
                            //截取试用产品信息
                            string sycp = doc["F_260_SYCP"].ToString();
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
                            where xg.FID =" + doc["Id"];
                            DynamicObjectCollection xgobjs = DBUtils.ExecuteDynamicObject(this.Context, xgsql);
                            foreach (DynamicObject xgobj in xgobjs)
                            {
                                string wl = xgobj["FCODE"].ToString();
                                if (!cps.Contains(wl)) { cps.Add(wl); }
                            }
                            //客户图更新客户版本号
                            if (ywlx == 1020115000000000000 && (name.Contains(".pdf")||name.Contains(".PDF")))
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
            }
        }
    }
}
