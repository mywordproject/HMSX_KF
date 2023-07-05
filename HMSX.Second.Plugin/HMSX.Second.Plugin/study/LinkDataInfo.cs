using System;
using System.Collections.Generic;
using System.Linq;
using Kingdee.BOS;
using Kingdee.BOS.Resource;
using Kingdee.BOS.Util;
namespace HMSX.Second.Plugin.study
{
	internal class LinkDataInfo
	{
		// Token: 0x17000006 RID: 6
		// (get) Token: 0x060000B0 RID: 176 RVA: 0x00008C35 File Offset: 0x00006E35
		// (set) Token: 0x060000B1 RID: 177 RVA: 0x00008C3D File Offset: 0x00006E3D
		private List<LinkBillInfo> Bills { get; set; }

		// Token: 0x060000B2 RID: 178 RVA: 0x00008C46 File Offset: 0x00006E46
		public LinkDataInfo()
		{
			this.Bills = new List<LinkBillInfo>();
		}

		// Token: 0x060000B3 RID: 179 RVA: 0x00008C5C File Offset: 0x00006E5C
		public int GetBillLinkRowCount(string formId)
		{
			LinkBillInfo billInfo = this.GetBillInfo(formId);
			foreach (LinkEntityInfo linkEntityInfo in billInfo.Entities)
			{
				if (linkEntityInfo.EntityIds.Count > 0)
				{
					return linkEntityInfo.EntityIds.Count;
				}
			}
			return 0;
		}

		// Token: 0x060000B4 RID: 180 RVA: 0x00008CEC File Offset: 0x00006EEC
		public LinkBillInfo GetBillInfo(string formId)
		{
			if (string.IsNullOrWhiteSpace(formId))
			{
				throw new KDExceptionValidate("", "formId", ResManager.LoadKDString("请传入正确的formId", "002546030019396", SubSystemType.BOS, new object[0]));
			}
			LinkBillInfo linkBillInfo = this.Bills.FirstOrDefault((LinkBillInfo t) => formId.EqualsIgnoreCase(t.FormId));
			if (linkBillInfo == null)
			{
				linkBillInfo = new LinkBillInfo(formId);
				this.Bills.Add(linkBillInfo);
			}
			return linkBillInfo;
		}

		// Token: 0x060000B5 RID: 181 RVA: 0x00008D70 File Offset: 0x00006F70
		public LinkEntityInfo GetEntityInfo(string formId, string entityKey)
		{
			if (string.IsNullOrWhiteSpace(formId))
			{
				throw new KDExceptionValidate("", "formId", ResManager.LoadKDString("请传入正确的formId", "002546030019396", SubSystemType.BOS, new object[0]));
			}
			if (string.IsNullOrWhiteSpace(entityKey))
			{
				throw new KDExceptionValidate("", "entityKey", ResManager.LoadKDString("请传入正确的entityKey", "002546030019399", SubSystemType.BOS, new object[0]));
			}
			LinkBillInfo billInfo = this.GetBillInfo(formId);
			LinkEntityInfo linkEntityInfo = null;
			foreach (LinkEntityInfo linkEntityInfo2 in billInfo.Entities)
			{
				if (linkEntityInfo2.EntityKey.EqualsIgnoreCase(entityKey))
				{
					linkEntityInfo = linkEntityInfo2;
					break;
				}
			}
			if (linkEntityInfo == null)
			{
				linkEntityInfo = new LinkEntityInfo(entityKey);
				billInfo.Entities.Add(linkEntityInfo);
			}
			return linkEntityInfo;
		}

		// Token: 0x060000B6 RID: 182 RVA: 0x00008E4C File Offset: 0x0000704C
		public void AddRowInfo(string formId, string entityKey, long entityId)
		{
			LinkEntityInfo entityInfo = this.GetEntityInfo(formId, entityKey);
			if (!entityInfo.EntityIds.Contains(entityId))
			{
				entityInfo.EntityIds.Add(entityId);
			}
		}
	}
}