using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.ZXJH
{
    public class WLQDServicePlugin : IScheduleService
    {
        public void Run(Kingdee.BOS.Context ctx, Schedule schedule)
        {
            string wlqdsql = $@"/*dialect*/select FID,A.FMATERIALID from T_ENG_BOM a
                            left join T_BD_MATERIAL B ON A.FMATERIALID=B.FMATERIALID
                            where (B.FNUMBER LIKE '260.02.%' OR B.FNUMBER LIKE '260.03.%' OR B.FNUMBER LIKE '260.07.%' )AND
                            year(A.FMODIFYDATE)=year(getdate())  and month(A.FMODIFYDATE)=month(getdate())  and day(A.FMODIFYDATE)=day(getdate())";
            var wlqd = DBUtils.ExecuteDynamicObject(ctx, wlqdsql);
            long i = 0;
            foreach (var date in wlqd)
            {
                string cxsql = $@"/*dialect*/IF OBJECT_ID('tempdb.dbo.#WLQD')IS NOT NULL DROP TABLE #WLQD 
                                           select a.FMATERIALID PWL,b.FMATERIALID WL ,FCREATEDATE
                                           INTO #WLQD
                                            from
                                           (select FMATERIALID,FNUMBER,FID,FCREATEDATE,row_number() over (partition by FMATERIALID order by FNUMBER desc) rn 
                                           from T_ENG_BOM where FUSEORGID=100026  and FFORBIDSTATUS='A') a
                                           left join T_ENG_BOMCHILD b on a.fid=b.fid
                                           left join t_BD_MaterialBase c on b.FMATERIALID=c.FMATERIALID
                                           where a.rn=1;                                            
                                           with cte as
                                           (
                                           select PWL,WL,FCREATEDATE,2 as CJ from #WLQD 
                                           where WL='{date["FMATERIALID"]}'
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
                                           where a.fmaterialid='{date["FMATERIALID"]}'
                                           )bb
                                           where FNUMBER LIKE '260.02.%'
                                           order by CJ DESC,FCREATEDATE ASC";
                var cxs = DBUtils.ExecuteDynamicObject(ctx, cxsql);
                string delsql = $@"/*dialect*/ delete t_260_WLEntry where FID='{date["FID"]}'";
                DBUtils.Execute(ctx, delsql);
                if (cxs.Count == 0)
                {
                    string upsql = $@"/*dialect*/ update T_ENG_BOM set F_260_ZSJWLWB='' where FID='{date["Id"]}'";
                    DBUtils.Execute(ctx, upsql);
                }
                string str = "";
                string wlname = "";
                foreach (var wl in cxs)
                {
                    i++;
                    long id = 0;
                    string xmhsql = $@"select MIN(FPKID)FPKID FROM t_260_WLEntry";
                    var xmh = DBUtils.ExecuteDynamicObject(ctx, xmhsql);
                    if (xmh.Count > 0)
                    {
                        id += Convert.ToInt64(xmh[0]["FPKID"]);
                    }
                    if ((id - i) == 0)
                    {
                        i++;
                    }
                    str += "(" + Convert.ToString(id - i) + "," + date["FID"].ToString() + "," + wl["PWL"].ToString() + "),";
                    wlname += wl["FNAME"].ToString() + ",";
                }
                if (str != "")
                {
                    string insertsql = $@"/*dialect*/ insert into t_260_WLEntry values {str.Trim(',')}";
                    DBUtils.Execute(ctx, insertsql);
                    string upsql = $@"/*dialect*/ update T_ENG_BOM set F_260_ZSJWLWB='{wlname.Trim(',')}' where FID='{date["FID"]}'";
                    DBUtils.Execute(ctx, upsql);
                }
            }
        }
    }
}
