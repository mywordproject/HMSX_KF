using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS.Orm.DataEntity;
using System.ComponentModel;

namespace HMSX.SCZZ.CJGL.MES
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("调拨单审核校验")]
    public class DBDSHJY:AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            e.FieldKeys.Add("FDestStockId");
            e.FieldKeys.Add("FLot");
            e.FieldKeys.Add("FSRCMATERIALID");
        }
        public override void OnAddValidators(AddValidatorsEventArgs e)
        {
            
            base.OnAddValidators(e);
            if (this.Context.CurrentOrganizationInfo.ID == 100026)
            {
                LotValidator validator = new LotValidator();
                validator.AlwaysValidate = true;
                validator.EntityKey = "FBillEntry";
                e.Validators.Add(validator);
            }
        }
        //自定义校验器
        private class LotValidator : AbstractValidator
        {
            public override void Validate(ExtendedDataEntity[] dataEntities, ValidateContext validateContext, Context ctx)
            {
                if (ctx.CurrentOrganizationInfo.ID == 100026)
                {
                    foreach (ExtendedDataEntity obj in dataEntities)
                    {
                        DynamicObject ck = (DynamicObject)obj.DataEntity["DestStockId"];
                        string ph = obj.DataEntity["Lot_Text"].ToString();
                        DynamicObject wl = (DynamicObject)obj.DataEntity["MaterialId"];
                        if ((ck["Name"].ToString()).Contains("线边仓"))
                        {
                            string sql = string.Format(@"/*dialect*/select ph.FNUMBER,left(ph.FNUMBER,8) 一,cast(SUBSTRING(ph.FNUMBER,9,3) as int) 二
                        from T_STK_INVENTORY a
                        inner join T_BD_STOCK_L ck on ck.FSTOCKID=a.FSTOCKID
                        inner join T_BD_LOTMASTER ph on ph.FLOTID=a.FLOT
                        where a.FSTOCKORGID=100026 and a.FMATERIALID={0} and a.FSTOCKID={1} and left(ph.FNUMBER,8)=left('{2}',8) 
                        and cast(SUBSTRING(ph.FNUMBER,9,3) as int)>cast(SUBSTRING('{2}',9,3) as int)", wl["Id"], ck["Id"], ph);
                            DynamicObjectCollection dynamics = DBUtils.ExecuteDynamicObject(ctx, sql);
                            if (dynamics.Count > 0)
                            {
                                validateContext.AddError(obj.DataEntity,
                                    new ValidationErrorInfo
                                    ("", obj.DataEntity["Id"].ToString(), obj.DataEntityIndex, obj.RowIndex,
                                    "001",
                                    "单据编号" + obj.BillNo + "第" + (obj.RowIndex + 1) + "行对应即时库存中存在大于该批号的流水号!",
                                    "审核:" + obj.BillNo,
                                    Kingdee.BOS.Core.Validation.ErrorLevel.FatalError
                                    ));
                            }
                        }
                    }
                }
            }
        }
    }
}
