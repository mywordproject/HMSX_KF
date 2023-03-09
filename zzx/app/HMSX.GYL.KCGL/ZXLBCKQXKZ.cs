using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.List.PlugIn;
using Kingdee.BOS.Core.List.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace HMSX.GYL.KCGL
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("双新列表查看权限控制")]
    public class ZXLBCKQXKZ:AbstractListPlugIn
    {
        public override void PrepareFilterParameter(FilterArgs e)
        {
            base.PrepareFilterParameter(e);
            if (this.Context.CurrentOrganizationInfo.ID == 100026)
            {
                long yh = this.Context.UserId;
                string form = this.View.BillBusinessInfo.GetForm().Id;
                string sql = $@"/*dialect*/select vi.fmasterid 数据,zd.FZDBS 标识 from PAEZ_t_Cust100383 a
                    --用户对应数据范围
                    inner join PAEZ_t_Cust_Entry100437 yh on a.FID=yh.FID
                    inner join PAEZ_t_Cust_Entry100438 sj on yh.FEntryID=sj.FEntryID
                    inner join V_SXV0001 vi on vi.fformid=a.FSJLX and vi.fitemid=sj.FSJZ
                    --表单及控制字段
                    inner join PAEZ_t_Cust_Entry100439 yw on yw.FID=a.FID
                    inner join PAEZ_t_Cust_Entry100440 zd on zd.FEntryID=yw.FEntryID
                    where yh.FUSER={yh} and yw.FDJM='{form}'";
                DynamicObjectCollection objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
                List<string> sj = new List<string>();
                List <string> bs= new List<string>();
                if (objs.Count > 0)
                {
                    foreach (DynamicObject obj in objs)
                    {
                        sj.Add(obj["数据"].ToString());
                        bs.Add(obj["标识"].ToString());
                    }
                    //获取数据范围
                    string datas = "";
                    foreach(string data in sj.Distinct<string>())
                    {
                        datas += data + ",";
                    }
                    string fw = $"({datas.Substring(0,datas.Length-1)})";
                    //获取控制字段标识
                    string filter = "";
                    foreach (string key in bs.Distinct<string>())
                    {
                        filter += $"{key} in {fw} OR ";
                    }
                    //列表添加过滤
                    if (filter != "")
                    {
                        e.FilterString = e.FilterString.JoinFilterString(filter.Substring(0,filter.Length-3));
                    }    
                }                
            }
        }
    }
}
