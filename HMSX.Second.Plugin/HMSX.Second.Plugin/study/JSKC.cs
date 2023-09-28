using Kingdee.K3.Core.SCM.Args;
using Kingdee.K3.Core.SCM.STK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.study
{
    [Kingdee.BOS.Util.HotUpdate]
    public class JSKC: AbstractBillQueryInvPlugIn
    {
        public override void BeforeShowInvList(long queryOrgId, List<long> orgIdList, ref string filter, ref string sortString, Dictionary<string, InvQueryHeaderArgs> headers)
        {
            base.BeforeShowInvList(queryOrgId, orgIdList, ref filter, ref sortString, headers);
        }
    }
}
