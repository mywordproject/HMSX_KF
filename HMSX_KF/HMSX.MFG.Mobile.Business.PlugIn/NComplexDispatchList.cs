using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Util;
using Kingdee.BOS.Resource;
using Kingdee.BOS.Mobile;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Mobile.PlugIn.ControlModel;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.Complex;
using Kingdee.K3.Core.MFG.SFC;
using Kingdee.K3.MFG.ServiceHelper.SFS;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Metadata.ConvertElement;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Core.Metadata.ConvertElement.ServiceArgs;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Core.DynamicForm.Operation;
using Kingdee.K3.MFG.Mobile.ServiceHelper;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS;
using System.Reflection;
using Kingdee.K3.MFG.ServiceHelper.SFC;
using Kingdee.K3.MFG.SFC.Common.Core.EnumConst.Mobile;
using System.Data;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Text;
using Kingdee.BOS.App.Data;

namespace HMSX.MFG.Mobile.Business.PlugIn
{
    [Description("复杂工序(派工工序报工列表)-表单插件")]
    [Kingdee.BOS.Util.HotUpdate]
    public class NComplexDispatchReportList : ComplexDispatchReportList
    {

        protected Dictionary<int, int> SelectedDataIndex = new Dictionary<int, int>();
        public override void EntityRowDoubleClick(EntityRowClickEventArgs e)
        {

            this.UpdateSelectedRowsOnCurPage("FMobileListViewEntity");
            List<int> list = new List<int>();
            if (this.CurrPageNumber > 0)
            {
                int num = 0;
                while (num < this.RowCountPerPage && num + (this.CurrPageNumber - 1) * this.RowCountPerPage < this.DicRowIndexRelation.Count)
                {
                    int key = this.DicRowIndexRelation[num + (this.CurrPageNumber - 1) * this.RowCountPerPage];
                    int num2;
                    if (this.SelectedDataIndex.TryGetValue(key, out num2) && num2 == 1)
                    {
                        list.Add(num);
                    }
                    num++;
                }
            }
            this.View.GetControl<MobileListViewControl>("FMobileListViewEntity").SetSelectRows(list.ToArray());
            this.View.UpdateView("FMobileListViewEntity");

        }
        private void UpdateSelectedRowsOnCurPage(string entityKey)
        {
            if (this.CurrPageNumber >= 1)
            {
                int[] selectedRows = base.View.GetControl<MobileListViewControl>(entityKey).GetSelectedRows();
                IEnumerable<KeyValuePair<int, int>> enumerable = from w in this.DicRowIndexRelation
                                                                 where w.Key >= (this.CurrPageNumber - 1) * this.RowCountPerPage && w.Key < this.CurrPageNumber * this.RowCountPerPage
                                                                 select w;
                foreach (KeyValuePair<int, int> keyValuePair in enumerable)
                {
                    if (selectedRows != null && selectedRows.Contains(keyValuePair.Key - (this.CurrPageNumber - 1) * this.RowCountPerPage))
                    {
                        this.SelectedDataIndex[keyValuePair.Value] = 1;
                    }
                    else
                    {
                        this.SelectedDataIndex[keyValuePair.Value] = 0;
                    }
                }

            }
        }
        private List<Dictionary<string, object>> GetSeletedDetails()
        {
            List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
            IEnumerable<KeyValuePair<int, int>> source = from o in this.SelectedDataIndex
                                                         where o.Value == 1
                                                         select o;
            if (source.Any<KeyValuePair<int, int>>())
            {
                list.AddRange(from o in source
                              select o.Key into key
                              select this.DicTableData[key] into dicRowData
                              select dicRowData);
            }
            return list;

        }

        public override void BeforeUpdateValue(BeforeUpdateValueEventArgs e)
        {
            base.BeforeUpdateValue(e);
            if (e.Key.ToUpper() == "FTEXT_SCANCODE")
            {
                this.SelectedDataIndex = new Dictionary<int, int>();
            }
        }

        public override void ButtonClick(ButtonClickEventArgs e)
        {
            base.ButtonClick(e);
            string a = e.Key.ToUpper();
            switch (a)
            {
                //批量报工
                case "FBUTTON_BATCHCONFIRM":
                    this.ReportOper();
                    break;
                //全选
                case "FBUTTON_SELECTALL":
                    this.selectAll(true, "FMobileListViewEntity");
                    break;
                case "FBUTTON_CONFIRME":
                    this.ReportOper(false);
                    break;
                case "FBUTTON_UNSTART":
                    UnStart();
                    break;
            }

        }

