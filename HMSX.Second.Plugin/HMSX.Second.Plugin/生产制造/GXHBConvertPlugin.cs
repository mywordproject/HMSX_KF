using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.K3.MFG.QM.App.BillConvertServicePlugIn;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.生产制造
{
    [Description("工序汇报--检验单")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class GXHBConvertPlugin : OperRpt2InspectConvert
    {
        public override void AfterConvert(AfterConvertEventArgs e)
        {
            base.AfterConvert(e);
            if (Context.CurrentOrganizationInfo.ID == 100026)
            {
                var targetForm = e.TargetBusinessInfo.GetForm();
                if (targetForm.LinkSet == null
                || targetForm.LinkSet.LinkEntitys == null
                || targetForm.LinkSet.LinkEntitys.Count == 0)
                {
                    //目标单未设置关联主实体，无法获取目标单的源单信息，携带不了
                    throw new KDBusinessException("", "未设置关联实体请设置");
                }
                //单据整体数据包
                var billDataObjs = e.Result.FindByEntityKey("FBillHead");
                foreach (var billObj in billDataObjs)
                {
                    DynamicObject billData = billObj.DataEntity;
                    //明细数据包
                    DynamicObjectCollection entryDataObjColl = billData["Entity"] as DynamicObjectCollection;
                    foreach (var entryRow in entryDataObjColl)
                    {
                        var FPolicyDetails = entryRow["PolicyDetail"] as DynamicObjectCollection;
                        int i = 0;
                        foreach (var PolicyDetail in FPolicyDetails)
                        {
                            string gxhbsql = $@"select a.fid,FOPTPLANOPTID,FOPTPLANNO,d.FISSTOREINPOINT from T_SFC_OPTRPT a
                                            inner join T_SFC_OPTRPTENTRY b on a.fid=b.fid
                                            inner join T_BD_MATERIAL C ON C.FMATERIALID=B.FMATERIALID
                                            left join T_SFC_OPERPLANNINGDETAIL d on d.FDETAILID=b.FOPTPLANOPTID
                                            where fbillno='{entryRow["SrcBillNo"]}'
                                            AND (b.FMONUMBER LIKE '%MO%' OR b.FMONUMBER LIKE '%XNY%' OR b.FMONUMBER LIKE '%YJ%')
                                            and substring(C.FNUMBER,1,6)='260.02'  and d.FISSTOREINPOINT=1";
                            var gxhb = DBUtils.ExecuteDynamicObject(Context, gxhbsql);

                            if ((PolicyDetail["UsePolicy"].ToString() == "A" || PolicyDetail["UsePolicy"].ToString() == "B") &&
                              gxhb.Count>0)
                            {
                                billData["F_260_JYQF"] = 1;
                                entryRow["InspectResult"] = 3;
                                entryRow["QualifiedQty"] = 0;//合格数
                                entryRow["BaseQualifiedQty"] = 0;//基本单位合格数
                                entryRow["SampleDamageQty"] = 0;//样本破坏数
                                entryRow["UnqualifiedQty"] = 0;//不合格数
                                entryRow["BaseAcceptQty"] = 0;//基本单位接收数量
                                entryRow["BaseRejectQty"] = 0;//基本单位判退数量
                                entryRow["BaseDefectQty"] = 0;//基本单位不良数量
                                entryRow["BaseScrapQty"] = 0;//基本单位报废数量
                                entryRow["BaseRepairQty"] = 0;//基本单位返修数量
                                entryRow["BasePickQty"] = 0;//基本单位挑选数量
                                entryRow["BaseMtrlScrapQty"] = 0;//基本单位料废数量
                                entryRow["BaseProcScrapQty"] = 0;//基本单位工废数量
                                i++;
                            }
                        }
                        if (i > 0)
                        {
                            FPolicyDetails.Clear();
                        }
                    }
                }
            }
        }
    }
}
