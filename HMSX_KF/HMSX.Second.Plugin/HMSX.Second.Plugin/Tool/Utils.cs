using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Kingdee.BOS;
using Kingdee.BOS.App;
using Kingdee.BOS.App.Core;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.Operation;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Interaction;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.ConvertElement.ServiceArgs;
using Kingdee.BOS.Core.Metadata.FormElement;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using HMSX;
using Newtonsoft.Json.Linq;

namespace HMSX.Second.Plugin.Tool
{
    /// <summary>
    /// 工具类
    /// </summary>
    public static class Utils
    {
        #region 加载数据包

        /// <summary>
        /// 生成计划订单
        /// </summary>
        /// <param name="planOrderNo"></param>
        /// <param name="context"></param>
        /// <param name="ordername"></param>
        /// <returns>true/false</returns>
        public static bool GenPlanOrder(Context context, object planOrderNo, string ordername)
        {
            const string formId = "PLN_PLANORDER";
            var meta = (FormMetadata)MetaDataServiceHelper.Load(context, formId);
            var selectPlanOrderIdSql = $"SELECT FID FROM T_PLN_PLANORDER WHERE FBILLNO = '{planOrderNo}';";
            // 获取计划订单信息
            var palnOrderInfo = DBUtils.ExecuteDynamicObject(context, selectPlanOrderIdSql);

            if (palnOrderInfo.Count == 0)
            {
                throw new KDException("", $"没有找到相关计划订单！传入的单据编号为:{planOrderNo}");
            }


            if (OpenWebView(context, meta, formId, palnOrderInfo[0]["FID"]) is IBillView
                planOrderView)
            {
                // 获取复制前的单据信息
                var beforeDataObj = planOrderView.Model.DataObject;
                // 执行复制操作
                planOrderView.Model.Copy(palnOrderInfo[0]["FID"]);

                // 获取复制后的操作
                var afterDataObj = planOrderView.Model.DataObject;
                // 设置备注
                afterDataObj["Description"] = $"计划订单号{planOrderNo}由{ordername}转回";
                // 运算编号
                afterDataObj["ComputerNo"] = beforeDataObj["ComputerNo"];
                // 净需求
                afterDataObj["DemandQty"] = beforeDataObj["DemandQty"];
                // 计划订单量
                afterDataObj["OrderQty"] = beforeDataObj["OrderQty"];
                // 需求日期
                afterDataObj["DemandDate"] = beforeDataObj["DemandDate"];
                // 计划跟踪号
                afterDataObj["MtoNo"] = beforeDataObj["MtoNo"];
                // 数据来源
                afterDataObj["DataSource"] = beforeDataObj["DataSource"];
                // 计划标签
                // afterDataObj["MrpNote"] = beforeDataObj["MrpNote"];
                // 需求来源
                afterDataObj["DemandType"] = beforeDataObj["DemandType"];
                // 需求单据编号
                afterDataObj["SaleOrderNo"] = beforeDataObj["SaleOrderNo"];
                // 需求单据行号
                afterDataObj["SaleOrderEntrySeq"] = beforeDataObj["SaleOrderEntrySeq"];
                // 基本计划订单数量
                afterDataObj["BaseOrderQty"] = beforeDataObj["BaseOrderQty"];
                // 基本单位净需求数量
                afterDataObj["BaseDemandQty"] = beforeDataObj["BaseDemandQty"];
                //建议采购、生产日期
                afterDataObj["PlanStartDate"] =
                    DateTime.Compare(DateTime.Now, DateTime.Parse(beforeDataObj["PlanStartDate"].ToString())) > 0
                        ? DateTime.Now
                        : beforeDataObj["PlanStartDate"];
                //确认采购、生产日期
                afterDataObj["FirmStartDate"] =
                    DateTime.Compare(DateTime.Now, DateTime.Parse(beforeDataObj["FirmStartDate"].ToString())) > 0
                        ? DateTime.Now
                        : beforeDataObj["FirmStartDate"];
                //建议到货、完工日期
                afterDataObj["PlanFinishDate"] =
                    DateTime.Compare(DateTime.Now, DateTime.Parse(beforeDataObj["PlanFinishDate"].ToString())) > 0
                        ? DateTime.Now
                        : beforeDataObj["PlanFinishDate"];
                //确认到货、完工日期
                afterDataObj["FirmFinishDate"] =
                    DateTime.Compare(DateTime.Now, DateTime.Parse(beforeDataObj["FirmFinishDate"].ToString())) > 0
                        ? DateTime.Now
                        : beforeDataObj["FirmFinishDate"];

                // 标准的复制操作不会复制计划bom和联副产品
                var modelPlanBomEntry = afterDataObj["PLBOMENTRY"] as DynamicObjectCollection;
                // 复制之前的单据信息到复制后的单据
                if (!(beforeDataObj["PLBOMENTRY"] is DynamicObjectCollection beforePlanBomEntry)) return false;
                foreach (var dynamicObject in beforePlanBomEntry)
                {
                    // dynamicObject["DemandDate"] = DateTime.Now;
                    // ？判空
                    modelPlanBomEntry?.Add(dynamicObject);
                }

                // 联副产品
                var plcobyEntry = afterDataObj["PLCOBYENTRY"] as DynamicObjectCollection;
                if (!(beforeDataObj["PLCOBYENTRY"] is DynamicObjectCollection beforePlanPlcobY)) return false;
                foreach (var dynamicObject in beforePlanPlcobY)
                {
                    // ？判空
                    plcobyEntry?.Add(dynamicObject);
                }


                var operationResult = planOrderView.Model.Save();
                if (operationResult.IsSuccess)
                {
                    //更新复制前单据
                    var updateSql = $"UPDATE T_PLN_PLANORDER_L " +
                                    $"SET FDESCRIPTION = '生成新单据{operationResult.OperateResult[0].Number}' " +
                                    $"WHERE FID = '{palnOrderInfo[0]["FID"]}' AND FLOCALEID = 2052;";
                    DBUtils.Execute(context, updateSql);
                    return operationResult.IsSuccess;
                }

                return false;
            }

            return false;
        }
        /// <summary>
        ///  清除缓存
        /// </summary>
        /// <param name="billId">物料单据标识</param>
        /// <param name="id">物料id</param>
        /// <param name="context"></param>
        public static void RemoveCache(string billId, object id, Context context)
        {
            var metaDataService = ServiceHelper.GetService<IMetaDataService>();
            var formMetadata = metaDataService.Load(context, billId) as FormMetadata;
            var dataEntityCacheManager = new DataEntityCacheManager(context, formMetadata?.BusinessInfo.GetDynamicObjectType());
            dataEntityCacheManager.RemoveCacheByPrimaryKey(id);
        }

