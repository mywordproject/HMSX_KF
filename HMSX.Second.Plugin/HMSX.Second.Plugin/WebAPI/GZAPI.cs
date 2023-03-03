using Kingdee.BOS.App.Data;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceFacade.KDServiceFx;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.WebApi.ServicesStub;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.WebAPI
{
    public class GZAPI: AbstractWebApiBusinessService
    {
        public GZAPI(KDServiceContext context) : base(context)
        {
        }
        /// <summary>
        /// 过站查询
        /// </summary>
        /// <param FBILLNO="e">来料批次</param>
        /// <param FLLBM="e">来料编码</param>
        /// <param FLLSMSJ="e">来料扫码时间</param>
        /// <param F_CPCODE="e">产品编码</param>
        /// <param FLDJ="e">镭雕二维码</param>
        /// <param FLDSJ="e">镭雕时间</param>
        /// <param FLDSN="e">镭雕SN码</param>
        /// <param FQDCSN="e">去镀层SN码</param>
        /// <param FQDCSJ="e">去镀层时间</param>
        /// <param FHJ="e">焊接</param>
        /// <param FHJSJ="e">焊接时间</param>
        /// <param FTM="e">贴膜</param>
        /// <param FTMSJ="e">贴膜时间</param>
        /// <param FSMSJ="e">二维码时间</param>
        /// <param FXM="e">箱码</param>
        /// <param FBZSJ="e">包装时间</param>
        /// <param F_CODE="e">二维码</param>
        /// <param FPM="e">盘码</param>
        /// <param F_SMTIME="e">扫码时间</param>
        /// <param F_ZYRY="e">作业人员</param>
        public object GETGZXX(string FBILLNO,string FLLBM,string FLLSMSJ,string F_CPCODE, string FLDJ,
                              string FLDSJ, string FLDSN, string FQDCSN, string FQDCSJ, string FHJ,
                              string FHJSJ, string FTM, string FTMSJ, string FSMSJ, string FXM,
                              string FBZSJ, string F_CODE, string FPM, string F_SMTIME, string F_ZYRY)
        {
            if (this.KDContext.Session.AppContext == null)
            {
                throw new Exception("会话超时,登录上下文为空");
            }
            DynamicObjectCollection dates =null;
            JObject jsonRoot = new JObject();
            JArray Rows = new JArray();
            bool success = true;
            try
            {
                string cxsql = $@"select * from keed_t_Cust100336 a
                             inner join keed_t_Cust_Entry100321 b on a.fid=b.fid
                             where (FBILLNO='{FBILLNO}'or '{FBILLNO}'='')
                             and   ( FLLBM='{FLLBM}' or '{FLLBM}'='')
                             and   ( FLLSMSJ='{FLLSMSJ}' or '{FLLSMSJ}'='')
                             and   ( F_CPCODE='{F_CPCODE}' or '{F_CPCODE}'='')
                             and   ( FLDJ='{FLDJ}' or '{FLDJ}'='')
                             and   ( FLDSJ='{FLDSJ}' or '{FLDSJ}'='')
                             and   ( FLDSN='{FLDSN}' or '{FLDSN}'='')
                             and   ( FQDCSN='{FQDCSN}'  or '{FQDCSN}'='')
                             and   ( FQDCSJ='{FQDCSJ}' or '{FQDCSJ}'='')
                             and   ( FHJ='{FHJ}' or '{FHJ}'='')
                             and   ( FHJSJ='{FHJSJ}' or '{FHJSJ}'='')
                             and   ( FTM='{FTM}' or '{FTM}'='')
                             and   ( FTMSJ='{FTMSJ}' or '{FTMSJ}'='')
                             and   ( FSMSJ='{FSMSJ}' or '{FSMSJ}'='')
                             and   ( FXM='{FXM}' or '{FXM}'='')
                             and   ( FBZSJ='{FBZSJ}' or '{FBZSJ}'='')
                             and   ( F_CODE='{F_CODE}' or '{F_CODE}'='')
                             and   ( FPM='{FPM}' or '{FPM}'='')
                             and   ( F_SMTIME='{F_SMTIME}' or '{F_SMTIME}'='')
                             and   ( F_ZYRY='{F_ZYRY}' or '{F_ZYRY}'='')";
                dates=DBUtils.ExecuteDynamicObject(this.KDContext.Session.AppContext, cxsql);
                //构造Json                          
                foreach(var date in dates)
                {
                    JObject Row = new JObject();
                    Row.Add("FBILLNO", date["FBILLNO"] == null ? "" : date["FBILLNO"].ToString());
                    Row.Add("FDOCUMENTSTATUS", date["FDOCUMENTSTATUS"] == null ? "" : date["FDOCUMENTSTATUS"].ToString());
                    Row.Add("FLLBM", date["FLLBM"] == null ? "" : date["FLLBM"].ToString());
                    Row.Add("F_KEED_CREATEDATE", date["F_KEED_CREATEDATE"] == null ? "" : date["F_KEED_CREATEDATE"].ToString());
                    Row.Add("F_KEED_MODIFYDATE", date["F_KEED_MODIFYDATE"] == null ? "" : date["F_KEED_MODIFYDATE"].ToString());
                    Row.Add("FLLSMSJ", date["FLLSMSJ"] == null ? "" : date["FLLSMSJ"].ToString());
                    Row.Add("F_CPCODE", date["F_CPCODE"] == null ? "" : date["F_CPCODE"].ToString());
                    Row.Add("FLDJ", date["FLDJ"] == null ? "" : date["FLDJ"].ToString());
                    Row.Add("FLDSJ", date["FLDSJ"] == null ? "" : date["FLDSJ"].ToString());
                    Row.Add("FLDSN", date["FLDSN"] == null ? "" : date["FLDSN"].ToString());
                    Row.Add("FQDCSN", date["FQDCSN"] == null ? "" : date["FQDCSN"].ToString());
                    Row.Add("FQDCSJ", date["FQDCSJ"] == null ? "" : date["FQDCSJ"].ToString());
                    Row.Add("FHJ", date["FHJ"] == null ? "" : date["FHJ"].ToString());
                    Row.Add("FHJSJ", date["FHJSJ"] == null ? "" : date["FHJSJ"].ToString());
                    Row.Add("FTM", date["FTM"] == null ? "" : date["FTM"].ToString());
                    Row.Add("FTMSJ", date["FTMSJ"] == null ? "" : date["FTMSJ"].ToString());
                    Row.Add("FSMSJ", date["FSMSJ"] == null ? "" : date["FSMSJ"].ToString());
                    Row.Add("FXM", date["FXM"] == null ? "" : date["FXM"].ToString());
                    Row.Add("FBZSJ", date["FBZSJ"] == null ? "" : date["FBZSJ"].ToString());
                    Row.Add("F_CODE", date["F_CODE"] == null ? "" : date["F_CODE"].ToString());
                    Row.Add("FPM", date["FPM"] == null ? "" : date["FPM"].ToString());
                    Row.Add("F_SMTIME", date["F_SMTIME"] == null ? "" : date["F_SMTIME"].ToString());
                    Row.Add("F_ZYRY", date["F_ZYRY"] == null ? "" : date["F_ZYRY"].ToString());
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
