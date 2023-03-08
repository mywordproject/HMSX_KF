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

namespace HMSX.Second.Plugin
{
    [Description("批号追溯2023")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class ZXSCPCZSFB: AbstractDynamicFormPlugIn
    {
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
                string wlbm= this.Model.GetValue("F_WLBM", e.Row) == null ? "" : this.Model.GetValue("F_WLBM", e.Row).ToString();
                int fj= this.Model.GetValue("FPARENTROWID",e.Row) == null ? 0 : Convert.ToInt32(this.Model.GetValue("FPARENTROWID",e.Row).ToString());
                string zwl = "";
                string zphnumber = "";
                string fx= this.Model.GetValue("F_ZSFX", 0) == null ? "" : this.Model.GetValue("F_ZSFX", 0).ToString();
                if (fx=="1" && fj>=0)
                {
                    zwl = this.Model.GetValue("F_WLID", fj - 1) == null ? "" : this.Model.GetValue("F_WLID", fj - 1).ToString();
                    zphnumber = this.Model.GetValue("F_PH", fj - 1) == null ? "" : this.Model.GetValue("F_PH", fj - 1).ToString();
                }
                else if (fx == "2" && fj >=0 && wlbm.Contains("260.01.")==false)
                {
                    zwl = this.Model.GetValue("F_WLID", e.Row +1) == null ? "" : this.Model.GetValue("F_WLID", e.Row + 1).ToString();
                    zphnumber = this.Model.GetValue("F_PH", e.Row +1) == null ? "" : this.Model.GetValue("F_PH", e.Row + 1).ToString();
                }
                else if (fx == "2" && fj == 0 && wlbm.Contains("260.01."))
                {
                    zwl = "1";
                    zphnumber = "1";
                }
                if (wl != null && phnumber != null && kssj != null && jssj != null)
                {
                    int i = 0;//判断是否带批号
                    if (phnumber.IndexOf('-') > 0)
                    {
                        i = 1;               
                        SJSJY(wl, phnumber, kssj, jssj, i,zwl,zphnumber);
                    }
                    else
                    {            
                        i = 2;
                        SJSJY(wl, phnumber, kssj, jssj, i, zwl, zphnumber);
                    }

                }
            }
        }
        public void SJSJY(string wl, string lot, string kssj, string jssj, int i, string zwl, string zlot)
        {
            int xh = 0;
            this.Model.DeleteEntryData("F_RUJP_Entity1");
            string ybsql = $@"exec HMSX_260_PHZSFB '{wl}','{lot}','{kssj}','{jssj}',{i},'{zwl}','{zlot}'";
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
        private int hs;
        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            this.Model.DeleteEntryData("F_RUJP_Entity1");
            DynamicObject wl = (DynamicObject)this.Model.GetValue("F_260_WL");
            DynamicObject ph = (DynamicObject)this.Model.GetValue("F_RUJP_PH");
            if(this.Model.GetValue("F_260_WL")==null || this.Model.GetValue("F_RUJP_PH")==null)
            {
                throw new KDBusinessException("", "物料或批号不能为空！！！");
            }
            string wlid = wl["Id"].ToString();
            string wldm = wl["Number"].ToString();
            string wlmc = wl["Name"].ToString();
            string phid = ph["Id"].ToString();
            string phdm = ph["Number"].ToString();
            string ggxh = wl["Specification"].ToString();
            string sql = $@"select sum(FREALQTY) FREALQTY from
            (select FREALQTY from T_PRD_INSTOCKENTRY where FMATERIALID={wlid} AND FLOT={phid}
            union all
            select FREALQTY from T_STK_INSTOCKENTRY where FMATERIALID={wlid} AND FLOT={phid})KS";
            //向上
            if (e.Key == "F_260_XS")
            {
                hs = 1;
                this.Model.DeleteEntryData("F_RUJP_Entity");
                DynamicObjectCollection rksl = DBUtils.ExecuteDynamicObject(this.Context, sql);
                MLTC(wldm, wlmc, wlid, phdm, phid, 0, Convert.ToDouble(rksl[0]["FREALQTY"]),ggxh,1);
                filldata(wlid, phdm, 1);
                this.View.UpdateView("F_RUJP_Entity");
            }
            //向下
            else if (e.Key == "F_260_XX")
            {
                hs = 1;
                this.Model.DeleteEntryData("F_RUJP_Entity");
                DynamicObjectCollection rksl = DBUtils.ExecuteDynamicObject(this.Context, sql);
                MLTC(wldm, wlmc, wlid, phdm, phid, 0, Convert.ToDouble(rksl[0]["FREALQTY"]),ggxh,2);
                filldata(wlid, phdm, 2);
                this.View.UpdateView("F_RUJP_Entity");
            }
        }
        private void filldata(string wlfid, string phbm, int sx)
        {
            string sql = $"exec HMSX_260_PHZSML {wlfid},'{phbm}',{sx}";
            DynamicObjectCollection objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
            int parent = hs - 1;
            foreach (DynamicObject obj in objs)
            {
                string wlid = obj["FMATERIALID"].ToString();
                string wldm = obj["FNUMBER"].ToString();
                string wlmc = obj["WLNAME"].ToString();
                string GGXH = obj["GGXH"].ToString();
                string phdm = obj["PHNUMBER"].ToString();
                string phid = obj["FLOT"].ToString();
                double rks = Convert.ToDouble(obj["FREALQTY"]);
                int isCZ = 0;
                int oROWID = 0;
                int oparID = 0;
                if (wldm.StartsWith("260.03"))
                {
                    for (int i = 0; i < hs - 1; i++)
                    {
                        string owldm = this.Model.GetValue("F_WLBM", i).ToString();
                        string ophdm = this.Model.GetValue("F_PH", i).ToString();
                        if (owldm == wldm && phdm == ophdm)
                        {
                            isCZ = 1;
                            oROWID = Convert.ToInt32(this.Model.GetValue("FROWID", i));
                            oparID = Convert.ToInt32(this.Model.GetValue("FPARENTROWID", i));
                            break;
                        }
                    }
                    MLTC(wldm, wlmc, wlid, phdm, phid, parent, rks, GGXH,0);
                    if (isCZ == 0)
                    {
                        filldata(wlid, phdm, sx);
                    }
                    else if (isCZ == 1)
                    {
                        copydata(oROWID, oparID);
                    }
                }
                else
                {
                    MLTC(wldm, wlmc, wlid, phdm, phid, parent, rks, GGXH,0);
                }
            }
        }
        private void MLTC(string wldm, string wlmc, string wlid, string phbm, string phid, int parid, double sl,string ggxh,int FX)
        {
            this.Model.CreateNewEntryRow("F_RUJP_Entity");
            this.View.Model.SetValue("FROWID", hs, hs - 1);
            this.View.Model.SetValue("FPARENTROWID", parid, hs - 1);
            this.View.Model.SetValue("FROWEXPANDTYPE", 16, hs - 1);
            this.View.Model.SetValue("F_WLBM", wldm, hs - 1);
            this.View.Model.SetValue("F_WLMC", wlmc, hs - 1);
            this.View.Model.SetValue("F_GGXH", ggxh, hs - 1);
            this.View.Model.SetValue("F_WLID", wlid, hs - 1);
            this.View.Model.SetValue("F_PH", phbm, hs - 1);
            this.View.Model.SetValue("F_PHID", phid, hs - 1);
            this.View.Model.SetValue("F_260_RKSL", sl, hs - 1);
            this.View.Model.SetValue("F_ZSFX", FX, hs - 1);
            hs++;
        }
        private void copydata(int oROWID, int oparID)
        {
            int copyfw = hs - oROWID;
            for (int j = 0; j < copyfw; j++)
            {
                int parid = Convert.ToInt32(this.View.Model.GetValue("FPARENTROWID", oROWID + j));
                if (parid <= oparID) { break; }
                string wldm = this.Model.GetValue("F_WLBM", oROWID + j).ToString();
                string wlmc = this.Model.GetValue("F_WLMC", oROWID + j).ToString();
                string GGXH = this.Model.GetValue("F_GGXH", oROWID + j).ToString();
                string wlid = this.Model.GetValue("F_WLID", oROWID + j).ToString();
                string phbm = this.Model.GetValue("F_PH", oROWID + j).ToString();
                string phid = this.Model.GetValue("F_PHID", oROWID + j).ToString();
                double nrks = Convert.ToDouble(this.View.Model.GetValue("F_260_RKSL", oROWID + j));
                MLTC(wldm, wlmc, wlid, phbm, phid, parid + copyfw - 1, nrks, GGXH,0);
            }
        }
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            DateTime dt = DateTime.Now;
            dt = dt.AddMonths(-3);
            this.Model.SetValue("F_260_KSRQ", dt);
            this.View.UpdateView("F_260_KSRQ");           
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
    }
}
