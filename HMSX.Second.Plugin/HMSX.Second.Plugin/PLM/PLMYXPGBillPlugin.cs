
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Resource;
using Kingdee.K3.PLM.Common.BusinessEntity.View;
using Kingdee.K3.PLM.STD.Business.PlugIn.EngineeringChange;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.App.Core.Utils;
using Kingdee.BOS.Business.Bill.Operation;
using Kingdee.BOS.Business.DynamicForm.Operation;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.Metadata.DataEntity;
using Kingdee.BOS.Util;
using Kingdee.K3.PLM.Business.PlugIn;
using Kingdee.K3.PLM.CFG.Business.PlugIn.Common;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.Base;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.CategoryStatus;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.Common;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.Entity;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.Enum;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.Manager;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.Manager.ControlRule;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.Manager.Document;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.Manager.MultiOrganization;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.Manager.PhysicalFile;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.Manager.User;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.Manager.Version;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.Metadata;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.View;
using Kingdee.K3.PLM.CFG.Common.Interface.EngineeringChange;
using Kingdee.K3.PLM.CFG.Common.Interface.STD.EngineeringChange;
using Kingdee.K3.PLM.CFG.Common.Interface.WF;
using Kingdee.K3.PLM.Common.BusinessEntity.Const;
using Kingdee.K3.PLM.Common.BusinessEntity.Operation;
using Kingdee.K3.PLM.Common.Core;
using Kingdee.K3.PLM.Common.Core.BOSBridge;
using Kingdee.K3.PLM.Common.Core.Cache;
using Kingdee.K3.PLM.Common.Core.Common;
using Kingdee.K3.PLM.Common.Core.DynamicPluginHelper;
using Kingdee.K3.PLM.Common.Core.Resources;
using Kingdee.K3.PLM.Common.Core.ServiceHelper;
using Kingdee.K3.PLM.Common.Core.Utility;
using Kingdee.K3.PLM.Common.Framework.Exceptions;
using Kingdee.K3.PLM.STD.Common.BusinessEntity.Bom;
using Kingdee.K3.PLM.STD.Common.BusinessEntity.Bom.EBom;
using Kingdee.K3.PLM.STD.Common.BusinessEntity.Document;
using Kingdee.K3.PLM.STD.Common.BusinessEntity.EngineeringChange;
using Kingdee.K3.PLM.STD.Common.BusinessEntity.EngineeringChange.Entity;
using Kingdee.K3.PLM.STD.Common.BusinessEntity.EngineeringChange.ItemOperation;
using Kingdee.K3.PLM.STD.Common.BusinessEntity.EngineeringChange.Relevancy;
using Kingdee.K3.PLM.STD.Common.BusinessEntity.EngineeringChange.Relevancy.ERP;
using Kingdee.K3.PLM.STD.Common.BusinessEntity.Enum;
using Kingdee.K3.PLM.STD.Common.BusinessEntity.ERPBusiness.Manager;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using Kingdee.BOS;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.App.Data;
using System.Data;

