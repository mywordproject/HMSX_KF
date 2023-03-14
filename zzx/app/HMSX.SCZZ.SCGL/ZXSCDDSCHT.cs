using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace HMSX.SCZZ.SCGL
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("生产订单删除释放占用库存")]
    public class ZXSCDDSCHT: AbstractOperationServicePlugIn
    {        
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            foreach(DynamicObject entity in e.DataEntitys)
            {
                string MOBI = entity["BillNo"].ToString();
                string sSQL = $"/*dialect*/select ZID,FPSL from SX_MOKCFP where MOBILLNO='{MOBI}'";
                DynamicObjectCollection objs = DBUtils.ExecuteDynamicObject(this.Context, sSQL);
                foreach(DynamicObject obj in objs)
                {
                    string uSQL = $"/*dialect*/update SX_WLKCJY set SYKC=SYKC+{obj["FPSL"]} where WLID={obj["ZID"]}";
                    DBUtils.Execute(this.Context, uSQL);
                }
                string dSQL = $"/*dialect*/delete SX_MOKCFP where MOBILLNO='{MOBI}'";
                DBUtils.Execute(this.Context, dSQL);
            }
        }
    }
}