        private void ReportOper(bool isNewCopy)
        {
            ExportLogInfo exportLogInfo = new ExportLogInfo
            {
                Code = this.runId,
                StartTime = DateTime.Now
            };
            List<Dictionary<string, object>> listRowData = this.GetSeletedDetails();
            Dictionary<string, object> currentRowData = listRowData[0];
            if (listRowData.Count < 1)
            {
                base.View.ShowMessage(ResManager.LoadKDString("当前未选中行！", "015747000028226", SubSystemType.MFG, new object[0]), MessageBoxType.Notice);
            }
            if (listRowData.Count > 1)
            {
                base.View.ShowMessage(ResManager.LoadKDString("当前选中多行，只允许选择一行数据！", "015747000028226", SubSystemType.MFG, new object[0]), MessageBoxType.Notice);
            }
            else if (this.ValidAndHandleUserBillStatus(currentRowData))
            {
                string userRecentData = SFSDiscreteServiceHelper.GetUserRecentData(base.Context, "YMCS_0031_SYS", "SFC_MobileComplexDispReportBillEdit");
                ListSelectedRow listSelectedRow = new ListSelectedRow(Convert.ToString(currentRowData["FOptPlanId"]), Convert.ToString(currentRowData["FOptPlanOptId"]), Convert.ToInt32(currentRowData["FOptPlanOptSeq"]), "SFC_OperationPlanning")
                {
                    EntryEntityKey = "FSubEntity"
                };
                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                dictionary["FSubEntity"] = Convert.ToString(currentRowData["FOptPlanOptId"]);
                listSelectedRow.FieldValues = dictionary;
                ConvertRuleElement rule = ConvertServiceHelper.GetConvertRules(base.Context, "SFC_OperationPlanning", "SFC_OperationReport").Find((ConvertRuleElement f) => f.IsDefault);
                PushArgs serviceArgs = new PushArgs(rule, new ListSelectedRow[]
                {
                    listSelectedRow
                });
                OperateOption operateOption = OperateOption.Create();
                operateOption.SetVariableValue("IsMobileInvoke", true);
                operateOption.SetVariableValue("MobileBizType", "Dispatch");
                if (isNewCopy)
                {
                    operateOption.SetVariableValue("newCoBy", true);
                }
                else
                {
                    operateOption.SetVariableValue("newCoBy", false);
                }
                operateOption.SetVariableValue("DispatchEntryId", Convert.ToInt64(currentRowData["EntryPkId"]));
                operateOption.SetVariableValue("DIsCanReport", SFSDiscreteServiceHelper.GetUserRecentData(base.Context, "YMCS_0024_SYS", base.View.BusinessInfo.GetForm().Id));
                try
                {
                    ConvertOperationResult convertOperationResult = MobileCommonServiceHelper.Push(base.Context, serviceArgs, operateOption, false);
                    if (convertOperationResult.IsSuccess)
                    {
                        DynamicObject dataEntity = convertOperationResult.TargetDataEntities.FirstOrDefault<ExtendedDataEntity>().DataEntity;
                        dataEntity["BillGenType"] = "C";
                        MobileShowParameter mobileShowParameter = new MobileShowParameter();
                        if (userRecentData == "B" && isNewCopy)
                        {
                            mobileShowParameter.FormId = "SFC_MobileComplexBatchNewCoBy";
                            mobileShowParameter.CustomComplexParams["IsDispatchReport"] = true;
                            mobileShowParameter.CustomComplexParams["OptRpt"] = dataEntity;
                            mobileShowParameter.CustomComplexParams["OptEntry"] = dataEntity["OptRptEntry"];
                            mobileShowParameter.CustomComplexParams["List"] = true;
                        }
                        else
                        {
                            mobileShowParameter.FormId = "SFC_MobileComplexDispReportBillEdit";
                        }
                        mobileShowParameter.ParentPageId = base.View.PageId;
                        mobileShowParameter.CustomComplexParams["DataPacket"] = dataEntity;
                        mobileShowParameter.CustomComplexParams["FWorkQty"] = currentRowData["FWorkQty"];
                        mobileShowParameter.CustomComplexParams["FFinishSelQty"] = currentRowData["FFinishSelQty"];
                        mobileShowParameter.CustomComplexParams["FOperPlanNo"] = currentRowData["FOperPlanNo"];
                        mobileShowParameter.CustomComplexParams["InspectStatus"] = currentRowData["InspectStatus"];
                        mobileShowParameter.CustomComplexParams["PkId"] = currentRowData["TransPkId"];
                        mobileShowParameter.CustomComplexParams["EntryPkId"] = currentRowData["TransEntryPkId"];
                        mobileShowParameter.CustomComplexParams["SeqId"] = currentRowData["FOpSeqId"];
                        mobileShowParameter.CustomComplexParams["EntryRowIndex"] = currentRowData["TransEntryRowIndex"];
                        mobileShowParameter.CustomComplexParams["FIsReturnList"] = SFSDiscreteServiceHelper.GetUserRecentData(base.Context, "YMCS_0002_SYS", this.BillModelFormId);
                        mobileShowParameter.CustomComplexParams["isNewCopy"] = isNewCopy;
                        mobileShowParameter.CustomComplexParams["IsUseRealTimeCalc"] = currentRowData["IsUseRealTimeCalc"];
                        base.View.ShowForm(mobileShowParameter, delegate (FormResult returnValue)
                        {
                            base.View.GetControl<MobileListViewControl>("FMobileListViewEntity").SetSelectRows(new int[0]);
                            base.View.OpenParameter.SetCustomParameter("ListCurrPage", this.CurrPageNumber);
                            this.ReloadListData(null, true);
                        });
                    }
                    else
                    {
                        StringBuilder stringBuilder = new StringBuilder();
                        stringBuilder.AppendLine(ResManager.LoadKDString("报工失败！", "015747000015462", SubSystemType.MFG, new object[0]));
                        if (convertOperationResult.ValidationErrors.Count > 0)
                        {
                            stringBuilder.AppendLine();
                            foreach (ValidationErrorInfo validationErrorInfo in convertOperationResult.ValidationErrors)
                            {
                                stringBuilder.AppendLine(validationErrorInfo.Message);
                            }
                        }
                        base.View.ShowStatusBarInfo(stringBuilder.ToString());
                    }
                }
                catch (KDBusinessException ex)
                {
                    base.View.ShowStatusBarInfo(new StringBuilder().AppendLine(ResManager.LoadKDString("报工失败！", "015747000015462", SubSystemType.MFG, new object[0])).AppendLine().AppendLine(ex.Message).ToString());
                }
                object paramter = SystemParameterServiceHelper.GetParamter(base.Context, 0L, 0L, "SFC_SystemParam", "ISExportLog", 0L);
                if (paramter != null && Convert.ToBoolean(paramter.ToString()))
                {
                    exportLogInfo.UserId = base.Context.UserId.ToString();
                    exportLogInfo.MethodName = MethodBase.GetCurrentMethod().DeclaringType.FullName + '.' + MethodBase.GetCurrentMethod().Name;
                    exportLogInfo.Detail = ResManager.LoadKDString("派工工序报工点击报工按钮", "015747000021954", SubSystemType.MFG, new object[0]);
                    exportLogInfo.EndTime = DateTime.Now;
                    ExportLogServiceHelper.WriteLog(base.Context, exportLogInfo);
                }
                // SelectedDataIndex = null;
            }

        }

