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
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.供应链
{
    [Description("库存形态转换单--校验库存是否一致")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class KCXTZHBillPlugin: AbstractBillPlugIn
    {
        string FNUMBER = "";
        string NUMBER = "";
        public override void OnInitialize(InitializeEventArgs e)
        {
            base.OnInitialize(e);
            try
            {
                FNUMBER = e.Paramter.GetCustomParameter("FNUMBER").ToString();
                NUMBER = e.Paramter.GetCustomParameter("NUMBER").ToString();
            }
            catch
            {
                FNUMBER = "";
                NUMBER = "";
            }
            finally
            {

            }     
        }
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            if (FNUMBER != "")
            {
                string zz = this.Model.GetValue("FSTOCKORGID") == null ? "" : ((DynamicObject)this.Model.GetValue("FSTOCKORGID"))["Id"].ToString();
                if (zz == "100026")
                {
                    this.Model.SetValue("F_260_BGDBM",NUMBER);
                    this.View.UpdateView("F_260_BGDBM");
                    var dates = this.Model.DataObject["StockConvertEntry"] as DynamicObjectCollection;
                    foreach (var date in dates)
                    {
                        if (date["ConvertType"].ToString() == "A")
                        {                         
                            string cxsql = $@"select FMATERIALID from T_BD_MATERIAL WHERE FNUMBER='{FNUMBER}'";
                            var cx = DBUtils.ExecuteDynamicObject(Context, cxsql);
                            if (cx.Count > 0)
                            {
                                this.Model.SetItemValueByID("FMATERIALID", cx[0]["FMATERIALID"], Convert.ToInt32(date["Seq"]) - 1);
                                this.View.InvokeFieldUpdateService("FMaterialId", Convert.ToInt32(date["Seq"]) - 1);
                            }
                        }
                    }
                    this.View.UpdateView("FEntity");
                }
            }
        }
        public override void BeforeSave(BeforeSaveEventArgs e)
        {
            base.BeforeSave(e);
            string zz = this.Model.GetValue("FSTOCKORGID") == null ? "" : ((DynamicObject)this.Model.GetValue("FSTOCKORGID"))["Id"].ToString();
            if (zz == "100026")
            {
                var dates = this.Model.DataObject["StockConvertEntry"] as DynamicObjectCollection;
                foreach (var date in dates) 
                {
                    if(date["ConvertType"].ToString() == "A")
                    {
                        foreach(var date1 in dates)
                        {
                            if(date1["ConvertType"].ToString() == "B" && 
                                date1["MaterialId_Id"].ToString() == date["MaterialId_Id"].ToString() && 
                                date1["Lot_Id"].ToString() == date["Lot_Id"].ToString() &&
                                date1["StockId_Id"].ToString() != date["StockId_Id"].ToString())
                            {
                                this.View.ShowMessage("转换前、后的仓库不一致，请确认仓库是否需要变更！！！");
                            }
                        }
                    }
                }
            }           
        }
    }
}
