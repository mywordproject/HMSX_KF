using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin
{
    [Description("资产租赁")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class ZCZLBillPlugin : AbstractBillPlugIn
    {
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (e.Field.Key == "FAlterID")
            {
                if (((DynamicObject)this.Model.GetValue("FBORROWORGID"))["Id"].ToString() == "100026")
                {
                    string fkdw = this.Model.GetValue("FALTERID",e.Row) == null ? "" : ((DynamicObject)this.Model.GetValue("FALTERID",e.Row))["Id"].ToString();
                    string zckpsql = $@"select c.FASSETNO,c.FUSEDEPTID,c.FUSERID,b.FPOSITIONID from t_fa_card a
                        inner join T_FA_CARDDETAIL b on b.FAlterID=a.FAlterID 
                        inner join t_fa_allocation c on b.FAlterID=c.FAlterID and b.FASSETNO=c.FASSETNO
                        where a.FAlterID='{fkdw}' group by  c.FASSETNO,c.FUSEDEPTID,c.FUSERID,b.FPOSITIONID";
                    var zckps = DBUtils.ExecuteDynamicObject(Context, zckpsql);
                    this.Model.DeleteEntryData("FDetailSubEntity");
                    int hs = 0;
                    foreach (var zckp in zckps)
                    {
                        this.Model.CreateNewEntryRow("FDetailSubEntity");
                        this.View.Model.SetItemValueByNumber("FALLOID", zckp["FASSETNO"].ToString(), hs);
                        this.View.Model.SetItemValueByID("F_260_BM", zckp["FUSEDEPTID"], hs);
                        this.View.Model.SetItemValueByID("FOLDPOSITIONID", zckp["FPOSITIONID"], hs);
                        hs++;
                    }
                    this.View.UpdateView("FDetailSubEntity");

                }
            }
        }
    }
}
