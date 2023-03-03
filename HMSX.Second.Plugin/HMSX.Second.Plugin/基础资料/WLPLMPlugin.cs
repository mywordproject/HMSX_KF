using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Resource;
using Kingdee.K3.PLM.App.Core;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.Manager.ERP;
using Kingdee.K3.PLM.Common.BusinessEntity.View;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.K3.PLM.Business.PlugIn;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.Base;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.Entity;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.Manager;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.Manager.Document;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.Manager.InitializationTool;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.Manager.PhysicalFile;
using Kingdee.K3.PLM.Common.Core.BOSBridge;
using Kingdee.K3.PLM.Common.Core.Operation;
using Kingdee.K3.PLM.Common.Core.ServiceHelper;
using Kingdee.K3.PLM.Common.Core.Utility;
using Kingdee.K3.PLM.Common.Framework.Utility;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.App;
using Kingdee.BOS.Contracts;
using Kingdee.BOS;
using Kingdee.BOS.Util;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.App.Core.RealTimeWarn.Log;
using Kingdee.BOS.Core.Log;
using Kingdee.K3.PLM.Common.Framework.Exceptions;
using Kingdee.K3.PLM.Common.Core.DynamicPluginHelper;
using Kingdee.BOS.App.Core.Utils;

namespace HMSX.Second.Plugin.基础资料
{
    [Description("物料同步PLM")]
    //热启动,不用重启IIS
    //python
    [Kingdee.BOS.Util.HotUpdate]
    public class WLPLMPlugin : AbstractPLMResourceOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            //base.OnPreparePropertys(e);
            String[] propertys = { "FSpecification", "FErpClsID", "FCreateDate" , "FCreatorId" ,"F_260_XMMCWB",
                 "FNumber", "FName", "FFixLeadTime" ,"FBaseUnitId","FCategoryID","F_260_KHWLBB","F_260_Textbbh","F_260_Basexm"};
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
                    foreach (var date in e.DataEntitys)
                    {
                        string cxsql = $@"select * from T_PLM_PDM_BASE_M where FERPMATERIALID='{date["Id"]}'";
                        var cx = DBUtils.ExecuteDynamicObject(Context, cxsql);
                        if (cx.Count == 0)
                        {
                            var formid = "";
                            var ywlx = "";
                            if (date["Number"].ToString().Substring(0, 7) == "260.02.")
                            {
                                formid = "10101000000000000007875BE6D201FE5A3";
                                ywlx = "1010100000000000000";
                            }
                            else if (date["Number"].ToString().Substring(0, 7) == "260.03.")
                            {
                                formid = "1010200000000000000907E427106110055";
                                ywlx = "1010200000000000000";
                            }
                            else if (date["Number"].ToString().Substring(0, 7) == "260.01."&& 
                                date["Number"].ToString().Contains("260.01.8888")==false &&
                                date["Number"].ToString().Contains("260.01.15") == false &&
                                date["Number"].ToString().Contains("260.01.19") == false &&
                                date["Number"].ToString().Contains("260.01.20") == false &&
                                date["Number"].ToString().Contains("260.01.18") == false &&
                                date["Number"].ToString().Contains("260.01.99") == false )
                            {
                                formid = "1010300000000000000893B1AFAF2B3442D";
                                ywlx = "1010300000000000000";
                            }
                            if (formid != "")
                            {
                                //创建BOM表单
                                using (CommonViewProxy proxy = new CommonViewProxy(PLMContext, formid, false))
                                {
                                    var view = proxy.GetEditView(0);
                                    var obj = view.Model.DataObject;
                                    //对对象进行赋值；                                                             
                                    //调用保存操作
                                    obj["Specification"] = date["Specification"].ToString();//规格型号
                                                                                            //obj["Specification"] =//重量单位                           
                                    obj["CreateDate"] = date["CreateDate"];//创建日期
                                    obj["CreatorId_Id"] = date["CreatorId_Id"];//创建人
                                    obj["Code"] = date["Number"];//编码
                                    obj["Name"] = date["Name"];//名称
                                    //obj["Specification"] =//库存单位
                                    //obj["Specification"] =//销售单位
                                    //obj["Specification"] =//采购单位                                                    
                                    obj["F260KHBB"] = date["F_260_KHWLBB"];//客户图纸版本号
                                    obj["F260NBBB"] = date["F_260_Textbbh"];//内部物料版本
                                    obj["F260XMMC"] = date["F_260_Basexm"];//项目号名称
                                    obj["F260XMMCWB"] = date["F_260_XMMCWB"];//项目名称文本
                                    obj["CategoryID_Id"] = ywlx;//业务类型  产成品
                                    obj["LifeCircleStage"] = "AC";//生命周期阶段
                                    foreach (var zdate in date["MaterialBase"] as DynamicObjectCollection)
                                    {
                                        obj["BaseUnitId_Id"] = zdate["BaseUnitId_Id"];//基本单位 基本
                                        obj["GoodsType_Id"] = zdate["CategoryID_Id"];//存货类别 基本
                                        obj["ErpClsID"] = zdate["ErpClsID"];//物料属性  基本
                                    }
                                    foreach (var zdate1 in date["MaterialPlan"] as DynamicObjectCollection)
                                    {
                                        obj["purchaseCycle"] = zdate1["FixLeadTime"];//固定提前期 计划 
                                    }
                                    obj["ErpMaterialID_Id"] = date["Id"];

                                    //if (dataObject.DynamicObjectType.Properties.Contains("BOMFLEXCONFIG"))
                                    //{
                                    //	dataObject.SetDynamicObjectItemValue("BOMFLEXCONFIG_ID", matId);
                                    //	dataObject.SetDynamicObjectItemValue("ErpMaterialID", dynamicObject2);
                                    //}
                                    obj["Inventory_Id"] = date["Id"];
                                    var service = ServiceHelper.GetService<IDoNothingService>();
                                    Kingdee.BOS.Orm.OperateOption option = Kingdee.BOS.Orm.OperateOption.Create();
                                    OperationHelper.MarkBackCalling(option);
                                    var result = service.DoNothingWithDataEntity(PLMContext.BOSContext, view.BillBusinessInfo, new DynamicObject[] { view.Model.DataObject }, "Save", option);
                                    if (result.IsSuccess == false)
                                    {
                                        throw new KDBusinessException("", "物料同步PLM失败");
                                    }
                                    //是否同步成功                     
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
