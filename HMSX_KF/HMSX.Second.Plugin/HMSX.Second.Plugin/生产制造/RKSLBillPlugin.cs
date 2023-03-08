using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List.PlugIn;
using Kingdee.BOS.JSON;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin
{
    [Description("入库单---入库数量反写到检验单")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class RKSLBillPlugin : AbstractOperationServicePlugIn
    {
        readonly string[] reloadKeys = new string[] { "FMtoNo" };
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "FMaterialId", "FLot", "FSrcBillNo", "FRealQty", "FSrcEntrySeq", "F_RUJP_PgBARCODE", "FHMSXBZ", "FMtoNo", "FSrcBillNo" };
            foreach (String property in propertys)
            {
                e.FieldKeys.Add(property);
            }
        }
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            if (FormOperation.Operation.Equals("Audit", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var date in e.DataEntitys)
                {
                    if (Context.CurrentOrganizationInfo.ID == 100026)
                    {
                        var entrys = date["Entity"] as DynamicObjectCollection;
                        foreach (var entry in entrys)
                        {
                            string upsql = $@"update T_QM_INSPECTBILLENTRY
                        set  F_260_RKSL=F_260_RKSL+{Convert.ToDouble(entry["RealQty"].ToString())}
                        where FENTRYID =(select a.FENTRYID from T_QM_INSPECTBILLENTRY_A a
                        inner join T_QM_INSPECTBILLENTRY b on a.fentryid=b.fentryid 
                        inner join T_BD_LOTMASTER c on b.FLOT=c.FLOTID
                        where FSRCBILLNO='{entry["SrcBillNo"]}'
                        and a.FMATERIALID='{ entry["MaterialId_Id"]}' and c.FNUMBER='{entry["Lot_text"]}' and FSRCENTRYSEQ={entry["SrcEntrySeq"]}
                        and F_260_PGMXTM='{entry["F_RUJP_PgBARCODE"]}' 
                        )";
                            DBUtils.Execute(Context, upsql);

                            string cxsql = $@"select a.FENTRYID from T_QM_INSPECTBILLENTRY_A a
                             inner join T_QM_INSPECTBILLENTRY b on a.fentryid=b.fentryid 
                             inner join T_BD_LOTMASTER c on b.FLOT=c.FLOTID
                             inner join (
                             select a.FENTRYID,a1.FBILLNO,FSEQ,a.FMATERIALID,c.FNUMBER from T_QM_INSPECTBILLENTRY_A a
                            inner join T_QM_INSPECTBILL a1 on a1.fid=a.fid
                            inner join T_QM_INSPECTBILLENTRY b on a.fentryid=b.fentryid 
                            inner join T_BD_LOTMASTER c on b.FLOT=c.FLOTID
                           where FSRCBILLNO='{entry["SrcBillNo"]}'
                           and a.FMATERIALID='{ entry["MaterialId_Id"]}' and c.FNUMBER='{entry["Lot_text"]}' and FSRCENTRYSEQ={entry["SrcEntrySeq"]}
                           )aa on aa.FBILLNO=a.FSRCBILLNO and aa.FSEQ=a.FSRCENTRYSEQ and aa.FMATERIALID=a.FMATERIALID and c.FNUMBER=aa.FNUMBER";
                            var cx = DBUtils.ExecuteDynamicObject(Context, cxsql);
                            if (cx.Count > 0)
                            {
                                string up1sql = $@"update T_QM_INSPECTBILLENTRY
                              set  F_260_RKSL=F_260_RKSL+{Convert.ToDouble(entry["RealQty"].ToString())}
                              where FENTRYID =(select a.FENTRYID from T_QM_INSPECTBILLENTRY_A a
                               inner join T_QM_INSPECTBILLENTRY b on a.fentryid=b.fentryid 
                               inner join T_BD_LOTMASTER c on b.FLOT=c.FLOTID
                               inner join (
                               select a.FENTRYID,a1.FBILLNO,FSEQ,a.FMATERIALID,c.FNUMBER from T_QM_INSPECTBILLENTRY_A a
                                inner join T_QM_INSPECTBILL a1 on a1.fid=a.fid
                              inner join T_QM_INSPECTBILLENTRY b on a.fentryid=b.fentryid 
                              inner join T_BD_LOTMASTER c on b.FLOT=c.FLOTID
                             where FSRCBILLNO='{entry["SrcBillNo"]}'
                             and a.FMATERIALID='{ entry["MaterialId_Id"]}' and c.FNUMBER='{entry["Lot_text"]}' and FSRCENTRYSEQ={entry["SrcEntrySeq"]}
                             and F_260_PGMXTM='{entry["F_RUJP_PgBARCODE"]}' 
                             )aa on aa.FBILLNO=a.FSRCBILLNO and aa.FSEQ=a.FSRCENTRYSEQ and aa.FMATERIALID=a.FMATERIALID and c.FNUMBER=aa.FNUMBER)";
                                DBUtils.Execute(Context, up1sql);
                            }

                        }
                    }
                }
            }
            else if (FormOperation.Operation.Equals("UnAudit", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var date in e.DataEntitys)
                {
                    if (Context.CurrentOrganizationInfo.ID == 100026)
                    {
                        var entrys = date["Entity"] as DynamicObjectCollection;
                        foreach (var entry in entrys)
                        {
                            string upsql = $@"update T_QM_INSPECTBILLENTRY
                        set  F_260_RKSL=F_260_RKSL-{Convert.ToDouble(entry["RealQty"].ToString())}
                        where FENTRYID =(select a.FENTRYID from T_QM_INSPECTBILLENTRY_A a
                        inner join T_QM_INSPECTBILLENTRY b on a.fentryid=b.fentryid 
                        inner join T_BD_LOTMASTER c on b.FLOT=c.FLOTID
                        where FSRCBILLNO='{entry["SrcBillNo"]}'
                        and a.FMATERIALID='{ entry["MaterialId_Id"]}' and c.FNUMBER='{entry["Lot_text"]}' and FSRCENTRYSEQ={entry["SrcEntrySeq"]}
                        and F_260_PGMXTM='{entry["F_RUJP_PgBARCODE"]}' )";
                            DBUtils.Execute(Context, upsql);

                            string cxsql = $@"select a.FENTRYID from T_QM_INSPECTBILLENTRY_A a
                             inner join T_QM_INSPECTBILLENTRY b on a.fentryid=b.fentryid 
                             inner join T_BD_LOTMASTER c on b.FLOT=c.FLOTID
                             inner join (
                             select a.FENTRYID,a1.FBILLNO,FSEQ,a.FMATERIALID,c.FNUMBER from T_QM_INSPECTBILLENTRY_A a
                            inner join T_QM_INSPECTBILL a1 on a1.fid=a.fid
                            inner join T_QM_INSPECTBILLENTRY b on a.fentryid=b.fentryid 
                            inner join T_BD_LOTMASTER c on b.FLOT=c.FLOTID
                           where FSRCBILLNO='{entry["SrcBillNo"]}'
                           and a.FMATERIALID='{ entry["MaterialId_Id"]}' and c.FNUMBER='{entry["Lot_text"]}' and FSRCENTRYSEQ={entry["SrcEntrySeq"]}
                           )aa on aa.FBILLNO=a.FSRCBILLNO and aa.FSEQ=a.FSRCENTRYSEQ and aa.FMATERIALID=a.FMATERIALID and c.FNUMBER=aa.FNUMBER";
                            var cx = DBUtils.ExecuteDynamicObject(Context, cxsql);
                            if (cx.Count > 0)
                            {
                                string up1sql = $@"update T_QM_INSPECTBILLENTRY
                              set  F_260_RKSL=F_260_RKSL-{Convert.ToDouble(entry["RealQty"].ToString())}
                              where FENTRYID =(select a.FENTRYID from T_QM_INSPECTBILLENTRY_A a
                               inner join T_QM_INSPECTBILLENTRY b on a.fentryid=b.fentryid 
                               inner join T_BD_LOTMASTER c on b.FLOT=c.FLOTID
                               inner join (
                               select a.FENTRYID,a1.FBILLNO,FSEQ,a.FMATERIALID,c.FNUMBER from T_QM_INSPECTBILLENTRY_A a
                                inner join T_QM_INSPECTBILL a1 on a1.fid=a.fid
                              inner join T_QM_INSPECTBILLENTRY b on a.fentryid=b.fentryid 
                              inner join T_BD_LOTMASTER c on b.FLOT=c.FLOTID
                             where FSRCBILLNO='{entry["SrcBillNo"]}'
                             and a.FMATERIALID='{ entry["MaterialId_Id"]}' and c.FNUMBER='{entry["Lot_text"]}' and FSRCENTRYSEQ={entry["SrcEntrySeq"]}
                             and F_260_PGMXTM='{entry["F_RUJP_PgBARCODE"]}' 
                             )aa on aa.FBILLNO=a.FSRCBILLNO and aa.FSEQ=a.FSRCENTRYSEQ and aa.FMATERIALID=a.FMATERIALID and c.FNUMBER=aa.FNUMBER)";
                                DBUtils.Execute(Context, up1sql);
                            }
                            //直接调拨单
                            string zjdbsql = $@"select FLOT, c.fnumber,FSRCMATERIALID from T_STK_STKTRANSFERIN a
                                               inner join T_STK_STKTRANSFERINENTRY b on a.FID = B.FID
                                               left join T_BD_LOTMASTER c on c.FLOTID = B.FLOT
                                               WHERE FSTOCKOUTORGID=100026 and
                                               FSRCMATERIALID ='{entry["MaterialId_Id"]}'
                                               AND c.FNUMBER='{entry["Lot_text"]}'";
                            var zjdb = DBUtils.ExecuteDynamicObject(Context, zjdbsql);
                            if (zjdb.Count > 0)
                            {
                                throw new KDBusinessException("", "有调拨单无法反审核！");
                            }
                        }
                    }
                }
            }
            else if (FormOperation.Operation.Equals("Save", StringComparison.OrdinalIgnoreCase))
            {
                if (Context.CurrentOrganizationInfo.ID == 100026)
                {
                    foreach (var date in e.DataEntitys)
                    {
                        foreach (var entry in date["Entity"] as DynamicObjectCollection)
                        {

                            if (((DynamicObject)entry["MaterialID"])["Number"].ToString().Substring(0, 6) == "260.02" &&
                               (entry["MtoNo"] == null || entry["MtoNo"].ToString() == "" || entry["MtoNo"].ToString() == " "))
                            {
                                string str = entry["FHMSXBZ"].ToString();
                                string jysql = $@"select XMH.F_260_XMH,XMH.FPKID,FNAME
                                    from T_BD_MATERIAL a
                                    left join t_BD_MaterialPlan c on c.FMATERIALID=a.FMATERIALID
                                    left join T_PLN_MANUFACTUREPOLICY d on c.FMFGPOLICYID=d.FID
                                    LEFT JOIN PAEZ_t_Cust_Entry100355 XMH ON XMH.FMATERIALID=A.FMATERIALID
                                    left join ora_t_Cust100045_L x on XMH.F_260_XMH=x.FID
                                    WHERE 
                                    --D.FNUMBER='ZZCL003_SYS'
                                    --and 
                                    a.FMATERIALID={entry["MaterialId_Id"]}
                                    and FCREATEORGID=100026
                                    and XMH.F_260_XMH is not null
                                    and x.FNAME is not null
                                    order by XMH.FPKID desc";
                                var jy = DBUtils.ExecuteDynamicObject(Context, jysql);
                                if (jy.Count > 0)
                                {
                                    str += "_" + jy[0]["FNAME"].ToString();
                                }
                                string upsql = $@"/*dialect*/ update T_PRD_INSTOCKENTRY set FMTONO='{str}' where FENTRYID={entry["Id"]}";
                                DBUtils.Execute(Context, upsql);
                            }

                            string gxhbsql = $@"select F_260_DDLX from T_SFC_OPTRPT where FBILLNO='{entry["SrcBillNo"]}' and F_260_DDLX<>''";
                            var gxhb = DBUtils.ExecuteDynamicObject(Context, gxhbsql);
                            if (gxhb.Count > 0)
                            {
                                string ddlxupsql = $@"/*dialect*/update T_PRD_INSTOCKENTRY set F_260_FLDDLX='{gxhb[0]["F_260_DDLX"]}' where FENTRYID={entry["Id"]}";
                                DBUtils.Execute(Context, ddlxupsql);
                            }
                            
                        }
                    }
                }
            }

        }
        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);
            if (FormOperation.Operation.Equals("Audit", StringComparison.OrdinalIgnoreCase) || FormOperation.Operation.Equals("UnAudit", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var date in e.DataEntitys)
                {
                    if (date["StockOrgId_Id"].ToString() == "100026")
                    {
                        foreach (var entry in date["Entity"] as DynamicObjectCollection)
                        {
                            string upsql = $@"/*dialect*/update T_SFC_OPTRPTENTRY set F_260_CYS=FFINISHQTY-FSTOCKINQUAAUXQTY from T_SFC_OPTRPTENTRY_A
                        where T_SFC_OPTRPTENTRY.FENTRYID=T_SFC_OPTRPTENTRY_A.FENTRYID AND T_SFC_OPTRPTENTRY.FID IN (SELECT FID FROM T_SFC_OPTRPT where FBILLNO='{entry["SrcBillNo"].ToString()}') 
						and T_SFC_OPTRPTENTRY.FSEQ='{entry["SrcEntrySeq"].ToString()}'";
                            DBUtils.Execute(Context, upsql);
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
