using System.ComponentModel;
using Kingdee.BOS.Contracts;
using Kingdee.BOS;
using Kingdee.BOS.Core;
using Kingdee.BOS.ServiceHelper;
using DingTalk.Api;
using DingTalk.Api.Request;
using DingTalk.Api.Response;
using static HMSX.Second.Plugin.Tool.Results;
using Newtonsoft.Json;
using Kingdee.BOS.App.Data;
using System;
using System.Collections.Generic;

namespace HMSX.Second.Plugin.ZXJH
{
    public class DDTXLServicePlugin : IScheduleService
    {
        public void Run(Context ctx, Schedule schedule)
        {
            string gzxxsql = $@"/*dialect*/delete SX_DataAdapt.dbo.hmsx_DDYG where RQ='{DateTime.Now.ToString("yyyy-MM-dd")}' 
               OR RQ='{DateTime.Now.AddDays(-60).ToString("yyyy-MM-dd")}';  ";
            DBUtils.Execute(ctx, gzxxsql);
            string gzxxsql2 = $@"/*dialect*/delete SX_DataAdapt.dbo.hmsx_DDBM";
            DBUtils.Execute(ctx, gzxxsql2);
            //登录
            IDingTalkClient dlclient = new DefaultDingTalkClient("https://oapi.dingtalk.com/gettoken");
            OapiGettokenRequest dlreq = new OapiGettokenRequest();
            dlreq.Appkey = "ding4zkzmf7yz0l5neya";
            dlreq.Appsecret = "xRmBxtjJQh9DwPGrz8HG9hMWqw0xOj9IySBvZR1Ga0iVoQ1YwA1PmFFDxhEAgSvJ";
            dlreq.SetHttpMethod("GET");
            OapiGettokenResponse dlrsp = dlclient.Execute(dlreq);
            DING_Token get = new DING_Token();
            get = JsonConvert.DeserializeObject<DING_Token>(dlrsp.Body);
            string access_token = get.Access_token;
            OapiSmartworkHrmEmployeeQueryonjobResponse date = ZZYG(0, access_token);
            YHXX(date, access_token, ctx);
            bool T = true;
            while (T)
            {
                try
                {
                    long x = date.Result.NextCursor;
                    if (x == 0)
                    {
                        T = false;
                    }
                    else
                    {
                        date = ZZYG(x, access_token);
                        YHXX(date, access_token,ctx);
                    }
                }
                catch
                {

                }
            }
            List<long> bms = new List<long>();
            bms.Add(544844262);
            bms.Add(576667036);
            BMID(access_token, bms,ctx);

        }
        public OapiSmartworkHrmEmployeeQueryonjobResponse ZZYG(long Y, string token)
        {
            IDingTalkClient client = new DefaultDingTalkClient("https://oapi.dingtalk.com/topapi/smartwork/hrm/employee/queryonjob");
            OapiSmartworkHrmEmployeeQueryonjobRequest req = new OapiSmartworkHrmEmployeeQueryonjobRequest();
            req.StatusList = "2,3,5,-1";
            req.Offset = Y;
            req.Size = 50;
            OapiSmartworkHrmEmployeeQueryonjobResponse rsp = client.Execute(req, token);
            return rsp;
        }
        public int j = 1;
        public IDingTalkClient clientYH = new DefaultDingTalkClient("https://oapi.dingtalk.com/topapi/v2/user/get");
        public OapiV2UserGetRequest reqYH = new OapiV2UserGetRequest();
        public void YHXX(OapiSmartworkHrmEmployeeQueryonjobResponse DA, string token, Context ctx)
        {
            string strsql = "";
            foreach (var list in DA.Result.DataList)
            {
                //获取名字                
                reqYH.Userid = list;
                OapiV2UserGetResponse rsp = clientYH.Execute(reqYH, token);
                YHXX yhxx = new YHXX();
                yhxx = JsonConvert.DeserializeObject<YHXX>(rsp.Body);
                string rylb = "";
                string gzcs = "";
                if (yhxx.result != null && yhxx.result.ext_attrs != null)
                {
                    foreach (var ext_attrs in yhxx.result.ext_attrs)
                    {
                        if (ext_attrs.name == "人员类别")
                        {
                            rylb = ext_attrs.value.text;
                        }
                        if (ext_attrs.name == "工作场所")
                        {
                            gzcs = ext_attrs.value.text;
                        }
                    }
                }
                string bm = "";
                if (yhxx.result != null && yhxx.result.dept_id_list != null)
                {
                    foreach (var bmid in yhxx.result.dept_id_list)
                    {
                        bm += bmid + ",";
                    }
                }             
                strsql += $@"({j},'{DateTime.Now.ToString("yyyy-MM-dd")}','{yhxx.result.job_number}','{yhxx.result.name}','{rylb}','{gzcs}','{bm.Trim(',')}'),";
                j++;
            }               
            string gzxxsql = $@"/*dialect*/insert into SX_DataAdapt.dbo.hmsx_DDYG values {strsql.Trim(',')}";
            DBUtils.Execute(ctx, gzxxsql);
        }
        public void BMID(string token, List<long> BMS,Context ctx)
        {
            IDingTalkClient client = new DefaultDingTalkClient("https://oapi.dingtalk.com/topapi/v2/department/listsubid");
            OapiV2DepartmentListsubidRequest req = new OapiV2DepartmentListsubidRequest();
            foreach (var BM in BMS)
            {
                string str = "";
                List<long> strbm = new List<long>();
                req.DeptId = BM;
                OapiV2DepartmentListsubidResponse rsp = client.Execute(req, token);
                str += "('" + BM + "'),";
                if (rsp.Result != null && rsp.Result.DeptIdList != null)
                {
                    foreach (var bmid in rsp.Result.DeptIdList)
                    {
                        strbm.Add(bmid);
                        str += "('" + bmid + "'),";                    
                    }
                    if (str != "")
                    {
                        string gzxxsql = $@"/*dialect*/insert into SX_DataAdapt.dbo.hmsx_DDBM values {str.Trim(',')}";
                        DBUtils.Execute(ctx, gzxxsql);
                        BMID(token, strbm,ctx);
                    }
                }
            }
        }
    }
}
