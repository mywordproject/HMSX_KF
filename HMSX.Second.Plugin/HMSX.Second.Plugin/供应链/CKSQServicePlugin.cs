using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
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
    [Description("出库申请单--校验、重置")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class CKSQServicePlugin: AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = {"FBillNo", "F_260_XTLY", "FMaterialId" };
            foreach (String property in propertys)
            {
                e.FieldKeys.Add(property);
            }
        }
        public override void BeforeExecuteOperationTransaction(BeforeExecuteOperationTransaction e)
        {
            base.BeforeExecuteOperationTransaction(e);
            if (Context.CurrentOrganizationInfo.ID == 100026)
            {
                if (FormOperation.Operation.Equals("UnAudit", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (ExtendedDataEntity extended in e.SelectedRows)
                    {
                        DynamicObject dy = extended.DataEntity;
                        //反审校验
                        if (dy["F_260_XTLY"].ToString() == "WMS")
                        {
                            // try
                            // {
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
                                jsonRoot.Add("fbilltype", "出库申请单");
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
                           // }
                           // catch
                           // {
                           //     throw new KDBusinessException("", "访问WMS接口异常");
                           // }
                        }
                    }
                }
                else if (FormOperation.Operation.Equals("BillClose", StringComparison.OrdinalIgnoreCase))
                {
                    //整单关闭校验
                    foreach (ExtendedDataEntity extended in e.SelectedRows)
                    {
                        DynamicObject dy = extended.DataEntity;
                        if (dy["F_260_XTLY"].ToString() == "WMS")
                        {
                            // try
                            // {
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
                                jsonRoot.Add("fbilltype", "出库申请单");
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
            }
        }
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            if (Context.CurrentOrganizationInfo.ID == 100026)
            {
                if (FormOperation.Operation.Equals("Audit", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var date in e.DataEntitys)
                    {
                        string upsql = $@"/*dialect*/update T_STK_OUTSTOCKAPPLY set F_260_XTLY='' where FID={date["Id"]}";
                        DBUtils.Execute(Context, upsql);
                        DynamicObjectCollection Entitys = date["BillEntry"] as DynamicObjectCollection;
                        foreach (var entity in Entitys)
                        {
                            string sql = $@"/*dialect*/select wl.fmaterialid,wl.FNUMBER,wlz.FNAME,sqdb.F_HMD_QTY,cksq.fqty,
                             case when sqdb.F_HMD_QTY<=cksq.fqty then '是'
                             else '否' end 是否按时交付
                             from HMD_t_Cust_Entry100111 sqdb
                             left join HMD_t_Cust100134 sqda on sqda.fid=sqdb.fid
                             left join T_BD_MATERIAL wl on sqdb.F_260_SBBM=wl.fmaterialid
                             left join T_BD_MATERIAL_L wlz on wlz.fmaterialid=wl.fmaterialid
                             left join (
                             select a.fmaterialid,sum(a.fqty) fqty
                             from HMD_t_Cust_Entry100111
                             left join (
                             select T_STK_OUTSTOCKAPPLYENTRY.fmaterialid,T_STK_OUTSTOCKAPPLYENTRY.fqty,convert(varchar,T_STK_OUTSTOCKAPPLY.fdate,23) fdate
                             from T_STK_OUTSTOCKAPPLYENTRY
                             left join T_STK_OUTSTOCKAPPLY on T_STK_OUTSTOCKAPPLY.fid=T_STK_OUTSTOCKAPPLYENTRY.fid
                             left join T_BD_MATERIAL wl on T_STK_OUTSTOCKAPPLYENTRY.fmaterialid=wl.fmaterialid and wl.FNUMBER like '%260.08%'
                             where T_STK_OUTSTOCKAPPLY.FDOCUMENTSTATUS='C') a on HMD_t_Cust_Entry100111.F_260_SBBM=a.fmaterialid
                             where a.fdate<=HMD_t_Cust_Entry100111.F_260_JHJFRQSYBMYS
                             group by a.fmaterialid
                             ) cksq on cksq.fmaterialid=sqdb.F_260_SBBM                    
                             where sqda.fbilltypeid1='64112ee428922a'
                             and cksq.fmaterialid='{entity["MaterialId_Id"]}'
                             and sqda.FDOCUMENTSTATUS='C'and sqdb.FBILLSTATUS1='B' and ((sqdb.F_260_SBXZGZ='2' and sqdb.F_260_YGJE>='20000') or sqdb.F_260_SBXZGZ='1')";
                            var cks = DBUtils.ExecuteDynamicObject(Context, sql);
                            foreach (var ck in cks)
                            {
                                if (ck["是否按时交付"].ToString() == "是")
                                {
                                    string upsql1 = $@"/*dialect*/update HMD_t_Cust_Entry100111 set F_260_SFASJF='1' where F_260_SBBM={ck["fmaterialid"]}";
                                    DBUtils.Execute(Context, upsql1);
                                }
                                else
                                {
                                    string upsql1 = $@"/*dialect*/update HMD_t_Cust_Entry100111 set F_260_SFASJF='0' where F_260_SBBM={ck["fmaterialid"]}";
                                    DBUtils.Execute(Context, upsql1);
                                }

                            }
                            string upsql2 = $@"/*dialect*/update HMD_t_Cust_Entry100111 set  F_260_SJJFRQJFSYBM='{date["ApproveDate"]}' where F_260_SBBM='{entity["MaterialId_Id"]}'";
                            DBUtils.Execute(Context, upsql2);
                        }
                    }
                }
                else if (FormOperation.Operation.Equals("BillUnClose", StringComparison.OrdinalIgnoreCase))
                {
                    //整单反关闭
                    foreach (var date in e.DataEntitys)
                    {
                        string upsql = $@"/*dialect*/update T_STK_OUTSTOCKAPPLY set F_260_XTLY='' where FID={date["Id"]}";
                        DBUtils.Execute(Context, upsql);
                    }
                }
                else if (FormOperation.Operation.Equals("UnAudit", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var date in e.DataEntitys)
                    {
                        if (date["F_260_XTLY"].ToString() == "WMS")
                        {
                            // try
                            // {
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
                                jsonRoot.Add("fbillno", date["BillNo"].ToString());
                                jsonRoot.Add("fbilltype", "出库申请单");
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
                           // }
                           // catch
                           // {
                           //     throw new KDBusinessException("", "访问WMS接口异常");
                           // }
                        }
                    }
                }
                else if (FormOperation.Operation.Equals("BillClose", StringComparison.OrdinalIgnoreCase))
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
                                jsonRoot.Add("fbilltype", "出库申请单");
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
                           // }
                           // catch
                           // {
                           //     throw new KDBusinessException("", "访问WMS接口异常");
                           // }
                        }
                    }
                }
            }
        }
    }
}
