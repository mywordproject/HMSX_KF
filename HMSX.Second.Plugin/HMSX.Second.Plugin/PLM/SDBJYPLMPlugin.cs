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
    public class SDBJYPLMPlugin: UpgradeBigVersion
	{
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
            if (((DynamicObject)dynamicObject["CategoryID"])["Code"].ToString() == "17" || ((DynamicObject)dynamicObject["CategoryID"])["Code"].ToString() == "52")
            {
                throw new KDBusinessException("", "请在左上角切换至“业务类型”，选择相应的文档业务类型，点击“客户版本升大版”进行升版操作");
            }
            base.Upgrade(ctx, form, upgradeObjList, e);
        }

    }
}
