using Kingdee.BOS;
using Kingdee.BOS.JSON;
using Kingdee.BOS.WebApi.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace HMSX.Second.Plugin
{
    class Class1
    {
        //static void Main(string[] args)
        //{
        //    K3CloudApiClient client = new K3CloudApiClient("http://10.42.4.211/k3cloud/");
        //    client.ValidateLogin("646585ba560dc4", "王锋", "666666", 2052);
        //    var result = client.Execute<Object>("HMSX.Second.Plugin.WebAPI.SBJDApi.GetSBJD,HMSX.Second.Plugin", new object[] { "2023-05-25 10:30:00", "2023-05-25 10:40:00" });
        //    Console.WriteLine(result.ToString());
        //    Console.ReadKey();
            //    K3CloudApiClient client = new K3CloudApiClient("http://10.41.1.54/k3cloud/");
            //    var loginResult = client.ValidateLogin("60d1d4bd412569", "李德飞", "wwy111", 2052);
            //    var resultType = JObject.Parse(loginResult)["LoginResultType"].Value<int>();
            //    //登录结果类型等于1，代表登录成功
            //    if (resultType == 1)
            //    {
            //        object[] paramInfo = new object[]
            //         {
            //            "{\"FormId\":\"HMD_MJSQD\","+// 
            //            "\"TopRowCount\":0,"+// 最多允许查询的数量，0或者不要此属性表示不限制
            //            "\"Limit\":10,"+// 分页取数每页允许获取的数据，最大不能超过2000
            //            "\"StartRow\":0,"+// 分页取数开始行索引，从0开始，例如每页10行数据，第2页开始是10，第3页开始是20
            //            "\"FilterString\":\"FID='107133'\","+// 过滤条件
            //            "\"OrderString\":\"FID ASC\"," + // 排序条件     
            //             "\"FieldKeys\":\"FID,F_HMD_CPTP\"}"// 获取采购订单数据参数，内码，供应商id，物料id,物料编码，物料名称
            //         };
            //        List<List<object>> ret = client.Execute<List<List<object>>>("Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.ExecuteBillQuery", paramInfo);
            //        if (ret != null && ret.Count > 0)
            //        {
            //            string s = ret[0][1] as string;
            //            var dataObj = new
            //            {
            //                FileId = s,
            //                StartIndex = 0
            //            };
            //            var data = JsonConvert.SerializeObject(dataObj);
            //            var result = client.AttachmentDownLoad(data);
            //            Console.WriteLine(result);
            //            var responseDto = ResponseDto.Parse(result);
            //            var filePath = @"G:\图片\8.png";
            //            if (responseDto.Result.IsLast)
            //            {
            //                var fileBytes = Convert.FromBase64String(responseDto.Result.FilePart);
            //                File.WriteAllBytes(filePath, fileBytes);
            //            }
            //        }
            //    }
        //}
        #region ResponseDto
        public class ResponseDto
        {
            #region method
            /// <summary>
            /// 将当前对象序列化为Json字符串
            /// </summary>
            /// <returns></returns>
            public virtual string ToJson()
            {
                return JsonConvert.SerializeObject(this);
            }
            /// <summary>
            /// 将Json字符串反序列化为指定对象
            /// </summary>
            /// <param name="json"></param>
            /// <returns></returns>
            public static ResponseDto Parse(string json)
            {
                return JsonConvert.DeserializeObject<ResponseDto>(json);
            }
            #endregion
            #region property
            /// <summary>
            /// 响应结果
            /// </summary>
            public ResponseResult Result { get; set; }
            #endregion
            #region class
            public class ResponseResult
            {
                public string Id { get; set; }
                public long StartIndex { get; set; }
                public bool IsLast { get; set; }
                public int FileSize { get; set; }
                public string FileName { get; set; }
                public string FilePart { get; set; }
                public string Message { get; set; }
                public ResponseResultStatus ResponseStatus { get; set; }
                public class ResponseResultStatus
                {
                    public string MsgCode { get; set; }
                    public bool IsSuccess { get; set; }
                    public string ErrorCode { get; set; }
                    public System.Collections.Generic.IList<ResponseMessage> Errors { get; set; }
                    public System.Collections.Generic.IList<ResponseMessage> SuccessMessages { get; set; }
                    public System.Collections.Generic.IList<SuccessEntity> SuccessEntitys { get; set; }
                    public class ResponseMessage
                    {
                        public string FieldName { get; set; }
                        public string Message { get; set; }
                        public int DIndex { get; set; }
                    }
                    public class SuccessEntity
                    {
                        public string Id { get; set; }
                        public string Number { get; set; }
                        public string BillNo { get; set; }
                        public int DIndex { get; set; }
                    }
                }
                #endregion
            }
            #endregion
        }
    }
}

