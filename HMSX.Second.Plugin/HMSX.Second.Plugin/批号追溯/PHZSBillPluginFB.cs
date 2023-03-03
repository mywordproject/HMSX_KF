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
using Kingdee.BOS;

namespace HMSX.Second.Plugin.批号追溯
{
    [Description("批号追溯11——单击过滤弹出对话框")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class PHZSBillPluginFB : AbstractDynamicFormPlugIn
    {
        //int X = 0;//判断向上追溯2还是向下1
        //Guid.NewGuid().ToString("n");
        int hs = 1;
        int CC = 1;
        string LSB = "";
        /// <summary>
        /// </summary>
        /// <param name="e"></param>
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);

            DateTime dt = DateTime.Now;
            dt = dt.AddMonths(-3);
            this.Model.SetValue("F_260_KSRQ", dt);
            this.View.UpdateView("F_260_KSRQ");
            LSB = "HMSX" + Guid.NewGuid().ToString("n");
            string createsql = $@"/*dialect*/create table {LSB}(
                    FROWID int ,
                    FPARENTROWID int ,
                    F_RUJP_WL varchar(255),
                    F_RUJP_LOT varchar(255),
                    F_260_RKSL varchar(255),
                    WLFNUMBER varchar(255),
                    PHFNUMBER varchar(255),
                    CC int,
                    WLNAME varchar(255))";
            DBUtils.Execute(Context, createsql);
        }
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (e.Field.Key == "F_260_WL")
            {
                this.Model.SetValue("F_260_CC", 1);
                this.View.UpdateView("F_260_CC");
            }

        }
        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            if (e.Key == "F_260_XS" || e.Key == "F_260_XX")
            {
                int CJ = Convert.ToInt32(this.Model.GetValue("F_260_CC").ToString());
                this.Model.DeleteEntryData("F_RUJP_Entity1");
                if (e.Key == "F_260_XS")
                {
                    if (CJ > 1)
                    {
                        filldata1(1, CJ);
                    }
                    else
                    {
                        DynamicObject wl = (DynamicObject)this.Model.GetValue("F_260_WL");
                        DynamicObject ph = (DynamicObject)this.Model.GetValue("F_RUJP_PH");
                        if (wl == null && ph == null)
                        {
                            throw new KDBusinessException("", "物料和批号必填！");
                        }
                        long wlid = Convert.ToInt64(wl["Id"]);
                        long phid = Convert.ToInt64(ph["Id"]);
                        string sql = $@"select sum(FREALQTY) FREALQTY from
                          (select FREALQTY from T_PRD_INSTOCKENTRY where FMATERIALID={wlid} AND FLOT={phid}
                          union all
                          select FREALQTY from T_STK_INSTOCKENTRY where FMATERIALID={wlid} AND FLOT={phid})KS";
                        DynamicObjectCollection rksl = DBUtils.ExecuteDynamicObject(this.Context, sql);
                        //向上

                        hs = 1;
                        string cxsql = $@"/*dialect*/delete from {LSB}";
                        DBUtils.Execute(Context, cxsql);
                        string insertsql = $@"/*dialect*/insert into {LSB} values ('{hs}','{0}','{wlid}','{phid}','{rksl[0]["FREALQTY"]}','{wl["Number"]}','{ph["Number"]}',1,'{wl["Name"]}')";
                        DBUtils.Execute(Context, insertsql);
                        filldata(wlid, ph["Number"].ToString(), 1, wl["Number"].ToString());
                    }
                }
                //向下
                else if (e.Key == "F_260_XX")
                {
                    if (CJ > 1)
                    {
                        filldata1(2, CJ);
                    }
                    else
                    {
                        DynamicObject wl = (DynamicObject)this.Model.GetValue("F_260_WL");
                        DynamicObject ph = (DynamicObject)this.Model.GetValue("F_RUJP_PH");
                        if (wl == null && ph == null)
                        {
                            throw new KDBusinessException("", "物料和批号必填！");
                        }
                        long wlid = Convert.ToInt64(wl["Id"]);
                        long phid = Convert.ToInt64(ph["Id"]);
                        string sql = $@"select sum(FREALQTY) FREALQTY from
                          (select FREALQTY from T_PRD_INSTOCKENTRY where FMATERIALID={wlid} AND FLOT={phid}
                          union all
                          select FREALQTY from T_STK_INSTOCKENTRY where FMATERIALID={wlid} AND FLOT={phid})KS";
                        DynamicObjectCollection rksl = DBUtils.ExecuteDynamicObject(this.Context, sql);
                        //向上                     
                        hs = 1;
                        string cxsql = $@"/*dialect*/delete from {LSB}";
                        DBUtils.Execute(Context, cxsql);
                        string insertsql = $@"/*dialect*/insert into {LSB} values ('{hs}','{0}','{wlid}','{phid}','{rksl[0]["FREALQTY"]}','{wl["Number"]}','{ph["Number"]}',1,'{wl["Name"]}')";
                        DBUtils.Execute(Context, insertsql);
                        filldata(wlid, ph["Number"].ToString(), 2, wl["Number"].ToString());
                    }

                }
            }
            else if (e.Key == "F_260_XSZS")
            {
                MLTC();
            }
        }
        public override void BeforeF7Select(BeforeF7SelectEventArgs e)
        {
            base.BeforeF7Select(e);
            if (e.FieldKey.EqualsIgnoreCase("F_RUJP_PH"))
            {
                string a = this.Model.GetValue("F_260_WL") == null ? null : ((DynamicObject)this.Model.GetValue("F_260_WL"))["Id"].ToString();
                string FMA = "FMATERIALID" + "=" + Convert.ToInt32(a) + "and FINSTOCKDATE >'2022-04-01'";
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
                string wl = this.Model.GetValue("F_WLID", e.Row) == null ? null : this.Model.GetValue("F_WLID", e.Row).ToString();
                string phnumber = this.Model.GetValue("F_PH", e.Row) == null ? null : this.Model.GetValue("F_PH", e.Row).ToString();
                string kssj = this.Model.GetValue("F_260_KSRQ") == null ? null : this.Model.GetValue("F_260_KSRQ").ToString();
                string jssj = this.Model.GetValue("F_260_JSRQ") == null ? null : this.Model.GetValue("F_260_JSRQ").ToString();
                if (wl != null && phnumber != null && kssj != null && jssj != null)
                {
                    int i = 0;//判断是否带批号
                    if (phnumber.IndexOf('-') > 0)
                    {
                        i = 1;
                        SJSJY(wl, phnumber, kssj, jssj, i);
                    }
                    else
                    {
                        i = 2;
                        SJSJY(wl, phnumber, kssj, jssj, i);
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
            string ybsql = $@"exec HMSX_260_PHZS '{wl}','{lot}','{kssj}','{jssj}',{i}";
            var DATES = DBUtils.ExecuteDynamicObject(Context, ybsql);
            foreach (var date in DATES)
            {
                this.Model.CreateNewEntryRow("F_RUJP_Entity1");
                this.View.Model.SetValue("F_260_DJMC", date["DJMC"], xh);
                this.View.Model.SetValue("F_260_DJ", date["DJLX"], xh);
                this.View.Model.SetValue("F_DJLXNAME", date["DJLXNAME"], xh);
                //this.View.Model.SetItemValueByID("F_260_ZZ", Convert.ToInt32(date["ZZ"]), xh);
                this.View.Model.SetValue("F_ZZNAME", date["ZZNAME"], xh);
                this.View.Model.SetValue("F_260_DJBH", date["DJBH"].ToString(), xh);
                this.View.Model.SetValue("F_260_HH", date["HH"].ToString(), xh);
                this.View.Model.SetValue("F_260_RQ", Convert.ToDateTime(date["RQ"]), xh);
                //this.View.Model.SetItemValueByID("F_260_DW", Convert.ToInt32(date["DW"]), xh);
                this.View.Model.SetValue("F_DWNAME", date["DWNAME"], xh);
                this.View.Model.SetValue("F_260_SL", Convert.ToDouble(date["SL"]), xh);
                //this.View.Model.SetItemValueByID("F_260_CK", Convert.ToInt32(date["CK"]), xh);
                this.View.Model.SetValue("F_CKNAME", date["CKNAME"], xh);
                //this.View.Model.SetItemValueByID("F_260_KCZT", Convert.ToInt32(date["KCZT"]), xh);
                this.View.Model.SetValue("F_KCZTNAME", date["KCZTNAME"], xh);
                //this.View.Model.SetItemValueByID("F_260_SCCJ", Convert.ToInt32(date["SCCJ"]), xh);
                this.View.Model.SetValue("F_SCCJNAME", date["SCCJNAME"], xh);
                this.View.Model.SetValue("F_260_KHBQ", date["KHBQ"] == null ? "" : date["KHBQ"].ToString(), xh);
                //this.View.Model.SetItemValueByID("F_260_DCCK", date["DCCK"].ToString() == "" ? 0 : Convert.ToInt32(date["DCCK"]), xh);
                this.View.Model.SetValue("F_DCCKNAME", date["DCCKNAME"], xh);
                this.View.Model.SetValue("F_260_GYSPC", date["GYSPC"] == null ? "" : date["GYSPC"].ToString(), xh);
                xh++;
            }
            this.View.UpdateView("F_RUJP_Entity1");
        }
        /// <summary>
        ///目录填充
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        private void filldata(long wl, string ph, int sx, string wlfunmer)
        {
            hs = 1;
            CC = 1;//层次
            /**
          // string sql = $"exec HMSX_260_PHZSML {wl},'{ph}',{sx}";
          // DynamicObjectCollection objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
          // int parent = hs - 1;
          // foreach (DynamicObject obj in objs)
          // {
          //     long wlid = Convert.ToInt64(obj["FMATERIALID"]);
          //     long phid = Convert.ToInt64(obj["FLOT"]);
          //     string phNumber = obj["PHNUMBER"].ToString();
          //     double rks = Convert.ToDouble(obj["FREALQTY"]);
          //     MLTC(wlid, phid, parent, parent, rks);
          //     if (!obj["FNUMBER"].ToString().StartsWith("260.01"))
          //     {
          //         filldata(wlid, phNumber, sx);
          //     }
          // }**/
            string strs = "";
            if (wlfunmer.Contains("260.01") == false && sx == 1)
            {
                string bomsql = $"exec HMSX_260_PHZSML {wl},'{ph}',{sx}";
                DynamicObjectCollection boms = DBUtils.ExecuteDynamicObject(this.Context, bomsql);
                if (boms.Count > 0)
                {
                    int CC2 = CC + 1;
                    foreach (var bom in boms)
                    {
                        hs++;
                        strs += "(" + hs + ",'" + 1 + "', '" + bom["FMATERIALID"] + "', '" + bom["FLOT"] + "', '" + bom["FREALQTY"] + "', '" + bom["FNUMBER"] + "', '" + bom["PHNUMBER"] + "'," + CC2 + ",'" + bom["WLNAME"] + "')" + ",";
                        // string insertsql = $@"/*dialect*/insert into {LSB} values ('{hs}','{1}','{bom["FMATERIALID"]}','{bom["FLOT"]}','{bom["FREALQTY"]}','{bom["FNUMBER"]}','{bom["PHNUMBER"]}',{CC2},'{bom["WLNAME"]}')";
                        // DBUtils.Execute(Context, insertsql);
                        if (bom["FNUMBER"].ToString().Contains("260.01") == false)
                        {
                            string bomsql1 = $"exec HMSX_260_PHZSML {bom["FMATERIALID"].ToString()},'{bom["PHNUMBER"].ToString()}',{sx}";
                            DynamicObjectCollection boms1 = DBUtils.ExecuteDynamicObject(this.Context, bomsql1);
                            if (boms1.Count > 0)
                            {
                                int CC3 = CC + 2;
                                int ej = 0;//二级
                                foreach (var bom1 in boms1)
                                {
                                    hs++;
                                    ej++;
                                    strs += "(" + hs + ",'" + (hs - ej) + "', '" + bom1["FMATERIALID"] + "', '" + bom1["FLOT"] + "', '" + bom1["FREALQTY"] + "', '" + bom1["FNUMBER"] + "', '" + bom1["PHNUMBER"] + "'," + CC3 + ",'" + bom1["WLNAME"] + "')" + ",";
                                    //string insertsql1 = $@"/*dialect*/insert into {LSB} values ('{hs}','{hs - ej}','{bom1["FMATERIALID"]}','{bom1["FLOT"]}','{bom1["FREALQTY"]}','{bom1["FNUMBER"]}','{bom1["PHNUMBER"]}',{CC3},'{bom1["WLNAME"]}')";
                                    //DBUtils.Execute(Context, insertsql1);                                   
                                }
                            }
                        }

                    }
                    string insertsql = $@"/*dialect*/insert into {LSB} values {strs.Trim(',')}";
                    DBUtils.Execute(Context, insertsql);
                }
            }
            else if (wlfunmer.Contains("260.02") == false && sx == 2)
            {
                string bomsql = $"exec HMSX_260_PHZSML {wl},'{ph}',{sx}";
                DynamicObjectCollection boms = DBUtils.ExecuteDynamicObject(this.Context, bomsql);
                if (boms.Count > 0)
                {
                    int CC2 = CC + 1;
                    foreach (var bom in boms)
                    {
                        hs++;
                        strs += "(" + hs + ",'" + 1 + "', '" + bom["FMATERIALID"] + "', '" + bom["FLOT"] + "', '" + bom["FREALQTY"] + "', '" + bom["FNUMBER"] + "', '" + bom["PHNUMBER"] + "'," + CC2 + ",'" + bom["WLNAME"] + "')" + ",";
                        if (bom["FNUMBER"].ToString().Contains("260.02") == false)
                        {
                            string bomsql1 = $"exec HMSX_260_PHZSML {bom["FMATERIALID"].ToString()},'{bom["PHNUMBER"].ToString()}',{sx}";
                            DynamicObjectCollection boms1 = DBUtils.ExecuteDynamicObject(this.Context, bomsql1);
                            if (boms1.Count > 0)
                            {
                                int CC3 = CC + 2;
                                int ej = 0;//二级
                                foreach (var bom1 in boms1)
                                {
                                    hs++;
                                    ej++;
                                    strs += "(" + hs + ",'" + (hs - ej) + "', '" + bom1["FMATERIALID"] + "', '" + bom1["FLOT"] + "', '" + bom1["FREALQTY"] + "', '" + bom1["FNUMBER"] + "', '" + bom1["PHNUMBER"] + "'," + CC3 + ",'" + bom1["WLNAME"] + "')" + ",";
                                }
                            }
                        }

                    }
                    string insertsql = $@"/*dialect*/insert into {LSB} values {strs.Trim(',')}";
                    DBUtils.Execute(Context, insertsql);
                }
            }
            string cxsql = $@"/*dialect*/select * from {LSB} where CC=3 ";
            var dates = DBUtils.ExecuteDynamicObject(Context, cxsql);
            if (dates.Count > 0)
            {
                this.Model.SetValue("F_260_SFY", true);
                this.View.UpdateView("F_260_SFY");
            }
            else
            {
                this.Model.SetValue("F_260_SFY", false);
                this.View.UpdateView("F_260_SFY");
            }
        }
        private void filldata1(int sx, int CJ)
        {
            if (this.Model.GetValue("F_260_SFY").ToString() == "True")
            {
                string cssql = $@"/*dialect*/select * from {LSB}";
                var cs = DBUtils.ExecuteDynamicObject(Context, cssql);
                hs = cs.Count;
                string tpf = "";
                if (sx == 1)
                {
                    tpf = "260.01%";
                }
                else
                {
                    tpf = "260.02%";
                }
                //累计当前层物料
                string ljsql = $@"/*dialect*/select F_RUJP_WL ,F_RUJP_LOT,PHFNUMBER
                                from {LSB} where CC='{CJ}' and  WLFNUMBER NOT LIKE '{tpf}'
                                group by F_RUJP_WL ,F_RUJP_LOT,PHFNUMBER ";
                var ljdates = DBUtils.ExecuteDynamicObject(Context, ljsql);
                string strs = "";
                foreach (var lsdate in ljdates)
                {
                    string bomsql = $"exec HMSX_260_PHZSML '{lsdate["F_RUJP_WL"].ToString()}','{lsdate["PHFNUMBER"].ToString()}',{sx}";
                    DynamicObjectCollection boms = DBUtils.ExecuteDynamicObject(this.Context, bomsql);
                    string cxsql = $@"/*dialect*/select FROWID,FPARENTROWID from {LSB} 
                     where CC='{CJ}' and  WLFNUMBER NOT LIKE '{tpf}' and F_RUJP_WL={lsdate["F_RUJP_WL"]} and PHFNUMBER={lsdate["PHFNUMBER"]}
                     order by FROWID,FPARENTROWID";
                    var dates = DBUtils.ExecuteDynamicObject(Context, cxsql);                                    
                    foreach (var date in dates)
                    {                     
                        int CJ1 = CJ + 1;
                        if (boms.Count > 0)
                        {
                            foreach (var bom in boms)
                            {
                                hs++;
                                strs += "(" + hs + ",'" + date["FROWID"] + "', '" + bom["FMATERIALID"] + "', '" + bom["FLOT"] + "', '" + bom["FREALQTY"] + "', '" + bom["FNUMBER"] + "', '" + bom["PHNUMBER"] + "'," + CJ1 + ",'" + bom["WLNAME"] + "')" + ",";                    
                            }
                        }                      
                    }
                }
                if (strs == "")
                {
                    string insertsql = $@"/*dialect*/insert into {LSB} values {strs.Trim(',')}";
                    DBUtils.Execute(Context, insertsql);
                }
                string cxsql1 = $@"/*dialect*/select * from {LSB} where CC='{CJ + 1}' ";
                var dates1 = DBUtils.ExecuteDynamicObject(Context, cxsql1);
                if (dates1.Count > 0)
                {
                    this.Model.SetValue("F_260_SFY", true);
                    this.View.UpdateView("F_260_SFY");
                }
                else
                {
                    this.Model.SetValue("F_260_SFY", false);
                    this.View.UpdateView("F_260_SFY");
                }
            }
        }
        /// <summary>
        ///目录填充
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        private void MLTC()
        {
            hs = 0;
            string cxsql = $@"/*dialect*/select FROWID,FPARENTROWID,F_RUJP_WL,F_RUJP_LOT,F_260_RKSL,WLFNUMBER,PHFNUMBER,CC,WLNAME from {LSB} 
             GROUP BY FROWID,FPARENTROWID,F_RUJP_WL,F_RUJP_LOT,F_260_RKSL,WLFNUMBER,PHFNUMBER,CC,WLNAME
             ORDER BY FROWID,FPARENTROWID";
            var dates = DBUtils.ExecuteDynamicObject(Context, cxsql);
            this.Model.DeleteEntryData("F_RUJP_Entity");
            this.Model.DeleteEntryData("F_RUJP_Entity1");
            foreach (var date in dates)
            {
                this.Model.CreateNewEntryRow("F_RUJP_Entity");
                this.View.Model.SetValue("FROWID", date["FROWID"].ToString(), hs);
                this.View.Model.SetValue("FPARENTROWID", date["FPARENTROWID"].ToString(), hs);
                this.View.Model.SetValue("FROWEXPANDTYPE", 16, hs);
                //this.View.Model.SetItemValueByID("F_RUJP_WL", Convert.ToInt32(date["F_RUJP_WL"]), hs);
                //this.View.Model.SetItemValueByID("F_RUJP_LOT", date["F_RUJP_LOT"] == null ? 0 : Convert.ToInt32(date["F_RUJP_LOT"]), hs); ;
                this.View.Model.SetValue("F_260_RKSL", date["F_260_RKSL"] == null ? "" : date["F_260_RKSL"].ToString(), hs);
                this.View.Model.SetValue("F_WLBM", date["WLFNUMBER"].ToString(), hs);
                this.View.Model.SetValue("F_PH", date["PHFNUMBER"].ToString(), hs);
                this.View.Model.SetValue("F_WLID", date["F_RUJP_WL"].ToString(), hs);
                this.View.Model.SetValue("F_PHID", date["F_RUJP_LOT"].ToString(), hs);
                this.View.Model.SetValue("F_WLMC", date["WLNAME"].ToString(), hs);
                hs++;
            }
            this.View.UpdateView("F_RUJP_Entity");
        }
        public override void FormClosed(FormClosedEventArgs e)
        {
            base.FormClosed(e);
            string delsql = $@"/*dialect*/drop table {LSB}";
            DBUtils.Execute(Context, delsql);
        }
    }
}