        /// <summary>
        /// 打开汇报编辑界面
        /// </summary>
        private void ReportOper()
        {
            List<Dictionary<string, object>> listRowData = this.GetSeletedDetails();
            int sumCount = listRowData.GroupBy(p => p["FOperPlanNo"]).Count();
            if (listRowData.Count < 1)
            {
                base.View.ShowMessage(ResManager.LoadKDString("当前未选中行！", "015747000028226", SubSystemType.MFG, new object[0]), MessageBoxType.Notice);
            }
            else if (listRowData.Count > 0)
            {
                if (sumCount > 1)
                {
                    base.View.ShowMessage(ResManager.LoadKDString("不为同一个工序计划，不能一起批量报工！", "015747000028226", SubSystemType.MFG, new object[0]), MessageBoxType.Notice);
                    return;
                }
                MobileShowParameter mobileShowParameter = new MobileShowParameter();
                mobileShowParameter.FormId = "kb8cdc04eee534d139f6d232e8c7b270e";
                mobileShowParameter.ParentPageId = this.View.PageId;
                mobileShowParameter.CustomComplexParams["DataPacket"] = listRowData;//生成的目标单据
                this.View.ShowForm(mobileShowParameter, delegate (FormResult returnValue)
                {
                    this.SelectedDataIndex = new Dictionary<int, int>();
                    base.View.GetControl<MobileListViewControl>("FMobileListViewEntity").SetSelectRows(new int[0]);
                    base.View.OpenParameter.SetCustomParameter("ListCurrPage", this.CurrPageNumber);
                    this.ReloadListData(null, true);
                });
            }
        }

