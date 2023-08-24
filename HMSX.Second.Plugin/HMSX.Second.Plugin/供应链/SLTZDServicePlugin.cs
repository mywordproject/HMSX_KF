using DingTalk.Api;
using DingTalk.Api.Request;
using DingTalk.Api.Response;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Core.Msg;
using Kingdee.BOS.JSON;
using Kingdee.BOS.Msg;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static HMSX.Second.Plugin.Tool.Results;

namespace HMSX.Second.Plugin.供应链
{
    [Description("收料通知单--检验审核日期")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class SLTZDServicePlugin : AbstractOperationServicePlugIn
    {
        readonly string[] reloadKeys = new string[] { "F_260_JHGZHBM", "F_260_XMHH", "FMtoNo" };
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FApproveDate", "F_260_WMSJYSHRQ" , "FBillNo", "F_260_XTLY", "FMaterialID", "F_260_XMHH",
                "FHMSXKH", "FMtoNo" ,"FSupplierId","FLot","FActReceiveQty","FPriceUnitId","FDemanderId","F_260_ComboSFDH"};
            foreach (String property in propertys)
            {
                e.FieldKeys.Add(property);
            }
        }
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            if (Context.CurrentOrganizationInfo.ID == 100026)
            {             
                if (FormOperation.Operation.Equals("Audit", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var data in e.DataEntitys)
                    {
                        string upsql = $@"/*dialect*/update T_PUR_Receive set F_260_WMSJYSHRQ=FAPPROVEDATE,F_260_XTLY='' where FID={data["Id"]}";
                        DBUtils.Execute(Context, upsql);
                    }
                }
                else if (FormOperation.Operation.Equals("UnAudit", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var data in e.DataEntitys)
                    {
                        string upsql = $@"/*dialect*/update T_PUR_Receive set F_260_WMSJYSHRQ=null where FID={data["Id"]}";
                        DBUtils.Execute(Context, upsql);
                        if (data["F_260_XTLY"].ToString() == "WMS")
                        {
                            //try
                            //{
                            string fsjysql = $@"select FNUMBER,FNAME from  T_BAS_PREBDFIVE a
                                           inner join T_BAS_PREBDFIVE_L b on a.FID=b.FID
                                           WHERE FNAME='执行反审核'";
                            var fsjy = DBUtils.ExecuteDynamicObject(Context, fsjysql);
                            if (fsjy.Count > 0)
                            {
                                Encoding encoding = Encoding.UTF8;
                                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(fsjy[0]["FNUMBER"].ToString());
                                request.Method = "POST";
                                request.ContentType = "application/json; charset=UTF-8";
                                request.Headers["Accept-Encoding"] = "gzip, deflate";
                                request.AutomaticDecompression = DecompressionMethods.GZip;
                                JObject jsonRoot = new JObject();
                                jsonRoot.Add("fbillno", data["BillNo"].ToString());
                                jsonRoot.Add("fbilltype", "收料通知单");
                                byte[] buffer = encoding.GetBytes(jsonRoot.ToString());
                                request.ContentLength = buffer.Length;
                                request.GetRequestStream().Write(buffer, 0, buffer.Length);
                                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                                using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                                {
                                    WMSclass jsonDatas = JsonConvert.DeserializeObject<WMSclass>(reader.ReadToEnd());
                                    if (jsonDatas.Code != "0")
                                    {
                                        throw new KDBusinessException("", jsonDatas.Message);
                                    }
                                }
                            }
                            //}
                            //catch
                            //{
                            //    throw new KDBusinessException("", "访问WMS接口异常");
                            //}
                        }
                    }
                }
                else if (FormOperation.Operation.Equals("UnTerminate", StringComparison.OrdinalIgnoreCase))
                {
                    //反终止
                    foreach (var date in e.DataEntitys)
                    {
                        string upsql = $@"/*dialect*/update T_PUR_Receive set F_260_XTLY='' where FID={date["Id"]}";
                        DBUtils.Execute(Context, upsql);
                    }
                }
                else if (FormOperation.Operation.Equals("Terminate", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var date in e.DataEntitys)
                    {
                        if (date["F_260_XTLY"].ToString() == "WMS")
                        {
                            //try
                            //{
                            string fsjysql = $@"select FNUMBER,FNAME from  T_BAS_PREBDFIVE a
                                           inner join T_BAS_PREBDFIVE_L b on a.FID=b.FID
                                           WHERE FNAME='执行关闭'";
                            var fsjy = DBUtils.ExecuteDynamicObject(Context, fsjysql);
                            if (fsjy.Count > 0)
                            {
                                Encoding encoding = Encoding.UTF8;
                                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(fsjy[0]["FNUMBER"].ToString());
                                request.Method = "POST";
                                request.ContentType = "application/json; charset=UTF-8";
                                request.Headers["Accept-Encoding"] = "gzip, deflate";
                                request.AutomaticDecompression = DecompressionMethods.GZip;
                                JObject jsonRoot = new JObject();
                                jsonRoot.Add("fbillno", date["BillNo"].ToString());
                                jsonRoot.Add("fbilltype", "收料通知单");
                                byte[] buffer = encoding.GetBytes(jsonRoot.ToString());
                                request.ContentLength = buffer.Length;
                                request.GetRequestStream().Write(buffer, 0, buffer.Length);
                                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                                using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                                {
                                    WMSclass jsonDatas = JsonConvert.DeserializeObject<WMSclass>(reader.ReadToEnd());
                                    if (jsonDatas.Code != "0")
                                    {
                                        throw new KDBusinessException("", jsonDatas.Message);
                                    }
                                }
                            }
                            //}
                            //catch
                            //{
                            //    throw new KDBusinessException("", "访问WMS接口异常");
                            //}
                        }
                    }
                }
                else if (FormOperation.Operation.Equals("Save", StringComparison.OrdinalIgnoreCase))
                {
                    if (Context.CurrentOrganizationInfo.ID == 100026)
                    {
                        foreach (var date in e.DataEntitys)
                        {
                            foreach (var entry in date["PUR_ReceiveEntry"] as DynamicObjectCollection)
                            {
                                if (((DynamicObject)entry["MaterialID"])["Number"].ToString().Substring(0, 6) == "260.02")
                                {
                                    if (entry["MtoNo"] == null || entry["MtoNo"].ToString() == "" || entry["MtoNo"].ToString() == " ")
                                    {
                                        //校验
                                        string jysql = $@"select *
                                            from T_BD_MATERIAL a
                                            left join t_BD_MaterialPlan c on c.FMATERIALID=a.FMATERIALID
                                            left join T_PLN_MANUFACTUREPOLICY d on c.FMFGPOLICYID=d.FID
                                            WHERE 
                                            --D.FNUMBER='ZZCL003_SYS'
                                            --and 
                                            SUBSTRING(a.FNUMBER,1,6)='260.02'
                                            and a.FMATERIALID={entry["MaterialID_Id"]}
                                            and a.FCREATEORGID=100026";
                                        var jy = DBUtils.ExecuteDynamicObject(Context, jysql);
                                        if (jy.Count > 0)
                                        {
                                            string str = "";
                                            string str1 = "";
                                            string khsql = $@"select FNUMBER,FSHORTNAME from T_BD_CUSTOMER a
                                                     inner join T_BD_CUSTOMER_L b ON a.FCUSTID=b.FCUSTID where a.FCUSTID={entry["FHMSXKH_Id"]}";
                                            var khs = DBUtils.ExecuteDynamicObject(Context, khsql);
                                            if (khs.Count > 0)
                                            {
                                                str = khs[0]["FSHORTNAME"].ToString();
                                                str1 = khs[0]["FNUMBER"].ToString();
                                            }
                                            foreach (var xmh in entry["F_260_XMHH"] as DynamicObjectCollection)
                                            {
                                                string xmhsql = $@"select FNUMBER,FNAME from ora_t_Cust100045 a
                                                  inner join ora_t_Cust100045_L b ON a.FID=b.FID WHERE a.FID={xmh["F_260_XMHH_Id"]}";
                                                var xmhs = DBUtils.ExecuteDynamicObject(Context, xmhsql);
                                                if (xmhs.Count > 0)
                                                {
                                                    str += "_" + xmhs[0]["FNAME"].ToString();
                                                    str1 += "_" + xmhs[0]["FNUMBER"].ToString();
                                                }
                                            }
                                            string upsql = $@"/*dialect*/ update T_PUR_ReceiveEntry set FMTONO='{str}',F_260_JHGZHBM='{str1}' where FENTRYID={entry["Id"]}";
                                            DBUtils.Execute(Context, upsql);
                                        }
                                    }
                                    else
                                    {
                                        string str2 = "";
                                        string str3 = "";
                                        string khsql2 = $@"select FNUMBER,FSHORTNAME from T_BD_CUSTOMER a
                                                     inner join T_BD_CUSTOMER_L b ON a.FCUSTID=b.FCUSTID where a.FCUSTID={entry["FHMSXKH_Id"]}";
                                        var khs2 = DBUtils.ExecuteDynamicObject(Context, khsql2);
                                        if (khs2.Count > 0)
                                        {
                                            str2 = khs2[0]["FNUMBER"].ToString();
                                            str3 = khs2[0]["FSHORTNAME"].ToString();
                                        }
                                        if (str3 != entry["MtoNo"].ToString().Substring(0, entry["MtoNo"].ToString().IndexOf('_')))
                                        {
                                            throw new KDBusinessException("", "客户标签与计划跟踪号上的客户标签不一致！");
                                        }
                                        try
                                        {
                                            string name = entry["MtoNo"].ToString().Substring(entry["MtoNo"].ToString().IndexOf('_') + 1, entry["MtoNo"].ToString().Length - (entry["MtoNo"].ToString().IndexOf('_') + 1));
                                            string xmhnamesql = $@"select e.FNUMBER,L.FNAME
                                                   from T_BD_MATERIAL a
                                                   left join t_BD_MaterialPlan c on c.FMATERIALID=a.FMATERIALID
                                                   left join T_PLN_MANUFACTUREPOLICY d on c.FMFGPOLICYID=d.FID
                                                   LEFT JOIN PAEZ_t_Cust_Entry100355 XMH ON XMH.FMATERIALID=A.FMATERIALID
                                                   LEFT JOIN ora_t_Cust100045 e on XMH.F_260_XMH=e.FID
                                                   LEFT JOIN ora_t_Cust100045_L l ON e.FID=l.FID
                                                   WHERE 
                                                   --D.FNUMBER='ZZCL003_SYS'
                                                   --and 
                                                   a.FMATERIALID={entry["MaterialID_Id"]}
                                                   and L.FNAME='{name}'
                                                   and a.FCREATEORGID=100026
                                                   and XMH.F_260_XMH is not null
                                                   order by XMH.FPKID DESC";
                                            var xmhname = DBUtils.ExecuteDynamicObject(Context, xmhnamesql);
                                            if (xmhname.Count > 0)
                                            {
                                                str2 += "_" + xmhname[0]["FNUMBER"];

                                            }
                                            string upsql = $@"/*dialect*/ update T_PUR_ReceiveEntry set F_260_JHGZHBM='{str2}' where FENTRYID={entry["Id"]}";
                                            DBUtils.Execute(Context, upsql);
                                        }
                                        catch
                                        {

                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else if(FormOperation.Operation.Equals("StatusConvertsfdh", StringComparison.OrdinalIgnoreCase))
                {
                    EndSetStatusTransactionArgs endSetStatusTransactionArgs = (EndSetStatusTransactionArgs)e;
                    List<KeyValuePair<object, object>> EntryIds = endSetStatusTransactionArgs.PkEntryIds;
                    foreach (var data in e.DataEntitys)
                    {
                        foreach (var entity in data["PUR_ReceiveEntry"] as DynamicObjectCollection)
                        {
                            foreach (var ids in EntryIds)
                            {
                                if (entity["Id"].ToString() == ids.Value.ToString())
                                {
                                    var wl = entity["MaterialID"] as DynamicObject;
                                    var msg = string.Format("您申请采购的“{0}”、“{1}”，供应商为“{2}”的“{3}”批次，已到货“{4}”，请知悉！",
                                        wl["Name"].ToString(), wl["Specification"].ToString(), ((DynamicObject)data["SupplierId"])["Name"].ToString(), entity["Lot_Text"].ToString(),
                                        entity["ActReceiveQty"].ToString() + ((DynamicObject)entity["PriceUnitId"])["Name"].ToString());
                                    // 发送消息给多人
                                    string yhsql = $@"/*dialect*/SELECT a.FUSERID, a.FNAME 用户名,yg.FMOBILE
                                            FROM T_SEC_USER a 
                                            INNER JOIN T_BD_PERSON b ON a.FLINKOBJECT = b.FPERSONID 
                                            INNER JOIN T_BD_STAFF c ON b.FPERSONID=c.FPERSONID
                                            left join T_HR_EMPINFO yg on c.FEMPINFOID=yg.fid
                                            WHERE c.FSTAFFID='{Convert.ToInt64(entity["DemanderId_Id"])}'";
                                    var yh = DBUtils.ExecuteDynamicObject(Context, yhsql);
                                    if (yh.Count > 0)
                                    {
                                        var receiverIds = new object[] { Convert.ToInt64(yh[0]["FUSERID"]) };
                                        SendMessage(this.Context, "PUR_ReceiveBill", data["Id"].ToString()
                                            , "收料通知单通知", msg, this.Context.UserId, receiverIds);

                                        IDingTalkClient client = new DefaultDingTalkClient("https://oapi.dingtalk.com/topapi/v2/user/getbymobile");
                                        OapiV2UserGetbymobileRequest req = new OapiV2UserGetbymobileRequest();
                                        req.Mobile = yh[0]["FMOBILE"].ToString();
                                        string access_token = Token();
                                        OapiV2UserGetbymobileResponse rsp = client.Execute(req, access_token);
                                        string userid = rsp.Result.Userid;
                                        dingding3(access_token, userid, msg);
                                    }
                                }
                            }
                        }
                    }
                }

            }
        }
        public override void BeforeExecuteOperationTransaction(BeforeExecuteOperationTransaction e)
        {
            base.BeforeExecuteOperationTransaction(e);
            if (Context.CurrentOrganizationInfo.ID == 100026)
            {               
                if (FormOperation.Operation.Equals("UnAudit", StringComparison.OrdinalIgnoreCase))
                {
                    //反审校验
                    foreach (Kingdee.BOS.Core.ExtendedDataEntity extended in e.SelectedRows)
                    {
                        DynamicObject dy = extended.DataEntity;
                        //反审校验
                        if (dy["F_260_XTLY"].ToString() == "WMS")
                        {
                            //try
                            //{
                            string fsjysql = $@"select FNUMBER,FNAME from  T_BAS_PREBDFIVE a
                                           inner join T_BAS_PREBDFIVE_L b on a.FID=b.FID
                                           WHERE FNAME='反审校验'";
                            var fsjy = DBUtils.ExecuteDynamicObject(Context, fsjysql);
                            if (fsjy.Count > 0)
                            {
                                Encoding encoding = Encoding.UTF8;
                                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(fsjy[0]["FNUMBER"].ToString());
                                request.Method = "POST";
                                request.ContentType = "application/json; charset=UTF-8";
                                request.Headers["Accept-Encoding"] = "gzip, deflate";
                                request.AutomaticDecompression = DecompressionMethods.GZip;
                                JObject jsonRoot = new JObject();
                                jsonRoot.Add("fbillno", dy["BillNo"].ToString());
                                jsonRoot.Add("fbilltype", "收料通知单");
                                byte[] buffer = encoding.GetBytes(jsonRoot.ToString());
                                request.ContentLength = buffer.Length;
                                request.GetRequestStream().Write(buffer, 0, buffer.Length);
                                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                                using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                                {
                                    WMSclass jsonDatas = JsonConvert.DeserializeObject<WMSclass>(reader.ReadToEnd());
                                    if (jsonDatas.Code != "0")
                                    {
                                        throw new KDBusinessException("", jsonDatas.Message);
                                    }
                                }
                            }
                            //}
                            //catch
                            //{
                            //    throw new KDBusinessException("", "访问WMS接口异常");
                            //}
                        }
                    }
                }
                else if (FormOperation.Operation.Equals("Terminate", StringComparison.OrdinalIgnoreCase))
                {
                    //终止
                    foreach (Kingdee.BOS.Core.ExtendedDataEntity extended in e.SelectedRows)
                    {
                        DynamicObject dy = extended.DataEntity;
                        if (dy["F_260_XTLY"].ToString() == "WMS")
                        {
                            //try
                            //{
                            string fsjysql = $@"select FNUMBER,FNAME from  T_BAS_PREBDFIVE a
                                           inner join T_BAS_PREBDFIVE_L b on a.FID=b.FID
                                           WHERE FNAME='关闭校验'";
                            var fsjy = DBUtils.ExecuteDynamicObject(Context, fsjysql);
                            if (fsjy.Count > 0)
                            {
                                Encoding encoding = Encoding.UTF8;
                                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(fsjy[0]["FNUMBER"].ToString());
                                request.Method = "POST";
                                request.ContentType = "application/json; charset=UTF-8";
                                request.Headers["Accept-Encoding"] = "gzip, deflate";
                                request.AutomaticDecompression = DecompressionMethods.GZip;
                                JObject jsonRoot = new JObject();
                                jsonRoot.Add("fbillno", dy["BillNo"].ToString());
                                jsonRoot.Add("fbilltype", "收料通知单");
                                byte[] buffer = encoding.GetBytes(jsonRoot.ToString());
                                request.ContentLength = buffer.Length;
                                request.GetRequestStream().Write(buffer, 0, buffer.Length);
                                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                                using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                                {
                                    WMSclass jsonDatas = JsonConvert.DeserializeObject<WMSclass>(reader.ReadToEnd());
                                    if (jsonDatas.Code != "0")
                                    {
                                        throw new KDBusinessException("", jsonDatas.Message);
                                    }
                                }
                            }
                            //}
                            //catch
                            //{
                            //    throw new KDBusinessException("", "访问WMS接口异常");
                            //}
                        }
                    }
                }
                else if (FormOperation.Operation.Equals("Save", StringComparison.OrdinalIgnoreCase))
                {
                    if (Context.CurrentOrganizationInfo.ID == 100026)
                    {
                        long i = 0;
                        foreach (ExtendedDataEntity extended in e.SelectedRows)
                        {
                            DynamicObject dates = extended.DataEntity;
                            foreach (var date in dates["PUR_ReceiveEntry"] as DynamicObjectCollection)
                            {
                                if (((DynamicObject)date["MaterialID"])["Number"].ToString().Substring(0, 6) == "260.02" &&
                                    (date["F_260_XMHH"] as DynamicObjectCollection).Count == 0 &&
                                    (date["MtoNo"] == null || date["MtoNo"].ToString() == "" || date["MtoNo"].ToString() == " "))
                                {
                                    string cxsql = $@"select 
                                        XMH.F_260_XMH,XMH.FPKID
                                        from T_BD_MATERIAL a
                                        left join t_BD_MaterialPlan c on c.FMATERIALID=a.FMATERIALID
                                        left join T_PLN_MANUFACTUREPOLICY d on c.FMFGPOLICYID=d.FID
                                        LEFT JOIN PAEZ_t_Cust_Entry100355 XMH ON XMH.FMATERIALID=A.FMATERIALID
                                        WHERE 
                                        --D.FNUMBER='ZZCL003_SYS'
                                        --and 
                                        a.FMATERIALID={date["MaterialID_Id"]}
                                        and FCREATEORGID=100026
                                        and XMH.F_260_XMH is not null
                                        order by XMH.FPKID DESC";
                                    var cxs = DBUtils.ExecuteDynamicObject(Context, cxsql);
                                    if (cxs.Count > 0)
                                    {
                                        i++;
                                        //FMULTITACCTBOOKID 是多选账簿，首先获取多选账簿的属性类型
                                        var dyc = new DynamicObject((date["F_260_XMHH"] as DynamicObjectCollection).DynamicCollectionItemPropertyType);
                                        long id = 0;
                                        string xmhsql = $@"select MIN(FPKID)FPKID FROM PAEZ_t_Cust_Entry100366";
                                        var xmh = DBUtils.ExecuteDynamicObject(Context, xmhsql);
                                        if (xmh.Count > 0)
                                        {
                                            id += Convert.ToInt64(xmh[0]["FPKID"]);
                                        }
                                        if ((id - i) == 0)
                                        {
                                            i++;
                                        }
                                        //给基础资料的Id赋值
                                        dyc["PKID"] = id - i;
                                        //单个的账簿Id对应的账簿实体
                                        dyc["F_260_XMHH_Id"] = cxs[0]["F_260_XMH"];
                                        (date["F_260_XMHH"] as DynamicObjectCollection).Add(dyc);
                                    }
                                }
                            }
                            //foreach (var date in dates["PLN_FORECASTENTRY"] as DynamicObjectCollection)
                            //{
                            //    if (((DynamicObject)date["MaterialID"])["Number"].ToString().Substring(0, 6) == "260.02")
                            //    {
                            //        if ((date["F_260_XMHH"] as DynamicObjectCollection).Count == 0)
                            //        {
                            //            throw new KDBusinessException("", "项目号未选择，不允许提交！");
                            //        }
                            //    }
                            //}
                        }
                    }
                }
            }
        }
        public override void OnPrepareOperationServiceOption(OnPrepareOperationServiceEventArgs e)
        {
            base.OnPrepareOperationServiceOption(e);
            if (Context.CurrentOrganizationInfo.ID == 100026)
            {
                if (this.FormOperation.OperationId == 8)
                {
                    if (!string.IsNullOrWhiteSpace(this.FormOperation.LoadKeys) && this.FormOperation.LoadKeys != "null")
                    {
                        // 设置操作完后刷新字段
                        var loadKeys = KDObjectConverter.DeserializeObject<List<string>>(this.FormOperation.LoadKeys);
                        if (loadKeys == null)
                        {
                            loadKeys = new List<string>();
                        }
                        foreach (var reloadKey in reloadKeys)
                        {
                            if (!loadKeys.Contains(reloadKey))
                            {
                                loadKeys.Add(reloadKey);
                            }
                        }
                        this.FormOperation.LoadKeys = KDObjectConverter.SerializeObject(loadKeys);
                    }
                }
            }
        }
       
        /// <summary>
        /// 发送消息（支持多个收件人，写发件箱）
        /// </summary>
        /// <param name="ctx">上下文</param>
        /// <param name="formId">业务对象标识(不可以使用扩展的标识)</param>
        /// <param name="pkId">单据主键值</param>
        /// <param name="title">标题</param>
        /// <param name="content">内容</param>
        /// <param name="senderId">发生人内码</param>
        /// <param name="receiverIds">接收人内码集合</param>
        private static void SendMessage(Context ctx, string formId, string pkId, string title, string content, long senderId, object[] receiverIds)
        {
            var businessInfo = FormMetaDataCache.GetCachedFormMetaData(ctx, "WF_MessageSendBill").BusinessInfo;
            var receiverField = (MulBaseDataField)businessInfo.GetField("FRECEIVERS");
            var dt = businessInfo.GetDynamicObjectType();
            var dataObject = new DynamicObject(dt);
            dataObject["TYPE"] = ((int)MsgType.CommonMessage).ToString();
            dataObject["SENDERID_Id"] = senderId;
            FieldSetValue(ctx, receiverField, dataObject, receiverIds);
            dataObject["Title"] = title;
            dataObject["Content"] = content;
            dataObject["ObjectTypeId_Id"] = formId;
            dataObject["KeyValue"] = pkId;
            dataObject["CREATETIME"] = DateTime.Now;
            // 保存消息
            var msgSend = new MessageSend(dataObject);
            msgSend.KeyValue = pkId;
            SMSServiceHelper.SendMessage(ctx, msgSend, true);
        }
        /// <summary>
        /// 发送消息（精简版，仅支持一个收件人，不写发件箱）
        /// </summary>
        /// <param name="ctx">上下文</param>
        /// <param name="formId">业务对象标识(不可以使用扩展的标识)</param>
        /// <param name="pkId">单据主键值</param>
        /// <param name="title">标题</param>
        /// <param name="content">内容</param>
        /// <param name="senderId">发生人内码</param>
        /// <param name="receiverId">接收人内码</param>
        private static void SendMessage(Context ctx, string formId, string pkId, string title, string content, long senderId, long receiverId)
        {
            Message msg = new DynamicObject(Message.MessageDynamicObjectType);
            msg.MessageId = SequentialGuid.NewGuid().ToString();
            msg.MsgType = MsgType.CommonMessage;
            msg.SenderId = senderId;
            msg.ReceiverId = receiverId;
            msg.Title = title;
            msg.Content = content;
            msg.ObjectTypeId = formId;
            msg.KeyValue = pkId;
            msg.CreateTime = DateTime.Now;
            // 保存消息
            var dataManager = DataManagerUtils.GetDataManager(Message.MessageDynamicObjectType, new OLEDbDriver(ctx));
            dataManager.Save(msg.DataEntity);
        }
        /// <summary>
        /// 多选基础资料字段赋值（通过字段修改数据包）
        /// </summary>
        /// <param name="ctx">上下文</param>
        /// <param name="field">多选基础资料字段</param>
        /// <param name="entityObj">多选基础资料字段所在数据行</param>
        /// <param name="pkValues">多个基础资料的内码的集合</param>
        private static void FieldSetValue(Context ctx, MulBaseDataField field, DynamicObject entityObj, object[] pkValues)
        {
            // 获取多选基础资料字段的数据包集合
            var mulBaseDataEntitySet = field.GetFieldValue(entityObj) as DynamicObjectCollection;
            if (mulBaseDataEntitySet == null)
            {
                mulBaseDataEntitySet = new DynamicObjectCollection(field.RefEntityDynamicObjectType, entityObj);
                field.RefEntityDynamicProperty.SetValue(entityObj, mulBaseDataEntitySet);
            }
            mulBaseDataEntitySet.Clear();
            // 从数据库读取指定的基础资料的数据包，并填充到当前多选基础资料字段的数据包集合中
            var baseDataObjects = BusinessDataServiceHelper.LoadFromCache(ctx, pkValues, field.RefFormDynamicObjectType);
            foreach (var baseDataObject in baseDataObjects)
            {
                var mulBaseDataEntity = new DynamicObject(field.RefEntityDynamicObjectType);
                mulBaseDataEntitySet.Add(mulBaseDataEntity);
                field.RefIDDynamicProperty.SetValue(mulBaseDataEntity, baseDataObject[0]);
                field.DynamicProperty.SetValue(mulBaseDataEntity, baseDataObject);
            }
        }


        public static string Token()//登录钉钉
        {
            IDingTalkClient dlclient = new DefaultDingTalkClient("https://oapi.dingtalk.com/gettoken");
            OapiGettokenRequest dlreq = new OapiGettokenRequest();
            dlreq.Appkey = "ding4zkzmf7yz0l5neya";
            dlreq.Appsecret = "xRmBxtjJQh9DwPGrz8HG9hMWqw0xOj9IySBvZR1Ga0iVoQ1YwA1PmFFDxhEAgSvJ";
            dlreq.SetHttpMethod("GET");
            OapiGettokenResponse dlrsp = dlclient.Execute(dlreq);
            DING_Token get = new DING_Token();
            get = JsonConvert.DeserializeObject<DING_Token>(dlrsp.Body);
            string access_token = get.Access_token;
            return access_token;
        }
        private void dingding3(string token, string id, string value)
        {

            IDingTalkClient client = new DefaultDingTalkClient("https://oapi.dingtalk.com/topapi/message/corpconversation/asyncsend_v2");
            OapiMessageCorpconversationAsyncsendV2Request req = new OapiMessageCorpconversationAsyncsendV2Request();
            req.AgentId = 1870548521L;
            req.UseridList = id;
            OapiMessageCorpconversationAsyncsendV2Request.MsgDomain obj1 = new OapiMessageCorpconversationAsyncsendV2Request.MsgDomain();
            obj1.Msgtype = "text";
            OapiMessageCorpconversationAsyncsendV2Request.TextDomain obj2 = new OapiMessageCorpconversationAsyncsendV2Request.TextDomain();
            obj2.Content = value;
            obj1.Text = obj2;
            OapiMessageCorpconversationAsyncsendV2Request.OADomain obj3 = new OapiMessageCorpconversationAsyncsendV2Request.OADomain();
            OapiMessageCorpconversationAsyncsendV2Request.BodyDomain obj4 = new OapiMessageCorpconversationAsyncsendV2Request.BodyDomain();
            obj4.Content = value;
            obj3.Body = obj4;
            obj1.Oa = obj3;
            req.Msg_ = obj1;
            OapiMessageCorpconversationAsyncsendV2Response rsp = client.Execute(req, token);
            if (rsp.Errmsg == "ok")
            {

            }
        }
    }
}
