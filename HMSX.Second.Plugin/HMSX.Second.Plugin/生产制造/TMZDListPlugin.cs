using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm;
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
    [Description("条码主档--批量修改")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class TMZDListPlugin: AbstractListPlugIn
    {
        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);
            if (e.BarItemKey.Equals("KEEP_PLXG"))
            {
                if (Context.CurrentOrganizationInfo.ID == 100026)
                {
                    //选择的行,获取所有信息,放在listcoll里面
                    ListSelectedRowCollection listcoll = this.ListView.SelectedRowsInfo;
                    //定义一个字符串数组,接收分录FID的值
                    string[] listKey = listcoll.GetPrimaryKeyValues();
                    if (listKey.Length == 0)
                    {
                        throw new KDBusinessException("", "请选择需要批量修改的行！");
                    }                   
                    DynamicFormShowParameter parameter = new DynamicFormShowParameter();
                    parameter.OpenStyle.ShowType = ShowType.Floating;
                    parameter.FormId = "keed_SXTMZDXG";
                    parameter.MultiSelect = false;
                    //获取返回的值
                    this.View.ShowForm(parameter, delegate (FormResult result)
                    {
                        string[] date = (string[])result.ReturnData;
                        if (date != null && date[0] == "1")
                        {
                            foreach (string key in listKey)
                            {
                                string upsql = $@"update T_BD_BARCODEMAIN set FTRACKINGNUMBER='{date[1]}' where FID={key}";
                                DBUtils.ExecuteDynamicObject(Context, upsql);
                            }
                            this.View.ShowMessage("批量修改成功！");
                            this.View.Refresh();
                        }
                        else if (date != null && date[0] == "2")
                        {
                            foreach (string key in listKey)
                            {
                                string upsql = $@"update T_BD_BARCODEMAIN set F_260_JHGZHBM='{date[2]}' where FID={key}";
                                DBUtils.ExecuteDynamicObject(Context, upsql);
                            }
                            this.View.ShowMessage("批量修改成功！");
                            this.View.Refresh();
                        }
                    });
                }
            }
        }
    }
}