        private bool ValidAndHandleUserBillStatus(Dictionary<string, object> dicRowData)
        {
            bool result;
            if (!Convert.ToBoolean(dicRowData["IsUseRealTimeCalc"]))
            {
                result = true;
            }
            else
            {
                bool flag = true;
                long pkId = Convert.ToInt64(dicRowData["EntryPkId"]);
                MobileEnums.BillStatus billStatus = (MobileEnums.BillStatus)Convert.ToInt32(SFSDiscreteServiceHelper.GetUserBillStatus(base.Context, true, pkId));
                if (billStatus == MobileEnums.BillStatus.Empty)
                {
                    base.View.ShowMessage(ResManager.LoadKDString("请先对该行进行“开始”操作，再进行报工！", "015747000018214", SubSystemType.MFG, new object[0]), MessageBoxType.Notice);
                    flag = false;
                }
                else if (billStatus == MobileEnums.BillStatus.Start)
                {
                    string optPlanBillNo = Convert.ToString(dicRowData["FOptPlanNo"]);
                    string seqNumber = Convert.ToString(dicRowData["FSeqNumber"]);
                    long operNumber = Convert.ToInt64(dicRowData["FOperNumber"]);
                    SFSDiscreteServiceHelper.PauseBillStatus(base.Context, true, pkId, optPlanBillNo, seqNumber, operNumber);
                }
                result = flag;
            }
            return result;
        }

        private void selectAll(bool selectAll, string entityKey)
        {

            // this.SelectedDataIndex.Clear();
            foreach (int key in this.DicRowIndexRelation.Values)
            {
                // int key;
                this.SelectedDataIndex[key] = (selectAll ? 1 : 0);
            }
            List<int> list = new List<int>();
            if (this.CurrPageNumber > 0)
            {
                int num = 0;
                while (num < this.RowCountPerPage && num + (this.CurrPageNumber - 1) * this.RowCountPerPage < this.DicRowIndexRelation.Count)
                {
                    int key = this.DicRowIndexRelation[num + (this.CurrPageNumber - 1) * this.RowCountPerPage];
                    int num2;
                    if (this.SelectedDataIndex.TryGetValue(key, out num2) && num2 == 1)
                    {
                        list.Add(num);
                    }
                    num++;
                }
            }
            IEnumerable<KeyValuePair<int, int>> enumerable = from w in this.SelectedDataIndex
                                                             where w.Value == 1
                                                             select w;
            if (enumerable == null || enumerable.Count<KeyValuePair<int, int>>() != 0)
            {
                base.View.GetControl<MobileListViewControl>(entityKey).SetSelectRows(list.ToArray());
            }
            else
            {
                base.View.GetControl<MobileListViewControl>(entityKey).SetSelectRows(new int[0]);
            }
        }

        protected override void SetHighLight()
        {
            // this.UpdateSelectedRowsOnCurPage("FMobileListViewEntity");
            List<int> list = new List<int>();
            if (this.CurrPageNumber > 0)
            {
                int num = 0;
                while (num < this.RowCountPerPage && num + (this.CurrPageNumber - 1) * this.RowCountPerPage < this.DicRowIndexRelation.Count)
                {
                    int key = this.DicRowIndexRelation[num + (this.CurrPageNumber - 1) * this.RowCountPerPage];
                    int num2;
                    if (this.SelectedDataIndex.TryGetValue(key, out num2) && num2 == 1)
                    {
                        list.Add(num);
                    }
                    num++;
                }
            }
            this.View.GetControl<MobileListViewControl>("FMobileListViewEntity").SetSelectRows(list.ToArray());
            this.View.UpdateView("FMobileListViewEntity");
        }

