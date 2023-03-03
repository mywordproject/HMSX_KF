using DingTalk.Api;
using DingTalk.Api.Request;
using DingTalk.Api.Response;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HMSX.Second.Plugin.Tool.Results;

namespace HMSX.Second.Plugin.DING
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("钉钉审批数据--审批次数")]
    public class SPCSReportPlugin : AbstractDynamicFormPlugIn
    {
        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {

            base.AfterButtonClick(e);
            //登录
            IDingTalkClient dlclient = new DefaultDingTalkClient("https://oapi.dingtalk.com/gettoken");
            OapiGettokenRequest dlreq = new OapiGettokenRequest();
            dlreq.Appkey = "ding9rewriw9fdlajhmi";
            dlreq.Appsecret = "IrPMDf_HApX0Qe0VJ_U9M32nY9JYkY7YOTOYRehtYg0u4shfR0bzdHh0dou9XGxz";
            dlreq.SetHttpMethod("GET");
            OapiGettokenResponse dlrsp = dlclient.Execute(dlreq);
            DING_Token get = new DING_Token();
            get = JsonConvert.DeserializeObject<DING_Token>(dlrsp.Body);
            string access_token = get.Access_token;
            if (e.Key.Equals("F_SLSB_BUTTON"))
            {
                string cxsql = $@"delete from HMSX_OA_SLID";
                DBUtils.Execute(Context, cxsql);
                //获取实例id列表
                IDingTalkClient idclient = new DefaultDingTalkClient("https://oapi.dingtalk.com/topapi/processinstance/listids");
                OapiProcessinstanceListidsRequest idreq = new OapiProcessinstanceListidsRequest();
                DateTime ksrq = Convert.ToDateTime(this.Model.GetValue("F_260_JJRKS").ToString());
                DateTime jsrq = Convert.ToDateTime(this.Model.GetValue("F_260_JJRJS").ToString());
                idreq.StartTime = (ksrq.ToUniversalTime().Ticks - 621355968000000000) / 10000;
                idreq.EndTime = (jsrq.AddDays(1).ToUniversalTime().Ticks - 621355968000000000) / 10000;
                idreq.Size = 20;
                idreq.Cursor = null;
                idreq.SetHttpMethod("GET");
                string[] mbs = { "PROC-F4D3C2A2-A0AB-4DA4-9A98-A7CA5258596D"
                                , "PROC-A5313BCD-C830-46C5-B325-FE75C6DB2517" , "PROC-E0D8646A-FCB6-4267-B4FB-851D2BB822FA"};
                int XH = 0;
                foreach (var mb in mbs)
                {
                    idreq.ProcessCode = mb.ToString();
                    string mbname = mb.ToString() == "PROC-F4D3C2A2-A0AB-4DA4-9A98-A7CA5258596D" ? "超时请假单" :
                                    mb.ToString() == "PROC-A5313BCD-C830-46C5-B325-FE75C6DB2517" ? "补卡申请单" :"超时补卡申请单" ;
                    //调用获取实例id列表
                    idreq.Cursor = 1;
                    OapiProcessinstanceListidsResponse rsp1 = idclient.Execute(idreq, access_token);
                    if (rsp1.Result != null)
                    {
                        List<string> lists = rsp1.Result.List;
                        foreach (var list in lists)
                        {
                            string insertsql = $@"insert into HMSX_OA_SLID values({XH},'{list}','{mbname}')";
                            DBUtils.Execute(Context, insertsql);
                            XH++;
                        }

                        int i = 2;
                        while (rsp1.Result != null)
                        {
                            idreq.Cursor = i;
                            idreq.ProcessCode = mb.ToString();
                            rsp1 = idclient.Execute(idreq, access_token);
                            if (rsp1.Result != null)
                            {
                                lists = rsp1.Result.List;
                                foreach (var list in lists)
                                {
                                    string insertsql = $@"insert into HMSX_OA_SLID values({XH},'{list}','{mbname}')";
                                    DBUtils.Execute(Context, insertsql);
                                    XH++;
                                }
                                i++;
                            }
                        }
                    }
                }
                this.Model.SetValue("F_260_SL", XH);
                this.View.UpdateView("F_260_SL");
            }
            else if(e.Key.Equals("F_SLSB_BUTTON1"))
            {
                string cxsql = $@"delete from HMSX_OA_SJZSB";
                DBUtils.Execute(Context, cxsql);
                string sql = this.Model.GetValue("F_260_QSFW").ToString() == "1000" ? "XH<=1000" :
                    this.Model.GetValue("F_260_QSFW").ToString() == "2000" ? "XH>1000 AND XH<=2000" :
                    this.Model.GetValue("F_260_QSFW").ToString() == "3000" ? "XH>2000 AND XH<=3000" :
                    this.Model.GetValue("F_260_QSFW").ToString() == "4000" ? "XH>3000 AND XH<=4000" :
                    this.Model.GetValue("F_260_QSFW").ToString() == "5000" ? "XH>4000 AND XH<=5000" :
                    this.Model.GetValue("F_260_QSFW").ToString() == "6000" ? "XH>5000 AND XH<=6000" :
                    this.Model.GetValue("F_260_QSFW").ToString() == "7000" ? "XH>6000 AND XH<=7000" :
                    this.Model.GetValue("F_260_QSFW").ToString() == "8000" ? "XH>7000 AND XH<=8000" :
                    "XH>8000 AND XH<=9000";
                //根据id获取详情
                IDingTalkClient xqclient = new DefaultDingTalkClient("https://oapi.dingtalk.com/topapi/processinstance/get");
                OapiProcessinstanceGetRequest xqreq = new OapiProcessinstanceGetRequest();
                xqreq.SetHttpMethod("GET");
                string listsql = $@"select * from HMSX_OA_SLID where {sql} ORDER BY XH";
                var lists = DBUtils.ExecuteDynamicObject(Context, listsql);
                foreach (var list in lists)
                {
                    //调用根据id获取详情
                    xqreq.ProcessInstanceId = list["ID"].ToString();
                    OapiProcessinstanceGetResponse rsp2 = xqclient.Execute(xqreq, access_token);
                    string fqrbmid = rsp2.ProcessInstance.OriginatorDeptId;//发起人部门
                    string spslbh = rsp2.ProcessInstance.BusinessId;//审批实例编号
                    string createtime = rsp2.ProcessInstance.CreateTime;//创建时间
                    string fqbm = rsp2.ProcessInstance.OriginatorDeptName;//部门
                    string spdz = rsp2.ProcessInstance.BizAction;//审批动作
                    //string spzt = rsp2.ProcessInstance.Status == "NEW" ? "新创建" : rsp2.ProcessInstance.Status == "RUNNING" ? "审批中" : rsp2.ProcessInstance.Status == "TERMINATED" ? "被终止" :
                    //              rsp2.ProcessInstance.Status == "COMPLETED" ? "完成" : "取消";//审批状态
                    string fqr = "";
                    string gh = "";//工号
                    if (rsp2.ProcessInstance.OriginatorUserid != null)
                    {
                        fqr = rsp2.ProcessInstance.OriginatorUserid;
                    }
                    string insertsql = $@"insert into HMSX_OA_SJZSB(XH,SPBH,MB,FQR,FQRGH,ZT,CLR) values({list["XH"].ToString()},'{spslbh}','{list["MBNAME"].ToString()}','{fqr}','{gh}','{fqbm}','{spdz}')";
                    DBUtils.Execute(Context, insertsql);
                }
            }
            else if (e.Key.Equals("F_SLSB_BUTTON3"))
            {
                string sql = this.Model.GetValue("F_260_QSFW").ToString() == "1000" ? "XH<=1000" :
                     this.Model.GetValue("F_260_QSFW").ToString() == "2000" ? "XH>1000 AND XH<=2000" :
                    this.Model.GetValue("F_260_QSFW").ToString() == "3000" ? "XH>2000 AND XH<=3000" :
                    this.Model.GetValue("F_260_QSFW").ToString() == "4000" ? "XH>3000 AND XH<=4000" :
                    this.Model.GetValue("F_260_QSFW").ToString() == "5000" ? "XH>4000 AND XH<=5000" :
                    this.Model.GetValue("F_260_QSFW").ToString() == "6000" ? "XH>5000 AND XH<=6000" :
                    this.Model.GetValue("F_260_QSFW").ToString() == "7000" ? "XH>6000 AND XH<=7000" :
                    this.Model.GetValue("F_260_QSFW").ToString() == "8000" ? "XH>7000 AND XH<=8000" :
                    "XH>8000 AND XH<=9000";
                //根据id获取详情
                IDingTalkClient xqclient = new DefaultDingTalkClient("https://oapi.dingtalk.com/topapi/processinstance/get");
                OapiProcessinstanceGetRequest xqreq = new OapiProcessinstanceGetRequest();
                xqreq.SetHttpMethod("GET");
                string listsql = $@"select * from HMSX_OA_SLID where {sql} ORDER BY XH";
                var lists = DBUtils.ExecuteDynamicObject(Context, listsql);
                foreach (var list in lists)
                {
                    //调用根据id获取详情
                    xqreq.ProcessInstanceId = list["ID"].ToString();
                    OapiProcessinstanceGetResponse rsp2 = xqclient.Execute(xqreq, access_token);
                    string fqrbmid = rsp2.ProcessInstance.OriginatorDeptId;//发起人部门
                    string spslbh = rsp2.ProcessInstance.BusinessId;//审批实例编号
                    string createtime = rsp2.ProcessInstance.CreateTime;//创建时间
                    string fqbm = rsp2.ProcessInstance.OriginatorDeptName;//部门
                    string spdz = rsp2.ProcessInstance.BizAction;//审批动作
                    //string spzt = rsp2.ProcessInstance.Status == "NEW" ? "新创建" : rsp2.ProcessInstance.Status == "RUNNING" ? "审批中" : rsp2.ProcessInstance.Status == "TERMINATED" ? "被终止" :
                    //              rsp2.ProcessInstance.Status == "COMPLETED" ? "完成" : "取消";//审批状态
                    string fqr = "";
                    if (rsp2.ProcessInstance.OriginatorUserid != null)
                    {
                        fqr = rsp2.ProcessInstance.OriginatorUserid;
                    }
                    string insertsql = $@"insert into HMSX_OA_SJZSB(XH,SPBH,MB,FQR,ZT,CLR) values({list["XH"].ToString()},'{spslbh}','{list["MBNAME"].ToString()}','{fqr}','{fqbm}','{spdz}')";
                    DBUtils.Execute(Context, insertsql);
                }
            }
            else if (e.Key.Equals("F_SLSB_BUTTON2"))
            {
                this.Model.DeleteEntryData("F_SLSB_Entity");
                //获取名字
                IDingTalkClient Nameclient = new DefaultDingTalkClient("https://oapi.dingtalk.com/topapi/v2/user/get");
                OapiV2UserGetRequest namereq = new OapiV2UserGetRequest();
                string cxjgsql = $@"select count(*)CS,MB,FQR,ZT from HMSX_OA_SJZSB where SPBH not in(select SPBH from HMSX_OA_SJZSB where CLR='REVOKE')
                             group by FQR,MB,ZT";
                var cxjgs = DBUtils.ExecuteDynamicObject(Context, cxjgsql);
                int y = 0;
                foreach (var cxjg in cxjgs)
                {
                    string fqr = "";
                    string gh = "";
                    namereq.Userid = cxjg["FQR"].ToString();
                    OapiV2UserGetResponse fqrs = Nameclient.Execute(namereq, access_token);
                    fqr = fqrs.Result == null ? "" : fqrs.Result.Name;//发起人
                    gh = fqrs.Result == null ? "" : fqrs.Result.JobNumber;//发起人工号
                    this.Model.CreateNewEntryRow("F_SLSB_Entity");
                    this.Model.SetValue("F_260_MB", cxjg["MB"].ToString(), y);
                    this.View.Model.SetValue("F_260_FQR", fqr, y);
                    this.View.Model.SetValue("F_260_GH", gh, y);
                    this.View.Model.SetValue("F_260_BM", cxjg["ZT"].ToString(), y);
                    this.View.Model.SetValue("F_260_SPCS", cxjg["CS"], y);
                    y++;
                }
                this.View.UpdateView("F_SLSB_Entity");
            }
        }      
    }
}


