using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.ComponentModel;

namespace HMSX.SCZZ.CJGL.MES
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("工序汇报保存校验")]
    public class ZXGXHBBCJY: AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);           
            e.FieldKeys.Add("FMtoNo");
            e.FieldKeys.Add("FHMSXKHBQYD");           
        }
        //添加校验器
        public override void OnAddValidators(AddValidatorsEventArgs e)
        {

            base.OnAddValidators(e);
            if (this.Context.CurrentOrganizationInfo.ID == 100026)
            {
                LotValidator validator = new LotValidator();
                validator.AlwaysValidate = true;
                validator.EntityKey = "FEntity";
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
                        try
                        {
                            string DDH = obj.DataEntity["MoNumber"].ToString();
                            if (DDH.Substring(0, 2) == "MO")
                            {
                                long khbq = Convert.ToInt64(((DynamicObject)obj.DataEntity["FHMSXKHBQYD"])["Id"]);
                                string jhgzh = obj.DataEntity["FMtoNo"].ToString();
                                string[] gzhArr = jhgzh.Split('_');
                                string sql = $"select FSHORTNAME from HMD_t_Cust100150 bq left join T_BD_CUSTOMER_L kh on bq.F_HMD_BASEKH = kh.FCUSTID where bq.FID={khbq}";
                                string khjc = DBUtils.ExecuteScalar<string>(ctx, sql, "");
                                if (gzhArr[0]!="" && gzhArr[0] != khjc)
                                {
                                    validateContext.AddError(obj.DataEntity,
                                            new ValidationErrorInfo
                                            ("", obj.DataEntity["Id"].ToString(), obj.DataEntityIndex, obj.RowIndex,
                                            "001",
                                            "单据编号" + obj.BillNo + "第" + (obj.RowIndex + 1) + "行,生产订单计划跟踪号的客户与客户标签不一致，请使用正确订单汇报入库！",
                                            obj.BillNo,
                                            Kingdee.BOS.Core.Validation.ErrorLevel.FatalError
                                            ));
                                }
                            }
                        }
                        catch { return; }
                    }
                }
            }
        }
    }
}
