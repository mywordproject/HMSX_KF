using HMSX.Second.Plugin.Tool;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.生产制造
{
    [Description("生产订单---带出比例")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class SCDDBill : AbstractBillPlugIn
    {
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);            
            if (Context.CurrentOrganizationInfo.ID == 100026)
            {
                var TEXTSJRW = this.Model.GetValue("F_260_TEXTSJRW");
                if (TEXTSJRW != null && TEXTSJRW.ToString()!= ""&& TEXTSJRW.ToString() != " ")
                {
                    string scsql = $@"select F_260_SCDDBHQF,FDESCRIPTION FROM T_PRD_MO A
                                    LEFT JOIN T_PRD_MO_L B ON A.FID=B.FID where FBILLNO='{TEXTSJRW.ToString()}'";
                    var scddqfs = DBUtils.ExecuteDynamicObject(Context, scsql);
                    foreach(var scddqf in scddqfs)
                    {
                        this.Model.SetValue("F_260_SCDDBHQF", scddqf["F_260_SCDDBHQF"]);
                        if(this.Model.GetValue("FDESCRIPTION") ==null || this.Model.GetValue("FDESCRIPTION").ToString()=="" || this.Model.GetValue("FDESCRIPTION").ToString() == " ")
                        {
                            this.Model.SetValue("FDESCRIPTION", scddqf["FDESCRIPTION"]);
                            this.View.UpdateView("FDESCRIPTION");
                        }                        
                        this.View.UpdateView("F_260_SCDDBHQF");
                        
                    }
                }
                var dates = this.Model.DataObject["TreeEntity"] as DynamicObjectCollection;
                string HH = "";
                string fsrbill ="";
                foreach (var date in dates)
                {                  
                    if (date["SrcBillNo"] != null && date["F_260_HH"] != null)
                    {
                        if(date["SrcBillNo"].ToString()!="" && date["F_260_HH"].ToString() != "" &&
                           date["SrcBillNo"].ToString() != " " && date["F_260_HH"].ToString() != " ")
                        {
                            fsrbill += "'" + date["SrcBillNo"] + "'" + ",";
                            HH += date["F_260_HH"] + ",";
                        }                        
                    }
                    var wl = Utils.LoadBDData(Context, "BD_MATERIAL", Convert.ToInt32(date["MaterialId_Id"]));
                    if (wl != null)
                    {
                        var MaterialProduce = wl["MaterialProduce"] as DynamicObjectCollection;
                        if (MaterialProduce.Count > 0)
                        {
                            this.Model.SetValue("FSTOCKINULRATIO", MaterialProduce[0]["FinishReceiptOverRate"], Convert.ToInt32(date["Seq"]) - 1);
                            this.Model.SetValue("FSTOCKINLIMITH", Convert.ToDecimal(date["Qty"]) * (1 + Convert.ToDecimal(MaterialProduce[0]["FinishReceiptOverRate"]) / 100), Convert.ToInt32(date["Seq"]) - 1);
                        }
                    }
                    
                }
                if (((DynamicObject)this.Model.GetValue("FBILLTYPE"))["Id"].ToString() == "0e74146732c24bec90178b6fe16a2d1c" && HH!="")
                {
                    foreach (var date in dates)
                    {
                        string sccjsql = $@"select e.fnumber bomnumber,g.FNUMBER,FWORKSHOPID,E.FID BOM,F.FID GYLX from  t_BD_MaterialProduce d 
                             left join T_ENG_BOM e on e.FMATERIALID=d .FMATERIALID
                             left join T_ENG_ROUTE f on f.FMATERIALID=d .FMATERIALID
                             left join T_BD_MATERIAL g on g.FMATERIALID=d .FMATERIALID
                             where d.FMATERIALID={date["MaterialId_Id"]}
                             order by e.fnumber desc";
                        var sccj = DBUtils.ExecuteDynamicObject(Context, sccjsql);
                        this.View.Model.SetItemValueByID("FWORKSHOPID", Convert.ToInt64(sccj[0]["FWORKSHOPID"]), Convert.ToInt32(date["Seq"]) - 1);
                        this.View.Model.SetItemValueByID("FBOMID", Convert.ToInt64(sccj[0]["BOM"]), Convert.ToInt32(date["Seq"]) - 1);
                        this.View.Model.SetItemValueByID("FROUTINGID", Convert.ToInt64(sccj[0]["GYLX"]), Convert.ToInt32(date["Seq"]) - 1);
                        this.View.UpdateView("FWorkShopID", Convert.ToInt32(date["Seq"]) - 1);
                        this.View.UpdateView("FBomId", Convert.ToInt32(date["Seq"]) - 1);
                        this.View.UpdateView("FRoutingId", Convert.ToInt32(date["Seq"]) - 1);
                        this.View.InvokeFieldUpdateService("FQTY", Convert.ToInt32(date["Seq"]) - 1);
                    }
                    string frbillno = this.Model.GetValue("FSRCBILLNO", 0).ToString();
                    string kczhsql = $@"select b.FMATERIALID,F_260_FGWL,sum(FCONVERTQTY)FCONVERTQTY from T_STK_StockConvert a
                                       inner join T_STK_StockConvertEntry b on a.fid=b.FID
                                       inner join  SLSB_t_Cust_Entry100350 c on b.fentryid=c.fentryid
                                       where FBILLNO in ({fsrbill.Trim(',')})
                                       and FCONVERTTYPE='B'
                                       and b.FSEQ in({HH.Trim(',')})
                                       group by b.FMATERIALID,F_260_FGWL";
                    var kczhs = DBUtils.ExecuteDynamicObject(Context, kczhsql);
                    int i = dates.Count;
                    foreach (var kczh in kczhs)
                    {
                        this.Model.CreateNewEntryRow("FTreeEntity");
                        this.Model.SetItemValueByID("FMATERIALID", kczh["F_260_FGWL"], i);
                        this.Model.SetValue("FQTY", kczh["FCONVERTQTY"], i);
                        this.View.InvokeFieldUpdateService("FMATERIALID", i);
                        this.View.InvokeFieldUpdateService("FQTY", i);
                        i++;
                    }
                    this.View.UpdateView("FTreeEntity");
                }
                if (((DynamicObject)this.Model.GetValue("FBILLTYPE"))["Id"].ToString() == "0e74146732c24bec90178b6fe16a2d1c")
                {
                    foreach (var date in dates)
                    {
                        if(date["SrcBillType"].ToString()== "QM_DefectProcessBill")
                        {
                            string wlqdsql = $@"select top 1 FID,FNUMBER from T_ENG_BOM where FMATERIALID='{date["MaterialId_Id"]}'
                             order by FNUMBER desc";
                            var wlqd = DBUtils.ExecuteDynamicObject(Context, wlqdsql);
                            if (wlqd.Count > 0)
                            {
                                this.Model.SetItemValueByID("FBOMID", Convert.ToInt64(wlqd[0]["FID"]), Convert.ToInt32(date["Seq"]) - 1);
                                this.View.InvokeFieldUpdateService("FBomId", Convert.ToInt32(date["Seq"]) - 1);
                            }
                        }                                     
                    }
                    this.View.UpdateView("FTreeEntity");
                }
            }
        }
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (e.Field.Key == "FMaterialId")
            {
                string sczz = this.Model.GetValue("FPRDORGID") == null ? "" : ((DynamicObject)this.Model.GetValue("FPRDORGID"))["Id"].ToString();
                if (sczz == "100026")
                {
                    int wlid = this.Model.GetValue("FMATERIALID", e.Row) == null ? 0 : Convert.ToInt32(((DynamicObject)this.Model.GetValue("FMATERIALID", e.Row))["Id"].ToString());
                    var wl = Utils.LoadBDData(Context, "BD_MATERIAL", wlid);
                    if (wl != null)
                    {
                        var MaterialProduce = wl["MaterialProduce"] as DynamicObjectCollection;
                        if (MaterialProduce.Count > 0)
                        {
                            this.Model.SetValue("FSTOCKINULRATIO", MaterialProduce[0]["FinishReceiptOverRate"], e.Row);
                            decimal sl = Convert.ToDecimal(this.Model.GetValue("FQTY", e.Row));
                            this.Model.SetValue("FSTOCKINLIMITH", sl * (1 + Convert.ToDecimal(MaterialProduce[0]["FinishReceiptOverRate"]) / 100), e.Row);
                        }
                    }
                }
            }
        }
        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);
            //var dates = this.Model.DataObject["TreeEntity"] as DynamicObjectCollection;
            //if (e.BarItemKey.Equals("tbSplitSubmit") || e.BarItemKey.Equals("tbSubmit"))
            //{
            //    this.View.Refresh();
            //}
        }
        public override void BeforeF7Select(BeforeF7SelectEventArgs e)
        {
            base.BeforeF7Select(e);
            if (this.Context.CurrentOrganizationInfo.ID == 100026)
            {
                if (e.FieldKey.EqualsIgnoreCase("F_260_XMHH"))
                {
                    string wl = this.Model.GetValue("FMATERIALID", e.Row) == null ? "" : ((DynamicObject)this.Model.GetValue("FMATERIALID", e.Row))["Id"].ToString();
                    string cxsql = $@"select 
                                        XMH.F_260_XMH
                                        from T_BD_MATERIAL a
                                        left join t_BD_MaterialPlan c on c.FMATERIALID=a.FMATERIALID
                                        left join T_PLN_MANUFACTUREPOLICY d on c.FMFGPOLICYID=d.FID
                                        LEFT JOIN PAEZ_t_Cust_Entry100355 XMH ON XMH.FMATERIALID=A.FMATERIALID
                                        WHERE 
                                        --D.FNUMBER='ZZCL003_SYS'
                                        --and 
                                        a.FMATERIALID='{wl}'
                                        and FCREATEORGID=100026
                                       and XMH.F_260_XMH is not null";
                    var cxs = DBUtils.ExecuteDynamicObject(Context, cxsql);
                    string str = "";
                    foreach (var cx in cxs)
                    {
                        str += cx["F_260_XMH"].ToString() + ",";
                    }
                    string xmh = "FID=0";
                    if (cxs.Count > 0)
                    {
                        xmh = "FID" + " in (" + str.Trim(',') + " )";
                    }

                    e.ListFilterParameter.Filter = e.ListFilterParameter.Filter.JoinFilterString(xmh);
                    return;
                }
                //if (e.FieldKey.EqualsIgnoreCase("F_260_XMH1"))
                //{
                //    string xmhsql = $@"select A.FID,A.FNUMBER from ora_t_Cust100045 A
                //                   LEFT JOIN ora_t_Cust100045GROUP B ON B.FID=A.F_260_GROUPFZ
                //                   left join ora_t_Cust100045GROUP_L C ON B.FPARENTID=C.FID
                //                   where C.FNAME='新能源'";
                //    var xmhs = DBUtils.ExecuteDynamicObject(Context, xmhsql);
                //    string str1 = "";
                //    foreach (var xmh in xmhs)
                //    {
                //        str1 += xmh["FID"].ToString() + ",";
                //    }
                //    string xmhh = "FID<>0";
                //    if (xmhs.Count > 0)
                //    {
                //        xmhh = "FID" + " not in (" + str1.Trim(',') + " )";
                //    }
                //    e.ListFilterParameter.Filter = e.ListFilterParameter.Filter.JoinFilterString(xmhh);
                //    return;
                //}
            }
        }
        public override void AfterDoOperation(AfterDoOperationEventArgs e)
        {
            base.AfterDoOperation(e);
            if (Context.CurrentOrganizationInfo.ID == 100026)
            {
                if (e.Operation.OperationId == 8 && e.Operation.Operation == "Save" && e.OperationResult.IsSuccess)
                {
                    this.View.InvokeFormOperation("Refresh");
                    //    // 保存后刷新字段
                    //    var loadKeys = e.Operation.ReLoadKeys == null ? new List<string>() : new List<string>(e.Operation.ReLoadKeys);
                    //    ((IBillModel)this.Model).SynDataFromDB(loadKeys);
                }
            }
        }
    }
}
