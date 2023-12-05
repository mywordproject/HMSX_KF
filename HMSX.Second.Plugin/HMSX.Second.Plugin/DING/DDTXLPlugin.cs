using DingTalk.Api;
using DingTalk.Api.Request;
using DingTalk.Api.Response;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HMSX.Second.Plugin.Tool.Results;

namespace HMSX.Second.Plugin.DING
{
    class DDTXLPlugin
    {
        static SqlConnection conn;
        static void Main1(string[] args)
        {
            //登录
            string strDataBase = "Server=10.40.1.23;DataBase=SX_DataAdapt;Uid=sxkf;pwd=sxkf123A;";
            conn = new SqlConnection(strDataBase);
            conn.Open();
            string gzxxsql1 = $@"/*dialect*/delete hmsx_DDYG where RQ='{DateTime.Now.ToString("yyyy-MM-dd")}' 
               OR RQ='{DateTime.Now.AddDays(-60).ToString("yyyy-MM-dd")}';  ";
            SqlCommand sqlcmd1 = new SqlCommand(gzxxsql1, conn);
            var cont = sqlcmd1.ExecuteNonQuery();

            string gzxxsql2 = $@"/*dialect*/delete hmsx_DDBM";
            SqlCommand sqlcmd2 = new SqlCommand(gzxxsql2, conn);
            var cont1 = sqlcmd2.ExecuteNonQuery();
            conn.Close();
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
            YHXX(date, access_token);
            bool T = true;
            while (T)
            {
                try
                {
                   // long x = date.Result.NextCursor;
                    if (date.Result==null || date.Result.NextCursor==0)
                    {
                        T = false;
                    }
                    else
                    {
                        long x = date.Result.NextCursor;
                        Console.WriteLine(x);
                        date = ZZYG(x, access_token);
                        YHXX(date, access_token);
                    }
                }
                catch
                {

                }
            }
            List<long> bms = new List<long>();
            bms.Add(544844262);
            bms.Add(576667036);
            BMID(access_token, bms);
        }
        //获取再职员工
        static OapiSmartworkHrmEmployeeQueryonjobResponse ZZYG(long Y, string token)
        {
            IDingTalkClient client = new DefaultDingTalkClient("https://oapi.dingtalk.com/topapi/smartwork/hrm/employee/queryonjob");
            OapiSmartworkHrmEmployeeQueryonjobRequest req = new OapiSmartworkHrmEmployeeQueryonjobRequest();
            req.StatusList = "2,3,5,-1";
            req.Offset = Y;
            req.Size = 50;
            OapiSmartworkHrmEmployeeQueryonjobResponse rsp = client.Execute(req, token);
            return rsp;
        }
        static int j = 1;
        //获取用户信息
        static IDingTalkClient clientYH = new DefaultDingTalkClient("https://oapi.dingtalk.com/topapi/v2/user/get");
        static OapiV2UserGetRequest reqYH = new OapiV2UserGetRequest();
        static void YHXX(OapiSmartworkHrmEmployeeQueryonjobResponse DA, string token)
        {
            string strsql = "";
            if (DA.Result != null)
            {
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
                    if (yhxx.result.dept_id_list != null)
                    {
                        foreach (var bmid in yhxx.result.dept_id_list)
                        {
                            bm += bmid + ",";
                        }
                    }
                    if (yhxx.result.name == "LL")
                    {

                    }
                    Console.WriteLine(j + "_" + yhxx.result.name + "_" + yhxx.result.job_number + "_" + rylb);
                    strsql += $@"({j},'{DateTime.Now.ToString("yyyy-MM-dd")}','{yhxx.result.job_number}','{yhxx.result.name}','{rylb}','{gzcs}','{bm.Trim(',')}'),";
                    j++;
                }
                string strDataBase = "Server=10.40.1.23;DataBase=SX_DataAdapt;Uid=sxkf;pwd=sxkf123A;";
                conn = new SqlConnection(strDataBase);
                conn.Open();
                string gzxxsql = $@"insert into hmsx_DDYG values {strsql.Trim(',')}";
                SqlCommand sqlcmd = new SqlCommand(gzxxsql, conn);
                var cont = sqlcmd.ExecuteNonQuery();
                conn.Close();
            }
        }
        static void BMID(string token, List<long> BMS)
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
                Console.WriteLine(BM);
                if (rsp.Result != null && rsp.Result.DeptIdList != null)
                {
                    foreach (var bmid in rsp.Result.DeptIdList)
                    {
                        strbm.Add(bmid);
                        str += "('" + bmid + "'),";
                        Console.WriteLine(bmid);
                    }
                    if (str != "")
                    {
                        string strDataBase = "Server=10.40.1.23;DataBase=SX_DataAdapt;Uid=sxkf;pwd=sxkf123A;";
                        conn = new SqlConnection(strDataBase);
                        conn.Open();
                        string gzxxsql = $@"insert into hmsx_DDBM values {str.Trim(',')}";
                        SqlCommand sqlcmd = new SqlCommand(gzxxsql, conn);
                        var cont = sqlcmd.ExecuteNonQuery();
                        conn.Close();
                        BMID(token, strbm);
                    }
                }
            }

        }
    }
}
