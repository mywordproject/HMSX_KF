using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DependencyRules;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.生产制造
{
    [Description("检验单--反写检验结果、决策到汇报单")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class JYDServerPlugin : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FMemo1", "FUsePolicy", "F_260_HBFLNM", "F_260_BHGMS",
                "F_260_HBBH", "F_260_HBHH", "FBillNo", "FUsePolicy","FSrcBillNo","FSrcBillType" };
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
                    foreach (var dates in e.DataEntitys)
                    {
                        var fentrys = dates["Entity"] as DynamicObjectCollection;
                        foreach (var fentry in fentrys)
                        {
                            string jyjg = "";
                            string bhgms = "";
                            var zfentrys = fentry["PolicyDetail"] as DynamicObjectCollection;
                            bhgms = fentry["F_260_BHGMS"].ToString();
                            foreach (var zfentry in zfentrys)
                            {
                                if (zfentry["Memo1"].ToString() != "")
                                {
                                    jyjg += zfentry["Memo1"].ToString() + ";";
                                }

                            }
                            string upsql = $@"update T_SFC_OPTRPTENTRY set F_260_JYJGMS='{jyjg.Trim(';')}',F_260_BHGMS='{bhgms}' where FENTRYID='{fentry["F_260_HBFLNM"].ToString()}'";
                            DBUtils.Execute(Context, upsql);
                            foreach (var ReferDetail in fentry["ReferDetail"] as DynamicObjectCollection)
                            {
                                if (ReferDetail["SrcBillType"].ToString() == "PUR_ReceiveBill")
                                {
                                    string upsql2 = $@"/*dialect*/update T_PUR_Receive set F_260_XTLY='' where FBILLNO='{ReferDetail["SrcBillNo"]}'";
                                    DBUtils.Execute(Context, upsql2);
                                }
                            }
                        }

                    }
                }
                else if (FormOperation.Operation.Equals("Save", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var dates in e.DataEntitys)
                    {
                        //检验区分
                        string CXSQL = $@"select D.FNUMBER,C.FSRCBILLTYPE,C.FORDERBILLNO,FINSPECTORGID from T_QM_INSPECTBILL a
                                       inner join T_QM_INSPECTBILLENTRY_A b on a.fid=b.fid
                                       INNER join T_QM_IBREFERDETAIL c on c.FENTRYID=B.FENTRYID
                                       INNER JOIN T_BD_MATERIAL D ON D.FMATERIALID=B.FMATERIALID
                                       WHERE A.FBILLNO='{dates["BillNo"]}' AND 
                                       C.FSRCBILLTYPE='SFC_OperationReport' AND
                                       SUBSTRING(D.FNUMBER,1,6)='260.02' AND
                                        (C.FORDERBILLNO LIKE '%MO%' OR C.FORDERBILLNO LIKE '%XNY%' OR C.FORDERBILLNO LIKE '%YJ%')
                                        AND  A.FINSPECTORGID=100026";
                        var CX = DBUtils.ExecuteDynamicObject(Context, CXSQL);
                        if (CX.Count > 0)
                        {
                            string JYQFSQL = $@" update T_QM_INSPECTBILL set F_260_JYQF=1 where FID={dates["Id"]}";
                            DBUtils.Execute(Context, JYQFSQL);
                        }
                        int tx = 0;
                        foreach (var entity in dates["Entity"] as DynamicObjectCollection)
                        {
                            string gxhbsql = $@"select d.FSHORTNAME from T_SFC_OPTRPTENTRY a
                             inner join T_SFC_OPTRPT b on a.FID=b.FID
                             inner join HMD_t_Cust100150 c on c.FID=a.FHMSXKHBQYD
                             inner join T_BD_CUSTOMER_L d on c.F_HMD_BASEKH=d.FCUSTID 
                             where FBILLNO='{entity["F_260_HBBH"]}' AND FSEQ='{entity["F_260_HBHH"]}'";
                            var gxhb = DBUtils.ExecuteDynamicObject(Context, gxhbsql);
                            if (gxhb.Count > 0)
                            {
                                // this.Model.SetValue("F_260_KHJC", gxhb[0]["FHMSXBZ"], Convert.ToInt32(date["Seq"]) - 1);
                                string jydsql = $@" update T_QM_INSPECTBILLENTRY set F_260_KHJC='{gxhb[0]["FSHORTNAME"]}' where FENTRYID={entity["Id"]}";
                                DBUtils.Execute(Context, jydsql);
                            }
                            //判断是否挑选                           
                            foreach (var FPolicyDetail in entity["PolicyDetail"] as DynamicObjectCollection)
                            {
                                if (FPolicyDetail["UsePolicy"] != null)
                                {
                                    if (FPolicyDetail["UsePolicy"].ToString() == "E")
                                    {
                                        tx++;
                                        string sftxsql = $@" update T_QM_INSPECTBILL set F_260_SFTX=1 where FID={dates["Id"]}";
                                        DBUtils.Execute(Context, sftxsql);
                                        break;
                                    }
                                }
                            }
                            string syjc = "";
                            foreach (var zfentry in entity["PolicyDetail"] as DynamicObjectCollection)
                            {
                                Field field = this.BusinessInfo.GetField("FUsePolicy");//转为ComboField
                                ComboField comboField = field as ComboField;//获取下拉列表字段绑定的枚举类型
                                var enumObj = (EnumObject)comboField.EnumObject;//根据枚举值获取枚举项，然后拿枚举项的枚举名称
                                var enumItemName = enumObj.Items.FirstOrDefault(p => p.Value.Equals(zfentry["UsePolicy"].ToString())).Caption.ToString();
                                syjc += enumItemName + ";"; 
                            }
                            string upsql = $@"update T_SFC_OPTRPTENTRY set F_260_JYSYJC='{syjc.Trim(';')}' where FENTRYID='{entity["F_260_HBFLNM"]}'";
                            DBUtils.Execute(Context, upsql);
                        }
                        if (tx == 0)
                        {
                            string sftxsql = $@" update T_QM_INSPECTBILL set F_260_SFTX=0 where FID={dates["Id"]}";
                            DBUtils.Execute(Context, sftxsql);
                        }
                    }
                }
                if (FormOperation.Operation.Equals("UnAudit", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var dates in e.DataEntitys)
                    {
                        var fentrys = dates["Entity"] as DynamicObjectCollection;
                        foreach (var fentry in fentrys)
                        {
                            foreach (var ReferDetail in fentry["ReferDetail"] as DynamicObjectCollection)
                            {
                                if (ReferDetail["SrcBillType"].ToString() == "PUR_ReceiveBill")
                                {
                                    string upsql2 = $@"/*dialect*/update T_PUR_Receive set F_260_XTLY='' where FBILLNO='{ReferDetail["SrcBillNo"]}'";
                                    DBUtils.Execute(Context, upsql2);
                                }
                            }
                        }
                    }
                }
            }
        }
        
    }
}
