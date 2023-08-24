using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.CommonFilter;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.生产制造
{
    [Description("工序转移")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class GXZYDBillePlugin: AbstractBillPlugIn
    {
        public override void BeforeF7Select(BeforeF7SelectEventArgs e)
        {
            base.BeforeF7Select(e);
            
        }
        public override void AfterEntryBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterEntryBarItemClick(e);
            if (e.BarItemKey.Equals("XD"))
            {
                ListShowParameter listShowParameter = new ListShowParameter();
                //FormId你要调用那个单据的列表,通过打开未扩展的销售订单,找到唯一标识     
                listShowParameter.FormId = "SFC_OperationReport";
                //IsLookUp弹出的列表界面是否有“返回数据”按钮
                listShowParameter.IsLookUp = true;
                var gxjh = this.Model.GetValue("FOUTOPBILLNO") == null ? "" : this.Model.GetValue("FOUTOPBILLNO").ToString();
                var xlh = this.Model.GetValue("FOUTSEQNUMBER") == null ? "" : this.Model.GetValue("FOUTSEQNUMBER").ToString();
                var gx = this.Model.GetValue("FOUTOPERNUMBER") == null ? "" : this.Model.GetValue("FOUTOPERNUMBER").ToString();
                if (gxjh != "" && xlh != "" && gx != "")
                {
                    string id = "";
                    string Fil = "FOPTPLANNO='" + gxjh + "' and FSEQNUMBER='" + xlh + "' and FOPERNUMBER='" + gx + "'";
                    string gxzysql = $@"select b.* from T_SFC_OPERATIONTRANSFER_a a
                                        inner join T_260_PGMXEntry b on a.fid=b.fid
                                        where FOUTOPBILLNO='{gxjh}' and FOUTSEQNUMBER='{xlh}' and FOUTOPERNUMBER='{gx}'";
                    var gxzys = DBUtils.ExecuteDynamicObject(Context, gxzysql);
                    foreach(var gxzy in gxzys)
                    {
                        id += gxzy["F_260_PGID"].ToString() + ",";
                    }
                    foreach(var da in this.Model.DataObject["F_260_PGMXEntity"] as DynamicObjectCollection)
                    {
                        if (da["F_260_PGID"] != null)
                        {
                            id += da["F_260_PGID"].ToString() + ",";
                        }
                        
                    }
                    if (id != "")
                    {
                        Fil += "AND FDISPATCHDETAILENTRYID not IN (" + id.Trim(',') + ")";
                    }
                    ListRegularFilterParameter regularFilterPara = new ListRegularFilterParameter();
                    regularFilterPara.Filter = Fil; 
                    listShowParameter.ListFilterParameter = regularFilterPara;
                }
                this.View.ShowForm(listShowParameter, delegate (FormResult result)
                {
                    //读取返回值
                    object returnData = result.ReturnData;
                    if (returnData is ListSelectedRowCollection)
                    {
                        //如果是,执行,转换格式
                        ListSelectedRowCollection listSelectedRowCollection = returnData as ListSelectedRowCollection;
                        //如果不是空值,说明有返回值
                        foreach (var li in listSelectedRowCollection)
                        {
                            DynamicObjectDataRow datarow = (DynamicObjectDataRow)li.DataRow;
                            string gxhbsql = $@"select a.FENTRYID,FQUAQTY,FLOT,FDISPATCHDETAILENTRYID from T_SFC_OPTRPTENTRY a
                                             left join T_SFC_OPTRPTENTRY_b b on a.fentryid=b.fentryid
                                             left join T_SFC_OPTRPTENTRY_A C on a.fentryid=C.fentryid
                                             WHERE a.FENTRYID = {datarow.DynamicObject["t1_FENTRYID"]}";
                            var gxhbs = DBUtils.ExecuteDynamicObject(Context, gxhbsql);
                            foreach(var gxhb in gxhbs)
                            {
                                this.Model.CreateNewEntryRow("F_260_PGMXEntity");
                                int x=this.Model.GetEntryRowCount("F_260_PGMXEntity");
                                this.View.Model.SetItemValueByID("F_260_PC",Convert.ToInt64(gxhb["FLOT"]), x-1);
                                this.View.Model.SetValue("F_260_SL",Convert.ToDecimal(gxhb["FQUAQTY"]), x-1);
                                this.View.Model.SetValue("F_260_PGID", gxhb["FDISPATCHDETAILENTRYID"].ToString(), x-1);
                                this.View.Model.SetValue("F_260_HBDID", gxhb["FENTRYID"].ToString(), x - 1);
                            }
                        }
                    }
                });

            }
        }

    }
}
