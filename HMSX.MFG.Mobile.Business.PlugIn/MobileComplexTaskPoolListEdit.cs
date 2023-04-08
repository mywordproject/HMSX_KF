using System;
using System.Collections.Generic;
using System.Linq;
using Kingdee.K3.Core.MFG.SFC;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.Complex;
using Kingdee.BOS.Orm.DataEntity;

using Kingdee.K3.MFG.ServiceHelper.SFS;
using System.Reflection;
using Kingdee.BOS.Resource;
using Kingdee.K3.MFG.ServiceHelper.SFC;
using Kingdee.BOS.Core;
using Kingdee.K3.MFG.SFC.Common.Core.EnumConst.Mobile;
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.DataModel;
using Kingdee.BOS;
using System.Data;
using Kingdee.BOS.ServiceHelper;
using Kingdee.K3.MFG.Common.BusinessEntity.SFC.SFCUtils;
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.Utils;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Mobile.PlugIn.ControlModel;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.K3.Core.SCM.STK;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Orm.Metadata.DataEntity;
using Kingdee.BOS.Mobile;
using Kingdee.BOS.Mobile.PlugIn;
using System.ComponentModel;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.K3.Core.BD;
using Kingdee.K3.MFG.Mobile.ServiceHelper;
using System.Text;
using Kingdee.BOS.Core.Validation;

namespace HMSX.MFG.Mobile.Business.PlugIn
{
    [Description("工序任务超市-表单插件")]
    [Kingdee.BOS.Util.HotUpdate]
    public class MobileComplexTaskPoolListEdit : ComplexTaskPoolList
    {
        protected long optPlanOptId;
        protected long materialId;
        protected long userOrgId;
        protected long masterId;
        string materIalId = "0";
        private MobileEnums.DataFilterType _currDataFilterType = MobileEnums.DataFilterType.None;
        private Kingdee.BOS.Core.ExtendedDataEntity[] _extDatas;
        private UnitConvert curUnitConvert;
        private bool isNeedStartNetWorkCtrl = false;
        protected List<long> listId = new List<long>();
        public override void ButtonClick(ButtonClickEventArgs e)
        {
            string a;
            string b = "";
            if ((a = e.Key.ToUpper()) != null)
            {

                if (a == "FBUTTON_PREVIOUS")
                {
                    this.Model.DataObject["FLot_Text"] = null;
                    this.View.Model.SetValue("FLot", "");

                    this.Model.DataObject["FMouldId_Id"] = null;
                    this.View.Model.SetValue("FMouldId", 0);
                }
                if (a == "FBUTTON_NEXT")
                {
                    this.Model.DataObject["FLot_Text"] = null;
                    this.View.Model.SetValue("FLot", "");

                    this.Model.DataObject["FMouldId_Id"] = null;
                    this.View.Model.SetValue("FMouldId", 0);
                }
                if (a == "FBUTTON_CONFIRM")//认领
                {
                    if (this.Model.GetValue("FLot").ToString() == "" || this.Model.GetValue("FLot").ToString() == null)
                    {
                        base.View.ShowMessage(ResManager.LoadKDString("批号不允许为空！", "015747000026506", SubSystemType.MFG, new object[0]), MessageBoxType.Notice);
                        e.Cancel = true;
                        return;
                    }
                    else
                    {
                        string relt = "";
                        int rowIndex = this.Model.GetEntryCurrentRowIndex("FMobileListViewEntity");
                        Dictionary<string, object> currentRowData = this.GetCurrentRowData(rowIndex);
                        string malnumber = currentRowData["FProductId"].ToString().Substring(0, currentRowData["FProductId"].ToString().IndexOf("/"));
                        string cxsql = $@" 
                        select * from T_SFC_DISPATCHDETAIL a
                        inner join T_SFC_DISPATCHDETAILENTRY b on a.FID=b.FID
                        inner join T_BD_Material c on a.fmaterialid=c.fmaterialid
                        where c.fnumber= '{malnumber}' and F_RUJP_Lot='{this.Model.GetValue("FLot").ToString()}'";
                        var cx = DBUtils.ExecuteDynamicObject(Context, cxsql);
                        if (cx.Count > 0)
                        {
                            base.View.ShowMessage(ResManager.LoadKDString("该批号在派工明细已存在！", "015747000026506", SubSystemType.MFG, new object[0]), MessageBoxType.Notice);
                            e.Cancel = true;
                            return;
                        }
                        string wpgsql = $@" 
                        select * from T_SFC_DISPATCHDETAIL a
                        inner join T_SFC_DISPATCHDETAILENTRY b on a.FID=b.FID
                        inner join T_BD_Material c on a.fmaterialid=c.fmaterialid
                        where c.fnumber= '{malnumber}' and  FOPTPLANNO='{currentRowData["FBillNo"]}' and FDISPATCHEDQTY !=0";
                        var wpg = DBUtils.ExecuteDynamicObject(Context, wpgsql);
                        if (wpg.Count == 0)
                        {
                            string ypgsql = $@" 
                            select distinct FOPTPLANNO from T_SFC_DISPATCHDETAIL a
                            inner join T_SFC_DISPATCHDETAILENTRY b on a.FID = b.FID
                            inner join T_BD_Material c on a.fmaterialid = c.fmaterialid
                            inner join T_PRD_MOENTRY_A d on d.FENTRYID=a.FMOENTRYID
                            where c.fnumber = '{malnumber}' and 
							FDISPATCHEDQTY != 0 and
                            FPLANNINGQTY>=FDISPATCHEDQTY and d.FSTATUS not in (5,6,7)";
                            var ypgs = DBUtils.ExecuteDynamicObject(Context, ypgsql);
                            if (ypgs.Count > 0)
                            {
                                string op = "";
                                foreach (var ypg in ypgs)
                                {
                                    op += ypg["FOPTPLANNO"].ToString() + ",";
                                }                              
                                this.View.ShowMessage("该产品有订单已派工" + op.Trim(',') + ",是否继续认领？",
                                 MessageBoxOptions.YesNo,
                                 new Action<MessageBoxResult>((result) =>
                                 {
                                     if (result == MessageBoxResult.Yes)
                                     {
                                         base.ButtonClick(e);
                                     }
                                     else if (result == MessageBoxResult.No)
                                     {
 
                                     }
                                 }));
                                b = "A";
                            }
                        }
                    }
                }
            }
            if (b == "")
            {
                base.ButtonClick(e);
            }          
        }
        public override void BeforeUpdateValue(BeforeUpdateValueEventArgs e)
        {
            string text = e.Key.ToUpper();
            if (text != null)
            {
                if (text == "FTEXT_SCANCODE")
                {
                    string[] strs = e.Value.ToString().Split('-');
                    if (strs.Length>0)
                    {
                        e.Value = strs[0];
                    }
                }
                else if (text == "FTEXT_SCANCODE1")
                {
                    if (e.Value.ToString().StartsWith("PGMX")==false)
                    {
                        string[] strs = e.Value.ToString().Split('-');
                        if (strs.Length > 0)
                        {
                            e.Value = strs[0];
                        }
                    }                    
                }
            }
            base.BeforeUpdateValue(e);
        }

