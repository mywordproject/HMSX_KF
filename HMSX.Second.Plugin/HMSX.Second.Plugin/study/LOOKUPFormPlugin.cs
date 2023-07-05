using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.BusinessEntity.BillGlobalParam;
using Kingdee.BOS.BusinessEntity.BillTrack;
using Kingdee.BOS.BusinessEntity.BusinessFlow;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.CommonFilter;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.List.PlugIn;
using Kingdee.BOS.Core.List.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.ConvertElement;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Core.Metadata.FormElement;
using Kingdee.BOS.Core.Objects.Permission.Objects;
using Kingdee.BOS.Core.Permission;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Resource;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace HMSX.Second.Plugin.study
{
    [Description("上下查")]
    [Kingdee.BOS.Util.HotUpdate]
    public class LOOKUPFormPlugin: AbstractDynamicFormPlugIn
    {
		private const string KEY_BillTree = "FBillTree";
		private const string KEY_BillTabControl = "FBillTabControl";
		private const string KEY_TrackQueryType = "FTrackQueryType";
		private Dictionary<string, BillGlobalParameter> _dctFormIdBillGlobalParamMap = new Dictionary<string, BillGlobalParameter>();
		private Dictionary<string, BillNode> _dctFormIdBillNodeMap = new Dictionary<string, BillNode>();
		private Dictionary<string, Tuple<bool, DynamicObjectCollection>> _dctFormIdDataMap = new Dictionary<string, Tuple<bool, DynamicObjectCollection>>();
		private IDynamicFormView _parentFormView;
		private string _formId = string.Empty;
		private LocaleValue _formName;
		private ViewLinkDataParameter _viewParameter;
		private LinkDataInfo _linkDataInfo;
		private ShowConvertOpFormEventArgs _plugEventArgs;
		private TreeNode _currentNode = new TreeNode();
		private Dictionary<string, TreeNode> _treeNodes = new Dictionary<string, TreeNode>();
		private string _tabShowFormId = string.Empty;
		private Dictionary<string, int> _formBillList = new Dictionary<string, int>();
		private bool _isEnablePermission;
		private readonly int _maxShowBillNo = 5;
		private Func<bool> _showLookUpTrackerFormFunc;
		private string _parentErrMsg;
		private Dictionary<string, TableDefine> _dctTableDefine = new Dictionary<string, TableDefine>();
		System.Collections.Generic.List<DynamicObject> GX;
		private List<ConvertBillElement> _optionBills
		{
			get;
			set;
		}
		public override void OnInitialize(InitializeEventArgs e)
		{
			GX = e.Paramter.GetCustomParameter("GX")as System.Collections.Generic.List<DynamicObject>;
			this.View.GetControl<TabControl>("FBillTabControl").SetFireSelChanged(true);
			this._parentFormView = this.View.ParentFormView;
			if (this._parentFormView == null)
			{
				this.View.Close();
				return;
			}
			this._formId = this._parentFormView.BillBusinessInfo.GetForm().Id;
			this._formName = this._parentFormView.BillBusinessInfo.GetForm().Name;
			string key = "LookUpTrackerParam";
			if (this._parentFormView.Session.ContainsKey(key))
			{
				Dictionary<string, object> dictionary = this.View.ParentFormView.Session[key] as Dictionary<string, object>;
				this._viewParameter = (dictionary["ViewParameter"] as ViewLinkDataParameter);
				this._optionBills = (dictionary["ConvertBills"] as List<ConvertBillElement>);
				this._plugEventArgs = (dictionary["PlugParam"] as ShowConvertOpFormEventArgs);
				object obj;
				dictionary.TryGetValue("ShowLookUpTrackerFormFunc", out obj);
				this._showLookUpTrackerFormFunc = (obj as Func<bool>);
				this.View.ParentFormView.Session.Remove(key);
			}
			this.GetTabShowFormId();
			this.BuildLinkDataInfo();
			this._isEnablePermission = SystemParameterServiceHelper.IsEnableBFPermission(base.Context);
		}
		public override void AfterBindData(EventArgs e)
		{
			this.SetFormTitle();
			this.InitAllTrackSettings();
		}
		public override List<TreeNode> GetTreeViewData(TreeNodeArgs treeNodeArgs)
		{
			if (string.IsNullOrEmpty(treeNodeArgs.NodeId) || treeNodeArgs.NodeId == "0")
			{
				List<TreeNode> list = this.BuildTreeNodes();
				if (this._parentErrMsg.IsNullOrEmptyOrWhiteSpace() && (list == null || list.Count == 0))
				{
					this.View.ShowMessage(ResManager.LoadKDString("没有关联业务数据!", "002012030021061", SubSystemType.BOS, new object[0]), MessageBoxType.Notice);
				}
				this._currentNode = null;
				if (!this._tabShowFormId.IsNullOrEmptyOrWhiteSpace() && this._treeNodes.TryGetValue(this._tabShowFormId, out this._currentNode))
				{
					TreeView control = this.View.GetControl<TreeView>("FBillTree");
					control.Select(this._tabShowFormId);
					this.ShowBillListPage(this._tabShowFormId);
				}
				return list;
			}
			return null;
		}
		public override void TreeNodeClick(TreeNodeArgs e)
		{
			if (!this._treeNodes.ContainsKey(e.NodeId))
			{
				return;
			}
			this._currentNode = this._treeNodes[e.NodeId];
			this.ShowBillListPage(e.NodeId);
		}
		public override void TreeNodeDoubleClick(TreeNodeArgs e)
		{
			if (e.NodeId == "0")
			{
				return;
			}
			this.ShowBillListPage(e.NodeId);
		}
		public override void TabItemSelectedChange(TabItemSelectedChangeEventArgs e)
		{
			if (this._formBillList.ContainsValue(e.TabIndex) && (!this._formBillList.ContainsKey(this._currentNode.id) || this._formBillList[this._currentNode.id] != e.TabIndex))
			{
				this._tabShowFormId = this._formBillList.First((KeyValuePair<string, int> p) => p.Value == e.TabIndex).Key;
			}
		}
		public override void BarItemClick(BarItemClickEventArgs e)
		{
			base.BarItemClick(e);
			string barItemKey;
			if ((barItemKey = e.BarItemKey) != null)
			{
				if (!(barItemKey == "tbBtnReload"))
				{
					return;
				}
				this.RefreshLookUpTrackerForm();
			}
		}
		private void RefreshLookUpTrackerForm()
		{
			this._parentErrMsg = string.Empty;
			if (this._showLookUpTrackerFormFunc == null)
			{
				return;
			}
			this.View.ParentFormView.ConcurrentSession["_TrackerFormIdParam"] = this._tabShowFormId;
			this.View.ParentFormView.ConcurrentSession["_TrackIsShowErrMsg"] = false;
			if (this._showLookUpTrackerFormFunc())
			{
				this._parentErrMsg = string.Empty;
				this.View.Close();
				this.View.SendDynamicFormAction(this.View.ParentFormView);
				return;
			}
			if (this._optionBills != null)
			{
				this._optionBills.Clear();
			}
			if (this._treeNodes != null)
			{
				this._treeNodes.Clear();
			}
			TreeView control = this.View.GetControl<TreeView>("FBillTree");
			if (control != null)
			{
				control.TreeNodes.Clear();
				control.SetNodes("0", control.TreeNodes);
				this.View.UpdateView("FBillTree");
			}
			TabControl control2 = this.View.GetControl<TabControl>("FBillTabControl");
			if (control2 != null)
			{
				control2.Visible = false;
				this.View.UpdateView("FBillTabControl");
				control2.HideTabItem("FBillTabControl");
				control2.CloseWebTabPage(0);
			}
			this.GetTabShowFormId();
			this.InitAllTrackSettings();
			object obj;
			this.View.ParentFormView.ConcurrentSession.TryRemove("_TrackErrMsg", out obj);
			if (obj != null)
			{
				this._parentErrMsg = Convert.ToString(obj);
				this.View.ShowMessage(this._parentErrMsg, MessageBoxType.Notice);
			}
		}
		private void GetTabShowFormId()
		{
			if (this._parentFormView != null && this._parentFormView.ConcurrentSession != null)
			{
				object value;
				this._parentFormView.ConcurrentSession.TryRemove("_TrackerFormIdParam", out value);
				this._tabShowFormId = Convert.ToString(value);
			}
		}
		private List<TreeNode> BuildTreeNodes()
		{
			List<TreeNode> list = new List<TreeNode>();
			if (this._optionBills == null)
			{
				return list;
			}
			foreach (ConvertBillElement current in this._optionBills)
			{
				bool flag = true;
				TreeNode treeNode = new TreeNode();
				treeNode.xtype = "leaf";
				treeNode.id = current.FormID;
				treeNode.text = this.BuildTreeNodeName(current, ref flag);
				treeNode.children = new List<TreeNode>();
				if (flag)
				{
					list.Add(treeNode);
				}
				this._treeNodes[current.FormID] = treeNode;
			}
			return list;
		}
		private void BuildLinkDataInfo()
		{
			this._linkDataInfo = new LinkDataInfo();
			if (this._viewParameter == null || this._viewParameter.Instances == null)
			{
				return;
			}
			foreach (KeyValuePair<string, List<BusinessFlowInstance>> current in this._viewParameter.Instances)
			{
				List<BusinessFlowInstance> value = current.Value;
				foreach (BusinessFlowInstance current2 in value)
				{
					if (current2.FirstNode != null)
					{
						List<RouteTreeNode> list = new List<RouteTreeNode>();
						this.GetAllChildNode(current2.FirstNode, ref list);
						foreach (RouteTreeNode current3 in list)
						{
							if (this._viewParameter.LookUpType == ViewLinkDataParameter.Enum_LookUpType.Down)
							{
								this.AddNodeToLinkDataInfo(current3, current3.ParentNode);
							}
							else
							{
								foreach (RouteTreeNode current4 in current3.ChildNodes)
								{
									if (this.AddNodeToLinkDataInfo(current3, current4))
									{
										break;
									}
								}
							}
						}
					}
				}
			}
		}
		private bool AddNodeToLinkDataInfo(RouteTreeNode node, RouteTreeNode linkNode)
		{
			if (linkNode == null)
			{
				return false;
			}
			if (this._viewParameter.LookUpType == ViewLinkDataParameter.Enum_LookUpType.Down && node.IsBreak)
			{
				return false;
			}
			string tbl = linkNode.Id.Tbl;
			long eId = linkNode.Id.EId;
			string str = "";
			string entityKey = "";
			if (!this.TryGetFormIdByTable(tbl, out str, out entityKey))
			{
				return false;
			}
			if (!str.EqualsIgnoreCase(this._viewParameter.BillInfo.FormId))
			{
				return false;
			}
			if (!this.IsLinkSelRow(eId, entityKey))
			{
				return false;
			}
			if (this._viewParameter.LookUpType == ViewLinkDataParameter.Enum_LookUpType.Up && linkNode.IsBreak)
			{
				return false;
			}
			string tbl2 = node.Id.Tbl;
			long eId2 = node.Id.EId;
			long eId3 = linkNode.Id.EId;
			string formId = "";
			string entityKey2 = "";
			if (this.TryGetFormIdByTable(tbl2, out formId, out entityKey2))
			{
				if(ISBill(eId2, formId))
                {
					if (this.IsCurrBill(eId2, eId3, formId, entityKey2))
					{
						return true;
					}
					this._linkDataInfo.AddRowInfo(formId, entityKey2, eId2);
				}
				
			}
			return true;
		}
		private bool IsCurrBill(long entityId, long entityParentId, string formId, string entityKey)
		{
			if (this._viewParameter.BillInfo.FormId.EqualsIgnoreCase(formId))
			{
				if (this._viewParameter.BillInfo.Rows.Any((ViewLinkDataRowInfo t) => t.EntityKey.EqualsIgnoreCase(entityKey) && t.EntityId == entityId))
				{
					if (this._viewParameter.BillInfo.Rows.Any((ViewLinkDataRowInfo t) => t.EntityKey.EqualsIgnoreCase(entityKey) && t.EntityId == entityParentId))
					{
						return false;
					}
				}
				if (this._viewParameter.BillInfo.Rows.Any((ViewLinkDataRowInfo t) => t.EntityKey.EqualsIgnoreCase(entityKey) && t.EntityId == entityId))
				{
					return true;
				}
			}
			return false;
		}
		private bool IsLinkSelRow(long eId, string entityKey)
		{
			return this._viewParameter.BillInfo.Rows.Any((ViewLinkDataRowInfo t) => t.EntityKey.EqualsIgnoreCase(entityKey) && t.EntityId == eId);
		}
		private void GetAllChildNode(RouteTreeNode parentNode, ref List<RouteTreeNode> allNodes)
		{
			allNodes.Add(parentNode);
			foreach (RouteTreeNode current in parentNode.ChildNodes)
			{
				if (!allNodes.Contains(current))
				{
					this.GetAllChildNode(current, ref allNodes);
				}
			}
		}
		private void SetFormTitle()
		{
			LocaleValue localeValue = new LocaleValue();
			LocaleValue localeValue2 = null;
			if (this._viewParameter != null)
			{
				if (this._viewParameter.LookUpType == ViewLinkDataParameter.Enum_LookUpType.Down)
				{
					localeValue2 = new LocaleValue(ResManager.LoadKDString("下查", "002546030019387", SubSystemType.BOS, new object[0]), base.Context.UserLocale.LCID);
				}
				else
				{
					localeValue2 = new LocaleValue(ResManager.LoadKDString("上查", "002546030019390", SubSystemType.BOS, new object[0]), base.Context.UserLocale.LCID);
				}
			}
			localeValue.Merger(this._formName, "", true);
			localeValue.Merger(localeValue2, " - ", true);
			this.View.SetFormTitle(localeValue);
		}
		private bool PreLoadThirdPush(string formId)
		{
			BillGlobalParameter billGlobalParameter = null;
			if (!this._dctFormIdBillGlobalParamMap.TryGetValue(formId, out billGlobalParameter))
			{
				DynamicObject[] array = SystemParameterServiceHelper.LoadBillGlobalParameter(base.Context, new string[]
				{
					formId
				});
				if (array == null || !array.Any<DynamicObject>())
				{
					this._dctFormIdBillGlobalParamMap[formId] = null;
				}
				else
				{
					billGlobalParameter = (this._dctFormIdBillGlobalParamMap[formId] = new BillGlobalParameter(array[0]));
				}
			}
			return billGlobalParameter != null && billGlobalParameter.PreLoadThirdPush;
		}
		private void BuildTreeNodeNameByParam(ConvertBillElement bill, ref bool bAdd, ref string name)
		{
			if (this.PreLoadThirdPush(this._formId))
			{
				BillNode billNode;
				if (this._viewParameter.LookUpType == ViewLinkDataParameter.Enum_LookUpType.Down)
				{
					billNode = this.BuildTargetNode(bill.FormID);
				}
				else
				{
					billNode = this.BuildSourceNode(bill.FormID);
				}
				if (!this.ExistsTrackerData(billNode))
				{
					bAdd = false;
				}
				else
				{
					name = string.Format("{0}({1})", bill.Name.ToString(), billNode.LinkIds.Count);
				}
				this._dctFormIdBillNodeMap[bill.FormID] = billNode;
			}
		}
		private string BuildTreeNodeName(ConvertBillElement bill, ref bool bAdd)
		{
			string text = bill.Name.ToString();
			 if (this.IsReplaceRelation(bill))
			{
				text = string.Format("{0}(*)", text);
				this.BuildTreeNodeNameByParam(bill, ref bAdd, ref text);
			}
			else
			{
				int billLinkRowCount = this._linkDataInfo.GetBillLinkRowCount(bill.FormID);
				if (billLinkRowCount == 0)
				{
					bAdd = false;
				}
				text = string.Format("{0}({1})", text, billLinkRowCount);
			}
			return text;
		}
		private bool IsReplaceRelation(ConvertBillElement bill)
		{
			if (this._plugEventArgs.ReplaceRelations != null)
			{
				return this._plugEventArgs.ReplaceRelations.Any((ReplaceRelation t) => t.TargetFormId.EqualsIgnoreCase(bill.FormID));
			}
			return false;
		}
		private void ShowBillListPage(string formId)
		{
			if (!this._treeNodes.TryGetValue(formId, out this._currentNode))
			{
				return;
			}
			if (this._currentNode.xtype.Equals("leaf"))
			{
				this._tabShowFormId = formId;
				if (this._formBillList.ContainsKey(formId))
				{
					this.View.GetControl<TabControl>("FBillTabControl").SelectedIndex = this._formBillList[formId];
					return;
				}
				this.OpenNewBillPage(formId);
			}
		}
		private void OpenNewBillPage(string formId)
		{
			this.CheckViewPermission(formId);
			this.DoTrack(formId);
		}
		private void DoTrack(string formId)
		{
			string empty = string.Empty;
			BillNode billNode = null;
			if (this._dctFormIdBillNodeMap.ContainsKey(formId))
			{
				this._dctFormIdBillNodeMap.TryGetValue(formId, out billNode);
			}
			else
			{
				if (this._viewParameter.LookUpType == ViewLinkDataParameter.Enum_LookUpType.Down)
				{
					billNode = this.BuildTargetNode(formId);
				}
				else
				{
					billNode = this.BuildSourceNode(formId);
				}
				this._dctFormIdBillNodeMap[formId] = billNode;
			}
			if (!this.ExistsTrackerData(billNode))
			{
				this.View.ShowMessage(ResManager.LoadKDString("没有业务数据发生!", "002546030019393", SubSystemType.BOS, new object[0]), MessageBoxType.Notice);
				return;
			}
			bool flag = true;
			string empty2 = string.Empty;
			BusinessInfo businessInfo = this.LoadBusinessInfo(formId);
			QueryBuilderParemeter queryBuilderParemeter = this.BuildTrackFilter(businessInfo, billNode, ref empty, ref flag, ref empty2);
			if (!flag)
			{
				this.View.ShowMessage(ResManager.LoadKDString("单据不存在，可能已经被清理！", "002546000003783", SubSystemType.BOS, new object[0]), MessageBoxType.Notice);
				return;
			}
			DynamicObjectCollection dynObjs = null;
			if (!this.ExistsTrackerDataExt(businessInfo, queryBuilderParemeter, empty2, ref dynObjs))
			{
				this.View.ShowMessage(ResManager.LoadKDString("单据不存在，可能已经被清理！", "002546000003783", SubSystemType.BOS, new object[0]), MessageBoxType.Notice);
				return;
			}
			if (this._isEnablePermission)
			{
				queryBuilderParemeter = this.FilterByDataPermission(businessInfo, dynObjs, empty2);
			}
			ListTrackBillShowParameter param = this.BuildListShowParameter(formId, empty, queryBuilderParemeter);
			this.View.ShowForm(param, new Action<FormResult>(this.CloseFun));
			this._formBillList.Add(formId, this._formBillList.Count);
		}
		private bool ExistsTrackerDataExt(BusinessInfo bInfo, QueryBuilderParemeter queryParameter, string entityPKName, ref DynamicObjectCollection dynObjs)
		{
			Tuple<bool, DynamicObjectCollection> tuple = null;
			string id = bInfo.GetForm().Id;
			if (this._dctFormIdDataMap.TryGetValue(id, out tuple))
			{
				dynObjs = tuple.Item2;
				return tuple.Item1;
			}
			dynObjs = QueryServiceHelper.GetDynamicObjectCollection(base.Context, queryParameter, null);
			bool flag = dynObjs != null && dynObjs.Count<DynamicObject>() > 0;
			this._dctFormIdDataMap[id] = Tuple.Create<bool, DynamicObjectCollection>(flag, dynObjs);
			return flag;
		}
		private bool ExistsTrackerData(BillNode targetNode)
		{
			return targetNode != null && targetNode.LinkIds != null && targetNode.LinkIds.Count != 0;
		}
		private BillNode BuildTargetNode(string formId)
		{
			BillNode targetNode = BillNode.Create(formId, "", null);
			BillNode billNode = null;
			if (this._plugEventArgs.ReplaceRelations != null)
			{
				ReplaceRelation replaceRelation = this._plugEventArgs.ReplaceRelations.FirstOrDefault((ReplaceRelation o) => o.TargetFormId == targetNode.FormKey);
				if (replaceRelation != null && replaceRelation.SourceFormId != replaceRelation.TargetFormId && replaceRelation.TargetFormId == targetNode.FormKey)
				{
					billNode = BillNode.BuildCurrentNode(replaceRelation.SourceLinkId, replaceRelation.SourceFormId, false);
				}
			}
			bool flag = false;
			if (billNode == null)
			{
				LinkBillInfo billInfo = this._linkDataInfo.GetBillInfo(formId);
				using (List<LinkEntityInfo>.Enumerator enumerator = billInfo.Entities.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						LinkEntityInfo current = enumerator.Current;
						if (current.EntityIds.Count > 0)
						{
							targetNode.TrackUpDownLinkEntry = current.EntityKey;
							targetNode.AddLinkCopyData((
								from p in current.EntityIds
								select p.ToString()).ToList<string>());
							flag = true;
							break;
						}
					}
					goto IL_1B3;
				}
			}
			billNode.IsLinkCopy = true;
			billNode.NodeDirection = 2;
			IOperationResult operationResult = this.ExpandTrackNode(this._parentFormView, billNode, targetNode);
			if (operationResult.IsSuccess)
			{
				targetNode = billNode.GetNextNodes().FirstOrDefault((BillNode o) => o.FormKey == formId);
				flag = true;
			}
			else
			{
				targetNode = null;
			}
		IL_1B3:
			if (flag)
			{
				ShowTrackResultEventArgs showTrackResultEventArgs = new ShowTrackResultEventArgs(FormOperationEnum.TrackDown, targetNode);
				showTrackResultEventArgs.TargetFormKey = formId;
				showTrackResultEventArgs.ReplaceRelations = this._plugEventArgs.ReplaceRelations;
				DynamicFormViewPlugInProxy parentFormViewProxy = this.GetParentFormViewProxy();
				if (this._parentFormView is IListView)
				{
					((ListViewPlugInProxy)parentFormViewProxy).FireOnShowTrackResult(showTrackResultEventArgs);
				}
				else
				{
					if (this._parentFormView is IBillView)
					{
						((BillViewPlugInProxy)parentFormViewProxy).FireOnShowTrackResult(showTrackResultEventArgs);
					}
				}
				if (showTrackResultEventArgs.Cancel || showTrackResultEventArgs.TrackResult == null)
				{
					return null;
				}
				targetNode = (showTrackResultEventArgs.TrackResult as BillNode);
			}
			return targetNode;
		}
		private BillNode BuildSourceNode(string formId)
		{
			string sourceFormKey = formId;
			BillNode sourceNode = BillNode.Create(sourceFormKey, "", null);
			if (this._plugEventArgs.ReplaceRelations != null)
			{
				ReplaceRelation replaceRelation = this._plugEventArgs.ReplaceRelations.FirstOrDefault((ReplaceRelation o) => o.TargetFormId == sourceNode.FormKey);
				if (replaceRelation != null && replaceRelation.SourceFormId != replaceRelation.TargetFormId && replaceRelation.TargetFormId == sourceNode.FormKey)
				{
					sourceFormKey = replaceRelation.SourceFormId;
					sourceNode.FormKey = sourceFormKey;
				}
			}
			if (sourceNode.FormKey.ToString().EqualsIgnoreCase(formId))
			{
				LinkBillInfo billInfo = this._linkDataInfo.GetBillInfo(formId);
				using (List<LinkEntityInfo>.Enumerator enumerator = billInfo.Entities.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						LinkEntityInfo current = enumerator.Current;
						if (current.EntityIds.Count > 0)
						{
							sourceNode.TrackUpDownLinkEntry = current.EntityKey;
							sourceNode.AddLinkCopyData((
								from p in current.EntityIds
								select p.ToString()).ToList<string>());
							break;
						}
					}
					goto IL_2C0;
				}
			}
			Dictionary<string, List<string>> dictionary = new Dictionary<string, List<string>>();
			foreach (ViewLinkDataRowInfo current2 in this._viewParameter.BillInfo.Rows)
			{
				List<string> list = null;
				if (!dictionary.TryGetValue(current2.EntityKey, out list))
				{
					list = new List<string>();
					dictionary.Add(current2.EntityKey, list);
				}
				list.Add(current2.EntityId.ToString());
			}
			foreach (KeyValuePair<string, List<string>> current3 in dictionary)
			{
				string key = current3.Key;
				List<string> value = current3.Value;
				if (value.Count != 0)
				{
					BillNode billNode = BillNode.BuildCurrentNode(value, this._viewParameter.BillInfo.FormId, false);
					billNode.LinkEntry = key;
					billNode.IsLinkCopy = true;
					billNode.NodeDirection = 1;
					IOperationResult operationResult = this.ExpandTrackNode(this._parentFormView, billNode, sourceNode);
					if (operationResult.IsSuccess)
					{
						BillNode billNode2 = billNode.GetPreviousNodes().FirstOrDefault((BillNode o) => o.FormKey == sourceFormKey);
						if (billNode2 != null)
						{
							sourceNode = billNode2;
							break;
						}
					}
				}
			}
		IL_2C0:
			if (sourceNode != null && sourceNode.LinkIds != null && sourceNode.LinkIds.Count > 0)
			{
				ShowTrackResultEventArgs showTrackResultEventArgs = new ShowTrackResultEventArgs(FormOperationEnum.TrackUp, sourceNode);
				showTrackResultEventArgs.TargetFormKey = formId;
				showTrackResultEventArgs.ReplaceRelations = this._plugEventArgs.ReplaceRelations;
				DynamicFormViewPlugInProxy parentFormViewProxy = this.GetParentFormViewProxy();
				if (this._parentFormView is IListView)
				{
					((ListViewPlugInProxy)parentFormViewProxy).FireOnShowTrackResult(showTrackResultEventArgs);
				}
				else
				{
					if (this._parentFormView is IBillView)
					{
						((BillViewPlugInProxy)parentFormViewProxy).FireOnShowTrackResult(showTrackResultEventArgs);
					}
				}
				if (showTrackResultEventArgs.Cancel)
				{
					return null;
				}
				sourceNode = (showTrackResultEventArgs.TrackResult as BillNode);
			}
			return sourceNode;
		}
		private IOperationResult ExpandTrackNode(IDynamicFormView view, BillNode trackNode, BillNode targetNode = null)
		{
			IOperationResult operationResult = null;
			if (trackNode == null)
			{
				operationResult.IsSuccess = false;
				operationResult.IsShowMessage = false;
				return operationResult;
			}
			string operationNumber = "TrackUp";
			if (trackNode.NodeDirection == 2)
			{
				operationNumber = "TrackDown";
			}
			if (trackNode.IsLinkCopy)
			{
				if (targetNode == null)
				{
					operationResult = BusinessDataServiceHelper.TrackLinkCopy(view.Context, trackNode, operationNumber, view.BillBusinessInfo, true);
				}
				else
				{
					operationResult = BusinessDataServiceHelper.TrackLinkCopy(view.Context, trackNode, targetNode, operationNumber, view.BillBusinessInfo);
				}
			}
			else
			{
				operationResult = BusinessDataServiceHelper.Track(view.Context, trackNode, operationNumber, view.BillBusinessInfo, false, true);
			}
			return operationResult;
		}
		private QueryBuilderParemeter BuildTrackFilter(BusinessInfo businessInfo, BillNode targetNode, ref string selectEntity, ref bool bQuery, ref string selectEntityPKName)
		{
			selectEntity = ((targetNode == null) ? string.Empty : targetNode.TrackUpDownLinkEntry);
			if (string.IsNullOrWhiteSpace(selectEntity))
			{
				if (businessInfo.GetForm().LinkSet.LinkEntitys.Count > 0)
				{
					selectEntity = businessInfo.GetForm().LinkSet.LinkEntitys.FirstOrDefault<LinkEntity>().ParentEntityKey;
				}
				if (string.IsNullOrWhiteSpace(selectEntity))
				{
					selectEntity = targetNode.LinkEntry;
				}
			}
			Entity entity = businessInfo.GetEntity(selectEntity);
			if (entity == null)
			{
				bQuery = false;
				return null;
			}
			if (targetNode == null || targetNode.LinkIds == null || targetNode.LinkIds.Count <= 0)
			{
				bQuery = false;
				return null;
			}
			List<string> linkIds = targetNode.LinkIds;
			string text = "";
			if (entity is HeadEntity || entity is SubHeadEntity)
			{
				text = businessInfo.GetForm().PkFieldName;
			}
			else
			{
				text = entity.Key + "_" + entity.EntryPkFieldName;
			}
			string pkFieldName = businessInfo.GetForm().PkFieldName;
			QueryBuilderParemeter queryBuilderParemeter = new QueryBuilderParemeter();
			SelectorItemInfo item = new SelectorItemInfo(pkFieldName);
			SelectorItemInfo item2 = new SelectorItemInfo(text);
			queryBuilderParemeter.FormId = businessInfo.GetForm().Id;
			queryBuilderParemeter.SelectItems.Add(item);
			queryBuilderParemeter.SelectItems.Add(item2);
			if (linkIds.Count <= 50)
			{
				queryBuilderParemeter.FilterClauseWihtKey = string.Format(" {0} IN ({1}) ", text, string.Join(",", linkIds));
			}
			else
			{
				List<long> list = new List<long>();
				foreach (string current in linkIds)
				{
					long item3 = Convert.ToInt64(current);
					if (!list.Contains(item3))
					{
						list.Add(item3);
					}
				}
				string sqlWithCardinality = StringUtils.GetSqlWithCardinality(list.Count, "@TrackPKValue", 1, true);
				ExtJoinTableDescription item4 = new ExtJoinTableDescription
				{
					TableName = sqlWithCardinality,
					TableNameAs = "sp",
					FieldName = "FID",
					ScourceKey = text
				};
				queryBuilderParemeter.ExtJoinTables.Add(item4);
				queryBuilderParemeter.SqlParams.Add(new SqlParam("@TrackPKValue", KDDbType.udt_inttable, list.ToArray()));
			}
			selectEntityPKName = text;
			return queryBuilderParemeter;
		}
		private ListTrackBillShowParameter BuildListShowParameter(string formId, string selectEntity, QueryBuilderParemeter queryBuilderParemeter)
		{
			IRegularFilterParameter regularFilterParameter = new ListRegularFilterParameter();
			regularFilterParameter.SelectEntitys = new List<string>
			{
				selectEntity
			};
			regularFilterParameter.Filter = queryBuilderParemeter.FilterClauseWihtKey;
			ListTrackBillShowParameter listTrackBillShowParameter = new ListTrackBillShowParameter
			{
				FormId = formId,
				IsShowFilter = false,
				ListFilterParameter = regularFilterParameter
			};
			if (queryBuilderParemeter.ExtJoinTables != null)
			{
				listTrackBillShowParameter.ExtJoinTables.AddRange(queryBuilderParemeter.ExtJoinTables);
			}
			if (queryBuilderParemeter.SqlParams != null)
			{
				listTrackBillShowParameter.SqlParams.AddRange(queryBuilderParemeter.SqlParams);
			}
			listTrackBillShowParameter.MutilListUseOrgId = string.Join<long>(",", OrganizationServiceHelper.GetOrgIdsByFuncId(base.Context, null));
			listTrackBillShowParameter.ParentPageId = this.View.PageId;
			listTrackBillShowParameter.PageId = formId + Guid.NewGuid();
			listTrackBillShowParameter.OpenStyle.CacheId = listTrackBillShowParameter.PageId;
			listTrackBillShowParameter.OpenStyle.TagetKey = "FBillTabControl";
			listTrackBillShowParameter.OpenStyle.ShowType = ShowType.NewTabPage;
			listTrackBillShowParameter.CustomParams.Add("OpenSource", "Track");
			return listTrackBillShowParameter;
		}
		private void CloseFun(FormResult data)
		{
			if (this._formBillList.ContainsKey(this._tabShowFormId))
			{
				this._formBillList.Remove(this._tabShowFormId);
				string[] array = (
					from p in this._formBillList
					orderby p.Value
					select p.Key).ToArray<string>();
				for (int i = 0; i < array.Length; i++)
				{
					this._formBillList[array[i]] = i;
					if (i == array.Length - 1)
					{
						this.ShowBillListPage(array[i]);
					}
				}
			}
		}
		private void InitAllTrackSettings()
		{
			string text = string.Empty;
			string text2 = string.Empty;
			if (this._parentFormView != null)
			{
				LocaleValue formTitle = this._parentFormView.GetFormTitle();
				IListView listView = this.View.ParentFormView as IListView;
				if (listView != null)
				{
					if (listView.SelectedRowsInfo != null && listView.SelectedRowsInfo.Count > 0)
					{
						List<string> list = (
							from item in listView.SelectedRowsInfo
							select item.BillNo).Distinct<string>().ToList<string>();
						text2 = string.Format("{0}: {1}", formTitle, string.Join(", ", list));
						if (list.Count > this._maxShowBillNo)
						{
							IEnumerable<string> values = list.Take(this._maxShowBillNo);
							text = string.Format(ResManager.LoadKDString("{0}: {1}...共{2}个单据", "002546000004197", SubSystemType.BOS, new object[0]), formTitle, string.Join(", ", values), list.Count);
						}
						else
						{
							text = text2;
						}
					}
					else
					{
						text2 = string.Format("{0}: ", formTitle);
						text = text2;
					}
				}
				else
				{
					string arg = string.Empty;
					Field billNoField = this.View.ParentFormView.Model.BusinessInfo.GetBillNoField();
					if (billNoField != null)
					{
						arg = Convert.ToString(this.View.ParentFormView.Model.GetValue(billNoField));
					}
					text = string.Format("{0}: {1}", formTitle, arg);
				}
			}
			Control control = this.View.GetControl("FTitleLabel");
			if (control != null)
			{
				control.Text = text;
				control.SetToolTip(text2);
			}
		}
		private void SetBarItemVisible(string barItemName, bool isShow)
		{
			BarItemControl mainBarItem = this.View.GetMainBarItem(barItemName);
			if (mainBarItem == null)
			{
				return;
			}
			mainBarItem.Visible = isShow;
		}
		private bool CheckPermission(string formId, string perItemId = "6e44119a58cb4a8e86f6c385e14a17ad")
		{
			if (string.IsNullOrEmpty(formId))
			{
				return false;
			}
			PermissionAuthResult permissionAuthResult = PermissionServiceHelper.FuncPermissionAuth(this.View.Context, new BusinessObject
			{
				Id = formId
			}, perItemId);
			return permissionAuthResult.Passed;
		}
		private DynamicFormViewPlugInProxy GetParentFormViewProxy()
		{
			return this._parentFormView.GetService<DynamicFormViewPlugInProxy>();
		}
		private BusinessInfo LoadBusinessInfo(string formId)
		{
			AbstractBusinessMetadata abstractBusinessMetadata = MetaDataServiceHelper.Load(this.View.Context, formId, true);
			return ((FormMetadata)abstractBusinessMetadata).BusinessInfo;
		}
		private bool TryGetFormIdByTable(string tableNumber, out string formId, out string entityKey)
		{
			formId = "";
			entityKey = "";
			if (string.IsNullOrWhiteSpace(tableNumber))
			{
				return false;
			}
			TableDefine tableDefine = null;
			if (!this._dctTableDefine.TryGetValue(tableNumber, out tableDefine))
			{
				tableDefine = BusinessFlowServiceHelper.LoadTableDefine(this.View.Context, tableNumber);
				this._dctTableDefine.Add(tableNumber, tableDefine);
			}
			if (tableDefine == null)
			{
				return false;
			}
			formId = tableDefine.FormId;
			entityKey = tableDefine.EntityKey;
			return true;
		}
		private void CheckViewPermission(string formId)
		{
			if (!this._isEnablePermission)
			{
				return;
			}
			Form form = this.LoadBusinessInfo(formId).GetForm();
			if (form.SupportPermissionControl == 1)
			{
				BusinessObject businessObject = new BusinessObject();
				businessObject.Id = form.Id;
				businessObject.PermissionControl = form.SupportPermissionControl;
				FormOperation operation = form.GetOperation(FormOperationEnum.View.ToString());
				if (operation != null && !operation.PermissionItemId.IsEmpty())
				{
					PermissionAuthResult permissionAuthResult = PermissionServiceHelper.FuncPermissionAuth(base.Context, businessObject, operation.PermissionItemId);
					if (!permissionAuthResult.Passed)
					{
						string message = string.Format(ResManager.LoadKDString("您没有{0}的查看权限。", "002013000004728", SubSystemType.BOS, new object[0]), form.Name.ToString());
						throw new KDException("", message);
					}
				}
			}
		}
		private QueryBuilderParemeter FilterByDataPermission(BusinessInfo bInfo, DynamicObjectCollection dynObjs, string entityPkFieldNameAlias)
		{
			string pkFieldName = bInfo.GetForm().PkFieldName;
			List<string> pkIds = (
				from x in dynObjs
				select ObjectUtils.Object2String(x[pkFieldName])).ToList<string>();
			FilterObjectByDataRuleParamenter filterObjectByDataRuleParamenter = new FilterObjectByDataRuleParamenter(bInfo, pkIds);
			filterObjectByDataRuleParamenter.IsLookUp = false;
			filterObjectByDataRuleParamenter.PermissionItemId = "6e44119a58cb4a8e86f6c385e14a17ad";
			List<string> idsPermssion = PermissionServiceHelper.FilterObjectByDataRule(base.Context, filterObjectByDataRuleParamenter);
			if (!pkFieldName.EqualsIgnoreCase(entityPkFieldNameAlias) && idsPermssion != null && idsPermssion.Count > 0)
			{
				idsPermssion = (
					from x in dynObjs
					where idsPermssion.Contains(ObjectUtils.Object2String(x[pkFieldName]))
					select ObjectUtils.Object2String(x[entityPkFieldNameAlias])).ToList<string>();
			}
			return this.BuildNewFilterParam(bInfo, entityPkFieldNameAlias, idsPermssion);
		}
		private QueryBuilderParemeter BuildNewFilterParam(BusinessInfo businessInfo, string pkKey, List<string> ids)
		{
			QueryBuilderParemeter queryBuilderParemeter = new QueryBuilderParemeter();
			queryBuilderParemeter.FormId = businessInfo.GetForm().Id;
			if (ids == null)
			{
				ids = new List<string>();
			}
			string filterClauseWihtKey = string.Empty;
			string[] array = ids.Distinct<string>().ToArray<string>();
			long[] array2 = new long[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array2[i] = ObjectUtils.Object2Int64(array[i], 0L);
			}
			if (array2.Length == 0)
			{
				filterClauseWihtKey = " 1 != 1 ";
			}
			else
			{
				if (array2.Length == 1)
				{
					filterClauseWihtKey = string.Format(" {0} = {1} ", pkKey, array2[0]);
				}
				else
				{
					if (array2.Length <= 50)
					{
						filterClauseWihtKey = string.Format(" {0} IN ({1}) ", pkKey, string.Join<long>(",", array2));
					}
					else
					{
						string sqlWithCardinality = StringUtils.GetSqlWithCardinality(array2.Length, "@TrackPKValue", 1, true);
						ExtJoinTableDescription item = new ExtJoinTableDescription
						{
							TableName = sqlWithCardinality,
							TableNameAs = "tsp",
							FieldName = "FID",
							ScourceKey = pkKey
						};
						queryBuilderParemeter.ExtJoinTables.Add(item);
						queryBuilderParemeter.SqlParams.Add(new SqlParam("@TrackPKValue", KDDbType.udt_inttable, array2));
					}
				}
			}
			queryBuilderParemeter.FilterClauseWihtKey = filterClauseWihtKey;
			return queryBuilderParemeter;
		}
		public bool ISBill(long id ,string formid)
        {
			string str = "";
			foreach(var gxjh in GX)
            {
				str += gxjh["OperId"].ToString() + ",";
            }
            switch (formid)
            {
				case "SFC_OperationReport":
					string gxhbsql = $@"/*dialect*/SELECT B.FENTRYID FROM T_SFC_OPERPLANNINGDETAIL A
                       LEFT JOIN T_SFC_OPTRPTENTRY B ON A.FDetailID=B.FOPTPLANOPTID
                       WHERE A.FDetailID in ({str.Trim(',')}) and B.FENTRYID={id}";
					var gxhb = DBUtils.ExecuteDynamicObject(Context, gxhbsql);
					if (gxhb.Count > 0)
					{
						return true;
					}
                    else
                    {
						return false;
					}
				case "SFC_OperationTransfer":
					string gxzysql = $@"/*dialect*/SELECT B.FID FROM T_SFC_OPERPLANNINGDETAIL A
                       LEFT JOIN T_SFC_OPERATIONTRANSFER B ON A.FDetailID=B.FSRCOPTPLANOPTID
                       WHERE A.FDetailID in ({str.Trim(',')}) and B.FID={id}";
					var gxzy = DBUtils.ExecuteDynamicObject(Context, gxzysql);
					if (gxzy.Count > 0)
					{
						return true;
					}
					else
					{
						return false;
					}
                case "SFC_DispatchDetail":
                    string pgmxsql = $@"/*dialect*/SELECT D.* FROM T_SFC_OPERPLANNINGDETAIL A
                        LEFT JOIN T_SFC_OPERPLANNINGSEQ B ON A.FENTRYID=B.FENTRYID
                        LEFT JOIN T_SFC_OPERPLANNING C ON C.FID=B.FID
                        LEFT JOIN T_SFC_DISPATCHDETAIL D ON B.FSEQNUMBER=D.FSEQNUMBER  AND A.FOPERNUMBER=D.FOPERNUMBER AND C.FBILLNO=D.FOPTPLANNO
                       WHERE A.FDetailID in ({str.Trim(',')}) and D.FID={id}";
                    var pgmx = DBUtils.ExecuteDynamicObject(Context, pgmxsql);
                    if (pgmx.Count > 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }

                default:
					return true;
			}			
        }
	}
}
