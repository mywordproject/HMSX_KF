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
    public class XNYGZXXAPI: AbstractWebApiBusinessService
    {
        public XNYGZXXAPI(KDServiceContext context) : base(context)
        {
        }
        public object GETGZXX(string cpbm)
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
                string cxsql = $@"select a.FID,b.FENTRYID from PAEZ_t_Cust100420 a
                              left join PAEZ_t_Cust_Entry100521 b on a.fid=b.fid
                                where F_260_CPM ='{cpbm}'";
                 var dates = DBUtils.ExecuteDynamicObject(this.KDContext.Session.AppContext, cxsql);
                //构造Json                          
                foreach (var date in dates)
                {
                    JObject Row = new JObject();
                    Row.Add("FID", date["FID"] == null ? "" : date["FID"].ToString());
                    Row.Add("FENTRYID", date["FENTRYID"] == null ? "" : date["FENTRYID"].ToString());
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
                jsonRoot.Add("Data", Rows);
            }
            return jsonRoot;
        }
        public object GETJG(string cpbm)
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
                string cxsql = $@"select a.FID,b.FENTRYID,F_260_CPM,F_260_HJJG,F_260_DGNJG,F_260_TMJG from PAEZ_t_Cust100420 a
                              left join PAEZ_t_Cust_Entry100521 b on a.fid=b.fid
                                where F_260_CPM ='{cpbm}'";
                var dates = DBUtils.ExecuteDynamicObject(this.KDContext.Session.AppContext, cxsql);
                //构造Json                          
                foreach (var date in dates)
                {
                    JObject Row = new JObject();
                    Row.Add("FID", date["FID"] == null ? "" : date["FID"].ToString());
                    Row.Add("FENTRYID", date["FENTRYID"] == null ? "" : date["FENTRYID"].ToString());
                    Row.Add("F_260_CPM", date["F_260_CPM"] == null ? "" : date["F_260_CPM"].ToString());
                    Row.Add("F_260_HJJG", date["F_260_HJJG"] == null ? "" : date["F_260_HJJG"].ToString());
                    Row.Add("F_260_DGNJG", date["F_260_DGNJG"] == null ? "" : date["F_260_DGNJG"].ToString());
                    Row.Add("F_260_TMJG", date["F_260_TMJG"] == null ? "" : date["F_260_TMJG"].ToString());
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
                jsonRoot.Add("Data", Rows);
            }
            return jsonRoot;
        }

    }
}
