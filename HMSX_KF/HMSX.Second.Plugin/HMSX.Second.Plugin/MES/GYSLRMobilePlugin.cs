using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.Mobile.PlugIn;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.App.Data;
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.ComplexCacheJson.Utils;
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.Utils;
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.ComplexCacheJson.Model;
using Kingdee.BOS.Resource;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Mobile;
using Kingdee.BOS.Util;

namespace HMSX.Second.Plugin.MES
{
    [Description("供应商录入--领料单")]
    [Kingdee.BOS.Util.HotUpdate]
    public class GYSLRMobilePlugin : AbstractMobilePlugin
    {
        string FEntryId;//派工明细EntryId
        string MoBillNo = "";//生产订单号
        string MoBillEntrySeq = "";//生产订明细行号
        protected FormCacheModel cacheModel4Save = new FormCacheModel();
        protected bool HasCached;
        public override void OnInitialize(InitializeEventArgs e)
        {
            base.OnInitialize(e);
            FEntryId = e.Paramter.GetCustomParameter("FPgEntryId").ToString();

            //获取生产订单编号，生产订单行号
            string strSql = string.Format(@"SELECT FMOBILLNO,FMOSEQ FROM T_SFC_DISPATCHDETAIL WHERE FID=(SELECT TOP 1 FID FROM T_SFC_DISPATCHDETAILENTRY WHERE FENTRYID IN ({0}))", FEntryId);
            DynamicObjectCollection rs = DBServiceHelper.ExecuteDynamicObject(this.Context, strSql);
            if (rs.Count > 0)
            {
                MoBillNo = rs[0]["FMOBILLNO"].ToString();
                MoBillEntrySeq = rs[0]["FMOSEQ"].ToString();
            }
        }
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            string strSql = $@"/*dialect*/SELECT  T1.FMATERIALID,t3.FNUMBER,T4.FNAME,T4.FSPECIFICATION,T6.GYS, 
            (
            SELECT distinct convert(varchar(255),FPgEntryId,111)+',' FROM t_PgBomInfo WHERE FPgEntryId IN ({FEntryId})  AND  t_PgBomInfo.FMATERIALID=T1.FMATERIALID 
            for xml path('')) as PGID
            FROM T_PRD_PPBOM T 
            INNER JOIN T_PRD_PPBOMENTRY T1 ON T.FID=T1.FID 
            INNER JOIN T_PRD_PPBOMENTRY_Q T2 ON T1.FID=T2.FID AND T1.FENTRYID=T2.FENTRYID  AND T1.FMUSTQTY>(T2.FPICKEDQTY-t2.FGOODRETURNQTY)
            INNER JOIN T_PRD_PPBOMENTRY_C T5 ON T1.FID=T5.FID AND T1.FENTRYID=T5.FENTRYID
            INNER JOIN T_BD_MATERIAL T3 ON T1.FMATERIALID=T3.FMATERIALID  AND T3.FMATERIALID NOT IN (SELECT FMATERIALID FROM T_BD_MATERIALBASE WHERE FErpClsID=5 )
            INNER JOIN T_BD_MATERIAL_L T4 ON T1.FMATERIALID=T4.FMATERIALID AND T4.FLOCALEID=2052
            INNER JOIN t_PgBomInfo T6 ON T1.FENTRYID=T6.FPPBomEntryId AND T6.FPgEntryId IN ({FEntryId})    AND T6.FMustQty-T6.FAvailableQty>0
            WHERE T.FMOBillNO='{MoBillNo}' AND T.FMOENTRYSEQ={MoBillEntrySeq} AND T5.FISSUETYPE IN ('1','3') 
			GROUP BY T1.FMATERIALID,t3.FNUMBER,T4.FNAME,T4.FSPECIFICATION,T6.GYS
            ORDER BY T1.FMATERIALID ASC ";
            var date = DBUtils.ExecuteDynamicObject(Context, strSql);
            this.View.Model.DeleteEntryData("F_SLSB_MobileListViewEntity");
            int i = 0;
            foreach (var da in date)
            {
                this.View.Model.CreateNewEntryRow("F_SLSB_MobileListViewEntity");
                this.View.Model.SetValue("F_XH", i + 1, i);
                this.View.Model.SetValue("F_WLID", da["FMATERIALID"], i);
                this.View.Model.SetValue("F_WLBM", da["FNUMBER"], i);
                this.View.Model.SetValue("F_WLMC", da["FNAME"], i);
                //this.View.Model.SetItemValueByID("F_GYS", da["GYS"], i);
                this.View.Model.SetValue("F_GYS", da["GYS"], i);
                this.View.Model.SetValue("F_GGXH", da["FSPECIFICATION"], i);
                this.View.Model.SetValue("F_PGID", da["PGID"].ToString().Trim(','), i);
                //this.View.Model.SetValue("F_PPBOM", da["FPPBOMID"], i);
                //this.View.Model.SetValue("F_PPBOMENTITY", da["FPPBOMENTRYID"], i);
                this.View.UpdateView("F_GYS",i);
                i++;
            }
            this.View.UpdateView("F_GYS");
            this.View.UpdateView("F_SLSB_MobileListViewEntity");
            this.View.GetControl("F_SLSB_MobileListViewEntity").SetCustomPropertyValue("listEditable", true);
        }