        /// <summary>
        /// 判断日期是否小于当前日期
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool IsDateBeforeOrToday(string input)
        {
            var result = true;

            if (input != null)
            {
                DateTime dTCurrent = DateTime.Now;
                int currentDateValues = Convert.ToInt32(dTCurrent.ToString("MMddyyyy"));
                int inputDateValues = Convert.ToInt32(input.Replace("-", ""));

                result = inputDateValues <= currentDateValues;
            }
            else
            {
                result = true;
            }

            return result;
        }


        /// <summary>
        /// 根据基础资料的编码，加载基础资料数据包
        /// </summary>
        /// <param name="ctx">上下文</param>
        /// <param name="formId">基础资料FormId</param>
        /// <param name="number">基础资料编码</param>
        /// <param name="orgnumber">使用组织的编码</param>
        /// <returns></returns>
        public static DynamicObject LoadBDData(Context ctx, string formId, string number, string orgnumber = "")
        {
            var meta = MetaDataServiceHelper.Load(ctx, formId) as FormMetadata;

            // 构建查询参数，设置过滤条件
            var queryParam = new QueryBuilderParemeter
            {
                FormId = formId,
                BusinessInfo = meta.BusinessInfo
            };
            if (string.IsNullOrWhiteSpace(orgnumber))
            {
                queryParam.FilterClauseWihtKey = $" {meta.BusinessInfo.GetForm().NumberFieldKey} = '{number}'";
            }
            else
            {
                var orgId = LoadBDData(ctx, formId, orgnumber)["Id"].ToString();
                queryParam.FilterClauseWihtKey =
                    $" {meta.BusinessInfo.GetForm().NumberFieldKey} = '{number}' AND {meta.BusinessInfo.GetForm().UseOrgFieldKey} = '{orgId}' ";
            }

            var bdObjs = BusinessDataServiceHelper.Load(ctx,
                meta.BusinessInfo.GetDynamicObjectType(),
                queryParam);

            if (bdObjs.Length == 0)
            {
                return null;
            }

            return bdObjs[0];
        }

        /// <summary>
        /// 根据基础资料的主键Id，加载基础资料数据包
        /// </summary>
        /// <param name="ctx">上下文</param>
        /// <param name="formId">基础资料FormId</param>
        /// <param name="id">基础资料ID</param>
        /// <returns></returns>
        public static DynamicObject LoadBDData(Context ctx, string formId, int id)
        {
            var meta = MetaDataServiceHelper.Load(ctx, formId) as FormMetadata;

            // 构建查询参数，设置过滤条件
            var queryParam = new QueryBuilderParemeter
            {
                FormId = formId,
                BusinessInfo = meta.BusinessInfo,

                FilterClauseWihtKey = $" {meta.BusinessInfo.GetForm().PkFieldName} = '{id}' "
            };

            var bdObjs = BusinessDataServiceHelper.Load(ctx,
                meta.BusinessInfo.GetDynamicObjectType(),
                queryParam);
            return bdObjs.Length == 0 ? null : bdObjs[0];
        }

        /// <summary>
        /// 查询基础资料
        /// </summary>
        /// <param name="ctx">上下文</param>
        /// <param name="meta">单据的Matadata</param>
        /// <param name="paremeter">查询参数</param>
        /// <returns></returns>
        public static List<DynamicObject> LoadBDDataList(Context ctx, FormMetadata meta,
            QueryBuilderParemeter paremeter)
        {
            var bdObjs = BusinessDataServiceHelper.Load(ctx,
                meta.BusinessInfo.GetDynamicObjectType(),
                paremeter);
            return bdObjs.ToList<DynamicObject>();
        }

        /// <summary>
        /// 加载单据数据
        /// </summary>
        /// <example><![CDATA[
        /// string sfilter = string.Format("FID = {0} ", fid);
        /// var filter = OQLFilter.CreateHeadEntityFilter(sfilter);
        /// var data = Utils.LoadBillData(Context, "PAEZ_DistributorContract", new List<string> { "FName", "FCustID" }, filter);
        /// ]]>
        /// </example>
        /// <param name="ctx"></param>
        /// <param name="formId">单据编号</param>
        /// <param name="items">需要返回的字段</param>
        /// <param name="filter">过滤器</param>
        public static DynamicObject[] LoadBillData(Context ctx, string formId, List<string> items, string filter)
        {
            var ofilter = OQLFilter.CreateHeadEntityFilter(filter);
            var iteminfo = new List<SelectorItemInfo>();
            foreach (var item in items)
                iteminfo.Add(new SelectorItemInfo(item));

            return BusinessDataServiceHelper.Load(ctx, formId, iteminfo, ofilter);
        }

        /// <summary>
        /// 加载单据数据包
        /// </summary>
        /// <param name="ctx">上下文</param>
        /// <param name="formId">单据编号</param>
        /// <param name="filter">过滤器回调</param>
        /// <param name="items">需要返回的属性</param>
        /// <returns></returns>
        public static DynamicObject[] LoadBillData(Context ctx, string formId, Func<FormMetadata, string> filter,
            params string[] items)
        {
            var meta = MetaDataServiceHelper.Load(ctx, formId) as FormMetadata;
            return LoadBillData(ctx, formId, items.ToList<string>(), filter?.Invoke(meta));
        }

