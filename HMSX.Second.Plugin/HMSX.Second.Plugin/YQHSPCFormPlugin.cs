using DingTalk.Api;
using DingTalk.Api.Request;
using DingTalk.Api.Response;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HMSX.Second.Plugin.Tool.Results;

namespace HMSX.Second.Plugin
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("疫情核酸排查")]
    public class YQHSPCFormPlugin : AbstractDynamicFormPlugIn
    // class YQHSPC
    {
        static SqlConnection conn;
        //static void Main(string[] args)
        //{
        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
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
            if (e.Key.Equals("F_CX"))
            {
                string bm = this.Model.GetValue("F_260_BM") == null ? "" : ((DynamicObject)this.Model.GetValue("F_260_BM"))["Name"].ToString();
                string strDataBase = "Server=10.41.1.23;DataBase=SX_DataAdapt;Uid=sxkf;pwd=sxkf123A;";
                conn = new SqlConnection(strDataBase);
                conn.Open();
                string gzxxsql = $@"exec zzx_yqtj '{bm}','否'";
                SqlCommand sqlcmd = new SqlCommand(gzxxsql, conn);
                SqlDataReader cont = sqlcmd.ExecuteReader();
                int hs = 0;
                this.Model.DeleteEntryData("F_SLSB_Entity");
                while (cont.Read())
                {
                    this.Model.CreateNewEntryRow("F_SLSB_Entity");
                    this.View.Model.SetValue("F_XM", cont["姓名"].ToString(), hs);
                    this.View.Model.SetValue("F_GH", cont["员工编码"].ToString(), hs);
                    this.View.Model.SetValue("F_ZRBM", cont["责任部门"], hs );
                    this.View.Model.SetValue("F_FZR", cont["负责人姓名"], hs);                    
                    //this.View.Model.SetValue("F_SFHSJC", cont["userid"].ToString(), hs);
                    this.View.Model.SetValue("F_YGID", cont["userid"].ToString(), hs );
                    this.View.Model.SetValue("F_FZRID", cont["负责人电话"], hs);
                    hs++;
                }
                cont.Close();
                conn.Close();
                this.View.UpdateView("F_SLSB_Entity");
            }
            if (e.Key.Equals("F_YGLD"))
            {
                
                var dates = this.Model.DataObject["SLSB_K79ef96fd"] as DynamicObjectCollection;
                int i = 1;
                string workid = "";
                Dictionary<string, string> people= new Dictionary<string, string>();
                foreach (var date in dates)
                {
                    if (date["F_FZRID"] != null )
                    {
                        if (people.ContainsKey(date["F_FZRID"].ToString()) == false)
                        {
                            people.Add(date["F_FZRID"].ToString(), date["F_XM"].ToString());
                        }
                        else
                        {
                            people[date["F_FZRID"].ToString()] = people[date["F_FZRID"].ToString()] + "," + date["F_XM"].ToString();
                        }
                    }
                    if (date["F_YGID"] != null)
                    {
                        workid += date["F_YGID"]+ ",";
                    }                                   
                    if (i % 100 == 0 && workid!="")
                    {
                        dingding(access_token, workid.Trim(','));
                        workid = "";
                    }
                    i++;
                }
                dingding(access_token, workid.Trim(','));
                string userid = "";
                foreach(var name in people)
                {
                    IDingTalkClient client = new DefaultDingTalkClient("https://oapi.dingtalk.com/topapi/v2/user/getbymobile");
                    OapiV2UserGetbymobileRequest req = new OapiV2UserGetbymobileRequest();
                    req.Mobile = name.Key;
                    OapiV2UserGetbymobileResponse rsp = client.Execute(req, access_token);
                    userid = rsp.Result.Userid;
                    dingding2(access_token, userid, name.Value);
                }
            }
            if (e.Key.Equals("F_GLRY"))
            {
                var dates = this.Model.DataObject["SLSB_K79ef96fd"] as DynamicObjectCollection;
                string str = "";
                foreach(var date in dates)
                {
                    str += date["F_XM"] + ",";
                }
                var gkrys = this.Model.GetValue("F_YQGKRY") as DynamicObjectCollection;
                foreach(var gkry in gkrys)
                {
                    string userid = "";
                    IDingTalkClient client = new DefaultDingTalkClient("https://oapi.dingtalk.com/topapi/v2/user/getbymobile");
                    OapiV2UserGetbymobileRequest req = new OapiV2UserGetbymobileRequest();
                    req.Mobile = ((DynamicObject)gkry["F_YQGKRY"])["Mobile"].ToString();
                    OapiV2UserGetbymobileResponse rsp = client.Execute(req, access_token);
                    userid = rsp.Result.Userid;
                    dingding3(access_token, userid, str.Trim(','));                   
                }
            }
        }
        private void dingding(string token, string id)
        {
            IDingTalkClient client = new DefaultDingTalkClient("https://oapi.dingtalk.com/topapi/message/corpconversation/asyncsend_v2");
            OapiMessageCorpconversationAsyncsendV2Request req = new OapiMessageCorpconversationAsyncsendV2Request();
            req.AgentId = 1870548521L;
            req.UseridList = id;
            OapiMessageCorpconversationAsyncsendV2Request.MsgDomain obj1 = new OapiMessageCorpconversationAsyncsendV2Request.MsgDomain();
            obj1.Msgtype = "text";
            OapiMessageCorpconversationAsyncsendV2Request.TextDomain obj2 = new OapiMessageCorpconversationAsyncsendV2Request.TextDomain();
            obj2.Content = "推送时间【"+DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")+"】，系统检测到您今天未进行核酸检测，为了您的安全，请尽快完成核酸检测！";
            obj1.Text = obj2;
            OapiMessageCorpconversationAsyncsendV2Request.OADomain obj3 = new OapiMessageCorpconversationAsyncsendV2Request.OADomain();
            OapiMessageCorpconversationAsyncsendV2Request.BodyDomain obj4 = new OapiMessageCorpconversationAsyncsendV2Request.BodyDomain();
            obj4.Content = "推送时间【" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "】，系统检测到您今天未进行核酸检测，为了您的安全，请尽快完成核酸检测！";
            obj3.Body = obj4;
            obj1.Oa = obj3;
            req.Msg_ = obj1;
            OapiMessageCorpconversationAsyncsendV2Response rsp = client.Execute(req, token);
            if (rsp.Errmsg == "ok")
            {
                this.View.ShowMessage("推送成功");
            }
        }
        private void dingding2(string token, string id,string value)
        {
            string[] cs=value.Split(',');
            string st = "";
            if (cs.Length == 1)
            {
                st =Convert.ToString(cs.Length);
            }
            else
            {
                st ="等"+Convert.ToString(cs.Length);
            }
            IDingTalkClient client = new DefaultDingTalkClient("https://oapi.dingtalk.com/topapi/message/corpconversation/asyncsend_v2");
            OapiMessageCorpconversationAsyncsendV2Request req = new OapiMessageCorpconversationAsyncsendV2Request();
            req.AgentId = 1870548521L;
            req.UseridList = id;
            OapiMessageCorpconversationAsyncsendV2Request.MsgDomain obj1 = new OapiMessageCorpconversationAsyncsendV2Request.MsgDomain();
            obj1.Msgtype = "text";
            OapiMessageCorpconversationAsyncsendV2Request.TextDomain obj2 = new OapiMessageCorpconversationAsyncsendV2Request.TextDomain();
            obj2.Content = "推送时间【" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "】，系统检测到你部门的" + value+st+"名员工今天未进行核酸检测，请及时督促部门员工完成核酸检测！";
            obj1.Text = obj2;
            OapiMessageCorpconversationAsyncsendV2Request.OADomain obj3 = new OapiMessageCorpconversationAsyncsendV2Request.OADomain();
            OapiMessageCorpconversationAsyncsendV2Request.BodyDomain obj4 = new OapiMessageCorpconversationAsyncsendV2Request.BodyDomain();
            obj4.Content = "推送时间【" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "】，系统检测到你部门的" + value + st + "名员工今天未进行核酸检测，请及时督促部门员工完成核酸检测！";
            obj3.Body = obj4;
            obj1.Oa = obj3;
            req.Msg_ = obj1;
            OapiMessageCorpconversationAsyncsendV2Response rsp = client.Execute(req, token);
            if (rsp.Errmsg == "ok")
            {
                this.View.ShowMessage("推送成功");
            }
        }
        private void dingding3(string token, string id, string value)
        {
            string[] cs = value.Split(',');
            string st = "";
            if (cs.Length == 1)
            {
                st = Convert.ToString(cs.Length);
            }
            else
            {
                st = "等" + Convert.ToString(cs.Length);
            }
            IDingTalkClient client = new DefaultDingTalkClient("https://oapi.dingtalk.com/topapi/message/corpconversation/asyncsend_v2");
            OapiMessageCorpconversationAsyncsendV2Request req = new OapiMessageCorpconversationAsyncsendV2Request();
            req.AgentId = 1870548521L;
            req.UseridList = id;
            OapiMessageCorpconversationAsyncsendV2Request.MsgDomain obj1 = new OapiMessageCorpconversationAsyncsendV2Request.MsgDomain();
            obj1.Msgtype = "text";
            OapiMessageCorpconversationAsyncsendV2Request.TextDomain obj2 = new OapiMessageCorpconversationAsyncsendV2Request.TextDomain();
            obj2.Content = "推送时间【" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "】，系统检测到" + value + st + "名员工今天未进行核酸检测，请及时通知员工完成核酸检测！!";
            obj1.Text = obj2;
            OapiMessageCorpconversationAsyncsendV2Request.OADomain obj3 = new OapiMessageCorpconversationAsyncsendV2Request.OADomain();
            OapiMessageCorpconversationAsyncsendV2Request.BodyDomain obj4 = new OapiMessageCorpconversationAsyncsendV2Request.BodyDomain();
            obj4.Content = "推送时间【" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "】，系统检测到" + value + st + "名员工今天未进行核酸检测，请及时通知员工完成核酸检测！!";
            obj3.Body = obj4;
            obj1.Oa = obj3;
            req.Msg_ = obj1;
            OapiMessageCorpconversationAsyncsendV2Response rsp = client.Execute(req, token);
            if (rsp.Errmsg == "ok")
            {
                this.View.ShowMessage("推送成功");
            }
        }
    }
}
