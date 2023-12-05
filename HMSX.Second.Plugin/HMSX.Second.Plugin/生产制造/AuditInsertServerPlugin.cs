using HMSX.Second.Plugin.供应链;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List.PlugIn;
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

namespace HMSX.Second.Plugin
{
    [Description("用料清单保存时--更新仓库")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class AuditInsertServerPlugin : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FStockID", "FIssueType", "FPrdOrgId", "FNumerator", "FMaterialID","FWorkshopID","FIssueType",
                                   "F_260_XTLY", "FBillNo", "FMOBillNO" , "FMaterialID2" ,"F_260_DXGYS","FBOMID"};
            foreach (String property in propertys)
            {
                e.FieldKeys.Add(property);
            }
        }
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            if (this.Context.CurrentOrganizationInfo.ID == 100026)
            {
                if (FormOperation.Operation.Equals("Audit", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var date in e.DataEntitys)
                    {
                        string upsql = $@"/*dialect*/update T_PRD_PPBOM set F_260_XTLY='' where FID={date["Id"]}";
                        DBUtils.Execute(Context, upsql);
                    }
                    //python挂起
                    //foreach (var date in e.DataEntitys)
                    //{
                    //    DynamicObjectCollection entrydates = (DynamicObjectCollection)date["PPBomEntry"];
                    //    foreach (var entrydate in entrydates)
                    //    {
                    //        if (entrydate["IssueType"].ToString() == "3" && (DynamicObject)entrydate["StockID"] == null ? false : ((DynamicObject)entrydate["StockID"])["Name"].ToString().Contains("线边仓"))
                    //        {
                    //            string cxsql = $@"select * from t_PgBomInfo where FPPBomEntryId='{entrydate["Id"]}'";
                    //            var cxs = DBUtils.ExecuteDynamicObject(Context, cxsql);
                    //            if (cxs.Count > 0)
                    //            {
                    //                foreach (var cx in cxs)
                    //                {
                    //                    if (cx["FPickQty"].ToString() == "0")
                    //                    {
                    //                        string delsql = $@"delete t_PgBomInfo where FPgENTRYID='{cx["FPgENTRYID"]}'";
                    //                        DBUtils.Execute(Context, delsql);
                    //                    }
                    //                    else
                    //                    {
                    //                        string upsql = $@"update t_PgBomInfo set fmustqty=fpgqty*{double.Parse(entrydate["Numerator"].ToString())} where FPgENTRYID='{cx["FPgENTRYID"]}'";
                    //                        DBUtils.Execute(Context, upsql);
                    //                    }
                    //                }
                    //            }
                    //        }
                    //    }
                    //}
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
                                jsonRoot.Add("fbilltype", "用料清单");
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
                                jsonRoot.Add("fbilltype", "用料清单");
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
                    foreach (ExtendedDataEntity extended in e.SelectedRows)
                    {
                        DynamicObject dy = extended.DataEntity;
                        DynamicObjectCollection docPriceEntity = dy["PPBomEntry"] as DynamicObjectCollection;
                        DynamicObjectCollection wldu = ((DynamicObject)dy["MaterialID"])["MaterialProduce"] as DynamicObjectCollection;
                        foreach (var entry in docPriceEntity)
                        {
                            if (wldu.Count > 0)
                            {
                                if (dy["MOBillNO"].ToString().Substring(0, 2) == "YJ" && dy["MaterialID"] != null)
                                {
                                    entry["StockID_Id"] = wldu[0]["PickStockId_Id"];
                                }
                                if(wldu[0]["PickStockId"] !=null &&((DynamicObject)wldu[0]["PickStockId"])["Number"].ToString() == "260CK090")
                                {
                                    entry["StockID_Id"] = wldu[0]["PickStockId_Id"];
                                }
                                if(dy["WorkshopID"] != null && ((DynamicObject)dy["WorkshopID"])["Number"].ToString() == "000362")
                                {
                                    entry["StockID_Id"] = wldu[0]["PickStockId_Id"];
                                }
                            }
                            if (dy["FBOMID"]!=null)
                            {
                                foreach (var wlqd in ((DynamicObject)dy["FBOMID"])["TreeEntity"] as DynamicObjectCollection)
                                {
                                    if (wlqd["MATERIALIDCHILD"] != null && entry["MaterialID"] != null &&
                                        Convert.ToInt64(wlqd["MATERIALIDCHILD_Id"]) == Convert.ToInt64(entry["MaterialID_Id"]))
                                    {
                                        entry["F_260_DXGYS"] = wlqd["F_260_DXGYSWB"];
                                    }
                                }
                            }
                        }
                        if (dy["WorkshopID"] != null && ((DynamicObject)dy["WorkshopID"])["Number"].ToString() == "001567")
                        {
                            foreach (var entry in dy["PPBomEntry"] as DynamicObjectCollection)
                            {
                                if (entry["IssueType"] != null && (entry["IssueType"].ToString()=="2" || entry["IssueType"].ToString() == "4"))
                                {
                                    entry["StockID_Id"] = 37513088;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