        /// <summary>
        /// 加载单据数据
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="info">BusinessInfo</param>
        /// <param name="items">需要返回的字段</param>
        /// <param name="filter">过滤器</param>
        public static DynamicObject[] LoadBillData(Context ctx, BusinessInfo info, List<string> items, OQLFilter filter)
        {
            var iteminfo = new List<SelectorItemInfo>();
            foreach (var item in items)
                iteminfo.Add(new SelectorItemInfo(item));

            return BusinessDataServiceHelper.Load(ctx, info, iteminfo, filter);
        }

        /// <summary>
        /// 加载单据数据
        /// </summary>
        /// <param name="ctx">上下文</param>
        /// <param name="formId">单据标志</param>
        /// <param name="filter">过滤字符串</param>
        /// <returns></returns>
        public static DynamicObject[] LoadBillData(Context ctx, string formId, string filter)
        {
            var meta = MetaDataServiceHelper.Load(ctx, formId) as FormMetadata;
            var queryParam = new QueryBuilderParemeter
            {
                FormId = formId,
                BusinessInfo = meta.BusinessInfo,
                FilterClauseWihtKey = filter
            };

            var bdObjs = BusinessDataServiceHelper.Load(ctx,
                meta.BusinessInfo.GetDynamicObjectType(), queryParam);

            return bdObjs;
        }

        /// <summary>
        /// 根据单据标识加载单据数据类型
        /// </summary>
        /// <param name="ctx">上下文</param>
        /// <param name="fromId">单据标志</param>
        /// <param name="key">单据体标志</param>
        /// <param name="orm">单据体ORM实体名</param>
        /// <returns></returns>
        public static DynamicObject LoadBillDataType(Context ctx, string fromId, string[] key = null,
            string[] orm = null)
        {
            var metadataService = ServiceHelper.GetService<IMetaDataService>();
            var metadata = metadataService.Load(ctx, fromId) as FormMetadata;
            var info = metadata.BusinessInfo;
            var obj = new DynamicObject(metadata.BusinessInfo.GetDynamicObjectType());
            if (key != null)
            {
                EIEnumerable.ForEach(key, (k, i) =>
                {
                    var datatype = info.GetEntryEntity(k).DynamicObjectType;
                    var dataobj = new DynamicObject(datatype);
                    EObject.TypeCast<DynamicObjectCollection>(obj[orm[i]]).Add(dataobj);
                });
            }

            return obj;
        }



        /// <summary>
        /// 加载单据体数据类型
        /// </summary>
        /// <param name="ctx">上下文</param>
        /// <param name="fromId">单据标志</param>
        /// <param name="entryId">分录标识</param>
        /// <returns></returns>
        public static DynamicObject LoadBillDataType(Context ctx, string fromId, string entryId)
        {
            var metadataService = ServiceHelper.GetService<IMetaDataService>();
            var metadata = metadataService.Load(ctx, fromId) as FormMetadata;
            var info = metadata.BusinessInfo;
            var datatype = info.GetEntryEntity(entryId).DynamicObjectType;
            var dataobj = new DynamicObject(datatype);
            return dataobj;
        }

        /// <summary>
        /// 加载当前单据的数据
        /// </summary>
        /// <param name="plugIn"></param>
        /// <returns></returns>
        public static DynamicObject LoadCurrentBillData(AbstractDynamicFormPlugIn plugIn)
        {
            return plugIn.Model.DataObject;
        }

        #endregion

        #region 下推相关

