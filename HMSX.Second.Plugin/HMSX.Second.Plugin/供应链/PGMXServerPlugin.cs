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
            String[] propertys = { "F_260_CSTM", "FMoBillNo", "FMoSeq" , "FMaterialId", "FWorkQty" , "F_260_LLSL" };
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
                            if (entity["F_260_CSTM"] != null && entity["F_260_CSTM"].ToString() != "" && Convert.ToDecimal(entity["F_260_LLSL"])==0)
                            {
                                string[] cstms = entity["F_260_CSTM"].ToString().Split(',');
                                string tm = "";
                                int i = 1;
                                foreach (string cstm in cstms)
                                {
                                    if(i== cstms.Length)
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
                                where a.FMATERIALID='{date["MaterialId_Id"]}'and  a.FMOBILLNO='{date["MoBillNo"]}' and a.FMOENTRYSEQ='{date["MoSeq"]}'";
                                var ylqds = DBUtils.ExecuteDynamicObject(Context, ylqdsql);
                                if (ylqds.Count <= 1 && !((DynamicObject)date["MaterialId"])["Number"].ToString().Contains("260.02."))
                                {
                                    foreach (var ylqd in ylqds)
                                    {
                                        string upsql = $@"update T_SFC_DISPATCHDETAILENTRY set 
                                    F_260_SYBDSL=FWORKQTY,F_260_XBSL=0
                                    where FENTRYID in ({ylqd["FENTRYID"].ToString().Trim(',')})";
                                        DBUtils.Execute(Context, upsql);
                                    }
                                }
                                else
                                {
                                    foreach (var ylqd in ylqds)
                                    {
                                        string upsql = $@"update T_SFC_DISPATCHDETAILENTRY set 
                             F_260_SYBDSL=F_260_SYBDSL+{Convert.ToDecimal(ylqd["bl"]) * Convert.ToDecimal(entity["WorkQty"])},
                               F_260_XBSL=F_260_XBSL-{Convert.ToDecimal(ylqd["bl"]) * Convert.ToDecimal(entity["WorkQty"])}
                             where FENTRYID in ({ylqd["FENTRYID"].ToString().Trim(',')})";
                                        DBUtils.Execute(Context, upsql);
                                    }
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
