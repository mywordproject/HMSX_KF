using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List.PlugIn;
using Kingdee.BOS.Core.List.PlugIn.Args;
using Kingdee.BOS.Core.Metadata.QueryElement;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;


namespace HMSX.Second.Plugin
{
    public class YCLListPlugin : AbstractListPlugIn
    {
        public override void PrepareFilterParameter(FilterArgs e)
        {
            base.PrepareFilterParameter(e);
            //定义过滤条件的字符串
            string filterString = "";

            //加载的时候,获取发布主控台设置的参数ListSet,My
            string listType = this.View.OpenParameter.GetCustomParameter("ListSet") as string;

            //判断字符串 listType的值是否为空,或者为空格,与My,不区分大小写对比
            if (!string.IsNullOrWhiteSpace(listType) && listType.Equals("My", StringComparison.OrdinalIgnoreCase))
            {
                //创建人ID等于登录用户ID
                filterString = string.Format(" FCREATORID={0}", this.Context.UserId);

            }
            //原本的过滤条件e.FilterString
            if (!string.IsNullOrWhiteSpace(e.FilterString) && !string.IsNullOrWhiteSpace(filterString))
            {
                e.FilterString += "AND";
            }
            e.FilterString += filterString;
        }
    }

}