        /// <summary>
        /// 自动下推
        /// </summary>
        /// <param name="ctx">上下文</param>
        /// <param name="sourceFormId">源单编号</param>
        /// <param name="targetFormId">目标单编号</param>
        /// <param name="sourceBillId">源单ID</param>
        /// <param name="saveOption">保存参数</param>
        /// <param name="func">修改目标单的数据并返回</param>
        /// <param name="callback">回调</param>
        /// <returns>是否下推成功</returns>
        public static Result<JArray, JArray> AutoPush(Context ctx, string sourceFormId,
            string targetFormId,
            string sourceBillId, SaveOption saveOption, ListSelectedRow[] listSelectedRows = null,
            Func<DynamicObject[], DynamicObject[]> func = null,
            Action<IOperationResult> callback = null)
        {
            var result = new Result<JArray, JArray>
            {
                IsSuccess = false,
                Msg = "",
                Ext = new JArray(),
                Data = new JArray()
            };

            var rules = ConvertServiceHelper.GetConvertRules(ctx, sourceFormId, targetFormId);
            var rule = rules.FirstOrDefault(t => t.IsDefault);
            if (listSelectedRows == null)
            {
                var row = new ListSelectedRow(sourceBillId, string.Empty, 0, sourceFormId);
                listSelectedRows = new ListSelectedRow[] { row };
            }

            var TargetBillTypeId = "";
            try
            {
                var sql =
                    $"SELECT FBILLTYPEID FROM T_BAS_BILLTYPE WHERE FBILLFORMID = '{targetFormId}' AND FISDEFAULT=1";
                TargetBillTypeId = DBServiceHelper.ExecuteDataSet(ctx, sql).Tables[0].Rows[0][0].ToString();
                if (StringUtils.EqualsIgnoreCase(sourceFormId, "PRD_MO"))
                {
                    TargetBillTypeId = "50fa7c2eda7947b89fab5431bf25d48e";
                }
            }
            catch (Exception)
            {
                TargetBillTypeId = "";
            }

            // 调用下推服务，生成下游单据数据包
            ConvertOperationResult operationResult = null;
            Dictionary<string, object> custParams = new Dictionary<string, object>();
            try
            {
                var pushArgs = new PushArgs(rule, listSelectedRows)
                {
                    TargetBillTypeId = TargetBillTypeId, // TargetBillTypeId, 请设定目标单据单据类型。如无单据类型，可以空字符
                    TargetOrgId = ctx.CurrentOrganizationInfo.ID, // 请设定目标单据主业务组织。如无主业务组织，可以为0
                    CustomParams = custParams, // 可以传递额外附加的参数给单据转换插件，如无此需求，可以忽略
                };
                // 执行下推操作，并获取下推结果
                operationResult = ConvertServiceHelper.Push(ctx, pushArgs, OperateOption.Create());
            }
            catch (KDExceptionValidate ex)
            {
                result.Msg = ex.Message + "\n" + ex.ValidateString;
                return result;
            }
            catch (Exception ex)
            {
                result.Msg = ex.Message;
                return result;
            }

            // 获取生成的目标单据数据包
            DynamicObject[] objs = (from p in operationResult.TargetDataEntities
                                    select p.DataEntity).ToArray();
            if (func != null)
            {
                objs = func.Invoke(objs);
            }


            // 读取目标单据元数据
            var targetBillMeta = MetaDataServiceHelper.Load(ctx, targetFormId) as FormMetadata;
            var Option = OperateOption.Create();
            // 忽略全部需要交互性质的提示，直接保存；
            Option.SetIgnoreWarning(true); // 提交数据库保存，并获取保存结果

            IOperationResult Iresult;
            if (saveOption == SaveOption.Submit)
            {
                var saveResult = BusinessDataServiceHelper.Save(ctx, targetBillMeta.BusinessInfo, objs, Option, "Save");
                if (!saveResult.IsSuccess)
                {
                    var msg = string.Join("\n", (from p in saveResult.ValidationErrors select p.Message));
                    result.Msg = msg;
                    result.Ext.Add(new JObject { { "OperationName", "Save" }, { "IsSuccess", false }, { "Msg", result.Msg } });
                    return result;
                }

                result.Ext.Add(new JObject { { "OperationName", "Save" }, { "IsSuccess", true }, { "Msg", "成功" } });

                long newbillId = 0;
                foreach (var dataResult in saveResult.SuccessDataEnity)
                {
                    if (dataResult["Id"] != null)
                    {
                        newbillId = long.Parse(dataResult["Id"].ToString());
                    }
                }

                var submitResult = BusinessDataServiceHelper.Submit(ctx, targetBillMeta.BusinessInfo,
                    new object[] { newbillId }, "Submit");
                if (!submitResult.IsSuccess)
                {
                    var msg = string.Join("\n", (from p in submitResult.ValidationErrors select p.Message));
                    result.Msg = msg;
                    result.Ext.Add(new JObject
                        {{"OperationName", "Submit"}, {"IsSuccess", false}, {"Msg", result.Msg}});
                    return result;
                }

                result.Ext.Add(new JObject { { "OperationName", "Submit" }, { "IsSuccess", true }, { "Msg", "成功" } });

                Iresult = submitResult;
            }
            else if (saveOption == SaveOption.Audit)
            {
                var saveResult = BusinessDataServiceHelper.Save(ctx, targetBillMeta.BusinessInfo, objs, Option, "Save");
                if (!saveResult.IsSuccess)
                {
                    var msg = string.Join("\n", (from p in saveResult.ValidationErrors select p.Message));
                    result.Msg = msg;
                    result.Ext.Add(new JObject { { "OperationName", "Save" }, { "IsSuccess", false }, { "Msg", result.Msg } });
                    return result;
                }

                result.Ext.Add(new JObject { { "OperationName", "Save" }, { "IsSuccess", true }, { "Msg", "成功" } });

                long newbillId = 0;
                var BillNo = "";
                foreach (var dataResult in saveResult.SuccessDataEnity)
                {
                    if (dataResult["Id"] != null)
                    {
                        newbillId = long.Parse(dataResult["Id"].ToString());
                        BillNo = dataResult["BillNo"].ToString();
                    }
                }

                var submitResult = BusinessDataServiceHelper.Submit(ctx, targetBillMeta.BusinessInfo,
                    new object[] { newbillId }, "Submit");
                if (!submitResult.IsSuccess)
                {
                    var msg = string.Join("\n", (from p in submitResult.ValidationErrors select p.Message));
                    result.Msg = msg;
                    result.Ext.Add(new JObject
                        {{"OperationName", "Submit"}, {"IsSuccess", false}, {"Msg", result.Msg}});
                    return result;
                }

                result.Ext.Add(new JObject { { "OperationName", "Submit" }, { "IsSuccess", true }, { "Msg", "成功" } });

                var auditResult =
                    BusinessDataServiceHelper.Audit(ctx, targetBillMeta.BusinessInfo, new object[] { newbillId }, null);
                if (!auditResult.IsSuccess)
                {
                    var msg = string.Join("\n", (from p in auditResult.ValidationErrors select p.Message));
                    var smsg = auditResult.InteractionContext.SimpleMessage;
                    if (!string.IsNullOrWhiteSpace(smsg))
                    {
                        msg += $"\n单据编号为:{BillNo}的销售出库单审核失败,原因如下:" + smsg;
                    }

                    result.Msg = msg;
                    result.Ext.Add(new JObject { { "OperationName", "Audit" }, { "IsSuccess", false }, { "Msg", result.Msg } });
                    return result;
                }

                result.Ext.Add(new JObject { { "OperationName", "Audit" }, { "IsSuccess", true }, { "Msg", "成功" } });

                Iresult = auditResult;
            }
            else if (saveOption == SaveOption.Draft)
            {
                var drafResult = BusinessDataServiceHelper.Draft(ctx, targetBillMeta.BusinessInfo, objs, Option);
                if (!drafResult.IsSuccess)
                {
                    var msg = string.Join("\n", (from p in drafResult.ValidationErrors select p.Message));
                    result.Msg = msg;
                    result.Ext.Add(new JObject { { "OperationName", "Draft" }, { "IsSuccess", false }, { "Msg", result.Msg } });
                    return result;
                }

                result.Ext.Add(new JObject { { "OperationName", "Draft" }, { "IsSuccess", true }, { "Msg", "成功" } });

                Iresult = drafResult;
            }
            else if (saveOption == SaveOption.Save)
            {
                var saveResult = BusinessDataServiceHelper.Save(ctx, targetBillMeta.BusinessInfo, objs, Option, "Save");
                if (!saveResult.IsSuccess)
                {
                    var msg = string.Join("\n", (from p in saveResult.ValidationErrors select p.Message))
                        .TrimStart('\n');
                    result.Msg = msg;
                    result.Ext.Add(new JObject { { "OperationName", "Save" }, { "IsSuccess", false }, { "Msg", result.Msg } });
                    return result;
                }

                result.Ext.Add(new JObject { { "OperationName", "Save" }, { "IsSuccess", true }, { "Msg", "成功" } });

                Iresult = saveResult;
            }
            else if (saveOption == SaveOption.Other)
            {
                result.IsSuccess = true;
                return result;
            }
            else
            {
                return result;
            }

            callback?.Invoke(Iresult);

            result.IsSuccess = true;
            return result;
        }


