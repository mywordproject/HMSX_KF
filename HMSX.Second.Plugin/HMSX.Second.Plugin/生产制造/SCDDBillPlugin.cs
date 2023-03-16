using HMSX.Second.Plugin.供应链;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Bill.PlugIn;
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

namespace HMSX.Second.Plugin.生产制造
{
    [Description("生产订单--提交时变更生产车间")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class SCDDServerPlugin : AbstractOperationServicePlugIn
    {
        readonly string[] reloadKeys = new string[] { "FStockId", "F_260_XMHH", "FMtoNo" };
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FPrdOrgId", "FMaterialId", "F_260_SFNPI", "F_260_SB", "FBillNo", "FStockId" ,
                "FSaleOrderNo", "FSaleOrderEntrySeq", "FReqSrc" ,"FMTONO","F_260_XMHH","F_260_WLDWLX","FApproveDate",
                "F_260_WLDWS","FBillType","FBillNo","FProductType","FGroup","FParentRowId","FRowId","FCheckProduct"};
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
                if (FormOperation.Operation.Equals("Submit", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var dates in e.DataEntitys)
                    {
                        var entrys = dates["TreeEntity"] as DynamicObjectCollection;
                        foreach (var entry in entrys)
                        {
                            if (((DynamicObject)entry["MaterialId"])["Number"].ToString().Substring(0, 6) == "260.02" && entry["F_260_SFNPI"].ToString() == "NPI_OLD")
                            {
                                string gxsql = $@"update T_PRD_MOENTRY set FSTOCKID=1370037 where FENTRYID='{entry["Id"].ToString()}'";
                                DBUtils.Execute(Context, gxsql);
                            }
                        }

                    }
                }
                else if (FormOperation.Operation.Equals("Save", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var dates in e.DataEntitys)
                    {
                        if (((DynamicObject)dates["BillType"])["Id"].ToString() == "0e74146732c24bec90178b6fe16a2d1c")
                        {
                            foreach (var entry in dates["TreeEntity"] as DynamicObjectCollection)
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
                                    if (entry["F_260_WLDWLX"].ToString() == "BD_Customer")
                                    {
                                        string khsql = $@"select FSHORTNAME from T_BD_CUSTOMER_L where FCUSTID={entry["F_260_WLDWS_Id"]}";
                                        var khs = DBUtils.ExecuteDynamicObject(Context, khsql);
                                        if (khs.Count > 0)
                                        {
                                            str = khs[0]["FSHORTNAME"].ToString();
                                        }
                                    }
                                    else if (entry["F_260_WLDWLX"].ToString() == "BD_Supplier")
                                    {
                                        string khsql = $@"select FSHORTNAME from t_BD_Supplier_L where FSUPPLIERID={entry["F_260_WLDWS_Id"]}";
                                        var khs = DBUtils.ExecuteDynamicObject(Context, khsql);
                                        if (khs.Count > 0)
                                        {
                                            str = khs[0]["FSHORTNAME"].ToString();
                                        }
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
                                    string upsql = $@"/*dialect*/ update T_PRD_MOENTRY set FMTONO='{str}' where FENTRYID={entry["Id"]}";
                                    DBUtils.Execute(Context, upsql);
                                }
                            }
                        }
                        foreach (var entry in dates["TreeEntity"] as DynamicObjectCollection)
                        {
                            if (entry["MaterialId"]!=null &&((DynamicObject)entry["MaterialId"])["Number"].ToString().EndsWith(".000")&&
                                entry["WorkShopID"]!=null && ((DynamicObject)entry["WorkShopID"])["Number"].ToString()== "000362" &&
                                entry["StockId"]!=null && ((DynamicObject)entry["StockId"])["Name"].ToString().Contains("模具"))
                            {
                                if (Convert.ToInt64(dates["F_260_BaseMJYT_Id"]) == 0)
                                {
                                    throw new KDBusinessException("", "母订单模具用途必填");
                                }
                                if (dates["BillNo"].ToString().Contains("-"))
                                {
                                    throw new KDBusinessException("", "母订单单据编号不允许有横杠（-）");
                                }
                            }
                            if(entry["WorkShopID"] != null && ((DynamicObject)entry["WorkShopID"])["Number"].ToString() == "000362" &&
                                dates["BillNo"].ToString().Contains("-"))
                            {
                                if (Convert.ToInt64(dates["F_260_BaseMJYT_Id"])!=0)
                                {
                                    throw new KDBusinessException("", "子订单模具用途为空");
                                }
                            }
                        }
                    }
                }
                else if (FormOperation.Operation.Equals("ToStart", StringComparison.OrdinalIgnoreCase) ||
                    FormOperation.Operation.Equals("UndoToStart", StringComparison.OrdinalIgnoreCase))
                {//执行开工、反执行开工
                    foreach (var date in e.DataEntitys)
                    {
                        foreach (var entry in date["TreeEntity"] as DynamicObjectCollection)
                        {
                            string upsql = $@"/*dialect*/update T_PRD_PPBOM set F_260_XTLY='' where FMOBILLNO='{date["BillNo"]}' and FMOENTRYSEQ='{entry["Seq"]}'";
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
                            jsonRoot.Add("fbilltype", "生产订单");
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
                else if (FormOperation.Operation.Equals("Audit", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var dates in e.DataEntitys)
                    {
                        string upsql = $@"update T_PRD_MOENTRY_A set FCONVEYDATE='{dates["ApproveDate"]}' where FENTRYID in 
                         (select FENTRYID from T_PRD_MOENTRY where FPRODUCTTYPE<>1 and FID='{dates["Id"]}')";
                        DBUtils.Execute(Context, upsql);
                    }
                }
            }
        }
        public override void BeforeExecuteOperationTransaction(BeforeExecuteOperationTransaction e)
        {
            base.BeforeExecuteOperationTransaction(e);
            if (Context.CurrentOrganizationInfo.ID == 100026)
            {
                if (FormOperation.Operation.Equals("Save", StringComparison.OrdinalIgnoreCase))
                {
                    long i = 0;
                    foreach (ExtendedDataEntity extended in e.SelectedRows)
                    {
                        DynamicObject dates = extended.DataEntity;
                        var entrys = dates["TreeEntity"] as DynamicObjectCollection;
                        foreach (var entry in entrys)
                        {
                            if (entry["MaterialId"]!=null &&
                                ((DynamicObject)entry["MaterialId"])["Number"].ToString().Substring(0,6)=="260.02"&&
                                dates["F_260_SCDDBHQF"].ToString() == "XNYYJ")
                            {
                                entry["StockId_Id"] = 31730768;//正式ID31730768
                            }
                            if (entry["ProductType"] != null && entry["ProductType"].ToString() == "1")
                            {
                                entry["Group"] = entry["Seq"];
                            }
                            else if (entry["ProductType"] != null && entry["ProductType"].ToString() != "1")
                            {
                                foreach (var entry1 in entrys)
                                {
                                    if ( entry["ParentRowId"].ToString() == entry1["RowId"].ToString())
                                    {
                                        entry["Group"] = entry1["Seq"];
                                        break;
                                    }
                                }
                                entry["CheckProduct"] = 1;
                            }
                            if (entry["MaterialId"]!=null && entry["ReqSrc"].ToString() == "1" && ((DynamicObject)entry["MaterialId"])["Number"].ToString().Substring(0,7)=="260.02.")
                            {
                                string xsddsql = $@"select F_260_SFNPI from T_SAL_ORDER where FBILLNO='{entry["SaleOrderNo"]}'";
                                var sxdd = DBUtils.ExecuteDynamicObject(Context, xsddsql);
                                if (sxdd.Count > 0)
                                {
                                    entry["F_260_SFNPI"] = sxdd[0]["F_260_SFNPI"];
                                }
                            }
                            else if (entry["ReqSrc"].ToString() == "2")
                            {
                                entry["F_260_SFNPI"] = "批量";
                            }
                            if (((DynamicObject)dates["BillType"])["Id"].ToString() == "0e74146732c24bec90178b6fe16a2d1c")
                            {
                                if (((DynamicObject)entry["MaterialID"])["Number"].ToString().Substring(0, 6) == "260.02" &&
                                (entry["F_260_XMHH"] as DynamicObjectCollection).Count == 0)
                                {
                                    string cxsql = $@"select 
                                        XMH.F_260_XMH,XMH.FPKID
                                        from T_BD_MATERIAL a
                                        left join t_BD_MaterialPlan c on c.FMATERIALID=a.FMATERIALID
                                        left join T_PLN_MANUFACTUREPOLICY d on c.FMFGPOLICYID=d.FID
                                        LEFT JOIN PAEZ_t_Cust_Entry100355 XMH ON XMH.FMATERIALID=A.FMATERIALID
                                        WHERE 
                                       -- D.FNUMBER='ZZCL003_SYS'
                                        --and 
                                       a.FMATERIALID={entry["MaterialID_Id"]}
                                        and FCREATEORGID=100026
                                        and XMH.F_260_XMH is not null
                                        order by XMH.FPKID DESC";
                                    var cxs = DBUtils.ExecuteDynamicObject(Context, cxsql);
                                    if (cxs.Count > 0)
                                    {
                                        i++;
                                        //FMULTITACCTBOOKID 是多选账簿，首先获取多选账簿的属性类型
                                        var dyc = new DynamicObject((entry["F_260_XMHH"] as DynamicObjectCollection).DynamicCollectionItemPropertyType);
                                        long id = 0;
                                        string xmhsql = $@"select MIN(FPKID)FPKID FROM PAEZ_t_Cust_Entry100365";
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
                                        (entry["F_260_XMHH"] as DynamicObjectCollection).Add(dyc);
                                    }
                                }
                            }
                        }
                        //foreach (var date in dates["TreeEntity"] as DynamicObjectCollection)
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
                else if (FormOperation.Operation.Equals("ToClose", StringComparison.OrdinalIgnoreCase) ||
                    FormOperation.Operation.Equals("ForceClose", StringComparison.OrdinalIgnoreCase))
                {
                    //结案校验
                    foreach (ExtendedDataEntity extended in e.SelectedRows)
                    {
                        DynamicObject dy = extended.DataEntity;
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
                            jsonRoot.Add("fbilltype", "生产订单");
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