        private void UnStart()
        {
            Dictionary<string, object> currentRowData = base.GetCurrentRowData();
            if (currentRowData == null)
            {
                base.View.ShowMessage(ResManager.LoadKDString("当前未选中行！", "015747000028226", SubSystemType.MFG, new object[0]), MessageBoxType.Notice);
            }
            else
            {
                List<long> list = new List<long>();
                long groupId = Convert.ToInt64(currentRowData["FGroupId"]);
                if (groupId == 0L)
                {
                    list.Add(Convert.ToInt64(currentRowData["EntryPkId"]));
                }
                else
                {
                    ExtendedDataEntity[] dispatchDataByGroupId = SFSDiscreteServiceHelper.GetDispatchDataByGroupId(base.Context, groupId);
                    ExtendedDataEntity[] array = dispatchDataByGroupId;
                    for (int i = 0; i < array.Length; i++)
                    {
                        ExtendedDataEntity extendedDataEntity = array[i];
                        DynamicObject dataEntity = extendedDataEntity.DataEntity;
                        DynamicObjectCollection source = dataEntity["DispatchDetailEntry"] as DynamicObjectCollection;
                        list.AddRange(
                            from col in source
                            where Convert.ToInt64(col["GroupId"]) == groupId
                            select Convert.ToInt64(col["Id"]));
                    }
                }
                string strSql = "select 1 from T_SFC_DITask PE inner join (select /*+ cardinality(b " + list.Distinct<long>().Count<long>() + ")*/ FID from table(fn_StrSplit(@DispEntryId,',',1)) b) EntryId ON EntryId.FID=PE.FDISPATCHDETAILENTRYID";
                List<SqlParam> list2 = new List<SqlParam>();
                list2.Add(new SqlParam("@DispEntryId", KDDbType.udt_inttable, list.Distinct<long>().ToArray<long>()));
                int num = DBServiceHelper.ExecuteScalar<int>(base.Context, strSql, 0, list2.ToArray());
                if (num != 0)
                {
                    base.View.ShowMessage(ResManager.LoadKDString("一出多派工已经联网生产，不能反开工！", "015747000022963", SubSystemType.MFG, new object[0]), MessageBoxType.Notice);
                }
                else
                {
                    string strSQL = "SELECT FBASEFINISHSELQTY,FBARCODE FROM T_SFC_DISPATCHDETAILENTRY PE inner join (select /*+ cardinality(b " + list.Distinct<long>().Count<long>() + ")*/ FID from table(fn_StrSplit(@DispEntryId,',',1)) b) EntryId ON EntryId.FID=PE.FENTRYID";
                    List<SqlParam> list3 = new List<SqlParam>();
                    list3.Add(new SqlParam("@DispEntryId", KDDbType.udt_inttable, list.Distinct<long>().ToArray<long>()));
                    using (IDataReader dataReader = DBServiceHelper.ExecuteReader(base.Context, strSQL, list3))
                    {
                        while (dataReader.Read())
                        {
                            if (Convert.ToInt64(dataReader["FBASEFINISHSELQTY"]) > 0L)
                            {
                                base.View.ShowMessage(ResManager.LoadKDString("只有未汇报的派工明细分录才能反开工！", "015747000039991", SubSystemType.MFG, new object[0]), MessageBoxType.Notice);
                                return;
                            }
                            //try
                            //{
                            string fsjysql1 = $@"select FNUMBER,FNAME from  T_BAS_PREBDFIVE a
                                           inner join T_BAS_PREBDFIVE_L b on a.FID=b.FID
                                           WHERE FNAME='反审校验'";
                            var fsjy1 = DBUtils.ExecuteDynamicObject(Context, fsjysql1);
                            if (fsjy1.Count > 0)
                            {
                                Encoding encoding1 = Encoding.UTF8;
                                HttpWebRequest request1 = (HttpWebRequest)WebRequest.Create(fsjy1[0]["FNUMBER"].ToString());
                                request1.Method = "POST";
                                request1.ContentType = "application/json; charset=UTF-8";
                                request1.Headers["Accept-Encoding"] = "gzip, deflate";
                                request1.AutomaticDecompression = DecompressionMethods.GZip;
                                JObject jsonRoot1 = new JObject();
                                jsonRoot1.Add("fbillno", dataReader["FBARCODE"].ToString());
                                jsonRoot1.Add("fbilltype", "派工明细");
                                byte[] buffer1 = encoding1.GetBytes(jsonRoot1.ToString());
                                request1.ContentLength = buffer1.Length;
                                request1.GetRequestStream().Write(buffer1, 0, buffer1.Length);
                                HttpWebResponse response1 = (HttpWebResponse)request1.GetResponse();
                                using (StreamReader reader = new StreamReader(response1.GetResponseStream(), Encoding.UTF8))
                                {
                                    WMSclass jsonDatas = JsonConvert.DeserializeObject<WMSclass>(reader.ReadToEnd());
                                    if (jsonDatas.Code != "0")
                                    {
                                        throw new KDBusinessException("", jsonDatas.Message);
                                    }
                                }
                            }
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
                                jsonRoot.Add("fbillno", dataReader["FBARCODE"].ToString());
                                jsonRoot.Add("fbilltype", "派工明细");
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
                            string upsql = $@"/*dialect*/update T_SFC_DISPATCHDETAILENTRY set F_260_LY = '' where FBARCODE = '{dataReader["FBARCODE"]}'";
                            DBUtils.Execute(Context, upsql);
                            // }
                            // catch
                            // {
                            //     throw new KDBusinessException("", "访问WMS接口异常");
                            // }
                        }
                    }
                }
            }
            
        }
    }
}
