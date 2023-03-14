using Kingdee.BOS;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.ComponentModel;

namespace HMSX.GYL.CGGL
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("双新供应商评分校验")]
    public class ZXGYSPFBCJY: AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            e.FieldKeys.Add("F_260_HGGYS");
            e.FieldKeys.Add("F_keed_Integer");
            //质量
            e.FieldKeys.Add("F_keed_Decimal1");
            e.FieldKeys.Add("F_keed_Base7");
            //采购
            e.FieldKeys.Add("F_keed_Decimal2");
            e.FieldKeys.Add("F_keed_Base8");
            //工程
            e.FieldKeys.Add("F_keed_Decimal3"); 
            e.FieldKeys.Add("F_keed_Base9");            
        }
        public override void OnAddValidators(AddValidatorsEventArgs e)
        {
            base.OnAddValidators(e);
            if(this.Context.CurrentOrganizationInfo.ID == 100026)
            {
                PFValidator validator = new PFValidator();
                validator.AlwaysValidate = true;
                validator.EntityKey = "F_HMD_Entity";
                e.Validators.Add(validator);
            }
        }
        private class PFValidator: AbstractValidator
        {
            public override void Validate(ExtendedDataEntity[] dataEntities, ValidateContext validateContext, Context ctx)
            {
                if (this.Context.CurrentOrganizationInfo.ID == 100026)
                {
                    foreach(ExtendedDataEntity entry in dataEntities)
                    {
                        if(Convert.ToDouble(entry["F_keed_Integer"]) > 0)
                        {
                            string err = "";
                            //质量未评分
                            if (Convert.ToInt64(entry["F_keed_Base7_Id"]) == this.Context.UserId && Convert.ToDouble(entry["F_keed_Decimal1"]) == 0)
                            {
                                err += "质量未评分,";
                            }
                            //采购未评分
                            if (Convert.ToInt64(entry["F_keed_Base8_Id"]) == this.Context.UserId && Convert.ToDouble(entry["F_keed_Decimal2"]) == 0)
                            {
                                err += "采购未评分,";
                            }
                            //工程未评分
                            if (Convert.ToInt64(entry["F_keed_Base9_Id"]) == this.Context.UserId && Convert.ToDouble(entry["F_keed_Decimal3"]) == 0)
                            {
                                err += "工程未评分,";
                            }
                            if (err != "")
                            {
                                validateContext.AddError(entry.DataEntity, new ValidationErrorInfo
                                    ("", entry.DataEntity["Id"].ToString(), entry.DataEntityIndex, entry.RowIndex,"001",
                                    $"第{entry.RowIndex + 1}行分录，{((DynamicObject)entry["F_260_HGGYS"])["Name"]}:{err.Substring(0,err.Length-1)};",
                                    entry.BillNo, Kingdee.BOS.Core.Validation.ErrorLevel.FatalError
                                    )
                                 );
                            }
                        }
                    }
                }                
            }
        }
    }
}
