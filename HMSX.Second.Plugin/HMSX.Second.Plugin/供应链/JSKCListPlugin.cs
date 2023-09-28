using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.List.PlugIn;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.供应链
{
	[HotUpdate]
	[Description("即时库存")]
	public class JSKCListPlugin : AbstractListPlugIn
	{
		int dbsq = 0;
        public override void OnInitialize(InitializeEventArgs e)
        {
            base.OnInitialize(e);
			object customParameter = this.View.OpenParameter.GetCustomParameter("DBSQ");
			if (customParameter != null)
			{
				this.dbsq = Convert.ToInt32(customParameter);
			}
		}
        public override void BarItemClick(BarItemClickEventArgs e)
		{
			base.BarItemClick(e);
			string key;
            if (dbsq == 1)
            {
				switch (key = e.BarItemKey.ToUpperInvariant())
				{
					case "TBRETURNDATA":
						FHSJ();
						break;
				}
			}
			
		}
		public void FHSJ()
        {
			ListSelectedRowCollection listcoll = this.ListView.SelectedRowsInfo;
			this.View.ReturnToParentWindow(listcoll);
			base.View.Close();
		}
	}
}
