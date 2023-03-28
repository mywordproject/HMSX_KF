using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Log;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Resource;
using Kingdee.BOS.Util;
using Kingdee.K3.Core.MFG.EntityHelper;
using Kingdee.K3.MFG.App;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.K3.MFG.Contracts.PRD;
using Kingdee.K3.MFG.Contracts.SFS;
using Kingdee.K3.MFG.PRD.App.ServicePlugIn;

namespace HMSX.Second.Plugin.生产制造
{
    [Description("生产订单--执行结案")]
    //热启动,不用重启IIS
    [HotUpdate]
    public class SCDDListPlugin : BasePRDOperationPlugIn  //SetStatus
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            String[] propertys = { "F_260_TextSJRW" };
            foreach (String property in propertys)
            {
                e.FieldKeys.Add(property);
            }
        }
        public override void OnPrepareOperationServiceOption(OnPrepareOperationServiceEventArgs e)
        {
            base.OnPrepareOperationServiceOption(e);
            e.SupportTransaction = false;
            e.AllowSetOperationResult = false;
        }
        public override void BeginOperationTransaction(BeginOperationTransactionArgs e)
        {
            base.BeginOperationTransaction(e);
            e.CancelOperation = true;
        }
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            string str = "";
            foreach (var date in e.DataEntitys)
            {
                if (date["PrdOrgId_Id"].ToString() == "100026")
                {
                    str += "'" + date["BillNo"] + "'" + ",";
                }
            }
            EndSetStatusTransactionArgs endSetStatusTransactionArgs = (EndSetStatusTransactionArgs)e;
            List<KeyValuePair<object, object>> EntryIds = endSetStatusTransactionArgs.PkEntryIds;
            if (FormOperation.Operation.Equals("ToClose", StringComparison.OrdinalIgnoreCase) ||
                FormOperation.Operation.Equals("ForceClose", StringComparison.OrdinalIgnoreCase))
            {
                if (str != "")
                {
                    //查询生产订单变更单状态为审核且超计划申请单未审核才允许结案
                    foreach (var ids in EntryIds)
                    {
                        string cjhsqsql = $@"	select F_260_SSDDBGZT,F_260_CJHZT from T_PRD_MO a
	                                  inner join T_PRD_MOENTRY b on a.fid=b.fid
	                                  where (F_260_SSDDBGZT in ('创建' ,'审核中','重新审核','暂存')	
	                                       and F_260_CJHZT in ('创建' ,'审核中','重新审核','暂存','已审核','')
	                                   or
	                                        F_260_SSDDBGZT in ('审核' )	
	                                   and     F_260_CJHZT in ('创建' ,'审核中','重新审核','暂存')
	                                   or     F_260_SSDDBGZT in ('' )	
	                                   and     F_260_CJHZT in ('创建' ,'审核中','重新审核','暂存','已审核'))
                                       AND FENTRYID IN({ids.Value})";
                        var cjhsqs = DBUtils.ExecuteDynamicObject(Context, cjhsqsql);
                        if (cjhsqs.Count > 0)
                        {
                            throw new KDBusinessException("", "1.生产订单变更单状态为审核且超计划申请单状态为审核才允许结案 \n 2.生产订单变更单状态为空且超计划申请单状态为空才允许结案！\n3.生产订单变更单状态为审核且超计划申请单状态为空才允许结案");
                        }
                    }
                    //查询所有子订单
                    string zddsql = $@"select a.FID,FENTRYID from T_PRD_MO a
                          inner join T_PRD_MOENTRY b on a.FID=b.FID
                          WHERE F_260_TEXTSJRW in ({str.Trim(',')}) AND SUBSTRING(FBILLNO,1,2)!='MO'";
                    var zdds = DBUtils.ExecuteDynamicObject(Context, zddsql);
                    if (zdds.Count > 0)
                    {
                        foreach (var zdd in zdds)
                        {
                            EntryIds.Add(new KeyValuePair<object, object>(zdd["FID"], zdd["FENTRYID"]));
                        }
                    }
                }
                foreach (var ids in EntryIds)
                {
                    if (Context.UserId != 9409480 && Context.UserId != 1226615)
                    {
                        //派工明细
                        string pgmxSQL = $@"select distinct FMOENTRYID from T_SFC_DISPATCHDETAIL a 
                         inner join T_SFC_DISPATCHDETAILENTRY b on a.FID = b.FID
                         where FSTATUS!='D' and FMOENTRYID={ids.Value} AND SUBSTRING(FMOBILLNO,1,2)='MO' and a.F_SBID_ORGID=100026";
                        var pgmx = DBUtils.ExecuteDynamicObject(Context, pgmxSQL);
                        if (pgmx.Count > 0)
                        {
                            throw new KDBusinessException("", "未汇报或派工未领料，不允许结案！");
                        }
                        //检验未入库
                        string gxhbsql = $@"select FFINISHQTY,FSTOCKINQUAAUXQTY,CASE WHEN D.FQTY IS NULL THEN 0 ELSE  D.FQTY END FQTY from T_SFC_OPTRPT a
                         inner join T_SFC_OPTRPTENTRY b on a.FID=b.FID
                         inner join T_SFC_OPTRPTENTRY_A C ON b.FENTRYID=C.FENTRYID
                         LEFT JOIN 
                         (SELECT F_260_HBBH,F_260_HBHH,SUM(FQTY)FQTY FROM T_QM_INSPECTBILL A
                         INNER JOIN T_QM_INSPECTBILLENTRY B ON A.FID=B.FID
                         INNER JOIN T_QM_INSPECTBILLENTRY_A B1 ON B1.FENTRYID=B.FENTRYID
                         INNER JOIN T_QM_IBPOLICYDETAIL C ON C.FENTRYID=B.FENTRYID
                         WHERE FUSEPOLICY='I' GROUP BY F_260_HBBH,F_260_HBHH  ) D ON D.F_260_HBBH=a.FBILLNO AND D.F_260_HBHH=b.FSEQ
                         WHERE FFINISHQTY-FSTOCKINQUAAUXQTY>(CASE WHEN D.FQTY IS NULL THEN 0 ELSE  D.FQTY END)
                         and FMOID={ids.Key} and FMOENTRYID={ids.Value} AND SUBSTRING(b.FMONUMBER,1,2)='MO' and a.FPRDORGID=100026";
                        var gxhb = DBUtils.ExecuteDynamicObject(Context, gxhbsql);
                        if (gxhb.Count > 0)
                        {
                            throw new KDBusinessException("", "检验未入库，不允许结案！");
                        }
                    }
                }
            }
            else if (FormOperation.Operation.Equals("UndoToRelease", StringComparison.OrdinalIgnoreCase) ||
                    FormOperation.Operation.Equals("UndoToStart", StringComparison.OrdinalIgnoreCase) ||
                    FormOperation.Operation.Equals("UndoToFinish", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var ids in EntryIds)
                {
                    if (Context.UserId != 9409480 && Context.UserId != 1226615)
                    {
                        //查询母订单
                        string mddsql = $@"select a.FID,b.FENTRYID,F_260_TEXTSJRW,FSTATUS from T_PRD_MO a
                                           inner join T_PRD_MOENTRY b on a.FID=b.FID
                                           inner join T_PRD_MOENTRY_A b1 on b.FENTRYID=b1.FENTRYID
                                           where FBILLNO in(
                                           select F_260_TEXTSJRW from T_PRD_MO a
                                           inner join T_PRD_MOENTRY b on a.FID=b.FID
                                           where fentryid={ids.Value})  
                                           and SUBSTRING(FBILLNO,1,2)!='MO' 
                                           and FPRDORGID=100026 
                                           and b1.FSTATUS=6";
                        var mdd = DBUtils.ExecuteDynamicObject(Context, mddsql);
                        if (mdd.Count > 0)
                        {
                            throw new KDBusinessException("", "母订单为结案状态，子订单不允许反结案！");
                        }


                        //手动结案必须是当前结案人
                        string scddsql = $@"select * from T_PRD_MOENTRY_Q a
                                        inner join T_PRD_MOENTRY_A a1 on a.FENTRYID=a1.FENTRYID
                                        inner join T_PRD_MO b on a.fid=b.fid  
                                        where (FCLOSETYPE in ('B','C') AND FFORCECLOSERID<>{Context.UserId} OR FCLOSETYPE='')
                                        and a.FENTRYID={ids.Value} and FPRDORGID=100026  and FSTATUS=6";
                        var scdd = DBUtils.ExecuteDynamicObject(Context, scddsql);
                        if (scdd.Count > 0)
                        {
                            throw new KDBusinessException("", "反结案人与结案人不一致或结案类型为空，不允许反结案！");
                        }
                    }
                }
            }
            else if(FormOperation.Operation.Equals("JAYQ", StringComparison.OrdinalIgnoreCase))
            {
                //查询所有母订单结案，子订单未结案得
                string zddsql = $@"select a.fbillno,a.FID,b.FENTRYID from T_PRD_MO a
                 inner join T_PRD_MOENTRY b on a.FID=b.FID
                 inner join T_PRD_MOENTRY_A b1 on b.FENTRYID=b1.FENTRYID
                 WHERE 
                 SUBSTRING(FBILLNO,1,2)!='MO'
                 and b1.FSTATUS not in (6,7)
                 and F_260_TEXTSJRW in(
                 select a.fbillno from T_PRD_MO a
                 inner join T_PRD_MOENTRY b on a.FID=b.FID
                 inner join T_PRD_MOENTRY_A b1 on b.FENTRYID=b1.FENTRYID
                 WHERE 
                 SUBSTRING(FBILLNO,1,2)!='MO'
                 and FPRDORGID=100026 
                 and b1.FSTATUS in (6,7))";
                var zdds = DBUtils.ExecuteDynamicObject(Context, zddsql);
                if (zdds.Count > 0)
                {
                    foreach (var zdd in zdds)
                    {
                        EntryIds.Add(new KeyValuePair<object, object>(zdd["FID"], zdd["FENTRYID"]));
                    }
                }
            }
            IEnumerable<long> moEntryIds;
            if (endSetStatusTransactionArgs.PkEntryIds.Any((KeyValuePair<object, object> x) => x.Value.IsNullOrEmpty()))
            {
                List<long> source = (
                    from m in endSetStatusTransactionArgs.PkEntryIds
                    where m.Value.IsNullOrEmpty()
                    select m into o
                    select Convert.ToInt64(o.Key)).ToList<long>();
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(" SELECT TMOE.FID,TMOE.FENTRYID FROM T_PRD_MOENTRY TMOE  ");
                stringBuilder.AppendLine(" INNER JOIN" + StringUtils.GetSqlWithCardinality(source.Distinct<long>().Count<long>(), "@FID", 1, true) + "T ON T.FID=TMOE.FID ");
                List<SqlParam> list = new List<SqlParam>();
                list.Add(new SqlParam("@FID", KDDbType.udt_inttable, source.Distinct<long>().ToArray<long>()));
                moEntryIds =
                    from m in AppServiceContext.DbUtils.ExecuteDynamicObject(base.Context, stringBuilder.ToString(), list.ToArray())
                    select m.GetDynamicValue("FENTRYID", 0L);
            }
            else
            {
                moEntryIds =
                    from o in endSetStatusTransactionArgs.PkEntryIds
                    select Convert.ToInt64(o.Value);
            }
            if (base.FormOperation.Operation == "ToFinish" || base.FormOperation.Operation == "ToClose")
            {
                base.Option.SetVariableValue("IsCalStockInLimitL", true);
            }
            if (base.FormOperation.Operation == "ForceClose")
            {
                base.Option.SetVariableValue("IsSFCStartValidator", true);
            }
            IOperationResult operationResult = AppServiceContext.GetMFGService<IMOService>().MOStateTransfer(base.Context, moEntryIds, base.FormOperation.Operation, base.Option);
            if (operationResult.OperateResult.Count == 0)
            {
                this.GetOperationResult(operationResult, e.DataEntitys, moEntryIds);
            }
            else
            {
                string message = operationResult.OperateResult[0].Message;
                operationResult.OperateResult.Add(new OperateResult
                {
                    SuccessStatus = true,
                    Message = message,
                    MessageType = MessageType.Warning,
                    PKValue = this.GetDefPkValue(e.DataEntitys)
                });
            }
            OperateResult item = (
                from w in operationResult.OperateResult
                where w.PKValue.IsNullOrEmpty()
                select w).ToList<OperateResult>().FirstOrDefault<OperateResult>();
            operationResult.OperateResult.Remove(item);
            base.OperationResult.MergeResult(operationResult);
        }

        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);
            List<long> list = (
                from p in e.DataEntitys
                from pp in p.GetDynamicObjectItemValue<DynamicObjectCollection>("TreeEntity", null)
                select pp.GetDynamicObjectItemValue("Id", 0L)).ToList<long>();
            if (list.Count > 0 && base.FormOperation.Operation.EqualsIgnoreCase("UNDOTOPLANCONFIRM"))
            {
                AppServiceContext.GetMFGService<ISFSCacheManagerService>().ClearCachesByMO(base.Context, list.Distinct<long>().ToList<long>(), true);
            }
        }
        protected void GetOperationResult(IOperationResult result, DynamicObject[] moDatas, IEnumerable<long> moEntryIds)
        {
            List<long> list = (
                from s in result.ValidationErrors
                select Convert.ToInt64(s.BillPKID)).ToList<long>();
            for (int i = 0; i < moDatas.Length; i++)
            {
                DynamicObject dynamicObject = moDatas[i];
                string dynamicValue = dynamicObject.GetDynamicValue<string>("BillNo", null);
                long dynamicValue2 = dynamicObject.GetDynamicValue("Id", 0L);
                DynamicObjectCollection dynamicValue3 = dynamicObject.GetDynamicValue<DynamicObjectCollection>("TreeEntity", null);
                foreach (DynamicObject current in dynamicValue3)
                {
                    long dynamicValue4 = current.GetDynamicValue("Id", 0L);
                    if (moEntryIds.Contains(dynamicValue4) && !list.Contains(dynamicValue4))
                    {
                        int dynamicValue5 = current.GetDynamicValue("Seq", 0);
                        result.OperateResult.Add(new OperateResult
                        {
                            SuccessStatus = true,
                            Message = this.GetMessage(dynamicValue, dynamicValue5),
                            MessageType = MessageType.Normal,
                            PKValue = dynamicValue2
                        });
                    }
                }
            }
        }
        protected string GetMessage(string billNo, int seq)
        {
            return string.Format(ResManager.LoadKDString("生产订单{0}第{1}行分录{2}成功！", "015077000012705", SubSystemType.MFG, new object[0]), billNo, seq, base.FormOperation.OperationName);
        }
        protected object GetDefPkValue(IEnumerable<DynamicObject> dataEntities)
        {
            if (dataEntities == null)
            {
                return null;
            }
            if (dataEntities.IsEmpty<DynamicObject>())
            {
                return null;
            }
            return dataEntities.First<DynamicObject>().GetDynamicObjectItemValue("Id", 0L);
        }
        private void WriteLog(Context ctx, OperateResultCollection result)
        {
            ILogService logService = ServiceFactory.GetLogService(ctx);
            List<LogObject> list = new List<LogObject>();
            foreach (OperateResult current in result)
            {
                list.Add(new LogObject
                {
                    Description = current.Message,
                    Environment = OperatingEnvironment.BizOperate,
                    OperateName = base.FormOperation.OperationName,
                    ObjectTypeId = "PRD_MO",
                    SubSystemId = "47"
                });
            }
            if (list.Count > 0)
            {
                logService.BatchWriteLog(ctx, list);
            }
        }
    }
}
