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
                                string ylqdsql = $@"select 
                                FNUMERATOR/FDENOMINATOR bl,F_260_SYBDSL,PGMX.FENTRYID
                                from T_PRD_PPBOM a
                                inner join T_PRD_PPBOMENTRY b on a.fid=b.fid                
                                INNER JOIN
                                (SELECT FENTRYID,FMATERIALID,F_260_SYBDSL from T_SFC_DISPATCHDETAIL t 
                                inner join T_SFC_DISPATCHDETAILENTRY t1 on t.FID=t1.FID  WHERE F_260_CSTM!=''and ({tm.Trim('r').Trim('o')})) PGMX ON PGMX.FMATERIALID=b.FMATERIALID
                                where a.FMATERIALID='{date["MaterialId_Id"]}'and  a.FMOBILLNO='{date["MoBillNo"]}' and a.FMOENTRYSEQ='{date["MoSeq"]}'";
                                var ylqds = DBUtils.ExecuteDynamicObject(Context, ylqdsql);
                                foreach (var ylqd in ylqds)
                                {
                                    string upsql = $@"update T_SFC_DISPATCHDETAILENTRY set 
                                    F_260_SYBDSL=FWORKQTY,F_260_XBSL=0
                                    where FENTRYID='{ylqd["FENTRYID"]}'";
                                    DBUtils.Execute(Context, upsql);
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