//K3CloudApiClient client = new K3CloudApiClient("http://10.41.1.87/k3cloud/");
//var a = client.ValidateLogin("63e9834523dea1", "王锋", "666666", 2052);
//var result = client.Execute<Object>("HMSX.Second.Plugin.WebAPI.GZXX.GETGZXX,HMSX.Second.Plugin", new object[] { "H806-37834-03122091402153MHN22K0377-DXXXXXXXX","N22K0377-DXXXXXXXX" });
//Console.WriteLine(result.ToString());
//Console.ReadKey();
//RestSharp.RestClient client = new RestSharp.RestClient();
//client.BaseUrl = url;
//client.Authenticator = new RestSharp.HttpBasicAuthenticator("username", "password");

//RestSharp.RestRequest request = new RestSharp.RestRequest();
//request.Method = RestSharp.Method.POST;
//request.AddFile("projectFileAddOrEdit", targetZipProjectFile);
//request.AddParameter("active", "add");
//request.AddParameter("projectName", fileName);
//request.AddParameter("projectDescription", fileName);
//request.AddParameter("creatorAccount", Wongoing.RegisteredTools.UserAccount);
//request.AddParameter("projectNumber", fileName);
//request.AddParameter("password", Wongoing.RegisteredTools.UserPassword);
//RestSharp.IRestResponse response = client.Execute(request);
//RestClient client = new RestSharp.RestClient();
//string x = "";

//Encoding encoding = Encoding.UTF8;
//HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://10.42.99.50:8026/api/BillProcess/BillClose?");
//request.Method = "POST";
//request.ContentType = "application/json; charset=UTF-8";
//request.Headers["Accept-Encoding"] = "gzip, deflate";
//request.AutomaticDecompression = DecompressionMethods.GZip;
//JObject jsonRoot = new JObject();
//jsonRoot.Add("fbillno", "DBSQ26020221100013");
//jsonRoot.Add("fbilltype", "调拨申请单");
//byte[] buffer = encoding.GetBytes(jsonRoot.ToString());
//request.ContentLength = buffer.Length;
//request.GetRequestStream().Write(buffer, 0, buffer.Length);
//HttpWebResponse response = (HttpWebResponse)request.GetResponse();
//using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
//{
//     x = reader.ReadToEnd();//利用StreamReader就可以从响应内容从头读到尾
//}
//Console.WriteLine(x);
//Console.ReadKey();
//var client = new RestClient("http://10.42.99.50:8026/api/BillProcess/BillUnaudit?");
//RestRequest request = new RestRequest();
//request.Method = Method.Post;
//request.AddHeader("Content-Type", "application/json");
//request.AddParameter("application/json", "{\"fbillno\":\"DBSQ26020221100013\",\"fbilltype\":\"调拨申请单\"}", ParameterType.RequestBody);
//RestSharp.RestResponse response = client.Execute(request);
//var client1 = new RestClient("http://10.42.99.50:8026/api/BillProcess/BillUnauditCheck?");
////client.Timeout = -1;
//RestRequest request1 = new RestRequest();
//request1.Method = Method.Post;
//request1.AddHeader("Content-Type", "application/json");
//JObject jsonRoot = new JObject();
//jsonRoot.Add("fbillno", "DBSQ26020221100013");
//jsonRoot.Add("fbilltype", "调拨申请单");
//request1.AddParameter("application/json",jsonRoot.ToString(), ParameterType.RequestBody);
//RestSharp.RestResponse response1 = client1.Execute(request1);
//Body jsonDatas = JsonConvert.DeserializeObject<Body>(response1.Content);   
//Console.WriteLine(response1.Content);
//Console.WriteLine(response.Content);
//Console.ReadKey();;

//    var test = new List<MesStock> {
//        new MesStock { Deid=1,Pno=1,Sno=1,TotalNums=1},
//        new MesStock { Deid=1,Pno=1,Sno=1,TotalNums=1},
//        new MesStock { Deid=1,Pno=1,Sno=1,TotalNums=11},
//        new MesStock {Deid=2,Pno=2,Sno=2,TotalNums=2},
//        new MesStock {Deid=2,Pno=2,Sno=2,TotalNums=2},
//        new MesStock {Deid=2,Pno=2,Sno=2,TotalNums=33},
//        new MesStock {Deid=3,Pno=3,Sno=3,TotalNums=3},
//    };
//    List<MesStock> res = new List<MesStock>();

//res = test.GroupBy(x => new { x.Deid, x.Pno, x.Sno }).
//    Select(group => new MesStock
//    {
//        Deid = group.Key.Deid,
//        Pno = group.Key.Pno,
//        Sno = group.Key.Sno
//    }).ToList(); 
// res = test.GroupBy((x,y) => new { x.Deid, x.Pno, x.Sno, y.Sum(a => a.TotalNums) }).ToList();
//test.GroupBy(x => new { x.Deid, x.Pno }, (x, y) =>
//{
//    var total = y.Sum(a => a.TotalNums);
//    var Sno = y.Max(a => a.Sno);              
//      var tt = y.Select(t =>
//      {                    
//          t.TotalNums = total;
//          t.Sno = Sno;
//          return t;
//      }).ToList();
//      res.Add(tt.First());
//      return tt;
//}).ToList();

