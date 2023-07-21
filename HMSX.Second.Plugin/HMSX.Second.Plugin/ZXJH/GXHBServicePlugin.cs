using System.ComponentModel;
using Kingdee.BOS.Contracts;
using Kingdee.BOS;
using Kingdee.BOS.Core;
using Kingdee.BOS.ServiceHelper;
namespace HMSX.Second.Plugin.ZXJH
{
    public class GXHBServicePlugin : IScheduleService
    {
        public void Run(Context ctx, Schedule schedule)
        {
            string gxhbsql = $@"/*dialect*/update ha set F_260_SJWGRQ=case when F_260_CHECKBOXWW=1 then ha.FDATE 
                                        when F_260_CHECKBOXWW = 0 and hbc.FCHECKTYPE = 1 then ha.FDATE
                                        else isnull(hbb.FINSPECTTIME, ha.FAPPROVEDATE) end
                                         from T_SFC_OPTRPT ha
                                        inner join T_SFC_OPTRPTENTRY hb on ha.FID = hb.FID
                                        left join T_SFC_OPTRPTENTRY_B hbb on hb.FENTRYID = hbb.FENTRYID
                                        left join T_SFC_OPTRPTENTRY_C hbc on hb.FENTRYID = hbc.FENTRYID
                                        where hb.FMONUMBER not like 'MO%'
                                        and hb.FMONUMBER not like 'XNY%'
                                        and hb.FMONUMBER not like 'YJ%'
                                        AND hb.FMONUMBER not like '%ZD%'
                                        AND F_260_SJWGRQ IS NULL
                                        and FPRDORGID=100026
                                        and ha.FDOCUMENTSTATUS='C'
                                        and case when F_260_CHECKBOXWW = 1 then ha.FDATE
                                          when F_260_CHECKBOXWW = 0 and hbc.FCHECKTYPE = 1 then ha.FDATE
                                        else isnull(hbb.FINSPECTTIME, ha.FAPPROVEDATE) end is not null
                                         and fdate > '2023-04-01'";
            DBServiceHelper.Execute(ctx, gxhbsql);

            string gxjhsql = $@"/*dialect*/update D SET D.FREALPROCESSFINISHTIME=h.F_260_SJWGRQ
                               FROM T_SFC_OPERPLANNING a
                               inner join T_SFC_OPERPLANNINGSEQ b on a.fid=b.fid
                               inner join T_SFC_OPERPLANNINGDETAIL c on c.fentryid=b.fentryid
                               INNER join T_SFC_OPERPLANNINGDETAIL_B D on D.FDETAILID=C.FDETAILID
                               INNER JOIN T_SFC_OPTRPTENTRY hb on hb.FOPTPLANOPTID=c.FDETAILID
                               inner join T_SFC_OPTRPT h on h.FID=hb.fid
                               where FREALPROCESSFINISHTIME IS NULL
                               and a.FMONUMBER not like '%MO%' 
                               AND a.FMONUMBER not like '%XNY%'
                               and a.FMONUMBER not like 'YJ%'
                               AND a.FMONUMBER not like '%ZD%'
                               AND FPROORGID=100026
                               and FPRDORGID=100026
                               AND FOPERSTATUS=5
                               and h.FDOCUMENTSTATUS='C'
                               and F_260_SJWGRQ is not null";
            DBServiceHelper.Execute(ctx, gxjhsql);
        }
    }
}
