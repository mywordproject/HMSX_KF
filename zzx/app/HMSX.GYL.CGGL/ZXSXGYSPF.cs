using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.ComponentModel;

namespace HMSX.GYL.CGGL
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("双新供应商评分表")]
    public class ZXSXGYSPF : AbstractBillPlugIn
    {
        public override void DataChanged(DataChangedEventArgs e)
        {
            try
            {
                base.DataChanged(e);
                long orid = this.Context.CurrentOrganizationInfo.ID;
                EntryEntity entry = this.View.BillBusinessInfo.GetEntryEntity("F_HMD_Entity");
                SubEntryEntity zlEntry = this.View.BillBusinessInfo.GetEntryEntity("F_keed_SubEntity") as SubEntryEntity;
                SubEntryEntity cgEntry = this.View.BillBusinessInfo.GetEntryEntity("F_keed_SubEntity1") as SubEntryEntity;
                SubEntryEntity gcEntry = this.View.BillBusinessInfo.GetEntryEntity("F_keed_SubEntity2") as SubEntryEntity;
                //合格供应商
                if (e.Field.Key == "F_260_HGGYS" && orid == 100026)
                {
                    DynamicObject entryobj = this.Model.GetEntityDataObject(entry, e.Row);
                    //质量
                    this.View.Model.DeleteEntryData("F_keed_SubEntity");
                    string zlsql = "/*dialect*/select FID from keed_t_Cust100346 where FDOCUMENTSTATUS='C' and FFORBIDSTATUS='A' and F_KEED_COMBO='1'";
                    DynamicObjectCollection zlobjs = DBUtils.ExecuteDynamicObject(this.Context, zlsql);
                    for (int i = 0; i < zlobjs.Count; i++)
                    {
                        this.Model.CreateNewEntryRow(entryobj, zlEntry, -1);
                        this.View.Model.SetValue("F_keed_Base4", zlobjs[i]["FID"], i);
                        this.View.InvokeFieldUpdateService("F_keed_Base4", i);
                    }
                    this.View.UpdateView("F_keed_SubEntity");
                    //采购
                    this.View.Model.DeleteEntryData("F_keed_SubEntity1");
                    string cgsql = "/*dialect*/select FID from keed_t_Cust100346 where FDOCUMENTSTATUS='C' and FFORBIDSTATUS='A' and F_KEED_COMBO='2'";
                    DynamicObjectCollection cgobjs = DBUtils.ExecuteDynamicObject(this.Context, cgsql);
                    for (int i = 0; i < cgobjs.Count; i++)
                    {
                        this.Model.CreateNewEntryRow(entryobj, cgEntry, -1);
                        this.View.Model.SetValue("F_keed_Base5", cgobjs[i]["FID"], i);
                        this.View.InvokeFieldUpdateService("F_keed_Base5", i);
                    }
                    this.View.UpdateView("F_keed_SubEntity1");
                    //工程               
                    this.View.Model.DeleteEntryData("F_keed_SubEntity2");
                    string gcsql = "/*dialect*/select FID from keed_t_Cust100346 where FDOCUMENTSTATUS='C' and FFORBIDSTATUS='A' and F_KEED_COMBO='3'";
                    DynamicObjectCollection gcobjs = DBUtils.ExecuteDynamicObject(this.Context, gcsql);
                    for (int i = 0; i < gcobjs.Count; i++)
                    {
                        this.Model.CreateNewEntryRow(entryobj, gcEntry, -1);
                        this.View.Model.SetValue("F_keed_Base6", gcobjs[i]["FID"], i);
                        this.View.InvokeFieldUpdateService("F_keed_Base6", i);
                    }
                    this.View.UpdateView("F_keed_SubEntity2");
                    //获取入库单数
                    DynamicObject gys = (DynamicObject)this.View.Model.GetValue("F_260_HGGYS", e.Row);
                    long gysid = Convert.ToInt64(gys["Id"]);
                    var date = this.View.Model.GetValue("F_keed_Date");                   
                    string rksql = $"/*dialect*/select FBILLNO from t_STK_InStock where FSUPPLIERID={gysid} and DATEPART(YY,FDATE)=DATEPART(YY,'{date}') and DATEPART(MM,FDATE)=DATEPART(MM,'{date}')";
                    DynamicObjectCollection obj = DBUtils.ExecuteDynamicObject(this.Context, rksql);
                    this.View.Model.SetValue("F_keed_Integer", obj.Count, e.Row);
                }
                //评估等级
                else if (e.Field.Key == "F_keed_Base" && orid == 100026)
                {
                    var fbillno = this.View.Model.GetValue("FBillNo");
                    string bcsql = fbillno == null ? "" : $" and FBILLNO!='{fbillno}'";
                    DynamicObject gys = (DynamicObject)this.View.Model.GetValue("F_260_HGGYS", e.Row);
                    long gysid = Convert.ToInt64(gys["Id"]);
                    string sql = $@"/*dialect*/select * from
                    (select top 2 F_KEED_BASE from keed_t_Cust100345 a
                    inner join keed_t_Cust_Entry100331 b on a.FID = b.FID
                    where F_260_HGGYS = {gysid}{bcsql} order by FCREATEDATE DESC) Z 
                    where F_KEED_BASE=25207220";
                    DynamicObjectCollection objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
                    DynamicObject pgdj = (DynamicObject)this.View.Model.GetValue("F_keed_Base", e.Row);
                    long pgdjid = Convert.ToInt64(pgdj["Id"]);
                    if (objs.Count >= 2 && pgdjid == 25207220)
                    {
                        this.View.Model.SetValue("F_KEED_COMBO", "0", e.Row);
                    }
                    else
                    {
                        this.View.Model.SetValue("F_KEED_COMBO", "1", e.Row);
                    }
                }
            }            
            catch
            {
                return;
            }
        }
    }
}
