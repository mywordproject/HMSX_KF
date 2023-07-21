using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Resource;
using Kingdee.K3.PLM.CFG.App.ServicePlugIn.VersionService;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.Common;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.Enum;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.Manager;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.Manager.Release;
using Kingdee.K3.PLM.CFG.Common.Interface.STD.BOM;
using Kingdee.K3.PLM.Common.BusinessEntity;
using Kingdee.K3.PLM.Common.BusinessEntity.View;
using Kingdee.K3.PLM.Common.Core.BOSBridge;
using Kingdee.K3.PLM.Common.Core.Permission;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace HMSX.Second.Plugin.PLM
{
    [Description("升大版")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class SDBJYPLMPlugin: UpgradeVersionService
	{
		//public override void Upgrade(PLMContext ctx, IPLMBusinessFormPlugIn form, List<DynamicObject> upgradeObjList, BeforeExecuteOperationTransaction e)
		//{
		//    if (upgradeObjList.Count == 0)
		//    {
		//        return;
		//    }
		//    List<DynamicObject> listupgradeObj = (
		//        from m in e.SelectedRows
		//        select m.DataEntity).ToList<DynamicObject>();
		//    Dictionary<string, object> dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
		//    DynamicObject dynamicObject = VersionManager.Instance.findDefaultVersionObj(ctx, listupgradeObj, true);
		//    if (((DynamicObject)dynamicObject["CategoryID"])["Number"].ToString()=="17" || ((DynamicObject)dynamicObject[""])["Number"].ToString() == "52")
		//    {
		//        throw new KDBusinessException("", "请在左上角切换至“业务类型”，选择相应的文档业务类型，点击“客户版本升大版”进行升版操作");
		//    }
		//    base.Upgrade(ctx, form, upgradeObjList, e);
		//}
		protected override bool HistoryObjectToggle
		{
			get
			{
				return false;
			}
		}
		public override PLMPermissionItem PermissionItem
		{
			get
			{
				return PLMPermissionItem.Upgrade_bigversion;
			}
		}
		public override void Upgrade(PLMContext ctx, DynamicObject o)
		{
			VersionManager.Instance.UpgradeVerNew(this.PLMContext, o, this.GetUpgradeVersionType(), 1);
		}
		public override void Upgrade(PLMContext ctx, IPLMBusinessFormPlugIn form, List<DynamicObject> upgradeObjList, BeforeExecuteOperationTransaction e)
		{
			if (upgradeObjList.Count == 0)
			{
				return;
			}
			List<DynamicObject> listupgradeObj = (
				from m in e.SelectedRows
				select m.DataEntity).ToList<DynamicObject>();
			Dictionary<string, object> dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
			DynamicObject dynamicObject = VersionManager.Instance.findDefaultVersionObj(ctx, listupgradeObj, true);
            if (((DynamicObject)dynamicObject["CategoryID"])["Number"].ToString() == "17" || ((DynamicObject)dynamicObject[""])["Number"].ToString() == "52")
            {
                throw new KDBusinessException("", "请在左上角切换至“业务类型”，选择相应的文档业务类型，点击“客户版本升大版”进行升版操作");
            }
            dictionary["CategoryId"] = dynamicObject["CategoryID_Id"];
			dictionary["objDyn"] = dynamicObject;
			dictionary["objList"] = listupgradeObj;
			dictionary["isMaxVersion"] = true;
			PageManager.Instance.ShowDynamicForm(form.PLMView, "PLM_CFG_UPGRADEMAXVERSION", "自定义升大版", dictionary, ShowType.Modal, delegate (FormResult result)
			{
				if (result.ReturnData != null)
				{
					using (KDTransactionScope kDTransactionScope = new KDTransactionScope(TransactionScopeOption.Required))
					{
						try
						{
							List<int> list = (List<int>)result.ReturnData;
							List<DynamicObject> list2 = new List<DynamicObject>();
							List<long> list3 = new List<long>();
							foreach (DynamicObject current in listupgradeObj)
							{
								long num = Convert.ToInt64(current["Id"]);
								DynamicObject dynamicObject2 = (num != 0L) ? DomainObjectManager.Instance(this.PLMContext, Convert.ToInt64(current["CategoryId_Id"])).Get(this.PLMContext, num) : null;
								DynamicObject dynamicObject3 = dynamicObject2 ?? current;
								list2.Add(dynamicObject3);
								IBomManagerEx bomManagerEx = PLMExtensionFactory.Create<IBomManagerEx>(true);
								bomManagerEx.CadByDocument(this.PLMContext, dynamicObject3, list3);
							}
							for (int i = 0; i < list2.Count; i++)
							{
								DynamicObject dynamicObject4 = list2[i];
								VersionManager.Instance.UpgradeVerNew(this.PLMContext, dynamicObject4, UpgradeVersionType.UpMaxVer, (list[i] == 2147483647) ? 1 : list[i]);
								ReleaseObjStatusManager.Instance.CancelObjReleaseStatus(ctx, Convert.ToInt64(dynamicObject4["Id"]), -1L);
								DomainObjectManager.Instance(ctx, Convert.ToInt64(dynamicObject4["CategoryId_Id"])).Save(ctx, dynamicObject4);
							}
							if (list3.Count <= 0)
							{
								IEnumerable<long> source =
									from t in list2
									select Convert.ToInt64(t["Id"]);
								foreach (DynamicObject current2 in this.upgradeRelatedObjList)
								{
									if (!source.Contains(Convert.ToInt64(current2["Id"])))
									{
										VersionManager.Instance.UpgradeVerNew(this.PLMContext, current2, this.GetUpgradeVersionType(), 1);
									}
								}
							}
						}
						catch (Exception ex)
						{
							string text = " " + ex.Message + "\n";
							if (ex.InnerException != null)
							{
								text = text + "InnerException:\n" + ex.InnerException.Message + "\n";
							}
							string text2 = text;
							text = string.Concat(new string[]
							{
								text2,
								ex.Source,
								"\n",
								ex.StackTrace,
								"\n"
							});
							form.PLMView.CurrentView.ShowErrMessage(text, "", MessageBoxType.Notice);
							return;
						}
						form.PLMView.CurrentView.ShowMessage(ResManager.LoadKDString("升大版成功！", "120006000000152", SubSystemType.PLM, new object[0]), MessageBoxType.Notice);
						form.PLMView.CurrentView.Refresh();
						kDTransactionScope.Complete();
					}
				}
			}, true, "", null);
		}
		public override bool CheckUpgradeVersionRule(PLMContext context, DynamicObject upgradeObj, PLMOperationParam param, out string errorMsg)
		{
			return VersionManager.Instance.CheckUpgradeVersionRule(context, upgradeObj, UpgradeVersionType.UpMaxVer, param, out errorMsg);
		}
		public override void GetAutoUpgradeRelatedObject(PLMContext context, DynamicObject upgradeObj, PLMOperationParam param, ref List<DynamicObject> updateReObjList, ref string errorMsg)
		{
			VersionManager.Instance.GetAutoUpgradeRelatedObject(context, upgradeObj, UpgradeVersionType.UpMaxVer, param, ref updateReObjList, ref errorMsg);
		}
		public override UpgradeVersionType GetUpgradeVersionType()
		{
			return UpgradeVersionType.UpMaxVer;
		}
	}
}
