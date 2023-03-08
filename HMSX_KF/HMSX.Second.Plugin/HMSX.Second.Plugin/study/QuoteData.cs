using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.study
{
	public class QuoteData
	{
		// Token: 0x0400002C RID: 44
		public long InquiryBillEntryId;

		// Token: 0x0400002D RID: 45
		public long QuoteBillId;

		// Token: 0x0400002E RID: 46
		public long QuoteBillEntryId;

		// Token: 0x0400002F RID: 47
		public string BillNO;

		// Token: 0x04000030 RID: 48
		public DateTime Date;

		// Token: 0x04000031 RID: 49
		public decimal Qty;

		// Token: 0x04000032 RID: 50
		public decimal BaseQty;

		// Token: 0x04000033 RID: 51
		public decimal Price;

		// Token: 0x04000034 RID: 52
		public decimal TaxPrice;

		// Token: 0x04000035 RID: 53
		public decimal TaxRate;

		// Token: 0x04000036 RID: 54
		public long UnitId;

		// Token: 0x04000037 RID: 55
		public long BaseUnitId;

		// Token: 0x04000038 RID: 56
		public long SupplierId;

		// Token: 0x04000039 RID: 57
		public string Contact;

		// Token: 0x0400003A RID: 58
		public string Phone;

		// Token: 0x0400003B RID: 59
		public long CurrencyId;

		// Token: 0x0400003C RID: 60
		public decimal Amount;

		// Token: 0x0400003D RID: 61
		public decimal AllAmount;

		// Token: 0x0400003E RID: 62
		public DynamicObject Data;
	}
}
