
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

namespace HMSX.Second.Plugin.PLM
{
    [Description("PLM影响评估")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class PLMYXPGBillPlugin : ECObjectEntityBill
    {
        private string userid="";
        private EntryGrid grds;
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
                this.AssociateALLs(list4, list2);
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
                if ((date["PARENTROWID"]==null || date["PARENTROWID"] == null?true:Convert.ToString(date["PARENTROWID"])==" "?true:false )&& Convert.ToBoolean( date["IsSelect"])==true)
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
            if (e.Field.Key == "FObjectType" && e.NewValue.ToString()=="4")
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
               // else
               // {
               //     userid = "";
               //     this.SetExecutors(0, e.Row);
               // }
            }
           else if(e.Field.Key == "FObjectType" && e.NewValue.ToString() == "5")
           {
               userid = "320646";
           }
           
            //else
            //{
            //     userid = "";
            //}
        }
    }
}
