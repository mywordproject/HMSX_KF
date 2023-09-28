using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.生产制造
{
    [Description("新能源过站信息查询")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class XNYGZXXFormPlugin: AbstractDynamicFormPlugIn
    {
        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            if (e.Key.Equals("F_CX"))
            {
               string cpm= this.Model.GetValue("F_CPM")==null?"": this.Model.GetValue("F_CPM").ToString();
               
                if(cpm=="" )
                {
                    throw new KDBusinessException("", "不允许产品码为空！");
                }
                else
                {
                    string strsql = $@"/*dialect*/select 
                           F_260_DGPPH,F_260_FBPPH,F_260_FBFHJSJ,F_260_HJJTH	
                           ,F_260_ZFJZPH,F_260_MFQPH,F_260_ZSSJ,F_260_ZSJTH	
                           ,F_260_XSJPH,F_260_RRSJ,F_260_RRXT,F_260_LLZ	
                           ,F_260_QGXBCPTM	,F_260_HJSJ	,F_260_HJXT	,F_260_JMPH	
                           ,F_260_ZJZZZ,F_260_FJGYJC,F_260_FJJYJC,F_260_DGNSJ	
                           ,F_260_DGNJT,F_260_FBPBDTFL	,F_260_FBPGQGL,F_260_FBPHJSD	
                           ,F_260_FBPLJL,F_260_FBPCCD,F_260_ZFJZSCS,F_260_RRZF	
                           ,F_260_RRGL,F_260_CPM,F_260_BZQSMSJ,F_260_XM	
                           ,F_260_HJJG,F_260_DGNJG,F_260_TMJG,F_260_TMSJ,F_260_CZY	,F_260_BC,F_260_CPBM
                           from PAEZ_t_Cust_Entry100521 where F_260_XM like '%{cpm}%' or F_260_CPM like '%{cpm}%'";
                    var cxs = DBUtils.ExecuteDynamicObject(Context, strsql);
                    int i = 0;
                    this.Model.DeleteEntryData("F_keed_Entity");
                    foreach (var cx in cxs)
                    {
                        this.Model.CreateNewEntryRow("F_keed_Entity");
                        this.Model.SetValue("F_260_DGPPH",cx["F_260_DGPPH"], i);
                        this.Model.SetValue("F_260_FBPPH", cx["F_260_FBPPH"], i);
                        this.Model.SetValue("F_260_FBFHJSJ", cx["F_260_FBFHJSJ"], i);
                        this.Model.SetValue("F_260_HJJTH", cx["F_260_HJJTH"], i);

                        this.Model.SetValue("F_260_ZFJZPH", cx["F_260_ZFJZPH"], i);
                        this.Model.SetValue("F_260_MFQPH", cx["F_260_MFQPH"], i);
                        this.Model.SetValue("F_260_ZSSJ", cx["F_260_ZSSJ"], i);
                        this.Model.SetValue("F_260_ZSJTH", cx["F_260_ZSJTH"], i);

                        this.Model.SetValue("F_260_XSJPH", cx["F_260_XSJPH"], i);
                        this.Model.SetValue("F_260_RRSJ", cx["F_260_RRSJ"], i);
                        this.Model.SetValue("F_260_RRXT", cx["F_260_RRXT"], i);
                        this.Model.SetValue("F_260_LLZ", cx["F_260_LLZ"], i);

                        this.Model.SetValue("F_260_QGXBCPTM", cx["F_260_QGXBCPTM"], i);
                        this.Model.SetValue("F_260_HJSJ", cx["F_260_HJSJ"], i);
                        this.Model.SetValue("F_260_HJXT", cx["F_260_HJXT"], i);
                        this.Model.SetValue("F_260_JMPH", cx["F_260_JMPH"], i);

                        this.Model.SetValue("F_260_ZJZZZ", cx["F_260_ZJZZZ"], i); 
                        this.Model.SetValue("F_260_FJGYJC", cx["F_260_FJGYJC"], i);
                        this.Model.SetValue("F_260_FJJYJC", cx["F_260_FJJYJC"], i);
                        this.Model.SetValue("F_260_DGNSJ", cx["F_260_DGNSJ"], i);

                        this.Model.SetValue("F_260_DGNJT", cx["F_260_DGNJT"], i);
                        this.Model.SetValue("F_260_FBPBDTFL", cx["F_260_FBPBDTFL"], i);
                        this.Model.SetValue("F_260_FBPGQGL", cx["F_260_FBPGQGL"], i);
                        this.Model.SetValue("F_260_FBPHJSD", cx["F_260_FBPHJSD"], i);

                        this.Model.SetValue("F_260_FBPLJL", cx["F_260_FBPLJL"], i);
                        this.Model.SetValue("F_260_FBPCCD", cx["F_260_FBPCCD"], i);
                        this.Model.SetValue("F_260_ZFJZSCS", cx["F_260_ZFJZSCS"], i);
                        this.Model.SetValue("F_260_RRZF", cx["F_260_RRZF"], i);

                        this.Model.SetValue("F_260_RRGL", cx["F_260_RRGL"], i);
                        this.Model.SetValue("F_260_BZQSMSJ", cx["F_260_BZQSMSJ"], i);
                        this.Model.SetValue("F_260_HJJG", cx["F_260_HJJG"], i);
                        this.Model.SetValue("F_260_DGNJG", cx["F_260_DGNJG"], i);

                        this.Model.SetValue("F_260_TMJG", cx["F_260_TMJG"], i);
                        this.Model.SetValue("F_260_TMSJ", cx["F_260_TMSJ"], i);
                        this.Model.SetValue("F_260_CZY", cx["F_260_CZY"], i);
                        this.Model.SetValue("F_260_BC", cx["F_260_BC"], i);
                        this.Model.SetValue("F_260_CPBM", cx["F_260_CPBM"], i);

                        i++;
                    }
                    this.View.UpdateView("F_keed_Entity");
                }
            }
        }
    }
}
