
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static HMSX.Second.Plugin.Tool.Results;

namespace HMSX.Second.Plugin.Tool
{
    public class POST
    {
        //获取身份方法
        public static string post()
        {
            JObject dates = new JObject();
            JObject rb = new JObject();
            rb.Add("appkey", "user001");
            rb.Add("appsecret", "unijkshgsye679nx5s89w");
            dates.Add("Data", rb);
            string json1 = dates.ToString();
            string url = "Http://222.210.102.21:8888/Api/LoginByAppKey";
            GetToken get1 = new GetToken();
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.ContentType = "application/json";
            req.Method = "POST";
            req.Timeout = 10000;
            byte[] data = Encoding.UTF8.GetBytes(json1);
            req.ContentLength = data.Length;
            using (Stream stream = req.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
                stream.Close();
            }
            HttpWebResponse resq = (HttpWebResponse)req.GetResponse();
            Stream sm = resq.GetResponseStream();
            using (StreamReader reader = new StreamReader(sm, Encoding.UTF8))
            {
                get1 = JsonConvert.DeserializeObject<GetToken>(reader.ReadToEnd());
            }
            return get1.Data.AccessToken;
        }

        //接口发送方法 日报
        public static GetToken HttpPost(string json, string url)
        {
             string token = post();
            GetToken get = new GetToken();
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.ContentType = "application/json";
            req.Method = "POST";
            req.Timeout = 10000;
            req.Headers.Add("Authorization", token);
            byte[] data = Encoding.UTF8.GetBytes(json);
            req.ContentLength = data.Length;
            using (Stream stream = req.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
                stream.Close();
            }
            HttpWebResponse resq = (HttpWebResponse)req.GetResponse();
            Stream sm = resq.GetResponseStream();
            using (StreamReader reader = new StreamReader(sm, Encoding.UTF8))
            {
                string res = reader.ReadToEnd();
                get = JsonConvert.DeserializeObject<GetToken>(res);
            }
            return get;
        }
        //接口发送方法 月报
        public static GetToken1 HttpPost1(string json, string url)
        {
            string token = post();
            GetToken1 get = new GetToken1();
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.ContentType = "application/json";
            req.Method = "POST";
            req.Timeout = 100000;
            req.Headers.Add("Authorization", token);
            byte[] data = Encoding.UTF8.GetBytes(json);
            req.ContentLength = data.Length;
            using (Stream stream = req.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
                stream.Close();
            }
            HttpWebResponse resq = (HttpWebResponse)req.GetResponse();
            Stream sm = resq.GetResponseStream();
            using (StreamReader reader = new StreamReader(sm, Encoding.UTF8))
            {
                string res = reader.ReadToEnd();
                get = JsonConvert.DeserializeObject<GetToken1>(res);
            }
            return get;
        }



        //设置单据头方法
        public static void SetHeaderValue(WebHeaderCollection header, string name, string value)
        {
            var property = typeof(WebHeaderCollection).GetProperty("InnerCollection", BindingFlags.Instance | BindingFlags.NonPublic);
            if (property != null)
            {
                var collection = property.GetValue(header, null) as NameValueCollection;
                collection[name] = value;
            }
        }
    }
}
