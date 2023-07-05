using Kingdee.BOS.App.Data;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceFacade.KDServiceFx;
using Kingdee.BOS.WebApi.ServicesStub;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.WebAPI
{
    /// <summary>
    /// 设备稼动记录
    /// </summary>
    public class SBJDApi : AbstractWebApiBusinessService
    {
        public SBJDApi(KDServiceContext context) : base(context)
        {
        }
        public object GetSBJD(string kssj, string jssj)
        {
            if (this.KDContext.Session.AppContext == null)
            {
                throw new Exception("会话超时,登录上下文为空");
            }
            List<object> result = new List<object>();
            DynamicObjectCollection dates = null;
            bool success = true;
            try
            {
                string strsql = $@"/*dialect*/ 
                       select TagName 设备名称,convert(datetime,LogDate+' '+LogTime)时间,LastValue 状态 
                       from OPENDATASOURCE('SQLOLEDB','DATA Source=10.42.70.253;User ID=sa;Password=sxkf123A').WASQL.dbo.BwAnalogTable where convert(datetime,LogDate+' '+LogTime) >'{kssj}' 
                                        and convert(datetime,LogDate+' '+LogTime) <='{jssj}' 
                                        order by LogDate desc,LogTime desc";
                 dates = DBUtils.ExecuteDynamicObject(this.KDContext.Session.AppContext, strsql);
            }
            catch
            {
                success = false;
            }
            finally
            {
                result.Add(success);
                result.Add(dates);
            }
            return result;
        }
    }
}
