using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.K3.BD.BarCode.Business.PlugIn;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.生产制造
{
    [Description("条码拆分--清空系统来源")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class TMCFBillPlugin: BarCodeSplitEdit
    {
        public override void ButtonClick(ButtonClickEventArgs e)
        {
            base.ButtonClick(e);
            string a = e.Key.ToUpperInvariant();
            if (a == "FBTNCONFIRM")
            {
                if(e.Cancel == false)
                {
                    string tm = this.Model.GetValue("FMAINBARCODE").ToString();
                    string upsql = $@"/*dialect*/update T_BD_BARCODEMAIN set F_260_XTLY='' where FPARENTBARCODE='{tm}'";
                    var x=DBUtils.Execute(Context, upsql);
                }
            }
        }
    }
}
