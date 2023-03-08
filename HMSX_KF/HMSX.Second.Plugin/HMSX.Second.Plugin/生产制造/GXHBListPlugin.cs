using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.List.PlugIn;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.生产制造
{
    [Description("工序汇报--批量修改合格数")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class GXHBListPlugin: AbstractListPlugIn
    {
        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);
            if (e.BarItemKey.Equals("KEEP_HXHGS"))
            {
                if (Context.CurrentOrganizationInfo.ID == 100026)
                {
                    //选择的行,获取所有信息,放在listcoll里面
                    ListSelectedRowCollection listcoll = this.ListView.SelectedRowsInfo;
                    //定义一个字符串数组,接收FID的值
                    string[] listKey = listcoll.GetPrimaryKeyValues();
                    if (listKey.Length == 0)
                    {
                        throw new KDBusinessException("", "请选择需要批量修改的行！");
                    }
                    foreach (string key in listKey)
                    {
                        string upsql = $@"/*dialect*/update c set c.FQUAQTY=c.FFINISHQTY,c.FBASEQUAQTY=c.FBASEFINISHQTY,c.FPRDQUAQTY=c.FFINISHQTY FROM T_SFC_OPTRPT a 
                                          INNER join T_SFC_OPTRPTENTRY b on a.fid=b.fid
                                          INNER join T_SFC_OPTRPTENTRY_A c on b.FENTRYID=c.FENTRYID where a.FID={key}";
                        DBUtils.ExecuteDynamicObject(Context, upsql);
                    }
                    this.View.ShowMessage("批量修改成功！");
                    this.View.Refresh();
                }
            }
        }
    }
}
