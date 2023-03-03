using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.供应链
{
    [Description("WMS库位查询")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class WMSKCCXFormPlugin:AbstractDynamicFormPlugIn
    {
        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            if (e.Key == "F_CX")
            {
                string wl = this.Model.GetValue("F_260_WL") == null ? "" : ((DynamicObject)this.Model.GetValue("F_260_WL"))["Number"].ToString();
                string ph = this.Model.GetValue("F_260_PH") == null ? "" : ((DynamicObject)this.Model.GetValue("F_260_PH"))["Number"].ToString();
                string ck = this.Model.GetValue("F_260_CK") == null ? "" : ((DynamicObject)this.Model.GetValue("F_260_CK"))["Number"].ToString();
                //string tm = this.Model.GetValue("F_260_TM").ToString();
                string tm = this.Model.GetValue("F_260_TM") == null ? "" : ((DynamicObject)this.Model.GetValue("F_260_TM"))["Number"].ToString();
                string kczt = this.Model.GetValue("F_260_KCZT") == null ? "" : ((DynamicObject)this.Model.GetValue("F_260_KCZT"))["Name"].ToString();
                string rq = this.Model.GetValue("F_260_RQ").ToString();
                string strDataBase = "Server=10.42.99.67;DataBase=wms_hmsx;Uid=sa;pwd=hmsx!@#456;";
                SqlConnection conn = new SqlConnection(strDataBase);
                conn.Open();
                string gzxxsql = $@"exec sp_kingdee_interface_kct04_box '{wl}','{ph}','{ck}','{tm}','{kczt}','{rq}'";
                SqlCommand sqlcmd = new SqlCommand(gzxxsql, conn);
                SqlDataReader cont = sqlcmd.ExecuteReader();
                int i = 0;
                this.Model.DeleteEntryData("F_keed_Entity");
                while (cont.Read())
                {
                    this.Model.CreateNewEntryRow("F_keed_Entity");
                    this.Model.SetValue("FWLBM", cont["物料编码"] == null ? "" : cont["物料编码"].ToString(), i);
                    this.Model.SetValue("FWLMC", cont["物料名称"] == null ? "" : cont["物料名称"].ToString(), i);
                    this.Model.SetValue("FGGXH", cont["规格"] == null ? "" : cont["规格"].ToString(), i);
                    this.Model.SetValue("FCK", cont["仓库编码"] == null ? "" : cont["仓库编码"].ToString(), i); 
                    this.Model.SetValue("FTM", cont["条码"] == null ? "" : cont["条码"].ToString(), i);
                    this.Model.SetValue("FKCSL", cont["库存数量"] == null ? "" : cont["库存数量"].ToString(), i);
                    this.Model.SetValue("FKW", cont["库位"] == null ? "" : cont["库位"].ToString(), i);
                    this.Model.SetValue("FJLDW", cont["计量单位"] == null ? "" : cont["计量单位"].ToString(), i);
                    this.Model.SetValue("FHZ", cont["货主"] == null ? "" : cont["货主"].ToString(), i);
                    this.Model.SetValue("FPH", cont["批号"] == null ? "" : cont["批号"].ToString(), i);
                    this.Model.SetValue("FKCZT", cont["库存状态"] == null ? "" : cont["库存状态"].ToString(), i);
                    i++;
                }
                this.View.UpdateView("F_keed_Entity");
            }
        }
    }
}
