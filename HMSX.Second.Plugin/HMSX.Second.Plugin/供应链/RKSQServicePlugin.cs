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

namespace HMSX.Second.Plugin.供应链
{
    [Description("入库申请--项目号")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class RKSQServicePlugin : AbstractOperationServicePlugIn
    {
        readonly string[] reloadKeys = new string[] { "F_260_JHGZHBM", "F_260_XMHH", "FMtoNo" };
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FMaterialID", "F_260_XMHH", "FHMSXKH", "FMtoNo" };
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
                        foreach (var date in dates["BillEntry"] as DynamicObjectCollection)
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
                                    string xmhsql = $@"select MIN(FPKID)FPKID FROM PAEZ_t_Cust_Entry100361";
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
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            if (FormOperation.Operation.Equals("Save", StringComparison.OrdinalIgnoreCase))
            {
                if (Context.CurrentOrganizationInfo.ID == 100026)
                {
                    foreach (var date in e.DataEntitys)
                    {
                        foreach (var entry in date["BillEntry"] as DynamicObjectCollection)
                        {
                            if (((DynamicObject)entry["MaterialID"])["Number"].ToString().Substring(0, 6) == "260.02")
                            {
                                if (entry["MtoNo"]==null || entry["MtoNo"].ToString() == "" || entry["MtoNo"].ToString() == " ")
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
                                        string khwlsql = $@"/*dialect*/select F_260_SFSI from t_Sal_CustMatMappingEntry a
                                              left join t_Sal_CustMatMapping b on a.fid=b.fid
                                              where FCUSTOMERID='{entry["FHMSXKH_Id"]}' 
                                              and FMATERIALID='{entry["MaterialID_Id"]}' and FEFFECTIVE=1";
                                        var khwl = DBUtils.ExecuteDynamicObject(Context, khwlsql);
                                        if (khwl.Count > 0 && khwl[0]["F_260_SFSI"].ToString() == "SI")
                                        {
                                            str += "_SI";
                                            str1 += "_SI";
                                        }
                                        string upsql = $@"/*dialect*/ update HMD_t_Cust_Entry100103 set FMTONO='{str}',F_260_JHGZHBM='{str1}' where FENTRYID={entry["Id"]}";
                                        DBUtils.Execute(Context, upsql);
                                    }
                                }
                                else
                                {
                                    try
                                    {
                           
                                        string str2 = "";
                                        string khsql2 = $@"select FNUMBER,FSHORTNAME from T_BD_CUSTOMER a
                                                     inner join T_BD_CUSTOMER_L b ON a.FCUSTID=b.FCUSTID where a.FCUSTID={entry["FHMSXKH_Id"]}";
                                        var khs2 = DBUtils.ExecuteDynamicObject(Context, khsql2);
                                        if (khs2.Count > 0)
                                        {
          
                                            str2 = khs2[0]["FNUMBER"].ToString();
                                        }
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
                                            str2+= "_" + xmhname[0]["FNUMBER"];
                                           
                                        }
                                        string khwlsql = $@"/*dialect*/select F_260_SFSI from t_Sal_CustMatMappingEntry a
                                              left join t_Sal_CustMatMapping b on a.fid=b.fid
                                              where FCUSTOMERID='{entry["FHMSXKH_Id"]}' 
                                              and FMATERIALID='{entry["MaterialID_Id"]}' and FEFFECTIVE=1";
                                        var khwl = DBUtils.ExecuteDynamicObject(Context, khwlsql);
                                        if (khwl.Count > 0 && khwl[0]["F_260_SFSI"].ToString() == "SI")
                                        {
                                            str2 += "_SI";
                                        }
                                        string upsql = $@"/*dialect*/ update HMD_t_Cust_Entry100103 set F_260_JHGZHBM='{str2}' where FENTRYID={entry["Id"]}";
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
