using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.Complex;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using System.ComponentModel;
using Kingdee.BOS.Resource;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Validation;

namespace HMSX.MFG.Mobile.Business.PlugIn
{
    [Description("派工工序汇报编辑-表单插件")]
    [Kingdee.BOS.Util.HotUpdate]
    public  class NComplexDispatchReportEdit: ComplexDispatchReportEdit
    {
        private long dispEntryId = 0L;
        private long MaterialId = 0L;

        public override void OnInitialize(InitializeEventArgs e)
        {
            base.OnInitialize(e);
            this.View.GetControl("FLable_User").SetValue(this.Context.UserName);
        }
        public override void AfterBindData(EventArgs e)
        {
           base.AfterBindData(e);
           DynamicObjectCollection dynamicObjectCollection = (DynamicObjectCollection)base.View.BillModel.DataObject["OptRptEntry"];
             dispEntryId = Convert.ToInt64(dynamicObjectCollection[0]["DispatchDetailEntryId"]);
             MaterialId = Convert.ToInt64(dynamicObjectCollection[0]["MaterialId_Id"]);
            string strSql = string.Format("select F_LOT,F_LOT_Text,F_RUJP_Lot,FBARCODE,F_260_NBBBH,F_260_WBBBH,F_260_KHBQ from T_SFC_DISPATCHDETAILENTRY where FENTRYID={0}", dispEntryId);
            DynamicObjectCollection rs = DBServiceHelper.ExecuteDynamicObject(this.Context, strSql);
            if (rs.Count > 0)
            {
                // this.View.BillModel.SetValue("FLot", rs[0]["F_LOT_Text"].ToString());
                this.View.BillModel.SetValue("FLot", rs[0]["F_RUJP_Lot"].ToString());
                this.View.BillModel.SetValue("F_SBID_BARCODE", rs[0]["FBARCODE"].ToString());
                this.View.BillModel.SetValue("F_260_NBBBH", rs[0]["F_260_NBBBH"].ToString());
                this.View.BillModel.SetValue("F_260_WBBBH", rs[0]["F_260_WBBBH"].ToString());
                this.View.BillModel.SetValue("FHMSXKHBQYD", rs[0]["F_260_KHBQ"].ToString());
                this.View.UpdateView("F_HMD_MobileProxyField5");
                this.View.UpdateView("FMobileProxyField_PgBarcode");
                this.View.UpdateView("F_SLSB_MobileProxyField1");
                this.View.UpdateView("F_SLSB_MobileProxyField");
                this.View.UpdateView("F_HMD_MobileProxyField4");
            }
        }
        public override void ButtonClick(ButtonClickEventArgs e)
        {
            string a;
            if ((a = e.Key.ToUpper()) != null)
            {
                if (a == "FBUTTON_SUBMIT")
                {
                    DynamicObject date= this.View.BillModel.DataObject;
                    DynamicObjectCollection dynamicObjectCollection = date["OptRptEntry"] as DynamicObjectCollection;
                    DynamicObject Material =dynamicObjectCollection[0]["MaterialId"] as DynamicObject;
                    string materialNum = Material["Number"].ToString();
                    string strSql = string.Format(@"SELECT count(FPgEntryId) as Fcount,sum(FAvailableQty) as Fqty, sum(FMustQty) as FMustQty FROM t_PgBomInfo WHERE FPgEntryId={0} ", dispEntryId);
                    //AND FMaterialId NOT IN (SELECT FMATERIALID FROM T_BD_MATERIAL WHERE FNUMBER like '260.07%'  )
                    DynamicObjectCollection rs = DBServiceHelper.ExecuteDynamicObject(this.Context, strSql);
                    if(materialNum.Substring(0, 6) != "260.07")
                    {
                        if (rs.Count > 0  )
                        {
                            string flfssql = $@"select A.FMOBILLNO,A.FMOENTRYSEQ,FISSUETYPE from T_PRD_PPBOM a
                                                inner join T_PRD_PPBOMENTRY b on a.fid=b.fid
                                                inner join T_PRD_PPBOMENTRY_C C on C.FENTRYID=b.FENTRYID
                                                where FPRDORGID=100026 and FISSUETYPE=7
                                                AND A.FMOBILLNO='{dynamicObjectCollection[0]["MoNumber"].ToString()}' AND A.FMOENTRYSEQ='{dynamicObjectCollection[0]["MoRowNumber"].ToString()}'";
                            var flfs = DBUtils.ExecuteDynamicObject(Context, flfssql);

                            if (Convert.ToInt16(rs[0]["Fcount"]) > 0)
                            {
                                if (Convert.ToDecimal(rs[0]["Fqty"]) < Convert.ToDecimal(rs[0]["FMustQty"]) * Convert.ToDecimal(0.98))
                                {
                                    base.View.ShowMessage(ResManager.LoadKDString("当前派工明细领料未完成，不允许报工！", "015747000026506", SubSystemType.MFG, new object[0]), MessageBoxType.Notice);
                                    e.Cancel = true;
                                    return;
                                }
                            }
                            else if(Convert.ToInt16(rs[0]["Fcount"]) <=0 && flfs.Count<=0)
                            {
                                base.View.ShowMessage(ResManager.LoadKDString("当前派工明细领料未完成，不允许报工！", "015747000026506", SubSystemType.MFG, new object[0]), MessageBoxType.Notice);
                                e.Cancel = true;
                                return;
                            }
                        }
                    }                    
                }
                else if (a == "F_ZLBG")
                {
                    DynamicObject date = this.View.BillModel.DataObject;
                    DynamicObjectCollection dynamicObjectCollection = date["OptRptEntry"] as DynamicObjectCollection;
                    //生产订单
                    string scddsql = $@"select A.FID ID,B.FENTRYID ENTRYID,* from T_PRD_MO a
                        inner join T_PRD_MOENTRY b on a.fid = b.fid
                        inner join T_PRD_MOENTRY_A C ON B.FENTRYID=C.FENTRYID
						inner join T_PRD_MOENTRY_C D ON B.FENTRYID=D.FENTRYID
						inner join T_PRD_MOENTRY_Q E ON B.FENTRYID=E.FENTRYID
                        WHERE 
                        FPRDORGID=100026 and(
                        FPARENTROWID IN(
                        select b.FROWID from T_PRD_MO a
                        inner join T_PRD_MOENTRY b on a.fid = b.fid
                        where fbillno = '{dynamicObjectCollection[0]["MoNumber"]}'
                        AND FSEQ = '{dynamicObjectCollection[0]["MoRowNumber"]}')
                        AND fbillno = '{dynamicObjectCollection[0]["MoNumber"]}'
                        or
                        fbillno = '{dynamicObjectCollection[0]["MoNumber"]}'
                        AND FSEQ = '{dynamicObjectCollection[0]["MoRowNumber"]}')";
                    var scdds = DBUtils.ExecuteDynamicObject(Context, scddsql);
                    
                    for (int i = 0; i < scdds.Count - 1; i++)
                    {
                        // var gainEntry = Utils.LoadBillDataType(Context, "SFC_OperationReport", "FEntity");
                        DynamicObject obj = dynamicObjectCollection.Last<DynamicObject>();
                        DynamicObject newRow = (DynamicObject)obj.Clone(false, true);
                        dynamicObjectCollection.Add(newRow);
                    }
                    int j = 0;
                    foreach (var obj in dynamicObjectCollection)
                    {
                        obj["Seq"] = j + 1;
                        obj["MoNumber"] = scdds[j]["FBILLNO"];
                        obj["MoRowNumber"] = scdds[j]["FSEQ"];
                        obj["PrdType"] = scdds[j]["FPRODUCTTYPE"]; //产品类型
                        if (scdds[j]["FPRODUCTTYPE"].ToString() == "2")
                        {
                            //物料清单
                            string wlqdsql = $@"select a.FMATERIALID,b.FMATERIALID,b.FQTY from T_ENG_BOM a
                                      left join T_ENG_BOMCOBY b on a.fid=b.fid
                                      where a.FMATERIALID={dynamicObjectCollection[0]["MaterialId_Id"]} and b.FMATERIALID={ scdds[j]["FMATERIALID"]}";
                            var wlqd = DBUtils.ExecuteDynamicObject(Context, wlqdsql);

                            obj["MoNumber"] = scdds[j]["FBILLNO"];//生产订单行号
                            obj["MoRowNumber"] = scdds[j]["FSEQ"];//生产订单行号
                            obj["MaterialId_Id"] = scdds[j]["FMATERIALID"];
                            obj["AuxPropId_Id"] = scdds[j]["FAUXPROPID"];//辅助属性                                    
                            obj["FailQty"] = 0;//废品数量
                            obj["FSourceBillNo"] = scdds[j]["FBILLNO"];//源单编号
                            obj["SrcInterId"] = scdds[j]["ID"];//源单内码
                            obj["SrcEntrySeq"] = scdds[j]["FSEQ"];//源单行号
                            obj["SrcEntryId"] = scdds[j]["ENTRYID"];//源单分录内码                                                       
                            obj["ProjectNo"] = scdds[j]["FPROJECTNO"];//项目编号
                            obj["SeqType"] = "M";//序列类型
                            obj["BaseQuaQty"] = scdds[j]["FCHECKPRODUCT"].ToString() != "0" ? 0 : Convert.ToDouble(obj["QuaQty"]) * Convert.ToDouble(wlqd[0]["FQTY"]);//基本合格数量
                            obj["BaseFailQty"] = 0;//基本单位不合格数量
                            obj["BaseReworkQty"] = 0;//基本单位带返修数量
                            obj["BaseFinishQty"] = Convert.ToDouble(obj["FinishQty"]) * Convert.ToDouble(wlqd[0]["FQTY"]);//基本单位完工数量
                            obj["OwnerTypeId"] = scdds[j]["FINSTOCKOWNERTYPEID"];//货主类型
                            obj["OwnerId_Id"] = scdds[j]["FINSTOCKOWNERID"];//入库货主
                            obj["BomId_Id"] = scdds[j]["FBOMID"];//BOM版本
                            obj["FBFLowId_Id"] = scdds[j]["FBFLOWID"];//业务流程        
                            obj["PrdQuaQty"] = scdds[j]["FCHECKPRODUCT"].ToString() != "0" ? 0 : Convert.ToDouble(obj["QuaQty"]) * Convert.ToDouble(wlqd[0]["FQTY"]);//生产单位合格数量
                            obj["PrdFinishQty"] = Convert.ToDouble(obj["FinishQty"]) * Convert.ToDouble(wlqd[0]["FQTY"]);//生产单位完工数量
                            obj["FMtoNo"] = scdds[j]["FMTONO"];//计划跟踪号
                            obj["RouteId_Id"] = scdds[j]["FROUTINGID"];//工艺路线
                            obj["InStockType"] = scdds[j]["FINSTOCKTYPE"];//入库类型-供下推入库单使用
                            obj["UnitTransHeadQty"] = 1;//单位转换表头数量
                            obj["UnitTransOperQty"] = 1;//单位转换工序数量
                            obj["OptBillCreatType"] = "A";//单据生成方式（工序汇报）
                            obj["StockId_Id"] = scdds[j]["FSTOCKID"];//仓库
                            obj["StockLocId_Id"] = scdds[j]["FSTOCKLOCID"];//仓位
                            obj["CheckType"] = scdds[j]["FCHECKPRODUCT"].ToString() != "1" ? 1 : 3;//检验方式                         
                            obj["WaitCheckQty"] =Convert.ToInt32(scdds[j]["FCHECKPRODUCT"])!= 1 ? 0 : Convert.ToDouble(obj["FinishQty"]) * Convert.ToDouble(wlqd[0]["FQTY"]);//初始待送检数量
                            obj["PrdWaitCheckQty"] = Convert.ToInt32(scdds[j]["FCHECKPRODUCT"]) != 1 ? 0 : Convert.ToDouble(obj["FinishQty"]) * Convert.ToDouble(wlqd[0]["FQTY"]);//生产单位初始待送检数量
                            obj["BaseWaitCheckQty"] = Convert.ToInt32(scdds[j]["FCHECKPRODUCT"]) != 1 ? 0 : Convert.ToDouble(obj["FinishQty"]) * Convert.ToDouble(wlqd[0]["FQTY"]);//基本单位初始待送检数量
                            obj["FSourceBillType"] = "PRD_MO";//源单类型                           
                            obj["OptPlanNo"] = "";//工序计划单号
                            obj["OptPlanSeqId"] = 0;//工序计划工序序列内码
                            obj["OptPlanOptId"] = 0;//工序计划工序内码
                            obj["FBFLowId"] = null;//业务流程
                            obj["FBFLowId_Id"] = "";//业务流程
                            obj["F_260_RKD"] = 0;//入库点
                            //obj["F_SBID_BARCODE"] = "";
                            obj["Activity1Qty"] = 0;
                            obj["CostRate"] = scdds[j]["FCOSTRATE"];//成本权重
                            obj["MouldId_Id"] = 0;
                            //obj["Lot_Id"] = 0;
                            obj["DispatchDetailEntryId"] = 0;//派工id
                            obj["F_PAEZ_Qty"] = 0;//额定数量
                            obj["QuaQty"] = scdds[j]["FCHECKPRODUCT"].ToString() != "0" ? 0 : Convert.ToDouble(obj["QuaQty"]) * Convert.ToDouble(wlqd[0]["FQTY"]);//合格数量
                            obj["FinishQty"] = Convert.ToDouble(obj["FinishQty"]) * Convert.ToDouble(wlqd[0]["FQTY"]);//完工数量
                            DynamicObjectCollection entry_lks = obj["FEntity_Link"] as DynamicObjectCollection;
                            foreach (var lk in entry_lks)
                            {
                                lk["SBillId"] = scdds[j]["ID"];//源单内码
                                lk["SId"] = scdds[j]["ENTRYID"];
                                //lk["FlowId"] = "";
                                lk["RuleId"] = "SFC_MO2OPTRPT";
                                lk["STableName"] = "T_PRD_MOENTRY";
                                //lk["BaseFinishQtyOld"] = "";
                            }                        
                        }
                        obj["MoEntryId"] = scdds[j]["ENTRYID"];//生产订单分录内码
                        j++;
                    }

                    DynamicObject Material = dynamicObjectCollection[0]["MaterialId"] as DynamicObject;
                    string materialNum = Material["Number"].ToString();
                    string strSql = string.Format(@"SELECT count(FPgEntryId) as Fcount,sum(FAvailableQty) as Fqty, sum(FMustQty) as FMustQty FROM t_PgBomInfo WHERE FPgEntryId={0} ", dispEntryId);
                    //AND FMaterialId NOT IN (SELECT FMATERIALID FROM T_BD_MATERIAL WHERE FNUMBER like '260.07%'  )
                    DynamicObjectCollection rs = DBServiceHelper.ExecuteDynamicObject(this.Context, strSql);
                    if (materialNum.Substring(0, 6) != "260.07")
                    {
                        if (rs.Count > 0)
                        {
                            string flfssql = $@"select A.FMOBILLNO,A.FMOENTRYSEQ,FISSUETYPE from T_PRD_PPBOM a
                                                inner join T_PRD_PPBOMENTRY b on a.fid=b.fid
                                                inner join T_PRD_PPBOMENTRY_C C on C.FENTRYID=b.FENTRYID
                                                where FPRDORGID=100026 and FISSUETYPE=7
                                                AND A.FMOBILLNO='{dynamicObjectCollection[0]["MoNumber"].ToString()}' AND A.FMOENTRYSEQ='{dynamicObjectCollection[0]["MoRowNumber"].ToString()}'";
                            var flfs = DBUtils.ExecuteDynamicObject(Context, flfssql);

                            if (Convert.ToInt16(rs[0]["Fcount"]) > 0)
                            {
                                if (Convert.ToDecimal(rs[0]["Fqty"]) < Convert.ToDecimal(rs[0]["FMustQty"]) * Convert.ToDecimal(0.98))
                                {
                                    base.View.ShowMessage(ResManager.LoadKDString("当前派工明细领料未完成，不允许报工！", "015747000026506", SubSystemType.MFG, new object[0]), MessageBoxType.Notice);
                                    e.Cancel = true;
                                    return;
                                }
                            }
                            else if (Convert.ToInt16(rs[0]["Fcount"]) <= 0 && flfs.Count <= 0)
                            {
                                base.View.ShowMessage(ResManager.LoadKDString("当前派工明细领料未完成，不允许报工！", "015747000026506", SubSystemType.MFG, new object[0]), MessageBoxType.Notice);
                                e.Cancel = true;
                                return;
                            }
                        }
                    }

                    FormMetadata cachedFormMetaData = FormMetaDataCache.GetCachedFormMetaData(base.Context, "SFC_OperationReport");
                    OperateOption option = OperateOption.Create();
                    option.SetVariableValue("MobileBizType", "Dispatch");
                    option.SetVariableValue("IsMobileInvoke", true);
                    option.SetVariableValue("AutoAudit", true);
                    IOperationResult operationResult = BusinessDataServiceHelper.Save(base.Context, cachedFormMetaData.BusinessInfo, date, option, "");
                    if (operationResult.IsSuccess)
                    {
                        base.View.ShowStatusBarInfo(ResManager.LoadKDString("报工成功！", "015747000015470", SubSystemType.MFG, new object[0]));
                        this.View.Close();
                    }
                    else
                    {
                        StringBuilder stringBuilder = new StringBuilder();
                        if (operationResult.ValidationErrors.Count > 0)
                        {
                            stringBuilder.AppendLine();
                            foreach (ValidationErrorInfo validationErrorInfo in operationResult.ValidationErrors)
                            {
                                stringBuilder.AppendLine(validationErrorInfo.Message);
                            }
                        }
                        base.View.ShowMessage(stringBuilder.ToString(), MessageBoxType.Notice);
                    }
                }
            }
            base.ButtonClick(e);
        }
    }
}
