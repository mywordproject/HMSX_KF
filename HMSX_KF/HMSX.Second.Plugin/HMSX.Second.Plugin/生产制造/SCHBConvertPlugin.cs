using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.生产制造
{
    [Description("工序汇报汇总")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class SCHBConvertPlugin: AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FMaterialId", "FHMSXKHBQYD", "FSourceBillNo" , "FMtoNo", "F_260_JHGZHBM" };
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
                if (FormOperation.Operation.Equals("Save", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var date in e.DataEntitys)
                    {
                        //long  fid = 0;
                        //foreach(var lk in date["FBillHead_Link"]as DynamicObjectCollection)
                        //{
                        //    if (Convert.ToInt64(lk["SId"]) != 0)
                        //    {
                        //        fid = Convert.ToInt64(lk["SId"]);
                        //        break;
                        //    }
                            
                        //}
                        //foreach (var entry in date["OptRptEntry"] as DynamicObjectCollection)
                        //{
                        //    string cxsql = $@"select F_260_JHGZHBM,FMTONO from T_SFC_OPTRPT a
                        //                  inner join T_SFC_OPTRPTENTRY b on a.FID=b.FID
                        //                  inner join T_SFC_OPTRPTENTRY_B c on b.FENTRYID=c.FENTRYID
                        //                  WHERE 
                        //                  FMATERIALID='{entry["MaterialId_Id"]}'
                        //                  AND a.FID='{fid}'
                        //                  and FSOURCEBILLNO='{entry["FSourceBillNo"]}'
                        //                  AND FHMSXKHBQYD='{entry["FHMSXKHBQYD_Id"]}'";
                        //    var cx = DBUtils.ExecuteDynamicObject(Context, cxsql);
                        //    if (cx.Count > 0)
                        //    {
                        //        string upsql = $@"/*dialect*/ update PAEZ_t_Cust_Entry100320 set F_260_JHGZHBM='{cx[0]["F_260_JHGZHBM"]}' where FENTRYID={entry["Id"]}";
                        //        DBUtils.Execute(Context, upsql);
                        //        string upsql1 = $@"/*dialect*/ update PAEZ_t_Cust_Entry100320_B set FMTONO='{cx[0]["FMTONO"]}' where FENTRYID={entry["Id"]}";
                        //        DBUtils.Execute(Context, upsql1);
                        //    }
                        //}
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
                                    string cxsql = $@"select 
                                        XMH.F_260_XMH,XMH.FPKID,XMHL.FNAME,XMHH.FNUMBER
                                        from T_BD_MATERIAL a
                                        left join t_BD_MaterialPlan c on c.FMATERIALID=a.FMATERIALID
                                        left join T_PLN_MANUFACTUREPOLICY d on c.FMFGPOLICYID=d.FID
                                        LEFT JOIN PAEZ_t_Cust_Entry100355 XMH ON XMH.FMATERIALID=A.FMATERIALID
                                        LEFT JOIN ora_t_Cust100045_L XMHL ON XMH.F_260_XMH=XMHL.FID
                                        LEFT JOIN ora_t_Cust100045 XMHH ON XMHL.FID=XMHH.FID
                                        WHERE 
                                        --D.FNUMBER='ZZCL003_SYS'
                                        --and 
                                        a.FMATERIALID={entry["MaterialID_Id"]}
                                        and a.FCREATEORGID=100026
                                        and XMH.F_260_XMH is not null
                                        order by XMH.FPKID DESC";
                                    var cxs = DBUtils.ExecuteDynamicObject(Context, cxsql);
                                    if (cxs.Count > 0)
                                    {
      
                                            str += "_" + cxs[0]["FNAME"].ToString();
                                            str1 += "_" + cxs[0]["FNUMBER"].ToString();
                                    }
                                    string upsql = $@"/*dialect*/ update PAEZ_t_Cust_Entry100320 set F_260_JHGZHBM='{str1}' where FENTRYID={entry["Id"]}";
                                    DBUtils.Execute(Context, upsql);
                                    string upsql1 = $@"/*dialect*/ update PAEZ_t_Cust_Entry100320_B set FMTONO='{str}' where FENTRYID={entry["Id"]}";
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
                                        string upsql1 = $@"/*dialect*/ update PAEZ_t_Cust_Entry100320_B set FMTONO='{khs[0]["FNUMBER"]}'+FMTONO where FENTRYID={entry["Id"]}";
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
                                    string upsql = $@"/*dialect*/ update PAEZ_t_Cust_Entry100320 set F_260_JHGZHBM='{str2}' where FENTRYID={entry["Id"]}";
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
}
