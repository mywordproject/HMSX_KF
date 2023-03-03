using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List.PlugIn;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.基础资料
{
    [Description("更新物料清单、用料清单")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class WLListPlugin: AbstractListPlugIn
    {
        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);            
            if (e.BarItemKey.Equals("KEEP_GX"))
            {
                if (Context.CurrentOrganizationInfo.ID == 100026)
                {
                    string wlqdsql = $@"/*dialect*/update T_ENG_BOMCHILD
                                 set T_ENG_BOMCHILD.FOVERCONTROLMODE=aa.FOVERCONTROLMODE,FISMINISSUEQTY=1
                                 from (
                                 select distinct a.FMATERIALID,FOVERCONTROLMODE from t_BD_MaterialProduce a
                                 left join T_BD_MATERIAL b on a.FMATERIALID=B.FMATERIALID
                                 where FISMINISSUEQTY=1
                                 and b.FUSEORGID=100026
                                 and SUBSTRING(b.FNUMBER,1,7)='260.01.'
                                 )aa where aa.FMATERIALID=T_ENG_BOMCHILD.FMATERIALID";
                    DBUtils.Execute(Context, wlqdsql);

                    string ylqdsql = $@"/*dialect*/update T_PRD_PPBOMENTRY_C
                               set T_PRD_PPBOMENTRY_C.FOVERCONTROLMODE=CC.FOVERCONTROLMODE,FISMINISSUEQTY=1
                               FROM
                               (
                               select a.FENTRYID,aa.FOVERCONTROLMODE from T_PRD_PPBOMENTRY a
                               inner join T_PRD_PPBOM b on a.fid=b.fid
                               inner join 
                               (
                               select fbillno,FSEQ from T_PRD_MOENTRY c
                               inner join T_PRD_MOENTRY_A E ON E.FENTRYID=C.FENTRYID
                               INNER JOIN T_PRD_MO D ON D.FID=C.FID 
                               where D.FDOCUMENTSTATUS='C'
                               AND E.FSTATUS in (1,2,3,4)
                               AND FBILLTYPE='00232405fc58a68311e33257e9e17076'
                               AND FPRDORGID=100026
                               )bb on  bb.FSEQ=B.FMOENTRYSEQ AND bb.FBILLNO=B.FMOBILLNO
                               inner join 
                               (
                               select distinct a.FMATERIALID,FOVERCONTROLMODE from t_BD_MaterialProduce a
                               left join T_BD_MATERIAL b on a.FMATERIALID=B.FMATERIALID
                               where FISMINISSUEQTY=1
                               and b.FUSEORGID=100026
                               and SUBSTRING(b.FNUMBER,1,7)='260.01.'
                               )aa on a.FMATERIALID=aa.FMATERIALID
                               WHERE B.FPRDORGID=100026
                               )CC WHERE CC.FENTRYID=T_PRD_PPBOMENTRY_C.FENTRYID";
                    DBUtils.Execute(Context, ylqdsql);

                    string ylqdsql1 = $@"/*dialect*/update A SET FBASEMINISSUEQTY=CEILING(FMUSTQTY/cc.FMINISSUEQTY)*cc.FMINISSUEQTY,FMINISSUEQTY=CEILING(FMUSTQTY/cc.FMINISSUEQTY)*cc.FMINISSUEQTY
                               FROM T_PRD_PPBOMENTRY_E A,
                               (
                               select a.FENTRYID,FMUSTQTY,aa.FMINISSUEQTY from T_PRD_PPBOMENTRY a
							   INNER JOIN T_PRD_PPBOMENTRY_Q a1 on a1.fentryid=a.fentryid
                               inner join T_PRD_PPBOM b on a.fid=b.fid
                               inner join 
                               (
                               select fbillno,FSEQ from T_PRD_MOENTRY c
                               inner join T_PRD_MOENTRY_A E ON E.FENTRYID=C.FENTRYID
                               INNER JOIN T_PRD_MO D ON D.FID=C.FID 
                               where D.FDOCUMENTSTATUS='C'
                               AND E.FSTATUS in (1,2,3,4)
                               AND FBILLTYPE='00232405fc58a68311e33257e9e17076'
                               AND FPRDORGID=100026
                               )bb on  bb.FSEQ=B.FMOENTRYSEQ AND bb.FBILLNO=B.FMOBILLNO
                               inner join 
                               (
                               select distinct a.FMATERIALID,FMINISSUEQTY from t_BD_MaterialProduce a
                               left join T_BD_MATERIAL b on a.FMATERIALID=B.FMATERIALID
                               where FISMINISSUEQTY=1
                               and b.FUSEORGID=100026
                               and SUBSTRING(b.FNUMBER,1,7)='260.01.'
                               )aa on a.FMATERIALID=aa.FMATERIALID
                               WHERE B.FPRDORGID=100026
                               )CC WHERE CC.FENTRYID=A.FENTRYID ";
                    DBUtils.Execute(Context, ylqdsql1);
                    this.View.ShowMessage("批量更新成功！");
                }
               
            }
        }
    }
}
