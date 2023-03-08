using Kingdee.BOS;
using Kingdee.BOS.Core.CommonFilter.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Permission;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace HMSX.Second.Plugin.考勤报表
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("考情过滤界面")]
    public class KQBBGLReportPlugin : AbstractCommonFilterPlugIn
    {
        public override void TreeNodeClick(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.TreeNodeArgs e)
        {
            base.TreeNodeClick(e);
        }
      
        public override void AfterCreateNewData(EventArgs e)
        {
            base.AfterCreateNewData(e);
            //if (this.Model.GetValue("F_260_KQFW").ToString() == "1")
            //{
            //    this.View.GetMainBarItem("F_260_KQKSRQ").Visible = false;
            //    this.View.GetMainBarItem("F_260_KQJZRQ").Visible = false;
            //    this.View.GetMainBarItem("F_260_RQ").Visible = true;
            //}
            //else
            //{
            //    this.View.GetMainBarItem("F_260_KQKSRQ").Visible = true;
            //    this.View.GetMainBarItem("F_260_KQJZRQ").Visible = true;
            //    this.View.GetMainBarItem("F_260_RQ").Visible = false;
            //}
            
        }
    }
}

