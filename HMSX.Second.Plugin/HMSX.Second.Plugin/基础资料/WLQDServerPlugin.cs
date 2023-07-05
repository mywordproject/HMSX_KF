using Kingdee.BOS.App;
using Kingdee.BOS.App.Core.Utils;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Resource;
using Kingdee.BOS.Util;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.Manager;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.Manager.ERP;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.Manager.InitializationTool;
using Kingdee.K3.PLM.Common.BusinessEntity.View;
using Kingdee.K3.PLM.Common.Core.BOSBridge;
using Kingdee.K3.PLM.Common.Core.Operation;
using Kingdee.K3.PLM.Common.Core.ServiceHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.K3.PLM.Business.PlugIn;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.Base;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.Entity;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.Manager.Document;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.Manager.PhysicalFile;
using Kingdee.K3.PLM.STD.Common.BusinessEntity.Project;


namespace HMSX.Second.Plugin
{
    [Description("物料清单--审核时，如果02，更新物料上的日期")]
    //热启动,不用重启IIS
    //python
    [Kingdee.BOS.Util.HotUpdate]
    public class WLQDServerPlugin : AbstractOperationServicePlugIn
    {

        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FCreateOrgId", "FMATERIALID", "FCreateDate" };
            foreach (String property in propertys)
            {
                e.FieldKeys.Add(property);
            }
        }
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            if (Context.CurrentOrganizationInfo.ID == 100026)
            {
                if (FormOperation.Operation.Equals("Audit", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var date in e.DataEntitys)
                    {
                        if (date["CreateOrgId_Id"].ToString() == "100026" && ((DynamicObject)date["MATERIALID"])["Number"].ToString().Contains("260.02"))
                        {
                            string countsql = $@"select top 1 FCREATEDATE from T_ENG_BOM where FMATERIALID = '{date["MATERIALID_Id"].ToString()}' order by FCREATEDATE asc";
                            var cont = DBUtils.ExecuteDynamicObject(Context, countsql);
                            if (cont.Count > 0)
                            {
                                DateTime createtime = Convert.ToDateTime(cont[0]["FCREATEDATE"].ToString());
                                string upsql = $@"update T_BD_MATERIAL set F_260_LCKSDATE='{createtime.AddDays(5)}'  where FMATERIALID='{date["MATERIALID_Id"].ToString()}'";
                                DBUtils.Execute(Context, upsql);
                            }
                        }

                    }
                }
                //更新成品信息
                else if (FormOperation.Operation.Equals("GXCPXX", StringComparison.OrdinalIgnoreCase))
                {
                    foreach(var date in e.DataEntitys)
                    {
                        if(date["MATERIALID"]!=null && ((DynamicObject)date["MATERIALID"])["Number"].ToString().Contains("260.03"))
                        {
                            string strsql = $@"/*dialect*/ IF OBJECT_ID('tempdb.dbo.#WLQD')IS NOT NULL DROP TABLE #WLQD 
                                           select a.FMATERIALID PWL,b.FMATERIALID WL ,FCREATEDATE
                                           INTO #WLQD
                                            from
                                           (select FMATERIALID,FNUMBER,FID,FCREATEDATE,row_number() over (partition by FMATERIALID order by FNUMBER desc) rn 
                                           from T_ENG_BOM where FUSEORGID=100026 ) a
                                           left join T_ENG_BOMCHILD b on a.fid=b.fid
                                           left join t_BD_MaterialBase c on b.FMATERIALID=c.FMATERIALID
                                           where a.rn=1 and FERPCLSID=2;                                            
                                           with cte as
                                           (
                                           select PWL,WL,FCREATEDATE,2 as CJ from #WLQD 
                                           where WL='{date["MATERIALID_Id"]}'
                                           union all
                                           select a.PWL,a.WL,a.FCREATEDATE,cte.CJ+1 cj from #WLQD a
                                           inner join cte on cte.PWL=a.WL
                                           )
                                           select top 1 PWL,WL,FCREATEDATE,CJ from cte order by CJ DESC,FCREATEDATE ASC";
                             var str = DBUtils.ExecuteDynamicObject(Context, strsql);
                            if (str.Count > 0)
                            {
                                string CPsql = $@"/*dialect*/ update T_ENG_BOM set F_260_ZSJCP='{str[0]["PWL"]}' where FID={date["Id"]}";
                                DBUtils.Execute(Context, CPsql);
                            }
                        }
                    }
                }
            }
        }
    }
}