        public override void ButtonClick(ButtonClickEventArgs e)
        {
            base.ButtonClick(e);
            string key;
            switch (key = e.Key.ToUpper())
            {
                case "FBUTTON_RETURN":
                    JsonCacheUtils.DeleteCache(base.Context, this.cacheModel4Save.DeviceCode, this.HasCached);
                    base.View.Close();
                    return;
                case "FBUTTON_LOGOUT":
                    JsonCacheUtils.DeleteCache(base.Context, this.cacheModel4Save.DeviceCode, this.HasCached);
                    LoginUtils.LogOut(base.Context, base.View);
                    base.View.Logoff("indexforpad.aspx");
                    return;
                case "F_SUBMIT":
                    Insertt();                   
                    return;
                case "F_QX":
                    SelectAll();
                    return;
            }
        }
        public void Insertt()
        {
            int rowIndex = 0;
            int rowcount = this.View.Model.GetEntryRowCount("F_SLSB_MobileListViewEntity");
            if (rowcount > 0)
            {
                string entryId = "0";
                for (int row = 0; row < rowcount; row++)
                {
                    if (Convert.ToBoolean(this.View.Model.GetValue("F_XZ", row)))
                    {
                        rowIndex = rowIndex + 1;
                    }
                }
                if (rowIndex == 0)
                {
                    this.View.ShowStatusBarInfo(ResManager.LoadKDString("请选择要录入行！", "015747000026624", SubSystemType.MFG, new object[0]));
                    return;
                }
                else
                {
                 
                    for (int i = 0; i < rowcount; i++)
                    {
                        if (Convert.ToBoolean(this.View.Model.GetValue("F_XZ", i)))
                        {
                            entryId = entryId + ',' + this.View.Model.GetValue("F_PGID", i).ToString();
                            string gys = this.View.Model.GetValue("F_GYS", i) == null ? null : ((DynamicObject)this.View.Model.GetValue("F_GYS", i))["Id"].ToString();
                            string upstr = $@"update t_PgBomInfo set GYS='{gys}' 
                                WHERE FMATERIALID='{this.View.Model.GetValue("F_WLID", i).ToString()}' 
                                AND FPgEntryId in({this.View.Model.GetValue("F_PGID", i).ToString()})";
                            DBUtils.Execute(Context, upstr);
                        }
                    }
                    if (true)
                    {
                        this.View.ShowMessage("提交成功，是否继续去领料？",
                                         MessageBoxOptions.YesNo,
                                         new Action<MessageBoxResult>((results) =>
                                         {
                                             if (results == MessageBoxResult.Yes)
                                             {
                                                 MobileShowParameter param = new MobileShowParameter();
                                                 param.FormId = "kcda126f86b6f4754a6d58570ca2221e3";
                                                 param.ParentPageId = this.View.PageId;
                                                 param.SyncCallBackAction = false;
                                                 param.CustomParams.Add("FPgEntryId", entryId.Trim(','));
                                                 this.View.ShowForm(param);
                                             }
                                             else if (results == MessageBoxResult.No)
                                             {
                                                 JsonCacheUtils.DeleteCache(base.Context, this.cacheModel4Save.DeviceCode, this.HasCached);
                                                 base.View.Close();
                                                 return;
                                             }
                                         }));             
                    }
                }
        
            }
        }
        public void SelectAll()
        {
            int entryRowCount = this.Model.GetEntryRowCount("F_SLSB_MobileListViewEntity");         
            for (int i = 0; i < entryRowCount; i++)
            {
                this.View.Model.SetValue("F_XZ", true, i);
            }          
            this.View.UpdateView("F_SLSB_MobileListViewEntity");
        }
        public override void BeforeF7Select(BeforeF7SelectEventArgs e)
        {
            base.BeforeF7Select(e);
            if (this.Context.CurrentOrganizationInfo.ID == 100026)
            {
                if (e.FieldKey.EqualsIgnoreCase("F_GYS"))
                {
                    string a = this.Model.GetValue("F_WLID", e.Row).ToString();
                    string jskcsql = $@"select FSUPPLYID from T_STK_INVENTORY a
                                      INNER join T_BD_LOTMASTER b on a.FMATERIALID=b.FMATERIALID and a.FLOT=B.FLOTID
                                      where a.FMATERIALID='{a}' and FSTOCKORGID=100026 AND
                                      FSTOCKSTATUSID=10000 GROUP BY FSUPPLYID";
                    var jskcs = DBUtils.ExecuteDynamicObject(Context, jskcsql);
                    string str = "";
                    foreach(var jskc in jskcs)
                    {
                        if (jskc["FSUPPLYID"] != null)
                        {
                            str +=Convert.ToInt32(jskc["FSUPPLYID"])+",";
                        }                                                  
                    }
                    string gys = "FSUPPLIERID" + " in (" + str.Trim(',') + ")";
                    e.ListFilterParameter.Filter = e.ListFilterParameter.Filter.JoinFilterString(gys);
                    return;
                }
            }
        }
    }
}
