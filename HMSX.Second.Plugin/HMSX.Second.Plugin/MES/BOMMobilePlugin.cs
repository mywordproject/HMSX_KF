using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.Complex;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.MES
{
    [Description("BOM--移动端")]
    [Kingdee.BOS.Util.HotUpdate]
    public class BOMMobilePlugin : ComplexBOMListEdit
    {
        public override void BeforeUpdateValue(BeforeUpdateValueEventArgs e)
        {
            if (e.Key== "FText_OptPlanNumberScan")
            {
                if (e.Value.ToString().Contains("PGMX"))
                {
                    string strSql = string.Format(@"/*dialect*/select top 1 concat(t.FOptPlanNo,'-',t.FSEQNUMBER,'-',t.FOperNumber) as OptPlanNo  
                                              from T_SFC_DISPATCHDETAIL t 
                                              inner join T_SFC_DISPATCHDETAILENTRY t1 on t.FID=t1.FID 
                                              where F_260_CSTM!=''and F_260_CSTM like '%{0}%' 
                                              order by FDISPATCHTIME desc", e.Value.ToString());
                    DynamicObjectCollection rs = DBServiceHelper.ExecuteDynamicObject(this.Context, strSql);
                    if (rs.Count > 0)
                    {
                        e.Value = rs[0]["OptPlanNo"].ToString();

                    }
                }
            }
            base.BeforeUpdateValue(e);
        }
    }
}
