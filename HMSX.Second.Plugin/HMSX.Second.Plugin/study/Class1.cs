using System;
using Kingdee.BOS.ServiceFacade.KDServiceFx;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.WebApi.ServicesStub;
namespace Jac.XkDemo.BOS.WebApi
{
    /// <summary>
    /// 【WebApi】自定义WebApi接口123 45662233-jajajazzx
    /// </summary>
    public class CustomWebApiService : AbstractWebApiBusinessService
    {
        public CustomWebApiService(KDServiceContext context)
        : base(context)
        {
            //
        }
        /// <summary>
        /// 执行SQL并返回查询结果
        /// </summary>
        /// <param name="sql">SQL脚本</param>
        /// <returns>返回DataSet</returns>
        public object ExecuteDataSet(string sql)
        {
            if (this.KDContext.Session.AppContext == null)
            {
                // 会话超时
                throw new Exception("ctx=null");
            }
            return DBServiceHelper.ExecuteDataSet(this.KDContext.Session.AppContext, sql);
        }
        /// <summary>
        /// 执行SQL并返回查询结果
        /// </summary>
        /// <param name="sql">SQL脚本</param>
        /// <returns>返回字典集合</returns>
        public object ExecuteDynamicObject(string sql)
        {
            return DBServiceHelper.ExecuteDynamicObject(this.KDContext.Session.AppContext, sql);
        }
    }
}


