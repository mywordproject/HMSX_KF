using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.study
{
	public class QuoteSumData
	{
		// Token: 0x17000006 RID: 6
		// (get) Token: 0x06000077 RID: 119 RVA: 0x00009721 File Offset: 0x00007921
		public bool IsMinQuote
		{
			get
			{
				return this.SumComRst == "1";
			}
		}

		// Token: 0x0400003F RID: 63
		public long QuoteBillId;

		// Token: 0x04000040 RID: 64
		public string QuoteBillNO;

		// Token: 0x04000041 RID: 65
		public string SumComRst = "9";

		// Token: 0x04000042 RID: 66
		public long SupplierId;

		// Token: 0x04000043 RID: 67
		public long CurrencyId;

		// Token: 0x04000044 RID: 68
		public decimal BillAmount;

		// Token: 0x04000045 RID: 69
		public decimal BillAllAmount;
	}
}
