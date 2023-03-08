using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.ComponentModel;

namespace HMSX.SCZZ.CJGL.MES
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("生产进度查询")]
    public class SCJDCX:AbstractDynamicFormPlugIn
    {
        private int hs;
        //查询
        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            if(e.Key.ToUpper()== "FSELECT")
            {
                long wl = Convert.ToInt64(this.View.Model.DataObject["FCXWL_Id"]);
                //需求明细
                this.View.Model.DeleteEntryData("F_PAEZ_XQEntity");
                xqdata(wl);
                this.View.UpdateView("F_PAEZ_XQEntity");
                //生产明细
                this.View.Model.DeleteEntryData("F_PAEZ_SCJD");
                hs = 1;
                filldata(wl, 0);
                select(wl);
                this.View.UpdateView("F_PAEZ_SCJD");
            }
        }
        //行双击打开明细
        public override void EntityRowDoubleClick(EntityRowClickEventArgs e)
        {
            base.EntityRowDoubleClick(e);
            if (e.Key== "F_PAEZ_SCJD")
            {
                this.View.Model.DeleteEntryData("F_PAEZ_SCJDMX");
                long wlid = Convert.ToInt64(((DynamicObject)this.View.Model.GetValue("FWLNumber", e.Row))["Id"]);
                string mxsql = $"exec SP_260_SCJDMX {wlid}";
                DynamicObjectCollection mxobjs = DBUtils.ExecuteDynamicObject(this.Context, mxsql);
                int i = 0;
                foreach (DynamicObject mx in mxobjs)
                {
                    this.View.Model.CreateNewEntryRow("F_PAEZ_SCJDMX");
                    this.View.Model.SetValue("FDJID", mx["FID"], i);
                    this.View.Model.SetValue("FDJ", mx["单据"], i);
                    this.View.Model.SetValue("FDJBH", mx["单据编号"], i);
                    this.View.Model.SetValue("FDJHH", mx["行号"], i);
                    this.View.Model.SetValue("FDDSL", mx["订单数量"], i);
                    this.View.Model.SetValue("FRKSL1", mx["入库数量"], i);
                    this.View.Model.SetValue("FWWDDSL1", mx["未完订单"], i);
                    this.View.Model.SetValue("FPGSL1", mx["派工数量"], i);
                    this.View.Model.SetValue("FPGSLLY1", mx["派工数量2"], i);
                    this.View.Model.SetValue("FWGDJYSL", mx["完工待检验"], i);
                    this.View.Model.SetValue("FJYHGDRK1", mx["检验合格待入库"], i);
                    this.View.Model.SetValue("FBHGSL1", mx["不合格"], i);
                    i++;
                }
                this.View.UpdateView("F_PAEZ_SCJDMX");
            }
            
        }
        //超链接打开单据
        public override void EntryButtonCellClick(EntryButtonCellClickEventArgs e)
        {
            base.EntryButtonCellClick(e);
            if (e.FieldKey.Equals("FDJBH"))
            {
                string fid = this.View.Model.GetValue("FDJID",e.Row).ToString();
                string dj = this.View.Model.GetValue("FDJ", e.Row).ToString();
                string formid;
                if (dj == "生产订单"){formid = "PRD_MO";}
                else{formid = "SUB_SUBREQORDER";}                
                var showParameter = new BillShowParameter
                {
                    FormId = formid,              
                    PKey = fid,                
                    Status = OperationStatus.VIEW           
                };
                this.View.ShowForm(showParameter);
            }
        }
        //生产进度
        private void select(long wl)
        {           
            string bomsql = $@"select b.FMATERIALID 物料ID from
                    (select top 1 FID from T_ENG_BOM where FDOCUMENTSTATUS = 'C' and FFORBIDSTATUS = 'A'
                     and FMATERIALID={wl} order by FCREATEDATE DESC) a
                    inner join T_ENG_BOMCHILD b on a.FID = b.FID
                    inner join T_BD_MATERIAL wl on wl.FMATERIALID = b.FMATERIALID
                    where wl.FNUMBER like '260.03%'";
            DynamicObjectCollection bomobjs = DBUtils.ExecuteDynamicObject(this.Context, bomsql);
            int parent = hs-1;
            foreach (DynamicObject bomobj in bomobjs)
            {
                long wlid = Convert.ToInt64(bomobj["物料ID"]);
                filldata(wlid,parent);
                select(wlid);
            }
        }
        private void filldata(long wl,int parent)
        {
            string scsql = $"exec SP_260_SCJDCX {wl}";
            DynamicObjectCollection objects = DBUtils.ExecuteDynamicObject(this.Context, scsql);
            if (objects.Count > 0)
            {
                this.View.Model.CreateNewEntryRow("F_PAEZ_SCJD");
                this.View.Model.SetValue("FROWID", hs, hs - 1);
                this.View.Model.SetValue("FPARENTROWID", parent, hs - 1);
                this.View.Model.SetValue("FROWEXPANDTYPE", 16, hs - 1);
                this.View.Model.SetValue("FWLNumber", objects[0]["物料ID"], hs - 1);
                this.View.Model.SetValue("FSCDDSL", objects[0]["订单数量"], hs - 1);
                this.View.Model.SetValue("FRKSL", objects[0]["入库数量"], hs - 1);
                this.View.Model.SetValue("FWWDDS", objects[0]["未完订单"], hs - 1);
                this.View.Model.SetValue("FKYKCS", objects[0]["可用库存"], hs - 1);
                this.View.Model.SetValue("FBLPKCS", objects[0]["不可用库存"], hs - 1);
                this.View.Model.SetValue("FCJKYKC", objects[0]["车间可用库存"], hs - 1);
                this.View.Model.SetValue("FCJBKYKC", objects[0]["车间不可用库存"], hs - 1);
                this.View.Model.SetValue("FZCKYKC", objects[0]["总仓可用库存"], hs - 1);
                this.View.Model.SetValue("FZCBKYKC", objects[0]["总仓不可用库存"], hs - 1);
                this.View.Model.SetValue("FPGSL", objects[0]["派工数量"], hs - 1);
                this.View.Model.SetValue("FPGSLLY", objects[0]["派工领料"], hs - 1);
                this.View.Model.SetValue("FHBSL", objects[0]["完工数量"], hs - 1);
                this.View.Model.SetValue("FJYHGDRK", objects[0]["检验合格待入库"], hs - 1);
                this.View.Model.SetValue("FBHGSL", objects[0]["不合格数"], hs - 1);
            }
            else
            {
                this.View.Model.CreateNewEntryRow("F_PAEZ_SCJD");
                this.View.Model.SetValue("FROWID", hs, hs - 1);
                this.View.Model.SetValue("FPARENTROWID", parent, hs - 1);
                this.View.Model.SetValue("FROWEXPANDTYPE", 16, hs - 1);
                this.View.Model.SetValue("FWLNumber", wl, hs - 1);               
            }
            hs++;
        }
        //需求明细
        private void xqdata(long wl)
        {
            string xqsql = $"exec sp_260_GETXQ {wl}";
            DynamicObjectCollection xqobjs = DBUtils.ExecuteDynamicObject(this.Context, xqsql);
            int row = 0;
            foreach(DynamicObject xqobj in xqobjs)
            {
                this.View.Model.CreateNewEntryRow("F_PAEZ_XQEntity");
                this.View.Model.SetValue("FKHMC", xqobj["客户名称"],row);
                this.View.Model.SetValue("FXQWL", wl, row);
                this.View.Model.SetValue("FXMMC", xqobj["项目名称"], row);
                this.View.Model.SetValue("FSCCJ", xqobj["生产车间"], row);
                this.View.Model.SetValue("FJHY", xqobj["计划员"], row);
                this.View.Model.SetValue("FLX", xqobj["类型"], row);
                this.View.Model.SetValue("FW1", xqobj["A"], row);
                this.View.Model.SetValue("FW2", xqobj["B"], row);
                this.View.Model.SetValue("FW3", xqobj["C"], row);
                this.View.Model.SetValue("FW4", xqobj["D"], row);
                this.View.Model.SetValue("FW5", xqobj["A1"], row);
                this.View.Model.SetValue("FW6", xqobj["B1"], row);
                this.View.Model.SetValue("FW7", xqobj["C1"], row);
                this.View.Model.SetValue("FW8", xqobj["D1"], row);
                this.View.Model.SetValue("FW9", xqobj["A2"], row);
                this.View.Model.SetValue("FW10", xqobj["B2"], row);
                this.View.Model.SetValue("FW11", xqobj["C2"], row);
                this.View.Model.SetValue("FW12", xqobj["D2"], row);
                this.View.Model.SetValue("FW13", xqobj["A3"], row);
                this.View.Model.SetValue("FW14", xqobj["B3"], row);
                this.View.Model.SetValue("FW15", xqobj["C3"], row);
                this.View.Model.SetValue("FW16", xqobj["D3"], row);
                row++;
            }
        }
    }
}
