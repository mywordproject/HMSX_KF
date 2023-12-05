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
using Kingdee.BOS.Core;

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
            String[] propertys = { "FCreateOrgId", "FMATERIALID", "FCreateDate", "F_260_ZSJWL" };
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
                    long i = 0;
                    foreach (var date in e.DataEntitys)
                    {
                        //if (date["CreateOrgId_Id"].ToString() == "100026" && ((DynamicObject)date["MATERIALID"])["Number"].ToString().Contains("260.02"))
                        //{
                        //    string countsql = $@"select top 1 FCREATEDATE from T_ENG_BOM where FMATERIALID = '{date["MATERIALID_Id"].ToString()}' order by FCREATEDATE asc";
                        //    var cont = DBUtils.ExecuteDynamicObject(Context, countsql);
                        //    if (cont.Count > 0)
                        //    {
                        //        DateTime createtime = Convert.ToDateTime(cont[0]["FCREATEDATE"].ToString());
                        //        string upsql = $@"update T_BD_MATERIAL set F_260_LCKSDATE='{createtime.AddDays(5)}'  where FMATERIALID='{date["MATERIALID_Id"].ToString()}'";
                        //        DBUtils.Execute(Context, upsql);
                        //    }
                        //}
                        string wlqdsql = $@"/*dialect*/
                            IF OBJECT_ID('tempdb.dbo.#WLQD')IS NOT NULL DROP TABLE #WLQD 
                              IF OBJECT_ID('tempdb.dbo.#WLQDZS')IS NOT NULL DROP TABLE #WLQDZS 
                              CREATE TABLE #WLQDZS(
                              FID INT,
                              PWL INT,
                              WL INT
                              )
                              select A.FID,a.FMATERIALID PWL,b.FMATERIALID WL ,FCREATEDATE
                              INTO #WLQD
                               from
                              (select FMATERIALID,FNUMBER,FID,FCREATEDATE,row_number() over (partition by FMATERIALID order by FNUMBER desc) rn 
                              from T_ENG_BOM where FUSEORGID=100026 and FFORBIDSTATUS='A' ) a
                              left join T_ENG_BOMCHILD b on a.fid=b.fid
                              left join t_BD_MaterialBase c on b.FMATERIALID=c.FMATERIALID
                              where a.rn=1;   
                                                                       
                              with cte as
                              (
                              select FID,PWL,WL,FCREATEDATE,2 as CJ from #WLQD 
                              where WL='{date["MATERIALID_Id"]}'
                              union all
                              select A.FID,a.PWL,a.WL,a.FCREATEDATE,cte.CJ+1 cj from #WLQD a
                              inner join cte on cte.PWL=a.WL
                              )
                              INSERT INTO #WLQDZS SELECT FID,PWL,WL FROM cte;
                            
                              with cte1 as
                              (
                              select FID,PWL,WL,FCREATEDATE,2 as CJ from #WLQD 
                              where PWL='{date["MATERIALID_Id"]}'
                              union all
                              select A.FID,a.PWL,a.WL,a.FCREATEDATE,cte1.CJ+1 cj from #WLQD a
                              inner join cte1 on cte1.WL=a.PWL
                              )
                              INSERT INTO #WLQDZS SELECT FID,PWL,WL FROM cte1;
                            
                              select distinct FID,PWL FMATERIALID from #WLQDZS a
                              inner join t_bd_material b on a.PWL=b.fmaterialid
							  where b.fnumber like '260.02.%' or b.fnumber like '260.03.%' or b.fnumber like '260.07.%'";
                        var wlqds = DBUtils.ExecuteDynamicObject(Context, wlqdsql);               
                        foreach (var wlqd in wlqds)
                        {
                            string cxsql = $@"/*dialect*/IF OBJECT_ID('tempdb.dbo.#WLQD1')IS NOT NULL DROP TABLE #WLQD1 
                                           select a.FMATERIALID PWL,b.FMATERIALID WL ,FCREATEDATE
                                           INTO #WLQD1
                                            from
                                           (select FMATERIALID,FNUMBER,FID,FCREATEDATE,row_number() over (partition by FMATERIALID order by FNUMBER desc) rn 
                                           from T_ENG_BOM where FUSEORGID=100026  and FFORBIDSTATUS='A'  and FDOCUMENTSTATUS='C') a
                                           left join T_ENG_BOMCHILD b on a.fid=b.fid
                                           left join t_BD_MaterialBase c on b.FMATERIALID=c.FMATERIALID
                                           where a.rn=1;                                            
                                           with cte as
                                           (
                                           select PWL,WL,FCREATEDATE,2 as CJ from #WLQD1 
                                           where WL='{wlqd["FMATERIALID"]}'
                                           union all
                                           select a.PWL,a.WL,a.FCREATEDATE,cte.CJ+1 cj from #WLQD1 a
                                           inner join cte on cte.PWL=a.WL
                                           )
                                           select * from (
                                           select 
                                           PWL,WL,A.FCREATEDATE,CJ,C.FNAME,FNUMBER from cte a
                                           left join t_bd_material b on a.PWL=b.fmaterialid
                                           left join t_bd_material_L C on a.PWL=C.fmaterialid
                                           union all 
                                           select a.fmaterialid PWL,a.fmaterialid WL,FCREATEDATE,1 cj,b.FNAME,FNUMBER from t_bd_material a
                                           left join t_bd_material_L b on a.fmaterialid=b.fmaterialid
                                           where a.fmaterialid='{wlqd["FMATERIALID"]}'
                                           )bb
                                           where FNUMBER LIKE '260.02.%'
                                           order by CJ DESC,FCREATEDATE ASC";
                            var cxs = DBUtils.ExecuteDynamicObject(Context, cxsql);
                            string delsql = $@"/*dialect*/ delete t_260_WLEntry where FID='{wlqd["FID"]}'";
                            DBUtils.Execute(Context, delsql);
                            if (cxs.Count == 0)
                            {
                                string upsql = $@"/*dialect*/ update T_ENG_BOM set F_260_ZSJWLWB='' where FID='{wlqd["FID"]}'";
                                DBUtils.Execute(Context, upsql);
                            }
                            string str = "";
                            string wlname = "";
                            foreach (var wl in cxs)
                            {
                                i++;
                                long id = 0;
                                string xmhsql = $@"select MIN(FPKID)FPKID FROM t_260_WLEntry";
                                var xmh = DBUtils.ExecuteDynamicObject(Context, xmhsql);
                                if (xmh.Count > 0)
                                {
                                    id += Convert.ToInt64(xmh[0]["FPKID"]);
                                }
                                if ((id - i) == 0)
                                {
                                    i++;
                                }
                                str += "(" + Convert.ToString(id - i) + "," + wlqd["FID"].ToString() + "," + wl["PWL"].ToString() + "),";
                                wlname += wl["FNAME"].ToString() + ",";
                            }
                            if (str != "")
                            {
                                string insertsql = $@"/*dialect*/ insert into t_260_WLEntry values {str.Trim(',')}";
                                DBUtils.Execute(Context, insertsql);
                                string upsql = $@"/*dialect*/ update T_ENG_BOM set F_260_ZSJWLWB='{wlname.Trim(',')}' where FID='{wlqd["FID"]}'";
                                DBUtils.Execute(Context, upsql);
                            }
                        }
                    }
                }
                //更新成品信息
                else if (FormOperation.Operation.Equals("GXCPXX", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var date in e.DataEntitys)
                    {
                        if (date["MATERIALID"] != null && (((DynamicObject)date["MATERIALID"])["Number"].ToString().Contains("260.03") ||
                                                          ((DynamicObject)date["MATERIALID"])["Number"].ToString().Contains("260.02") ||
                                                          ((DynamicObject)date["MATERIALID"])["Number"].ToString().Contains("260.07")))
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
                else if (FormOperation.Operation.Equals("GXSJWL", StringComparison.OrdinalIgnoreCase))
                {
                    long i = 0;
                    foreach (var date in e.DataEntitys)
                    {
                        if (date["MATERIALID"] != null && (((DynamicObject)date["MATERIALID"])["Number"].ToString().Contains("260.03") ||
                                                            ((DynamicObject)date["MATERIALID"])["Number"].ToString().Contains("260.02") ||
                                                            ((DynamicObject)date["MATERIALID"])["Number"].ToString().Contains("260.07")))
                        {
                            string cxsql = $@"/*dialect*/IF OBJECT_ID('tempdb.dbo.#WLQD')IS NOT NULL DROP TABLE #WLQD 
                                           select a.FMATERIALID PWL,b.FMATERIALID WL ,FCREATEDATE
                                           INTO #WLQD
                                            from
                                           (select FMATERIALID,FNUMBER,FID,FCREATEDATE,row_number() over (partition by FMATERIALID order by FNUMBER desc) rn 
                                           from T_ENG_BOM where FUSEORGID=100026 and FFORBIDSTATUS='A'  and FDOCUMENTSTATUS='C' ) a
                                           left join T_ENG_BOMCHILD b on a.fid=b.fid
                                           left join t_BD_MaterialBase c on b.FMATERIALID=c.FMATERIALID
                                           where a.rn=1;                                            
                                           with cte as
                                           (
                                           select PWL,WL,FCREATEDATE,2 as CJ from #WLQD 
                                           where WL='{date["MATERIALID_Id"]}'
                                           union all
                                           select a.PWL,a.WL,a.FCREATEDATE,cte.CJ+1 cj from #WLQD a
                                           inner join cte on cte.PWL=a.WL
                                           )
                                           select * from (
                                           select 
                                           PWL,WL,A.FCREATEDATE,CJ,C.FNAME,FNUMBER from cte a
                                           left join t_bd_material b on a.PWL=b.fmaterialid
                                           left join t_bd_material_L C on a.PWL=C.fmaterialid
                                           union all 
                                           select a.fmaterialid PWL,a.fmaterialid WL,FCREATEDATE,1 cj,b.FNAME,FNUMBER from t_bd_material a
                                           left join t_bd_material_L b on a.fmaterialid=b.fmaterialid
                                           where a.fmaterialid='{date["MATERIALID_Id"]}'
                                           )bb
                                           where FNUMBER LIKE '260.02.%'
                                           order by CJ DESC,FCREATEDATE ASC";
                            var cxs = DBUtils.ExecuteDynamicObject(Context, cxsql);
                            if ((date["F_260_ZSJWL"] as DynamicObjectCollection).Count != 0)
                            {
                                string delsql = $@"/*dialect*/ delete t_260_WLEntry where FID='{date["Id"]}'";
                                DBUtils.Execute(Context, delsql);
                                string upsql = $@"/*dialect*/ update T_ENG_BOM set F_260_ZSJWLWB='' where FID='{date["Id"]}'";
                                DBUtils.Execute(Context, upsql);
                            }
                            string str = "";
                            string wlname = "";
                            foreach (var wl in cxs)
                            {
                                i++;
                                long id = 0;
                                string xmhsql = $@"select MIN(FPKID)FPKID FROM t_260_WLEntry";
                                var xmh = DBUtils.ExecuteDynamicObject(Context, xmhsql);
                                if (xmh.Count > 0)
                                {
                                    id += Convert.ToInt64(xmh[0]["FPKID"]);
                                }
                                if ((id - i) == 0)
                                {
                                    i++;
                                }
                                str += "(" + Convert.ToString(id - i) + "," + date["Id"].ToString() + "," + wl["PWL"].ToString() + "),";
                                wlname += wl["FNAME"].ToString() + ",";
                            }
                            if (str != "")
                            {
                                string insertsql = $@"/*dialect*/ insert into t_260_WLEntry values {str.Trim(',')}";
                                DBUtils.Execute(Context, insertsql);
                                string upsql = $@"/*dialect*/ update T_ENG_BOM set F_260_ZSJWLWB='{wlname.Trim(',')}' where FID='{date["Id"]}'";
                                DBUtils.Execute(Context, upsql);
                            }
                        }

                    }
                }
                else if (FormOperation.Operation.Equals("Save", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var date in e.DataEntitys)
                    {
                        string upsql = $@"/*dialect*/
                               update b set B.F_260_DXGYS=d.F_260_DXGYSWB
                               from T_PRD_PPBOM a
                               inner join T_PRD_PPBOMENTRY b on a.fid=b.fid
                               inner join T_ENG_BOM c on c.FMATERIALID=a.FMATERIALID   and c.FID=a.FBOMID
                               inner join T_ENG_BOMCHILD d on c.fid=d.fid and d.FMATERIALID=b.FMATERIALID
                               inner join T_PRD_MOENTRY_A e on e.fentryid=a.FMOENTRYID 
                               where 
                               e.FSTATUS<>7 AND 
                               c.FID='{date["Id"]}'
                               
                               update T6 set T6.GYS=d.F_260_DXGYSWB
                               from T_PRD_PPBOM a
                               inner join T_PRD_PPBOMENTRY b on a.fid=b.fid
                               inner join T_ENG_BOM c on c.FMATERIALID=a.FMATERIALID   and c.FID=a.FBOMID
                               inner join T_ENG_BOMCHILD d on c.fid=d.fid and d.FMATERIALID=b.FMATERIALID
                               inner join T_PRD_MOENTRY_A e on e.fentryid=a.FMOENTRYID 
                               inner join t_PgBomInfo T6 ON B.FENTRYID=T6.FPPBomEntryId
                               where 
                               e.FSTATUS<>7 AND 
                               c.FID='{date["Id"]}' ";
                        DBUtils.Execute(Context, upsql);
                    }
                }
            }
        }
        public override void BeforeExecuteOperationTransaction(BeforeExecuteOperationTransaction e)
        {
            base.BeforeExecuteOperationTransaction(e);
            if (Context.CurrentOrganizationInfo.ID == 100026)
            {
                if (FormOperation.Operation.Equals("Save", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (ExtendedDataEntity extended in e.SelectedRows)
                    {
                        DynamicObject date = extended.DataEntity;
                        foreach (var entity in date["TreeEntity"] as DynamicObjectCollection)
                        {

                            string k = entity["MATERIALIDCHILD"] == null ? "": ((DynamicObject)((DynamicObjectCollection)((DynamicObject)entity["MATERIALIDCHILD"])["MaterialPlan"])[0])["PlanerID_Id"].ToString();
                            string yhsql = $@"/*dialect*/select A.FUSERID from T_SEC_USER a  -- 用户表
                                         left JOIN T_BD_PERSON b ON a.FLINKOBJECT = b.FPERSONID -- 人员表
                                         left JOIN T_BD_STAFF c ON b.FPERSONID = c.FPERSONID and c.FFORBIDSTATUS='A'-- 员工任岗表
                                         left join T_BD_OPERATORENTRY d on d.FSTAFFID=C.FSTAFFID
                                         where D.fentryid='{k}'";
                            var yh = DBUtils.ExecuteDynamicObject(Context, yhsql);
                            if (yh.Count > 0)
                            {
                                entity["F_260_JHYYH_Id"] = Convert.ToInt64(yh[0]["FUSERID"]);
                            }
                            if (k == "")
                            {
                                entity["F_260_JHYYH_Id"] = "";
                            }
                        }
                    }
                }
            }
        }
    }
}
