using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.JSON;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.生产制造
{
    [Description("工序汇报--保存时截取批号日期字符")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class GXHBServerPlugin : AbstractOperationServicePlugIn
    {
        readonly string[] reloadKeys = new string[] { "F_260_JHGZHBM", "F_260_XMHH", "FMtoNo" };
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FPrdOrgId", "FLot", "FMoNumber", "FDispatchDetailEntryId", "FFinishQty", "FMaterialId", "FBillNo", "FHMSXKHBQYD", "FHMSXKHBQYD", "F_260_SFNPI1",
             "F_260_XMHH", "FHMSXKH", "FMtoNo"};
            foreach (String property in propertys)
            {
                e.FieldKeys.Add(property);
            }
        }
        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);
            if (FormOperation.Operation.Equals("Save", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var dates in e.DataEntitys)
                {
                    if (dates["PrdOrgId_Id"].ToString() == "100026")
                    {
                        var entrys = dates["OptRptEntry"] as DynamicObjectCollection;
                        foreach (var entry in entrys)
                        {
                            if (entry["MoNumber"].ToString().Contains("MO")|| entry["MoNumber"].ToString().Contains("XNY"))
                            {
                                //单据转换上
                                //string x = ((DynamicObject)entry["MaterialId"])["Number"].ToString().Substring(0, 6);
                                //if (((DynamicObject)entry["MaterialId"])["Number"].ToString().Substring(0, 6) == "260.02")
                                //{
                                //    string cpupsql = $@"/*dialect*/update T_SFC_OPTRPT set F_260_CP=1 where FID='{dates["Id"].ToString()}'";
                                //    DBUtils.Execute(Context, cpupsql);
                                //}
                                //截取批号日期字符
                                string rq = ((DynamicObject)entry["lot"])["Number"].ToString().Substring(0, 8);
                                string upsql = $@"/*dialect*/update T_SFC_OPTRPTENTRY set F_260_PHRQ='{rq}' where FENTRYID='{entry["Id"].ToString()}'";
                                DBUtils.Execute(Context, upsql);


                                string cxsql = $@"select b.FSHORTNAME  from HMD_t_Cust100150 a
                                                inner join T_BD_CUSTOMER_L b on a.F_HMD_BASEKH=b.FCUSTID 
                                                WHERE a.FID={entry["FHMSXKHBQYD_Id"]}";
                                var cxs = DBUtils.ExecuteDynamicObject(Context, cxsql);
                                if (cxs.Count > 0)
                                {
                                    //带出客户简称
                                    string khbqsql = $@"/*dialect*/update T_SFC_OPTRPTENTRY set FHMSXBZ=aa.FSHORTNAME from
                                                (select a.FID,b.FSHORTNAME  from HMD_t_Cust100150 a
                                                inner join T_BD_CUSTOMER_L b on a.F_HMD_BASEKH=b.FCUSTID 
                                                WHERE a.FID={entry["FHMSXKHBQYD_Id"]}) aa where aa.fid=T_SFC_OPTRPTENTRY.FHMSXKHBQYD
                                                and FENTRYID={entry["Id"]}";
                                    DBUtils.Execute(Context, khbqsql);
                                    //带出客户
                                    string khsql = $@"/*dialect*/ update T_SFC_OPTRPTENTRY set FHMSXKH=bb.F_HMD_BASEKH from
                                                (select FID,F_HMD_BASEKH  from HMD_t_Cust100150                                        
                                                WHERE FID={entry["FHMSXKHBQYD_Id"]}) bb where bb.fid=T_SFC_OPTRPTENTRY.FHMSXKHBQYD
                                                and FENTRYID={entry["Id"]}";
                                    DBUtils.Execute(Context, khsql);

                                    long FStockId = 0;
                                    if (cxs[0]["FSHORTNAME"].ToString() == "CDFX" && entry["F_260_SFNPI1"].ToString() != "NPI_OLD")
                                    {
                                        FStockId = 11784504;
                                    }
                                    else if (cxs[0]["FSHORTNAME"].ToString() == "LHFX" && entry["F_260_SFNPI1"].ToString() != "NPI_OLD")
                                    {
                                        FStockId = 11784506;
                                    }
                                    else if (cxs[0]["FSHORTNAME"].ToString() == "吉宝" && entry["F_260_SFNPI1"].ToString() != "NPI_OLD")
                                    {
                                        FStockId = 11784508;
                                    }
                                    else if (cxs[0]["FSHORTNAME"].ToString() == "达功" && entry["F_260_SFNPI1"].ToString() != "NPI_OLD")
                                    {
                                        FStockId = 11784509;
                                    }
                                    else if (cxs[0]["FSHORTNAME"].ToString() == "BYD" && entry["F_260_SFNPI1"].ToString() != "NPI_OLD")
                                    {
                                        FStockId = 11784511;
                                    }
                                    else if (cxs[0]["FSHORTNAME"].ToString() == "翊宝" && entry["F_260_SFNPI1"].ToString() != "NPI_OLD")
                                    {
                                        FStockId = 11784513;
                                    }
                                    else if (cxs[0]["FSHORTNAME"].ToString() == "歌尔" && entry["F_260_SFNPI1"].ToString() != "NPI_OLD")
                                    {
                                        FStockId = 25856631;
                                    }
                                    else if (cxs[0]["FSHORTNAME"].ToString() == "鸿富成" && entry["F_260_SFNPI1"].ToString() != "NPI_OLD")
                                    {
                                        FStockId = 31116287;
                                    }
                                    else if (cxs[0]["FSHORTNAME"].ToString() == "VNFX" && entry["F_260_SFNPI1"].ToString() != "NPI_OLD")
                                    {
                                        FStockId = 32379391;
                                    }
                                    if (FStockId != 0)
                                    {
                                        string cksql = $@"/*dialect*/update T_SFC_OPTRPTENTRY set FSTOCKID={FStockId} where FENTRYID={entry["Id"]}";
                                        DBUtils.Execute(Context, cksql);
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
            if (FormOperation.Operation.Equals("Audit", StringComparison.OrdinalIgnoreCase))
            {
                if (Context.CurrentOrganizationInfo.ID == 100026)
                {
                    long i = 0;
                    foreach (ExtendedDataEntity extended in e.SelectedRows)
                    {
                        DynamicObject dy = extended.DataEntity;
                        DynamicObjectCollection docPriceEntity = dy["OptRptEntry"] as DynamicObjectCollection;
                        foreach (var entry in docPriceEntity)
                        {                         
                            if (entry["MoNumber"].ToString().Contains("MO") || entry["MoNumber"].ToString().Contains("XNY"))
                            {
                                string cxsql = $@"/*dialect*/select FMATERIALID,FFINISHQTY,FDISPATCHDETAILENTRYID from T_SFC_OPTRPTENTRY a
                                            inner join T_SFC_OPTRPT b on a.FID=b.FID
                                            INNER JOIN T_SFC_OPTRPTENTRY_A C ON C.FENTRYID=A.FENTRYID
                                            INNER JOIN T_SFC_OPTRPTENTRY_B D ON D.FENTRYID=A.FENTRYID
                                            WHERE  FMATERIALID='{entry["MaterialId_Id"]}'
                                            AND FFINISHQTY={Convert.ToDouble(entry["FinishQty"])}
                                            AND FDISPATCHDETAILENTRYID='{entry["DispatchDetailEntryId"]}'
                                            and FLOT_TEXT='{entry["Lot_Text"]}'
                                            and FHMSXKHBQYD='{entry["FHMSXKHBQYD_Id"]}' ";
                                var cx = DBUtils.ExecuteDynamicObject(Context, cxsql);
                                if (dy["BillNo"] == null)
                                {
                                    if (cx.Count > 0)
                                    {
                                        throw new KDBusinessException("", "重复汇报，不允许保存！");
                                    }
                                }
                                else
                                {
                                    if (cx.Count > 1)
                                    {
                                        throw new KDBusinessException("", "重复汇报，不允许保存！");
                                    }
                                }
                            }
                            if (((DynamicObject)entry["MaterialID"])["Number"].ToString().Substring(0, 6) == "260.02" &&
                                    (entry["F_260_XMHH"] as DynamicObjectCollection).Count == 0 &&
                                    (entry["FMtoNo"]==null || entry["FMtoNo"].ToString() == "" || entry["FMtoNo"].ToString() == " "))
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
                                    string xmhsql = $@"select MIN(FPKID)FPKID FROM PAEZ_t_Cust_Entry100368";
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
                }
            }
        }
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            if (FormOperation.Operation.Equals("Audit", StringComparison.OrdinalIgnoreCase))
            {
                if (Context.CurrentOrganizationInfo.ID == 100026)
                {
                    foreach (var date in e.DataEntitys)
                    {
                        foreach (var entry in date["OptRptEntry"] as DynamicObjectCollection)
                        {
                            if (entry["FMtoNo"] == null || entry["FMtoNo"].ToString() == "" || entry["FMtoNo"].ToString() == " ")
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
                                    string khsql = $@"select a.FID,b.FSHORTNAME,c.FNUMBER  from HMD_t_Cust100150 a
                                                    inner join T_BD_CUSTOMER_L b on a.F_HMD_BASEKH=b.FCUSTID 
                                                    left join T_BD_CUSTOMER c on c.FCUSTID =b.FCUSTID  
                                                    where a.FID={entry["FHMSXKHBQYD_Id"]}";
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
                                    string upsql = $@"/*dialect*/ update T_SFC_OPTRPTENTRY set F_260_JHGZHBM='{str1}' where FENTRYID={entry["Id"]}";
                                    DBUtils.Execute(Context, upsql);
                                    string upsql1 = $@"/*dialect*/ update T_SFC_OPTRPTENTRY_B set FMTONO='{str}' where FENTRYID={entry["Id"]}";
                                    DBUtils.Execute(Context, upsql1);
                                }
                            }
                            else
                            {
                                if (entry["FMtoNo"].ToString().Substring(0, 1) == "_")
                                {
                                    string khsql2 = $@"select a.FID,b.FSHORTNAME,c.FNUMBER  from HMD_t_Cust100150 a
                                                    inner join T_BD_CUSTOMER_L b on a.F_HMD_BASEKH=b.FCUSTID 
                                                    left join T_BD_CUSTOMER c on c.FCUSTID =b.FCUSTID  
                                                    where a.FID={entry["FHMSXKHBQYD_Id"]}";
                                    var khs = DBUtils.ExecuteDynamicObject(Context, khsql2);
                                    if (khs.Count > 0)
                                    {
                                        string upsql1 = $@"/*dialect*/ update T_SFC_OPTRPTENTRY_B set FMTONO='{khs[0]["FNUMBER"]}'+FMTONO where FENTRYID={entry["Id"]}";
                                        DBUtils.Execute(Context, upsql1);
                                    }
                                }
                                try
                                {
                                    string str2 = "";
                                    string khsql2 = $@"select a.FID,b.FSHORTNAME,c.FNUMBER  from HMD_t_Cust100150 a
                                                    inner join T_BD_CUSTOMER_L b on a.F_HMD_BASEKH=b.FCUSTID 
                                                    left join T_BD_CUSTOMER c on c.FCUSTID =b.FCUSTID  
                                                    where a.FID={entry["FHMSXKHBQYD_Id"]}";
                                    var khs = DBUtils.ExecuteDynamicObject(Context, khsql2);
                                    if (khs.Count > 0)
                                    {
                                        str2 = khs[0]["FNUMBER"].ToString();
                                    }
                                    string name = entry["FMtoNo"].ToString().Substring(entry["FMtoNo"].ToString().IndexOf('_') + 1, entry["FMtoNo"].ToString().Length - (entry["FMtoNo"].ToString().IndexOf('_') + 1));
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
                                    string upsql = $@"/*dialect*/ update T_SFC_OPTRPTENTRY set F_260_JHGZHBM='{str2}' where FENTRYID={entry["Id"]}";
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
        public override void OnPrepareOperationServiceOption(OnPrepareOperationServiceEventArgs e)
        {
            base.OnPrepareOperationServiceOption(e);
            if (Context.CurrentOrganizationInfo.ID == 100026)
            {
                if (this.FormOperation.OperationId == 9)
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
