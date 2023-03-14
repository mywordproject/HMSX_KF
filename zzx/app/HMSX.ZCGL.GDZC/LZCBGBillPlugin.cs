using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Bill.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace HMSX.ZCGL.GDZC
{
    [Description("资产变更--带出源单编号")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class LZCBGBillPlugin: AbstractBillPlugIn
    {
        public override void BeforeSave(BeforeSaveEventArgs e)
        {
            base.BeforeSave(e);
            string zz = this.Model.GetValue("FASSETORGID") == null ? "" : ((DynamicObject)this.Model.GetValue("FASSETORGID"))["Id"].ToString();
            if (zz == "100026")
            {
                string kpbm = this.Model.GetValue("FALTERID") == null ? "" : ((DynamicObject)this.Model.GetValue("FALTERID"))["Id"].ToString();
                string bglx = this.Model.GetValue("F_260_BGLX") == null ? "" : this.Model.GetValue("F_260_BGLX").ToString();
                if (bglx == "资产借出")
                {
                    string zczlsql = $@"select FBILLNO,FALTERID,FISRETURN,FISRECEIVE from PAEZ_t_Cust_Entry100315 a
                inner join PAEZ_t_Cust100334 b on a.FID=b.FID
                WHERE FISRETURN=0 and FISRECEIVE=0
                and FALTERID='{kpbm}' and FDOCUMENTSTATUS='C'";
                    var zczl = DBUtils.ExecuteDynamicObject(Context, zczlsql);
                    if (zczl.Count > 0)
                    {
                        this.Model.SetValue("F_260_ZCZLDDH", zczl[0]["FBILLNO"].ToString());
                        this.View.UpdateView("F_260_ZCZLDDH");
                    }
                }
                else if (bglx == "资产回收")
                {
                    string zczlsql = $@"select FBILLNO,FALTERID,FISRETURN,FISRECEIVE from PAEZ_t_Cust_Entry100315 a
                inner join PAEZ_t_Cust100334 b on a.FID=b.FID
                WHERE FISRETURN=1 and FISRECEIVE=0
                and FALTERID='{kpbm}' and FDOCUMENTSTATUS='C'";
                    var zczl = DBUtils.ExecuteDynamicObject(Context, zczlsql);
                    if (zczl.Count > 0)
                    {
                        this.Model.SetValue("F_260_ZCZLDDH", zczl[0]["FBILLNO"].ToString());
                        this.View.UpdateView("F_260_ZCZLDDH");
                    }
                }
            }
           
        }
    }
}