namespace HMSX.Second.Plugin.PLM
{
    [Description("PLM影响评估")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class PLMYXPGBillPlugin : ECObjectEntityBill
    {
        private string userid = "";
        private EntryGrid grds;
        private string langIds = string.Empty;
        public override void OnLoad(EventArgs e)
        {
            this.grds = base.View.GetControl<EntryGrid>("FChangeObjectEntity");
            base.OnLoad(e);
        }
        public override void EntryBarItemClick(BarItemClickEventArgs e)
        {
            if (base.OperationRefused(e, false))
            {
                return;
            }
            List<string> list = new List<string>
            {
                "TBOPENATT",
                "TBBROWSERATT",
                "TBBROWSERATTPDF"
            };
            if (this.IsECR && !list.Contains(e.BarItemKey.ToUpper()) && ECRModel.Instance.HasEcn(this.PLMContext, Convert.ToInt64(this.Model.DataObject["Id"])))
            {
                string message = ResManager.LoadKDString("已经生成了变更单的申请不允许修改！", "120036000015310", SubSystemType.PLM, new object[0]);
                e.Cancel = true;
                this.PLMView.ShowNotificationMessage(message, "", MessageBoxType.Error);
                return;
            }
            DynamicObjectCollection entityCollection = GridHelper.GetEntityCollection(base.View, this.ChangeObjectEntity);
            List<long> list2 = new List<long>();
            List<string> list3 = new List<string>();
            List<int> list4 = new List<int>();
            for (int i = 0; i < base.View.Model.GetEntryRowCount(this.ChangeObjectEntity); i++)
            {
                long num = Convert.ToInt64(entityCollection[i]["BaseObject"]);
                if ((bool)base.View.Model.GetValue("FIsSelect", i) && num != 0L)
                {
                    list2.Add(num);
                    list4.Add(i);
                    int num2 = Convert.ToInt32(base.View.Model.GetValue("FItemType", i));
                    if (num2 == 3 || num2 == 2)
                    {
                        string item = (string)entityCollection[i]["Operation"];
                        list3.Add(item);
                    }
                }
            }

            if (e.BarItemKey == "keed_YXPG")
            {
                this.AssociateALLSS(list4, list2);
            }
            else if (e.BarItemKey == "SLSB_SCZHD")
            {
                if (Context.CurrentOrganizationInfo.ID == 100026)
                {
                    if (this.Model.GetValue("FLIFECIRCLESTAGE").ToString() != "AL")
                    {
                        throw new KDBusinessException("", "生命周期阶段为审核时才允许操作！！！");
                    }
                    if (this.Model.GetValue("FCODE") == null)
                    {
                        throw new KDBusinessException("", "请先保存！！！");
                    }
                    else
                    {
                        create();
                    }
                }
            }
            else if (e.BarItemKey.Equals("SLSB_XZBGDX"))
            {
                this.AddDesignChangeObject();
            }
            base.EntryBarItemClick(e);
        }
        public void AssociateALLs(List<int> rowIndexs, List<long> objIds)
        {
            if (rowIndexs.Count <= 0)
            {
                this.PLMView.ShowNotificationMessage(MessageBoxShowHelper.CheckedSuitableItems, "", MessageBoxType.Advise);
                return;
            }
            if (ECNManager.Instance.IsChangeObject(this.Entity[rowIndexs.FirstOrDefault<int>()]))
            {
                long objId = objIds.FirstOrDefault<long>();
                int index = rowIndexs.FirstOrDefault<int>();
                List<ERPRelevancyModel> list = new List<ERPRelevancyModel>();
                int num = Convert.ToInt32(this.Model.GetValue("FItemType", index));
                if (num == 2)
                {
                    ERPRelevancyModel eRPRelevancyModel = new ERPRelevancyModel();
                    string xml = (string)this.Model.GetValue("FOperation", index);
                    eRPRelevancyModel.Detail = OperationDataManager.Instance.DeserializeOperationXML(xml).FirstOrDefault<Operation>();
                    eRPRelevancyModel.Type = OperationType.StructureChanging;
                    eRPRelevancyModel.MainDyn = objId;
                    eRPRelevancyModel.MainCategoryId = 1030000000000000000L;
                    list.Add(eRPRelevancyModel);
                }
                else
                {
                    if (num == 3)
                    {
                        ERPRelevancyModel eRPRelevancyModel2 = new ERPRelevancyModel();
                        eRPRelevancyModel2.MainDyn = 0L;
                        string xml2 = (string)this.Model.GetValue("FOperation", index);
                        eRPRelevancyModel2.Detail = OperationDataManager.Instance.DeserializeOperationXML(xml2).FirstOrDefault<Operation>();
                        eRPRelevancyModel2.Type = OperationType.BatchSubstitute;
                        eRPRelevancyModel2.MainDyn = objId;
                        eRPRelevancyModel2.MainCategoryId = 1010000000000000000L;
                        list.Add(eRPRelevancyModel2);
                    }
                    else
                    {
                        DynamicObjectCollection source = this.Model.DataObject["ChangeObjectEntity"] as DynamicObjectCollection;
                        string key = (string)this.Model.GetValue("FROWID", index);
                        List<long> list2 = (
                            from q in source
                            where (string)q["PARENTROWID"] == key && string.IsNullOrWhiteSpace((string)q["ERPBillCode"])
                            select Convert.ToInt64(q["BaseObject"])).ToList<long>();
                        list2.Add(objId);
                        DynamicObject dynamicObject = source.FirstOrDefault((DynamicObject p) => p["BaseObject"].GetString() == objId.GetString());
                        if (CategoryContract.Instance.IsDocumentCategory(Convert.ToInt64(dynamicObject["ObjectCategory_Id"])))
                        {
                            RelatedType[] relationType = new RelatedType[]
                            {
                        RelatedType.RelatedMaterial
                            };
                            DynamicObjectCollection allRelatedObjectList = RelatedObjectModel.Instance.GetAllRelatedObjectList(this.PLMContext, objId, false, relationType, false);
                            foreach (DynamicObject current in allRelatedObjectList)
                            {
                                if (!list2.Contains(Convert.ToInt64(current["FRELATEDOBJECT"])))
                                {
                                    list2.Add(Convert.ToInt64(current["FRELATEDOBJECT"]));
                                }
                            }
                        }
                        DynamicObject[] source2 = BaseObjectManager.Instance(this.PLMContext).Load(this.PLMContext, list2.Cast<object>().ToArray<object>());
                        DynamicObject[] source3 = BaseObjectManager.Instance(this.PLMContext).Load(this.PLMContext, (
                            from m in source2
                            where m.Contains("IsChangeObject") && m["IsChangeObject"].ToString() == "True"
                            select m into k
                            select k["ChangeObjectId_Id"]).ToArray<object>());
                        foreach (long current2 in list2)
                        {
                            long newId = current2;
                            DynamicObject dynamicObject2 = (
                                from m in source2
                                where Convert.ToInt64(m["Id"]) == newId
                                select m).FirstOrDefault<DynamicObject>();
                            if (dynamicObject2 != null)
                            {
                                bool flag = false;
                                if (dynamicObject2.Contains("IsChangeObject"))
                                {
                                    flag = (dynamicObject2["IsChangeObject"] != null && dynamicObject2["IsChangeObject"].ToString() == "True");
                                }
                                if (flag)
                                {
                                    newId = Convert.ToInt64(dynamicObject2["ChangeObjectId_Id"]);
                                    dynamicObject2 = (
                                        from m in source3
                                        where Convert.ToInt64(m["Id"]) == newId
                                        select m).FirstOrDefault<DynamicObject>();
                                    if (objId == current2)
                                    {
                                        objId = newId;
                                    }
                                }
                                if (dynamicObject2 != null)
                                {
                                    long num2 = Convert.ToInt64(dynamicObject2["CategoryId_id"]);
                                    if (CategoryContract.Instance.IsMaterialCategory(num2) || CategoryContract.Instance.IsBOMCategory(num2))
                                    {
                                        list.Add(new ERPRelevancyModel
                                        {
                                            Type = OperationType.PropertyChanging,
                                            MainDyn = newId,
                                            MainCategoryId = num2
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
                string parentRowId = Convert.ToString(base.View.Model.GetValue("FROWID", index));
                List<string> value = (
                    from m in this.Entity
                    where (string)m["PARENTROWID"] == parentRowId
                    select m["BaseObject"].GetString()).ToList<string>();
                DynamicFormShowParameter dynamicFormShowParameter = new DynamicFormShowParameter();
                dynamicFormShowParameter.PageId = PLMGuid.NewGuidString().ToString();
                dynamicFormShowParameter.OpenStyle.ShowType = ShowType.Modal;
                dynamicFormShowParameter.FormId = "PLM_STD_EC_REFEVANCY_ALL";
                dynamicFormShowParameter.Caption = ResManager.LoadKDString("影响评估", "120036000015320", SubSystemType.PLM, new object[0]);
                dynamicFormShowParameter.CustomComplexParams.Add("plm_objectid", objId);
                dynamicFormShowParameter.CustomComplexParams.Add("plm_relevancyModels", list);
                dynamicFormShowParameter.CustomComplexParams.Add("plm_selRelevancyObjIdList", value);
                dynamicFormShowParameter.CustomComplexParams.Add("plm_ECNLIFECIRCLE", this.Model.DataObject["LifeCircleStage"].ToString());
                dynamicFormShowParameter.CustomComplexParams.Add("plm_FEntity", this.Entity);
                base.View.ShowForm(dynamicFormShowParameter, delegate (FormResult r)
                {
                    if (r.ReturnData != null)
                    {
                        object[] array = (object[])r.ReturnData;
                        string value2 = (string)this.View.Model.GetValue("FItemType", index);
                        Dictionary<ChangeObjectType, List<long>> dictionary = (Dictionary<ChangeObjectType, List<long>>)array[0];
                        List<string[]> list3 = new List<string[]>();
                        if (dictionary != null)
                        {
                            List<DynamicObject> list4 = (
                                from m in this.Entity
                                where (string)m["PARENTROWID"] == parentRowId && Convert.ToInt32(m["ObjectType"]) < 10
                                select m).ToList<DynamicObject>();
                            List<long> WLID = new List<long>();
                            foreach (DynamicObject current3 in list4)
                            {
                                //this.Entity.Remove(current3);
                                WLID.Add(Convert.ToInt64(current3["BaseObject"].ToString()));
                            }
                            foreach (KeyValuePair<ChangeObjectType, List<long>> current4 in dictionary)
                            {

                                DynamicObject[] array2 = BaseObjectManager.Instance(this.PLMContext).Load(this.PLMContext, current4.Value.Cast<object>().ToArray<object>());
                                DynamicObject[] array3 = array2;
                                for (int i = 0; i < array3.Length; i++)
                                {
                                    DynamicObject dynamicObject3 = array3[i];
                                    if (WLID.Contains(Convert.ToInt64(dynamicObject3["Id"])))
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        List<object> list5 = ECNManager.Instance.CanReturnData(this.PLMContext, Convert.ToInt64(dynamicObject3["Id"]), this.fid, this.IsECN);
                                        if (!(bool)list5[0])
                                        {
                                            ECNManager.Instance.AddErrObject(this.PLMContext, list3, dynamicObject3, list5);
                                        }
                                        else
                                        {
                                            this.Model.CreateNewEntryRow(this.ChangeObjectEntity);
                                            int entryCurrentRowIndex = this.Model.GetEntryCurrentRowIndex(this.ChangeObjectEntity);
                                            this.SetEntryValue(entryCurrentRowIndex, dynamicObject3, null, 1, current4.Key, string.Empty, string.Empty, "NotFinish,NotEffective,NotSync");
                                            if (userid == "")
                                            {
                                                this.SetExecutors(Convert.ToInt64(this.Model.DataObject["CreatorId_Id"]), entryCurrentRowIndex);
                                            }
                                            else
                                            {
                                                this.SetExecutors(Convert.ToInt64(userid), entryCurrentRowIndex);
                                                userid = "";
                                            }
                                            this.Model.SetValue("FPARENTROWID", parentRowId, entryCurrentRowIndex);
                                            this.SetVersionInfo(entryCurrentRowIndex, Convert.ToInt64(dynamicObject3["Id"]));
                                        }
                                    }
                                }
                            }
                        }
                        Dictionary<int, List<RefevancyReturnData>> dictionary2 = array[1] as Dictionary<int, List<RefevancyReturnData>>;
                        if (dictionary2 != null)
                        {
                            List<DynamicObject> list6 = (
                                from m in this.Entity
                                where (string)m["PARENTROWID"] == parentRowId && Convert.ToInt32(m["ObjectType"]) < 40 && Convert.ToInt32(m["ObjectType"]) > 20
                                select m).ToList<DynamicObject>();
                            List<long> WLID = new List<long>();
                            foreach (DynamicObject current5 in list6)
                            {
                                //this.Entity.Remove(current5);
                                WLID.Add(Convert.ToInt64(current5["BaseObject"].ToString()));
                            }
                            foreach (KeyValuePair<int, List<RefevancyReturnData>> current6 in dictionary2)
                            {
                                foreach (RefevancyReturnData current7 in current6.Value)
                                {
                                    DynamicObject dynamicObject4 = BaseObjectManager.Instance(this.PLMContext).Get(this.PLMContext, current7.MatId);
                                    if (WLID.Contains(Convert.ToInt64(dynamicObject4["Id"])))
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        this.Model.CreateNewEntryRow(this.ChangeObjectEntity);
                                        int entryCurrentRowIndex2 = this.Model.GetEntryCurrentRowIndex(this.ChangeObjectEntity);
                                        this.SetEntryValue(entryCurrentRowIndex2, current7.Id, "", current7.BillNo, "", 0L, "", 0, (int)current7.BillTypeEmun, "", "", "NotFinish,NotEffective,NotSync");
                                        this.Model.SetValue("FPARENTROWID", parentRowId, entryCurrentRowIndex2);
                                        this.Model.SetValue("FInventory", current7.Qty, entryCurrentRowIndex2);
                                        this.Model.SetValue("FERPBillCode", current7.BillNo, entryCurrentRowIndex2);
                                        this.Model.SetValue("FItemType", value2, entryCurrentRowIndex2);
                                        if (current7.BillTypeEmun == ChangeObjectType.STK_Inventory || current7.BillTypeEmun == ChangeObjectType.SAL_OUTSTOCK || current7.BillTypeEmun == ChangeObjectType.SAL_RETURNSTOCK)
                                        {
                                            this.Model.SetValue("FERPBillCode", (current7.BillTypeEmun == ChangeObjectType.STK_Inventory) ? "Inventory" : ((current7.BillTypeEmun == ChangeObjectType.SAL_OUTSTOCK) ? "OUTSTOCK" : "RETURNSTOCK"), entryCurrentRowIndex2);
                                            this.Model.SetValue("FObjectCode", dynamicObject4["Code"], entryCurrentRowIndex2);
                                            this.Model.SetValue("FBaseObject", current7.MatId, entryCurrentRowIndex2);
                                            this.Model.SetValue("FObjectName", ((ILocaleValue)dynamicObject4["Name"])[this.PLMContext.LanguageId].ToString(), entryCurrentRowIndex2);
                                        }
                                        else
                                        {
                                            this.SetExecutors(current7.ExecutorId, entryCurrentRowIndex2);
                                        }
                                    }
                                }
                            }
                        }
                        this.View.UpdateView(this.ChangeObjectEntity);
                        this.UpdateEntryRowEffectiveTime();
                        if (list3.Count > 0)
                        {
                            string text = ECNManager.Instance.ShowECNAndECRErrMessage(list3);
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                this.View.ShowErrMessage(text, "", MessageBoxType.Notice);
                            }
                        }
                    }
                });
                return;
            }
            base.View.ShowWarnningMessage(ResManager.LoadKDString("影响评估的目标对象必须是变更对象！", "120036000015313", SubSystemType.PLM, new object[0]), "", MessageBoxOptions.OK, null, MessageBoxType.Advise);
        }
        private bool _relevancyReturning;
        private void AssociateALLSS(List<int> rowIndexs, List<long> objIds)
        {
            if (rowIndexs.Count <= 0)
            {
                this.PLMView.ShowNotificationMessage(MessageBoxShowHelper.CheckedSuitableItems, "", MessageBoxType.Advise);
                return;
            }
            if (ECNManager.Instance.IsChangeObject(this.Entity[rowIndexs.First<int>()]))
            {
                long objId = objIds.First<long>();
                int index = rowIndexs.First<int>();
                List<ERPRelevancyModel> list = new List<ERPRelevancyModel>();
                int num = Convert.ToInt32(this.Model.GetValue("FItemType", index));
                if (num == 2)
                {
                    ERPRelevancyModel eRPRelevancyModel = new ERPRelevancyModel();
                    string xml = (string)this.Model.GetValue("FOperation", index);
                    eRPRelevancyModel.Detail = OperationDataManager.Instance.DeserializeOperationXML(xml).FirstOrDefault<Operation>();
                    eRPRelevancyModel.Type = OperationType.StructureChanging;
                    eRPRelevancyModel.MainDyn = objId;
                    eRPRelevancyModel.MainCategoryId = 1030000000000000000L;
                    list.Add(eRPRelevancyModel);
                }
                else
                {
                    if (num == 3)
                    {
                        ERPRelevancyModel eRPRelevancyModel2 = new ERPRelevancyModel();
                        eRPRelevancyModel2.MainDyn = 0L;
                        string xml2 = (string)this.Model.GetValue("FOperation", index);
                        eRPRelevancyModel2.Detail = OperationDataManager.Instance.DeserializeOperationXML(xml2).FirstOrDefault<Operation>();
                        eRPRelevancyModel2.Type = OperationType.BatchSubstitute;
                        eRPRelevancyModel2.MainDyn = objId;
                        eRPRelevancyModel2.MainCategoryId = 1010000000000000000L;
                        list.Add(eRPRelevancyModel2);
                    }
                    else
                    {
                        using (PLMBasketContext pLMBasketContext = new PLMBasketContext(this.PLMContext))
                        {
                            DynamicObjectCollection asType = this.Model.DataObject["ChangeObjectEntity"].GetAsType<DynamicObjectCollection>();
                            string key = (string)this.Model.GetValue("FROWID", index);
                            List<long> list2 = new List<long>();
                            if (num == 1)
                            {
                                list2 = (
                                    from q in asType
                                    where (string)q["PARENTROWID"] == key && string.IsNullOrWhiteSpace((string)q["ERPBillCode"]) && Convert.ToBoolean(q["IsSelect"])
                                    select Convert.ToInt64(q["BaseObject"])).ToList<long>();
                            }
                            else
                            {
                                list2 = (
                                    from q in asType
                                    where (string)q["PARENTROWID"] == key && string.IsNullOrWhiteSpace((string)q["ERPBillCode"])
                                    select Convert.ToInt64(q["BaseObject"])).ToList<long>();
                            }
                            list2.Add(objId);
                            DynamicObject dynamicObject = asType.FirstOrDefault((DynamicObject p) => p["BaseObject"].GetString() == objId.GetString());
                            if (CategoryContract.Instance.IsDocumentCategory(Convert.ToInt64(dynamicObject["ObjectCategory_Id"])))
                            {
                                RelatedType[] relationType = new RelatedType[]
                                {
                            RelatedType.RelatedMaterial
                                };
                                DynamicObjectCollection allRelatedObjectList = RelatedObjectModel.Instance.GetAllRelatedObjectList(this.PLMContext, objId, false, relationType, false);
                                foreach (DynamicObject current in allRelatedObjectList)
                                {
                                    if (!list2.Contains(Convert.ToInt64(current["FRELATEDOBJECT"])))
                                    {
                                        list2.Add(Convert.ToInt64(current["FRELATEDOBJECT"]));
                                    }
                                }
                            }
                            List<DynamicObject> source = DomainObjectManager.AutoLoad(pLMBasketContext, list2.ToArray());
                            List<DynamicObject> source2 = DomainObjectManager.AutoLoad(pLMBasketContext, (
                                from m in source
                                where m.Contains("IsChangeObject") && m["IsChangeObject"].ToString() == "True"
                                select m into k
                                select Convert.ToInt64(k["ChangeObjectId_Id"])).ToArray<long>());
                            foreach (long current2 in list2)
                            {
                                long newId = current2;
                                DynamicObject dynamicObject2 = (
                                    from m in source
                                    where Convert.ToInt64(m["Id"]) == newId
                                    select m).FirstOrDefault<DynamicObject>();
                                if (dynamicObject2 != null)
                                {
                                    bool flag = false;
                                    if (dynamicObject2.Contains("IsChangeObject"))
                                    {
                                        flag = (dynamicObject2["IsChangeObject"] != null && dynamicObject2["IsChangeObject"].ToString() == "True");
                                    }
                                    if (flag)
                                    {
                                        newId = Convert.ToInt64(dynamicObject2["ChangeObjectId_Id"]);
                                        dynamicObject2 = (
                                            from m in source2
                                            where Convert.ToInt64(m["Id"]) == newId
                                            select m).FirstOrDefault<DynamicObject>();
                                        if (objId == current2)
                                        {
                                            objId = newId;
                                        }
                                    }
                                    if (dynamicObject2 != null)
                                    {
                                        long num2 = Convert.ToInt64(dynamicObject2["CategoryId_id"]);
                                        if ((!CategoryContract.Instance.IsBOMCategory(num2) || !dynamicObject2.Contains("MainRelation_id") || !list2.Contains(dynamicObject2.GetDynamicObjectItemValue("MainRelation_id", 0L))) && (CategoryContract.Instance.IsMaterialCategory(num2) || CategoryContract.Instance.IsBOMCategory(num2)))
                                        {
                                            list.Add(new ERPRelevancyModel
                                            {
                                                Type = OperationType.PropertyChanging,
                                                MainDyn = newId,
                                                MainCategoryId = num2
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                string parentRowId = Convert.ToString(base.View.Model.GetValue("FROWID", index));
                List<long> bomExsitsList = this.GetBomExsitsList(index, parentRowId);
                List<string> value = (
                    from m in this.Entity
                    where (string)m["PARENTROWID"] == parentRowId
                    select m["BaseObject"].GetString()).ToList<string>();
                DynamicFormShowParameter dynamicFormShowParameter = new DynamicFormShowParameter();
                dynamicFormShowParameter.PageId = PLMGuid.NewGuidString().ToString();
                dynamicFormShowParameter.OpenStyle.ShowType = ShowType.Modal;
                dynamicFormShowParameter.FormId = "PLM_STD_EC_REFEVANCY_ALL";
                dynamicFormShowParameter.Caption = ResManager.LoadKDString("影响评估", "120036000015320", SubSystemType.PLM, new object[0]);
                dynamicFormShowParameter.CustomComplexParams.Add("plm_objectid", objId);
                dynamicFormShowParameter.CustomComplexParams.Add("plm_relevancyModels", list);
                dynamicFormShowParameter.CustomComplexParams.Add("plm_selRelevancyObjIdList", value);
                dynamicFormShowParameter.CustomComplexParams.Add("plm_ECNLIFECIRCLE", this.Model.DataObject["LifeCircleStage"].ToString());
                dynamicFormShowParameter.CustomComplexParams.Add("plm_FEntity", this.Entity);
                dynamicFormShowParameter.CustomComplexParams.Add("plm_ExistsBomList", bomExsitsList);
                dynamicFormShowParameter.CustomComplexParams.Add("plm_EcnId", Convert.ToInt64(base.View.Model.DataObject["Id"]));
                dynamicFormShowParameter.CustomComplexParams.Add("plm_IsEcn", this.IsECN);
                base.View.ShowForm(dynamicFormShowParameter, delegate (FormResult r)
                {
                    if (r.ReturnData != null)
                    {
                        Action<Action<int>> act = delegate (Action<int> m)
                        {
                            this._relevancyReturning = true;
                            Action<int> aProgressAction = PageManager.Instance.GetAProgressAction(m);
                            object[] array = (object[])r.ReturnData;
                            string value2 = (string)this.View.Model.GetValue("FItemType", index);
                            Dictionary<ChangeObjectType, List<long>> dictionary = (Dictionary<ChangeObjectType, List<long>>)array[0];
                            List<string[]> list3 = new List<string[]>();
                            using (PLMBasketContext pLMBasketContext2 = new PLMBasketContext(this.PLMContext))
                            {
                                Dictionary<int, List<RefevancyReturnData>> dictionary2 = array[1] as Dictionary<int, List<RefevancyReturnData>>;
                                List<long> list4 = new List<long>();
                                List<long> list5 = new List<long>();
                                if (dictionary2 != null)
                                {
                                    foreach (KeyValuePair<int, List<RefevancyReturnData>> current3 in dictionary2)
                                    {
                                        foreach (RefevancyReturnData current4 in current3.Value)
                                        {
                                            list4.Add(current4.MatId);
                                            if (current4.ExecutorId != 0L)
                                            {
                                                list5.Add(current4.ExecutorId);
                                            }
                                        }
                                    }
                                }
                                DataTable dt = ECNModel.Instance.BuildExecutorsDataTableColumn();
                                int num3 = list4.Count;
                                int num4 = 0;
                                if (dictionary != null)
                                {
                                    List<DynamicObject> list6 = (
                                        from c in this.Entity
                                        where (string)c["PARENTROWID"] == parentRowId && Convert.ToInt32(c["ObjectType"]) < 10
                                        select c).ToList<DynamicObject>();
                                    List<long> WLID = new List<long>();
                                    foreach (DynamicObject current5 in list6)
                                    {
                                        //this.Entity.Remove(current5);
                                        WLID.Add(Convert.ToInt64(current5["BaseObject"].ToString()));
                                    }
                                    if (WLID.Count > 0)
                                    {
                                        foreach (KeyValuePair<ChangeObjectType, List<long>> current6 in dictionary)
                                        {
                                            foreach (var id in WLID)
                                            {
                                                current6.Value.Remove(id);
                                            }
                                        }
                                    }
                                }
                                if (dictionary != null && dictionary.Count > 0)
                                {
                                    List<long> allChangeChangeObjectIds = this.GetAllChangeChangeObjectIdss(dictionary);
                                    Dictionary<long, DynamicObject> lastetVersionBatch = VersionManager.Instance.GetLastetVersionBatch(this.PLMContext, allChangeChangeObjectIds);
                                    num3 += allChangeChangeObjectIds.Count;
                                    DynamicObject[] array2 = BaseObjectManager.Instance(this.PLMContext).Load(this.PLMContext, allChangeChangeObjectIds.Cast<object>().ToArray<object>());
                                    ECNManager.Instance.SetCacheCanReturnData(pLMBasketContext2, this.fid, array2.ToList<DynamicObject>());
                                    this.Model.BatchCreateNewEntryRow(this.ChangeObjectEntity, allChangeChangeObjectIds.Count);
                                    int num5 = this.Model.GetEntryCurrentRowIndex(this.ChangeObjectEntity);
                                    long[] array3 = PLMDBUtils.Instance.GetSequenceInt64(this.PLMContext, "T_PLM_STD_EC_ITEM", allChangeChangeObjectIds.Count).ToArray<long>();
                                    long[] array4 = PLMDBUtils.Instance.GetSequenceInt64(this.PLMContext, "T_PLM_STD_EC_ASSIGN", allChangeChangeObjectIds.Count).ToArray<long>();
                                    int num6 = 0;
                                    this.View.Model.BeginIniti();
                                    foreach (KeyValuePair<ChangeObjectType, List<long>> current6 in dictionary)
                                    {
                                        using (List<long>.Enumerator enumerator7 = current6.Value.GetEnumerator())
                                        {
                                            while (enumerator7.MoveNext())
                                            {
                                                long id = enumerator7.Current;
                                                if (aProgressAction != null)
                                                {
                                                    aProgressAction(++num4 * 100 / num3);
                                                }
                                                if (array2 != null)
                                                {
                                                    DynamicObject dynamicObject3 = array2.First((DynamicObject c) => Convert.ToInt64(c["Id"]) == id);
                                                    if (dynamicObject3 != null)
                                                    {
                                                        List<object> list7 = ECNManager.Instance.CanReturnData(pLMBasketContext2, id, this.fid, this.IsECN);
                                                        if (!(bool)list7[0])
                                                        {
                                                            ECNManager.Instance.AddErrObject(this.PLMContext, list3, dynamicObject3, list7);
                                                        }
                                                        else
                                                        {
                                                            this.SetEntryValue(num5, dynamicObject3, null, 1, current6.Key, string.Empty, string.Empty, "NotFinish,NotEffective,NotSync");
                                                            if (dynamicObject3["Code"].ToString().Substring(0, 3) == "DOC")
                                                            {
                                                                this.SetExecutorsDOC(dynamicObject3.GetDynamicObjectItemValue("CreatorID_Id", 0L), num5, array4[num6], array3[num6], dt, dynamicObject3["CreatorId"] as DynamicObject);
                                                            }
                                                            else if (dynamicObject3["CategoryID"] != null && ((DynamicObject)dynamicObject3["CategoryID"])["Code"].ToString() == "PBOM")
                                                            {
                                                                this.SetExecutorsBOM(dynamicObject3.GetDynamicObjectItemValue("CreatorID_Id", 0L), num5, array4[num6], array3[num6], dt, dynamicObject3["CreatorId"] as DynamicObject);
                                                            }
                                                            else
                                                            {
                                                                this.SetExecutors(dynamicObject3.GetDynamicObjectItemValue("CreatorID_Id", 0L), num5, array4[num6], array3[num6], dt, dynamicObject3["CreatorId"] as DynamicObject);
                                                            }
                                                            this.Model.SetValue("FPARENTROWID", parentRowId, num5);
                                                            this.SetRefVersionId(num5, lastetVersionBatch, id);
                                                            num5++;
                                                            num6++;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    this.View.Model.EndIniti();
                                }
                                if (dictionary2 != null)
                                {
                                    List<DynamicObject> list8 = (
                                        from c in this.Entity
                                        where (string)c["PARENTROWID"] == parentRowId && Convert.ToInt32(c["ObjectType"]) < 40 && Convert.ToInt32(c["ObjectType"]) > 20
                                        select c).ToList<DynamicObject>();

                                    foreach (DynamicObject current7 in list8)
                                    {
                                        this.Entity.Remove(current7);
                                        //WLID.Add(Convert.ToInt64(current7["BaseObject"].ToString()));
                                    }

                                    DynamicObject[] array5 = null;
                                    if (list5.Count != 0)
                                    {
                                        array5 = UserManager.Instance.Load(this.PLMContext, list5.Cast<object>().ToArray<object>());
                                    }
                                    if (list4.Count != 0)
                                    {
                                        long[] array6 = PLMDBUtils.Instance.GetSequenceInt64(this.PLMContext, "T_PLM_STD_EC_ITEM", list4.Count).ToArray<long>();
                                        long[] array7 = PLMDBUtils.Instance.GetSequenceInt64(this.PLMContext, "T_PLM_STD_EC_ASSIGN", list4.Count).ToArray<long>();
                                        int num7 = 0;
                                        this.Model.BatchCreateNewEntryRow(this.ChangeObjectEntity, list4.Count);
                                        int num8 = this.Model.GetEntryCurrentRowIndex(this.ChangeObjectEntity);
                                        DynamicObject[] array8 = BaseObjectManager.Instance(this.PLMContext).Load(this.PLMContext, list4.Cast<object>().ToArray<object>());
                                        foreach (KeyValuePair<int, List<RefevancyReturnData>> current8 in dictionary2)
                                        {
                                            using (List<RefevancyReturnData>.Enumerator enumerator10 = current8.Value.GetEnumerator())
                                            {
                                                while (enumerator10.MoveNext())
                                                {
                                                    RefevancyReturnData data = enumerator10.Current;
                                                    if (aProgressAction != null)
                                                    {
                                                        aProgressAction(++num4 * 100 / num3);
                                                    }
                                                    if (array8 != null)
                                                    {
                                                        DynamicObject dynamicObject4 = array8.First((DynamicObject c) => Convert.ToInt64(c["Id"]) == data.MatId);
                                                        if (dynamicObject4 != null)
                                                        {
                                                            this.SetEntryValue(num8, data.Id, "", data.BillNo, "", 0L, "", 0, (int)data.BillTypeEmun, "", "", "NotFinish,NotEffective,NotSync", null);
                                                            this.Model.SetValue("FPARENTROWID", parentRowId, num8);
                                                            this.Model.SetValue("FInventory", data.Qty, num8);
                                                            this.Model.SetValue("FERPBillCode", data.BillNo, num8);
                                                            this.Model.SetValue("FItemType", value2, num8);
                                                            if (data.BillTypeEmun == ChangeObjectType.STK_Inventory || data.BillTypeEmun == ChangeObjectType.SAL_OUTSTOCK || data.BillTypeEmun == ChangeObjectType.SAL_RETURNSTOCK)
                                                            {
                                                                this.Model.SetValue("FERPBillCode", (data.BillTypeEmun == ChangeObjectType.STK_Inventory) ? "Inventory" : ((data.BillTypeEmun == ChangeObjectType.SAL_OUTSTOCK) ? "OUTSTOCK" : "RETURNSTOCK"), num8);
                                                                this.Model.SetValue("FObjectCode", dynamicObject4["Code"], num8);
                                                                this.Model.SetValue("FBaseObject", data.MatId, num8);
                                                                this.Model.SetValue("FObjectName", ((ILocaleValue)dynamicObject4["Name"])[this.PLMContext.LanguageId].ToString(), num8);
                                                            }
                                                            else
                                                            {
                                                                if (array5 != null)
                                                                {
                                                                    DynamicObject user = array5.FirstOrDefault((DynamicObject c) => Convert.ToInt64(c["Id"]) == data.ExecutorId);
                                                                    this.SetExecutors(data.ExecutorId, num8, array7[num7], array6[num7], dt, user);
                                                                }
                                                            }
                                                            num8++;
                                                            num7++;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                ECNModel.Instance.BatchSaveExecutorsData(this.PLMContext, dt);
                            }
                            this.ClearNullRowData();
                            this.View.UpdateView(this.ChangeObjectEntity);
                            this.UpdateEntryRowEffectiveTime();
                            this.SetToolTips();
                            if (list3.Count > 0)
                            {
                                string text = ECNManager.Instance.ShowECNAndECRErrMessage(list3);
                                if (!string.IsNullOrWhiteSpace(text))
                                {
                                    this.View.ShowErrMessage(text, "", MessageBoxType.Notice);
                                }
                            }
                            this._relevancyReturning = false;
                        };
                        PageManager.Instance.RunAsyncTaskWithProgress(this.PLMView, act, "数据加载中...", null, "", null, null);
                    }
                });
                return;
            }
            base.View.ShowWarnningMessage(ResManager.LoadKDString("影响评估的目标对象必须是变更对象！", "120036000015313", SubSystemType.PLM, new object[0]), "", MessageBoxOptions.OK, null, MessageBoxType.Advise);
        }
        private void SetEntryValue(int index, DynamicObject bom, DynamicObject oldobj, int itemType, ChangeObjectType objectType, string description, string operationXml, string stateIconName = "NotFinish,NotEffective,NotSync")
        {
            if (!ECNManager.Instance.IsPLMObjectOperation(objectType))
            {
                return;
            }
            long id = Convert.ToInt64(bom["Id"]);
            string code = string.Format("{0}{1}", (bom["Code"] as string) ?? "", (itemType == 3) ? "..." : "");
            string name = string.Format("{0}{1}", ((ILocaleValue)bom["Name"])[this.PLMContext.LanguageId].GetString(), (itemType == 3) ? "..." : "");
            long categoryId = Convert.ToInt64(bom["CategoryID_Id"]);
            string verNo = string.Format("{0}{1}", (oldobj == null) ? ((bom["VerNo"] as string) ?? "") : ((oldobj["VerNo"] as string) ?? ""), (itemType == 3) ? "..." : "");
            string icon = (bom["Icon"] as string) ?? "";
            this.SetEntryValue(index, id, icon, code, name, categoryId, verNo, itemType, Convert.ToInt32(objectType), description, operationXml, stateIconName);
        }
        private void SetEntryValue(int index, long id, string icon, string code, string name, long categoryId, string verNo, int itemType = 0, int objectType = 0, string description = "", string operationXml = "", string stateIconName = "NotFinish,NotEffective,NotSync", DynamicObject obj = null)
        {
            this.Model.SetValue("FBaseObject", id, index);
            this.Model.SetValue("FEntryIcon", SmallSysIcon.Instance.Get(this.PLMContext, icon, true, true, false, OrganizationShareManager.Instance.IsShare(this.PLMContext, id, -1L), "", base.View), index);
            this.Model.SetValue("FStateIcon", ECStateIconManager.Instance.GetStateIcon(this.PLMContext, stateIconName, base.View), index);
            this.Model.SetValue("FObjectCode", code, index);
            this.Model.SetValue("FObjectName", name, index);
            this.Model.SetValue("FObjectCategory", categoryId, index);
            this.Model.SetValue("FOperation", operationXml, index);
            this.Model.SetValue("FObjectVerNo", verNo, index);
            if (itemType != 0)
            {
                this.Model.SetValue("FItemType", itemType, index);
            }
            if (objectType != 0)
            {
                this.Model.SetValue("FObjectType", objectType, index);
            }
            this.SetEntryRowEffectiveTime(index);
            string stateIconTooltip = ECStateIconManager.Instance.GetStateIconTooltip(stateIconName);
            TooltipEntity tip = new TooltipEntity
            {
                E = true,
                T = stateIconTooltip
            };
            this.grds.SetTooltipEnabled("FStateIcon", true);
            this.grds.SetCellTooltip("FStateIcon", tip, index);
            this.grds.SetTooltipEnabled("FEntryIcon", true);
            if (obj != null)
            {
                List<DynamicObject> cache = PLMCacheUtil.GetCache<List<DynamicObject>>(this.PLMContext, "_lifeStageList", () => LifeCycleTemplateModel.Instance.GetLifecycleStageNameList(this.PLMContext).ToList<DynamicObject>(), new TimeSpan(0, 0, 30, 0), PLMCacheRegionType.PLM_Common, false);
                string t = IconKeyStructure.FromTooltipsString(obj["ICON"].GetString(), cache, Convert.ToInt64(obj["CategoryId_id"]), this.langIds);
                TooltipEntity tip2 = new TooltipEntity
                {
                    E = true,
                    T = t
                };
                this.grds.SetCellTooltip("FEntryIcon", tip2, index);
            }
        }

        private void SetEntryValue(int index, long id, string icon, string code, string name, long categoryId, string verNo, int itemType = 0, int objectType = 0, string description = "", string operationXml = "", string stateIconName = "NotFinish,NotEffective,NotSync")
        {
            this.Model.SetValue("FBaseObject", id, index);
            this.Model.SetValue("FEntryIcon", SmallSysIcon.Instance.Get(this.PLMContext, icon, true, true, false, OrganizationShareManager.Instance.IsShare(this.PLMContext, id, 0L), "", base.View), index);
            this.Model.SetValue("FStateIcon", ECStateIconManager.Instance.GetStateIcon(this.PLMContext, stateIconName, base.View), index);
            this.Model.SetValue("FObjectCode", code, index);
            this.Model.SetValue("FObjectName", name, index);
            this.Model.SetValue("FObjectCategory", categoryId, index);
            this.Model.SetValue("FOperation", operationXml, index);
            this.Model.SetValue("FObjectVerNo", verNo, index);
            if (itemType != 0)
            {
                this.Model.SetValue("FItemType", itemType, index);
            }
            if (objectType != 0)
            {
                this.Model.SetValue("FObjectType", objectType, index);
            }
            this.SetEntryRowEffectiveTime(index);
            string stateIconTooltip = ECStateIconManager.Instance.GetStateIconTooltip(stateIconName);
            TooltipEntity tip = new TooltipEntity
            {
                E = true,
                T = stateIconTooltip
            };
            this.grds.SetTooltipEnabled("FStateIcon", true);
            this.grds.SetCellTooltip("FStateIcon", tip, index);
            this.grds.SetTooltipEnabled("FEntryIcon", true);
            this.grds.SetCellTooltip("FEntryIcon", tip, index);
        }
        private void SetVersionInfo(int index, long id)
        {
            DynamicObject latestVersion = VersionManager.Instance.GetLatestVersion(this.PLMContext, id);
            if (latestVersion != null)
            {
                long num = Convert.ToInt64(latestVersion["FVERSIONID"]);
                DynamicObject dynamicObject = GlobalVersionManager.BaseInstance(this.PLMContext).Get(this.PLMContext, num);
                if (dynamicObject != null)
                {
                    this.Model.SetValue("FRefVersionId", dynamicObject["Id"], index);
                }
            }
        }
        private void UpdateEntryRowEffectiveTime()
        {
            for (int i = 0; i < this.Entity.Count; i++)
            {
                this.SetEntryRowEffectiveTime(i);
            }
        }
        private void SetEntryRowEffectiveTime(int rowIndex)
        {
            string[] source = new string[]
            {
        "AA",
        "AJ",
        "AL"
            };
            if (!this.IsECN || rowIndex >= this.Entity.Count || rowIndex < 0)
            {
                return;
            }
            DynamicObject dynamicObject = this.Entity[rowIndex];
            if (string.IsNullOrWhiteSpace(dynamicObject["SyncErpMode"] as string))
            {
                base.View.GetFieldEditor("FSyncErpMode", rowIndex).SetValue("0");
            }
            OperationType @int = (OperationType)StringIntegerConverter.GetInt32(dynamicObject["ItemType"], 0);
            long categoryId = Convert.ToInt64(dynamicObject["ObjectCategory_Id"]);
            bool flag = source.Contains(this.Model.DataObject["LifeCircleStage"].ToString());
            bool flag2 = @int == OperationType.BatchSubstitute || @int == OperationType.StructureChanging;
            if (@int == OperationType.PropertyChanging && CategoryManager.Instance.IsCategoryOf(categoryId, StandardCategoryType.BOM))
            {
                flag2 = true;
            }
            if (flag2 && flag && ECNManager.Instance.IsPLMEcnOperation(dynamicObject))
            {
                object obj = dynamicObject["SyncErpMode"];
                FieldEditor fieldEditor = base.View.GetFieldEditor("FSyncErpTime", rowIndex);
                fieldEditor.Enabled = (obj as string == "1");
                if (!fieldEditor.Enabled)
                {
                    fieldEditor.SetValue(null);
                    return;
                }
            }
            else
            {
                FieldEditor fieldEditor2 = base.View.GetFieldEditor("FSyncErpMode", rowIndex);
                fieldEditor2.Enabled = false;
                base.View.GetFieldEditor("FSyncErpTime", rowIndex).Enabled = false;
                if (!CategoryManager.Instance.IsCategoryOf(categoryId, 1010000000000000000L) && !CategoryManager.Instance.IsCategoryOf(categoryId, 1030000000000000000L))
                {
                    dynamicObject["SyncErpMode"] = null;
                    fieldEditor2.SetValue("");
                }
            }
        }
        private void create()
        {
            string NUMBER = this.Model.GetValue("FCODE").ToString();
            string FNUMBER = "";
            var dates = this.Model.DataObject["ChangeObjectEntity"] as DynamicObjectCollection;
            foreach (var date in dates)
            {
                if ((date["PARENTROWID"] == null || date["PARENTROWID"] == null ? true : Convert.ToString(date["PARENTROWID"]) == " " ? true : false) && Convert.ToBoolean(date["IsSelect"]) == true)
                {
                    FNUMBER = date["ObjectCode"].ToString();
                }
            }
            if (FNUMBER == "")
            {
                throw new KDBusinessException("", "请选择主物料！！！");
            }
            String DJLX = "STK_StockConvert";
            string pageId = Guid.NewGuid().ToString();
            BillShowParameter showParameter = new BillShowParameter();
            showParameter.FormId = DJLX;
            showParameter.OpenStyle.ShowType = ShowType.MainNewTabPage;
            showParameter.PageId = pageId;
            showParameter.CustomParams.Add("NUMBER", NUMBER);
            showParameter.CustomParams.Add("FNUMBER", FNUMBER);
            this.View.ShowForm(showParameter);
        }
        public override void EntryButtonCellClick(EntryButtonCellClickEventArgs e)
        {
            base.EntryButtonCellClick(e);
            if (!e.FieldKey.EqualsIgnoreCase("F_260_KCZHD"))
            {
                return;
            }
            if (e.Row < 0)
            {
                return;
            }
            var formId = "STK_StockConvert";
            var requisitionMetadata = (FormMetadata)MetaDataServiceHelper.Load(this.Context, formId);
            var billNo = this.Model.GetValue(e.FieldKey, e.Row);
            var objs = BusinessDataServiceHelper.Load(this.Context, requisitionMetadata.BusinessInfo,
                new List<SelectorItemInfo>(new[] { new SelectorItemInfo("FID") }), OQLFilter.CreateHeadEntityFilter("FBillNo='" + billNo + "'"));
            if (objs == null || objs.Length == 0) { return; }
            var pkId = objs[0]["Id"].ToString(); var showParameter = new BillShowParameter
            {
                FormId = formId, // 业务对象标识              
                PKey = pkId, // 单据内码                
                Status = OperationStatus.VIEW // 查看模式打开                
                // Status = OperationStatus.EDIT// 编辑模式打开            
            };
            this.View.ShowForm(showParameter);
        }
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (this._relevancyReturning)
            {
                return;
            }
            base.DataChanged(e);
            if (e.Field.Key.ToUpper() == "FSYNCERPMODE")
            {
                this.SetEntryRowEffectiveTime(e.Row);
            }
            if (e.Field.Key == "FObjectType" && e.NewValue.ToString() == "4")
            {
                string bm = this.Model.GetValue("FOBJECTCODE", e.Row).ToString();
                string x = bm.Substring(0, 3);
                if (bm.Substring(0, 3) == "DOC")
                {
                    string cxsql = $@"select a.FCREATORID,FNAME from T_PLM_PDM_BASE a
                                      inner join T_SEC_user b on a.FCREATORID=b.FUserID 
                                      where a.FCODE='{bm}'";
                    var cx = DBUtils.ExecuteDynamicObject(Context, cxsql);
                    if (cx.Count > 0)
                    {
                        string ygsql = $@"select * from T_SEC_user where FUserID = '{cx[0]["FCREATORID"]}' and FFORBIDSTATUS = 'A'";
                        var yg = DBUtils.ExecuteDynamicObject(Context, ygsql);
                        if (yg.Count > 0)
                        {
                            userid = cx[0]["FCREATORID"].ToString();
                        }
                    }
                }
            }
            else if (e.Field.Key == "FObjectType" && e.NewValue.ToString() == "5")
            {
                userid = "1296611";
            }
            else if (e.Field.Key == "FAssignText")
            {

            }
            else
            {
            }
        }

        private void AddDesignChangeObject()
        {
            Dictionary<string, object> param = new Dictionary<string, object>();
            long category = 1000000000000000000L;
            List<long> exsitsList = this.GetExsitsList(1);
            List<long> bomExsitsList = this.GetBomExsitsList(-1, null);
            exsitsList.AddRange(bomExsitsList);
            exsitsList = exsitsList.Distinct<long>().ToList<long>();
            string text = "FISCHANGE='0' AND FISFLOW=0 AND FISCHECKOUT=0";
            IECObjectEntityBillEx iECObjectEntityBillEx = PLMExtensionFactory.Create<IECObjectEntityBillEx>(true);
            text += iECObjectEntityBillEx.AddDesignChangeObject();
            this.AddMultiOrgFilter(this.PLMContext, ref text);
            text += (exsitsList.Any<long>() ? string.Format(" AND FID NOT IN ({0})", string.Join<long>(",", exsitsList)) : string.Empty);
            BomFormView.Instance.OpenLibarayObjectForm(this, "", text, delegate (FormResult r)
            {
                if (r.ReturnData != null)
                {
                    List<string[]> errObjects = new List<string[]>();
                    ListSelectedRowCollection listData = (ListSelectedRowCollection)r.ReturnData;
                    if (listData != null)
                    {
                        Action<Action<int>> act = delegate (Action<int> m)
                        {
                            Action<int> aProgressAction = PageManager.Instance.GetAProgressAction(m);
                            List<string> list = (
                                from p in listData
                                select p.PrimaryKeyValue).ToList<string>();
                            if (list == null || list.Count == 0)
                            {
                                return;
                            }
                            Dictionary<long, DynamicObject> lastetVersionBatch = VersionManager.Instance.GetLastetVersionBatch(this.PLMContext, (
                                from c in list
                                select Convert.ToInt64(c)).ToList<long>());
                            List<DynamicObject> list2 = BaseObjectManager.Instance(this.PLMContext).Load(this.PLMContext, list.ToArray<string>()).ToList<DynamicObject>();
                            IECObjectEntityManagerEx iECObjectEntityManagerEx = PLMExtensionFactory.Create<IECObjectEntityManagerEx>(true);
                            OperateResultCollection operateResultCollection = new OperateResultCollection();
                            List<DynamicObject> relatedChangeObjects = iECObjectEntityManagerEx.GetRelatedChangeObjects(this.PLMContext, list2, list, exsitsList, ref operateResultCollection);
                            if (relatedChangeObjects != null && relatedChangeObjects.Any<DynamicObject>())
                            {
                                list2 = relatedChangeObjects;
                            }
                            using (PLMBasketContext pLMBasketContext = new PLMBasketContext(this.PLMContext))
                            {
                                ECNManager.Instance.SetCacheCanReturnData(pLMBasketContext, this.fid, list2);
                                int num = 0;
                                int count = list2.Count;
                                int num2 = 0;
                                long[] array = PLMDBUtils.Instance.GetSequenceInt64(this.PLMContext, "T_PLM_STD_EC_ITEM", count).ToArray<long>();
                                long[] array2 = PLMDBUtils.Instance.GetSequenceInt64(this.PLMContext, "T_PLM_STD_EC_ASSIGN", count).ToArray<long>();
                                DataTable dt = ECNModel.Instance.BuildExecutorsDataTableColumn();
                                this.Model.BatchCreateNewEntryRow(this.ChangeObjectEntity, count);
                                int num3 = this.Model.GetEntryCurrentRowIndex(this.ChangeObjectEntity);
                                this.View.Model.BeginIniti();
                                foreach (DynamicObject current in list2)
                                {
                                    if (aProgressAction != null)
                                    {
                                        aProgressAction(++num * 100 / count);
                                    }
                                    long num4 = Convert.ToInt64(current["Id"]);
                                    if (num4 != 0L)
                                    {
                                        List<object> list3 = ECNManager.Instance.CanReturnData(pLMBasketContext, num4, this.fid, this.IsECN);
                                        if (!(bool)list3[0])
                                        {
                                            ECNManager.Instance.AddErrObject(this.PLMContext, errObjects, current, list3);
                                        }
                                        else
                                        {
                                            this.SetExecutorss(current.GetDynamicObjectItemValue("CreatorId_Id", 0L), num3, array2[num2], array[num2], dt, current["CreatorId"] as DynamicObject);
                                            this.SetEntryValue(num3, current, null, 1, ChangeObjectType.ChangeItem, string.Empty, string.Empty, "NotFinish,NotEffective,NotSync");
                                            this.SetRefVersionId(num3, lastetVersionBatch, num4);
                                            num3++;
                                            num2++;
                                        }
                                    }
                                }
                                ECNModel.Instance.BatchSaveExecutorsData(this.PLMContext, dt);
                                this.View.Model.EndIniti();
                            }
                            this.ClearNullRowData();
                            this.View.UpdateView(this.ChangeObjectEntity);
                            this.UpdateEntryRowEffectiveTime();
                            this.SetToolTips();
                            if (errObjects.Count > 0)
                            {
                                string text2 = ECNManager.Instance.ShowECNAndECRErrMessage(errObjects);
                                if (!string.IsNullOrWhiteSpace(text2))
                                {
                                    this.View.ShowErrMessage(text2, "", MessageBoxType.Notice);
                                }
                            }
                        };
                        PageManager.Instance.RunAsyncTaskWithProgress(this.PLMView, act, "数据加载中...", null, "", null, null);
                    }
                }
            }, category, false, false, true, "EChange", true, this._showSouceMultiCates, false, param, false, false, null);
        }
        private void AddMultiOrgFilter(PLMContext ctx, ref string filter)
        {
            if (!ctx.IsRDMultiOrg)
            {
                return;
            }
            string text = string.Format(" FCreateOrgId = {0} ", ctx.OrganizationInfo.ID);
            if (!string.IsNullOrEmpty(filter))
            {
                filter = filter + " AND " + text;
                return;
            }
            filter = text;
        }
        private void SetRefVersionId(int index, Dictionary<long, DynamicObject> versionObjMap, long baseId)
        {
            if (versionObjMap != null)
            {
                DynamicObject dynamicObject = null;
                versionObjMap.TryGetValue(baseId, out dynamicObject);
                if (dynamicObject != null)
                {
                    this.Model.SetValue("FRefVersionId", dynamicObject["FVERSIONID"], index);
                }
            }
        }
        private void ClearNullRowData()
        {
            DynamicObjectCollection dynamicObjectCollection = (DynamicObjectCollection)this.Model.DataObject["ChangeObjectEntity"];
            List<DynamicObject> list = (
                from c in dynamicObjectCollection
                where string.IsNullOrWhiteSpace(c["BaseObject"].GetString()) || c["BaseObject"].GetString() == "0"
                select c).ToList<DynamicObject>();
            for (int i = 0; i < list.Count; i++)
            {
                dynamicObjectCollection.Remove(list[i]);
            }
        }
        private void SetToolTips()
        {
            long categoryId = Convert.ToInt64(this.Model.DataObject["CategoryId_Id"]);
            DynamicObjectCollection asType = this.Model.DataObject["ChangeObjectEntity"].GetAsType<DynamicObjectCollection>();
            List<DynamicObject> cache = PLMCacheUtil.GetCache<List<DynamicObject>>(this.PLMContext, "_lifeStageList", () => LifeCycleTemplateModel.Instance.GetLifecycleStageNameList(this.PLMContext).ToList<DynamicObject>(), new TimeSpan(0, 0, 30, 0), PLMCacheRegionType.PLM_Common, false);
            this.langIds = this.PLMContext.LanguageId.ToString();
            List<long> fids = (
                from p in asType
                select Convert.ToInt64(p["BaseObject"])).Distinct<long>().ToList<long>();
            DynamicObjectCollection iconByCode = BaseObjectModel.Instance.GetIconByCode(this.PLMContext, fids);
            for (int i = 0; i < asType.Count; i++)
            {
                DynamicObject dynamicObject = asType[i];
                long objectId = Convert.ToInt64(dynamicObject["BaseObject"]);
                DynamicObject dynamicObject2 = (iconByCode == null) ? null : iconByCode.FirstOrDefault((DynamicObject p) => Convert.ToInt64(p["FID"]) == objectId);
                if (dynamicObject2 != null)
                {
                    string stateIconName = CategoryContract.Instance.IsECNCategory(categoryId) ? ECStateIconManager.Instance.GetChangingObjStateIcon(dynamicObject) : string.Empty;
                    string stateIconTooltip = ECStateIconManager.Instance.GetStateIconTooltip(stateIconName);
                    TooltipEntity tip = new TooltipEntity
                    {
                        E = true,
                        T = stateIconTooltip
                    };
                    this.grds.SetCellTooltip("FStateIcon", tip, i);
                    string t = IconKeyStructure.FromTooltipsString(dynamicObject2["FICON"].GetString(), cache, Convert.ToInt64(dynamicObject2["FCategoryId"]), this.langIds);
                    TooltipEntity tip2 = new TooltipEntity
                    {
                        E = true,
                        T = t
                    };
                    this.grds.SetCellTooltip("FEntryIcon", tip2, i);
                    object value = this.Model.GetValue("FItemType", i);
                    if (Convert.ToInt16(value) != 2 && Convert.ToInt16(value) != 3 && (Convert.ToInt16(value) != 1 || !CategoryContract.Instance.IsBOMCategory(Convert.ToInt64(this.Entity[i]["ObjectCategory_Id"]))))
                    {
                        base.View.GetFieldEditor("FModifyReport", i).Enabled = false;
                        TooltipEntity tip3 = new TooltipEntity
                        {
                            E = true,
                            T = "非Bom对象的设计变更暂不支持查看修改报告"
                        };
                        this.grds.SetCellTooltip("FModifyReport", tip3, i);
                    }
                }
            }
        }
        public void SetExecutorss(long userId, int index, long pkId, long entryId, DataTable dt, DynamicObject user)
        {
            this.Entity[index]["Id"] = entryId;
            if (user != null)
            {
                string ygsql = $@"select * from T_SEC_user where FUserID = '{user["Id"]}' and FFORBIDSTATUS = 'A'";
                var yg = DBUtils.ExecuteDynamicObject(Context, ygsql);
                if (yg.Count == 0 && this.Model.GetValue("FCHARGEUSERID") != null)
                {
                    ECNModel.Instance.BuildExecutorsData(pkId, entryId, Convert.ToInt64(((DynamicObject)this.Model.GetValue("FCHARGEUSERID"))["Id"]), dt);
                    this.PLMView.CurrentView.Model.SetValue("FAssignText", ((DynamicObject)this.Model.GetValue("FCHARGEUSERID"))["Name"].ToString(), index);
                }
                else
                {
                    ECNModel.Instance.BuildExecutorsData(pkId, entryId, userId, dt);
                    this.PLMView.CurrentView.Model.SetValue("FAssignText", user["Name"], index);
                }

            }
        }
        public void SetExecutorsDOC(long userId, int index, long pkId, long entryId, DataTable dt, DynamicObject user)
        {
            this.Entity[index]["Id"] = entryId;
            if (user != null)
            {
                string ygsql = $@"select * from T_SEC_user where FUserID = '{user["Id"]}' and FFORBIDSTATUS = 'A'";
                var yg = DBUtils.ExecuteDynamicObject(Context, ygsql);
                if (yg.Count == 0 && this.Model.GetValue("FCREATORID") != null)
                {
                    ECNModel.Instance.BuildExecutorsData(pkId, entryId, Convert.ToInt64(((DynamicObject)this.Model.GetValue("FCREATORID"))["Id"]), dt);
                    this.PLMView.CurrentView.Model.SetValue("FAssignText", ((DynamicObject)this.Model.GetValue("FCREATORID"))["Name"].ToString(), index);
                }
                else
                {
                    ECNModel.Instance.BuildExecutorsData(pkId, entryId, userId, dt);
                    this.PLMView.CurrentView.Model.SetValue("FAssignText", user["Name"], index);
                }

            }
        }
        public void SetExecutorsBOM(long userId, int index, long pkId, long entryId, DataTable dt, DynamicObject user)
        {
            this.Entity[index]["Id"] = entryId;
            if (user != null)
            {
                ECNModel.Instance.BuildExecutorsData(pkId, entryId, 1296611, dt);
                this.PLMView.CurrentView.Model.SetValue("FAssignText", "邓灵萍", index);
            }
        }
        protected List<long> GetAllChangeChangeObjectIdss(Dictionary<ChangeObjectType, List<long>> allChangeObjectDic)
        {
            List<long> list = new List<long>();
            foreach (KeyValuePair<ChangeObjectType, List<long>> current in allChangeObjectDic)
            {
                list.AddRange(current.Value);
            }
            return list;
        }
    }
}
