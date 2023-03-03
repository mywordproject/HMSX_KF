using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.JSON;
using Kingdee.BOS.Orm.DataEntity;
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

namespace HMSX.Second.Plugin.供应链
{
    [Description("委外订单--项目号、计划跟踪号")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class WWDDServicePlugin : AbstractOperationServicePlugIn
    {
        readonly string[] reloadKeys = new string[] {"F_260_XMHH", "FMtoNo" };
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FMaterialId", "F_260_KH", "F_260_XMHH", "FBillNo" };
            foreach (String property in propertys)
            {
                e.FieldKeys.Add(property);
            }
        }
        public override void BeforeExecuteOperationTransaction(BeforeExecuteOperationTransaction e)
        {
            base.BeforeExecuteOperationTransaction(e);
            if (FormOperation.Operation.Equals("Save", StringComparison.OrdinalIgnoreCase))
            {
                if (Context.CurrentOrganizationInfo.ID == 100026)
                {
                    long i = 0;
                    foreach (ExtendedDataEntity extended in e.SelectedRows)
                    {
                        DynamicObject dates = extended.DataEntity;
                        foreach (var date in dates["TreeEntity"] as DynamicObjectCollection)
                        {
                            if (((DynamicObject)date["MaterialId"])["Number"].ToString().Substring(0, 6) == "260.02" &&
                                (date["F_260_XMHH"] as DynamicObjectCollection).Count == 0)
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
                                        a.FMATERIALID={date["MaterialId_Id"]}
                                        and FCREATEORGID=100026
                                       and XMH.F_260_XMH is not null
                                        order by XMH.FPKID desc";
                                var cxs = DBUtils.ExecuteDynamicObject(Context, cxsql);
                                if (cxs.Count > 0)
                                {
                                    i++;
                                    long id = 0;
                                    string xmhsql = $@"select MIN(FPKID)FPKID FROM PAEZ_t_Cust_Entry100364";
                                    var xmh = DBUtils.ExecuteDynamicObject(Context, xmhsql);
                                    if (xmh.Count > 0)
                                    {
                                        id += Convert.ToInt64(xmh[0]["FPKID"]);
                                    }
                                    var dyc = new DynamicObject((date["F_260_XMHH"] as DynamicObjectCollection).DynamicCollectionItemPropertyType);
                                    //给基础资料的Id赋值
                                    if ((id - i) == 0)
                                    {
                                        i++;
                                    }
                                    dyc["PKID"] = id - i;
                                    //单个的账簿Id对应的账簿实体
                                    dyc["F_260_XMHH_Id"] = cxs[0]["F_260_XMH"];
                                    (date["F_260_XMHH"] as DynamicObjectCollection).Add(dyc);

                                }
                            }

                        }
                        //foreach (var date in dates["TreeEntity"] as DynamicObjectCollection)
                        //{
                        //    if (((DynamicObject)date["MaterialId"])["Number"].ToString().Substring(0, 6) == "260.02")
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
            else if (FormOperation.Operation.Equals("ToClose", StringComparison.OrdinalIgnoreCase) ||
                   FormOperation.Operation.Equals("ForceClose", StringComparison.OrdinalIgnoreCase))
            {
                //结案校验
                foreach (ExtendedDataEntity extended in e.SelectedRows)
                {
                    DynamicObject dy = extended.DataEntity;
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
                        jsonRoot.Add("fbilltype", "委外订单");
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
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            if (FormOperation.Operation.Equals("Save", StringComparison.OrdinalIgnoreCase))
            {
                if (Context.CurrentOrganizationInfo.ID == 100026)
                {
                    foreach (var date in e.DataEntitys)
                    {
                        foreach (var entry in date["TreeEntity"] as DynamicObjectCollection)
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
                                            and a.FMATERIALID={entry["MaterialId_Id"]}
                                            and a.FCREATEORGID=100026";
                            var jy = DBUtils.ExecuteDynamicObject(Context, jysql);
                            if (jy.Count > 0)
                            {
                                string str = "";
                                string khsql = $@"select FSHORTNAME from T_BD_CUSTOMER_L where FCUSTID={entry["F_260_KH_Id"]}";
                                var khs = DBUtils.ExecuteDynamicObject(Context, khsql);
                                if (khs.Count > 0)
                                {
                                    str = khs[0]["FSHORTNAME"].ToString();
                                }
                                foreach (var xmh in entry["F_260_XMHH"] as DynamicObjectCollection)
                                {
                                    string xmhsql = $@"select FNAME from ora_t_Cust100045_L WHERE FID={xmh["F_260_XMHH_Id"]}";
                                    var xmhs = DBUtils.ExecuteDynamicObject(Context, xmhsql);
                                    if (xmhs.Count > 0)
                                    {
                                        str += "_" + xmhs[0]["FNAME"].ToString();
                                    }
                                }
                                string upsql = $@"/*dialect*/ update T_SUB_REQORDERENTRY set FMTONO='{str}' where FENTRYID={entry["Id"]}";
                                DBUtils.Execute(Context, upsql);
                            }
                        }
                    }
                }
            }
            else if (FormOperation.Operation.Equals("ToStart", StringComparison.OrdinalIgnoreCase) ||
                    FormOperation.Operation.Equals("UndoToStart", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var date in e.DataEntitys)
                {
                    foreach (var entry in date["TreeEntity"] as DynamicObjectCollection)
                    {
                        string upsql = $@"/*dialect*/update T_SUB_PPBOM set F_260_XTLY='' where FSUBBILLNO='{date["BillNo"]}' and FSUBREQENTRYSEQ='{entry["Seq"]}'";
                        DBUtils.Execute(Context, upsql);
                    }
                }
            }
            else if (FormOperation.Operation.Equals("ToClose", StringComparison.OrdinalIgnoreCase) ||
                    FormOperation.Operation.Equals("ForceClose", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var date in e.DataEntitys)
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
                        jsonRoot.Add("fbilltype", "委外订单");
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
    }
}
