using Kingdee.BOS.App.Data;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceFacade.KDServiceFx;
using Kingdee.BOS.WebApi.ServicesStub;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.WebAPI
{
    public class PGCXAPI : AbstractWebApiBusinessService
    {
        public PGCXAPI(KDServiceContext context) : base(context)
        {
        }
        //派工时间
        public object GETPGXX(string zz, string rq)
        {
            if (this.KDContext.Session.AppContext == null)
            {
                throw new Exception("会话超时,登录上下文为空");
            }
            JObject jsonRoot = new JObject();
            JArray Rows = new JArray();
            bool success = true;
            long i = 0;
            try
            {
                string cxsql = $@"select A.FID,FBILLNO,MAX(FDISPATCHTIME) FDISPATCHTIME from T_SFC_DISPATCHDETAIL a
                                 left join T_SFC_DISPATCHDETAILENTRY b on a.fid=b.fid
                                 left join t_BD_MaterialProduce c  on a.FMATERIALID=c.fmaterialid
                                 WHERE F_SBID_ORGID={zz} AND FDISPATCHTIME > '2022-10-25' AND F_260_LY=''  and c.FWORKSHOPID='146867'
                                 GROUP BY A.FID,FBILLNO
                                 order by FDISPATCHTIME ";
                var cxs = DBUtils.ExecuteDynamicObject(this.KDContext.Session.AppContext, cxsql);           
                foreach (var cx in cxs)
                {
                    i++;
                    JObject Row = new JObject();
                    Row.Add("XH", i);
                    Row.Add("FID", cx["FID"].ToString());
                    Row.Add("FBILLNO", cx["FBILLNO"].ToString());
                    Row.Add("FDISPATCHTIME", cx["FDISPATCHTIME"].ToString());
                    Rows.Add(Row);                    
                }
            }
            catch
            {
                success = false;
                Rows = null;
            }
            finally
            {
                jsonRoot.Add("IsSuccess", success);
                jsonRoot.Add("Rows", i);
                jsonRoot.Add("Data", Rows);
            }
            return jsonRoot;
        }
        //派工明细中间表
        public object GETZJBXX(string pgid)
        {
            if (this.KDContext.Session.AppContext == null)
            {
                throw new Exception("会话超时,登录上下文为空");
            }
            JObject jsonRoot = new JObject();
            JArray Rows = new JArray();
            bool success = true;
            long i = 0;
            try
            {
                string cxsql = $@"
                  select B.FNUMBER,A.FPgEntryId,A.FPPBomId,A.FPPBomEntryId,A.FMustQty,A.FAvailableQty from t_PgBomInfo a
                  left join T_BD_MATERIAL b on a.FMaterialId=b.FMATERIALID
                  where a.FPgEntryId IN ({pgid}) ";
                var cxs = DBUtils.ExecuteDynamicObject(this.KDContext.Session.AppContext, cxsql);
                foreach (var cx in cxs)
                {
                    i++;
                    JObject Row = new JObject();
                    Row.Add("XH", i);
                    Row.Add("FNUMBER", cx["FNUMBER"].ToString());
                    Row.Add("FPgEntryId", cx["FPgEntryId"].ToString());
                    Row.Add("FPPBomId", cx["FPPBomId"].ToString());
                    Row.Add("FPPBomEntryId", cx["FPPBomEntryId"].ToString());
                    Row.Add("FMustQty", cx["FMustQty"].ToString());
                    Row.Add("FAvailableQty", cx["FAvailableQty"].ToString());
                    Rows.Add(Row);
                }
            }
            catch
            {
                success = false;
                Rows = null;
            }
            finally
            {
                jsonRoot.Add("IsSuccess", success);
                jsonRoot.Add("Rows", i);
                jsonRoot.Add("Data", Rows);
            }
            return jsonRoot;
        }
        //派工明细      
        public object GETPGMX(string zz, string rq)
        {
            if (this.KDContext.Session.AppContext == null)
            {
                throw new Exception("会话超时,登录上下文为空");
            }
            JObject jsonRoot = new JObject();
            JArray Rows = new JArray();
            bool success = true;
            long i = 0;
            try
            {
                string cxsql = $@"select a.FID,a.FBILLNO,b.FENTRYID,b.FBARCODE,FDiSPatchtime,a.FoptplanNo,A.FSEQNUMBER,A.FOPERNUMBER,a.fmobillno,a.FMOSEQ
                               from T_SFC_DISPATCHDETAIL A,T_SFC_DISPATCHDETAILENTRY B ,T_BD_MATERIAL M
                               where A.Fid = B.Fid  and A.FMATERIALID = M.FMATERIALID
                               and F_SBID_ORGID='{zz}' AND FDISPATCHTIME > '2022-10-25' AND F_260_LY=''
                               order by A.FID";
                var cxs = DBUtils.ExecuteDynamicObject(this.KDContext.Session.AppContext, cxsql);
                var FFID = (from p in cxs.ToList<DynamicObject>() select new { FID = Convert.ToString(p["FID"]), FBILLNO = Convert.ToString(p["FBILLNO"])}).Distinct().ToList();
                foreach (var cx in FFID)
                {
                    i++;
                    JObject Row = new JObject();
                    Row.Add("XH", i);
                    Row.Add("FID", cx.FID);
                    Row.Add("FBILLNO", cx.FBILLNO);
                    JArray fentity = new JArray();
                    var FFENTRYID = (from p in cxs.ToList<DynamicObject>() where Convert.ToString(p["FID"]) == cx.FID select p);
                    int j = 0;
                    foreach (var entry in FFENTRYID)
                    {
                        j++;
                        JObject entity = new JObject();
                        entity.Add("FSEQ", j);
                        entity.Add("FENTRYID", entry["FENTRYID"].ToString());
                        entity.Add("FBARCODE", entry["FBARCODE"].ToString());
                        fentity.Add(entity);
                    }
                    Row.Add("Fentity", fentity);
                    Rows.Add(Row);
                }
            }
            catch
            {
                success = false;
                Rows = null;
            }
            finally
            {
                jsonRoot.Add("IsSuccess", success);
                jsonRoot.Add("Rows", i);
                jsonRoot.Add("Data", Rows);
            }
            return jsonRoot;
        }
        //获取数据库时间     
        public object GETTIME()
        {
            if (this.KDContext.Session.AppContext == null)
            {
                throw new Exception("会话超时,登录上下文为空");
            }
            JObject jsonRoot = new JObject();
            JArray Rows = new JArray();
            bool success = true;
            try
            {

                    JObject Row = new JObject();
                    Row.Add("TIME", DateTime.Now.ToString());                                               
                    Rows.Add(Row);
              
            }
            catch
            {
                success = false;
                Rows = null;
            }
            finally
            {
                jsonRoot.Add("IsSuccess", success);
                jsonRoot.Add("Data", Rows);
            }
            return jsonRoot;
        }
    }
}