        /// <summary>
        /// 执行下推并弹出窗口
        /// </summary>
        /// <param name="Bill">原单的this对象</param>
        /// <param name="targetFormId">目标单标识</param>
        /// <param name="pushResult">下推转化的结果</param>
        /// <param name="objs">数据包</param>
        public static void ShowPushResult(AbstractDynamicFormPlugIn Bill, string targetFormId,
            ConvertOperationResult pushResult, DynamicObject[] objs)
        {
            // 构建界面显示参数
            var param = new BillShowParameter
            {
                ParentPageId = Bill.View.PageId
            };
            if (objs.Length == 1)
            {
                // 如果下推生成的目标单仅仅只有一张，则直接打开下游单据的编辑界面
                param.FormId = targetFormId; // formId
                param.Status = OperationStatus.ADDNEW; // 新建状态
                param.CreateFrom = CreateFrom.Push; // 标志：下推创建的单据
                param.AllowNavigation = false; // 不显示导航菜单
                // 把下推结果放在缓存交换区
                var customParamKey = "_ConvertSessionKey";
                var sessionKey_Result = "ConverOneResult";
                var sessionKey_ErrorInfo = "ConvertValidationInfo";
                param.CustomParams.Add(customParamKey, sessionKey_Result);
                Bill.View.Session[sessionKey_ErrorInfo] = pushResult.ValidationErrors;
                Bill.View.Session[sessionKey_Result] = objs[0];
            }
            else if (objs.Length > 1)
            {
                // 如果下推生成的目标单有多行，则打开批量编辑界面
                param.FormId = "BOS_ConvertResultForm";

                // 把下推结果放在缓存交换区
                var sessionKey_Result = "ConvertResults";
                var sessionKey_ErrorInfo = "ConvertValidationInfo";
                Bill.View.Session[sessionKey_Result] = objs;
                Bill.View.Session[sessionKey_ErrorInfo] = pushResult.ValidationErrors;
                param.CustomParams.Add("_ConvertResultFormId", targetFormId);
            }
            else
            {
                return;
            }

            // 显示界面
            param.OpenStyle.ShowType = ShowType.MainNewTabPage;
            Bill.View.ShowForm(param);
        }

        /// <summary>
        /// 保存参数
        /// </summary>
        public enum SaveOption
        {
            /// <summary>
            /// 保存
            /// </summary>
            Save,

            /// <summary>
            /// 暂存
            /// </summary>
            Draft,

            /// <summary>
            /// 提交
            /// </summary>
            Submit,

            /// <summary>
            /// 审核
            /// </summary>
            Audit,

            /// <summary>
            /// 其他
            /// </summary>
            Other
        }

        #endregion

        #region 数据库相关

        /// <summary>
        /// 获取主键
        /// </summary>
        /// <param name="ctx">上下文</param>
        /// <param name="tableName">表名</param>
        /// <returns></returns>
        public static long GetPK(Context ctx, string tableName)
        {
            return GetPK(ctx, tableName, 1).ElementAtOrDefault(0);
        }

        /// <summary>
        /// 获取数据库主键
        /// </summary>
        /// <param name="ctx">上下文</param>
        /// <param name="tableName">表名</param>
        /// <param name="count">数量</param>
        /// <returns></returns>
        public static IEnumerable<long> GetPK(Context ctx, string tableName, int count)
        {
            return DBServiceHelper.GetSequenceInt64(ctx, tableName, count);
        }

        // /// <summary>
        // /// 通过SQL生产主键
        // /// </summary>
        // /// <param name="ctx">上下文</param>
        // /// <param name="tableName">表名</param>
        // /// <returns></returns>
        // public static long GetPkBySQL(Context ctx, string tableName)
        // {
        //     var sql = CommonArgs.SQLPreFix +
        //               $"insert into {tableName}(Column1) values(1);select Id from {tableName} ;delete from {tableName}";
        //     var ds = DBServiceHelper.ExecuteDataSet(ctx, sql);
        //     return EObject.TypeCast<int>(ds.Tables[0].Rows[0][0]);
        // }

        /// <summary>
        /// 获取连接字符串
        /// </summary>
        /// <param name="ctx">当前上下文</param>
        /// <returns></returns>
        // public static string GetConnectionString(Context ctx)
        // {
        //var ass = Assembly.LoadFile(AppDomain.CurrentDomain.BaseDirectory + "Bin/Kingdee.BOS.App.dll");
        //var tx = ass.DefinedTypes.Where((t) => { return t.Name == "KDatabaseFactory"; }).FirstOrDefault();
        //var mf = tx.GetMethod("GetConnectionString", BindingFlags.NonPublic | BindingFlags.Static, null,
        //    new Type[] { ctx.GetType() }, null);
        //var ConnectionString = mf.Invoke(null, new object[] { ctx }).ToString();
        //return ConnectionString;
        // }

        #endregion

        #region 单据相关