//foreach (var item in res)
//{
//    Console.WriteLine(item.Deid+","+ item.Pno+","+ item.Sno+","+item.TotalNums);
//}
//Console.ReadKey();
//string strDataBase = "Server=10.42.99.67;DataBase=wms_hmsx;Uid=sa;pwd=hmsx!@#456;";
//conn = new SqlConnection(strDataBase);
//conn.Open();
//string gzxxsql = $@"exec sp_kingdee_interface_kct04_box '','','','','','2022-01-01'";
//SqlCommand sqlcmd = new SqlCommand(gzxxsql, conn);
//SqlDataReader cont = sqlcmd.ExecuteReader();
//int hs = 0;
//while (cont.Read())
//{
//    string a = cont["仓库编码"].ToString();
//    string b = cont["条码"] == null ? "" : cont["条码"].ToString();
//}
//cont.Close();
//conn.Close();
//    string xm = cont["userid"].ToString();
//    // this.View.Model.SetValue("F_WLBM", wldm, hs);
//    // this.View.Model.SetValue("F_WLMC", wlmc, hs);
//    // this.View.Model.SetValue("F_GGXH", ggxh, hs );
//    // this.View.Model.SetValue("F_WLID", wlid, hs);
//    // this.View.Model.SetValue("F_PH", phbm, hs );
//    // this.View.Model.SetValue("F_PHID", phid, hs - 1);
//    // this.View.Model.SetValue("F_260_RKSL", sl, hs );
//    hs++;
//}
////conn.Close();
//K3CloudApiClient client = new K3CloudApiClient("http://10.41.1.87/k3cloud/");
//var loginResult = client.ValidateLogin("62cda3a69f579e", "李德飞", "w111111", 2052);
//int resultType = JObject.Parse(loginResult)["LoginResultType"].Value<int>();

//K3CloudApiClient client1 = new K3CloudApiClient("http://10.41.1.87/k3cloud/");
//var loginResult1 = client1.ValidateLogin("62cda3a69f579e", "李德飞", "w111111", 2052);
//int resultType1 = JObject.Parse(loginResult1)["LoginResultType"].Value<int>();

//K3CloudApiClient client2 = new K3CloudApiClient("http://10.41.1.87/k3cloud/");
//var loginResult2 = client2.ValidateLogin("62cda3a69f579e", "李德飞", "w111111", 2052);
//int resultType2 = JObject.Parse(loginResult2)["LoginResultType"].Value<int>();

//K3CloudApiClient client3 = new K3CloudApiClient("http://10.41.1.87/k3cloud/");
//var loginResult3 = client3.ValidateLogin("62cda3a69f579e", "李德飞", "w111111", 2052);
//int resultType3 = JObject.Parse(loginResult3)["LoginResultType"].Value<int>();

//K3CloudApiClient client4 = new K3CloudApiClient("http://10.41.1.87/k3cloud/");
//var loginResult4 = client4.ValidateLogin("62cda3a69f579e", "李德飞", "w111111", 2052);
//int resultType4 = JObject.Parse(loginResult4)["LoginResultType"].Value<int>();

//K3CloudApiClient client5 = new K3CloudApiClient("http://10.41.1.87/k3cloud/");
//var loginResult5 = client5.ValidateLogin("62cda3a69f579e", "李德飞", "w111111", 2052);
//int resultType5 = JObject.Parse(loginResult5)["LoginResultType"].Value<int>();
//if (resultType == 1)
//{
//    JObject json = new JObject();
//    json.Add("Numbers", "IPQC26020220900027");
//    var result = client1.Submit("QM_InspectBill", json.ToString());              

//}
//Console.ReadKey();
// var result = client.Execute<Object>("HMSX.Second.Plugin.WebAPI.PGCXAPI.GETPGXX,HMSX.Second.Plugin", new object[] { "100026", "2022-11-1" });
// Console.WriteLine(result.ToString());
// Console.ReadKey();

//K3CloudApiClient client = new K3CloudApiClient("http://10.42.66.223/k3cloud/");
//client.ValidateLogin("635a9aca3759c6", "王锋", "kingdee@123", 2052);
//var result = client.Execute<Object>("HMSX.Second.Plugin.WebAPI.PGCXAPI.GETTIME,HMSX.Second.Plugin", new object[] {});
//Console.WriteLine(result.ToString());
//Console.ReadKey();


//K3CloudApiClient client = new K3CloudApiClient("http://10.41.1.87/k3cloud/");
//client.ValidateLogin("634e451b5e777e", , 2052);
////string str = "915708";
//var result = client.Execute<Object>("HMSX.Second.Plugin.WebAPI.PGCXAPI.GETPGMX,HMSX.Second.Plugin", new object[] { "100026", "2022-11-01" });
//Console.WriteLine(result.ToString());
//Console.ReadKey();
