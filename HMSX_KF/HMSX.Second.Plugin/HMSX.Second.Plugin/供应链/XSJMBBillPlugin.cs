using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.供应链
{
    [Description("销售价目表--切换视图")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class XSJMBBillPlugin : AbstractBillPlugIn
    {      
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            BindComboField();
            if (this.View.OpenParameter.Status == OperationStatus.ADDNEW &&
                    (Context.UserId == 6481124 || Context.UserId == 22797700 ||
                    Context.UserId == 15701258 || Context.UserId == 1410291))
            {
                this.Model.DataObject["F_260_ST"] = "72cc4e3d-9498-44a0-b53a-fafca7db4825";
                this.View.UpdateView("F_260_ST");            
            }
            var layoutId = Convert.ToString(this.View.OpenParameter.GetCustomParameter("Jac_LayoutId", true));
            if (!string.IsNullOrWhiteSpace(layoutId))
            {
                // 切换视图后，下拉列表绑定当前视图
                this.Model.DataObject["F_260_ST"] = layoutId;
                this.View.UpdateView("F_260_ST");
            }          
        }
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (e.Field.Key.EqualsIgnoreCase("F_260_ST"))
            {
                var layoutId = e.NewValue as string;
                var para = new BillShowParameter();
                CopyCustomeParameter(para);
                para.OpenStyle.ShowType = ShowType.InCurrentForm;
                para.FormId = this.View.OpenParameter.FormId;
                //para.DefaultBillTypeId = ((DynamicObject)this.Model.GetValue(this.View.BillBusinessInfo.GetBillTypeField())).ToString();
                para.LayoutId = layoutId;
                para.Status = this.View.OpenParameter.Status;
                para.PKey = null;
                para.ParentPageId = this.View.OpenParameter.ParentPageId;
                // PageId必须确保一致
                para.PageId = this.View.PageId;
                // 本view标记为已经失效，需要重新构建
                this.View.OpenParameter.IsOutOfTime = true;
                para.CustomParams["Jac_LayoutId"] = layoutId;
                this.View.ShowForm(para);
            }
            
        }
        /// <summary>
        /// 复制用户定制的属性
        /// </summary>
        /// <param name="para"></param>
        private void CopyCustomeParameter(BillShowParameter para)
        {
            var openParamType = this.View.OpenParameter.GetType();
            var properties = openParamType.GetProperties();
            var IsnotCustomerPropertyKeys = new List<string>() { "pk", "billType" };
            // 复制定制参数
            var customerParams = this.View.OpenParameter.GetCustomParameters();
            if (customerParams != null && customerParams.Count > 0)
            {
                foreach (var item in customerParams)
                {
                    if (IsnotCustomerPropertyKeys.Contains(item.Key)) continue;
                    if (item.Value == null) continue;
                    if (!(item.Value is string)) continue; // 只复制字符串参数
                    var prop = properties.FirstOrDefault(p => p.Name.EqualsIgnoreCase(item.Key));
                    if (prop != null) continue;
                    para.CustomParams[item.Key] = Convert.ToString(item.Value);
                }
            }
        }
        /// <summary>
        /// 将当前业务对象的扩展节点上的视图，绑定到下拉列表
        /// </summary>
        private void BindComboField()
        {
            var enumList = GetEnumItems();
            var comboList = this.View.GetFieldEditor<ComboFieldEditor>("F_260_ST", 0);
            if (comboList != null)
            {
                comboList.SetComboItems(enumList);
            }              
        }    
        /// <summary>
        /// 从数据库读取自定义数据源并转换成枚举项集合
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        private List<EnumItem> GetEnumItems()
        {
            // 获取视图数据
            var enumList = new List<EnumItem>();
            var enumItem1 = new EnumItem();
            enumItem1.Caption = new LocaleValue("默认");
            enumItem1.EnumId = "";
            enumItem1.Invalid = false;
            enumItem1.Value = "";
            enumList.Add(enumItem1);
           
            var sql = string.Format(@"SELECT a.FID,b.FNAME FROM T_META_OBJECTTYPEVIEW a
LEFT JOIN T_META_OBJECTTYPEVIEW_L b ON a.FID=b.FID AND b.FLOCALEID=2052
JOIN T_META_OBJECTTYPE c ON a.FDEPENDENCYOBJECTID=c.FID 
WHERE c.FINHERITPATH LIKE '%,{0},%'  AND c.FDEVTYPE=2  and b.FNAME like '%260%'
", this.View.BillBusinessInfo.GetForm().Id);
            var objs = DBUtils.ExecuteDynamicObject(Context, sql);
            if (objs != null && objs.Count > 0)
            {
                foreach (var obj in objs)
                {
                    var enumItem = new EnumItem();
                    enumItem = new EnumItem();
                    enumItem.Caption = new LocaleValue(obj["FNAME"].ToString());
                    enumItem.EnumId = obj["FID"].ToString();
                    enumItem.Invalid = false;
                    enumItem.Value = obj["FID"].ToString();
                    enumList.Add(enumItem);
                }
            }
            return enumList;
        }
        public override void PreOpenForm(PreOpenFormEventArgs e)
        {
            base.PreOpenForm(e);
           
            try
            {               
                var z = e.OpenParameter.GetCustomParameters()["Pk"];
                string cxsql = $@"select * from T_SAL_PRICELIST where FID={z} and F_260_ST='72cc4e3d-9498-44a0-b53a-fafca7db4825'";
                var cx = DBUtils.ExecuteDynamicObject(e.Context, cxsql);
                if (cx.Count > 0)
                {
                    e.OpenParameter.LayoutId = "72cc4e3d-9498-44a0-b53a-fafca7db4825";
                }
            }
            catch
            {

            }
            finally
            {
                if (e.OpenParameter.Status == OperationStatus.ADDNEW && 
                    (e.Context.UserId==6481124 || e.Context.UserId == 22797700 ||
                    e.Context.UserId == 15701258 || e.Context.UserId == 1410291))
                {
                    e.OpenParameter.LayoutId = "72cc4e3d-9498-44a0-b53a-fafca7db4825";
                }
            }
         }
    }
}
