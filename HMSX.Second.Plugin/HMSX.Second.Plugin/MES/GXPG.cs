//using Microsoft.Analytics.Interfaces;
//using Microsoft.Analytics.Types.Sql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Kingdee.K3.MFG.Mobile.Business.PlugIn;
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.Complex;
using System.ComponentModel;
using Kingdee.BOS.Resource;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;

using System.Reflection;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm;

using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.NetworkCtrl;
using Kingdee.BOS.Mobile;
using Kingdee.BOS.Mobile.PlugIn.ControlModel;
using Kingdee.BOS.Orm.DataEntity;

using Kingdee.BOS.ServiceHelper;
using Kingdee.K3.Core.MFG.SFC;
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.DataModel;
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.Utils;
using Kingdee.K3.MFG.ServiceHelper.SFC;
using Kingdee.K3.MFG.ServiceHelper.SFS;
using Kingdee.K3.MFG.SFC.Common.Core.EnumConst.Mobile;
using static Kingdee.K3.MFG.SFC.Common.Core.EnumConst.Mobile.MobileEnums;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Mobile.PlugIn;
using Kingdee.BOS.App.Data;

namespace HMSX.Second.Plugin.MES
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("车间管理-工序派工-表单插件")]
    public class GXPG : ComplexDispatchList
    {
        private MobileEnums.DataFilterType _currDataFilterType = MobileEnums.DataFilterType.None;

        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            //this.CurrDataFilterType = 41;
            //base.View.GetControl("FLable_BillTitle").Enabled = false;
            //base.View.GetControl("FLable_BillTitle").Visible = false;
            //base.View.UpdateView("FLable_BillTitle");


            //this.View.ShowMessage(ResManager.LoadKDString("当前未选中行111！", "015747000028226", Kingdee.BOS.Resource.SubSystemType.MFG));

            //this.View.GetControl("FText_ScanCode").SetFocus();
            //this.View.GetControl("F_HMD_Text").SetFocus();


            //this.View.GetControl("FText_ScanCode").SetFocus();



            //this.View.GetControl("F_PAEZ_Remarks ").SetFocus()------ - 设置焦点
            //this.View.GetControl("F_SB_TM").SetFocus(); ----设置焦点

            //this.View.SetEntityFocusRow("FMobileListViewEntity", 0); // ----设置单据体焦点

            //EntryGrid grid = this.View.GetControl<EntryGrid>("FMobileListViewEntity");
            //grid.SetFocusRowIndex(1);


        }

        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            string key;
            if ((key = e.Field.Key) != null)
            {
                if (!(key == "FText_ScanCode"))
                {
                    return;
                }
                else
                {
                    //this.View.ShowMessage(ResManager.LoadKDString("条码字段变化！", "015747000028226", Kingdee.BOS.Resource.SubSystemType.MFG));

                    

                    int FCount = 0;
                    int FCount2 = 0;
                    FCount = this.DicTableData.Count();
                    if (FCount == 0)
                    {
                        return;
                    }

                    string TM= this.CurrScanCode;

                    if (TM == "")
                    {
                        return;
                    }

                    string SQL = "";
                    SQL = "/*dialect*/ select FPROORGID,FPRODEPARTMENTID,T2.FNUMBER from T_SFC_OPERPLANNING T1 LEFT JOIN T_BD_DEPARTMENT T2 ON T1.FPRODEPARTMENTID=T2.FDEPTID where FPROORGID=100026  and t2.FNUMBER='000362' and FBILLNO  like '%" + TM + "%'";
                    DynamicObjectCollection Dyobj = DBUtils.ExecuteDynamicObject(this.Context, SQL);
                    foreach (DynamicObject obj in Dyobj)
                    {
                        FCount2 = FCount2 + 1;
                       
                    }

                    //不是模具生产部
                    if (FCount2 == 0)
                    {
                        return;
                    }

                    //存在记录是 模具生产部
                    Dispatch();


                    //this.View.Model.SetValue("FText_ScanCode", "");   //清空条码字段

                }

            }

        }

        public override void ButtonClick(ButtonClickEventArgs e)
        {
            base.ButtonClick(e);
            string a;
            if ((a = e.Key.ToUpper()) != null)
            {
                //if (a == "F_HMD_BUTTON")
                //{
                //    this.Dispatch();
                //}

                //base.View.ShowMessage(ResManager.LoadKDString("当前未选中！" + a, "015747000028226", Kingdee.BOS.Resource.SubSystemType.MFG));

            }
        }


        private void Dispatch()
        {
            ExportLogInfo exportLogInfo = new ExportLogInfo
            {
                Code = this.runId,
                StartTime = DateTime.Now
            };


            int FCount = 0;
            decimal FOperQty = 0;
            decimal FDisPatchQty = 0;
            int FRow = -1;
            int FDETAILID = 0;
            string SQL = "";
            string SL = "";

            FCount = this.DicTableData.Count();

            Dictionary<string, object> FirstRowData;


            for (int i = 0; i < FCount; i++)
            {
                this.DicTableData.TryGetValue(i, out FirstRowData);
                FOperQty = Convert.ToDecimal(FirstRowData["FOperQty"]);
                FDisPatchQty = Convert.ToDecimal(FirstRowData["FDispatchQty"]);
                FDETAILID = Convert.ToInt32(FirstRowData["EntryPkId"]);

                SQL = @"/*dialect*/select count(*) as SL from T_SFC_OPERPLANNINGDETAIL t1
                                    left join T_SFC_OPERPLANNINGSEQ t2 on t1.FENTRYID = t2.FENTRYID
                                    left join T_SFC_OPERPLANNING t3 on t2.FID = t3.FID
                                    left join T_BD_DEPARTMENT t4 on t3.FPRODEPARTMENTID = t4.FDEPTID
                                    where FPROORGID = 100026  and t4.FNUMBER = '000362' and FDETAILID = " + FDETAILID + " ";
                DynamicObjectCollection Dyobj = DBUtils.ExecuteDynamicObject(this.Context, SQL);
                foreach (DynamicObject obj in Dyobj)
                {
                    SL = obj["SL"].ToString();

                }

                if (FOperQty > FDisPatchQty && SL!="0")
                {
                    FRow = i;
                    i = FCount;
                }

            }

            //如果全部查询结果都已经派工完成,退出
            if (FRow == -1)
            {
                return;
            }


            //int[] selectedRowIndexs = GetSelectedRowIndexs();

            //if (selectedRowIndexs.Length <= 0)
            //{
            //    return;
            //}

            //    //Dictionary<string, object> currentRowData = base.GetCurrentRowData();

            //base.View.GetControl<MobileListViewControl>("FMobileListViewEntity").SetSelectRows(new int[0]);
            //int[] rowIndexs = new int[1];
            //rowIndexs[0] = 0;
            //List<Dictionary<string, object>> S = GetSelectedRowData(rowIndexs);//S里面可以获取当前用户名及扫描信息对应的行信息



            Dictionary<string, object> currentRowData = GetCurrentRowData2(FRow);

            //Dictionary<string, object> currentRowData = base.GetCurrentRowData();
            if (currentRowData == null)
            {
                base.View.ShowMessage(ResManager.LoadKDString("当前未选中行！", "015747000028226", Kingdee.BOS.Resource.SubSystemType.MFG));
                return;
            }
            MobileShowParameter mobileShowParameter = new MobileShowParameter
            {
                FormId = "SFC_MobileComplexDispatchBillEdit",
                CustomComplexParams =
                {
                    {
                        "OperId",
                        currentRowData["OperId"]
                    },
                    {
                        "IsList",
                        this._currDataFilterType == MobileEnums.DataFilterType.Dispatched
                    }
                }
            };
            if (currentRowData.ContainsKey("DispatchDetailId") && Convert.ToInt64(currentRowData["DispatchDetailId"]) > 0L)
            {
                BusinessInfo businessInfo = FormMetaDataCache.GetCachedFormMetaData(base.Context, "SFC_DispatchDetail").BusinessInfo;
                NetworkCtrlResult networkCtrlResult = NetCtrlUtils.StartNetworkCtrl(base.Context, businessInfo, "Edit", currentRowData["DispatchDetailId"], "");
                if (networkCtrlResult != null && !networkCtrlResult.StartSuccess && networkCtrlResult.ConflictUserName == base.Context.UserName)
                {
                    foreach (NetWorkCtrlMonitorInfo netWorkCtrlMonitorInfo in networkCtrlResult.ConflictMonitorInfos)
                    {
                        networkCtrlResult.TaskID = netWorkCtrlMonitorInfo.Id;
                        NetCtrlUtils.CommitNetworkCtrl(base.Context, networkCtrlResult);
                    }
                    networkCtrlResult = NetCtrlUtils.StartNetworkCtrl(base.Context, businessInfo, "Edit", currentRowData["DispatchDetailId"], "");
                }
                if (networkCtrlResult != null && networkCtrlResult.StartSuccess)
                {
                    mobileShowParameter.CustomComplexParams.Add("NetworkCtrlResult", networkCtrlResult);
                    base.View.ShowForm(mobileShowParameter, delegate (FormResult returnValue)
                    {
                        base.View.GetControl<MobileListViewControl>("FMobileListViewEntity").SetSelectRows(new int[0]);
                        base.View.OpenParameter.SetCustomParameter("ListCurrPage", this.CurrPageNumber);
                        this.ReloadListData(null, true);
                    });
                }
                else
                {
                    string text = string.Format(ResManager.LoadKDString("派工明细被[{0}]锁定，暂不能进行修改操作！", "015747000022006", Kingdee.BOS.Resource.SubSystemType.MFG));
                    base.View.ShowStatusBarInfo(text);
                }
            }
            else
            {
                base.View.ShowForm(mobileShowParameter, delegate (FormResult returnValue)
                {
                    base.View.GetControl<MobileListViewControl>("FMobileListViewEntity").SetSelectRows(new int[0]);
                    base.View.OpenParameter.SetCustomParameter("ListCurrPage", this.CurrPageNumber);
                    this.ReloadListData(null, true);
                });
            }
            if (base.IsWriteLog)
            {
                exportLogInfo.UserId = base.Context.UserId.ToString();
                exportLogInfo.MethodName = MethodBase.GetCurrentMethod().DeclaringType.FullName + '.' + MethodBase.GetCurrentMethod().Name;
                exportLogInfo.Detail = ResManager.LoadKDString("工序派工点击派工按钮", "015747000021953", Kingdee.BOS.Resource.SubSystemType.MFG);
                exportLogInfo.EndTime = DateTime.Now;
                ExportLogServiceHelper.WriteLog(base.Context, exportLogInfo);
            }
        }

        public Dictionary<string, object> GetCurrentRowData2(int Row)
        {
            //int[] selectedRowIndexs = this.GetSelectedRowIndexs();
            int[] selectedRowIndexs = new int[1];
            selectedRowIndexs[0] = Row;
            List<Dictionary<string, object>> selectedRowData = this.GetSelectedRowData(selectedRowIndexs);
            if (selectedRowData.Count <= 0)
            {
                return null;
            }
            return selectedRowData[0];
        }

        
       


    }


}