        /// <summary>
        /// 获取表单
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="meta"></param>
        /// <param name="formId"></param>
        /// <param name="pkid"></param>
        /// <returns></returns>
        public static IDynamicFormView OpenWebView(Context ctx, FormMetadata meta, string formId, object pkid = null)
        {
            BusinessInfo info = meta.BusinessInfo;
            Form form = info.GetForm();
            BillOpenParameter param = new BillOpenParameter(formID: formId, null);
            param.SetCustomParameter("formID",
                form.Id); //根据主键是否为空 置为新增或修改状态
            param.SetCustomParameter("status", !IsPrimaryValueEmpty(pkid) ? "View" : "Edit");
            param.SetCustomParameter("PlugIns", form.CreateFormPlugIns()); //插件实例模型
            //修改主业务组织无须用户确认
            param.SetCustomParameter("ShowConformDialogWhenChangeOrg", false);
            param.Context = ctx;
            param.FormMetaData = meta;
            param.LayoutId = param.FormMetaData.GetLayoutInfo().Id;
            // 设置状态为查看状态
            param.Status = OperationStatus.VIEW;

            param.PkValue = !IsPrimaryValueEmpty(pkid) ? pkid : null; //单据主键内码FID
            IResourceServiceProvider
                provider = form
                    .GetFormServiceProvider(); //普通的动态表单模式DynamicFormView
            IDynamicFormView billview = provider.GetService(typeof(IDynamicFormView)) as IDynamicFormView;
            //这里模拟为引入模式的WebView，否则遇到交互的时候会有问题，移动端目前无法直接交互
            Type type = Type.GetType("Kingdee.BOS.Web.Import.ImportBillView,Kingdee.BOS.Web");
            billview = (IDynamicFormView)Activator.CreateInstance(type);
            ((IBillViewService)billview).Initialize(param, provider); //初始化
            ((IBillViewService)billview).LoadData();
            //加载单据数据
            //如果是普通DynamicFormView时，LoadData的时候会加网控，要清除。
            //引入模式View不需要
            // (billview as IBillView).CommitNetworkCtrl();
            return billview;
        }

        private static bool IsPrimaryValueEmpty(object pk)
        {
            return pk == null || pk.ToString() == "0" || string.IsNullOrWhiteSpace(pk.ToString());
        }

        /// <summary>
        /// 单据操作
        /// </summary>
        /// <param name="Ctx">上下文</param>
        /// <param name="formId">要保存的单据标识</param>
        /// <param name="Option">保存参数</param>
        /// <param name="data">数据</param>
        /// <returns></returns>
        public static IOperationResult BillOperate(Context Ctx, string formId, SaveOption Option, DynamicObject data)
        {
            var metadataService = ServiceHelper.GetService<IMetaDataService>();
            var metadata = metadataService.Load(Ctx, formId) as FormMetadata;
            var objs = new DynamicObject[] { data };
            var saveService = ServiceHelper.GetService<ISaveService>();
            IOperationResult result;
            switch (Option)
            {
                case SaveOption.Submit:
                    {
                        var saveResult =
                            BusinessDataServiceHelper.Save(Ctx, metadata.BusinessInfo, objs, OperateOption.Create(), "Save");
                        if (!saveResult.IsSuccess)
                        {
                            var msg = string.Join("\n", (from p in saveResult.ValidationErrors select p.Message));
                            throw new Exception(msg);
                        }

                        long newbillId = 0;
                        foreach (var dataResult in saveResult.SuccessDataEnity)
                        {
                            if (dataResult["Id"] != null)
                            {
                                newbillId = long.Parse(dataResult["Id"].ToString());
                            }
                        }

                        var submitResult =
                            BusinessDataServiceHelper.Submit(Ctx, metadata.BusinessInfo, new object[] { newbillId }, "Submit");
                        if (!submitResult.IsSuccess)
                        {
                            var msg = string.Join("\n", (from p in submitResult.ValidationErrors select p.Message));
                            throw new Exception(msg);
                        }

                        result = submitResult;
                        break;
                    }
                case SaveOption.Audit:
                    {
                        var saveResult =
                            BusinessDataServiceHelper.Save(Ctx, metadata.BusinessInfo, objs, OperateOption.Create(), "Save");
                        if (!saveResult.IsSuccess)
                        {
                            var msg = string.Join("\n", (from p in saveResult.ValidationErrors select p.Message));
                            throw new Exception(msg);
                        }

                        long newbillId = 0;
                        foreach (var dataResult in saveResult.SuccessDataEnity)
                        {
                            if (dataResult["Id"] != null)
                            {
                                newbillId = long.Parse(dataResult["Id"].ToString());
                            }
                        }

                        var submitResult =
                            BusinessDataServiceHelper.Submit(Ctx, metadata.BusinessInfo, new object[] { newbillId }, "Submit");
                        if (!submitResult.IsSuccess)
                        {
                            var msg = string.Join("\n", (from p in submitResult.ValidationErrors select p.Message));
                            throw new Exception(msg);
                        }

                        var auditResult =
                            BusinessDataServiceHelper.Audit(Ctx, metadata.BusinessInfo, new object[] { newbillId }, null);
                        if (!auditResult.IsSuccess)
                        {
                            var msg = string.Join("\n", (from p in auditResult.ValidationErrors select p.Message));
                            throw new Exception(msg);
                        }

                        result = auditResult;
                        break;
                    }
                case SaveOption.Draft:
                    {
                        var drafResult =
                            BusinessDataServiceHelper.Draft(Ctx, metadata.BusinessInfo, objs, OperateOption.Create());
                        if (!drafResult.IsSuccess)
                        {
                            var msg = string.Join("\n", (from p in drafResult.ValidationErrors select p.Message));
                            throw new Exception(msg);
                        }

                        result = drafResult;
                        break;
                    }
                case SaveOption.Save:
                    {
                        var saveResult =
                            BusinessDataServiceHelper.Save(Ctx, metadata.BusinessInfo, objs, OperateOption.Create(), "Save");
                        if (!saveResult.IsSuccess)
                        {
                            var msg = string.Join("\n", (from p in saveResult.ValidationErrors select p.Message));
                            throw new Exception(msg);
                        }

                        result = saveResult;
                        break;
                    }

                default:
                    result = new OperationResult
                    {
                        IsSuccess = false
                    };
                    break;
            }

            result.MergeValidateErrors();
            return result;
        }

