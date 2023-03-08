using Kingdee.BOS;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.CommonFilter;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.Enums;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.ControlElement;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Core.Metadata.PreInsertData;
using Kingdee.BOS.Core.NetworkCtrl;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Core.Util;
using Kingdee.BOS.JSON;
using Kingdee.BOS.KDThread;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Resource;
using Kingdee.BOS.Serialization;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using Kingdee.K3.BD.ServiceHelper;
using Kingdee.K3.Core.BD;
using Kingdee.K3.Core.BD.ServiceArgs;
using Kingdee.K3.Core.MFG.ENG.BomExpand;
using Kingdee.K3.Core.MFG.ENG.MaterialTree;
using Kingdee.K3.Core.MFG.ENG.ParamOption;
using Kingdee.K3.Core.MFG.EntityHelper;
using Kingdee.K3.Core.MFG.EnumConst;
using Kingdee.K3.Core.MFG.Utils;
using Kingdee.K3.MFG.ServiceHelper;
using Kingdee.K3.MFG.ServiceHelper.ENG;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace HMSX.Second.Plugin.基础资料
{
    public class WLQDZCFormPlugin: AbstractDynamicFormPlugIn
    {
		protected const string EntityKey_FBomHeadEntity = "FTopEntity";
		protected const string EntityKey_FBomChildEntity = "FBottomEntity";
		protected const string EntityKey_FBomBillHead = "FBillHead";
		protected const string EntityKey_FCobyEntity = "FCobyEntity";
		protected const string FieldKey_FExpandLevel = "FExpandLevel";
		protected const string FieldKey_FValidDate = "FValidDate";
		protected const string FieldKey_FQty = "FQty";
		protected const string FieldKey_FBomId = "FBomId";
		protected const string FieldKey_FMaterialId = "FMaterialId";
		protected const string ControlKey_SplitContainer = "FSpliteContainer1";
		protected const string FieldKey_FBillBomId = "FBillBomId";
		protected const string FieldKey_FBillMaterialId = "FBillMaterialId";
		protected const string FieldKey_FBillMtrlAuxId = "FBillMtrlAuxId";
		protected const string FieldKey_FBomUseOrgId = "FBomUseOrgId";
		protected const string FiledKey_FBomVersion = "FBomVersion";
		protected const string FieldKey_FBomChildMaterialId = "FMaterialId2";
		protected const string FieldKey_FBomEntryId = "FBomEntryId";
		protected const string ControlKey_tab = "FTABBOTTOM";
		protected const string FormKey_MaterialTree = "MFG_MaterialTree";
		protected const string Key_Contain = "FTreePanel";
		private bool IsOpenAttach;
		protected string currentFormId = "ENG_BomQueryForward2";
		private string _materialTree_PageId;
		protected List<DynamicObject> bomQueryChildItems = new List<DynamicObject>();
		protected List<DynamicObject> bomPrintChildItems = new List<DynamicObject>();
		protected MemBomExpandOption_ForPSV memBomExpandOption;
		protected FilterParameter filterParam;
		private int curTabindex;
		private int _curRow;
		private List<NetworkCtrlResult> networkCtrlResults;
		private List<DynamicObject> _exportData = new List<DynamicObject>();
		
	}
}