        public override void EntityRowDoubleClick(EntityRowClickEventArgs e)
        {
            base.EntityRowDoubleClick(e);
            if (!this.IsClaim)
            {
                this.SetLotinfo();
                this.SetMouldIdInfo();
                this.setMinPackCount();
                this.SetMaterialProperty();
                this.SetKHBQ();
                return;
            }

        }
        private void SetKHBQ()
        {
            Dictionary<string, object> currentRowData = this.GetCurrentRowData();
            optPlanOptId = Convert.ToInt64(currentRowData["OperId"]);
            DynamicObject obj = GetOptplan(optPlanOptId);
            materialId = Convert.ToInt64(obj["ProductId_Id"]);
            string khbqsql = $@"select FID from HMD_t_Cust100150 where F_HMD_BASEWLBM={materialId}";
            var bhbqs = DBUtils.ExecuteDynamicObject(Context, khbqsql);
            if (bhbqs.Count == 1)
            {
                this.View.Model.SetValue("F_260_KHBQ", Convert.ToInt64(bhbqs[0]["FID"]));
                this.View.UpdateView("F_260_KHBQ");
            }
            else
            {
                this.View.Model.SetValue("F_260_KHBQ", 0);
                this.View.UpdateView("F_260_KHBQ");
            }
        }
        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            if (!this.IsClaim)
            {
                if (this.DicTableData.Keys.Count<int>() == 1)
                {
                    this.SetLotinfo();
                    this.SetMouldIdInfo();
                    this.setMinPackCount();
                    return;
                }
            }
        }
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (e.Field.Key == "F_260_FXCP")
            {

                string strSql = string.Format(@"select F_260_Textbbh,F_260_KHWLBB from T_BD_MATERIAL where FMATERIALID={0}", Convert.ToInt64(e.NewValue));
                DynamicObjectCollection rs = DBServiceHelper.ExecuteDynamicObject(this.Context, strSql);
                if (rs.Count > 0)
                {
                    this.View.Model.SetValue("F_260_Textbbh", rs[0]["F_260_Textbbh"].ToString());
                    this.View.Model.SetValue("F_260_KHWLBB", rs[0]["F_260_KHWLBB"].ToString());
                }
                else
                {
                    this.View.Model.SetValue("F_260_Textbbh", "");
                    this.View.Model.SetValue("F_260_KHWLBB", "");
                }
            }
        }
        /// <summary>
        /// 设置物料属性信息
        /// </summary>
        private void SetMaterialProperty()
        {
            Dictionary<string, object> currentRowData = this.GetCurrentRowData();
            optPlanOptId = Convert.ToInt64(currentRowData["OperId"]);
            DynamicObject obj = GetOptplan(optPlanOptId);
            materialId = Convert.ToInt64(obj["ProductId_Id"]);//物料Id
            DynamicObject material = obj["ProductId"] as DynamicObject;
            string materialNumber = material["Number"].ToString();
            if (materialNumber.Substring(0, 6) == "260.03")
            {
                //PAEZ_t_Cust_Entry100297
                string strSql = string.Format(@"select t.FMATERIALID,F_260_Textbbh,F_260_KHWLBB,t1.F_260_FXCP from T_BD_MATERIAL t inner join PAEZ_t_Cust_Entry100301 t1 on t.FMATERIALID=t1.FMATERIALID
                                                              where t.FMATERIALID={0}", materialId);
                DynamicObjectCollection rs = DBServiceHelper.ExecuteDynamicObject(this.Context, strSql);
                if (rs.Count > 1)
                {
                    foreach (DynamicObject row in rs)
                    {
                        long id = Convert.ToInt64(row["F_260_FXCP"]);
                        materIalId = materIalId + ',' + id.ToString();
                        this.View.Model.SetValue("F_260_FXCP", 0);
                        this.View.Model.SetValue("F_260_Textbbh", "");
                        this.View.Model.SetValue("F_260_KHWLBB", "");
                    }

                }
                else if (rs.Count == 1)
                {
                    string strSql1 = string.Format(@"select F_260_Textbbh,F_260_KHWLBB from T_BD_MATERIAL where FMATERIALID={0}", Convert.ToInt64(rs[0]["F_260_FXCP"]));
                    DynamicObjectCollection rs1 = DBServiceHelper.ExecuteDynamicObject(this.Context, strSql1);
                    if (rs1.Count > 0)
                    {
                        this.View.Model.SetValue("F_260_FXCP", Convert.ToInt64(rs[0]["F_260_FXCP"]));
                        this.View.Model.SetValue("F_260_Textbbh", rs1[0]["F_260_Textbbh"].ToString());
                        this.View.Model.SetValue("F_260_KHWLBB", rs1[0]["F_260_KHWLBB"].ToString());
                    }
                }
                else
                {
                    this.View.Model.SetValue("F_260_FXCP", 0);
                    this.View.Model.SetValue("F_260_Textbbh", "");
                    this.View.Model.SetValue("F_260_KHWLBB", "");
                }

            }
            else
            {
                string strSql = string.Format(@"select F_260_Textbbh,F_260_KHWLBB from T_BD_MATERIAL where FMATERIALID={0}", materialId);
                DynamicObjectCollection rs = DBServiceHelper.ExecuteDynamicObject(this.Context, strSql);
                if (rs.Count > 0)
                {
                    this.View.Model.SetValue("F_260_Textbbh", rs[0]["F_260_Textbbh"].ToString());
                    this.View.Model.SetValue("F_260_KHWLBB", rs[0]["F_260_KHWLBB"].ToString());
                }
                else
                {
                    this.View.Model.SetValue("F_260_FXCP", 0);
                    this.View.Model.SetValue("F_260_Textbbh", "");
                    this.View.Model.SetValue("F_260_KHWLBB", "");
                }

            }
        }
        private void SetLotinfo()
        {
            // int rowIndex = this.Model.GetEntryCurrentRowIndex("FMobileListViewEntity");
            Dictionary<string, object> currentRowData = this.GetCurrentRowData();
            string today = DateTime.Today.ToString("yyyy-MM-dd");
            //工序ID
            optPlanOptId = Convert.ToInt64(currentRowData["OperId"]);
            userOrgId = GetOrgId(optPlanOptId);
            DynamicObject obj = GetOptplan(optPlanOptId);
            materialId = Convert.ToInt64(obj["ProductId_Id"]);
            string strSql = string.Format(@"/*dialect*/select top 1  t1.FentryId,t1.F_LOT_Text from T_SFC_DISPATCHDETAIL t inner join T_SFC_DISPATCHDETAILENTRY t1 on t.FID=t1.FID and  DATEDIFF ( YEAR , cast('{0}' as datetime) , t1.FDISPATCHTIME )=0  and FMATERIALID={1} order by t1.FentryId desc", today, materialId);
            DynamicObjectCollection rs = DBServiceHelper.ExecuteDynamicObject(this.Context, strSql);
            if (rs.Count > 0)
            {
                // lot :作业号+物料编码+当前日期+3位流水号
                //20211102001
                string strLot;
                if (rs[0]["F_LOT_Text"] != null)
                {
                    strLot = rs[0]["F_LOT_Text"].ToString();
                }
                else { strLot = "0"; }
                this.View.Model.SetValue("FLot", this.CreateLot(strLot));
                this.View.UpdateView("FLot");
            }
            else
            {
                this.View.Model.SetValue("FLot", this.CreateLot("0"));
                this.View.UpdateView("FLot");

            }
        }
        /// <summary>
        /// 设置模具字段
        /// </summary>
        private void SetMouldIdInfo()
        {
            //  int rowIndex = this.Model.GetEntryCurrentRowIndex("FMobileListViewEntity");
            Dictionary<string, object> currentRowData = this.GetCurrentRowData();
            optPlanOptId = Convert.ToInt64(currentRowData["OperId"]);
            userOrgId = GetOrgId(optPlanOptId);
            DynamicObject obj = GetOptplan(optPlanOptId);
            materialId = Convert.ToInt64(obj["ProductId_Id"]);
            string strSql = string.Format("select FID from T_ENG_MOULD where FUSEORGID={0} and FMouldModelId={1} AND FDOCUMENTSTATUS='C' AND FFORBIDSTATUS='A'", userOrgId, materialId);
            DynamicObjectCollection rs = DBServiceHelper.ExecuteDynamicObject(this.Context, strSql);
            if (rs.Count == 1)
            {
                this.View.Model.SetValue("FMouldId", Convert.ToInt64(rs[0]["FID"]));
                this.View.UpdateView("FMouldId");
            }
            else
            {
                this.View.Model.SetValue("FMouldId", 0);
                this.View.UpdateView("FMouldId");
            }
        }
        private void setMinPackCount()
        {
            Dictionary<string, object> currentRowData = this.GetCurrentRowData();
            optPlanOptId = Convert.ToInt64(currentRowData["OperId"]);
            DynamicObject obj = GetOptplan(optPlanOptId);
            materialId = Convert.ToInt64(obj["ProductId_Id"]);
            string strSql = string.Format(@"select FMinPackCount from T_BD_MATERIALPURCHASE where FMATERIALID={0}", materialId);
            DynamicObjectCollection rs = DBServiceHelper.ExecuteDynamicObject(this.Context, strSql);
            decimal minPackcount = Convert.ToDecimal(rs[0]["FMinPackCount"]);
            if (minPackcount > 0)
            {
                this.View.Model.SetValue("F_RUJP_Qty", minPackcount);
            }
            else
            {
                this.View.Model.SetValue("F_RUJP_Qty", 0);
            }
            this.View.UpdateView("F_RUJP_Qty");
        }
        public string CreateLot(string lot)
        {
            var today = DateTime.Today.ToString("yyyyMMdd");
            if (lot.Length > 1)
            {
                var lotdate = lot.Substring(0, 8);
                if (today == lotdate)
                {
                    var no = Convert.ToInt32(lot.Substring(8));
                    return $"{today}{++no:000}";
                }
            }
            return $"{today}001";
        }
        /// <summary>
        /// 获取组织Id
        /// </summary>
        /// <param name="operId"></param>
        /// <returns></returns>
        private long GetOrgId(long operId)
        {
            long userOrgId = 0;
            string strSQL = "select FProcessOrgId,FWorkCenterId,FDepartmentId,fdetailId from T_SFC_OPERPLANNINGDETAIL where fdetailId=@operId ";
            List<SqlParam> paramList = new List<SqlParam>
                 {
                      new SqlParam("@operId", KDDbType.Int64, operId)
                 };
            using (IDataReader dataReader = DBServiceHelper.ExecuteReader(base.Context, strSQL, paramList))
            {
                while (dataReader.Read())
                {
                    userOrgId = Convert.ToInt64(dataReader["FProcessOrgId"]);
                }
            }
            return userOrgId;
        }
        protected override DynamicObject CreateDispatchDetailData()
        {
            DynamicObject obj = base.CreateDispatchDetailData();
            int rowIndex = this.Model.GetEntryCurrentRowIndex("FMobileListViewEntity");
            Dictionary<string, object> currentRowData = this.GetCurrentRowData(rowIndex);
            optPlanOptId = Convert.ToInt64(currentRowData["OperId"]);
            obj["F_SBID_OrgId_Id"] = GetOrgId(optPlanOptId);
            obj["PlanningQty"] = Convert.ToDecimal(this.optPlanOper["OperQty"]) - Convert.ToDecimal(this.optPlanOper["ReportQty"]);
            return obj;
        }
        /// <summary>
        ///  
        /// </summary>
        /// <param name="dispatchObj"></param>
        /// <param name="entryEntity"></param>
        /// <returns></returns>
        protected override DynamicObject CreateDispatchEntryData(DynamicObject dispatchObj, EntryEntity entryEntity)
        {
            int _seq;
            if (entryEntity == null)
            {
                FormMetadata formMetadata = MetaDataServiceHelper.Load(base.Context, "SFC_DispatchDetail", true) as FormMetadata;
                entryEntity = (formMetadata.BusinessInfo.GetEntity("FEntity") as EntryEntity);
            }
            DynamicObjectCollection dynamicObjectCollection = dispatchObj["DispatchDetailEntry"] as DynamicObjectCollection;
            _seq = dynamicObjectCollection.Count;
            decimal qty = Convert.ToDecimal(this.Model.DataObject["ClaimQty"]);
            decimal minqty = Convert.ToDecimal(this.Model.DataObject["F_RUJP_Qty"]);
            int _qty;
            if (minqty == 0)
            {
                _qty = 1;
            }
            else
            {
                _qty = Convert.ToInt32(Math.Ceiling(qty / minqty));
            }
            decimal tmp = 0;
            listId = new List<long>();
            for (int i = 0; i < _qty; i++)
            {
                DynamicObject dynamicObject = entryEntity.DynamicObjectType.CreateInstance() as DynamicObject;
                if (dynamicObjectCollection.Count > 0)
                {
                    DynamicObject dynamicObject2 = dynamicObjectCollection.Last<DynamicObject>();
                    dynamicObject["Seq"] = _seq + 1 + i;
                    dynamicObject["ResourceId_Id"] = dynamicObject2["ResourceId_Id"];
                    dynamicObject["ShiftSliceId_Id"] = dynamicObject2["ShiftSliceId_Id"];
                    dynamicObject["ShiftGroupId_Id"] = dynamicObject2["ShiftGroupId_Id"];
                    dynamicObject["EquipmentId_Id"] = dynamicObject2["EquipmentId_Id"];
                    dynamicObject["PlanBeginTime"] = dynamicObject2["PlanBeginTime"];
                    dynamicObject["PlanEndTime"] = dynamicObject2["PlanEndTime"];
                    dynamicObject["Status"] = "A";
                    if (qty - tmp > minqty)
                    {
                        dynamicObject["WorkQty"] = minqty;
                    }
                    else
                    {
                        dynamicObject["WorkQty"] = qty - tmp;
                    }
                    //dynamicObject["WorkQty"] =this.Model.DataObject["ClaimQty"];
                    if (this.curUnitConvert == null)
                    {
                        this.curUnitConvert = this.GetUnitConvert(Convert.ToInt64(dispatchObj["MaterialId_Id"]), Convert.ToInt64(dispatchObj["FUnitID_Id"]), Convert.ToInt64(dispatchObj["BaseUnitID_Id"]));
                    }
                    decimal num = 0;
                    if (qty - tmp > minqty)
                    {
                        num = Convert.ToDecimal(minqty) * Convert.ToDecimal(dispatchObj["UnitTransHeadQty"]) / Convert.ToDecimal(dispatchObj["UnitTransOperQty"]);
                    }
                    else
                    {
                        num = Convert.ToDecimal(qty - tmp) * Convert.ToDecimal(dispatchObj["UnitTransHeadQty"]) / Convert.ToDecimal(dispatchObj["UnitTransOperQty"]);
                    }

                    dynamicObject["WorkHeadQty"] = num;
                    dynamicObject["BaseWorkQty"] = this.curUnitConvert.ConvertQty(num, "");
                    dynamicObject["DispatchTime"] = CommonServiceHelper.GetCurrentTime(base.Context);
                    string lot_txt = this.Model.DataObject["FLot_Text"].ToString();
                    dynamicObject["F_Lot_Text"] = (long.Parse(lot_txt) + i).ToString("00000000000");
                    dynamicObject["F_RUJP_Lot"] = (long.Parse(lot_txt) + i).ToString("00000000000");
                    dynamicObject["MouldId_Id"] = this.Model.DataObject["FMouldId_Id"];
                    dynamicObject["F_260_NBBBH"] = this.Model.DataObject["F_260_Textbbh"];
                    dynamicObject["F_260_WBBBH"] = this.Model.DataObject["F_260_KHWLBB"];
                    dynamicObject["F_260_KHBQ_Id"] = this.Model.DataObject["F_260_KHBQ_Id"];
                    dynamicObject["F_260_DDLX"] = this.optPlan["F_260_DDLX"];
                    dynamicObject["F_260_JHGZH"] = this.optPlan["MTONo"];
                }
                else
                {
                    dynamicObject["Seq"] = _seq + 1 + i;
                    dynamicObject["ResourceId_Id"] = this.optPlanOper["ResourceId_Id"];
                    dynamicObject["ShiftSliceId_Id"] = this.optPlanOper["ShiftSliceId_Id"];
                    dynamicObject["ShiftGroupId_Id"] = this.optPlanOper["ShiftGroupId_Id"];
                    dynamicObject["EquipmentId_Id"] = this.optPlanOper["EquipmentId_Id"];
                    dynamicObject["PlanBeginTime"] = this.optPlanOper["OperPlanStartTime"];
                    dynamicObject["PlanEndTime"] = this.optPlanOper["OperPlanFinishTime"];
                    dynamicObject["Status"] = "A";                
                    dynamicObject["DispatchTime"] = CommonServiceHelper.GetCurrentTime(base.Context);
                    DynamicObjectCollection dynamicObjectCollection2 = this.optPlanOper["EmpId"] as DynamicObjectCollection;
                    DynamicObjectCollection dynamicObjectCollection3 = dynamicObject["EmpId"] as DynamicObjectCollection;
                    foreach (DynamicObject dynamicObject3 in dynamicObjectCollection2)
                    {
                        DynamicObject dynamicObject4 = new DynamicObject(dynamicObjectCollection2.DynamicCollectionItemPropertyType);
                        dynamicObject4["PkId"] = 0;
                        dynamicObject4["EmpId_Id"] = dynamicObject3["EmpId_Id"];
                        dynamicObject4["EmpId"] = dynamicObject3["EmpId"];
                        dynamicObjectCollection3.Add(dynamicObject4);
                    }
                    if (Convert.ToInt64(dynamicObject["ResourceId_Id"]) == 0L)
                    {
                        DynamicObject dynamicObject5 = this.optPlanOper["WorkCenterId"] as DynamicObject;
                        DynamicObjectCollection source = dynamicObject5["WorkCenterCapacity"] as DynamicObjectCollection;
                        DynamicObject dynamicObject6 = source.FirstOrDefault((DynamicObject f) => Convert.ToBoolean(f["JoinScheduling"]));
                        if (dynamicObject6 != null)
                        {
                            dynamicObject["ResourceId_Id"] = dynamicObject6["RESOURCEID_Id"];
                        }
                    }
                    decimal num2 = 0;
                    if (qty - tmp > minqty)
                    {
                        num2 = minqty;
                    }
                    else
                    {
                        num2 = qty - tmp;
                    }
                    // num2 = Convert.ToDecimal(this.Model.DataObject["ClaimQty"]);
                    dynamicObject["WorkQty"] = ((num2 < 0m) ? 0m : num2);
                    if (this.curUnitConvert == null)
                    {
                        this.curUnitConvert = this.GetUnitConvert(Convert.ToInt64(dispatchObj["MaterialId_Id"]), Convert.ToInt64(dispatchObj["FUnitID_Id"]), Convert.ToInt64(dispatchObj["BaseUnitID_Id"]));
                    }
                    //decimal num = Convert.ToDecimal(dynamicObject["WorkQty"]) * Convert.ToDecimal(dispatchObj["UnitTransHeadQty"]) / Convert.ToDecimal(dispatchObj["UnitTransOperQty"]);
                    decimal num = 0;
                    if (qty - tmp > minqty)
                    {
                        num = Convert.ToDecimal(minqty) * Convert.ToDecimal(dispatchObj["UnitTransHeadQty"]) / Convert.ToDecimal(dispatchObj["UnitTransOperQty"]);
                    }
                    else
                    {
                        num = Convert.ToDecimal(qty - tmp) * Convert.ToDecimal(dispatchObj["UnitTransHeadQty"]) / Convert.ToDecimal(dispatchObj["UnitTransOperQty"]);
                    }
                    dynamicObject["WorkHeadQty"] = num;
                    dynamicObject["BaseWorkQty"] = this.curUnitConvert.ConvertQty(num, "");
                }
                DynamicObjectCollection dynamicObjectCollection4 = dynamicObject["EmpId"] as DynamicObjectCollection;
                DynamicObject dynamicObject7 = new DynamicObject(dynamicObjectCollection4.DynamicCollectionItemPropertyType);
                dynamicObject7["EmpId_Id"] = this.empId;
                dynamicObjectCollection4.Add(dynamicObject7);
                dynamicObject["EmpText"] = this.empName;
                dynamicObject["CreateMode"] = 'A';
                long wcId = Convert.ToInt64(dispatchObj["WorkCenterId_Id"]);
                List<long> mould = DataUtils.GetMould(base.Context, wcId);
                if (mould != null && mould.Count == 1)
                {
                    dynamicObject["MouldId_Id"] = mould.ElementAt(0);
                }
                dynamicObject["PlanBeginTime"] = this.Model.DataObject["PlanBeginTime"];
                dynamicObject["PlanEndTime"] = this.Model.DataObject["PlanEndTime"];
                string lot = this.Model.DataObject["FLot_Text"].ToString();
                dynamicObject["F_Lot_Text"] = (long.Parse(lot) + i).ToString("00000000000");
                dynamicObject["F_RUJP_Lot"] = (long.Parse(lot) + i).ToString("00000000000");
                dynamicObject["MouldId_Id"] = this.Model.DataObject["FMouldId_Id"];
                dynamicObject["F_260_NBBBH"] = this.Model.DataObject["F_260_Textbbh"];
                dynamicObject["F_260_WBBBH"] = this.Model.DataObject["F_260_KHWLBB"];
                dynamicObject["F_260_KHBQ_Id"] = this.Model.DataObject["F_260_KHBQ_Id"];
                dynamicObject["F_260_DDLX"] = this.optPlan["F_260_DDLX"];
                dynamicObject["F_260_JHGZH"] = this.optPlan["MTONo"];
                listId.Add(Convert.ToInt32(dynamicObject["Seq"]));
                dynamicObjectCollection.Add(dynamicObject);
                tmp = tmp + minqty;
            }
            return dispatchObj;
        }
        protected override void BuildDispatchDetailData(long optPlanOptId, bool isCreateNewEntry = true)
        {
            FormMetadata formMetadata = MetaDataServiceHelper.Load(base.Context, "SFC_DispatchDetail", true) as FormMetadata;
            this.GetOptPlanOper(optPlanOptId);
            OQLFilter ofilter = OQLFilter.CreateHeadEntityFilter(string.Format("FOperId={0}", Convert.ToInt64(this.optPlanOper["Id"])));
            DynamicObject dynamicObject = BusinessDataServiceHelper.Load(base.Context, "SFC_DispatchDetail", null, ofilter).FirstOrDefault<DynamicObject>();
            if (dynamicObject == null)
            {
                dynamicObject = this.CreateDispatchDetailData();
                this.isNeedStartNetWorkCtrl = true;
            }
            else if (isCreateNewEntry)
            {
                dynamicObject = this.CreateDispatchEntryData(dynamicObject, null);
                DBServiceHelper.LoadReferenceObject(base.Context, new DynamicObject[]
                {
            dynamicObject
                }, formMetadata.BusinessInfo.GetDynamicObjectType(), false);
            }
            IOperationResult operationResult = BusinessDataServiceHelper.Save(base.Context, formMetadata.BusinessInfo, dynamicObject, null, "");
            if (operationResult.IsSuccess)
            {
                this.Model.DataObject["ClaimQty"] = 0;
                base.View.SetControlProperty("FClaimQty", 0);
                base.View.UpdateView("FClaimQty");
                this.Model.DataObject["PlanBeginTime"] = null;
                this.Model.DataObject["PlanEndTime"] = null;
                base.View.UpdateView("FPlanBeginTime");
                base.View.UpdateView("FPlanEndTime");
                base.View.GetControl("FButton_Confirm").Enabled = false;
                base.View.UpdateView("FButton_Confirm");
                DynamicObject dynamicObject2 = operationResult.SuccessDataEnity.FirstOrDefault<DynamicObject>();
                DynamicObjectCollection source = dynamicObject2["DispatchDetailEntry"] as DynamicObjectCollection;
                var result = from p in source where listId.Contains(Convert.ToInt32(p["Seq"])) orderby p["Seq"], p["F_RUJP_Lot"] select p;
                //DynamicObject dynamicObject3 = source.LastOrDefault<DynamicObject>();
                List<KeyValuePair<object, object>> list = new List<KeyValuePair<object, object>>();
                string hmiautoStartOper = this.GetHMIAutoStartOper(Convert.ToInt64(dynamicObject2["WorkCenterId_Id"]));
                if (hmiautoStartOper == "1")
                {
                    foreach (DynamicObject obj in result)
                    {
                        if (obj != null)
                        {
                            list.Add(new KeyValuePair<object, object>(dynamicObject2["Id"], obj["Id"]));
                            IOperationResult operationResult2 = DispatchDetailServiceHelper.SetStatus(base.Context, list, "ToStart", null);
                            if (operationResult2.IsSuccess)
                            {
                                obj["Status"] = "B";
                            }
                        }
                    }
                }
                foreach (DynamicObject obj in result)
                {
                    String upsql = $@"update T_SFC_DISPATCHDETAILENTRY set 
                        F_260_CSTM='{dynamicObject2["OptPlanNo"].ToString().Replace(" ", "") + '-' + dynamicObject2["SeqNumber"].ToString().Replace(" ", "") + '-' + dynamicObject2["OperNumber"].ToString().Replace(" ", "") + '-' + obj["BarCode"].ToString().Replace(" ", "")}'                    
                        where FENTRYID='{obj["Id"]}'";
                    DBUtils.Execute(Context, upsql);
                    string billBarCode = obj["BarCode"].ToString();
                    this.Print(billBarCode, true);
                }
                this.ReloadListData(null, false);
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

        private string GetHMIAutoStartOper(long workCenterId)
        {
            string strSql = "select FHMIDISPAUTOSTART from  T_ENG_WORKCENTER where fid=@workCenterId";
            List<SqlParam> list = new List<SqlParam>();
            SqlParam item = new SqlParam("@workCenterId", KDDbType.Int64, workCenterId);
            list.Add(item);
            return DBServiceHelper.ExecuteScalar<string>(base.Context, strSql, string.Empty, list.ToArray());
        }

        public override void BeforeF7Select(BeforeF7SelectEventArgs e)
        {
            base.BeforeF7Select(e);
            string a;
            if ((a = e.FieldKey.ToUpper()) != null)
            {
                if (a == "FLOT")
                {
                    int rowIndex = this.Model.GetEntryCurrentRowIndex("FMobileListViewEntity");
                    Dictionary<string, object> currentRowData = this.GetCurrentRowData(rowIndex);
                    optPlanOptId = Convert.ToInt64(currentRowData["OperId"]);
                    DynamicObject obj = GetOptplan(optPlanOptId);
                    materialId = Convert.ToInt64(obj["ProductId_Id"]);
                    masterId = GetMasterId(materialId);
                    userOrgId = GetOrgId(optPlanOptId);
                    this.SetLotFilter(e);
                    return;
                }
                if (a == "FMOULDID")
                {
                    int rowIndex = this.Model.GetEntryCurrentRowIndex("FMobileListViewEntity");
                    Dictionary<string, object> currentRowData = this.GetCurrentRowData(rowIndex);
                    optPlanOptId = Convert.ToInt64(currentRowData["OperId"]);
                    DynamicObject obj = GetOptplan(optPlanOptId);
                    materialId = Convert.ToInt64(obj["ProductId_Id"]);
                    masterId = GetMasterId(materialId);
                    userOrgId = GetOrgId(optPlanOptId);
                    this.SetMouldFilter(e);
                    return;
                }
                if (a == "F_260_KHBQ")
                {
                    int rowIndex = this.Model.GetEntryCurrentRowIndex("FMobileListViewEntity");
                    Dictionary<string, object> currentRowData = this.GetCurrentRowData(rowIndex);
                    optPlanOptId = Convert.ToInt64(currentRowData["OperId"]);
                    DynamicObject obj = GetOptplan(optPlanOptId);
                    materialId = Convert.ToInt64(obj["ProductId_Id"]);
                    this.SetKHBQFilter(e);
                    return;
                }
                if (a == "F_260_FXCP")
                {
                    this.SetMaterialFilter(e);
                    return;
                }
                ListShowParameter listShowParameter = e.DynamicFormShowParameter as ListShowParameter;
                listShowParameter.UseOrgId = this.userOrgId;
                listShowParameter.ListFilterParameter.Filter = e.ListFilterParameter.Filter;
            }
        }
        private void SetKHBQFilter(BeforeF7SelectEventArgs e)
        {
            string text = string.Format(" F_HMD_Basewlbm ={0}", this.materialId);
            e.ListFilterParameter.Filter = text;
        }
        private void SetMaterialFilter(BeforeF7SelectEventArgs e)
        {
            if (materIalId != null)
            {
                string text = string.Format(@"FMATERIALID in ({0})", materIalId);
                e.ListFilterParameter.Filter = text;
            }
        }
        private void SetLotFilter(BeforeF7SelectEventArgs e)
        {
            string text = string.Format(" FMATERIALID ={0} AND FUseOrgId = {1} AND FLotStatus = '1' AND FBizType = '2' AND FCancelStatus = 'A' ", this.masterId, this.userOrgId);
            e.ListFilterParameter.Filter = text;
        }
        private void SetMouldFilter(BeforeF7SelectEventArgs e)
        {
            string text = string.Format(" FMouldModelId ={0} AND FUseOrgId = {1}  ", this.masterId, this.userOrgId);
            e.ListFilterParameter.Filter = text;
        }

        /// <summary>
        /// 获取工序计划物料信息
        /// </summary>
        /// <param name="operId"></param>
        /// <returns></returns>
        private DynamicObject GetOptplan(long operId)
        {
            SqlParam sqlParam = new SqlParam("@OperId", KDDbType.Int64, optPlanOptId);
            long num = DBServiceHelper.ExecuteScalar<long>(base.Context, "SELECT S.FID FROM T_SFC_OPERPLANNINGDETAIL d INNER JOIN T_SFC_OPERPLANNINGSEQ s ON s.FENTRYID=d.FENTRYID WHERE d.FDETAILID=@OperId", 0L, new SqlParam[]
            {
              sqlParam
            });
            DynamicObjectType dynamicObjectType = ((FormMetadata)MetaDataServiceHelper.Load(base.Context, "SFC_OperationPlanning", true)).BusinessInfo.GetDynamicObjectType();
            DynamicObject optPlan = BusinessDataServiceHelper.LoadSingle(base.Context, num, dynamicObjectType, null);
            return optPlan;
        }

        /// <summary>
        /// 根据物料ID获取MasterId
        /// </summary>
        /// <param name="materialId"></param>
        /// <returns></returns>
        private long GetMasterId(long materialId)
        {
            SqlParam sqlParam = new SqlParam("@materialId", KDDbType.Int64, materialId);
            long masterId = DBServiceHelper.ExecuteScalar<long>(base.Context, "select FMASTERID from T_BD_MATERIAL where FMATERIALID=@materialId", 0L, new SqlParam[]
            {
              sqlParam
            });
            return masterId;
        }

        protected override void PrepareDispDetailBindFields(Dictionary<string, string> dicFieldLabelKeys)
        {
            base.PrepareDispDetailBindFields(dicFieldLabelKeys);
            DataUtils.AddDicFieldLabel(dicFieldLabelKeys, "F_RUJP_Lot");
        }

        //protected override IEnumerable<DynamicObject> GetListData(List<string> fieldLabelKeys)
        //{               
        //    var result= base.GetListData(fieldLabelKeys);
        //    return result;
        //}     
        protected override System.Collections.Generic.IEnumerable<Kingdee.BOS.Orm.DataEntity.DynamicObject> GetListData(System.Collections.Generic.List<string> fieldLabelKeys)
        {
            IEnumerable<DynamicObject> result = base.GetListData(fieldLabelKeys);
            /**
            ExportLogInfo exportLogInfo = new ExportLogInfo
             {
                 Code = this.runId,
                 StartTime = System.DateTime.Now
             };
             System.Collections.Generic.IEnumerable<Kingdee.BOS.Orm.DataEntity.DynamicObject> result = new System.Collections.Generic.List<Kingdee.BOS.Orm.DataEntity.DynamicObject>();
             MobileEnums.DataFilterType currDataFilterType = this.CurrDataFilterType;
             if (currDataFilterType != MobileEnums.DataFilterType.ScanCode)
             {
                 if (currDataFilterType == MobileEnums.DataFilterType.UnDispatch)
                 {
                     this._extDatas = SFSDiscreteServiceHelper.GetUnDispatchOperPlanningDatas(base.Context, this.dicFieldEntityPaths.Keys.ToList<string>(), this.DispSelKeys, this.userId);
                     if (this._extDatas != null)
                     {
                         result =
                             from s in this._extDatas
                             select s.DataEntity;
                     }
                 }
             }
             else
             {
                 this._extDatas = SFSDiscreteServiceHelper.GetOperPlanningDataByBarCode(base.Context, this.CurrScanCode, true, false, null, this.userId, this.dicFieldEntityPaths.Keys.ToList<string>(), true, this.DispSelKeys, "UnDispatchOperPlanning");
                 if (this._extDatas != null)
                 {
                     result =
                         from s in this._extDatas
                         select s.DataEntity;
                 }
             }
             if (base.IsWriteLog)
             {
                 exportLogInfo.UserId = base.Context.UserId.ToString();
                 exportLogInfo.MethodName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName + '.' + System.Reflection.MethodBase.GetCurrentMethod().Name;
                 exportLogInfo.Detail = Kingdee.BOS.Resource.ResManager.LoadKDString("子类GetListData事件", "015747000021952", Kingdee.BOS.Resource.SubSystemType.MFG, new object[0]);
                 exportLogInfo.EndTime = System.DateTime.Now;
                 ExportLogServiceHelper.WriteLog(base.Context, exportLogInfo);
             }
             **/
            return result;
        }
        private MobileEnums.DataFilterType CurrDataFilterType
        {
            get
            {
                return this._currDataFilterType;
            }
            set
            {
                switch (value)
                {
                    case MobileEnums.DataFilterType.UnDispatch:
                        base.View.GetControl("FButton_Claim").SetCustomPropertyValue(ControlProperty.Forecolor.ToString(), "54,92,143");
                        base.View.GetControl("FButton_Claim").SetCustomPropertyValue(ControlProperty.Backcolor.ToString(), "255,255,255");
                        base.View.GetControl("FButton_Claim").SetCustomPropertyValue(ControlProperty.ImageKey.ToString(), "sfc_selectbox_sel.png");
                        base.View.GetControl("FButton_Claimed").SetCustomPropertyValue(ControlProperty.Forecolor.ToString(), "255,255,255");
                        base.View.GetControl("FButton_Claimed").SetCustomPropertyValue(ControlProperty.Backcolor.ToString(), "54,92,143");
                        base.View.GetControl("FButton_Claimed").SetCustomPropertyValue(ControlProperty.ImageKey.ToString(), "sfc_selectbox_unsel.png");
                        base.View.GetControl("FFlowLayout_Claimed").Visible = false;
                        base.View.GetControl("FFlowLayout_Claim").Visible = true;
                        break;
                    case MobileEnums.DataFilterType.UnFinishedDetail:
                        base.View.GetControl("FButton_Claim").SetCustomPropertyValue(ControlProperty.Forecolor.ToString(), "255,255,255");
                        base.View.GetControl("FButton_Claim").SetCustomPropertyValue(ControlProperty.Backcolor.ToString(), "54,92,143");
                        base.View.GetControl("FButton_Claim").SetCustomPropertyValue(ControlProperty.ImageKey.ToString(), "sfc_selectbox_unsel.png");
                        base.View.GetControl("FButton_Claimed").SetCustomPropertyValue(ControlProperty.Forecolor.ToString(), "54,92,143");
                        base.View.GetControl("FButton_Claimed").SetCustomPropertyValue(ControlProperty.Backcolor.ToString(), "255,255,255");
                        base.View.GetControl("FButton_Claimed").SetCustomPropertyValue(ControlProperty.ImageKey.ToString(), "sfc_selectbox_sel.png");
                        base.View.GetControl("FFlowLayout_Claim").Visible = false;
                        base.View.GetControl("FFlowLayout_Claimed").Visible = true;
                        this.BindDispatchDetailList("");
                        break;
                    default:
                        base.View.GetControl("FButton_Claim").SetCustomPropertyValue(ControlProperty.Forecolor.ToString(), "54,92,143");
                        base.View.GetControl("FButton_Claim").SetCustomPropertyValue(ControlProperty.Backcolor.ToString(), "255,255,255");
                        base.View.GetControl("FButton_Claim").SetCustomPropertyValue(ControlProperty.ImageKey.ToString(), "sfc_selectbox_sel.png");
                        base.View.GetControl("FButton_Claimed").SetCustomPropertyValue(ControlProperty.Forecolor.ToString(), "255,255,255");
                        base.View.GetControl("FButton_Claimed").SetCustomPropertyValue(ControlProperty.Backcolor.ToString(), "54,92,143");
                        base.View.GetControl("FButton_Claimed").SetCustomPropertyValue(ControlProperty.ImageKey.ToString(), "sfc_selectbox_unsel.png");
                        base.View.GetControl("FFlowLayout_Claimed").Visible = false;
                        base.View.GetControl("FFlowLayout_Claim").Visible = true;
                        break;
                }
                base.View.UpdateView("FButton_Claim");
                base.View.UpdateView("FButton_Claimed");
                this._currDataFilterType = value;
                this.ReloadListData(null, false);
            }
        }
    }
}
