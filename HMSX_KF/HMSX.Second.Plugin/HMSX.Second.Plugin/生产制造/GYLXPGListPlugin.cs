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
    [Description("工艺路线--批量修改")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class GYLXPGListPlugin : AbstractListPlugIn
    {
        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);
            if (e.BarItemKey.Equals("SLSB_PLXG"))
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
                    parameter.FormId = "SLSB_GYLXPLXG";
                    parameter.MultiSelect = false;
                    //获取返回的值
                    this.View.ShowForm(parameter, delegate (FormResult result)
                    {
                        string[] date = (string[])result.ReturnData;
                        foreach (var lists in listcoll)
                        {
                            if (date != null && date[0] == "1")
                            {
                                if (lists.FieldValues.Count >= 3)
                                {
                                    string FENTRYID = "";
                                    string FDetailID = "";
                                    try
                                    {
                                        FDetailID = lists.FieldValues["FSubEntity"];
                                    }
                                    catch
                                    {
                                        FENTRYID = lists.FieldValues["FEntity"];
                                    }
                                    finally
                                    {
                                        string upsql = $@"update T_ENG_ROUTEOPERDETAIL_B set FREPORTCEILRATIO='{date[1]}' where 
                                   (FDetailID='{FDetailID}' OR '{FDetailID}'='') AND
                                    (FENTRYID='{FENTRYID}' OR '{FENTRYID}'='')";
                                        DBUtils.ExecuteDynamicObject(Context, upsql);
                                    }
                                }
                                else if (lists.FieldValues.Count < 3)
                                {
                                    string upsql = $@"update T_ENG_ROUTEOPERDETAIL_B SET FREPORTCEILRATIO={date[1]}
                                WHERE FENTRYID IN 
                                (SELECT FENTRYID from T_ENG_ROUTE a
                                inner join T_ENG_ROUTEOPERSEQ b on a.FID=b.FID 
                                WHERE A.FID={lists.PrimaryKeyValue})";
                                    DBUtils.ExecuteDynamicObject(Context, upsql);
                                }
                                this.View.ShowMessage("批量修改成功！");
                            }
                            else if (date != null && date[0] == "2")
                            {

                                string upsql = $@"update T_ENG_ROUTE set FROUTEGROUPID={date[2]}  where FID={lists.PrimaryKeyValue}";
                                DBUtils.ExecuteDynamicObject(Context, upsql);
                                this.View.ShowMessage("批量修改成功！");
                            }
                        }
                    });
                }
            }
        }
    }
}
