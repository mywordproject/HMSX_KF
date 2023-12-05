using HMSX.Second.Plugin.供应链;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm;
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

namespace HMSX.Second.Plugin.生产制造
{
    [Description("调拨申请单--校验相同物料不允许有相同批号")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class DBSQDServerPlugin : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FAPPORGID", "FMATERIALID", "FLot", "FBillNo", "F_260_XTLY", "F_260_DXGYS", "FQty", "FStockId",
                "F_260_SFSCYLQD" ,"F_260_SFBMZGK","F_SFBMZGK"};
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
                if (FormOperation.Operation.Equals("Save", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (ExtendedDataEntity extended in e.SelectedRows)
                    {
                        int sf = 0;
                        DynamicObject dy = extended.DataEntity;
                        DynamicObjectCollection Entitys = dy["STK_STKTRANSFERAPPENTRY"] as DynamicObjectCollection;
                        for (int i = 0; i < Entitys.Count - 1; i++)
                        {
                            if (Entitys.Count > 1)
                            {
                                for (int j = i + 1; j <= Entitys.Count - 1; j++)
                                {
                                    if (Entitys[i]["FLot_Text"]!= null && Entitys[i]["FLot_Text"].ToString()!="" 
                                        && Entitys[i]["MATERIALID_Id"].ToString() == Entitys[j]["MATERIALID_Id"].ToString() &&
                                        Entitys[i]["FLot_Text"].ToString() == Entitys[j]["FLot_Text"].ToString())
                                    {
                                        throw new KDBusinessException("", "相同物料不允许有相同批号！");
                                    }
                                }
                            }
                        }
                        for (int i = 0; i < Entitys.Count; i++)
                        {
                            if (((DynamicObjectCollection)((DynamicObject)Entitys[i]["MATERIALID"])["MaterialStock"]).Count > 0 &&
                                Convert.ToBoolean(((DynamicObjectCollection)((DynamicObject)Entitys[i]["MATERIALID"])["MaterialStock"])[0]["IsBatchManage"]))
                            {
                                if (Entitys[i]["F_260_DXGYS"] != null && Entitys[i]["F_260_DXGYS"].ToString() != "" && Entitys[i]["F_260_DXGYS"].ToString() != " ")
                                {
                                    string gysname = "";
                                    foreach (var sup in Entitys[i]["F_260_DXGYS"].ToString().Split(';'))
                                    {
                                        gysname += "'" + sup + "',";
                                    }
                                    string strsql = $@"select  a.fid,A.FMATERIALID,FLOT,c.FNAME from T_STK_INVENTORY a
                                     left join T_BD_LOTMASTER b on a.FLOT= b.FLOTid
                                     left join T_BD_SUPPLier_l c on c.FSUPPLIERID=b.FSUPPLYID
                                     where FSTOCKORGID=100026 and a.FMATERIALID='{Entitys[i]["MATERIALID_Id"]}'
                                     and B.FNUMBER='{Entitys[i]["FLot_Text"]}'
                                     and (c.Fname in ({gysname.Trim(',')}))";
                                    var strs = DBUtils.ExecuteDynamicObject(Context, strsql);
                                    if (strs.Count == 0)
                                    {
                                        var log = String.Format("明细第" + Entitys[i]["Seq"].ToString() + "行，选择的批号对应的供应商不属于POR供应商！请确认是否申请");
                                        this.OperationResult.OperateResult.Add(new OperateResult()
                                        {
                                            MessageType = MessageType.FatalError,
                                            Message = string.Format("{0}", log),
                                            Name = "",
                                            SuccessStatus = true,
                                        });
                                        this.OperationResult.IsShowMessage = true;
                                        sf += 1;
                                        Entitys[i]["F_260_SFBMZGK"] = true;
                                        // throw new KDBusinessException("", "明细第" + Entitys[i]["Seq"].ToString() + "行，选择的批号对应的供应商不属于POR供应商！");
                                    }
                                    else
                                    {
                                        Entitys[i]["F_260_SFBMZGK"] = false;
                                    }

                                }
                                else
                                {
                                    Entitys[i]["F_260_SFBMZGK"] = false;
                                }
                            }
                            else
                            {
                                Entitys[i]["F_260_SFBMZGK"] = false;
                            }

                        }
                        if (sf > 0)
                        {
                            dy["F_SFBMZGK"] = true;
                        }
                        else
                        {
                            dy["F_SFBMZGK"] = false;
                        }
                        if (dy["F_260_SFSCYLQD"] != null && Convert.ToBoolean(dy["F_260_SFSCYLQD"]) == true)
                        {
                            //var ppBominfosum = (from pp in Entitys select new { MATERIALID_Id = Convert.ToInt64(pp["MATERIALID_Id"]),FLot_Text = Convert.ToInt64(pp["FLot_Text"]) }).Distinct().ToList();
                            //var x=Entitys.ToList().GroupBy((x, y) => new { x["MATERIALID_Id"], x.Pno, x.Sno, y.Sum(a => a.TotalNums) }).ToList();
                            var ppBominfosum = from d1 in Entitys
                                               group d1 by new
                                               {
                                                   MATERIALID = d1["MATERIALID_Id"],
                                                   FLot = d1["FLot_Text"],
                                                   GYS = d1["F_260_DXGYS"]==null?"": d1["F_260_DXGYS"].ToString(),
                                                   CK = d1["StockId_Id"],
                                                   SFGK =Convert.ToBoolean(d1["F_260_SFBMZGK"]),
                                                   PHGL = ((DynamicObjectCollection)((DynamicObject)d1["MATERIALID"])["MaterialStock"]).Count > 0 ? Convert.ToBoolean(((DynamicObjectCollection)((DynamicObject)d1["MATERIALID"])["MaterialStock"])[0]["IsBatchManage"]) : false
                                               }
                            into s
                                               select new
                                               {
                                                   MATERIALID = s.Select(p => p["MATERIALID_Id"]).First(),
                                                   FLot = s.Select(p => p["FLot_Text"]).First(),
                                                   GYS = s.Select(p => p["F_260_DXGYS"]==null?"": p["F_260_DXGYS"].ToString()).First(),
                                                   CK = s.Select(p => p["StockId_Id"]).First(),
                                                   SFGK = s.Select(p =>Convert.ToBoolean(p["F_260_SFBMZGK"])).First(),
                                                   PHGL = s.Select(p => ((DynamicObjectCollection)((DynamicObject)p["MATERIALID"])["MaterialStock"]).Count > 0 ? Convert.ToBoolean(((DynamicObjectCollection)((DynamicObject)p["MATERIALID"])["MaterialStock"])[0]["IsBatchManage"]) : false).First(),
                                                   Qty = s.Sum(p => Convert.ToDecimal(p["Qty"]))
                                               };
                            var ppBominfosum1 = ppBominfosum.OrderBy(p => p.MATERIALID).ThenBy(k => k.FLot).ToList();
                            var wlfz = (from p in ppBominfosum1 select new { MATERIALID = Convert.ToInt64(p.MATERIALID) }).Distinct();
                            foreach (var wl in wlfz)
                            {
                                var x = (from pp in ppBominfosum1 where Convert.ToInt64(pp.MATERIALID) == wl.MATERIALID select pp);
                                var wlmx = (from pp in ppBominfosum1 where Convert.ToInt64(pp.MATERIALID) == wl.MATERIALID && pp.SFGK == false && pp.PHGL == true && pp.GYS!=null && pp.GYS !="" && pp.GYS != " " select pp).OrderBy(p => p.MATERIALID).ThenBy(k => k.FLot).ToList();
                                for (int i = 0; i < wlmx.Count; i++)
                                {
                                    string gysname = "";
                                    if (wlmx[i].GYS != null && wlmx[i].GYS.ToString()!="" && wlmx[i].GYS.ToString() != " ")
                                    {
                                        foreach (var sup in wlmx[i].GYS.ToString().Split(';'))
                                        {
                                            gysname += "'" + sup + "',";
                                        }
                                        if (gysname != "" && gysname != " " && wlmx[i].GYS.ToString() != "" && wlmx[i].GYS.ToString() != " ")
                                        {
                                            if (i == wlmx.Count - 1)
                                            {
                                                string strsql = $@"
                                    select * FROM (
                                     select TOP {i + 1} a.fid,A.FMATERIALID,FLOT,c.FNAME,B.FNUMBER,FBASEQTY from T_STK_INVENTORY a
                                     left join T_BD_LOTMASTER b on a.FLOT= b.FLOTid
                                     left join T_BD_SUPPLier_l c on c.FSUPPLIERID=b.FSUPPLYID
                                     where FSTOCKORGID=100026 and a.FMATERIALID='{wlmx[i].MATERIALID}' AND a.FBASEQTY>0 AND  a.FStockId={wlmx[i].CK}
                                     and a.FSTOCKSTATUSID=case when a.FStockId in (22315406,31786848) then 27910195 else 10000 end
                                     and (c.Fname in ({gysname.Trim(',')})) order by B.FNUMBER)A
                                     WHERE  FNUMBER='{wlmx[i].FLot}'";
                                                var strs = DBUtils.ExecuteDynamicObject(Context, strsql);
                                                if (strs.Count == 0)
                                                {
                                                    throw new KDBusinessException("", "选择的POR供应商对应的批号没有遵循先进先出原则！");
                                                }
                                            }
                                            else
                                            {
                                                string strsql = $@"
                                    select * FROM (
                                     select TOP {i + 1} a.fid,A.FMATERIALID,FLOT,c.FNAME,B.FNUMBER,FBASEQTY from T_STK_INVENTORY a
                                     left join T_BD_LOTMASTER b on a.FLOT= b.FLOTid
                                     left join T_BD_SUPPLier_l c on c.FSUPPLIERID=b.FSUPPLYID
                                     where FSTOCKORGID=100026 and a.FMATERIALID='{wlmx[i].MATERIALID}' AND a.FBASEQTY>0 AND a.FStockId={wlmx[i].CK}
                                     and a.FSTOCKSTATUSID=case when a.FStockId in (22315406,31786848) then 27910195 else 10000 end
                                     and (c.Fname in ({gysname.Trim(',')}))order by B.FNUMBER )A
                                     WHERE  FNUMBER='{wlmx[i].FLot}' AND FBASEQTY={wlmx[i].Qty}";
                                                var strs = DBUtils.ExecuteDynamicObject(Context, strsql);
                                                if (strs.Count == 0)
                                                {
                                                    throw new KDBusinessException("", "选择的POR供应商对应的批号没有遵循先进先出原则！");
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                        }
                    }
                }
                else if (FormOperation.Operation.Equals("UnAudit", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (ExtendedDataEntity extended in e.SelectedRows)
                    {
                        DynamicObject dy = extended.DataEntity;
                        //反审校验
                        if (dy["F_260_XTLY"].ToString() == "WMS")
                        {
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
                                jsonRoot.Add("fbilltype", "调拨申请单");
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
                                jsonRoot.Add("fbilltype", "调拨申请单");
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

        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            if (Context.CurrentOrganizationInfo.ID == 100026)
            {
                if (FormOperation.Operation.Equals("Audit", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var date in e.DataEntitys)
                    {
                        string upsql = $@"/*dialect*/update T_STK_STKTRANSFERAPP set F_260_XTLY='' where FID={date["Id"]}";
                        DBUtils.Execute(Context, upsql);
                    }
                }
                else if (FormOperation.Operation.Equals("BillUnClose", StringComparison.OrdinalIgnoreCase))
                {
                    //整单反关闭
                    foreach (var date in e.DataEntitys)
                    {
                        string upsql = $@"/*dialect*/update T_STK_STKTRANSFERAPP set F_260_XTLY='' where FID={date["Id"]}";
                        DBUtils.Execute(Context, upsql);
                    }
                }
                else if (FormOperation.Operation.Equals("UnAudit", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var date in e.DataEntitys)
                    {
                        if (date["F_260_XTLY"].ToString() == "WMS")
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
                                jsonRoot.Add("fbillno", date["BillNo"].ToString());
                                jsonRoot.Add("fbilltype", "调拨申请单");
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
                                jsonRoot.Add("fbilltype", "调拨申请单");
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
    }
}
