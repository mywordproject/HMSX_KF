using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using System.ComponentModel;

namespace HMSX.SCZZ.MJGL
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("扫描领用更新工序汇报领用时间")]
    public class SMLY: AbstractDynamicFormPlugIn
    {
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            long orid = this.Context.CurrentOrganizationInfo.ID;
            if(e.Field.Key== "F_PAEZ_SM" && orid==100026)
            {
                string OPH = this.View.Model.GetValue("F_PAEZ_SM",0).ToString();
                this.View.Refresh();
                if (OPH!="" && OPH != null)
                {
                    string hbsql = $@"/*dialect*/select a.FID ID,a.F_260_LYZT 状态,'内部' 类型 from T_SFC_OPTRPT a
                        inner join T_SFC_OPTRPTENTRY b on a.FID=b.FID and b.F_260_RKD=1
                        where a.FDOCUMENTSTATUS='C' and b.FOPTPLANNO='{OPH}'
                        union all
                        select FEntryID ID,FLYZT,'外部' 类型 from HMD_260_WWZTQRDEntry2
                        where F_HMD_GXJH_FBILLNO2='{OPH}'";                   
                    DynamicObjectCollection hbs = DBUtils.ExecuteDynamicObject(this.Context, hbsql);
                    if (hbs.Count == 0) { this.View.Model.SetValue("F_PAEZ_SMJG", "未找到可领用工序计划单！", 0); }
                    else
                    {
                        int num = 0;
                        foreach(DynamicObject hb in hbs)
                        {
                            if (hb["状态"].ToString() != "B")
                            {
                                string sql;
                                if (hb["类型"].ToString() == "内部")
                                {
                                    sql = "/*dialect*/update T_SFC_OPTRPT set F_260_LYZT='B',F_260_LYRQ=GETDATE() where FID=" + hb["ID"].ToString();
                                }
                                else
                                {
                                    sql = "/*dialect*/update HMD_260_WWZTQRDEntry2 set FLYZT='B',FLYRQ=GETDATE() where FEntryID=" + hb["ID"].ToString();
                                }
                                DBUtils.Execute(this.Context, sql);
                                num++;
                            }                           
                        }
                        if (num > 0)
                        {
                            this.View.Model.SetValue("F_PAEZ_SMJG", OPH + "领用成功！", 0);
                        }
                        else { this.View.Model.SetValue("F_PAEZ_SMJG", OPH + "已经领用！", 0); }
                        
                    }                    
                    this.View.GetControl("F_PAEZ_SM").SetFocus();
                }
            }
        }
    }
}
