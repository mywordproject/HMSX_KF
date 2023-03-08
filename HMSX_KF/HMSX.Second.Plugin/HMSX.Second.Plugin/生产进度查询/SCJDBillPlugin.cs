using HMSX.Second.Plugin.Tool;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.生产进度查询
{
    [Description("生产进度查询——单击过滤弹出对话框")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class SCJDBillPlugin: AbstractDynamicFormPlugIn
    {
        private DynamicObjectCollection WLQD(string ID)
        {
            string wlqdsql = $@"select b.FMATERIALID from T_ENG_BOM a 
            inner join T_ENG_BOMCHILD b on a.FID=b.FID
            inner join t_bd_material c on a.FMATERIALID=c.FMATERIALID
            where c.FNUMBER='260.02.04523'";
            var wlqds = DBUtils.ExecuteDynamicObject(Context,wlqdsql);
            return wlqds;
            //this.Model.InsertEntryRow()
        }
    }
}
