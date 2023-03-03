using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS;
using Kingdee.BOS.App;
using Kingdee.BOS.App.Core.Utils;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Resource;
using Kingdee.BOS.Util;
using Kingdee.K3.PLM.App.Core;
using Kingdee.K3.PLM.Business.PlugIn;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.Base;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.Entity;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.Manager;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.Manager.Document;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.Manager.ERP;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.Manager.InitializationTool;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.Manager.PhysicalFile;
using Kingdee.K3.PLM.Common.BusinessEntity.View;
using Kingdee.K3.PLM.Common.Core.BOSBridge;
using Kingdee.K3.PLM.Common.Core.Operation;
using Kingdee.K3.PLM.Common.Core.ServiceHelper;
using Kingdee.K3.PLM.Common.Core.Utility;
using Kingdee.K3.PLM.Common.Framework.Utility;
using Kingdee.K3.PLM.STD.Common.BusinessEntity.Project;
namespace HMSX.Second.Plugin.基础资料
{
    [Description("物料清单--更新PLM")]
    //热启动,不用重启IIS
    //python
    [Kingdee.BOS.Util.HotUpdate]
    public class WLQDPLMPlugin : AbstractPLMResourceOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            //base.OnPreparePropertys(e);
            String[] propertys = { "FDescription", "FRowId", "FReplaceGroup" , "FMATERIALIDCHILD" ,
                 "FMATERIALTYPE", "FNUMERATOR", "FDENOMINATOR" ,"FSCRAPRATE","FEFFECTDATE","FEXPIREDATE","FBOMSRC"};
            foreach (String property in propertys)
            {
                e.FieldKeys.Add(property);
            }
        }
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            if (Context.CurrentOrganizationInfo.ID == 100026)
            {//Audit
                if (FormOperation.Operation.Equals("Audit", StringComparison.OrdinalIgnoreCase))
                {
                    //获取BOM的表单标识
                    var formid = "10320000000000000005CA7946FD3274AAA";
                    foreach (var date in e.DataEntitys)
                    {
                        string wl = date["MATERIALID"] == null ? "" : (date["MATERIALID"] as DynamicObject)["Number"].ToString().Substring(0, 7);
                        if ((wl == "260.02." || wl == "260.03.") && date["BOMSRC"].ToString()!="A")
                        {
                            //创建BOM表单
                            using (CommonViewProxy proxy = new CommonViewProxy(PLMContext, formid, false))
                            {
                                //查询出FID
                                long fid = 0;
                                string cxsql = $@"select FID from T_PLM_PDM_BASE_0 where FERPBOMID = '{date["Id"]}'";
                                var cx = DBUtils.ExecuteDynamicObject(Context, cxsql);
                                if (cx.Count > 0)
                                {
                                    fid = Convert.ToInt64(cx[0]["FID"]);

                                    var view = proxy.GetEditView(fid);
                                    var obj = view.Model.DataObject;
                                    //对对象进行赋值；
                                    if (obj.Contains("SlaveRelationEntry"))
                                    {
                                        (obj["SlaveRelationEntry"] as DynamicObjectCollection).Clear();
                                    }
                                    obj["F_260_MS"] = date["Description"].ToString();
                                    var SlaveRelationEntry = obj["SlaveRelationEntry"] as DynamicObjectCollection;
                                    //获取ERPBOM的数据
                                    int i = 1;
                                    foreach (var erpBomId in date["TreeEntity"] as DynamicObjectCollection)
                                    {
                                        List<long> list = new List<long>();
                                        list.Add(Convert.ToInt64(erpBomId["MATERIALIDCHILD_Id"]));
                                        IEnumerable<long> wlid = list;
                                        var x = GetMatIdByMultiErpId(PLMContext, wlid);
                                        long id = 0;
                                        foreach (var k in x.Values)
                                        {
                                            if (k == null)
                                            {
                                                throw new KDBusinessException("", "子项物料"+((DynamicObject)erpBomId["MATERIALIDCHILD"])["Number"].ToString()+"未同步到plm，请联系系统管理员同步后再审核！");
                                            }
                                            id = Convert.ToInt64(k["Id"]);
                                        }
                                        var newRow = new DynamicObject((obj["SlaveRelationEntry"] as DynamicObjectCollection).DynamicCollectionItemPropertyType);
                                        newRow["Seq"] = i;
                                        newRow["SR_ReplaceGroupSeq"] = i;
                                        newRow["ROWID"] = SequentialGuid.NewGuid().ToString();
                                        newRow["RP_FMATERIALTYPE"] = erpBomId["MATERIALTYPE"];
                                        newRow["SlaveRelation_Id"] = id;
                                        newRow["RP_FBOMNUMBER"] = erpBomId["NUMERATOR"];
                                        newRow["RP_FRadix"] = erpBomId["DENOMINATOR"];
                                        newRow["RP_FWaste"] = erpBomId["FSCRAPRATE"];
                                        newRow["RP_FSUBEFFECTIVEDATE"] = erpBomId["EFFECTDATE"];
                                        newRow["RP_FSUBEXPIRYDATE"] = erpBomId["EXPIREDATE"];
                                        SlaveRelationEntry.Add(newRow);
                                        i++;
                                    }
                                    //调用保存操作
                                    var service = ServiceHelper.GetService<IDoNothingService>();
                                    Kingdee.BOS.Orm.OperateOption option = Kingdee.BOS.Orm.OperateOption.Create();
                                    OperationHelper.MarkBackCalling(option);
                                    var result = service.DoNothingWithDataEntity(PLMContext.BOSContext, view.BillBusinessInfo, new DynamicObject[] { view.Model.DataObject }, "Save", option);

                                }
                                //是否同步成功                     
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 通过erpID获取物料ID
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="lstErpId"></param>
        /// <returns>ERPID与物料对应的字典</returns>
        public Dictionary<long, DynamicObject> GetMatIdByMultiErpId(PLMContext ctx, IEnumerable<long> lstErpId)
        {
            if (lstErpId == null || lstErpId.Count() == 0) return new Dictionary<long, DynamicObject>();
            //查询对应关系
            var dynErpIdMap = PLMDBUtils.Instance.ExecuteDynamicObject(ctx,
                "SELECT A.FMATERIALID, B.FMATERIALID FMATERIALID_ORG FROM T_BD_MATERIAL A INNER JOIN T_BD_MATERIAL B ON A.FMASTERID = B.FMASTERID WHERE A.FCREATEORGID = A.FUSEORGID AND A.FNUMBER = B.FNUMBER AND B.FMATERIALID in (SELECT FID FROM TABLE(fn_StrSplit(@lstErpId, ',',1)))",
                PLMDBUtils.Instance.GetAParameter("@lstErpId", KDDbType.udt_inttable, lstErpId.Distinct().ToArray()));

            var lstMatId = dynErpIdMap.Select(d => d["FMATERIALID"].ToInt64Ex());
            if (!lstMatId.Any())
            {
                return new Dictionary<long, DynamicObject>();
            }

            //获取物料ID信息
            var erpMatInfo = DomainObjectManager.Instance(ctx, (long)StandardCategoryType.Material).Load(ctx,
                    string.Format("FERPMATERIALID in ({0})", string.Join(",", lstMatId))).ToList()
                .GroupBy(d => d["ErpMaterialID_Id"].ToInt64Ex()).ToDictionary(d => d.Key, d => d.First());

            //ERPID与物料信息匹配
            Dictionary<long, DynamicObject> dicErpMapMat = new Dictionary<long, DynamicObject>();
            foreach (var dynErpId in dynErpIdMap)
            {
                var matIdOrg = dynErpId["FMATERIALID_ORG"].ToInt64Ex();
                var matId = dynErpId["FMATERIALID"].ToInt64Ex();
                if (!dicErpMapMat.ContainsKey(matId))
                {
                    var mat = erpMatInfo.ContainsKey(matId) ? erpMatInfo[matId] : null;
                    dicErpMapMat.Add(matIdOrg, mat);
                }
            }

            return dicErpMapMat;
        }
    }
}
