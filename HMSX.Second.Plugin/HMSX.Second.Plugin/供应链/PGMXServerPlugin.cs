using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
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
    [Description("派工明细--整单删除")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class PGMXServerPlugin : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "F_260_CSTM", "FMoBillNo", "FMoSeq", "FMaterialId", "FWorkQty", "F_260_LLSL", "F_260_CFYSM", "FOptPlanNo", "FSeqNumber", "FOperNumber" , "F_260_SFCJ" };
            foreach (String property in propertys)
            {
                e.FieldKeys.Add(property);
            }
        }

        public override void BeforeExecuteOperationTransaction(BeforeExecuteOperationTransaction e)
        {
            base.BeforeExecuteOperationTransaction(e);
            if (Context.CurrentOrganizationInfo.ID == 100026)
            {
                if (FormOperation.Operation.Equals("Delete", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (ExtendedDataEntity extended in e.SelectedRows)
                    {
                        DynamicObject date = extended.DataEntity;
                        foreach (var entity in date["DispatchDetailEntry"] as DynamicObjectCollection)
                        {
                            if (entity["F_260_SFCJ"] != null && Convert.ToBoolean(entity["F_260_SFCJ"]) == false)
                            {
                                string cftm = entity["F_260_CFYSM"] == null ? "" : entity["F_260_CFYSM"].ToString();
                                if (cftm == "" || cftm == " ")
                                {
                                    if (entity["F_260_CSTM"] != null && entity["F_260_CSTM"].ToString() != "" && Convert.ToDecimal(entity["F_260_LLSL"]) == 0)
                                    {
                                        string[] cstms = entity["F_260_CSTM"].ToString().Split(',');
                                        string tm = "";
                                        int i = 1;
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
                                where a.FMATERIALID='{date["MaterialId_Id"]}'and FNUMERATOR!=0 and  a.FMOBILLNO='{date["MoBillNo"]}' and a.FMOENTRYSEQ='{date["MoSeq"]}'";
                                        var ylqds = DBUtils.ExecuteDynamicObject(Context, ylqdsql);
                                        foreach (var ylqd in ylqds)
                                        {
                                            //派工数
                                            decimal pgs = Convert.ToDecimal(entity["WorkQty"]);
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
                                }
                                else
                                {
                                    //拆分表
                                    string upsql = $@"/*dialect*/ update T_SFC_DISPATCHDETAILENTRY set
                            F_260_SYBDSL+=SL,F_260_XBSL-=SL
                            from HMSX_CFB b where B.PGTM=T_SFC_DISPATCHDETAILENTRY.FBARCODE AND B.ZPGTM='{entity["Id"]}'";
                                    DBUtils.Execute(Context, upsql);
                                    string delsql = $@"/*dialect*/ delete HMSX_CFB where ZPGTM='{entity["Id"]}'";
                                    DBUtils.Execute(Context, delsql);
                                }
                            }
                            else
                            {
                                string cftm = entity["F_260_CFYSM"] == null ? "" : entity["F_260_CFYSM"].ToString();
                                if (cftm == "" || cftm == " ")
                                {
                                    if (entity["F_260_CSTM"] != null && entity["F_260_CSTM"].ToString() != "" && Convert.ToDecimal(entity["F_260_LLSL"]) == 0)
                                    {
                                        string[] cstms = entity["F_260_CSTM"].ToString().Split(',');
                                        string tm = "";
                                        int i = 1;
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
                                         decimal pgs = Convert.ToDecimal(entity["WorkQty"]);
                                         string pgmxsql = $@"/*dialect*/select B.FENTRYID,F_260_XBSL-isnull(SL,0) F_260_XBSL  from T_SFC_DISPATCHDETAIL a
                                                                    left join T_SFC_DISPATCHDETAILENTRY b on a.fid=b.fid
                                                                    left join T_SFC_OPERATIONTRANSFER_A c on c.FOUTOPBILLNO=a.FOptPlanNo and c.FOUTSEQNUMBER=a.FSEQNUMBER and  c.FOUTOPERNUMBER=a.FOperNumber
                                                                    left join (select PGTM,sum(SL)SL FROM  HMSX_CFB GROUP BY PGTM) D on B.FBARCODE=D.PGTM
                                                                    where c.FINOPBILLNO='{date["OptPlanNo"]}' and c.FINSEQNUMBER='{date["SeqNumber"]}' 
                                                                    and  c.FINOPERNUMBER='{date["OperNumber"]}' and F_260_CSTM!=''and ({tm})
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
                                }
                                else
                                {
                                    //拆分表
                                    string upsql = $@"/*dialect*/ update T_SFC_DISPATCHDETAILENTRY set
                            F_260_SYBDSL+=SL,F_260_XBSL-=SL
                            from HMSX_CFB b where B.PGTM=T_SFC_DISPATCHDETAILENTRY.FBARCODE AND B.ZPGTM='{entity["Id"]}'";
                                    DBUtils.Execute(Context, upsql);
                                    string delsql = $@"/*dialect*/ delete HMSX_CFB where ZPGTM='{entity["Id"]}'";
                                    DBUtils.Execute(Context, delsql);
                                }
                            }


                        }
                    }
                }
            }
        }
        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);
            if (Context.CurrentOrganizationInfo.ID == 100026)
            {
            }
        }
    }
}
