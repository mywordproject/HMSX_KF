using HMSX.MFG.Mobile.Business.PlugIn.Second;
using HMSX.Second.Plugin.供应链;
using Kingdee.BOS;
using Kingdee.BOS.App.Core.Utils;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.NetworkCtrl;
using Kingdee.BOS.Mobile;
using Kingdee.BOS.Mobile.PlugIn.ControlModel;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Resource;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using Kingdee.K3.Core.Mobile.Utils;
using Kingdee.K3.MFG.Common.BusinessEntity.SFC.SFCDymObjManager.SFC.Bill;
using Kingdee.K3.MFG.Mobile.Business.PlugIn.SFC.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace HMSX.Second.Plugin.MES
{
    [HotUpdate]
    [Description("工序任务超市--领料数大于零，不允许关闭2023")]
    public class GXRWCS2023 : MobileComplexTaskPoolListEdit
    {
        List<long> list = new List<long>();
        Dictionary<long, long> dic = new Dictionary<long, long>();
        protected Dictionary<int, int> SelectedDataIndex = new Dictionary<int, int>();
        public override void ButtonClick(ButtonClickEventArgs e)
        {
            base.ButtonClick(e);
            string a = e.Key.ToUpper();
            switch (a)
            {
                case "F_260_QX":
                    AllSelect("FMobileListViewEntity_Detail");
                    break;
                case "F_260_PLGB":
                    this.CloseRow1(false);
                    break;
                case "F_DYBQ":
                    this.printLable1();
                    break;
                case "F_ZJLL":
                    string ysm = this.Model.GetValue("F_YSM") == null ? "" : this.Model.GetValue("F_YSM").ToString();
                    PickMaterial(ysm);
                    break;
                case "F_SMBD":
                    SMBD();
                    break;
            }
        }
        public void SMBD()
        {
            string relt = "";
            int rowIndex = this.Model.GetEntryCurrentRowIndex("FMobileListViewEntity");
            Dictionary<string, object> currentRowData = this.GetCurrentRowData(rowIndex);
            string malnumber = currentRowData["FProductId"].ToString().Substring(0, currentRowData["FProductId"].ToString().IndexOf("/"));
            string[] scdd = currentRowData["FMONumber"].ToString().Split('-');
            string wlqdsql = $@"select * from T_PRD_PPBOM a
                         inner join T_PRD_PPBOMENTRY b  on b.fid=a.fid
                         left join t_BD_MaterialBase c ON c.FMATERIALID=b.FMATERIALID
                         left join t_BD_Material d on a.FMATERIALID=d.FMATERIALID 
                         where d.fnumber='{malnumber}' and a.FMOBILLNO='{scdd[0]}' and a.FMOENTRYSEQ='{scdd[1]}' and FERPCLSID!=1 ";
            wlqd = DBUtils.ExecuteDynamicObject(Context, wlqdsql);
            if (wlqd.Count == 0)
            {
                throw new KDBusinessException("", "冲压工序无需扫码绑定初始条码！！");

            }
            MobileShowParameter param = new MobileShowParameter();
            param.FormId = "k2ffc9666e58c4553a7ecacdb69e9b10a";
            param.ParentPageId = this.View.PageId;
            param.SyncCallBackAction = false;
            param.CustomParams.Add("WLDM", malnumber);
            param.CustomParams.Add("SCDD", scdd[0]);
            param.CustomParams.Add("SEQ", scdd[1]);
            param.CustomParams.Add("RLSL", this.Model.DataObject["ClaimQty"].ToString());
            this.View.ShowForm(param, delegate (FormResult result)
            {
                string[] date = (string[])result.ReturnData;
                if (date != null)
                {
                    this.Model.SetValue("F_YSM", date[0]);
                    this.View.UpdateView("F_YSM");
                    this.Model.SetValue("FCLAIMQTY", date[1]);
                    this.View.UpdateView("FCLAIMQTY");
                    this.Model.SetValue("F_RUJP_QTY", date[1]);
                    this.View.UpdateView("F_RUJP_QTY");
                }
            });
        }
        public void PickMaterial(string tm)
        {
            string[] ysms = tm.Split(',');
            string entryId = "0";
            string cstm = "";
            dic.Clear();
            if (ysms.Length > 0 && tm != "")
            {

                int i = 1;
                foreach (var ysm in ysms)
                {
                    if (i == ysms.Length)
                    {
                        cstm = cstm + "F_260_CSTM like '%" + ysm + "%'";
                    }
                    else
                    {
                        cstm = cstm + "F_260_CSTM like '%" + ysm + "%'  or ";
                    }
                    i++;
                }
                string pgsql = $@"select top 1 FENTRYID,F_260_CSTM
                                     FROM T_SFC_DISPATCHDETAILENTRY 
                                     where F_260_CSTM!=''and ({cstm}) order by FDISPATCHTIME desc";
                var pgs = DBUtils.ExecuteDynamicObject(Context, pgsql);
                foreach (var pg in pgs)
                {
                    entryId = entryId + ',' + pg["FENTRYID"].ToString();
                    dic.Add(Convert.ToInt64(pg["FENTRYID"].ToString()), Convert.ToInt64(pg["FENTRYID"].ToString()));
                    list.Add(Convert.ToInt64(pg["FENTRYID"].ToString()));
                }

            }
            SavePgBom();
            MobileShowParameter param = new MobileShowParameter();
            param.FormId = "k06daef5616224128b31d49c5ccbc9d76";
            param.ParentPageId = this.View.PageId;
            param.SyncCallBackAction = false;
            param.CustomParams.Add("FPgEntryId", entryId);
            param.CustomParams.Add("FYSM", cstm);
            this.ShowFrom(param);

        }
        public void SavePgBom()
        {
            string MoBillNo = "";//生产订单号
            string MoBillEntrySeq = "";//生产订明细行号
            foreach (var entryId in dic)
            {
                string strSql = string.Format(@"SELECT T.FMOBILLNO,T.FMOSEQ,T1.FWORKQTY FROM T_SFC_DISPATCHDETAIL T INNER JOIN T_SFC_DISPATCHDETAILENTRY T1 ON T.FID=T1.FID AND T1.FENTRYID IN({0})", entryId.Key);
                DynamicObjectCollection rs = DBServiceHelper.ExecuteDynamicObject(this.Context, strSql);
                if (rs.Count > 0)
                {
                    for (int i = 0; i < rs.Count; i++)
                    {
                        MoBillNo = rs[i]["FMOBILLNO"].ToString();
                        MoBillEntrySeq = rs[i]["FMOSEQ"].ToString();
                        List<DynamicObject> PPBomInfo = this.GetPPBomInfo(MoBillNo, MoBillEntrySeq);
                        foreach (DynamicObject obj in PPBomInfo)
                        {
                            Decimal mustQty = Convert.ToDecimal(obj["FNUMERATOR"]) / Convert.ToDecimal(obj["FDENOMINATOR"]) * Convert.ToDecimal(rs[i]["FWORKQTY"]) * (Convert.ToDecimal(obj["FUSERATE"]) / 100);
                            DynamicObjectCollection rsentrys = DBServiceHelper.ExecuteDynamicObject(this.Context, string.Format("select * from t_PgBomInfo where FPgEntryId={0} AND FPPBomEntryId={1}", entryId.Key, Convert.ToInt64(obj["FENTRYID"])));
                            if (rsentrys.Count == 0)
                            {
                                string Sql = string.Format(@" INSERT INTO t_PgBomInfo(FPgEntryId,FPPBomId,FPPBomEntryId,FMaterialId,FPgQty,FMustQty,FPickQty,FReturnQty,FFeedQty,FAllPickQty,FAvailableQty)
                       Values({0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10})", entryId.Key, Convert.ToInt64(obj["FID"]), Convert.ToInt64(obj["FENTRYID"]), Convert.ToInt64(obj["FMATERIALID"]), Convert.ToDecimal(rs[i]["FWORKQTY"]), mustQty, 0, 0, 0, 0, 0);
                                int row = DBServiceHelper.Execute(this.Context, Sql);
                            }
                        }
                    }
                }
            }
        }
        private List<DynamicObject> GetPPBomInfo(string MoBillNo, string MoBillEntrySeq)
        {
            string strSql = string.Format(@"SELECT T.FPRDORGID,T.FMOBillNO,T.FMOENTRYSEQ,T1.FSEQ,T1.FID,T1.FENTRYID,T1.FMATERIALID,T3.FMASTERID,T3.FNUMBER,T4.FNAME,T4.FSPECIFICATION,T2.FPICKEDQTY,T5.FSTOCKID,T1.FNUMERATOR,T1.FDENOMINATOR,T1.FSCRAPRATE,FUSERATE  FROM T_PRD_PPBOM T 
                                                             INNER JOIN T_PRD_PPBOMENTRY T1 ON T.FID=T1.FID 
                                                             INNER JOIN T_PRD_PPBOMENTRY_Q T2 ON T1.FID=T2.FID AND T1.FENTRYID=T2.FENTRYID  AND( T1.FMUSTQTY>(T2.FPICKEDQTY-t2.FGOODRETURNQTY) or FMUSTQTY=0 and FUSERATE=0)
                                                             INNER JOIN T_PRD_PPBOMENTRY_C T5 ON T1.FID=T5.FID AND T1.FENTRYID=T5.FENTRYID
                                                             INNER JOIN T_BD_MATERIAL T3 ON T1.FMATERIALID=T3.FMATERIALID  AND T3.FMATERIALID NOT IN (SELECT FMATERIALID FROM T_BD_MATERIALBASE WHERE FErpClsID=5 )
                                                             INNER JOIN T_BD_MATERIAL_L T4 ON T1.FMATERIALID=T4.FMATERIALID AND T4.FLOCALEID=2052
                                                             WHERE T.FMOBillNO='{0}' AND T.FMOENTRYSEQ={1} AND T5.FISSUETYPE IN ('1','3')", MoBillNo, MoBillEntrySeq);
            DynamicObjectCollection source = DBServiceHelper.ExecuteDynamicObject(base.Context, strSql);
            return source.ToList<DynamicObject>();
        }
        private void ShowFrom(MobileShowParameter param)
        {
            this.View.ShowForm(param, new Action<FormResult>((res) =>
            {
                this.View.Refresh();
            }));
        }
        protected void AllSelect(string entityKey)
        {
            int entryRowCount = this.Model.GetEntryRowCount(entityKey);
            List<int> list = new List<int>();
            for (int i = 0; i < entryRowCount; i++)
            {
                list.Add(i);
                this.ListFormaterManager.SetControlProperty("FFlowLayout_Detail", i, "255,234,199", MobileFormatConditionPropertyEnums.BackColor);
            }
            base.View.GetControl<MobileListViewControl>(entityKey).SetSelectRows(list.ToArray());
            base.View.GetControl<MobileListViewControl>(entityKey).SetFormat(this.ListFormaterManager);
            this.View.UpdateView(entityKey);
        }
        protected void CloseRow1(bool flag)
        {
            int[] selectedRows = base.View.GetControl<MobileListViewControl>("FMobileListViewEntity_Detail").GetSelectedRows();
            if (!selectedRows.Any<int>())
            {
                base.View.ShowStatusBarInfo(Kingdee.BOS.Resource.ResManager.LoadKDString("未选择分录！", "015747000028217", Kingdee.BOS.Resource.SubSystemType.MFG, new object[0]));
            }
            else
            {
                List<Dictionary<string, object>> dictionarys = new List<Dictionary<string, object>>();
                for (int i = 0; i < selectedRows.Length; i++)
                {
                    int num = selectedRows[i] + this.RowCountPerPage * (this.detailCurrPageIndex - 1);
                    System.Collections.Generic.Dictionary<string, object> dictionary = this.detailTableData[num];
                    bool flag3 = dictionary != null;
                    if (flag3)
                    {
                        bool flag4 = Convert.ToDouble(dictionary["F_260_LLSL"].ToString()) > 0.0;
                        if (flag4)
                        {
                            throw new KDBusinessException("", "领料数量大于0不允许关闭！");
                        }
                    }
                    dictionarys.Add(dictionary);
                }
                foreach (var dictionary in dictionarys)
                {//反写绑定数量
                    string pgtmsql = $@"select * from T_SFC_DISPATCHDETAILENTRY where FBARCODE='{dictionary["FBarCode"]}'";
                    var pgtms = DBUtils.ExecuteDynamicObject(Context, pgtmsql);
                    foreach (var pgtm in pgtms)
                    {
                        string[] cstms = pgtm["F_260_CSTM"].ToString().Split(',');
                        string tm = "";
                        int i = 1;
                        foreach (string cstm in cstms)
                        {
                            if (i == cstms.Length)
                            {
                                tm = tm + "F_260_CSTM like '%" + cstm + "%'";
                            }
                            else
                            {
                                tm = tm + "F_260_CSTM like '%" + cstm + "%'  or ";
                            }
                            i++;
                        }
                        string ylqdsql = $@"/*dialect*/select 
                              FNUMERATOR/FDENOMINATOR bl,PGMX.FENTRYID
                              from T_PRD_PPBOM a
                              inner join T_PRD_PPBOMENTRY b on a.fid=b.fid
                              INNER JOIN t_BD_Material c on a.FMATERIALID=c.FMATERIALID
                              inner join t_BD_MaterialBase d ON b.FMATERIALID=d.FMATERIALID and FERPCLSID!=1
                              INNER JOIN
                               (  SELECT distinct FMATERIALID,
                           (SELECT distinct  convert(varchar(255),b.FENTRYID)+','
                           from T_SFC_DISPATCHDETAIL A
                           inner join T_SFC_DISPATCHDETAILENTRY B on A.FID=B.FID  
                           WHERE F_260_CSTM!=''and ({tm}) AND A.FMATERIALID=T.FMATERIALID for xml path(''))as FENTRYID
                           from T_SFC_DISPATCHDETAIL t 
                           inner join T_SFC_DISPATCHDETAILENTRY t1 on t.FID=t1.FID  
                           WHERE F_260_CSTM!=''and ({tm})) PGMX ON PGMX.FMATERIALID=b.FMATERIALID
                             where c.FNUMBER='{dictionary["FMaterialNumber"]}'and  a.FMOBILLNO='{dictionary["FMoBillNo"]}' and a.FMOENTRYSEQ='{dictionary["FMoSeq"]}'";
                        var ylqds = DBUtils.ExecuteDynamicObject(Context, ylqdsql);
                        if (ylqds.Count <= 1)
                        {
                            foreach (var ylqd in ylqds)
                            {
                                string upsql = $@"update T_SFC_DISPATCHDETAILENTRY set 
                             F_260_SYBDSL=FWORKQTY,F_260_XBSL=0
                             where FENTRYID in ({ylqd["FENTRYID"].ToString().Trim(',')})";
                                DBUtils.Execute(Context, upsql);
                            }
                        }
                        else
                        {
                            foreach (var ylqd in ylqds)
                            {
                                string upsql = $@"update T_SFC_DISPATCHDETAILENTRY set 
                             F_260_SYBDSL=F_260_SYBDSL+{Convert.ToDecimal(ylqd["bl"]) * Convert.ToDecimal(pgtm["FWORKQTY"])},
                               F_260_XBSL=F_260_XBSL-{Convert.ToDecimal(ylqd["bl"]) * Convert.ToDecimal(pgtm["FWORKQTY"])}
                             where FENTRYID in ({ylqd["FENTRYID"].ToString().Trim(',')})";
                                DBUtils.Execute(Context, upsql);
                            }
                        }
                        
                    }
                    string fsjysql = $@"select FNUMBER,FNAME from  T_BAS_PREBDFIVE a
                                           inner join T_BAS_PREBDFIVE_L b on a.FID=b.FID
                                           WHERE FNAME='关闭校验'";
                    var fsjy = DBUtils.ExecuteDynamicObject(Context, fsjysql);
                    if (fsjy.Count > 0)
                    {
                        Encoding encoding = Encoding.UTF8;
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(fsjy[0]["FNUMBER"].ToString());
                        request.Method = "POST";
                        request.ContentType = "application/json; charset=UTF-8";
                        request.Headers["Accept-Encoding"] = "gzip, deflate";
                        request.AutomaticDecompression = DecompressionMethods.GZip;
                        JObject jsonRoot = new JObject();
                        jsonRoot.Add("fbillno", dictionary["FBarCode"].ToString());
                        jsonRoot.Add("fbilltype", "派工明细");
                        byte[] buffer = encoding.GetBytes(jsonRoot.ToString());
                        request.ContentLength = buffer.Length;
                        request.GetRequestStream().Write(buffer, 0, buffer.Length);
                        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                        using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                        {
                            供应链.WMSclass jsonDatas = JsonConvert.DeserializeObject<供应链.WMSclass>(reader.ReadToEnd());
                            if (jsonDatas.Code != "0")
                            {
                                throw new KDBusinessException("", jsonDatas.Message);
                            }
                        }
                    }
                    string fsjysql1 = $@"select FNUMBER,FNAME from  T_BAS_PREBDFIVE a
                                           inner join T_BAS_PREBDFIVE_L b on a.FID=b.FID
                                           WHERE FNAME='执行关闭'";
                    var fsjy1 = DBUtils.ExecuteDynamicObject(Context, fsjysql1);
                    if (fsjy1.Count > 0)
                    {
                        Encoding encoding1 = Encoding.UTF8;
                        HttpWebRequest request1 = (HttpWebRequest)WebRequest.Create(fsjy1[0]["FNUMBER"].ToString());
                        request1.Method = "POST";
                        request1.ContentType = "application/json; charset=UTF-8";
                        request1.Headers["Accept-Encoding"] = "gzip, deflate";
                        request1.AutomaticDecompression = DecompressionMethods.GZip;
                        JObject jsonRoot1 = new JObject();
                        jsonRoot1.Add("fbillno", dictionary["FBarCode"].ToString());
                        jsonRoot1.Add("fbilltype", "派工明细");
                        byte[] buffer1 = encoding1.GetBytes(jsonRoot1.ToString());
                        request1.ContentLength = buffer1.Length;
                        request1.GetRequestStream().Write(buffer1, 0, buffer1.Length);
                        HttpWebResponse response1 = (HttpWebResponse)request1.GetResponse();
                        using (StreamReader reader = new StreamReader(response1.GetResponseStream(), Encoding.UTF8))
                        {
                            供应链.WMSclass jsonDatas = JsonConvert.DeserializeObject<供应链.WMSclass>(reader.ReadToEnd());
                            if (jsonDatas.Code != "0")
                            {
                                throw new KDBusinessException("", jsonDatas.Message);
                            }
                        }
                    }

                    System.Collections.Generic.List<string> lstDisPatchIds = new System.Collections.Generic.List<string>
                    {
                    dictionary["PkId"].ToString()
                    };
                    System.Collections.Generic.List<Kingdee.BOS.Core.NetworkCtrl.NetworkCtrlResult> netCtrlDispatchIds = this.GetNetCtrlDispatchIds(lstDisPatchIds);
                    if (netCtrlDispatchIds.Count > 0)
                    {
                        NetworkCtrlServiceHelper.BatchCommitNetCtrl(base.Context, netCtrlDispatchIds);
                        System.Collections.Generic.List<string> list = (
                            from o in netCtrlDispatchIds
                            select o.InterID).ToList<string>();
                        DynamicObject dynamicObject = SFCDispatchManager.Instance.Load(base.Context, list.ToArray()).FirstOrDefault<Kingdee.BOS.Orm.DataEntity.DynamicObject>();
                        string text = System.Convert.ToString(dictionary["PkId"]);
                        object entryId = dictionary["EntryPkId"];
                        DynamicObjectCollection dynamicObjectItemValue = dynamicObject.GetDynamicObjectItemValue<DynamicObjectCollection>("DispatchDetailEntry", null);
                        DynamicObject dynamicObject2 = (
                            from o in dynamicObjectItemValue
                            where entryId.Equals(o["Id"])
                            select o).FirstOrDefault<Kingdee.BOS.Orm.DataEntity.DynamicObject>();
                        if (dynamicObject2 != null)
                        {
                            if (System.Convert.ToDecimal(dynamicObject2["FinishSelQty"]) == 0m)
                            {
                                dynamicObjectItemValue.Remove(dynamicObject2);
                            }
                            else
                            {
                                dynamicObject2["BaseWorkQty"] = dynamicObject2["BaseFinishSelQty"];
                                dynamicObject2["WorkQty"] = dynamicObject2["FinishSelQty"];
                                dynamicObject2["WorkHeadQty"] = dynamicObject2["FinishSelHeadQty"];
                                dynamicObject2["Status"] = "D";
                            }
                        }
                        Kingdee.BOS.Orm.OperateOption operateOption = Kingdee.BOS.Orm.OperateOption.Create();
                        operateOption.SetVariableValue("IsMobileInvoke", true);
                        Kingdee.BOS.Core.DynamicForm.IOperationResult operationResult = SFCDispatchManager.Instance.Save(base.Context, new Kingdee.BOS.Orm.DataEntity.DynamicObject[]
                        {
                        dynamicObject
                        }, operateOption);
                        if (operationResult.IsSuccess)
                        {
                            this.BindDispatchDetailList("");
                            if (!flag)
                            {
                                base.View.ShowStatusBarInfo(Kingdee.BOS.Resource.ResManager.LoadKDString("批量关闭成功！", "015747000026594", Kingdee.BOS.Resource.SubSystemType.MFG, new object[0]));
                            }
                            else
                            {
                                base.View.ShowStatusBarInfo(Kingdee.BOS.Resource.ResManager.LoadKDString("批量删除成功！", "015747000026594", Kingdee.BOS.Resource.SubSystemType.MFG, new object[0]));
                            }
                        }

                    }
                }
            }

        }
        public override void EntityRowDoubleClick(EntityRowClickEventArgs e)
        {
            base.EntityRowDoubleClick(e);
            int entryRowCount = this.Model.GetEntryRowCount("FMobileListViewEntity_Detail");
            int[] selectedRows = base.View.GetControl<MobileListViewControl>("FMobileListViewEntity_Detail").GetSelectedRows();
            this.View.GetControl<MobileListViewControl>("FMobileListViewEntity_Detail").SetSelectRows(selectedRows);
            for (int i = 0; i < entryRowCount; i++)
            {
                if (Array.IndexOf(selectedRows, i) == -1)
                {
                    this.ListFormaterManager.SetControlProperty("FFlowLayout_Detail", i, "255,255,255", MobileFormatConditionPropertyEnums.BackColor);
                }
                else
                {
                    this.ListFormaterManager.SetControlProperty("FFlowLayout_Detail", i, "255,234,199", MobileFormatConditionPropertyEnums.BackColor);
                }
            }
            this.View.GetControl<MobileListViewControl>("FMobileListViewEntity_Detail").SetFormat(this.ListFormaterManager);
            this.View.UpdateView("FMobileListViewEntity_Detail");

        }
        public void printLable1()
        {
            int[] selectedRows = base.View.GetControl<MobileListViewControl>("FMobileListViewEntity_Detail").GetSelectedRows();
            int num = selectedRows.FirstOrDefault<int>() + this.RowCountPerPage * (this.detailCurrPageIndex - 1);
            if (!selectedRows.Any<int>())
            {
                base.View.ShowStatusBarInfo(ResManager.LoadKDString("未选择分录！", "015747000028217", SubSystemType.MFG, new object[0]));
            }
            else
            {
                Dictionary<string, object> dictionary = this.detailTableData[num];
                string billBarCode = dictionary["FBarCode"].ToString();
                this.Print(billBarCode, false);
            }
        }

        /**
        protected void ClaimedModity()
        {
            int[] selectedRows = base.View.GetControl<MobileListViewControl>("FMobileListViewEntity_Detail").GetSelectedRows();
            if (!selectedRows.Any<int>())
            {
                base.View.ShowStatusBarInfo(ResManager.LoadKDString("未选择分录！", "015747000028217", SubSystemType.MFG, new object[0]));
            }
            else
            {
                int num = selectedRows.FirstOrDefault<int>() + this.RowCountPerPage * (this.detailCurrPageIndex - 1);
                Dictionary<string, object> dictionary = this.detailTableData[num - 1];
                decimal dispatchQtyByPK = this.GetDispatchQtyByPK(Convert.ToInt64(dictionary["EntryPkId"]));
                if (Convert.ToDecimal(this.Model.DataObject["ClaimedQty"]) > dispatchQtyByPK || Convert.ToDecimal(this.Model.DataObject["ClaimedQty"]) <= 0m)
                {
                    base.View.ShowErrMessage(ResManager.LoadKDString("数量必须大于0且不能超过选中行的“可认领”数量！", "0151515153512030033830", SubSystemType.MFG, new object[0]), "", MessageBoxType.Notice);
                }
                else
                {
                    if (Convert.ToDateTime(this.Model.DataObject["PlanBeginedTime"]) < Convert.ToDateTime(this.Model.DataObject["PlanBeginedTime"]))
                    {
                        base.View.ShowErrMessage(ResManager.LoadKDString("计划结束时间不能早于计划开始时间！", "0151515153512030033831", SubSystemType.MFG, new object[0]), "", MessageBoxType.Notice);
                    }
                    List<string> lstDisPatchIds = new List<string>
            {
                dictionary["PkId"].ToString()
            };
                    List<NetworkCtrlResult> netCtrlDispatchIds = this.GetNetCtrlDispatchIds(lstDisPatchIds);
                    if (netCtrlDispatchIds.Count > 0)
                    {
                        NetworkCtrlServiceHelper.BatchCommitNetCtrl(base.Context, netCtrlDispatchIds);
                        List<string> list = (
                            from o in netCtrlDispatchIds
                            select o.InterID).ToList<string>();
                        string text = Convert.ToString(dictionary["PkId"]);
                        object entryId = dictionary["EntryPkId"];
                        DynamicObject dynamicObject = SFCDispatchManager.Instance.Load(base.Context, list.ToArray()).FirstOrDefault<DynamicObject>();
                        DynamicObjectCollection dynamicObjectItemValue = dynamicObject.GetDynamicObjectItemValue<DynamicObjectCollection>("DispatchDetailEntry", null);
                        DynamicObject dynamicObject2 = (
                            from o in dynamicObjectItemValue
                            where entryId.Equals(o["Id"])
                            select o).FirstOrDefault<DynamicObject>();
                        if (dynamicObject2 != null)
                        {
                            if (this.curUnitConvert == null)
                            {
                                this.curUnitConvert = this.GetUnitConvert(Convert.ToInt64(dynamicObject["MaterialId_Id"]), Convert.ToInt64(dynamicObject["FUnitID_Id"]), Convert.ToInt64(dynamicObject["BaseUnitID_Id"]));
                            }
                            decimal num2 = Convert.ToDecimal(this.Model.DataObject["ClaimedQty"]) * Convert.ToDecimal(dynamicObject["UnitTransHeadQty"]) / Convert.ToDecimal(dynamicObject["UnitTransOperQty"]);
                            dynamicObject2["BaseWorkQty"] = this.curUnitConvert.ConvertQty(num2, "");
                            dynamicObject2["WorkQty"] = this.Model.DataObject["ClaimedQty"];
                            dynamicObject2["WorkHeadQty"] = num2;
                            dynamicObject2["PlanBeginTime"] = this.Model.DataObject["PlanBeginedTime"];
                            dynamicObject2["PlanEndTime"] = this.Model.DataObject["PlanEndedTime"];
                        }
                        OperateOption operateOption = OperateOption.Create();
                        operateOption.SetVariableValue("IsMobileInvoke", true);
                        IOperationResult operationResult = SFCDispatchManager.Instance.Save(base.Context, new DynamicObject[]
                        {
                    dynamicObject
                        }, operateOption);
                        if (operationResult.IsSuccess)
                        {
                            base.View.ShowStatusBarInfo(ResManager.LoadKDString("成功！", "015747000028224", SubSystemType.MFG, new object[0]));
                            this.Model.DataObject["ClaimedQty"] = 0;
                            base.View.SetControlProperty("FClaimedQty", 0);
                            base.View.UpdateView("FClaimedQty");
                            this.Model.DataObject["PlanBeginedTime"] = null;
                            this.Model.DataObject["PlanEndedTime"] = null;
                            base.View.UpdateView("FPlanBeginedTime");
                            base.View.UpdateView("FPlanEndedTime");
                            base.View.GetControl("FBtn_ClaimModify").Enabled = false;
                            base.View.UpdateView("FBtn_ClaimModify");
                            this.IsClaim = true;
                            this.CurrDataFilterType = MobileEnums.DataFilterType.UnFinishedDetail;
                        }
                        else
                        {
                            base.View.ShowErrMessage(MobBusinessUtils.GetErrMsgFromOperationResult("", operationResult), "", MessageBoxType.Notice);
                        }
                    }
                }
            }
        }
        **/
    }
}

