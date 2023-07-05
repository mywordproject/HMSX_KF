using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Kingdee.BOS;
using Kingdee.BOS.BusinessEntity.BillTrack;
using Kingdee.BOS.BusinessEntity.BusinessFlow;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.BusinessFlow.ServiceArgs;
using Kingdee.BOS.Core.CommonFilter;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.List.PlugIn.Args;
using Kingdee.BOS.Core.Log;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.ConvertElement;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Core.Metadata.PreInsertData;
using Kingdee.BOS.Core.NetworkCtrl;
using Kingdee.BOS.Core.Permission;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS.Log;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Resource;
using Kingdee.BOS.Serialization;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.VerificationHelper;
using Kingdee.K3.Core.MFG.EntityHelper;
using Kingdee.K3.Core.MFG.EnumConst;
using Kingdee.K3.Core.MFG.Utils;
using Kingdee.K3.FIN.Core;
using Kingdee.K3.MFG.ServiceHelper;
using Kingdee.K3.MFG.ServiceHelper.SFC;
using Kingdee.K3.MFG.SFC.Business.PlugIn.Utils;
using Kingdee.K3.SCM.Core;

namespace HMSX.Second.Plugin.study
{
    [Description("车间调度工作台")]
    [Kingdee.BOS.Util.HotUpdate]
    public class CJDDGZTBillPlugin : AbstractDynamicFormPlugIn
    {
        private const string TreeViewKey = "FTreeViewPlan";
        private System.Collections.Generic.List<string> lastSelOPlan = new System.Collections.Generic.List<string>();
        protected FilterParameter filterParam = new FilterParameter();
        private FormMetadata _oplanMeta;
        private string currNodeID = string.Empty;
        private long parentNodeID;
        private DynamicObject resourceObject;
        private System.Collections.Generic.List<TreeNode> nodeLst = new System.Collections.Generic.List<TreeNode>();
        private bool isFirstLoad = true;
        private TreeView curTreeView
        {
            get
            {
                return this.View.GetControl<TreeView>("FTreeViewPlan");
            }
        }
        protected FormMetadata OPlanMetaData
        {
            get
            {
                if (this._oplanMeta == null)
                {
                    this._oplanMeta = (MetaDataServiceHelper.Load(this.View.Context, "SFC_OperationPlanning", true) as FormMetadata);
                }
                return this._oplanMeta;
            }
        }
        public System.Collections.Generic.List<NetworkCtrlResult> NetworkCtrlResults
        {
            get;
            set;
        }
        public override void OnInitialize(InitializeEventArgs e)
        {
            base.OnInitialize(e);
            this.ShowFilter(true);
        }
        public override void BarItemClick(BarItemClickEventArgs e)
        {
            base.BarItemClick(e);
            string barItemKey;
            switch (barItemKey = e.BarItemKey)
            {
                case "tbBtnToPlanConfirm":
                case "tbBtnToRelease":
                case "tbBtnToStart":
                case "tbBtnToFinish":
                case "tbBtnToClose":
                case "tbBtnUndoToPlan":
                case "tbBtnUndoToPlanConfirm":
                case "tbBtnUndoToRelease":
                case "tbBtnUndoToStart":
                case "tbBtnUndoToFinish":
                    LicenseVerifier.CheckViewOnlyOperation(base.Context, ResManager.LoadKDString("执行状态", "015165000014153", SubSystemType.MFG, new object[0]));
                    this.SetStatus(e);
                    return;
                case "tbClose":
                    this.View.Close();
                    return;
                case "tbDispatchDetail":
                    LicenseVerifier.CheckViewOnlyOperation(base.Context, ResManager.LoadKDString("终端派工明细", "015165000014155", SubSystemType.MFG, new object[0]));
                    this.CallDispatchDetail();
                    break;

                    return;
            }
        }
        private void CallDispatchDetail()
        {
            System.Collections.Generic.List<DynamicObject> selectedRows = this.GetSelectedRows();
            if (selectedRows.Count != 1)
            {
                this.View.ShowErrMessage(ResManager.LoadKDString("请选择一条工序记录！", "015165000009505", SubSystemType.MFG, new object[0]), Consts.ERROR_TITLE, MessageBoxType.Notice);
                return;
            }
            object arg = selectedRows[0]["OperId"];
            string strSQL = string.Format("SELECT TOP 1 FID FROM T_SFC_DispatchDetail WHERE FOPERID = {0}", arg);
            long num = 0L;
            using (IDataReader dataReader = DBServiceHelper.ExecuteReader(base.Context, strSQL))
            {
                while (dataReader.Read())
                {
                    num = System.Convert.ToInt64(dataReader["FID"]);
                }
            }
            if (num > 0L)
            {
                BillShowParameter param = new BillShowParameter
                {
                    FormId = "SFC_DispatchDetail",
                    ParentPageId = this.View.PageId,
                    MultiSelect = false,
                    Status = OperationStatus.VIEW,
                    PKey = num.ToString()
                };
                this.View.ShowForm(param);
                return;
            }
            this.View.ShowMessage(ResManager.LoadKDString("当前工序不存在派工明细！", "015165000015808", SubSystemType.MFG, new object[0]), MessageBoxType.Notice);
        }
        public override void BeforeDoOperation(BeforeDoOperationEventArgs e)
        {
            base.BeforeDoOperation(e);
            string operation = e.Operation.FormOperation.Operation;
            if (!operation.Equals("Filter") && !operation.Equals("Refresh"))
            {
                if (!this.CheckSelectDataId())
                {
                    e.Cancel = true;
                    return;
                }
                if (!this.CheckPermission(e.Operation.FormOperation.Operation))
                {
                    e.Cancel = true;
                    return;
                }
            }
            string operation2;

            if ((operation2 = e.Operation.FormOperation.Operation) != null)
            {
                switch (operation2)
                {
                    case "Filter":
                        this.ShowFilter(false);
                        return;
                    case "Edit":
                        this.Edit();
                        return;
                    case "Delete":
                        bool flag = this.ValidateDocumentStatus(e);
                        if (flag)
                        {
                            this.parentNodeID =Convert.ToInt64( this.Model.GetValue("FParentId"));
                            this.SetBillStatus(this.OPlanMetaData.BusinessInfo, e.Operation.FormOperation.Operation);
                            return;
                        }
                        return;
                    case "Submit":
                        return;
                    case "CancelAssign":
                        return;
                    case "Audit":
                        return;
                    case "UnAudit":
                        this.SetBillStatus(this.OPlanMetaData.BusinessInfo, e.Operation.FormOperation.Operation);
                        return;
                    case "TrackDown":
                        this.TrckDown();
                        return;
                    case "TrackDown1":
                        this.TrckDown1();
                        return;
                    case "SimpleDispatching":
                        LicenseVerifier.CheckViewOnlyOperation(base.Context, ResManager.LoadKDString("简易派工", "015376000004737", SubSystemType.MFG, new object[0]));
                        this.SimpleDispatching();
                        return;
                    case "TransQtyAdjust":
                        this.TransQtyAdjust();
                        return;
                    case "LogQuery":
                        this.LogQuery();
                        return;
                    case "OperationScheduling":
                        LicenseVerifier.CheckViewOnlyOperation(base.Context, ResManager.LoadKDString("工序排程", "015165000004601", SubSystemType.MFG, new object[0]));
                        try
                        {
                            this.ShowProgressBar(false);
                            return;
                        }
                        catch (System.Exception ex)
                        {
                            throw new KDException(ResManager.LoadKDString("工序排程", "015165000004601", SubSystemType.MFG, new object[0]), ex.Message);
                        }
                        break;
                    case "SpecifiedScheduling":
                        break;
                    default:
                        return;
                }

                LicenseVerifier.CheckViewOnlyOperation(base.Context, ResManager.LoadKDString("指定工序排程", "015165000011034", SubSystemType.MFG, new object[0]));
                try
                {
                    this.ShowProgressBar(true);
                }
                catch (System.Exception ex2)
                {
                    throw new KDException(ResManager.LoadKDString("工序排程", "015165000004601", SubSystemType.MFG, new object[0]), ex2.Message);
                }
            }
        }
        private void SplitFirstToLast(AfterDoOperationEventArgs e)
        {
            if (e.OperationResult.IsSuccess)
            {
                if (this.CheckSelectDataId() && this.ValidateSplitFirstToLast())
                {
                    this.ShowForm("SFC_SplitFirstToLast", true, "Split", "", false, OperationStatus.EDIT);
                    return;
                }
            }
            else
            {
                e.OperationResult.IsShowMessage = true;
            }
        }
        private void SplitMidToLast(AfterDoOperationEventArgs e)
        {
            if (e.OperationResult.IsSuccess)
            {
                if (this.CheckSelectDataId() && this.ValidateSplitMidToLast())
                {
                    this.ShowForm("SFC_SplitMidToLast", true, "Split", "", false, OperationStatus.EDIT);
                    return;
                }
            }
            else
            {
                e.OperationResult.IsShowMessage = true;
            }
        }
        private void SplitSelect(AfterDoOperationEventArgs e)
        {
            if (e.OperationResult.IsSuccess)
            {
                if (this.CheckSelectDataId() && this.ValidateSplitSelect())
                {
                    this.ShowForm("SFC_SplitSelect", true, "Split", "", false, OperationStatus.EDIT);
                    return;
                }
            }
            else
            {
                e.OperationResult.IsShowMessage = true;
            }
        }
        private void SimpleDispatching()
        {
            if (this.CheckSelectDataId() && this.ValidateDispatching())
            {
                this.ShowForm("SFC_SimpleDispatching", true, "SimpleDispatching", "", true, OperationStatus.EDIT);
            }
        }
        private void TransQtyAdjust()
        {
            if (this.CheckSelectDataId() && this.ValidateTransQtyAdjust())
            {
                this.ShowForm("SFC_TransQtyAdjust", true, "TransQtyAdjust", "", false, OperationStatus.EDIT);
            }
        }
        private void LogQuery()
        {
            if (this.CheckSelectDataId())
            {
                string text = this.ValidateLogQuery();
                if (!text.Equals("false"))
                {
                    this.ShowForm("SFC_OperationLog", false, "", text, false, OperationStatus.VIEW);
                }
            }
        }
        private bool ValidateSplitFirstToLast()
        {
            System.Collections.Generic.List<DynamicObject> selectedRows = this.GetSelectedRows();
            System.Collections.Generic.List<string> list = (
                from a in selectedRows
                select a["SeqType"].ToString()).ToList<string>();
            if (list.Distinct<string>().Count<string>() > 1 && (list.Contains("A") || list.Contains("R")))
            {
                this.View.ShowErrMessage(ResManager.LoadKDString("首序拆卡到底只支持主干序列和并行序列同时拆卡！", "015376030034190", SubSystemType.MFG, new object[0]), "", MessageBoxType.Notice);
                return false;
            }
            if (selectedRows.Count <= 0)
            {
                this.View.ShowErrMessage(ResManager.LoadKDString("请选择至少一道工序！", "015376000004766", SubSystemType.MFG, new object[0]), "", MessageBoxType.Notice);
                return false;
            }
            string dynamicObjectItemValue = selectedRows.First<DynamicObject>()["SeqType"].ToString();
            string dynamicObjectItemValue2 = selectedRows.First<DynamicObject>()["SeqId"].ToString();
            Tuple<bool, bool> seqInfoBySeqId = this.GetSeqInfoBySeqId(System.Convert.ToInt64(dynamicObjectItemValue2));
            if (dynamicObjectItemValue.Equals("A") && (!seqInfoBySeqId.Item1 || !seqInfoBySeqId.Item2))
            {
                this.View.ShowErrMessage(ResManager.LoadKDString("替代序列中包含首序和入库点时才可以首序到底拆卡", "015165000015887", SubSystemType.MFG, new object[0]), "", MessageBoxType.Notice);
                return false;
            }
            return this.ValidateSplitCommon(selectedRows, "F");
        }
        private bool ValidateSplitMidToLast()
        {
            System.Collections.Generic.List<DynamicObject> selectedRows = this.GetSelectedRows();
            if (selectedRows.Count != 1)
            {
                this.View.ShowErrMessage(ResManager.LoadKDString("请选择且只能选择一道工序！", "015376000004724", SubSystemType.MFG, new object[0]), "", MessageBoxType.Notice);
                return false;
            }
            string dynamicObjectItemValue = selectedRows.First<DynamicObject>()["SeqId"].ToString();
            Tuple<bool, bool> seqInfoBySeqId = this.GetSeqInfoBySeqId(System.Convert.ToInt64(dynamicObjectItemValue));
            string dynamicObjectItemValue2 = selectedRows.First<DynamicObject>()["SeqType"].ToString();
            if (dynamicObjectItemValue2.Equals("A") && !seqInfoBySeqId.Item2)
            {
                this.View.ShowErrMessage(ResManager.LoadKDString("替代序列中包含入库点工序时才可以中间工序到底拆卡 ", "015165000015886", SubSystemType.MFG, new object[0]), "", MessageBoxType.Notice);
                return false;
            }
            string selectedSeqNumber = selectedRows.First<DynamicObject>()["SeqNumber"].ToString();
            int dynamicObjectItemValue3 = Convert.ToInt32(selectedRows.First<DynamicObject>()["OperNumber"]);
            EntryEntity entryEntity = this.View.BusinessInfo.GetEntryEntity("FDetailEntity");
            int num = (
                from w in this.Model.GetEntityDataObject(entryEntity)
                where w["SeqNumber"].ToString().Equals(selectedSeqNumber) && w["OperCancel"].ToString().Equals("A")
                select w).Min((DynamicObject m) => Convert.ToInt32(m["OperNumber"]));
            if (dynamicObjectItemValue3 == num)
            {
                this.View.ShowErrMessage(ResManager.LoadKDString("所选工序不允许是序列的首序！", "015376000004725", SubSystemType.MFG, new object[0]), "", MessageBoxType.Notice);
                return false;
            }
            return this.ValidateSplitCommon(selectedRows, "M");
        }
        private bool ValidateSplitSelect()
        {
            System.Collections.Generic.List<DynamicObject> selectedRows = this.GetSelectedRows();
            if (selectedRows.Count <= 0)
            {
                this.View.ShowErrMessage(ResManager.LoadKDString("请选择至少一道工序！", "015376000004766", SubSystemType.MFG, new object[0]), "", MessageBoxType.Notice);
                return false;
            }
            System.Collections.Generic.List<string> list = (
                from s in selectedRows
                select s["SeqNumber"].ToString()).Distinct<string>().ToList<string>();
            if (list.Count > 1)
            {
                this.View.ShowErrMessage(ResManager.LoadKDString("所选工序必须是同一序列的连续工序！", "015376000004726", SubSystemType.MFG, new object[0]), "", MessageBoxType.Notice);
                return false;
            }
            string selectedSeqNumber = list.First<string>();
            int minOperNumber = selectedRows.Min((DynamicObject m) => Convert.ToInt32(m["OperNumber"].ToString()));
            int maxOperNumber = selectedRows.Max((DynamicObject m) => Convert.ToInt32(m["OperNumber"]));
            EntryEntity entryEntity = this.View.BusinessInfo.GetEntryEntity("FDetailEntity");
            System.Collections.Generic.List<DynamicObject> list2 = (
                from w in this.Model.GetEntityDataObject(entryEntity)
                where w["SeqNumber"].Equals(selectedSeqNumber) && Convert.ToInt32(w["OperNumber"]) >= minOperNumber && Convert.ToInt32(w["OperNumber"]) <= maxOperNumber
                select w).ToList<DynamicObject>();
            if (selectedRows.Count != list2.Count)
            {
                this.View.ShowErrMessage(ResManager.LoadKDString("所选工序必须是同一序列的连续工序！", "015376000004726", SubSystemType.MFG, new object[0]), "", MessageBoxType.Notice);
                return false;
            }
            System.Collections.Generic.List<long> list3 = (
                from s in selectedRows
                select Convert.ToInt64(s["DepartmentId_Id"])).Distinct<long>().ToList<long>();
            if (list3.Count > 1)
            {
                this.View.ShowErrMessage(ResManager.LoadKDString("所选工序的加工车间必须相同！", "015376000004727", SubSystemType.MFG, new object[0]), "", MessageBoxType.Notice);
                return false;
            }
            return this.ValidateSplitCommon(selectedRows, "S");
        }
        private bool ValidateSplitCommon(System.Collections.Generic.List<DynamicObject> selectedRows, string splitType)
        {
            string value = this.Model.GetValue("FPlanType").ToString();
            if (value.Equals("D"))
            {
                this.View.ShowErrMessage(ResManager.LoadKDString("工序计划的计划类型为[分卡_选中序]，不允许拆卡！", "015376000004767", SubSystemType.MFG, new object[0]), "", MessageBoxType.Notice);
                return false;
            }
            string dynamicObjectItemValue = selectedRows.First<DynamicObject>()["SeqType"].ToString();
            if (dynamicObjectItemValue.Equals("R"))
            {
                this.View.ShowErrMessage(ResManager.LoadKDString("序列类型为[返修序列]，不允许拆卡！", "015376000021499", SubSystemType.MFG, new object[0]), "", MessageBoxType.Notice);
                return false;
            }
            System.Collections.Generic.List<DynamicObject> rows = new System.Collections.Generic.List<DynamicObject>();
            EntryEntity entryEntity = this.View.BusinessInfo.GetEntryEntity("FDetailEntity");
            string seqNumber = selectedRows.FirstOrDefault<DynamicObject>()["SeqNumber"].ToString();
            if (splitType.Equals("F"))
            {
                rows = (
                    from w in this.Model.GetEntityDataObject(entryEntity)
                    where w["SeqNumber"].ToString().Equals(seqNumber)
                    select w).ToList<DynamicObject>();
            }
            else
            {
                if (splitType.Equals("M"))
                {
                    int operNumber = Convert.ToInt32(selectedRows.FirstOrDefault<DynamicObject>()["OperNumber"]);
                    rows = (
                        from w in this.Model.GetEntityDataObject(entryEntity)
                        where w["SeqNumber"].ToString().Equals(seqNumber) && Convert.ToInt32(w["OperNumber"]) >= operNumber
                        select w).ToList<DynamicObject>();
                }
                else
                {
                    rows = selectedRows;
                }
            }
            if (!this.ValidateOperStatus(rows))
            {
                this.View.ShowErrMessage(ResManager.LoadKDString("拆卡工序集合含有完工、关闭状态的工序，不允许拆卡！", "015376000004729", SubSystemType.MFG, new object[0]), "", MessageBoxType.Notice);
                return false;
            }
            DynamicObject dynamicObjectItemValue2 = selectedRows.First<DynamicObject>()["OptCtrlCodeId"] as DynamicObject;
            if (!Convert.ToBoolean(selectedRows.First<DynamicObject>()["IsOutSrc"]) == false && !"10".Equals(dynamicObjectItemValue2["ReportMode"].ToString()))
            {
                this.View.ShowErrMessage(ResManager.LoadKDString("非汇报点或非委外序，不允许拆卡！", "015376000004907", SubSystemType.MFG, new object[0]), "", MessageBoxType.Notice);
                return false;
            }
            return true;
        }
        private bool ValidateDispatching()
        {
            System.Collections.Generic.List<DynamicObject> selectedRows = this.GetSelectedRows();
            if (selectedRows.Count <= 0)
            {
                this.View.ShowErrMessage(ResManager.LoadKDString("请选择至少一道工序！", "015376000004766", SubSystemType.MFG, new object[0]), "", MessageBoxType.Notice);
                return false;
            }
            System.Collections.Generic.List<long> list = (
                from s in selectedRows
                select Convert.ToInt64(s["WorkCenterId_Id"])).Distinct<long>().ToList<long>();
            System.Collections.Generic.List<long> list2 = (
                from s in selectedRows
                select Convert.ToInt64(s["DepartmentId_Id"])).Distinct<long>().ToList<long>();
            System.Collections.Generic.List<DynamicObject> list3 = (
                from s in selectedRows
                select s["ResourceId"] as DynamicObject).Distinct<DynamicObject>().ToList<DynamicObject>();
            if (list2.Count > 1 || list.Count > 1)
            {
                this.View.ShowErrMessage(ResManager.LoadKDString("所选工序的工作中心、加工车间必须相同！", "015376000004735", SubSystemType.MFG, new object[0]), "", MessageBoxType.Notice);
                return false;
            }
            if (list3.Count > 1)
            {
                this.View.ShowErrMessage(ResManager.LoadKDString("所选工序的排程资源必须相同", "015376000014173", SubSystemType.MFG, new object[0]), "", MessageBoxType.Notice);
                return false;
            }
            this.resourceObject = list3.Distinct<DynamicObject>().FirstOrDefault<DynamicObject>();
            if (!this.ValidateOperStatus(selectedRows))
            {
                this.View.ShowErrMessage(ResManager.LoadKDString("所选工序含有完工、关闭状态，不允许派工！", "015376000004736", SubSystemType.MFG, new object[0]), "", MessageBoxType.Notice);
                return false;
            }
            return true;
        }
        private bool ValidateTransQtyAdjust()
        {
            System.Collections.Generic.List<DynamicObject> selectedRows = this.GetSelectedRows();
            if (selectedRows.Count != 1)
            {
                this.View.ShowErrMessage(ResManager.LoadKDString("请选择且只能选择一道工序！", "015376000004724", SubSystemType.MFG, new object[0]), "", MessageBoxType.Notice);
                return false;
            }
            if (!this.ValidateOperStatus(selectedRows))
            {
                this.View.ShowErrMessage(ResManager.LoadKDString("所选工序含有完工、关闭状态，不允许调整转移数量！", "015376000004771", SubSystemType.MFG, new object[0]), "", MessageBoxType.Notice);
                return false;
            }
            bool dynamicObjectItemValue = Convert.ToBoolean(selectedRows.First<DynamicObject>()["IsOutSrc"]) == false;
            if (dynamicObjectItemValue)
            {
                this.View.ShowErrMessage(ResManager.LoadKDString("所选工序为委外工序，不允许调整转移数量！", "015376000004814", SubSystemType.MFG, new object[0]), "", MessageBoxType.Notice);
                return false;
            }
            return true;
        }
        private string ValidateLogQuery()
        {
            DynamicObjectCollection dynamicObjectCollection = MFGServiceHelper.GetDynamicObjectCollection(base.Context, new QueryBuilderParemeter
            {
                FormId = "SFC_OperationLog",
                SelectItems = SelectorItemInfo.CreateItems("FID"),
                FilterClauseWihtKey = string.Format(" FBILLFORMID = '{0}' AND FBILLID = {1}", "SFC_OperationPlanning", this.currNodeID)
            }, null);
            if (dynamicObjectCollection.Count <= 0)
            {
                string value = this.Model.GetValue("FBillNo").ToString();
                this.View.ShowMessage(string.Format(ResManager.LoadKDString("工序计划[{0}]暂无操作日志！", "015376000004805", SubSystemType.MFG, new object[0]), value), MessageBoxType.Notice);
                return "false";
            }
            return dynamicObjectCollection.First<DynamicObject>()["FID"].ToString();
        }
        private bool ValidateOperStatus(System.Collections.Generic.List<DynamicObject> rows)
        {
            System.Collections.Generic.List<string> list = (
                from s in rows
                select s["OperStatus"].ToString()).Distinct<string>().ToList<string>();
            foreach (string current in list)
            {
                if (current.Equals(5.ToString()) || current.Equals(6.ToString()))
                {
                    return false;
                }
            }
            return true;
        }
        private System.Collections.Generic.List<DynamicObject> GetSelectedRows()
        {
            EntryEntity entryEntity = this.View.BusinessInfo.GetEntryEntity("FDetailEntity");
            System.Collections.Generic.List<DynamicObject> list = new System.Collections.Generic.List<DynamicObject>();
            return (
                from w in this.Model.GetEntityDataObject(entryEntity)
                where Convert.ToBoolean(w["IsSelect"])== true
                select w).ToList<DynamicObject>();
        }
        private void ShowForm(string formId, bool needNetCtrl = true, string netCtrlFuncId = "", string pKey = "", bool simpleDispatch = false, OperationStatus status = OperationStatus.EDIT)
        {
            NetworkCtrlResult netCtrlResult = null;
            if (needNetCtrl)
            {
                netCtrlResult = this.BeginNetCtrl(netCtrlFuncId);
            }
            if ((needNetCtrl && netCtrlResult.StartSuccess) || !needNetCtrl)
            {
                BillShowParameter billShowParameter = new BillShowParameter();
                billShowParameter.FormId = formId;
                billShowParameter.ParentPageId = this.View.PageId;
                billShowParameter.OpenStyle.ShowType = ShowType.Modal;
                if (simpleDispatch)
                {
                    billShowParameter.CustomComplexParams.Add("DataObject", this.GetSimpleDispatchingDynamicObject());
                    billShowParameter.CustomComplexParams.Add("ResourceObject", this.resourceObject);
                }
                else
                {
                    billShowParameter.CustomComplexParams.Add("WorkBenchData", this.Model.DataObject);
                    billShowParameter.CustomComplexParams.Add("SelectedRows", this.GetSelectedRows());
                }
                billShowParameter.Status = status;
                if (!string.IsNullOrEmpty(pKey))
                {
                    billShowParameter.PKey = pKey;
                    billShowParameter.Height = 800;
                }
                this.View.ShowForm(billShowParameter, delegate (FormResult result)
                {
                    if (needNetCtrl)
                    {
                        this.CommitNetCtrl(netCtrlResult);
                    }
                    if (result != null && result.ReturnData != null)
                    {
                        IOperationResult operationResult = result.ReturnData as IOperationResult;
                        if (operationResult != null && operationResult.FuncResult != null)
                        {
                            if (operationResult.FuncResult is System.Collections.Generic.List<IOperationResult>)
                            {
                                this.currNodeID = System.Convert.ToString((operationResult.FuncResult as System.Collections.Generic.List<IOperationResult>)[0].FuncResult);
                            }
                            else
                            {
                                this.currNodeID = System.Convert.ToString(((OperationResult)operationResult.FuncResult).FuncResult);
                            }
                        }
                        this.Refresh();
                        if (operationResult != null)
                        {
                            OperateResultCollection operateResultCollection = new OperateResultCollection();
                            System.Collections.Generic.List<IOperationResult> list = operationResult.FuncResult as System.Collections.Generic.List<IOperationResult>;
                            if (list != null)
                            {
                                foreach (IOperationResult current in list)
                                {
                                    if (!current.IsSuccess)
                                    {
                                        operateResultCollection.Add(current.OperateResult[0]);
                                    }
                                }
                                if (operateResultCollection.Count > 0)
                                {
                                    this.View.ShowOperateResult(operateResultCollection, "BOS_BatchTips");
                                    return;
                                }
                            }
                            else
                            {
                                if (!operationResult.IsSuccess)
                                {
                                    this.View.ShowOperateResult(operationResult.OperateResult, "BOS_BatchTips");
                                }
                            }
                        }
                    }
                });
            }
        }
        private DynamicObject GetSimpleDispatchingDynamicObject()
        {
            IDynamicFormModel model = GetModelServiceHelper.GetModel(base.Context, "SFC_SimpleDispatching");
            FormMetadata formMetadata = MetaDataServiceHelper.Load(base.Context, "SFC_SimpleDispatching", true) as FormMetadata;
            EntryEntity entryEntity = formMetadata.BusinessInfo.GetEntity("FEntity") as EntryEntity;
            SubEntryEntity subEntryEntity = formMetadata.BusinessInfo.GetEntity("FSubEntity") as SubEntryEntity;
            DynamicObject dynamicObject = entryEntity.DynamicObjectType.CreateInstance() as DynamicObject;
            DynamicObject dynamicObject2 = subEntryEntity.DynamicObjectType.CreateInstance() as DynamicObject;
            System.Collections.Generic.List<DynamicObject> selectedRows = this.GetSelectedRows();
            DynamicObject dynamicObject3 = selectedRows.First<DynamicObject>();
            model.SetValue("FWorkCenterId", Convert.ToInt64(dynamicObject3["WorkCenterId_Id"]));
            model.SetValue("FDepartmentId", dynamicObject3["DepartmentId"]);
            model.SetValue("FProcessOrgId", dynamicObject3["ProcessOrgId"]);
            dynamicObject = (entryEntity.DynamicObjectType.CreateInstance() as DynamicObject);
            dynamicObject["OPId"] = this.Model.DataObject["OPlanID"];
            dynamicObject["OPBillNo"] = this.Model.DataObject["BillNo"];
            model.CreateNewEntryRow(entryEntity, -1, dynamicObject);
            foreach (DynamicObject current in selectedRows)
            {
                dynamicObject2 = (subEntryEntity.DynamicObjectType.CreateInstance() as DynamicObject);
                dynamicObject2["OperId"] = current["OperId"];
                DynamicObjectCollection dynamicObjectItemValue = dynamicObject["SubEntity"] as DynamicObjectCollection;
                dynamicObjectItemValue.Add(dynamicObject2);
            }
            return model.DataObject;
        }
        private NetworkCtrlResult BeginNetCtrl(string funcId)
        {
            string text = string.Empty;
            if (funcId != null)
            {
                if (!(funcId == "Split"))
                {
                    if (!(funcId == "SimpleDispatching"))
                    {
                        if (!(funcId == "TransQtyAdjust"))
                        {
                            if (funcId == "Submit")
                            {
                                text = ResManager.LoadKDString("提交", "015376000004803", SubSystemType.MFG, new object[0]);
                            }
                        }
                        else
                        {
                            text = ResManager.LoadKDString("转移数量调整", "015376000004769", SubSystemType.MFG, new object[0]);
                        }
                    }
                    else
                    {
                        text = ResManager.LoadKDString("简易派工", "015376000004737", SubSystemType.MFG, new object[0]);
                    }
                }
                else
                {
                    text = ResManager.LoadKDString("拆卡", "015376000004730", SubSystemType.MFG, new object[0]);
                }
            }
            NetWorkRunTimeParam netWorkRunTimeParam = new NetWorkRunTimeParam();
            netWorkRunTimeParam.OperationName = new LocaleValue(text);
            netWorkRunTimeParam.FuncDeatilID = funcId;
            netWorkRunTimeParam.InterID = this.Model.GetValue("FOPlanID").ToString();
            NetworkCtrlObject netCtrl = NetworkCtrlServiceHelper.GetNetCtrl(base.Context, "SFC_OperationPlanning", NetworkCtrlType.BusinessObjOperateMutex, netWorkRunTimeParam.FuncDeatilID);
            NetworkCtrlResult networkCtrlResult = NetworkCtrlServiceHelper.BeginNetCtrl(base.Context, netCtrl, netWorkRunTimeParam);
            if (!networkCtrlResult.StartSuccess)
            {
                string format = ResManager.LoadKDString("工序计划[{0}]被[{1}]锁定，不允许进行[{2}]操作！", "015376000004770", SubSystemType.MFG, new object[0]);
                this.View.ShowMessage(string.Format(format, this.Model.GetValue("FBillNo"), networkCtrlResult.ConflictUserName, text), MessageBoxType.Notice);
            }
            return networkCtrlResult;
        }
        private void CommitNetCtrl(NetworkCtrlResult netCtrlResult)
        {
            NetworkCtrlServiceHelper.CommitNetCtrl(base.Context, netCtrlResult);
        }
        protected virtual System.Collections.Generic.List<DynamicObject> GetOPlanData()
        {
            return ShopWorkBenchServiceHelper.GetOperationPlanByFilter(base.Context, this.filterParam);
        }
        private TreeNode BuildTree()
        {
            System.Collections.Generic.List<DynamicObject> oPlanData = this.GetOPlanData();
            this.nodeLst.Clear();
            TreeNode treeNode = new TreeNode();
            treeNode.text = ResManager.LoadKDString("工序计划", "015165000004619", SubSystemType.MFG, new object[0]);
            treeNode.id = "0";
            treeNode.cls = "parentnode";
            treeNode.parentid = "root";
            this.nodeLst.Add(treeNode);
            System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<DynamicObject>> dicPlanData = (
                from c in oPlanData
                group c by c["FParentId"].ToString()).ToDictionary((IGrouping<string, DynamicObject> c) => c.Key, (IGrouping<string, DynamicObject> c) => c.ToList<DynamicObject>());
            this.AddChildNode(treeNode, dicPlanData);
            return treeNode;
        }
        private void AddChildNode(TreeNode parentNode, System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<DynamicObject>> dicPlanData)
        {
            string id = parentNode.id;
            System.Collections.Generic.List<DynamicObject> list = null;
            if (!dicPlanData.TryGetValue(id, out list))
            {
                return;
            }
            foreach (DynamicObject current in list)
            {
                string id2 = System.Convert.ToString(current["FID"]);
                string parentid = System.Convert.ToString(current["FParentId"]);
                string nodeShowTitle = this.GetNodeShowTitle(current);
                TreeNode treeNode = new TreeNode();
                treeNode.id = id2;
                treeNode.parentid = parentid;
                treeNode.text = nodeShowTitle;
                parentNode.children.Add(treeNode);
                this.AddChildNode(treeNode, dicPlanData);
            }
        }
        private string GetPlanTypeName(string strPlanType)
        {
            string result = string.Empty;
            if (strPlanType.Equals("A"))
            {
                result = ResManager.LoadKDString("主计划", "015376000004779", SubSystemType.MFG, new object[0]);
            }
            else
            {
                if (strPlanType.Equals("B"))
                {
                    result = ResManager.LoadKDString("分卡_首序", "015376000004780", SubSystemType.MFG, new object[0]);
                }
                else
                {
                    if (strPlanType.Equals("C"))
                    {
                        result = ResManager.LoadKDString("分卡_中间工序", "015376000004781", SubSystemType.MFG, new object[0]);
                    }
                    else
                    {
                        if (strPlanType.Equals("D"))
                        {
                            result = ResManager.LoadKDString("分卡_选中序", "015376000004782", SubSystemType.MFG, new object[0]);
                        }
                    }
                }
            }
            return result;
        }
        public void Refresh()
        {
            long dynamicObjectItemValue = Convert.ToInt64(this.filterParam.CustomFilter["PrdOrgId_Id"]);
            if (dynamicObjectItemValue == 0L)
            {
                return;
            }
            TreeNode rootNode = this.BuildTree();
            this.curTreeView.SetRootNode(rootNode);
            if (this.currNodeID != string.Empty)
            {
                this.curTreeView.Select(this.currNodeID);
            }
            this.ExpandTreeNode(this.curTreeView, this.nodeLst);
            this.ReLoadData();
        }
        private void ExpandTreeNode(TreeView treeView, System.Collections.Generic.List<TreeNode> nodeLst)
        {
            if (nodeLst.Count == 0)
            {
                return;
            }
            foreach (TreeNode current in nodeLst)
            {
                treeView.InvokeControlMethod("ExpandNode", new object[]
                {
                    current.id
                });
            }
            treeView.SetExpanded(true);
        }
        protected virtual void ShowFilter(bool OnInitialize = false)
        {
            FilterParameter nextEntrySchemeFilter = this.GetNextEntrySchemeFilter();
            if (OnInitialize && this.isFirstLoad && nextEntrySchemeFilter != null)
            {
                this.filterParam = nextEntrySchemeFilter;
                this.currNodeID = string.Empty;
                this.Refresh();
                this.isFirstLoad = false;
                return;
            }
            this.View.ShowFilterForm("SFC_ShopWorkBench", null, delegate (FormResult filterResult)
            {
                if (filterResult.ReturnData is FilterParameter)
                {
                    this.filterParam = (filterResult.ReturnData as FilterParameter);
                    this.currNodeID = string.Empty;
                    this.Refresh();
                    return;
                }
                if (OnInitialize)
                {
                    this.View.Close();
                }
            }, "SFC_ShopWorkBenchFilter", ShowType.Default);
        }
        public void ReLoadData()
        {
            if (this.currNodeID != null && !this.currNodeID.Equals(string.Empty))
            {
                long oplanId = System.Convert.ToInt64(this.currNodeID);
                DynamicObject shopWorkBenchByOPlanId = ShopWorkBenchServiceHelper.GetShopWorkBenchByOPlanId(base.Context, oplanId);
                this.View.Model.DataObject = shopWorkBenchByOPlanId;
                this.View.UpdateView();
                return;
            }
            if (!this.isFirstLoad)
            {
                this.ClearData();
            }
        }
        public override void TreeNodeClick(TreeNodeArgs e)
        {
            if (!e.NodeId.Equals("0") && !e.NodeId.Equals(this.currNodeID))
            {
                this.currNodeID = e.NodeId;
                this.ReLoadData();
            }
        }
        protected void ClearData()
        {
            DynamicObject dataObject = new DynamicObject(this.Model.BusinessInfo.GetDynamicObjectType());
            this.Model.DataObject = dataObject;
            this.View.UpdateView();
        }
        public override void AfterDoOperation(AfterDoOperationEventArgs e)
        {
            System.Collections.Generic.List<string> list = new System.Collections.Generic.List<string>
            {
                "SplitFirstToLast",
                "SplitMidToLast",
                "SplitSelect"
            };
            if (list.Contains(e.Operation.Operation))
            {
                LicenseVerifier.CheckViewOnlyOperation(base.Context, ResManager.LoadKDString("拆卡", "015376000004730", SubSystemType.MFG, new object[0]));
            }
            string operation;
            if ((operation = e.Operation.Operation) != null)
            {
                if (operation == "Refresh")
                {
                    this.Refresh();
                    return;
                }
                if (operation == "SplitFirstToLast")
                {
                    this.SplitFirstToLast(e);
                    return;
                }
                if (operation == "SplitMidToLast")
                {
                    this.SplitMidToLast(e);
                    return;
                }
                if (!(operation == "SplitSelect"))
                {
                    return;
                }
                this.SplitSelect(e);
            }
        }
        protected void Edit()
        {
            bool flag = this.CheckSelectDataId();
            if (flag)
            {
                this.ShowForm("SFC_OperationPlanning", false, "", this.currNodeID, false, OperationStatus.EDIT);
            }
        }
        private bool CheckSelectDataId()
        {
            bool result = true;
            object value = this.Model.GetValue("FOPlanID");
            if (value == null)
            {
                result = false;
                this.View.ShowMessage(ResManager.LoadKDString("没有选择工序计划！", "015376000004785", SubSystemType.MFG, new object[0]), MessageBoxType.Notice);
            }
            long num = System.Convert.ToInt64(value);
            if (num == 0L)
            {
                result = false;
                this.View.ShowMessage(ResManager.LoadKDString("没有选择工序计划！", "015376000004785", SubSystemType.MFG, new object[0]), MessageBoxType.Notice);
            }
            if (!ShopWorkBenchServiceHelper.IsExistOPlan(base.Context, num))
            {
                result = false;
                this.View.ShowMessage(ResManager.LoadKDString("工序计划已经删除，请刷新界面或重新选择其他工序计划！", "015376000004804", SubSystemType.MFG, new object[0]), MessageBoxType.Notice);
            }
            return result;
        }
        protected DynamicObject GetDirectInScheme()
        {
            DynamicObject result = null;
            string nextEntrySchemeId = UserParamterServiceHelper.GetNextEntrySchemeId(base.Context, "SFC_ShopWorkBench");
            if (nextEntrySchemeId != null && nextEntrySchemeId != string.Empty)
            {
                FormMetadata formMetaData = MetaDataServiceHelper.GetFormMetaData(base.Context, "BOS_FilterScheme");
                FormMetadata formMetaData2 = MetaDataServiceHelper.GetFormMetaData(base.Context, "SFC_ShopWorkBenchFilter");
                DynamicObject dynamicObject = BusinessDataServiceHelper.Load(base.Context, new object[]
                {
                    nextEntrySchemeId
                }, formMetaData.BusinessInfo.GetDynamicObjectType()).FirstOrDefault<DynamicObject>();
                if (dynamicObject != null)
                {
                    FilterScheme filterScheme = new FilterScheme(dynamicObject);
                    if (filterScheme.Scheme != null && !filterScheme.Scheme.Equals(string.Empty))
                    {
                        SchemeEntity schemeEntity = (SchemeEntity)new DcxmlSerializer(new PreInsertDataDcxmlBinder()).DeserializeFromString(filterScheme.Scheme, null);
                        DcxmlBinder dcxmlBinder = new DynamicObjectDcxmlBinder(formMetaData2.BusinessInfo);
                        dcxmlBinder.OnlyDbProperty = false;
                        System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo(2052);
                        dcxmlBinder.Culture = culture;
                        DcxmlSerializer dcxmlSerializer = new DcxmlSerializer(dcxmlBinder);
                        DynamicObject dynamicObject2 = (DynamicObject)dcxmlSerializer.DeserializeFromString(schemeEntity.CustomFilterSetting, null);
                        if (dynamicObject2 != null)
                        {
                            result = dynamicObject2;
                        }
                    }
                }
            }
            return result;
        }
        private FilterParameter GetNextEntrySchemeFilter()
        {
            DynamicObject directInScheme = this.GetDirectInScheme();
            if (directInScheme != null)
            {
                return new FilterParameter
                {
                    CustomFilter = directInScheme
                };
            }
            return null;
        }
        protected virtual string GetNodeShowTitle(DynamicObject childNode)
        {
            string text = (string)childNode["FBILLNO"];
            string text2 = (string)childNode["FNUMBER"];
            LocaleValue localeValue = new LocaleValue(System.Convert.ToString(childNode["FNAME"]), base.Context.UserLocale.LCID);
            string text3 = localeValue.ToString();
            string text4 = (string)childNode["FPlanType"];
            decimal d = System.Convert.ToDecimal(childNode["FMOQty"]);
            int decimals = System.Convert.ToInt32(childNode["FPRECISION"]);
            string text5 = System.Math.Round(d, decimals).ToString();
            string result;
            if (text4.Equals("A"))
            {
                result = string.Format("{0} {1} {2} {3}", new object[]
                {
                    text,
                    text2,
                    text3,
                    text5
                });
            }
            else
            {
                result = string.Format("{0} {1} {2} ", text, this.GetPlanTypeName(text4), text5);
            }
            return result;
        }
        private Tuple<bool, bool> GetSeqInfoBySeqId(long seqId)
        {
            bool item = false;
            bool item2 = false;
            string strSQL = "SELECT CO.FIsFirstOper,CO.FIsStoreInPoint\r\n                                FROM T_SFC_OPERPLANNINGDETAIL CO\r\n                                INNER JOIN T_SFC_OPERPLANNINGSEQ CS ON CO.FENTRYID=CS.FENTRYID\r\n                                INNER JOIN T_SFC_OPERPLANNING OP ON CS.FID = OP.FID\r\n                                WHERE CS.FentryID=@SeqId";
            System.Collections.Generic.List<SqlParam> list = new System.Collections.Generic.List<SqlParam>();
            list.Add(new SqlParam("@SeqId", KDDbType.Int64, seqId));
            DynamicObjectCollection dynamicObjectCollection = DBServiceHelper.ExecuteDynamicObject(base.Context, strSQL, null, null, CommandType.Text, list.ToArray());
            foreach (DynamicObject current in dynamicObjectCollection)
            {
                if ((string)current["FIsFirstOper"] == "1")
                {
                    item = true;
                }
                if ((string)current["FIsStoreInPoint"] == "1")
                {
                    item2 = true;
                }
            }
            return new Tuple<bool, bool>(item, item2);
        }
        public ListSelectedRowCollection CreateListSelectedRow()
        {
            ListSelectedRowCollection listSelectedRowCollection = new ListSelectedRowCollection();
            string formID = "SFC_OperationPlanning";
            string primaryKeyValue = this.currNodeID;
            string value = this.Model.GetValue("FBillNo").ToString();
            listSelectedRowCollection.Add(new ListSelectedRow(primaryKeyValue, null, 0, formID)
            {
                BillNo = value
            });
            return listSelectedRowCollection;
        }
        protected System.Collections.Generic.List<ListSelectedRow> StartNetworkCtrl_New(Context ctx, ListSelectedRowCollection selectedRows, string formId, string operatNumber, ref OperateResultCollection resultCollection)
        {
            string text = ResManager.LoadKDString("工序计划", "015165000004619", SubSystemType.MFG, new object[0]);
            string text2 = string.Empty;
            if (operatNumber != null)
            {
                if (!(operatNumber == "Delete"))
                {
                    if (!(operatNumber == "Submit"))
                    {
                        if (!(operatNumber == "CancelAssign"))
                        {
                            if (!(operatNumber == "Audit"))
                            {
                                if (operatNumber == "UnAudit")
                                {
                                    text2 = ResManager.LoadKDString("反审核", "015649000004840", SubSystemType.MFG, new object[0]);
                                }
                            }
                            else
                            {
                                text2 = ResManager.LoadKDString("审核", "015649000004839", SubSystemType.MFG, new object[0]);
                            }
                        }
                        else
                        {
                            text2 = ResManager.LoadKDString("撤销", "015649000004838", SubSystemType.MFG, new object[0]);
                        }
                    }
                    else
                    {
                        text2 = ResManager.LoadKDString("提交", "015376000004803", SubSystemType.MFG, new object[0]);
                    }
                }
                else
                {
                    text2 = ResManager.LoadKDString("删除", "015649000004837", SubSystemType.MFG, new object[0]);
                }
            }
            string filter = string.Format(" FMetaObjectID = '{0}' and Fnumber = '{1}'  and ftype={2}  and FStart = '1'  ", formId, operatNumber, 6);
            NetworkCtrlObject networkCtrlObject = NetworkCtrlServiceHelper.GetNetCtrlList(ctx, filter).FirstOrDefault<NetworkCtrlObject>();
            System.Collections.Generic.List<NetWorkRunTimeParam> list = new System.Collections.Generic.List<NetWorkRunTimeParam>();
            foreach (ListSelectedRow current in selectedRows)
            {
                list.Add(new NetWorkRunTimeParam
                {
                    BillName = new LocaleValue(text, 2052),
                    InterID = current.PrimaryKeyValue,
                    OperationDesc = string.Concat(new string[]
                    {
                        text,
                        "-",
                        current.BillNo,
                        "-",
                        text2
                    }),
                    OperationName = new LocaleValue(text2, 2052)
                });
            }
            if (networkCtrlObject != null)
            {
                this.NetworkCtrlResults = NetworkCtrlServiceHelper.BatchBeginNetCtrl(ctx, networkCtrlObject, list, false);
                return this.FormNetworkCtrlResult(ctx, selectedRows, this.NetworkCtrlResults, text2, ref resultCollection);
            }
            this.NetworkCtrlResults = null;
            return null;
        }
        private System.Collections.Generic.List<ListSelectedRow> FormNetworkCtrlResult(Context ctx, ListSelectedRowCollection selectedRows, System.Collections.Generic.List<NetworkCtrlResult> networkCtrlResults, string operationName, ref OperateResultCollection collection)
        {
            System.Collections.Generic.List<ListSelectedRow> list = new System.Collections.Generic.List<ListSelectedRow>();
            using (System.Collections.Generic.List<NetworkCtrlResult>.Enumerator enumerator = networkCtrlResults.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    NetworkCtrlResult networkCtrlResult = enumerator.Current;
                    if (networkCtrlResult.StartSuccess)
                    {
                        ListSelectedRow listSelectedRow = selectedRows.FirstOrDefault((ListSelectedRow e) => e.PrimaryKeyValue.Equals(networkCtrlResult.InterID));
                        if (listSelectedRow != null)
                        {
                            list.Add(listSelectedRow);
                        }
                    }
                    else
                    {
                        collection.Add(new OperateResult
                        {
                            PKValue = networkCtrlResult.InterID,
                            SuccessStatus = false,
                            Message = networkCtrlResult.Message,
                            Name = operationName
                        });
                    }
                }
            }
            return list;
        }
        protected virtual void CommitNetworkCtrl(Context ctx, System.Collections.Generic.List<NetworkCtrlResult> NetworkCtrlResults)
        {
            NetworkCtrlServiceHelper.BatchCommitNetCtrl(ctx, NetworkCtrlResults);
        }
        private void SetBillStatus(BusinessInfo billBusinessInfo, string operatNumber)
        {
            if (!this.CheckSelectDataId())
            {
                return;
            }
            ListSelectedRowCollection selectedRows = this.CreateListSelectedRow();
            OperateResultCollection operateResults = new OperateResultCollection();
            System.Collections.Generic.List<ListSelectedRow> list = this.StartNetworkCtrl_New(base.Context, selectedRows, billBusinessInfo.GetForm().Id, operatNumber, ref operateResults);
            if (list == null || list.Count < 1)
            {
                this.View.ShowOperateResult(operateResults, "BOS_BatchTips");
                return;
            }
            System.Collections.Generic.List<string> list2 = new System.Collections.Generic.List<string>();
            if (list != null)
            {
                list2 = (
                    from p in list
                    select p.PrimaryKeyValue).Distinct<string>().ToList<string>();
            }
            IOperationResult operationResult = null;
            OperateOption option = OperateOption.Create();
            string arg = string.Empty;
            string value = this.Model.GetValue("FBillNo").ToString();
            if (operatNumber != null)
            {
                if (!(operatNumber == "Delete"))
                {
                    if (!(operatNumber == "Submit"))
                    {
                        if (!(operatNumber == "CancelAssign"))
                        {
                            if (!(operatNumber == "Audit"))
                            {
                                if (operatNumber == "UnAudit")
                                {
                                    arg = ResManager.LoadKDString("反审核", "015649000004840", SubSystemType.MFG, new object[0]);
                                    operationResult = this.SetUnAuditStatus(billBusinessInfo, operatNumber, list2);
                                }
                            }
                            else
                            {
                                arg = ResManager.LoadKDString("审核", "015649000004839", SubSystemType.MFG, new object[0]);
                                operationResult = this.SetAuditStatus(billBusinessInfo, operatNumber, list2);
                            }
                        }
                        else
                        {
                            arg = ResManager.LoadKDString("撤销", "015649000004838", SubSystemType.MFG, new object[0]);
                            operationResult = this.SetCancelAssignStatus(billBusinessInfo, operatNumber, list2);
                        }
                    }
                    else
                    {
                        arg = ResManager.LoadKDString("提交", "015376000004803", SubSystemType.MFG, new object[0]);
                        operationResult = BusinessDataServiceHelper.Submit(base.Context, billBusinessInfo, list2.ToArray(), "Submit", option);
                    }
                }
                else
                {
                    arg = ResManager.LoadKDString("删除", "015649000004837", SubSystemType.MFG, new object[0]);
                    object[] ids = new object[]
                    {
                        this.currNodeID
                    };
                    operationResult = BusinessDataServiceHelper.Delete(this.View.Context, billBusinessInfo, ids, option, "");
                }
            }
            this.CommitNetworkCtrl(base.Context, this.NetworkCtrlResults);
            if (!operationResult.IsSuccess)
            {
                foreach (ValidationErrorInfo current in operationResult.ValidationErrors)
                {
                    operationResult.OperateResult.Add(new OperateResult
                    {
                        SuccessStatus = false,
                        Message = current.Message,
                        PKValue = current.BillPKID,
                        Name = current.Title
                    });
                }
                this.View.ShowOperateResult(operationResult.OperateResult, "BOS_BatchTips");
                return;
            }
            foreach (OperateResult current2 in operationResult.OperateResult)
            {
                if (current2.Name == null || current2.Name.Equals(string.Empty))
                {
                    current2.Name = string.Format("{0}:{1}", arg, value);
                }
            }
            this.View.ShowOperateResult(operationResult.OperateResult, "BOS_BatchTips");
            if ("Delete".Equals(operatNumber))
            {
                if (this.parentNodeID != 0L)
                {
                    this.currNodeID = this.parentNodeID.ToString();
                }
                else
                {
                    this.currNodeID = string.Empty;
                }
                this.Refresh();
                return;
            }
            this.ReLoadData();
        }
        private IOperationResult SetAuditStatus(BusinessInfo billBusinessInfo, string operNumber, System.Collections.Generic.List<string> listId)
        {
            System.Collections.Generic.List<object> list = new System.Collections.Generic.List<object>();
            list.Add("1");
            list.Add("");
            System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<object, object>> list2 = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<object, object>>();
            foreach (string current in listId)
            {
                list2.Add(new System.Collections.Generic.KeyValuePair<object, object>(current, ""));
            }
            return BusinessDataServiceHelper.SetBillStatus(base.Context, billBusinessInfo, list2, list, operNumber, null);
        }
        private IOperationResult SetUnAuditStatus(BusinessInfo billBusinessInfo, string operNumber, System.Collections.Generic.List<string> listId)
        {
            System.Collections.Generic.List<object> list = new System.Collections.Generic.List<object>();
            list.Add("2");
            list.Add("");
            System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<object, object>> list2 = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<object, object>>();
            foreach (string current in listId)
            {
                list2.Add(new System.Collections.Generic.KeyValuePair<object, object>(current, ""));
            }
            return BusinessDataServiceHelper.SetBillStatus(base.Context, billBusinessInfo, list2, list, operNumber, null);
        }
        private IOperationResult SetCancelAssignStatus(BusinessInfo billBusinessInfo, string operNumber, System.Collections.Generic.List<string> listId)
        {
            System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<object, object>> list = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<object, object>>();
            foreach (string current in listId)
            {
                list.Add(new System.Collections.Generic.KeyValuePair<object, object>(current, ""));
            }
            return BusinessDataServiceHelper.SetBillStatus(base.Context, billBusinessInfo, list, null, operNumber, null);
        }
        private void ShowProgressBar(bool isSpecified = false)
        {
            ListSelectedRowCollection listSelectedRowCollection = this.CreateListSelectedRow();
            HashSet<string> hashSet = new HashSet<string>();
            foreach (ListSelectedRow current in listSelectedRowCollection)
            {
                if (!string.IsNullOrWhiteSpace(current.PrimaryKeyValue))
                {
                    hashSet.Add(current.PrimaryKeyValue);
                }
            }
            if (hashSet.Count <= 0)
            {
                this.View.ShowErrMessage(ResManager.LoadKDString("请选择需要工序排程的数据！", "015165000004602", SubSystemType.MFG, new object[0]), "", MessageBoxType.Notice);
                return;
            }
            DynamicFormShowParameter dynamicFormShowParameter = new DynamicFormShowParameter();
            dynamicFormShowParameter.FormId = "SFC_ProcessScheduling";
            dynamicFormShowParameter.ParentPageId = this.View.PageId;
            dynamicFormShowParameter.OpenStyle.ShowType = ShowType.Floating;
            dynamicFormShowParameter.CustomComplexParams.Add("PkIds", hashSet.ToList<string>());
            dynamicFormShowParameter.CustomComplexParams.Add("IsNetCtrl", true);
            dynamicFormShowParameter.CustomComplexParams.Add("ProOrgId", ((DynamicObject)this.Model.GetValue("FProOrgId"))["Id"].ToString());
            dynamicFormShowParameter.CustomComplexParams.Add("IsSpecified", isSpecified);
            if (isSpecified)
            {
                System.Collections.Generic.List<DynamicObject> selectedRows = this.GetSelectedRows();
                if (selectedRows.Count != 1)
                {
                    this.View.ShowErrMessage(ResManager.LoadKDString("请选择一行工序明细分录！", "015165000011028", SubSystemType.MFG, new object[0]), "", MessageBoxType.Notice);
                    return;
                }
                DynamicObject dynamicObject = selectedRows.FirstOrDefault<DynamicObject>();
                System.Collections.Generic.Dictionary<long, long> dictionary = new System.Collections.Generic.Dictionary<long, long>();
                dictionary[Convert.ToInt64(dynamicObject["SeqId"])] = Convert.ToInt64(dynamicObject["OperId"]);
                dynamicFormShowParameter.CustomComplexParams.Add("SpecifiedOperIds", dictionary);
            }
            this.View.ShowForm(dynamicFormShowParameter, new System.Action<FormResult>(this.CloseProcessBar));
        }
        private void CloseProcessBar(FormResult formResult)
        {
            this.ReLoadData();
        }
        private void SetStatus(BarItemClickEventArgs e)
        {
            System.Collections.Generic.List<DynamicObject> selectedRows = this.GetSelectedRows();
            if (selectedRows == null || selectedRows.Count == 0)
            {
                this.View.ShowErrMessage(ResManager.LoadKDString("请选择工序计划工序分录", "015376000004806", SubSystemType.MFG, new object[0]), "", MessageBoxType.Notice);
                e.Cancel = true;
                return;
            }
            System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<object, object>> list = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<object, object>>();
            foreach (DynamicObject current in selectedRows)
            {
                string key = this.currNodeID;
                string dynamicObjectItemValue = current["OperId"].ToString();
                list.Add(new System.Collections.Generic.KeyValuePair<object, object>(key, dynamicObjectItemValue));
            }
            string operation = string.Empty;
            string barItemKey;
            switch (barItemKey = e.BarItemKey)
            {
                case "tbBtnToPlanConfirm":
                    operation = "ToPlanConfirm";
                    break;
                case "tbBtnToRelease":
                    operation = "ToRelease";
                    break;
                case "tbBtnToStart":
                    operation = "ToStart";
                    break;
                case "tbBtnToFinish":
                    operation = "ToFinish";
                    break;
                case "tbBtnToClose":
                    operation = "ToClose";
                    break;
                case "tbBtnUndoToPlan":
                    operation = "UnDoToPlan";
                    break;
                case "tbBtnUndoToPlanConfirm":
                    operation = "UnDoToPlanConfirm";
                    break;
                case "tbBtnUndoToRelease":
                    operation = "UnDoToRelease";
                    break;
                case "tbBtnUndoToStart":
                    operation = "UnDoToStart";
                    break;
                case "tbBtnUndoToFinish":
                    operation = "UnDoToFinish";
                    break;
            }
            if (!this.CheckPermission(operation))
            {
                e.Cancel = true;
                return;
            }
            string operRowOperationName = SFCBillUtil.GetOperRowOperationName(e.BarItemKey);
            string text = string.Format(ResManager.LoadKDString("工序计划行状态执行[{0}]开始，共选中{1}行：{2}", "015165030034463", SubSystemType.MFG, new object[0]), operRowOperationName, list.Count, this.GetSelectRowInfo(selectedRows));
            Logger.Info("SetStatus", text);
            LogObject logObject = new LogObject
            {
                Description = text,
                Environment = OperatingEnvironment.BizOperate,
                OperateName = string.Format(ResManager.LoadKDString("行状态执行[{0}]", "015165030034464", SubSystemType.MFG, new object[0]), operRowOperationName),
                ObjectTypeId = "SFC_ShopWorkBench",
                SubSystemId = this.Model.BillBusinessInfo.GetForm().SubsysId
            };
            LogServiceHelper.WriteLog(base.Context, logObject);
            IOperationResult operationResult = OperationPlanningServiceHelper.SetStatus(base.Context, list, operation);
            OperateResultCollection operateResultCollection = new OperateResultCollection();
            string name = ResManager.LoadKDString("工序计划行执行", "015165000004605", SubSystemType.MFG, new object[0]);
            string format = ResManager.LoadKDString("工序计划[{0}]序列[{1}]工序[{2}]，行执行操作成功！", "015165000004606", SubSystemType.MFG, new object[0]);
            System.Collections.Generic.List<LogObject> list2 = new System.Collections.Generic.List<LogObject>();
            if (operationResult != null)
            {
                if (operationResult.ValidationErrors.Count > 0)
                {
                    System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
                    foreach (ValidationErrorInfo current2 in operationResult.ValidationErrors)
                    {
                        operateResultCollection.Add(new OperateResult
                        {
                            Message = current2.Message,
                            Name = name,
                            SuccessStatus = false
                        });
                        stringBuilder.AppendLine(current2.Message);
                    }
                    list2.Add(new LogObject
                    {
                        Description = stringBuilder.ToString(),
                        Environment = OperatingEnvironment.BizOperate,
                        OperateName = string.Format(ResManager.LoadKDString("行状态执行[{0}]错误", "015165030034466", SubSystemType.MFG, new object[0]), operRowOperationName),
                        ObjectTypeId = "SFC_ShopWorkBench",
                        SubSystemId = this.Model.BillBusinessInfo.GetForm().SubsysId
                    });
                }
                System.Collections.Generic.List<DynamicObject> list3 = operationResult.FuncResult as System.Collections.Generic.List<DynamicObject>;
                if (list3.Count > 0)
                {
                    System.Text.StringBuilder stringBuilder2 = new System.Text.StringBuilder();
                    foreach (DynamicObject current3 in list3)
                    {
                        OperateResult operateResult = new OperateResult();
                        string dynamicObjectItemValue2 = ((current3.Parent as DynamicObject).Parent as DynamicObject)["BillNo"].ToString();
                        string dynamicObjectItemValue3 = (current3.Parent as DynamicObject)["SeqNumber"].ToString();
                        int dynamicObjectItemValue4 = Convert.ToInt32(current3["OperNumber"]);
                        operateResult.Message = string.Format(format, dynamicObjectItemValue2, dynamicObjectItemValue3, dynamicObjectItemValue4);
                        operateResult.Name = name;
                        operateResult.SuccessStatus = true;
                        operateResultCollection.Add(operateResult);
                        stringBuilder2.AppendLine(operateResult.Message);
                    }
                    list2.Add(new LogObject
                    {
                        Description = stringBuilder2.ToString(),
                        Environment = OperatingEnvironment.BizOperate,
                        OperateName = string.Format(ResManager.LoadKDString("行状态执行[{0}]成功", "015165030034467", SubSystemType.MFG, new object[0]), operRowOperationName),
                        ObjectTypeId = "SFC_ShopWorkBench",
                        SubSystemId = this.Model.BillBusinessInfo.GetForm().SubsysId
                    });
                }
            }
            if (list2.Count > 0)
            {
                LogServiceHelper.BatchWriteLog(base.Context, list2);
            }
            if (operateResultCollection.Count > 0)
            {
                this.View.ShowOperateResult(operateResultCollection, "BOS_BatchTips");
                this.ReLoadData();
            }
        }
        private bool ValidateDocumentStatus(BeforeDoOperationEventArgs e)
        {
            bool result = false;
            if (!this.CheckSelectDataId())
            {
                return result;
            }
            e.Ids = new string[]
            {
                this.currNodeID
            };
            FormMetadata formMetadata = MetaDataServiceHelper.Load(base.Context, "SFC_OperationPlanning", true) as FormMetadata;
            DynamicObject[] array = BusinessDataServiceHelper.Load(base.Context, e.Ids, formMetadata.BusinessInfo.GetDynamicObjectType());
            System.Collections.Generic.List<object> list = new System.Collections.Generic.List<object>();
            IOperationResult operationResult = new OperationResult();
            string format = ResManager.LoadKDString("工序计划[{0}]的单据状态是审核态，不能删除", "015165000004607", SubSystemType.MFG, new object[0]);
            string title = ResManager.LoadKDString("执行操作失败", "015165000004608", SubSystemType.MFG, new object[0]);
            DynamicObject[] array2 = array;
            for (int i = 0; i < array2.Length; i++)
            {
                DynamicObject dynamicObject = array2[i];
                long dynamicObjectItemValue = Convert.ToInt64(dynamicObject["Id"]);
                string dynamicObjectItemValue2 = dynamicObject["BillNo"].ToString();
                string dynamicObjectItemValue3 = dynamicObject["DocumentStatus"].ToString();
                if (dynamicObjectItemValue3.Equals("C"))
                {
                    ValidationErrorInfo item = new ValidationErrorInfo("FDocumentStatus", dynamicObjectItemValue.ToString(), 0, 0, "Delete", string.Format(format, dynamicObjectItemValue2), title, ErrorLevel.Error);
                    operationResult.ValidationErrors.Add(item);
                }
                else
                {
                    list.Add(dynamicObjectItemValue);
                }
            }
            e.Ids = list.ToArray();
            if (operationResult.ValidationErrors.Count > 0)
            {
                result = false;
                operationResult.IsSuccess = false;
                e.Result.MergeResult(operationResult);
            }
            else
            {
                result = true;
            }
            return result;
        }
        //下查
        protected void TrckDown()
        {
            System.Collections.Generic.List<DynamicObject> selectedRows = this.GetSelectedRows();
            if (selectedRows.Count == 0)
            {
                this.View.ShowMessage(ResManager.LoadKDString("请选择需要下查的的分录!", "015649000004851", SubSystemType.MFG, new object[0]), MessageBoxType.Notice);
                return;
            }
            ViewLinkDataParameter viewLinkDataParameter = this.BuildViewLinkDataParameter();
            ShowConvertOpFormEventArgs thirdConvertEventArgs = null;
            System.Collections.Generic.List<ConvertBillElement> list = this.LoadConvertBills(viewLinkDataParameter, out thirdConvertEventArgs);
            if (list == null || list.Count == 0)
            {
                this.View.ShowMessage(ResManager.LoadKDString("没有关联业务数据!", "015649000004852", SubSystemType.MFG, new object[0]), MessageBoxType.Notice);
                return;
            }
            if (!this.ExistsTrackerData(viewLinkDataParameter, thirdConvertEventArgs))
            {
                this.View.ShowMessage(ResManager.LoadKDString("没有关联业务数据!", "015649000004852", SubSystemType.MFG, new object[0]), MessageBoxType.Notice);
                return;
            }
            this.ShowLookUpTrackerForm(viewLinkDataParameter, list, thirdConvertEventArgs);
        }
        protected void TrckDown1()
        {
            System.Collections.Generic.List<DynamicObject> selectedRows = this.GetSelectedRows();
            if (selectedRows.Count == 0)
            {
                this.View.ShowMessage(ResManager.LoadKDString("请选择需要下查的的分录!", "015649000004851", SubSystemType.MFG, new object[0]), MessageBoxType.Notice);
                return;
            }
            ViewLinkDataParameter viewLinkDataParameter = this.BuildViewLinkDataParameter();
            ShowConvertOpFormEventArgs thirdConvertEventArgs = null;
            System.Collections.Generic.List<ConvertBillElement> list = this.LoadConvertBills(viewLinkDataParameter, out thirdConvertEventArgs);
            if (list == null || list.Count == 0)
            {
                this.View.ShowMessage(ResManager.LoadKDString("没有关联业务数据!", "015649000004852", SubSystemType.MFG, new object[0]), MessageBoxType.Notice);
                return;
            }
            if (!this.ExistsTrackerData(viewLinkDataParameter, thirdConvertEventArgs))
            {
                this.View.ShowMessage(ResManager.LoadKDString("没有关联业务数据!", "015649000004852", SubSystemType.MFG, new object[0]), MessageBoxType.Notice);
                return;
            }
            this.ShowLookUpTrackerForm1(viewLinkDataParameter, list, thirdConvertEventArgs);
        }
        private ViewLinkDataParameter BuildViewLinkDataParameter()
        {
            if (!this.CheckSelectDataId() && this.GetSelectedRows().Count == 0)
            {
                this.View.ShowMessage(ResManager.LoadKDString("没有选择要下查的数据！", "015649000004853", SubSystemType.MFG, new object[0]), MessageBoxType.Notice);
                return null;
            }
            System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Tuple<long, long>>> linkIds = this.GetLinkIds();
            ViewLinkDataParameter viewLinkDataParameter = new ViewLinkDataParameter(this.OPlanMetaData.Id, ViewLinkDataParameter.Enum_LookUpType.Down);
            foreach (System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.List<Tuple<long, long>>> current in linkIds)
            {
                string key = current.Key;
                System.Collections.Generic.List<Tuple<long, long>> value = current.Value;
                foreach (Tuple<long, long> current2 in value)
                {
                   // if (current2.Item1 != current2.Item2)
                   // {
                        ViewLinkDataRowInfo item = new ViewLinkDataRowInfo(key, current2.Item1, current2.Item2);
                        viewLinkDataParameter.BillInfo.Rows.Add(item);
                   // }

                }
            }
            this.BuildInstanceParameter(viewLinkDataParameter, linkIds);
            return viewLinkDataParameter;
        }
        private void BuildInstanceParameter(ViewLinkDataParameter viewParameter, System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Tuple<long, long>>> allEntityIds)
        {
            string formId = viewParameter.BillInfo.FormId;
            foreach (System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.List<Tuple<long, long>>> current in allEntityIds)
            {
                string key = current.Key;
                System.Collections.Generic.List<Tuple<long, long>> value = current.Value;
                System.Collections.Generic.List<long> list = (
                    from p in value
                    select p.Item2).ToList<long>();
                BusinessFlowInstanceCollection businessFlowInstanceCollection = this.LoadInstances(this.View.Context, formId, key, list.ToArray());
                System.Collections.Generic.List<BusinessFlowInstance> list2 = new System.Collections.Generic.List<BusinessFlowInstance>();
                foreach (BusinessFlowInstance current2 in businessFlowInstanceCollection)
                {
                    if (current2.FirstNode != null)
                    {
                        list2.Add(current2);
                    }
                }
                if (list2.Count > 0)
                {
                    viewParameter.Instances[key] = list2;
                }
            }
        }
        public BusinessFlowInstanceCollection LoadInstances(Context ctx, string formId, string entityKey, long[] entityIds)
        {
            LoadInstancesByEntityIdArgs args = new LoadInstancesByEntityIdArgs(formId, entityKey, entityIds);
            return BusinessFlowDataServiceHelper.LoadInstancesByEntityId(ctx, args);
        }
        protected System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Tuple<long, long>>> GetLinkIds()
        {
            System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Tuple<long, long>>> dictionary = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Tuple<long, long>>>();
            System.Collections.Generic.List<long> list = new System.Collections.Generic.List<long>();
            System.Collections.Generic.List<Tuple<long, long>> list2 = new System.Collections.Generic.List<Tuple<long, long>>();
            long num = System.Convert.ToInt64(this.currNodeID);
            list.Add(num);
            list2.Add(new Tuple<long, long>(num, num));
            dictionary.Add("FBILLHEAD", list2);
            System.Collections.Generic.List<long> list3 = new System.Collections.Generic.List<long>();
            System.Collections.Generic.List<Tuple<long, long>> list4 = new System.Collections.Generic.List<Tuple<long, long>>();
            System.Collections.Generic.List<DynamicObject> selectedRows = this.GetSelectedRows();
            foreach (DynamicObject current in selectedRows)
            {
                long dynamicObjectItemValue = Convert.ToInt64(current["SeqId"]);
                if (!list3.Contains(dynamicObjectItemValue))
                {
                    list3.Add(dynamicObjectItemValue);
                    list4.Add(new Tuple<long, long>(num, dynamicObjectItemValue));
                }
            }
            if (list3.Count > 0)
            {
                dictionary.Add("FENTITY", list4);
            }
            return dictionary;
        }
        protected System.Collections.Generic.List<ConvertBillElement> LoadConvertBills(ViewLinkDataParameter viewParameter, out ShowConvertOpFormEventArgs thirdConvertEventArgs)
        {
            System.Collections.Generic.List<ConvertBillElement> list = ConvertServiceHelper.GetConvertBills(this.View.Context, FormOperationEnum.Push, viewParameter.BillInfo.FormId, false);
            DynamicFormViewPlugInProxy service = this.View.GetService<DynamicFormViewPlugInProxy>();
            thirdConvertEventArgs = new ShowConvertOpFormEventArgs(FormOperationEnum.TrackDown, list);
            if (service != null)
            {
                new System.Collections.Generic.List<ListSelectedRow>();
                thirdConvertEventArgs.SelectedRows = this.GetSelectedEntityRows();
                list = (thirdConvertEventArgs.Bills as System.Collections.Generic.List<ConvertBillElement>);
            }
            if (list == null || list.Count == 0)
            {
                this.View.ShowMessage(ResManager.LoadKDString("从启用的单据转换流程中找不到可下查的单据", "015649000004854", SubSystemType.MFG, new object[0]), MessageBoxType.Notice);
                return new System.Collections.Generic.List<ConvertBillElement>();
            }
            return list;
        }
        public ListSelectedRow[] GetSelectedEntityRows()
        {
            System.Collections.Generic.List<ListSelectedRow> list = new System.Collections.Generic.List<ListSelectedRow>();
            System.Collections.Generic.List<DynamicObject> selectedRows = this.GetSelectedRows();
            foreach (DynamicObject current in selectedRows)
            {
                long dynamicObjectItemValue = Convert.ToInt64(current["OperId"]);
                list.Add(new ListSelectedRow(this.currNodeID, dynamicObjectItemValue.ToString(), 0, this._oplanMeta.Id)
                {
                    EntryEntityKey = "FENTITY"
                });
            }
            if (list.Count == 0)
            {
                ListSelectedRow item = new ListSelectedRow(this.currNodeID, "0", 0, this._oplanMeta.Id);
                list.Add(item);
            }
            return list.ToArray();
        }
        private bool ExistsTrackerData(ViewLinkDataParameter viewParameter, ShowConvertOpFormEventArgs thirdConvertEventArgs)
        {
            if (thirdConvertEventArgs.ReplaceRelations != null && thirdConvertEventArgs.ReplaceRelations.Count > 0)
            {
                return true;
            }
            if (viewParameter.Instances.Count == 0)
            {
                return false;
            }
            string formId = viewParameter.BillInfo.FormId;
            foreach (System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.List<BusinessFlowInstance>> current in viewParameter.Instances)
            {
                string key = current.Key;
                System.Collections.Generic.List<BusinessFlowInstance> value = current.Value;
                TableDefine tableDefine = BusinessFlowServiceHelper.LoadTableDefine(this.View.Context, formId, key);
                System.Collections.Generic.List<BusinessFlowInstance> list = new System.Collections.Generic.List<BusinessFlowInstance>();
                foreach (BusinessFlowInstance current2 in value)
                {
                    if (!this.ValidateInstance(current2, viewParameter.LookUpType, tableDefine.TableNumber))
                    {
                        list.Add(current2);
                    }
                }
                foreach (BusinessFlowInstance current3 in list)
                {
                    value.Remove(current3);
                }
            }
            foreach (System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.List<BusinessFlowInstance>> current4 in viewParameter.Instances)
            {
                System.Collections.Generic.List<BusinessFlowInstance> value2 = current4.Value;
                if (value2.Count > 0)
                {
                    return true;
                }
            }
            return false;
        }
        private bool ValidateInstance(BusinessFlowInstance instance, ViewLinkDataParameter.Enum_LookUpType lookUpType, string tableNumber)
        {
            if (instance.FirstNode == null)
            {
                return false;
            }
            System.Collections.Generic.List<RouteTreeNode> list = instance.SerarchTargetFormNodes(tableNumber);
            if (lookUpType == ViewLinkDataParameter.Enum_LookUpType.Down)
            {
                using (System.Collections.Generic.List<RouteTreeNode>.Enumerator enumerator = list.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        RouteTreeNode current = enumerator.Current;
                        if (current.ChildNodes.Count > 0)
                        {
                            bool result = true;
                            return result;
                        }
                    }
                    return false;
                }
            }
            foreach (RouteTreeNode current2 in list)
            {
                if (current2.ParentNode != null)
                {
                    bool result = true;
                    return result;
                }
            }
            return false;
        }
        private bool ShowLookUpTrackerForm(ViewLinkDataParameter viewParamter, System.Collections.Generic.List<ConvertBillElement> convertBills, ShowConvertOpFormEventArgs thirdConvertEventArgs)
        {
            if (convertBills != null && convertBills.Count > 0)
            {
                System.Collections.Generic.List<DynamicObject> selectedRows = this.GetSelectedRows();
                string key = "LookUpTrackerParam";
                System.Collections.Generic.Dictionary<string, object> dictionary = new System.Collections.Generic.Dictionary<string, object>();
                dictionary["ViewParameter"] = viewParamter;
                dictionary["ConvertBills"] = convertBills;
                dictionary["PlugParam"] = thirdConvertEventArgs;
                this.View.Session[key] = dictionary;
                DynamicFormShowParameter dynamicFormShowParameter = new DynamicFormShowParameter();
                dynamicFormShowParameter.FormId = "BF_LookUpTracker";
                dynamicFormShowParameter.ParentPageId = this.View.PageId;
                dynamicFormShowParameter.OpenStyle.ShowType = ShowType.Default;
                this.View.ShowForm(dynamicFormShowParameter);
                return true;
            }
            return false;
        }
        private bool ShowLookUpTrackerForm1(ViewLinkDataParameter viewParamter, System.Collections.Generic.List<ConvertBillElement> convertBills, ShowConvertOpFormEventArgs thirdConvertEventArgs)
        {
            if (convertBills != null && convertBills.Count > 0)
            {
                System.Collections.Generic.List<DynamicObject> selectedRows = this.GetSelectedRows();
                string key = "LookUpTrackerParam";
                System.Collections.Generic.Dictionary<string, object> dictionary = new System.Collections.Generic.Dictionary<string, object>();
                dictionary["ViewParameter"] = viewParamter;
                dictionary["ConvertBills"] = convertBills;
                dictionary["PlugParam"] = thirdConvertEventArgs;
                this.View.Session[key] = dictionary;
                DynamicFormShowParameter dynamicFormShowParameter = new DynamicFormShowParameter();
                dynamicFormShowParameter.FormId = "keed_SXC";
                dynamicFormShowParameter.ParentPageId = this.View.PageId;
                dynamicFormShowParameter.OpenStyle.ShowType = ShowType.Default;
                dynamicFormShowParameter.CustomComplexParams.Add("GX", selectedRows);
                this.View.ShowForm(dynamicFormShowParameter);
                return true;
            }
            return false;
        }
        private bool CheckPermission(string operation)
        {
            long value = Convert.ToInt64(((DynamicObject)this.Model.GetValue("FProOrgId"))["Id"]);
            string strPerItemId = string.Empty;
            string msg = string.Empty;
            switch (operation)
            {
                case "Filter":
                case "Refresh":
                    return true;
                case "Edit":
                    strPerItemId = "f323992d896745fbaab4a2717c79ce2e";
                    msg = ResManager.LoadKDString("您没有修改的权限，请授权！", "015649000004874", SubSystemType.MFG, new object[0]);
                    goto IL_4D1;
                case "Delete":
                    strPerItemId = "24f64c0dbfa945f78a6be123197a63f5";
                    msg = ResManager.LoadKDString("您没有删除的权限，请授权！", "015649000004875", SubSystemType.MFG, new object[0]);
                    goto IL_4D1;
                case "Submit":
                    strPerItemId = "dd4d4cb1f143409da5777ec417cff26b";
                    msg = ResManager.LoadKDString("您没有提交的权限，请授权！", "015649000004876", SubSystemType.MFG, new object[0]);
                    goto IL_4D1;
                case "CancelAssign":
                    strPerItemId = "4ce350fdd203407cab4939d50f0022cc";
                    msg = ResManager.LoadKDString("您没有撤销的权限，请授权！", "015649000004877", SubSystemType.MFG, new object[0]);
                    goto IL_4D1;
                case "Audit":
                    strPerItemId = "47afe3d45bc84016b416a1206e121d45";
                    msg = ResManager.LoadKDString("您没有审核的权限，请授权！", "015649000004878", SubSystemType.MFG, new object[0]);
                    goto IL_4D1;
                case "UnAudit":
                    strPerItemId = "e4d6cdd9125a4ee5a32a4c27c12dadc9";
                    msg = ResManager.LoadKDString("您没有反审核的权限，请授权！", "015649000004879", SubSystemType.MFG, new object[0]);
                    goto IL_4D1;
                case "TrackDown1":
                    strPerItemId = "85a827bc453e4393b78806ec5c8e2042";
                    msg = ResManager.LoadKDString("您没有下查的权限，请授权！", "015649000004880", SubSystemType.MFG, new object[0]);
                    goto IL_4D1;
                case "TrackDown":
                    strPerItemId = "85a827bc453e4393b78806ec5c8e2042";
                    msg = ResManager.LoadKDString("您没有下查的权限，请授权！", "015649000004880", SubSystemType.MFG, new object[0]);
                    goto IL_4D1;
                case "SplitFirstToLast":
                case "SplitMidToLast":
                case "SplitSelect":
                    strPerItemId = "005056a0395083d011e393bcbb82ef60";
                    msg = ResManager.LoadKDString("您没有拆卡的权限，请授权！", "015649000004881", SubSystemType.MFG, new object[0]);
                    goto IL_4D1;
                case "SimpleDispatching":
                    strPerItemId = "005056a0395083d011e393bcd72b8241";
                    msg = ResManager.LoadKDString("您没有简易派工的权限，请授权！", "015649000004882", SubSystemType.MFG, new object[0]);
                    goto IL_4D1;
                case "TransQtyAdjust":
                    strPerItemId = "005056a03950aa3111e3948d7c977a9f";
                    msg = ResManager.LoadKDString("您没有转移数量的权限，请授权！", "015649000004883", SubSystemType.MFG, new object[0]);
                    goto IL_4D1;
                case "LogQuery":
                    strPerItemId = "005056a03950a14111e39a9ae8cc4c24";
                    msg = ResManager.LoadKDString("您没有日志查询的权限，请授权！", "015649000004884", SubSystemType.MFG, new object[0]);
                    goto IL_4D1;
                case "OperationScheduling":
                    strPerItemId = "00232405fc58883711e328b89640b728";
                    msg = ResManager.LoadKDString("您没有工序排程的权限，请授权！", "015649000004885", SubSystemType.MFG, new object[0]);
                    goto IL_4D1;
                case "ToPlanConfirm":
                    strPerItemId = "00232405fc58883711e328c05799349d";
                    msg = ResManager.LoadKDString("您没有执行至确认的权限，请授权！", "015649000004886", SubSystemType.MFG, new object[0]);
                    goto IL_4D1;
                case "ToRelease":
                    strPerItemId = "00232405fc58883711e328c06e9487cb";
                    msg = ResManager.LoadKDString("您没有执行至下达的权限，请授权！", "015649000004866", SubSystemType.MFG, new object[0]);
                    goto IL_4D1;
                case "ToStart":
                    strPerItemId = "00232405fc58883711e328c0926cdfd5";
                    msg = ResManager.LoadKDString("您没有执行至开工的权限，请授权！", "015649000004867", SubSystemType.MFG, new object[0]);
                    goto IL_4D1;
                case "ToFinish":
                    strPerItemId = "00232405fc58883711e328c0a559c9f2";
                    msg = ResManager.LoadKDString("您没有执行至完工的权限，请授权！", "015649000004868", SubSystemType.MFG, new object[0]);
                    goto IL_4D1;
                case "ToClose":
                    strPerItemId = "00232405fc58883711e328c0b94a15df";
                    msg = ResManager.LoadKDString("您没有执行至关闭的权限，请授权！", "015649000004869", SubSystemType.MFG, new object[0]);
                    goto IL_4D1;
                case "UnDoToPlan":
                    strPerItemId = "00232405fc58883711e328c0ed94d308";
                    msg = ResManager.LoadKDString("您没有反执行至计划的权限，请授权！", "015649000004870", SubSystemType.MFG, new object[0]);
                    goto IL_4D1;
                case "UnDoToPlanConfirm":
                    strPerItemId = "00232405fc58883711e328c103984e0a";
                    msg = ResManager.LoadKDString("您没有反执行至确认的权限，请授权！", "015649000004871", SubSystemType.MFG, new object[0]);
                    goto IL_4D1;
                case "UnDoToRelease":
                    strPerItemId = "00232405fc58883711e328c1110cef67";
                    msg = ResManager.LoadKDString("您没有反执行至下达的权限，请授权！", "015649000004872", SubSystemType.MFG, new object[0]);
                    goto IL_4D1;
            }
            strPerItemId = "6e44119a58cb4a8e86f6c385e14a17ad";
            msg = ResManager.LoadKDString("您没有查看操作的权限，请授权！", "015649000004873", SubSystemType.MFG, new object[0]);
        IL_4D1:
            System.Collections.Generic.List<long> permissionOrg = PermissionServiceHelper.GetPermissionOrg(base.Context, new BusinessObject
            {
                Id = "SFC_ShopWorkBench"
            }, strPerItemId);
            bool flag = permissionOrg.Contains(value);
            if (!flag)
            {
                this.View.ShowMessage(msg, MessageBoxType.Notice);
            }
            return flag;
        }
        private string GetSelectRowInfo(System.Collections.Generic.List<DynamicObject> selectRows)
        {
            System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
            foreach (DynamicObject current in selectRows)
            {
                stringBuilder.AppendFormat(ResManager.LoadKDString("工序计划明细-[{0}] ", "015165030034480", SubSystemType.MFG, new object[0]), string.Format("{0}-{1}-{2}", (current.Parent as DynamicObject)["BillNo"], current["SeqNumber"], current["OperNumber"]));
                stringBuilder.AppendLine();
            }
            return stringBuilder.ToString();
        }
    }
}
