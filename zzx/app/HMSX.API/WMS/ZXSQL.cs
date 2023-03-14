using Kingdee.BOS.ServiceFacade.KDServiceFx;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.WebApi.ServicesStub;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.API.WMS
{
    public class ZXSQL:AbstractWebApiBusinessService
    {
        public ZXSQL(KDServiceContext context) : base(context)
        {
        }
        //物料批号复检日期
        public object GETWLFJ(string wl)
        {
            if (this.KDContext.Session.AppContext == null)
            {
                throw new Exception("会话超时,登录上下文为空");
            }
            string sql = $"exec zzx_sxwlfj {wl}";
            return DBServiceHelper.ExecuteDataSet(this.KDContext.Session.AppContext, sql);
        }
        //派工领料情况
        public object GETSCLL(string pgid)
        {
            if (this.KDContext.Session.AppContext == null)
            {
                throw new Exception("会话超时,登录上下文为空");
            }
            string sql = $"/*dialect*/select wl.FNUMBER 物料编码,FAvailableQty 已领数量 from t_PgBomInfo a inner join T_BD_MATERIAL wl on a.FMaterialId=wl.FMATERIALID where FPgEntryId in ({pgid})";
            return DBServiceHelper.ExecuteDataSet(this.KDContext.Session.AppContext, sql);
        }
        //标记WMS拉取状态
        public void FXDJ(string table,string id)
        {
            if (this.KDContext.Session.AppContext == null)
            {
                throw new Exception("会话超时,登录上下文为空");
            }
            if (table == "T_SFC_DISPATCHDETAILENTRY")
            {
                string sql = $"/*dialect*/update {table} set F_260_LY='WMS' where FENTRYID in ({id})";
                DBServiceHelper.Execute(this.KDContext.Session.AppContext, sql);
            }
            else
            {
                string sql = $"/*dialect*/update {table} set F_260_XTLY='WMS' where FID in ({id})";
                DBServiceHelper.Execute(this.KDContext.Session.AppContext, sql);
            }           
        }
    }
}
