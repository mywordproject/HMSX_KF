using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Bill.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.供应链
{
    [Description("派工明细--清空系统来源")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class PGMXBillPlugin : AbstractBillPlugIn
    {
        public override void AfterSave(AfterSaveEventArgs e)
        {
            base.AfterSave(e);
            foreach (var date in this.Model.DataObject["DispatchDetailEntry"] as DynamicObjectCollection)
            {
                this.Model.SetValue("F_260_LY", "", Convert.ToInt32(date["Seq"]) - 1);
                this.View.UpdateView("F_260_LY", Convert.ToInt32(date["Seq"]) - 1);
            }
        }
        public override void AfterEntryBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterEntryBarItemClick(e);
            if (e.BarItemKey.Equals("PAEZ_TBBUTTON"))
            {
                this.View.Refresh();
                this.View.InvokeFormOperation("Save");
            }
        }
        public override void BeforeDeleteRow(BeforeDeleteRowEventArgs e)
        {
            base.BeforeDeleteRow(e);
            if (Convert.ToDecimal(this.Model.GetValue("F_260_LLSL", e.Row)) == 0 &&
                this.Model.GetValue("FSTATUS", e.Row).ToString() != "D")
            {
                DynamicObject wl = (DynamicObject)this.Model.GetValue("FMATERIALID");
                string csm = this.Model.GetValue("F_260_CSTM", e.Row) == null ? "" : this.Model.GetValue("F_260_CSTM", e.Row).ToString();
                if (wl != null)
                {
                    string id = this.View.Model.GetEntryPKValue("FEntity", e.Row).ToString();
                    string[] cstms = this.Model.GetValue("F_260_CSTM", e.Row).ToString().Split(',');
                    string tm = "";
                    int i = 1;
                    string gxjh = this.Model.GetValue("FOPTPLANNO").ToString();
                    string xlh = this.Model.GetValue("FSEQNUMBER").ToString();
                    string gx = this.Model.GetValue("FOPERNUMBER").ToString();
                    string scdd = this.Model.GetValue("FMOBILLNO").ToString();
                    string scddhh = this.Model.GetValue("FMOSEQ").ToString();
                    decimal pgsl = Convert.ToDecimal(this.Model.GetValue("FWORKQTY", e.Row));
                    string cftm = this.Model.GetValue("F_260_CFYSM", e.Row) == null ? "" : this.Model.GetValue("F_260_CFYSM", e.Row).ToString();
                    if (this.Model.GetValue("F_260_SFCJ", e.Row) != null && Convert.ToBoolean(this.Model.GetValue("F_260_SFCJ", e.Row)) == false)
                    {
                        if (cftm == "" || cftm == " ")
                        {
                            foreach (string cstm in cstms)
                            {
                                if (i == cstms.Length)
                                {
                                    tm = tm + "F_260_CSTM like '%" + cstm + "%'";
                                }
                                else
                                {
                                    tm = tm + "F_260_CSTM like '%" + cstm + "%'  or ";
                                }
                                i++;
                            }
                            string ylqdsql = $@"/*dialect*/select 
                                FNUMERATOR/FDENOMINATOR bl,PGMX.FENTRYID
                                from T_PRD_PPBOM a
                                inner join T_PRD_PPBOMENTRY b on a.fid=b.fid
                                inner join t_BD_MaterialBase c ON c.FMATERIALID=b.FMATERIALID and FERPCLSID!=1
                                INNER JOIN
                                 (  SELECT distinct FMATERIALID,
                                     (SELECT distinct  convert(varchar(255),b.FENTRYID)+','
                                     from T_SFC_DISPATCHDETAIL A
                                     inner join T_SFC_DISPATCHDETAILENTRY B on A.FID=B.FID  
                                     WHERE F_260_CSTM!=''and ({tm}) AND A.FMATERIALID=T.FMATERIALID for xml path(''))as FENTRYID
                                     from T_SFC_DISPATCHDETAIL t 
                                     inner join T_SFC_DISPATCHDETAILENTRY t1 on t.FID=t1.FID  
                                     WHERE F_260_CSTM!=''and ({tm})) PGMX ON PGMX.FMATERIALID=b.FMATERIALID
                                where a.FMATERIALID='{wl["Id"].ToString()}'and FNUMERATOR!=0 AND a.FMOBILLNO='{scdd}' and a.FMOENTRYSEQ='{scddhh}'";
                            var ylqds = DBUtils.ExecuteDynamicObject(Context, ylqdsql);
                            foreach (var ylqd in ylqds)
                            {
                                //派工数
                                decimal pgs = pgsl;
                                string pgmxsql = $@"/*dialect*/select FENTRYID,F_260_XBSL-isnull(SL,0) F_260_XBSL from T_SFC_DISPATCHDETAILENTRY a
	                                 left join (select PGTM,sum(SL)SL FROM  HMSX_CFB GROUP BY PGTM) b on a.FBARCODE=b.PGTM
	                                 where FENTRYID in ({ylqd["FENTRYID"].ToString().Trim(',')}) order by FENTRYID";
                                var pgmxs = DBUtils.ExecuteDynamicObject(Context, pgmxsql);
                                foreach (var pgmx in pgmxs)
                                {
                                    if (pgs > Convert.ToDecimal(pgmx["F_260_XBSL"]) / Convert.ToDecimal(ylqd["bl"]))
                                    {
                                        string upsql = $@"/*dialect*/update T_SFC_DISPATCHDETAILENTRY set 
                                        F_260_SYBDSL+={Convert.ToDecimal(pgmx["F_260_XBSL"]) / Convert.ToDecimal(ylqd["bl"])},
                                        F_260_XBSL-={Convert.ToDecimal(pgmx["F_260_XBSL"]) / Convert.ToDecimal(ylqd["bl"])}
                                        where FENTRYID in ({pgmx["FENTRYID"].ToString().Trim(',')})";
                                        DBUtils.Execute(Context, upsql);
                                        pgs -= Convert.ToDecimal(pgmx["F_260_XBSL"]) / Convert.ToDecimal(ylqd["bl"]);
                                    }
                                    else
                                    {
                                        string upsql = $@"/*dialect*/update T_SFC_DISPATCHDETAILENTRY set 
                                        F_260_SYBDSL=F_260_SYBDSL+{pgs * Convert.ToDecimal(ylqd["bl"])},
                                        F_260_XBSL=F_260_XBSL-{pgs * Convert.ToDecimal(ylqd["bl"])}
                                        where FENTRYID in ({pgmx["FENTRYID"].ToString().Trim(',')})";
                                        DBUtils.Execute(Context, upsql);
                                        pgs = 0;
                                    }
                                }
                            }
                        }
                        else
                        {
                            //拆分表
                            string upsql = $@"/*dialect*/ update T_SFC_DISPATCHDETAILENTRY set
                            F_260_SYBDSL+=SL,F_260_XBSL-=SL
                            from HMSX_CFB b where B.PGTM=T_SFC_DISPATCHDETAILENTRY.FBARCODE AND B.ZPGTM='{id}'";
                            DBUtils.Execute(Context, upsql);
                            string delsql = $@"/*dialect*/ delete HMSX_CFB where ZPGTM='{id}'";
                            DBUtils.Execute(Context, delsql);
                        }
                    }
                    else
                    {
                        if (cftm == "" || cftm == " ")
                        {
                            foreach (string cstm in cstms)
                            {
                                if (i == cstms.Length)
                                {
                                    tm = tm + "F_260_CSTM like '%" + cstm + "%'";
                                }
                                else
                                {
                                    tm = tm + "F_260_CSTM like '%" + cstm + "%'  or ";
                                }
                                i++;
                            }

                            //派工数
                            decimal pgs = pgsl;
                            string pgmxsql = $@"/*dialect*/select B.FENTRYID,F_260_XBSL-isnull(SL,0) F_260_XBSL  from T_SFC_DISPATCHDETAIL a
                                                                    left join T_SFC_DISPATCHDETAILENTRY b on a.fid=b.fid
                                                                    left join T_SFC_OPERATIONTRANSFER_A c on c.FOUTOPBILLNO=a.FOptPlanNo and c.FOUTSEQNUMBER=a.FSEQNUMBER and  c.FOUTOPERNUMBER=a.FOperNumber
                                                                    left join (select PGTM,sum(SL)SL FROM  HMSX_CFB GROUP BY PGTM) D on B.FBARCODE=D.PGTM
                                                                    where c.FINOPBILLNO='{gxjh}' and c.FINSEQNUMBER='{xlh}' 
                                                                    and  c.FINOPERNUMBER='{gx}' and F_260_CSTM!=''and ({tm})
                                                                     order by B.FENTRYID";
                            var pgmxs = DBUtils.ExecuteDynamicObject(Context, pgmxsql);
                            foreach (var pgmx in pgmxs)
                            {
                                if (pgs > Convert.ToDecimal(pgmx["F_260_XBSL"]))
                                {
                                    string upsql = $@"/*dialect*/update T_SFC_DISPATCHDETAILENTRY set 
                                        F_260_SYBDSL+={Convert.ToDecimal(pgmx["F_260_XBSL"])},
                                        F_260_XBSL-={Convert.ToDecimal(pgmx["F_260_XBSL"])}
                                        where FENTRYID in ({pgmx["FENTRYID"].ToString().Trim(',')})";
                                    DBUtils.Execute(Context, upsql);
                                    pgs -= Convert.ToDecimal(pgmx["F_260_XBSL"]);
                                }
                                else
                                {
                                    string upsql = $@"/*dialect*/update T_SFC_DISPATCHDETAILENTRY set 
                                        F_260_SYBDSL=F_260_SYBDSL+{pgs},
                                        F_260_XBSL=F_260_XBSL-{pgs}
                                        where FENTRYID in ({pgmx["FENTRYID"].ToString().Trim(',')})";
                                    DBUtils.Execute(Context, upsql);
                                    pgs = 0;
                                }
                            }

                        }
                        else
                        {
                            //拆分表
                            string upsql = $@"/*dialect*/ update T_SFC_DISPATCHDETAILENTRY set
                            F_260_SYBDSL+=SL,F_260_XBSL-=SL
                            from HMSX_CFB b where B.PGTM=T_SFC_DISPATCHDETAILENTRY.FBARCODE AND B.ZPGTM='{id}'";
                            DBUtils.Execute(Context, upsql);
                            string delsql = $@"/*dialect*/ delete HMSX_CFB where ZPGTM='{id}'";
                            DBUtils.Execute(Context, delsql);
                        }
                    }
                    this.View.InvokeFormOperation("Save");
                }
            }
            else
            {
                throw new KDBusinessException("", "领料数大于0或完工状态不允许删除！！");
            }

        }
    }
}