        /// <summary>
        /// 执行表单操作
        /// </summary>
        /// <param name="view"></param>
        /// <param name="operationEnum"></param>
        public static void InvokeFormOperation(IDynamicFormView view, FormOperationEnum operationEnum)
        {
            view.InvokeFormOperation(operationEnum);
        }

        /// <summary>
        /// 单据操作
        /// </summary>
        /// <param name="ctx">上下文</param>
        /// <param name="formId">单据唯一标志</param>
        /// <param name="Option">保存参数</param>
        /// <param name="FormViewService">数据填充</param>
        public static Dictionary<string, IOperationResult> BillOperate(Context ctx, string formId, SaveOption Option,
            Action<IDynamicFormViewService> FormViewService)
        {
            var result = new Dictionary<string, IOperationResult>();
            IBillView billView = CreateView(ctx, formId);
            ((IBillViewService)billView).LoadData();
            DynamicFormViewPlugInProxy eventProxy = billView.GetService<DynamicFormViewPlugInProxy>();
            //触发插件的OnLoad事件
            eventProxy.FireOnLoad();
            // 把billView转换为IDynamicFormViewService接口：
            // 调用IDynamicFormViewService.UpdateValue: 会执行字段的值更新事件
            // 调用 dynamicFormView.SetItemValueByNumber ：不会执行值更新事件，需要继续调用：
            // ((IDynamicFormView)dynamicFormView).InvokeFieldUpdateService(key, rowIndex);
            IDynamicFormViewService dynamicFormView = billView as IDynamicFormViewService;
            FormViewService?.Invoke(dynamicFormView);

            OperateOption saveOption = OperateOption.Create();
            Form form = billView.BillBusinessInfo.GetForm();
            if (form.FormIdDynamicProperty != null)
            {
                form.FormIdDynamicProperty.SetValue(billView.Model.DataObject, form.Id);
            }

            OperateOption OpOption = OperateOption.Create();
            OpOption.SetIgnoreWarning(false); //忽略警告
            OpOption.SetIgnoreInteractionFlag(false); //忽略交互信息

            result.Add("Final", null);
            if (Option == SaveOption.Submit)
            {
                var saveResult = BusinessDataServiceHelper.Save(ctx, billView.BillBusinessInfo,
                    billView.Model.DataObject, OperateOption.Create(), "Save");

                result.Add("Save", saveResult);
                result["Final"] = saveResult;

                if (saveResult.IsSuccess)
                {
                    long newbillId = 0;
                    foreach (var dataResult in saveResult.SuccessDataEnity)
                    {
                        if (dataResult["Id"] != null)
                        {
                            newbillId = long.Parse(dataResult["Id"].ToString());
                        }
                    }

                    var submitResult = BusinessDataServiceHelper.Submit(ctx, billView.BusinessInfo,
                        new object[] { newbillId }, "Submit");
                    result.Add("Submit", submitResult);
                    result["Final"] = submitResult;
                }
            }
            else if (Option == SaveOption.Audit)
            {
                var saveResult = BusinessDataServiceHelper.Save(ctx, billView.BusinessInfo, billView.Model.DataObject,
                    OperateOption.Create(), "Save");

                result.Add("Save", saveResult);
                result["Final"] = saveResult;

                if (saveResult.IsSuccess)
                {
                    long newbillId = 0;
                    foreach (var dataResult in saveResult.SuccessDataEnity)
                    {
                        if (dataResult["Id"] != null)
                        {
                            newbillId = long.Parse(dataResult["Id"].ToString());
                        }
                    }

                    var submitResult = BusinessDataServiceHelper.Submit(ctx, billView.BusinessInfo,
                        new object[] { newbillId }, "Submit");
                    result.Add("Submit", submitResult);
                    result["Final"] = submitResult;
                    if (submitResult.IsSuccess)
                    {
                        var auditResult = BusinessDataServiceHelper.Audit(ctx, billView.BusinessInfo,
                            new object[] { newbillId }, OpOption);
                        result.Add("Audit", auditResult);
                        result["Final"] = auditResult;
                    }
                }
            }
            else if (Option == SaveOption.Draft)
            {
                var drafResult = BusinessDataServiceHelper.Draft(ctx, billView.BusinessInfo, billView.Model.DataObject,
                    OperateOption.Create());
                result.Add("Draft", drafResult);
                result["Final"] = drafResult;
            }
            else if (Option == SaveOption.Save)
            {
                var saveResult = BusinessDataServiceHelper.Save(ctx, billView.BusinessInfo, billView.Model.DataObject,
                    OperateOption.Create(), "Save");
                result.Add("Save", saveResult);
                result["Final"] = saveResult;
            }
            else
            {
                result.Add("Save", new OperationResult { IsSuccess = false });
                result["Final"] = new OperationResult { IsSuccess = false };
            }

            foreach (var item in result)
            {
                item.Value.MergeValidateErrors();
            }

            return result;
        }

        /// <summary>
        /// 创建视图
        /// </summary>
        /// <param name="ctx">上下文</param>
        /// <param name="formId">单据唯一标志</param>
        /// <returns></returns>
        public static IBillView CreateView(Context ctx, string formId)
        {
            // 读取元数据
            FormMetadata meta = MetaDataServiceHelper.Load(ctx, formId) as FormMetadata;
            Form form = meta.BusinessInfo.GetForm();
            // 创建用于引入数据的单据view
            Type type = Type.GetType("Kingdee.BOS.Web.Import.ImportBillView,Kingdee.BOS.Web");
            var billView = (IDynamicFormViewService)Activator.CreateInstance(type);
            // 开始初始化billView：
            // 创建视图加载参数对象，指定各种参数，如FormId, 视图(LayoutId)等
            BillOpenParameter openParam = CreateOpenParameter(ctx, meta);
            // 动态领域模型服务提供类，通过此类，构建MVC实例
            var provider = form.GetFormServiceProvider();
            billView.Initialize(openParam, provider);
            return billView as IBillView;
        }

