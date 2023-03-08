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
    //过站信息
    public class GZXX : AbstractWebApiBusinessService
    {
        public GZXX(KDServiceContext context) : base(context)
        {
        }
        public object GETGZXX(string code, string fbillno)
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
                string gzxxsql=$@"select 
                               FBILLNO, 
                                F_CODE, 
                                 a.FID,
                               FLLSMSJ,
                               FENTRYID,
                               CASE WHEN FENTRYID IS NULL THEN 0 ELSE 1 END ID
                               from keed_t_Cust100336 a
                               left join keed_t_Cust_Entry100321 b on a.FID = b.FID and F_CODE = '{code}'
                               where FBILLNO = '{fbillno}'";
                 var gzxxs= DBUtils.ExecuteDynamicObject(this.KDContext.Session.AppContext, gzxxsql);
                foreach (var gzxx in gzxxs)
                {
                    i++;
                    JObject Row = new JObject();
                    Row.Add("XH", i);              
                    Row.Add("FBILLNO", gzxx["FBILLNO"] == null ? "" : gzxx["FBILLNO"].ToString());
                    Row.Add("F_CODE", gzxx["F_CODE"]==null?"":gzxx["F_CODE"].ToString());
                    Row.Add("FID", gzxx["FID"] == null ? "" : gzxx["FID"].ToString());
                    Row.Add("FLLSMSJ", gzxx["FLLSMSJ"] == null ? "" : gzxx["FLLSMSJ"].ToString());
                    Row.Add("FENTRYID", gzxx["FENTRYID"] == null ? "" : gzxx["FENTRYID"].ToString());
                    Row.Add("ID", gzxx["ID"] == null ? "" : gzxx["ID"].ToString());
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
    }

}
