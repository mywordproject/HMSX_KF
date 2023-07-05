using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.study
{
	internal class LinkEntityInfo
	{
		// Token: 0x17000009 RID: 9
		// (get) Token: 0x060000BC RID: 188 RVA: 0x00008EB8 File Offset: 0x000070B8
		// (set) Token: 0x060000BD RID: 189 RVA: 0x00008EC0 File Offset: 0x000070C0
		public string EntityKey { get; set; }

		// Token: 0x1700000A RID: 10
		// (get) Token: 0x060000BE RID: 190 RVA: 0x00008EC9 File Offset: 0x000070C9
		// (set) Token: 0x060000BF RID: 191 RVA: 0x00008ED1 File Offset: 0x000070D1
		public List<long> EntityIds { get; set; }

		// Token: 0x060000C0 RID: 192 RVA: 0x00008EDA File Offset: 0x000070DA
		public LinkEntityInfo(string entityKey)
		{
			this.EntityKey = entityKey;
			this.EntityIds = new List<long>();
		}
	}
}