        /// <summary>
        /// 创建视图加载参数对象，指定各种初始化视图时，需要指定的属性
        /// </summary>
        /// <param name="meta">元数据</param>
        /// <returns>视图加载参数对象</returns>
        public static BillOpenParameter CreateOpenParameter(Context ctx, FormMetadata meta)
        {
            Form form = meta.BusinessInfo.GetForm();
            // 指定FormId, LayoutId
            BillOpenParameter openParam = new BillOpenParameter(form.Id, meta.GetLayoutInfo().Id)
            {
                // 数据库上下文
                Context = ctx,
                // 本单据模型使用的MVC框架
                ServiceName = form.FormServiceName,
                // 随机产生一个不重复的PageId，作为视图的标识
                PageId = Guid.NewGuid().ToString(),
                // 元数据
                FormMetaData = meta,
                // 界面状态：新增 (修改、查看)
                Status = OperationStatus.ADDNEW,
                // 单据主键：本案例演示新建物料，不需要设置主键
                PkValue = null,
                // 界面创建目的：普通无特殊目的 （为工作流、为下推、为复制等）
                CreateFrom = CreateFrom.Default,
                // 基础资料分组维度：基础资料允许添加多个分组字段，每个分组字段会有一个分组维度
                // 具体分组维度Id，请参阅 form.FormGroups 属性
                GroupId = "",
                // 基础资料分组：如果需要为新建的基础资料指定所在分组，请设置此属性
                ParentId = 0,
                // 单据类型
                DefaultBillTypeId = "",
                // 业务流程
                DefaultBusinessFlowId = ""
            };
            // 主业务组织改变时，不用弹出提示界面
            openParam.SetCustomParameter("ShowConfirmDialogWhenChangeOrg", false);
            // 插件
            List<AbstractDynamicFormPlugIn> plugs = form.CreateFormPlugIns();
            openParam.SetCustomParameter(FormConst.PlugIns, plugs);
            PreOpenFormEventArgs args = new PreOpenFormEventArgs(ctx, openParam);
            foreach (var plug in plugs)
            {
                // 触发插件PreOpenForm事件，供插件确认是否允许打开界面
                plug.PreOpenForm(args);
            }

            if (args.Cancel == true)
            {
                // 插件不允许打开界面
                // 本案例不理会插件的诉求，继续....
            }

            // 返回
            return openParam;
        }

        /// <summary>
        /// 加载指定的单据进行修改
        /// </summary>
        /// <param name="billView">单据视图</param>
        /// <param name="pkValue">主键值</param>
        public static void ModifyBill(IBillView billView, string pkValue)
        {
            billView.OpenParameter.Status = OperationStatus.EDIT;
            billView.OpenParameter.CreateFrom = CreateFrom.Default;
            billView.OpenParameter.PkValue = pkValue;
            billView.OpenParameter.DefaultBillTypeId = string.Empty;
            ((IDynamicFormViewService)billView).LoadData();
        }

        #endregion

        // #region 报表
        //
        // public static ReportHeader GenerateRptTitle(string Snode, int Lcid)
        // {
        //     var xml = new XmlDocument();
        //     try
        //     {
        //         var uri = new Uri(Path.GetDirectoryName(Assembly.GetCallingAssembly().CodeBase));
        //         var XmlUrl = Path.Combine(uri.LocalPath, CommonArgs.GetParameter("RptConfig"));
        //         xml.Load(XmlUrl);
        //     }
        //     catch (Exception)
        //     {
        //         return null;
        //     }
        //
        //     var header = new ReportHeader();
        //     var nlist = xml.SelectNodes("/AppSetting/" + Snode + "/item");
        //     if (nlist == null) return header;
        //     foreach (XmlNode node in nlist)
        //     {
        //         var title = "";
        //         try
        //         {
        //             if (node.Attributes != null) title = node.Attributes.GetNamedItem("title").Value;
        //         }
        //         catch (Exception)
        //         {
        //         }
        //
        //         var sqlname = "";
        //         try
        //         {
        //             if (node.Attributes != null) sqlname = node.Attributes.GetNamedItem("sqlname").Value;
        //         }
        //         catch (Exception)
        //         {
        //         }
        //
        //         var type = "";
        //         try
        //         {
        //             if (node.Attributes != null) type = node.Attributes.GetNamedItem("type").Value;
        //         }
        //         catch (Exception)
        //         {
        //         }
        //
        //         var visible = "";
        //         try
        //         {
        //             if (node.Attributes != null) visible = node.Attributes.GetNamedItem("visible").Value;
        //         }
        //         catch (Exception)
        //         {
        //         }
        //
        //         if (string.IsNullOrWhiteSpace(type))
        //         {
        //             type = "Sqlnvarchar";
        //         }
        //
        //         if (string.IsNullOrWhiteSpace(visible))
        //         {
        //             visible = "true";
        //         }
        //
        //         header.AddChild(sqlname, new LocaleValue(title, Lcid),
        //             (SqlStorageType) Enum.Parse(typeof(SqlStorageType), type), bool.Parse(visible));
        //     }
        //
        //     return header;
        // }
        //
        // #endregion


        #region 安全相关

        /// <summary>
        /// 获取32位长度的Md5摘要
        /// </summary>
        /// <param name="inputStr"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string GetMD5_32(string inputStr, Encoding encoding = null)
        {
            RefEncoding(ref encoding);
            byte[] data = GetMD5(inputStr, encoding);
            var tmp = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
                tmp.Append(data[i].ToString("x2"));


            return tmp.ToString();
        }

        /// <summary>
        /// 获取MD5值
        /// </summary>
        /// <param name="inputStr"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        static byte[] GetMD5(string inputStr, Encoding encoding)
        {
            using (MD5 md5Hash = MD5.Create())
                return md5Hash.ComputeHash(encoding.GetBytes(inputStr));
        }

        /// <summary>
        /// RefEncoding
        /// </summary>
        /// <param name="encoding">encoding</param>
        static void RefEncoding(ref Encoding encoding)
        {
            if (encoding == null)
            {
                encoding = Encoding.Default;
            }
        }

        #endregion
    }
}
