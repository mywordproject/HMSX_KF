using Kingdee.BOS.WebApi.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Data.SqlClient;


namespace HMSX.SCZZ.CJGL.MES
{
    class LGZXXJL
    {
        static SqlConnection conn;
        static String FID;
        static K3CloudApiClient client = new K3CloudApiClient("http://10.41.1.87/k3cloud/");
        static void Main(string[] args)
        {
            //查询数据，判断数据是否存在
            sqlserver("20220622011", "12");//传入批次和二维码
        }
        /// <summary>
        /// 查询数据库
        /// </summary>
        /// <pc name="e">批次</param>
        /// <ewm name="e">二维码</param>
        static void sqlserver(string pc, string ewm)
        {
            string strDataBase = "Server=10.41.1.87;DataBase=AI20200517;Uid=sa;pwd=hmdz123!@#;";
            conn = new SqlConnection(strDataBase);
            conn.Open();
            string gzxxsql = $@"select a.FID,FENTRYID,CASE WHEN FENTRYID IS NULL THEN 0 ELSE 1 END ID
                                from keed_t_Cust100336 a 
                               left join keed_t_Cust_Entry100321 b on a.FID = b.FID and FLDJ='{ewm}'
                               where FBILLNO='{pc}'";
            SqlCommand sqlcmd = new SqlCommand(gzxxsql, conn);            
            SqlDataReader cont = sqlcmd.ExecuteReader(); 
            int count=0;
            while (cont.Read())
            {
                count++;
            }
            cont.Close();
            if (count > 0)
            {
                SqlDataReader date = sqlcmd.ExecuteReader(); 
                //调用修改接口
                XG(date);
            }
            else
            {
                //调用保存接口
                 save();
            }
        }
        /// <summary>
        /// 登录金蝶
        /// </summary>
        static int login()
        {
            var loginResult = client.ValidateLogin("628a1d41b778e4", "李德飞", "w111111", 2052);
            int resultType = JObject.Parse(loginResult)["LoginResultType"].Value<int>();
            return resultType;
        }
        /// <summary>
        /// 调用修改接口
        /// </summary>
        static void XG(SqlDataReader dates)
        {
            if (login() == 1)
            {
                JObject json = new JObject();
                json.Add("IsDeleteEntry", false);
                JObject model = new JObject();
                JArray FEntity = new JArray();
                SqlDataReader date = dates;
                while (date.Read())
                {
                    /**多行修改时需要增加循环**/
                    FID = date["FID"].ToString();
                    JObject Entity = new JObject();
                    long a = Convert.ToInt64(date["ID"])==0 ? 0 : Convert.ToInt64(date["FENTRYID"]);
                    Entity.Add("FENTRYID", Convert.ToInt64(date["ID"]) == 0 ? 0 : Convert.ToInt64(date["FENTRYID"]));
                    Entity.Add("FLDJ", "12");//镭雕二维码
                    Entity.Add("FLDSJ", "2020-02-31");//镭雕时间
                    Entity.Add("FLDSN", "456");//镭雕SN码                  
                    FEntity.Add(Entity);
                }
                model.Add("FID", FID);
                JObject F_keed_ModifierId = new JObject();
                F_keed_ModifierId.Add("FUserID", "");//修改人
                model.Add("F_keed_ModifierId", F_keed_ModifierId);
                model.Add("F_keed_ModifyDate", DateTime.Now.ToString());
                model.Add("FEntity", FEntity);
                json.Add("Model", model);
                string save = client.Save("keed_HMSX_GZXXJLD", json.ToString());
                JObject savejson = (JObject)JsonConvert.DeserializeObject(save.Trim(new char[] { '"' }));
                string savestatus = savejson["Result"]["ResponseStatus"]["IsSuccess"].ToString();
                if (savestatus == "True")
                {
                    Console.WriteLine("修改成功");
                    Console.ReadLine();
                }
                date.Close();
                conn.Close();
            }
        }
        /// <summary>
        /// 调用保存接口
        /// </summary>
        static void save()
        {
            if (login() == 1)
            {
                JObject json = new JObject();
                JObject model = new JObject();
                model.Add("FBillNo", "");//批次
                JObject FLLBM = new JObject();
                FLLBM.Add("FNUMBER", "");//来料编码
                model.Add("FLLBM", FLLBM);
                model.Add("F_keed_CreateDate", DateTime.Now.ToString());
                JObject F_keed_CreatorId = new JObject();
                F_keed_CreatorId.Add("FUserID", "");//创建人
                model.Add("F_keed_CreatorId", F_keed_CreatorId);
                model.Add("FLLSMSJ", "");//来料扫码时间
                JArray FEntity = new JArray();
                JObject Entity = new JObject();
                Entity.Add("FLDJ", "");//镭雕二维码
                Entity.Add("FLDSJ", "");//镭雕时间
                Entity.Add("FLDSN", "");//镭雕SN码            
                FEntity.Add(Entity);
                model.Add("FEntity", FEntity);
                json.Add("Model", model);
                string save = client.Save("keed_HMSX_GZXXJLD", json.ToString());
                JObject savejson = (JObject)JsonConvert.DeserializeObject(save.Trim(new char[] { '"' }));
                string savestatus = savejson["Result"]["ResponseStatus"]["IsSuccess"].ToString();
                if (savestatus == "True")
                {
                    Console.WriteLine("保存成功");
                    Console.ReadLine();
                }
            }
        }
    }
}
