﻿using HMSX.Second.Plugin.Tool;
using Kingdee.BOS;
using Kingdee.BOS.App.Core;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.JSON;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Orm.Metadata.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.供应链
{
    [Description("预测单--项目号")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class YCDServicePlugin : AbstractOperationServicePlugIn
    {
        //readonly string[] reloadKeys = new string[] { "F_260_XMHH", "FMtoNo" };
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);//, "F_260_SFSI1" 
            String[] propertys = { "FMaterialID", "F_260_XMHH", "FCustID", "F_260_SFSI" };
            foreach (String property in propertys)
            {
                e.FieldKeys.Add(property);
            }
        }
        public override void BeforeExecuteOperationTransaction(BeforeExecuteOperationTransaction e)
        {
            base.BeforeExecuteOperationTransaction(e);
            if (FormOperation.Operation.Equals("Submit", StringComparison.OrdinalIgnoreCase))
            {
                if (Context.CurrentOrganizationInfo.ID == 100026)
                {
                    long i = 0;
                    foreach (ExtendedDataEntity extended in e.SelectedRows)
                    {
                        DynamicObject dates = extended.DataEntity;
                        foreach (var date in dates["PLN_FORECASTENTRY"] as DynamicObjectCollection)
                        {
                            if (((DynamicObject)date["MaterialID"])["Number"].ToString().Substring(0, 6) == "260.02" &&
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
                                        a.FMATERIALID={date["MaterialID_Id"]}
                                        and FCREATEORGID=100026
                                        and XMH.F_260_XMH is not null
                                        order by XMH.FPKID DESC";
                                var cxs = DBUtils.ExecuteDynamicObject(Context, cxsql);
                                if (cxs.Count >0)
                                {
                                    i++;
                                    //FMULTITACCTBOOKID 是多选账簿，首先获取多选账簿的属性类型
                                    var dyc = new DynamicObject((date["F_260_XMHH"] as DynamicObjectCollection).DynamicCollectionItemPropertyType);
                                    long id = 0;
                                    string xmhsql = $@"select MIN(FPKID)FPKID FROM PAEZ_t_Cust_Entry100362";
                                    var xmh = DBUtils.ExecuteDynamicObject(Context, xmhsql);
                                    if (xmh.Count > 0)
                                    {
                                        id += Convert.ToInt64(xmh[0]["FPKID"]);
                                    }
                                    if((id - i) == 0)
                                    {
                                        i++;
                                    }
                                    //给基础资料的Id赋值
                                    dyc["PKID"] = id-i;
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
            else if (FormOperation.Operation.Equals("SLSB_SAVE", StringComparison.OrdinalIgnoreCase))
            {
                if (Context.CurrentOrganizationInfo.ID == 100026)
                {
                    long i = 0;
                    foreach (ExtendedDataEntity extended in e.SelectedRows)
                    {
                        DynamicObject dates = extended.DataEntity;
                        foreach (var date in dates["PLN_FORECASTENTRY"] as DynamicObjectCollection)
                        {
                            if (((DynamicObject)date["MaterialID"])["Number"].ToString().Substring(0, 6) == "260.02" &&
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
                                    string xmhsql = $@"select MIN(FPKID)FPKID FROM PAEZ_t_Cust_Entry100362";
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
            if (FormOperation.Operation.Equals("Submit", StringComparison.OrdinalIgnoreCase))
            {
                if (Context.CurrentOrganizationInfo.ID == 100026)
                {
                    foreach (var date in e.DataEntitys)
                    {
                        foreach (var entry in date["PLN_FORECASTENTRY"] as DynamicObjectCollection)
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
                                string khsql = $@"select FSHORTNAME from T_BD_CUSTOMER_L where FCUSTID={entry["CustID_Id"]}";
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
                                        str +="_"+ xmhs[0]["FNAME"].ToString();
                                    }
                                }
                                string khwlsql = $@"/*dialect*/select F_260_SFSI from t_Sal_CustMatMappingEntry a
                                              left join t_Sal_CustMatMapping b on a.fid=b.fid
                                              where FCUSTOMERID='{entry["CustID_Id"]}' 
                                              and FMATERIALID='{entry["MaterialID_Id"]}' and FEFFECTIVE=1 and F_260_SFSI!=''";
                                var khwl = DBUtils.ExecuteDynamicObject(Context, khwlsql);
                                if (khwl.Count > 0 && khwl[0]["F_260_SFSI"].ToString() == "SI")
                                {
                                    str += "_SI";
                                }
                                string upsql = $@"/*dialect*/ update T_PLN_FORECASTENTRY set FMTONO='{str}' where FENTRYID={entry["Id"]}";
                                DBUtils.Execute(Context, upsql);
                            }
                        }
                    }
                }
            }
            else if(FormOperation.Operation.Equals("SLSB_SAVE", StringComparison.OrdinalIgnoreCase))
            {
                if (Context.CurrentOrganizationInfo.ID == 100026)
                {
                    foreach (var date in e.DataEntitys)
                    {
                        foreach (var entry in date["PLN_FORECASTENTRY"] as DynamicObjectCollection)
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
                                string khsql = $@"select FSHORTNAME from T_BD_CUSTOMER_L where FCUSTID={entry["CustID_Id"]}";
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
                                string khwlsql = $@"/*dialect*/select F_260_SFSI from t_Sal_CustMatMappingEntry a
                                              left join t_Sal_CustMatMapping b on a.fid=b.fid
                                              where FCUSTOMERID='{entry["CustID_Id"]}' 
                                              and FMATERIALID='{entry["MaterialID_Id"]}' and FEFFECTIVE=1 and F_260_SFSI!=''";
                                var khwl = DBUtils.ExecuteDynamicObject(Context, khwlsql);
                                if (khwl.Count>0 && khwl[0]["F_260_SFSI"].ToString()=="SI")
                                {
                                    str += "_SI";
                                }
                                string upsql = $@"/*dialect*/ update T_PLN_FORECASTENTRY set FMTONO='{str}' where FENTRYID={entry["Id"]}";
                                DBUtils.Execute(Context, upsql);
                            }
                        }
                    }
                }
            }
        }
        public override void OnPrepareOperationServiceOption(OnPrepareOperationServiceEventArgs e)
        {
            base.OnPrepareOperationServiceOption(e);
            //if (Context.CurrentOrganizationInfo.ID == 100026)
            //{
            //    if (this.FormOperation.OperationId == 8 || this.FormOperation.OperationId == 9)
            //    {
            //        if (!string.IsNullOrWhiteSpace(this.FormOperation.LoadKeys) && this.FormOperation.LoadKeys != "null")
            //        {
            //            // 设置操作完后刷新字段
            //            var loadKeys = KDObjectConverter.DeserializeObject<List<string>>(this.FormOperation.LoadKeys);
            //            if (loadKeys == null)
            //            {
            //                loadKeys = new List<string>();
            //            }
            //            foreach (var reloadKey in reloadKeys)
            //            {
            //                if (!loadKeys.Contains(reloadKey))
            //                {
            //                    loadKeys.Add(reloadKey);
            //                }
            //            }
            //            this.FormOperation.LoadKeys = KDObjectConverter.SerializeObject(loadKeys);
            //        }
            //    }
            //}
        }
    }
}
