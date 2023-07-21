using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Resource;
using Kingdee.BOS.Util;
using Kingdee.K3.PLM.Business.PlugIn;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.Entity;
using Kingdee.K3.PLM.CFG.Common.BusinessEntity.Manager;
using Kingdee.K3.PLM.Common.Core.BOSBridge;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
namespace HMSX.Second.Plugin.PLM
{
	[HotUpdate, Description("自定义客户版本升版动态表单插件")]
	public class ZDYKHBBSDBFormPlugin : AbstractPLMDynamicFormPlugIn
	{
		private long _currentCategroyId;
		private DynamicObject objDyn;
		private List<DynamicObject> objList;
		private DynamicObject categoryDyn;
		private bool isMaxVersion = true;
		private string sequenceType = string.Empty;
		private string[] sequence;
		public override void OnLoad(EventArgs e)
		{
			this.categoryDyn = CategoryManager.Instance.Get(this.PLMContext, this._currentCategroyId);
			this.sequenceType = (this.isMaxVersion ? "Max" : "Min");
			this.InitUpgradeView(this.isMaxVersion);
			List<string> maxOrMinVersionSequence = SequenceModel.Instance.GetMaxOrMinVersionSequence(this.PLMContext, this._currentCategroyId, this.isMaxVersion, !this.isMaxVersion);
			if (!maxOrMinVersionSequence.Any<string>())
			{
				this.View.ShowWarnningMessage(ResManager.LoadKDString("请先维护该业务类型版本序列", "120549000004114", SubSystemType.PLM, new object[0]), "", MessageBoxOptions.OK, null, MessageBoxType.Advise);
				return;
			}
			this.sequence = maxOrMinVersionSequence.FirstOrDefault<string>().Split(new char[]
			{
				','
			});
			base.OnLoad(e);
		}
		public override void EntityRowClick(EntityRowClickEventArgs e)
		{
			base.EntityRowClick(e);
		}
		public int GetVersionIndex(PLMContext ctx, string version)
		{
			for (int i = 0; i < this.sequence.Count<string>(); i++)
			{
				if (this.sequence[i].Equals(version))
				{
					return i;
				}
			}
			return -1;
		}
		public void InitUpgradeView(bool isMax)
		{
			if (isMax && Convert.ToInt64(this.categoryDyn["MajorStruct_Id"]) != 0L)
			{
				this.SetDefaultComboField(this.PLMContext, isMax, this._currentCategroyId);
			}
			if (!isMax && Convert.ToInt64(this.categoryDyn["MinorStruct_Id"]) != 0L)
			{
				this.SetDefaultComboField(this.PLMContext, isMax, this._currentCategroyId);
			}
		}
		protected void SetDefaultComboField(PLMContext ctx, bool isMaxVersion, long categroyId)
		{
			int num = Convert.ToInt32(this.objDyn[this.sequenceType + "Ver"]);
			if (num < 0)
			{
				this.View.ShowErrMessage("当前版本不存在！", "", MessageBoxType.Notice);
				return;
			}
			List<string> maxOrMinVersionSequence = SequenceModel.Instance.GetMaxOrMinVersionSequence(ctx, categroyId, isMaxVersion, !isMaxVersion);
			ComboFieldEditor comboFieldEditor = (ComboFieldEditor)this.View.GetControl("FUpgrade" + this.sequenceType + "Ver");
			string[] array = maxOrMinVersionSequence.FirstOrDefault<string>().Split(new char[]
			{
				','
			});
			if (comboFieldEditor != null)
			{
				List<EnumItem> list = new List<EnumItem>();
				for (int i = num + 1; i < array.Count<string>(); i++)
				{
					list.Add(new EnumItem
					{
						Seq = i,
						Value = array[i],
						EnumId = array[i],
						Caption = new LocaleValue(ResManager.LoadKDString(array[i], "120007000000200", SubSystemType.PLM, new object[0]))
					});
				}
				comboFieldEditor.SetComboItems(list);
				if (num + 1 < array.Count<string>())
				{
					this.View.Model.SetValue("FUpgrade" + this.sequenceType + "Ver", array[num + 1]);
				}
			}
			//客户版本序列
			string bbxlsql = $@" SELECT C.FID,C.FSEPARATE,MSEQ.FSEQSTRUCT FROM T_PLM_CFG_CATEGORY C 
                              inner JOIN  T_PLM_CFG_SEQUENCE MSEQ ON C.F_260_KHBBXL = MSEQ.FID 
                              WHERE C.FID = '{categroyId}'";
			var bbxl = DBUtils.ExecuteDynamicObject(Context, bbxlsql);
            if (bbxl.Count > 0)
            {
				ComboFieldEditor comboFieldEditor1 = (ComboFieldEditor)this.View.GetControl("F_26_KHDBBH");
				string[] array1 = bbxl[0]["FSEQSTRUCT"].ToString().Split(new char[]
			     {
				','
			    });
				string bbhsql = $@"select F_260_KHBB from T_PLM_PDM_BASE_0 where F_260_KHBB!='' and FID='{this.objDyn["Id"]}'";
				var bbh = DBUtils.ExecuteDynamicObject(Context, bbhsql);
				if (comboFieldEditor1 != null)
				{
					List<EnumItem> list = new List<EnumItem>();
					int k = -1;
					for (int i = 1; i < array1.Count<string>(); i++)
					{
                        if (bbh.Count > 0)
                        {
							if(bbh[0]["F_260_KHBB"].ToString()!= array1[i] && k != -1)
                            {
								list.Add(new EnumItem
								{
									Seq = i,
									Value = array1[i],
									EnumId = array1[i],
									Caption = new LocaleValue(ResManager.LoadKDString(array1[i], "120007000000200", SubSystemType.PLM, new object[0]))
								});
								
                            }
                            else if(bbh[0]["F_260_KHBB"].ToString() == array1[i])
                            {
								k = i;
							}						
						}
                        else
                        {
							list.Add(new EnumItem
							{
								Seq = i,
								Value = array1[i],
								EnumId = array1[i],
								Caption = new LocaleValue(ResManager.LoadKDString(array1[i], "120007000000200", SubSystemType.PLM, new object[0]))
							});
						}
						
					}
					comboFieldEditor1.SetComboItems(list);
                    if (k !=-1 && k<array1.Length-1)
                    {
                        this.View.Model.SetValue("F_26_KHDBBH", array1[k+1]);
                    }
                    else
                    {
						this.View.Model.SetValue("F_26_KHDBBH", array1[0]);
					}
                    if (bbh.Count > 0)
                    {
						this.View.Model.SetValue("F_260_DQKHDBB", bbh[0]["F_260_KHBB"]);
					}
				}
			}

		}
		public override void PreOpenForm(PreOpenFormEventArgs e)
		{
			long num = (e.OpenParameter.GetCustomParameter("CategoryId") == null) ? 0L : Convert.ToInt64(e.OpenParameter.GetCustomParameter("CategoryId"));
			this.isMaxVersion = Convert.ToBoolean(e.OpenParameter.GetCustomParameter("isMaxVersion"));
			this.objDyn = (DynamicObject)e.OpenParameter.GetCustomParameter("objDyn");
			this.objList = (List<DynamicObject>)e.OpenParameter.GetCustomParameter("objList");
			if (num < 1L || this.objDyn == null || this.objList == null)
			{
				e.Cancel = true;
				return;
			}
			this._currentCategroyId = num;
			base.PreOpenForm(e);
		}
		public override void ButtonClick(ButtonClickEventArgs e)
		{
			if (base.OperationRefused(e, false))
			{
				return;
			}
			string a;
			if ((a = e.Key.ToUpper()) != null)
			{
				if (!(a == "FMAXCONFIRMBTN") && !(a == "FMINCONFIRMBTN"))
				{
					if (a == "FMAXCANCELBTN" || a == "FMINCANCELBTN")
					{
						this.View.Close();
					}
				}
				else
				{
					if ("".Equals(this.PLMView.CurrentView.Model.GetValue("FUpgrade" + this.sequenceType + "Ver").ToString()))
					{
						this.View.ShowWarnningMessage("未选择任何版本数据！", "", MessageBoxOptions.OK, null, MessageBoxType.Advise);
					}
					else
					{
						this.SaveCustomizedPages(this.PLMContext, this.isMaxVersion, this._currentCategroyId);
					}
				}
			}
			base.ButtonClick(e);
		}
		public void SaveCustomizedPages(PLMContext ctx, bool isMaxVersion, long categroyId)
		{
			List<object> li = new List<object>();
			List<int> list = new List<int>();
			bool flag = Convert.ToBoolean(this.PLMView.CurrentView.Model.GetValue("FAutoUpgrade"));
			List<DynamicObject> list2 = new List<DynamicObject>();
			foreach (DynamicObject current in this.objList)
			{
				long num = Convert.ToInt64(current["Id"]);
				DynamicObject dynamicObject = (num != 0L) ? BaseObjectManager.Instance(this.PLMContext).Get(this.PLMContext, num) : null;
				DynamicObject item = dynamicObject ?? current;
				list2.Add(item);
			}
			foreach (DynamicObject current2 in list2)
			{
				int item2 = flag ? 2147483647 : (this.GetVersionIndex(ctx, this.PLMView.CurrentView.Model.GetValue("FUpgrade" + this.sequenceType + "Ver").ToString()) - Convert.ToInt32(current2[this.sequenceType + "Ver"]));
				list.Add(item2);
			}
			li.Add(list);
			li.Add(this.PLMView.CurrentView.Model.GetValue("F_26_KHDBBH"));
			this.View.ReturnToParentWindow(li);
			this.View.Close();
		}
		public override void AfterBindData(EventArgs e)
		{
			base.AfterBindData(e);
			if (this.sequence == null)
			{
				return;
			}
			int num = Convert.ToInt32(this.objDyn[this.sequenceType + "Ver"]);
			if (num > this.sequence.Length)
			{
				this.View.ShowErrMessage(string.Format("当前对象的版本序号[{0}]超过了当前业务类型的版本序列的最大序号[{1}]，请检查！", num, this.sequence.Length - 1), "", MessageBoxType.Notice);
				return;
			}
			if (num < 0)
			{
				this.View.ShowErrMessage(string.Format("当前对象的版本序号[{0}]小于0不符合规则，请检查！", num), "", MessageBoxType.Notice);
				return;
			}
			this.View.GetControl("FCurrent" + this.sequenceType + "Ver").SetValue(this.sequence[Convert.ToInt32(this.objDyn[this.sequenceType + "Ver"])]);
			this.View.GetControl("FCurrent" + this.sequenceType + "Ver").Enabled = false;
		}
		public override void DataChanged(DataChangedEventArgs e)
		{
			string a;
			if ((a = e.Field.Key.ToUpper()) != null && a == "FAUTOUPGRADE")
			{
				this.View.GetControl("FUpgrade" + this.sequenceType + "Ver").Enabled = !Convert.ToBoolean(e.NewValue);
			}
			base.DataChanged(e);
		}
	}
}

