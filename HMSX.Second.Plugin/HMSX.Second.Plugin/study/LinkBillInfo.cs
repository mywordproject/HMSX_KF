using System;
using System.Collections.Generic;
namespace HMSX.Second.Plugin.study
{
	internal class LinkBillInfo
	{
		// Token: 0x17000007 RID: 7
		// (get) Token: 0x060000B7 RID: 183 RVA: 0x00008E7C File Offset: 0x0000707C
		// (set) Token: 0x060000B8 RID: 184 RVA: 0x00008E84 File Offset: 0x00007084
		public string FormId { get; set; }

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x060000B9 RID: 185 RVA: 0x00008E8D File Offset: 0x0000708D
		// (set) Token: 0x060000BA RID: 186 RVA: 0x00008E95 File Offset: 0x00007095
		public List<LinkEntityInfo> Entities { get; set; }

		// Token: 0x060000BB RID: 187 RVA: 0x00008E9E File Offset: 0x0000709E
		public LinkBillInfo(string formId)
		{
			this.FormId = formId;
			this.Entities = new List<LinkEntityInfo>();
		}
	}
}