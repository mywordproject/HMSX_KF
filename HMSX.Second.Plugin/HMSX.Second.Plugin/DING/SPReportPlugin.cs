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
    [Description("钉钉审批数据")]
    public class SPReportPlugin : AbstractDynamicFormPlugIn
    {
        int hs = 0;
        /**
        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            hs = 0;
            this.Model.DeleteEntryData("F_SLSB_Entity");
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

            //获取名字
            IDingTalkClient Nameclient = new DefaultDingTalkClient("https://oapi.dingtalk.com/topapi/v2/user/get");
            OapiV2UserGetRequest namereq = new OapiV2UserGetRequest();
            //获取模板
            IDingTalkClient mbclient = new DefaultDingTalkClient("https://oapi.dingtalk.com/topapi/process/template/manage/get");
            OapiProcessTemplateManageGetRequest mbreq = new OapiProcessTemplateManageGetRequest();
            mbreq.SetHttpMethod("GET");
            mbreq.Userid = "2028341816849616";
            OapiProcessTemplateManageGetResponse mbrsp = mbclient.Execute(mbreq, access_token);
            foreach (var mblist in mbrsp.Result)
            {
                //获取实例id列表
                IDingTalkClient idclient = new DefaultDingTalkClient("https://oapi.dingtalk.com/topapi/processinstance/listids");
                OapiProcessinstanceListidsRequest idreq = new OapiProcessinstanceListidsRequest();
                idreq.ProcessCode = mblist.ProcessCode;
                //idreq.ProcessCode = "PROC-B1E20999-481B-49DF-AED5-9AA6CE3FC2F1";
                DateTime dt = Convert.ToDateTime(DateTime.Now.ToShortDateString().ToString());
                dt = dt.AddDays(-7);
                idreq.StartTime = (dt.ToUniversalTime().Ticks - 621355968000000000) / 10000;
                idreq.EndTime = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000;
                idreq.Size = 20;
                idreq.Cursor = null;
                idreq.SetHttpMethod("GET");
                OapiProcessinstanceListidsResponse rsp1 = idclient.Execute(idreq, access_token);
                if (rsp1.Result != null)
                {
                    List<string> lists = rsp1.Result.List;
                    foreach (var list in lists)
                    {
                        //根据id获取详情
                        IDingTalkClient xqclient = new DefaultDingTalkClient("https://oapi.dingtalk.com/topapi/processinstance/get");
                        OapiProcessinstanceGetRequest xqreq = new OapiProcessinstanceGetRequest();
                        xqreq.ProcessInstanceId = list;
                        xqreq.SetHttpMethod("GET");
                        OapiProcessinstanceGetResponse rsp2 = xqclient.Execute(xqreq, access_token);
                        string bt = rsp2.ProcessInstance.Title;//标题
                        //string kssj = rsp2.ProcessInstance.CreateTime;//开始时间
                        //string jssj = rsp2.ProcessInstance.FinishTime;//结束时间
                        string fqr = "";
                        if (rsp2.ProcessInstance.OriginatorUserid != null)
                        {
                            namereq.Userid = rsp2.ProcessInstance.OriginatorUserid;
                            OapiV2UserGetResponse fqrs = Nameclient.Execute(namereq, access_token);
                            fqr = fqrs.Result == null ? "" : fqrs.Result.Name;//发起人
                        }
                        string fqrbm = rsp2.ProcessInstance.OriginatorDeptId;//发起人部门
                       // string spzt = rsp2.ProcessInstance.Status == "NEW" ? "新创建" : rsp2.ProcessInstance.Status == "RUNNING" ? "审批中" : rsp2.ProcessInstance.Status == "TERMINATED" ? "被终止" :
                       //               rsp2.ProcessInstance.Status == "COMPLETED" ? "完成" : "取消";//审批状态
                       // List<string> spr = rsp2.ProcessInstance.ApproverUserids;//审批人
                       // List<string> csrs = rsp2.ProcessInstance.CcUserids;//抄送人
                       // string csrname = "";
                       // if (csrs != null)
                       // {
                       //     foreach (var csr in csrs)
                       //     {
                       //         namereq.Userid = csr;
                       //         OapiV2UserGetResponse fqrs = Nameclient.Execute(namereq, access_token);
                       //         csrname += fqrs.Result == null ? "" : fqrs.Result.Name + ",";//抄送人
                       //     }
                       // }
                       // string spjg = rsp2.ProcessInstance.Result == "AGREE" ? "同意" : rsp2.ProcessInstance.Result == "" ? "" : "拒绝";//审批结果
                        string spslbh = rsp2.ProcessInstance.BusinessId;//审批实例编号
                        string fqbm = rsp2.ProcessInstance.OriginatorDeptName;//部门
                        var titys = rsp2.ProcessInstance.OperationRecords;//操作列表
                     
                        foreach (var tity in titys)
                        {
                            string czsj = tity.Date == null ? DateTime.Now.ToString() : tity.Date;//操作时间
                            string czjg = tity.OperationResult == "AGREE" ? "同意" : tity.OperationResult == "REFUSE" ? "拒绝" : "";//操作结果
                            string czlx = tity.OperationType == "EXECUTE_TASK_NORMAL" ? "正常执行任务" : tity.OperationType == "EXECUTE_TASK_AGENT" ? "代理人执行任务" :
                                          tity.OperationType == "APPEND_TASK_BEFORE" ? "前加签任务" : tity.OperationType == "APPEND_TASK_AFTER" ? "后加签任务" :
                                          tity.OperationType == "REDIRECT_TASK" ? "转交任务" : tity.OperationType == "START_PROCESS_INSTANCE" ? "发起流程实例" :
                                          tity.OperationType == "TERMINATE_PROCESS_INSTANCE" ? "终止(撤销)流程实例" : tity.OperationType == "FINISH_PROCESS_INSTANCE" ? "结束流程实例" :
                                          tity.OperationType == "ADD_REMARK" ? "添加评论" : tity.OperationType == "REDIRECT_PROCESS" ? "审批退回" : "抄送";//操作类型
                            string czr = "";//操作人
                            if (tity.Userid != null)
                            {
                                namereq.Userid = tity.Userid;
                                OapiV2UserGetResponse czrs = Nameclient.Execute(namereq, access_token);
                                czr = czrs.Result == null ? "" : czrs.Result.Name;//发起人
                            }
                            //this.Model.CreateNewEntryRow("F_SLSB_Entity");
                            //this.View.Model.SetValue("F_260_MB", bt, hs);//模板
                            //this.View.Model.SetValue("F_260_FQR", fqr, hs);//发起人
                            //this.View.Model.SetValue("F_260_JDMC", czlx, hs);//节点名称
                            //this.View.Model.SetValue("F_260_CLR", czr, hs);//处理人
                            //this.View.Model.SetValue("F_260_ZRBM", fqbm, hs);//责任部门
                            //this.View.Model.SetValue("F_260_CJSJ", kssj, hs);//创建时间
                            //this.View.Model.SetValue("F_260_CLSJ", "", hs);//处理时间
                            //this.View.Model.SetValue("F_260_CS", "", hs);//超时
                            //hs++;
                        }
                        var titys1 = rsp2.ProcessInstance.Tasks;//任务列表
                        if (titys1 != null)
                        {
                            foreach (var tity1 in titys1)
                            {
                                string rwclr = "";
                                if (tity1.Userid != null)
                                {
                                    namereq.Userid = tity1.Userid;
                                    OapiV2UserGetResponse rwclrs = Nameclient.Execute(namereq, access_token);
                                    rwclr = rwclrs.Result == null ? "" : rwclrs.Result.Name;//任务处理人
                                }
                                string zt = tity1.TaskStatus == "new" ? "未启动" : tity1.TaskStatus == "RUNNING" ? "处理中" : tity1.TaskStatus == "PAUSED" ? "暂停" : tity1.TaskStatus == "CANCELED" ? "取消" : tity1.TaskStatus == "COMPLETED" ? "完成" : "终止";//状态
                               // string jg = tity1.TaskResult == "AGREE" ? "同意" : tity1.TaskResult == "REFUSE" ? "拒绝" : "转交";//结果
                                DateTime ks = tity1.CreateTime == null ? DateTime.Now : Convert.ToDateTime(tity1.CreateTime);//开始时间
                                DateTime js = tity1.FinishTime == null ? DateTime.Now : Convert.ToDateTime(tity1.FinishTime);//结束时间
                                System.TimeSpan t = js - ks;
                                if (Math.Round(t.TotalHours, 2) > 24)
                                {
                                    this.Model.CreateNewEntryRow("F_SLSB_Entity");
                                    this.Model.SetValue("F_260_SPBH", spslbh, hs);
                                    this.View.Model.SetValue("F_260_MB", bt, hs);
                                    this.View.Model.SetValue("F_260_FQR", fqr, hs);
                                    this.View.Model.SetValue("F_260_JDMC", zt, hs);
                                    this.View.Model.SetValue("F_260_CLR", rwclr, hs);
                                    this.View.Model.SetValue("F_260_ZRBM", fqbm, hs);
                                    this.View.Model.SetValue("F_260_CJSJ", ks, hs);
                                    this.View.Model.SetValue("F_260_CLSJ", js, hs);
                                    this.View.Model.SetValue("F_260_CS", Math.Round(t.TotalHours, 2), hs);
                                    hs++;
                                }
                            }
                        }
                    }
                }    
            }
            this.View.UpdateView("F_SLSB_Entity");
        }
        **/
        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            hs = 0;
            this.Model.DeleteEntryData("F_SLSB_Entity");
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

            //获取名字
            IDingTalkClient Nameclient = new DefaultDingTalkClient("https://oapi.dingtalk.com/topapi/v2/user/get");
            OapiV2UserGetRequest namereq = new OapiV2UserGetRequest();
         
            if (e.Key.Equals("F_SLSB_BUTTON"))
            {
                string cxsql = $@"delete from HMSX_OA_MB";
                DBUtils.Execute(Context, cxsql);
                //获取模板
                IDingTalkClient mbclient = new DefaultDingTalkClient("https://oapi.dingtalk.com/topapi/process/template/manage/get");
                OapiProcessTemplateManageGetRequest mbreq = new OapiProcessTemplateManageGetRequest();
                mbreq.SetHttpMethod("GET");
                mbreq.Userid = "2028341816849616";
                OapiProcessTemplateManageGetResponse mbrsp = mbclient.Execute(mbreq, access_token);
                int i = 1;
                foreach (var mblist in mbrsp.Result)
                {
                    string insertsql = $@"insert into HMSX_OA_MB values({ i},'{mblist.ProcessCode}','{mblist.FlowTitle}')";
                    DBUtils.Execute(Context, insertsql);
                    i++;
                }
            }
            else if (e.Key.Equals("F_SLSB_BUTTON1"))
            {
                string cxsql = $@"delete from HMSX_OA_SLID";
                DBUtils.Execute(Context, cxsql);
                //获取实例id列表
                IDingTalkClient idclient = new DefaultDingTalkClient("https://oapi.dingtalk.com/topapi/processinstance/listids");
                OapiProcessinstanceListidsRequest idreq = new OapiProcessinstanceListidsRequest();
                DateTime dt = Convert.ToDateTime(DateTime.Now.ToShortDateString().ToString());
                dt = dt.AddDays(-7);
                idreq.StartTime = (dt.ToUniversalTime().Ticks - 621355968000000000) / 10000;
                idreq.EndTime = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000;
                idreq.Size = 20;
                idreq.Cursor = null;
                idreq.SetHttpMethod("GET");
                string mbsql = $@"select * from HMSX_OA_MB ORDER BY XH";
                var mbs = DBUtils.ExecuteDynamicObject(Context, mbsql);
                int XH = 0;
                foreach (var mb in mbs)
                {
                    idreq.ProcessCode = mb["MB"].ToString();
                    string mbname = mb["MBNAME"].ToString();
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
                            idreq.ProcessCode = mb["MB"].ToString();
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
                this.Model.SetValue("F_260_SL",XH);
                this.View.UpdateView("F_260_SL");
            }
            else if (e.Key.Equals("F_SLSB_BUTTON2"))
            {
                string cxsql = $@"delete from HMSX_OA_SJZSB";
                DBUtils.Execute(Context, cxsql);
                string sql = this.Model.GetValue("F_260_TS").ToString() == "1000" ? "XH<=1000" :
                    this.Model.GetValue("F_260_TS").ToString() == "2000" ? "XH>1000 AND XH<=2000" :
                    this.Model.GetValue("F_260_TS").ToString() == "3000" ? "XH>2000 AND XH<=3000" :
                    this.Model.GetValue("F_260_TS").ToString() == "4000" ? "XH>3000 AND XH<=4000" :
                    this.Model.GetValue("F_260_TS").ToString() == "5000" ? "XH>4000 AND XH<=5000" :
                    this.Model.GetValue("F_260_TS").ToString() == "6000" ? "XH>5000 AND XH<=6000" :
                    this.Model.GetValue("F_260_TS").ToString() == "7000" ? "XH>6000 AND XH<=7000" :
                    this.Model.GetValue("F_260_TS").ToString() == "8000" ? "XH>7000 AND XH<=8000" :
                    "XH>8000 AND XH<=9000";
                string slidsql = $@"SELECT * FROM HMSX_OA_SLID WHERE {sql} ORDER BY XH";
                var slids = DBUtils.ExecuteDynamicObject(Context, slidsql);
                foreach (var slid in slids)
                {
                    //根据id获取详情
                    IDingTalkClient xqclient = new DefaultDingTalkClient("https://oapi.dingtalk.com/topapi/processinstance/get");
                    OapiProcessinstanceGetRequest xqreq = new OapiProcessinstanceGetRequest();
                    xqreq.SetHttpMethod("GET");
                    //调用根据id获取详情
                    xqreq.ProcessInstanceId = slid["ID"].ToString();
                    OapiProcessinstanceGetResponse rsp2 = xqclient.Execute(xqreq, access_token);
                    string bt = rsp2.ProcessInstance.Title;//标题                      
                    string spslbh = rsp2.ProcessInstance.BusinessId;//审批实例编号
                    var titys = rsp2.ProcessInstance.OperationRecords;//操作列表
                    var titys1 = rsp2.ProcessInstance.Tasks;//任务列表
                    if (titys1 != null)
                    {
                        foreach (var tity1 in titys1)
                        {
                            DateTime ks = tity1.CreateTime == null ? DateTime.Now : Convert.ToDateTime(tity1.CreateTime);//开始时间
                            DateTime js = tity1.FinishTime == null ? DateTime.Now : Convert.ToDateTime(tity1.FinishTime);//结束时间
                            System.TimeSpan t = js - ks;
                            if (Math.Round(t.TotalHours, 2) > 24)
                            {
                                string rwclr = "";                              
                                string JobNumber = "";
                                if (tity1.Userid != null)
                                {
                                    namereq.Userid = tity1.Userid;
                                    OapiV2UserGetResponse rwclrs = Nameclient.Execute(namereq, access_token);
                                    rwclr = rwclrs.Result == null ? "" : rwclrs.Result.Name;//任务处理人
                                    JobNumber = rwclrs.Result == null ? "" : rwclrs.Result.JobNumber;
                                }
                                string zt = tity1.TaskStatus == "new" ? "未启动" : tity1.TaskStatus == "RUNNING" ? "处理中" : tity1.TaskStatus == "PAUSED" ? "暂停" :
                                tity1.TaskStatus == "CANCELED" ? "取消" : tity1.TaskStatus == "COMPLETED" ? "完成" : "终止";//状态  
                                if (rwclr != "" && zt != "终止" && JobNumber != "")
                                {
                                    DateTime jjrks = Convert.ToDateTime(this.Model.GetValue("F_260_JJRKS").ToString());
                                    DateTime jjrjs = Convert.ToDateTime(this.Model.GetValue("F_260_JJRJS").ToString());
                                    double xs = (jjrjs - jjrks.AddDays(-1)).TotalHours;
                                    double sjc = (jjrjs.AddDays(1).AddHours(8).AddMinutes(45) - ks).TotalHours;
                                    if (ks.ToShortDateString() == jjrks.AddDays(-1).ToShortDateString() && ((Math.Round(t.TotalHours, 2) - xs) > 24))
                                    {
                                        if (JobNumber != "" && JobNumber.Substring(0, 1) == "8")
                                        { 
                                            string insertsql = $@"insert into HMSX_OA_SJZSB values({hs},'{spslbh}','{slid["MBNAME"].ToString()}','{rsp2.ProcessInstance.OriginatorUserid}','{JobNumber}','{zt}','{rwclr}','{ks}','{js}','{Math.Round(t.TotalHours, 2) - xs}')";
                                            DBUtils.Execute(Context, insertsql);
                                            hs++;
                                        }
                                    }
                                    else if (jjrks <= Convert.ToDateTime(ks.ToShortDateString()) && jjrjs >= Convert.ToDateTime(ks.ToShortDateString()) && ((Math.Round(t.TotalHours, 2) - sjc) > 24))
                                    {
                                        if (JobNumber != "" && JobNumber.Substring(0, 1) == "8")
                                        {
                                            string insertsql = $@"insert into HMSX_OA_SJZSB values({hs},'{spslbh}','{slid["MBNAME"].ToString()}','{rsp2.ProcessInstance.OriginatorUserid}','{JobNumber}','{zt}','{rwclr}','{ks}','{js}','{Math.Round(t.TotalHours, 2) - sjc}')";
                                            DBUtils.Execute(Context, insertsql);
                                            hs++;
                                        }
                                    }
                                    else if (jjrks.AddDays(-1) > Convert.ToDateTime(ks.ToShortDateString()) || jjrjs < Convert.ToDateTime(ks.ToShortDateString()))
                                    {
                                        if (JobNumber != "" && JobNumber.Substring(0, 1) == "8")
                                        {
                                            string insertsql = $@"insert into HMSX_OA_SJZSB values({hs},'{spslbh}','{slid["MBNAME"].ToString()}','{rsp2.ProcessInstance.OriginatorUserid}','{JobNumber}','{zt}','{rwclr}','{ks}','{js}','{Math.Round(t.TotalHours, 2)}')";
                                            DBUtils.Execute(Context, insertsql);
                                            hs++;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else if (e.Key.Equals("F_SLSB_BUTTON3"))           
            {
                hs = 0;
                string sjysql = $@"SELECT * FROM HMSX_OA_SJZSB  ORDER BY XH";
                var sjys = DBUtils.ExecuteDynamicObject(Context, sjysql);
                foreach(var sjy in sjys)
                {
                    // string clrsql = $@"exec Pro_bm '{sjy["FQRGH"].ToString()}'";
                    //string clrsql = $@"SELECT FNAME FROM T_BD_DEPARTMENT_L WHERE FDEPTID=[SX_DataAdapt].dbo.getBM(16139100)";
                   // var clr = DBUtils.ExecuteDynamicObject(Context, clrsql);
                   // string zrbm = clr.Count == 0 ? "" : clr[0]["部门"].ToString();//责任部门

                    string fqr = "";
                   string zrbm = "";
                   string zrbmsql = $@"select c.FNAME 姓名,FRYLB,d.FDEPTID,e.FNAME 部门,f.FNAME 上级部门 from T_HR_EMPINFO a
                                    inner join T_BD_STAFFTEMP b on a.FID=b.FID 
                                    inner join T_HR_EMPINFO_L c on a.FID=c.FID 
                                    inner join T_BD_DEPARTMENT d on d.FDEPTID=b.FDEPTID
                                    inner join T_BD_DEPARTMENT_L e on d.FDEPTID=e.FDEPTID
                                    left join T_BD_DEPARTMENT_L f on d.FPARENTID=f.FDEPTID
                                    where FISFIRSTPOST=1 and a.FNUMBER='{sjy["FQRGH"].ToString()}'";
                   var bmid = DBUtils.ExecuteDynamicObject(Context, zrbmsql);
                    if (bmid.Count > 0)
                    {
                        string clrsql = $@"/*dialect*/SELECT FNAME FROM T_BD_DEPARTMENT_L WHERE FDEPTID=[SX_DataAdapt].dbo.getBM({Convert.ToInt32(bmid[0]["FDEPTID"].ToString())})";
                        //string clrsql = $@"/*dialect*/SELECT FNAME FROM T_BD_DEPARTMENT_L WHERE FDEPTID=dbo.getBM({Convert.ToInt32(bmid[0]["FDEPTID"].ToString())})";
                        var clr = DBUtils.ExecuteDynamicObject(Context, clrsql);
                        zrbm = clr.Count == 0 ? "" : clr[0]["FNAME"].ToString();//责任部门
                        if (sjy["FQR"] != null)
                        {
                            namereq.Userid = sjy["FQR"].ToString();
                            OapiV2UserGetResponse fqrs = Nameclient.Execute(namereq, access_token);
                            fqr = fqrs.Result == null ? "" : fqrs.Result.Name;//发起人                      
                        }
                        this.Model.CreateNewEntryRow("F_SLSB_Entity");
                        this.Model.SetValue("F_260_SPBH", sjy["SPBH"].ToString(), hs);
                        this.View.Model.SetValue("F_260_MB", sjy["MB"].ToString(), hs);
                        this.View.Model.SetValue("F_260_FQR", fqr, hs);
                        this.View.Model.SetValue("F_260_JDMC", sjy["ZT"].ToString(), hs);
                        this.View.Model.SetValue("F_260_CLR", sjy["CLR"].ToString(), hs);
                        this.View.Model.SetValue("F_260_ZRBM", zrbm, hs);
                        this.View.Model.SetValue("F_260_CJSJ", sjy["KSSJ"].ToString(), hs);
                        this.View.Model.SetValue("F_260_CLSJ", sjy["JSSJ"].ToString(), hs);
                        this.View.Model.SetValue("F_260_CS", sjy["CS"].ToString(), hs);
                        hs++;
                    }                   
                }
                this.View.UpdateView("F_SLSB_Entity");
            }
            else if (e.Key.Equals("F_SLSB_BUTTON4"))
            {
                string sql = this.Model.GetValue("F_260_TS").ToString() == "1000" ? "XH<=1000" :
                    this.Model.GetValue("F_260_TS").ToString() == "2000" ? "XH>1000 AND XH<=2000" :
                    this.Model.GetValue("F_260_TS").ToString() == "3000" ? "XH>2000 AND XH<=3000" :
                    this.Model.GetValue("F_260_TS").ToString() == "4000" ? "XH>3000 AND XH<=4000" :
                    this.Model.GetValue("F_260_TS").ToString() == "5000" ? "XH>4000 AND XH<=5000" :
                    this.Model.GetValue("F_260_TS").ToString() == "6000" ? "XH>5000 AND XH<=6000" :
                    this.Model.GetValue("F_260_TS").ToString() == "7000" ? "XH>6000 AND XH<=7000" :
                    this.Model.GetValue("F_260_TS").ToString() == "8000" ? "XH>7000 AND XH<=8000" :
                    "XH>8000 AND XH<=9000";
                string slidsql = $@"SELECT * FROM HMSX_OA_SLID WHERE {sql} ORDER BY XH";
                var slids = DBUtils.ExecuteDynamicObject(Context, slidsql);
                foreach (var slid in slids)
                {
                    //根据id获取详情
                    IDingTalkClient xqclient = new DefaultDingTalkClient("https://oapi.dingtalk.com/topapi/processinstance/get");
                    OapiProcessinstanceGetRequest xqreq = new OapiProcessinstanceGetRequest();
                    xqreq.SetHttpMethod("GET");
                    //调用根据id获取详情
                    xqreq.ProcessInstanceId = slid["ID"].ToString();
                    OapiProcessinstanceGetResponse rsp2 = xqclient.Execute(xqreq, access_token);
                    string bt = rsp2.ProcessInstance.Title;//标题                      
                    string spslbh = rsp2.ProcessInstance.BusinessId;//审批实例编号
                    
                    var titys = rsp2.ProcessInstance.OperationRecords;//操作列表
                    var titys1 = rsp2.ProcessInstance.Tasks;//任务列表
                    if (titys1 != null)
                    {
                        foreach (var tity1 in titys1)
                        {
                            DateTime ks = tity1.CreateTime == null ? DateTime.Now : Convert.ToDateTime(tity1.CreateTime);//开始时间
                            DateTime js = tity1.FinishTime == null ? DateTime.Now : Convert.ToDateTime(tity1.FinishTime);//结束时间
                            System.TimeSpan t = js - ks;
                            if (Math.Round(t.TotalHours, 2) > 24)
                            {
                                string rwclr = "";
                                string JobNumber = "";
                                if (tity1.Userid != null)
                                {
                                    namereq.Userid = tity1.Userid;
                                    OapiV2UserGetResponse rwclrs = Nameclient.Execute(namereq, access_token);
                                    rwclr = rwclrs.Result == null ? "" : rwclrs.Result.Name;//任务处理人
                                    JobNumber = rwclrs.Result == null ? "" : rwclrs.Result.JobNumber;
                                }
                                string zt = tity1.TaskStatus == "new" ? "未启动" : tity1.TaskStatus == "RUNNING" ? "处理中" : tity1.TaskStatus == "PAUSED" ? "暂停" :
                                tity1.TaskStatus == "CANCELED" ? "取消" : tity1.TaskStatus == "COMPLETED" ? "完成" : "终止";//状态  
                                if (rwclr != "" && zt != "终止" && JobNumber != "")
                                {
                                    DateTime jjrks = Convert.ToDateTime(this.Model.GetValue("F_260_JJRKS").ToString());
                                    DateTime jjrjs = Convert.ToDateTime(this.Model.GetValue("F_260_JJRJS").ToString());
                                    double xs = (jjrjs - jjrks.AddDays(-1)).TotalHours;
                                    double sjc = (jjrjs.AddDays(1).AddHours(8).AddMinutes(45) - ks).TotalHours;
                                    if (ks.ToShortDateString() == jjrks.AddDays(-1).ToShortDateString() && ((Math.Round(t.TotalHours, 2) - xs) > 24))
                                    {
                                        if (JobNumber != "" && JobNumber.Substring(0, 1) == "8")
                                        {
                                            string insertsql = $@"insert into HMSX_OA_SJZSB values({hs},'{spslbh}','{slid["MBNAME"].ToString()}','{rsp2.ProcessInstance.OriginatorUserid}','{JobNumber}','{zt}','{rwclr}','{ks}','{js}','{Math.Round(t.TotalHours, 2) - xs}')";
                                            DBUtils.Execute(Context, insertsql);
                                            hs++;
                                        }
                                    }
                                    else if (jjrks <= Convert.ToDateTime(ks.ToShortDateString()) && jjrjs >= Convert.ToDateTime(ks.ToShortDateString()) && ((Math.Round(t.TotalHours, 2) - sjc) > 24))
                                    {
                                        if (JobNumber != "" && JobNumber.Substring(0, 1) == "8")
                                        {
                                            string insertsql = $@"insert into HMSX_OA_SJZSB values({hs},'{spslbh}','{slid["MBNAME"].ToString()}','{rsp2.ProcessInstance.OriginatorUserid}','{JobNumber}','{zt}','{rwclr}','{ks}','{js}','{Math.Round(t.TotalHours, 2) - xs}')";
                                            DBUtils.Execute(Context, insertsql);
                                            hs++;
                                        }
                                    }
                                    else if (jjrks.AddDays(-1) > Convert.ToDateTime(ks.ToShortDateString()) || jjrjs < Convert.ToDateTime(ks.ToShortDateString()))
                                    {
                                        if (JobNumber != "" && JobNumber.Substring(0, 1) == "8")
                                        {
                                            string insertsql = $@"insert into HMSX_OA_SJZSB values({hs},'{spslbh}','{slid["MBNAME"].ToString()}','{rsp2.ProcessInstance.OriginatorUserid}','{JobNumber}','{zt}','{rwclr}','{ks}','{js}','{Math.Round(t.TotalHours, 2) - xs}')";
                                            DBUtils.Execute(Context, insertsql);
                                            hs++;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

