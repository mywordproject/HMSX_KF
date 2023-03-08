using HMSX.Second.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.Tool
{
    public class Results
    {
        //获取钉钉登录身份返回信息类 
        public class DING_Token
        {
            public int Errcode { get; set; }
            public string  Access_token { get; set; }
            public string Errmsg { get; set; }

        }
        //获取钉钉登录单个审批信息

        public class Data2
        {
            public List<Body> body { get; set; }

        }
        //获取登录身份返回信息类 日报
        public class GetToken
        {
            public int Code { get; set; }
            public Data Data { get; set; }
            public string Msg { get; set; }
         
        }

        public class Data
        {
            public string AccessToken { get; set; }
            public List<Rootobject> DataList { get; set; }
           
        }
        //获取登录身份返回信息类 周报
        public class GetToken1
        {
            public int Code { get; set; }
            public Data1 Data { get; set; }
            public string Msg { get; set; }
        }

        public class Data1
        {          
            public List<Rootobject1> DataList { get; set; }
        }

        //基础资料返回结果类
        public class Response
        {
            public string code { get; set; }
            public string message { get; set; }
            public string[] messageData { get; set; }
            public string[] data { get; set; }
        }


        public class Response_PRODUCT
        {
            public string code { get; set; }
            public string message { get; set; }
            public string[] messageData { get; set; }
            public Row_OrderQty[] data { get; set; }
        }

        public class Row_OrderQty
        {
            public string erpWorkOrderNumber { get; set; }
            public int seq { get; set; }
            public double quantity { get; set; }
        }


    }


}
