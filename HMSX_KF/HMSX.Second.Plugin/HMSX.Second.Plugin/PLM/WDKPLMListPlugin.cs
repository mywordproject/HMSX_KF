using Kingdee.BOS;
using Kingdee.BOS.App.Core.Utils;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.K3.PLM.Business.PlugIn;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.Manager;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.PLM
{
    [Description("PLM文档库")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class WDKPLMListPlugin:AbstractPLMListPlugIn
    {
        /// <summary>
        /// 放在周泽祥插件中
        /// </summary>
        /// <param name="e"></param>
        public override void BeforeDoOperation(BeforeDoOperationEventArgs e)
        {
            if (e.Operation.FormOperation.Operation == "DoStartFlow")
            {
                var lst = this.ListView.SelectedRowsInfo;
                var selectDatas = lst.Select(x => x.DataRow).ToList().ToDictionary(w => w["FID"], w => long.Parse(w["FCategoryID_Id"].ToString()));
                var pdmObjList = new List<DynamicObject>();
                foreach (var objs in selectDatas.GroupBy(w => w.Value))
                {
                    var pdmobj = DomainObjectManager.Instance(PLMContext, objs.Key).Load(PLMContext, objs.Select(w => w.Key).ToArray());
                    pdmObjList = pdmObjList.Concat(pdmobj).ToList();
                }
                foreach (var pdmobj in pdmObjList)
                {
                    if (pdmobj != null)
                    {
                        string code = pdmobj.GetDynamicObjectItemValue<string>("Code");
                        string bgdsql = $@"SELECT DISTINCT FCODE FROM T_PLM_PDM_BASE A
                        LEFT JOIN T_PLM_STD_EC_ITEM B ON A.FID=B.FID
                        WHERE B.FOBJECTCODE='{code}' and FLIFECIRCLESTAGE='AJ'";
                        var bgds=DBUtils.ExecuteDynamicObject(Context, bgdsql);
                        foreach(var bgd in bgds)
                        {
                             string cxsql = $@"select FINSTCODE 实例编码,DJ.fname 单据,A.FBILLNUMBER 单据编号,YHB.FNAME 发送人,A.FCREATETIME 发送时间,C.FASSIGNNAME 节点名称,YH.FNAME 处理人,
                             B.FOPENTIME 查看时间,B.FCOMPLETEDTIME 完成时间,B.FDISPOSITION 审批意见,AB.FRESULTNAME 审批结果,'否' 流程完成,A.FCREATETIME,B.FCOMPLETEDTIME
                             from v_wf_PMAssign A
                             left join v_wf_PMReceiverItem B on A.FASSIGNID=B.FASSIGNID
                             left join V_WF_PMRECEIVERITEM_L AB on AB.freceiveritemid=B.freceiveritemid and AB.FLOCALEID=2052
                             left join T_SEC_user YH on YH.FUSERID=B.FRECEIVERID
                             left join T_SEC_user YHB on YHB.FUSERID=A.FSENDERID
                             left join V_WF_PMASSIGN_L C on A.FASSIGNID=C.FASSIGNID and C.FLOCALEID=2052
                             left join V_META_OBJECTTYPE_L DJ on DJ.FID=A.FOBJECTTYPEID
                             where A.FORGID=100026 and
                             DJ.fname='业务类型_ECN(变更单)'and
                             FINSTCODE like '%{bgd["FCODE"]}%'";
                             var cx = DBUtils.ExecuteDynamicObject(Context, cxsql);
                            //bool isChange = pdmobj.GetDynamicObjectItemValue<bool>("IsChange");
                            bool isChangeObject = pdmobj.GetDynamicObjectItemValue<bool>("IsChangeObject");
                            string lifeCircleStage = pdmobj.GetDynamicObjectItemValue<string>("LifeCircleStage");
                            LocaleValue name = pdmobj.GetDynamicObjectItemValue<LocaleValue>("Name");
                            if (cx.Count > 0 && lifeCircleStage == "AJ")
                            {
                                this.View.ShowWarnningMessage(string.Format("名称：{0}  编码：{1},对象在提交状态的变更中，不允许提交;", name, code));
                                e.Cancel = true;
                                return;
                            }
                        }                                         
                    }
                }
                base.BeforeDoOperation(e);
            }
        }
    }
}
