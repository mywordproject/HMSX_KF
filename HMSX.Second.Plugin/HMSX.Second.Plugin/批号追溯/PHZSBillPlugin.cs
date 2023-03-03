using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Permission;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using HMSX.Second.Plugin.Tool;
using Kingdee.BOS.Core.DynamicForm.PlugIn;

namespace HMSX.Second.Plugin.批号追溯
{
    [Description("批号追溯——单击过滤弹出对话框")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class PHZSBillPlugin : AbstractDynamicFormPlugIn
    { 
         //  int X = 0;//判断向上追溯2还是向下1
        /// <summary>
        /// </summary>
        /// <param name="e"></param>
        /**
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            DynamicFormShowParameter parameter = new DynamicFormShowParameter();
            parameter.OpenStyle.ShowType = ShowType.Floating;
            parameter.FormId = "RUJP_260_PHZSGL";
            parameter.MultiSelect = false;
            this.View.GetMainBarItem("RUJP_XS").Visible = false;
            this.View.GetMainBarItem("RUJP_XX").Visible = false;
            //获取返回的值
            this.View.ShowForm(parameter, delegate (FormResult result)
            {
                string[] date = (string[])result.ReturnData;
                if (date != null)
                {
                    this.Model.SetValue("F_260_WL", date[0]);
                    this.Model.SetValue("F_RUJP_PH", date[1]);
                    this.Model.SetValue("F_260_KSRQ", date[2]);
                    this.Model.SetValue("F_260_JSRQ", date[3]);
                    if (Convert.ToInt32(date[4]) == 1)
                    {
                        this.View.GetMainBarItem("RUJP_XX").Visible = true;
                    }
                    else
                    {
                        this.View.GetMainBarItem("RUJP_XS").Visible = true;
                    }
                    FZ(date[0], date[1], date[2], date[3], Convert.ToInt32(date[4]));
                }
            });
        }
        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);

            if (e.BarItemKey.Equals("RUJP_GL"))
            {
                DynamicFormShowParameter parameter = new DynamicFormShowParameter();
                parameter.OpenStyle.ShowType = ShowType.Floating;
                parameter.FormId = "RUJP_260_PHZSGL";
                parameter.MultiSelect = false;

                //获取返回的值
                this.View.ShowForm(parameter, delegate (FormResult result)
                {
                    string[] date = (string[])result.ReturnData;
                    if (date != null)
                    {
                        this.Model.SetValue("F_260_WL", date[0]);
                        this.Model.SetValue("F_RUJP_PH", date[1]);
                        this.Model.SetValue("F_260_KSRQ", date[2]);
                        this.Model.SetValue("F_260_JSRQ", date[3]);
                        if (Convert.ToInt32(date[4]) == 1)
                        {
                            this.View.GetMainBarItem("RUJP_XX").Visible = true;
                            this.View.GetMainBarItem("RUJP_XS").Visible = false;
                        }
                        else
                        {
                            this.View.GetMainBarItem("RUJP_XS").Visible = true;
                            this.View.GetMainBarItem("RUJP_XX").Visible = false;
                        }
                        FZ(date[0], date[1], date[2], date[3], Convert.ToInt32(date[4]));
                    }
                });
            }
            else if (e.BarItemKey.Equals("RUJP_XX"))
            {
                X = 2;
                string wl = this.Model.GetValue("F_260_WL") == null ? null : ((DynamicObject)this.Model.GetValue("F_260_WL"))["Id"].ToString();
                string ph = this.Model.GetValue("F_RUJP_PH") == null ? null : ((DynamicObject)this.Model.GetValue("F_RUJP_PH"))["Id"].ToString();
                string ksrq = this.Model.GetValue("F_260_KSRQ").ToString();
                string jsrq = this.Model.GetValue("F_260_JSRQ").ToString();
                if (wl != null && ph != null)
                {
                    FZ(wl, ph, ksrq, ksrq, X);
                }
            }
            else if (e.BarItemKey.Equals("RUJP_XS"))
            {
                X = 1;
                string wl = this.Model.GetValue("F_260_WL") == null ? null : ((DynamicObject)this.Model.GetValue("F_260_WL"))["Id"].ToString();
                string ph = this.Model.GetValue("F_RUJP_PH") == null ? null : ((DynamicObject)this.Model.GetValue("F_RUJP_PH"))["Id"].ToString();
                string ksrq = this.Model.GetValue("F_260_KSRQ").ToString();
                string jsrq = this.Model.GetValue("F_260_JSRQ").ToString();
                if (wl != null && ph != null)
                {
                    FZ(wl, ph, ksrq, ksrq, X);
                }
            }
        }**/
        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            string wl = this.Model.GetValue("F_260_WL").ToString();
            string ph = this.Model.GetValue("F_RUJP_PH").ToString();
            string ksrq = this.Model.GetValue("F_260_KSRQ").ToString();
            string jsrq = this.Model.GetValue("F_260_JSRQ").ToString();
            if (e.Key.Equals("F_260_XS") && wl != null && ph != null && ksrq != null && jsrq != null)
            {               
                FZ(wl, ph,1);
            }
            else if (e.Key.Equals("F_260_XX") && wl != null && ph != null && ksrq != null && jsrq != null)
            {
                FZ(wl, ph,2);
            }

        }
        public override void BeforeF7Select(BeforeF7SelectEventArgs e)
        {
            base.BeforeF7Select(e);
            if (e.FieldKey.EqualsIgnoreCase("F_RUJP_PH"))
            {
                string a = this.Model.GetValue("F_260_WL") == null ? null : ((DynamicObject)this.Model.GetValue("F_260_WL"))["Id"].ToString();
                string FMA = "FMATERIALID" + "=" + Convert.ToInt32(a);
                e.ListFilterParameter.Filter = e.ListFilterParameter.Filter.JoinFilterString(FMA);
                return;
            }
        }
        public override void EntryButtonCellClick(EntryButtonCellClickEventArgs e)
        {
            base.EntryButtonCellClick(e);
            if (!e.FieldKey.EqualsIgnoreCase("F_260_DJBH"))
            {
                return;
            }
            if (e.Row < 0)
            {
                return;
            }
            string number = ((DynamicObject)this.Model.GetValue("F_260_DJ", e.Row))["Number"].ToString();
            string formidsql = $@"SELECT FBILLFORMID,FNUMBER,FNAME FROM T_BAS_BILLTYPE A
            INNER JOIN T_BAS_BILLTYPE_L B ON A.FBILLTYPEID=B.FBILLTYPEID WHERE FNUMBER='{number}'";
            var formid = DBUtils.ExecuteDynamicObject(Context, formidsql);

            var formId = formid[0]["FBILLFORMID"].ToString();
            var requisitionMetadata = (FormMetadata)MetaDataServiceHelper.Load(this.Context, formId);
            var billNo = this.Model.GetValue(e.FieldKey, e.Row);
            var objs = BusinessDataServiceHelper.Load(this.Context, requisitionMetadata.BusinessInfo,
                new List<SelectorItemInfo>(new[] { new SelectorItemInfo("FID") }), OQLFilter.CreateHeadEntityFilter("FBillNo='" + billNo + "'"));
            if (objs == null || objs.Length == 0) { return; }
            var pkId = objs[0]["Id"].ToString(); var showParameter = new BillShowParameter
            {
                FormId = formId, // 业务对象标识              
                PKey = pkId, // 单据内码                
                Status = OperationStatus.VIEW // 查看模式打开                
                                              // Status = OperationStatus.EDIT// 编辑模式打开            
            };
            this.View.ShowForm(showParameter);
        }
        public override void EntityRowDoubleClick(EntityRowClickEventArgs e)
        {
            base.EntityRowDoubleClick(e);
            if (e.Key.Equals("F_RUJP_ENTITY"))
            {
                var wl = this.Model.GetValue("F_RUJP_WL", e.Row) == null ? null : (DynamicObject)this.Model.GetValue("F_RUJP_WL", e.Row);
                var ph = this.Model.GetValue("F_RUJP_LOT", e.Row) == null ? null : (DynamicObject)this.Model.GetValue("F_RUJP_LOT", e.Row);
                string kssj = this.Model.GetValue("F_260_KSRQ") == null ? null : this.Model.GetValue("F_260_KSRQ").ToString();
                string jssj = this.Model.GetValue("F_260_JSRQ") == null ? null : this.Model.GetValue("F_260_JSRQ").ToString();
                if (wl != null && ph != null && kssj != null && jssj != null)
                {
                    int i = 0;//判断是否带批号
                    if (ph["Number"].ToString().IndexOf('-') > 0)
                    {
                        i = 1;
                        SJSJY(wl["Id"].ToString(), ph["Id"].ToString(), kssj, jssj, i);
                    }
                    else
                    {
                        i = 2;
                        SJSJY(wl["Id"].ToString(), ph["Id"].ToString(), kssj, jssj, i);
                    }

                }
            }
        }
        /// <summary>
        ///双击数据源
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public void SJSJY(string wl, string lot, string kssj, string jssj, int i)
        {
            int xh = 0;
            this.Model.DeleteEntryData("F_RUJP_Entity1");
            string phnumber = (Tool.Utils.LoadBDData(Context, "BD_BatchMainFile", Convert.ToInt32(lot)))["Number"].ToString();
            string ybsql = $@"exec HMSX_260_PHZS '{wl}','{phnumber}','{kssj}','{jssj}',{i}";
            var DATES = DBUtils.ExecuteDynamicObject(Context, ybsql);
            foreach (var date in DATES)
            {
                this.Model.CreateNewEntryRow("F_RUJP_Entity1");
                this.View.Model.SetValue("F_260_DJMC", date["DJMC"], xh);
                this.View.Model.SetValue("F_260_DJ", date["DJLX"], xh);
                this.View.Model.SetItemValueByID("F_260_ZZ", Convert.ToInt32(date["ZZ"]), xh);
                this.View.Model.SetValue("F_260_DJBH", date["DJBH"].ToString(), xh);
                this.View.Model.SetValue("F_260_HH", date["HH"].ToString(), xh);
                this.View.Model.SetValue("F_260_RQ", Convert.ToDateTime(date["RQ"]), xh);
                this.View.Model.SetItemValueByID("F_260_DW", Convert.ToInt32(date["DW"]), xh);
                this.View.Model.SetValue("F_260_SL", Convert.ToDouble(date["SL"]), xh);
                this.View.Model.SetItemValueByID("F_260_CK", Convert.ToInt32(date["CK"]), xh);
                this.View.Model.SetItemValueByID("F_260_KCZT", Convert.ToInt32(date["KCZT"]), xh);
                this.View.Model.SetItemValueByID("F_260_SCCJ", Convert.ToInt32(date["SCCJ"]), xh);
                this.View.Model.SetItemValueByID("F_260_DCCK", date["DCCK"].ToString() == "" ? 0 : Convert.ToInt32(date["DCCK"]), xh);
                xh++;
            }
            this.View.UpdateView("F_RUJP_Entity1");
        }
        /// <summary>
        ///目录填充
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public void FZ(string mater, string lot, int X)
        {
            this.Model.DeleteEntryData("F_RUJP_Entity");
            this.Model.DeleteEntryData("F_RUJP_Entity1");
            int zjid = 1;//主键
            this.Model.CreateNewEntryRow("F_RUJP_Entity");
            this.View.Model.SetValue("FROWID", zjid, zjid - 1);
            this.View.Model.SetValue("F_260_CJ", 1, zjid - 1);
            this.View.Model.SetValue("FPARENTROWID", 0, zjid - 1);
            this.View.Model.SetValue("FROWEXPANDTYPE", 16, zjid - 1);
            this.View.Model.SetItemValueByID("F_RUJP_WL", Convert.ToInt32(mater), zjid - 1);
            this.View.Model.SetItemValueByID("F_RUJP_LOT", Convert.ToInt32(lot), zjid - 1);
            var wl = Tool.Utils.LoadBDData(Context, "BD_MATERIAL", Convert.ToInt32(mater));
            if (wl["Number"].ToString().Contains("260.01") == false)
            {
                var piha = Tool.Utils.LoadBDData(Context, "BD_BatchMainFile", Convert.ToInt32(lot));
                string zjsql = $@"EXEC HMSX_260_PHZSML '{mater}','{piha["Number"].ToString()}',{X}";
                var boms = DBUtils.ExecuteDynamicObject(Context, zjsql);
                if (boms.Count > 0)
                {
                    foreach (var bom in boms)
                    {
                        zjid++;
                        this.Model.CreateNewEntryRow("F_RUJP_Entity");
                        this.View.Model.SetValue("FROWID", zjid, zjid - 1);
                        this.View.Model.SetValue("F_260_CJ", 2, zjid - 1);
                        this.View.Model.SetValue("FPARENTROWID", 1, zjid - 1);
                        this.View.Model.SetValue("FROWEXPANDTYPE", 16, zjid - 1);
                        this.View.Model.SetItemValueByID("F_RUJP_WL", Convert.ToInt32(bom["FMATERIALID"].ToString()), zjid - 1);
                        this.View.Model.SetItemValueByID("F_RUJP_LOT", Convert.ToInt32(bom["FLOT"].ToString()), zjid - 1);
                        var wl1 = Tool.Utils.LoadBDData(Context, "BD_MATERIAL", Convert.ToInt32(bom["FMATERIALID"].ToString()));
                        if (wl1["Number"].ToString().Contains("260.01") == false)
                        {
                            var piha1 = Tool.Utils.LoadBDData(Context, "BD_BatchMainFile", Convert.ToInt32(bom["FLOT"].ToString()));
                            string yjsql = $@"EXEC HMSX_260_PHZSML '{bom["FMATERIALID"].ToString()}','{piha1["Number"].ToString()}',{X}";
                            var bom1s = DBUtils.ExecuteDynamicObject(Context, yjsql);
                            if (bom1s.Count > 0)
                            {
                                int ej = 0;//二级
                                foreach (var bom1 in bom1s)
                                {
                                    zjid++;
                                    ej++;
                                    this.Model.CreateNewEntryRow("F_RUJP_Entity");
                                    this.View.Model.SetValue("FROWID", zjid, zjid - 1);
                                    this.View.Model.SetValue("F_260_CJ", 3, zjid - 1);
                                    this.View.Model.SetValue("FPARENTROWID", zjid - ej, zjid - 1);
                                    this.View.Model.SetValue("FROWEXPANDTYPE", 16, zjid - 1);
                                    this.View.Model.SetItemValueByID("F_RUJP_WL", Convert.ToInt32(bom1["FMATERIALID"].ToString()), zjid - 1);
                                    this.View.Model.SetItemValueByID("F_RUJP_LOT", Convert.ToInt32(bom1["FLOT"].ToString()), zjid - 1);
                                    var wl2 = Tool.Utils.LoadBDData(Context, "BD_MATERIAL", Convert.ToInt32(bom1["FMATERIALID"].ToString()));
                                    if (wl2["Number"].ToString().Contains("260.01") == false)
                                    {
                                        var piha2 = Tool.Utils.LoadBDData(Context, "BD_BatchMainFile", Convert.ToInt32(bom1["FLOT"].ToString()));
                                        string ejsql = $@"EXEC HMSX_260_PHZSML '{bom1["FMATERIALID"].ToString()}','{piha2["Number"].ToString()}',{X}";
                                        var bom2s = DBUtils.ExecuteDynamicObject(Context, ejsql);
                                        if (bom2s.Count > 0)
                                        {
                                            int sj = 0;//三级
                                            foreach (var bom2 in bom2s)
                                            {
                                                ej++;
                                                sj++;
                                                zjid++;
                                                this.Model.CreateNewEntryRow("F_RUJP_Entity");
                                                this.View.Model.SetValue("FROWID", zjid, zjid - 1);
                                                this.View.Model.SetValue("F_260_CJ", 4, zjid - 1);
                                                this.View.Model.SetValue("FPARENTROWID", zjid - sj, zjid - 1);
                                                this.View.Model.SetValue("FROWEXPANDTYPE", 16, zjid - 1);
                                                this.View.Model.SetItemValueByID("F_RUJP_WL", Convert.ToInt32(bom2["FMATERIALID"].ToString()), zjid - 1);
                                                this.View.Model.SetItemValueByID("F_RUJP_LOT", Convert.ToInt32(bom2["FLOT"].ToString()), zjid - 1);
                                                var wl3 = Tool.Utils.LoadBDData(Context, "BD_MATERIAL", Convert.ToInt32(bom2["FMATERIALID"].ToString()));
                                                if (wl3["Number"].ToString().Contains("260.01") == false)
                                                {
                                                    var piha3 = Tool.Utils.LoadBDData(Context, "BD_BatchMainFile", Convert.ToInt32(bom2["FLOT"].ToString()));
                                                    string sjsql = $@"EXEC HMSX_260_PHZSML '{bom2["FMATERIALID"].ToString()}','{piha3["Number"].ToString()}',{X}";
                                                    var bom3s = DBUtils.ExecuteDynamicObject(Context, sjsql);
                                                    if (bom3s.Count > 0)
                                                    {
                                                        int sij = 0;//四级
                                                        foreach (var bom3 in bom3s)
                                                        {
                                                            ej++;
                                                            sj++;
                                                            sij++;
                                                            zjid++;
                                                            this.Model.CreateNewEntryRow("F_RUJP_Entity");
                                                            this.View.Model.SetValue("FROWID", zjid, zjid - 1);
                                                            this.View.Model.SetValue("F_260_CJ", 5, zjid - 1);
                                                            this.View.Model.SetValue("FPARENTROWID", zjid - sij, zjid - 1);
                                                            this.View.Model.SetValue("FROWEXPANDTYPE", 16, zjid - 1);
                                                            this.View.Model.SetItemValueByID("F_RUJP_WL", Convert.ToInt32(bom3["FMATERIALID"].ToString()), zjid - 1);
                                                            this.View.Model.SetItemValueByID("F_RUJP_LOT", Convert.ToInt32(bom3["FLOT"].ToString()), zjid - 1);
                                                            var wl4 = Tool.Utils.LoadBDData(Context, "BD_MATERIAL", Convert.ToInt32(bom3["FMATERIALID"].ToString()));
                                                            if (wl4["Number"].ToString().Contains("260.01") == false)
                                                            {
                                                                var piha4 = Tool.Utils.LoadBDData(Context, "BD_BatchMainFile", Convert.ToInt32(bom3["FLOT"].ToString()));
                                                                string sijsql = $@"EXEC HMSX_260_PHZSML '{bom3["FMATERIALID"].ToString()}','{piha4["Number"].ToString()}',{X}";
                                                                var bom4s = DBUtils.ExecuteDynamicObject(Context, sijsql);
                                                                if (bom4s.Count > 0)
                                                                {
                                                                    int wj = 0;
                                                                    foreach (var bom4 in bom4s)
                                                                    {
                                                                        ej++;
                                                                        sj++;
                                                                        sij++;
                                                                        wj++;
                                                                        zjid++;
                                                                        this.Model.CreateNewEntryRow("F_RUJP_Entity");
                                                                        this.View.Model.SetValue("FROWID", zjid, zjid - 1);
                                                                        this.View.Model.SetValue("F_260_CJ", 6, zjid - 1);
                                                                        this.View.Model.SetValue("FPARENTROWID", zjid - wj, zjid - 1);
                                                                        this.View.Model.SetValue("FROWEXPANDTYPE", 16, zjid - 1);
                                                                        this.View.Model.SetItemValueByID("F_RUJP_WL", Convert.ToInt32(bom4["FMATERIALID"].ToString()), zjid - 1);
                                                                        this.View.Model.SetItemValueByID("F_RUJP_LOT", Convert.ToInt32(bom4["FLOT"].ToString()), zjid - 1);
                                                                        var wl5 = Tool.Utils.LoadBDData(Context, "BD_MATERIAL", Convert.ToInt32(bom4["FMATERIALID"].ToString()));
                                                                        if (wl5["Number"].ToString().Contains("260.01") == false)
                                                                        {
                                                                            var piha5 = Tool.Utils.LoadBDData(Context, "BD_BatchMainFile", Convert.ToInt32(bom4["FLOT"].ToString()));
                                                                            string wjsql = $@"EXEC HMSX_260_PHZSML '{bom4["FMATERIALID"].ToString()}','{piha5["Number"].ToString()}',{X}";
                                                                            var bom5s = DBUtils.ExecuteDynamicObject(Context, wjsql);
                                                                            if (bom5s.Count > 0)
                                                                            {
                                                                                int lj = 0;
                                                                                foreach (var bom5 in bom5s)
                                                                                {
                                                                                    ej++;
                                                                                    sj++;
                                                                                    sij++;
                                                                                    wj++;
                                                                                    zjid++;
                                                                                    lj++;
                                                                                    this.Model.CreateNewEntryRow("F_RUJP_Entity");
                                                                                    this.View.Model.SetValue("FROWID", zjid, zjid - 1);
                                                                                    this.View.Model.SetValue("F_260_CJ", 7, zjid - 1);
                                                                                    this.View.Model.SetValue("FPARENTROWID", zjid - lj, zjid - 1);
                                                                                    this.View.Model.SetValue("FROWEXPANDTYPE", 16, zjid - 1);
                                                                                    this.View.Model.SetItemValueByID("F_RUJP_WL", Convert.ToInt32(bom5["FMATERIALID"].ToString()), zjid - 1);
                                                                                    this.View.Model.SetItemValueByID("F_RUJP_LOT", Convert.ToInt32(bom5["FLOT"].ToString()), zjid - 1);
                                                                                    var wl6 = Tool.Utils.LoadBDData(Context, "BD_MATERIAL", Convert.ToInt32(bom5["FMATERIALID"].ToString()));
                                                                                    if (wl6["Number"].ToString().Contains("260.01") == false)
                                                                                    {
                                                                                        var piha6 = Tool.Utils.LoadBDData(Context, "BD_BatchMainFile", Convert.ToInt32(bom5["FLOT"].ToString()));
                                                                                        string ljsql = $@"EXEC HMSX_260_PHZSML '{bom5["FMATERIALID"].ToString()}','{piha6["Number"].ToString()}',{X}";
                                                                                        var bom6s = DBUtils.ExecuteDynamicObject(Context, ljsql);
                                                                                        if (bom6s.Count > 0)
                                                                                        {
                                                                                            int qj = 0;
                                                                                            foreach (var bom6 in bom6s)
                                                                                            {
                                                                                                ej++;
                                                                                                sj++;
                                                                                                sij++;
                                                                                                wj++;
                                                                                                zjid++;
                                                                                                lj++;
                                                                                                qj++;
                                                                                                this.Model.CreateNewEntryRow("F_RUJP_Entity");
                                                                                                this.View.Model.SetValue("FROWID", zjid, zjid - 1);
                                                                                                this.View.Model.SetValue("F_260_CJ", 7, zjid - 1);
                                                                                                this.View.Model.SetValue("FPARENTROWID", zjid - qj, zjid - 1);
                                                                                                this.View.Model.SetValue("FROWEXPANDTYPE", 16, zjid - 1);
                                                                                                this.View.Model.SetItemValueByID("F_RUJP_WL", Convert.ToInt32(bom6["FMATERIALID"].ToString()), zjid - 1);
                                                                                                this.View.Model.SetItemValueByID("F_RUJP_LOT", Convert.ToInt32(bom6["FLOT"].ToString()), zjid - 1);
                                                                                                var wl7 = Tool.Utils.LoadBDData(Context, "BD_MATERIAL", Convert.ToInt32(bom6["FMATERIALID"].ToString()));
                                                                                                if (wl7["Number"].ToString().Contains("260.01") == false)
                                                                                                {
                                                                                                    var piha7 = Tool.Utils.LoadBDData(Context, "BD_BatchMainFile", Convert.ToInt32(bom6["FLOT"].ToString()));
                                                                                                    string qjsql = $@"EXEC HMSX_260_PHZSML '{bom6["FMATERIALID"].ToString()}','{piha7["Number"].ToString()}',{X}";
                                                                                                    var bom7s = DBUtils.ExecuteDynamicObject(Context, qjsql);
                                                                                                    if (bom7s.Count > 0)
                                                                                                    {
                                                                                                        int bj = 0;
                                                                                                        foreach (var bom7 in bom7s)
                                                                                                        {
                                                                                                            ej++;
                                                                                                            sj++;
                                                                                                            sij++;
                                                                                                            wj++;
                                                                                                            zjid++;
                                                                                                            lj++;
                                                                                                            qj++;
                                                                                                            bj++;
                                                                                                            this.Model.CreateNewEntryRow("F_RUJP_Entity");
                                                                                                            this.View.Model.SetValue("FROWID", zjid, zjid - 1);
                                                                                                            this.View.Model.SetValue("F_260_CJ", 7, zjid - 1);
                                                                                                            this.View.Model.SetValue("FPARENTROWID", zjid - bj, zjid - 1);
                                                                                                            this.View.Model.SetValue("FROWEXPANDTYPE", 16, zjid - 1);
                                                                                                            this.View.Model.SetItemValueByID("F_RUJP_WL", Convert.ToInt32(bom7["FMATERIALID"].ToString()), zjid - 1);
                                                                                                            this.View.Model.SetItemValueByID("F_RUJP_LOT", Convert.ToInt32(bom7["FLOT"].ToString()), zjid - 1);
                                                                                                            var wl8 = Tool.Utils.LoadBDData(Context, "BD_MATERIAL", Convert.ToInt32(bom7["FMATERIALID"].ToString()));
                                                                                                            if (wl8["Number"].ToString().Contains("260.01") == false)
                                                                                                            {
                                                                                                                var piha8 = Tool.Utils.LoadBDData(Context, "BD_BatchMainFile", Convert.ToInt32(bom7["FLOT"].ToString()));
                                                                                                                string bjsql = $@"EXEC HMSX_260_PHZSML '{bom7["FMATERIALID"].ToString()}','{piha8["Number"].ToString()}',{X}";
                                                                                                                var bom8s = DBUtils.ExecuteDynamicObject(Context, bjsql);
                                                                                                                if (bom8s.Count > 0)
                                                                                                                {
                                                                                                                    int jj = 0;
                                                                                                                    foreach (var bom8 in bom8s)
                                                                                                                    {
                                                                                                                        ej++;
                                                                                                                        sj++;
                                                                                                                        sij++;
                                                                                                                        wj++;
                                                                                                                        zjid++;
                                                                                                                        lj++;
                                                                                                                        qj++;
                                                                                                                        bj++;
                                                                                                                        jj++;
                                                                                                                        this.Model.CreateNewEntryRow("F_RUJP_Entity");
                                                                                                                        this.View.Model.SetValue("FROWID", zjid, zjid - 1);
                                                                                                                        this.View.Model.SetValue("F_260_CJ", 7, zjid - 1);
                                                                                                                        this.View.Model.SetValue("FPARENTROWID", zjid - jj, zjid - 1);
                                                                                                                        this.View.Model.SetValue("FROWEXPANDTYPE", 16, zjid - 1);
                                                                                                                        this.View.Model.SetItemValueByID("F_RUJP_WL", Convert.ToInt32(bom8["FMATERIALID"].ToString()), zjid - 1);
                                                                                                                        this.View.Model.SetItemValueByID("F_RUJP_LOT", Convert.ToInt32(bom8["FLOT"].ToString()), zjid - 1);

                                                                                                                        //var piha8 = Tool.Utils.LoadBDData(Context, "BD_BatchMainFile", Convert.ToInt32(bom7["FLOT"].ToString()));
                                                                                                                        //string bjsql = $@"EXEC HMSX_260_PHZSML '{bom7["FMATERIALID"].ToString()}','{piha8["Number"].ToString()}',{X}";
                                                                                                                        //var bom8s = DBUtils.ExecuteDynamicObject(Context, bjsql);

                                                                                                                    }
                                                                                                                }
                                                                                                            }
                                                                                                        }
                                                                                                    }
                                                                                                }
                                                                                            }
                                                                                        }
                                                                                    }
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }

                                }
                            }
                        }

                    }
                }
            }
            this.View.UpdateView("F_RUJP_Entity");
            this.View.UpdateView("F_RUJP_Entity1");
        }
    }
}

