using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
namespace Kingdee.K3.GY.PD.Business.ServicePlugin
{
	[Description("盘点")]
	[HotUpdate]
	public class OnCreatePDListData : AbstractOperationServicePlugIn
	{
		public override void EndOperationTransaction(EndOperationTransactionArgs e)
		{
			base.EndOperationTransaction(e);
			List<string> list = new List<string>();
			DynamicObject[] dataEntitys = e.DataEntitys;
			for (int i = 0; i < dataEntitys.Length; i++)
			{
				DynamicObject dynamicObject = dataEntitys[i];
				if (dynamicObject != null)
				{
					list.Add(dynamicObject["Id"].ToString());
				}
			}
			DynamicObjectCollection dynamicObjectCollection = DBUtils.ExecuteDynamicObject(Context, string.Format("select FID,FBILLNO,F_PAEZ_TEXT FName,FASSETORGID FOrgID from PAEZ_t_CustRFIDSCHEME where FID in({0})", string.Join(",", list)), null, null, CommandType.Text, new SqlParam[0]);
			DataTable headDt = this.GetHeadDt();
			DataTable entryDt = this.GetEntryDt();
			DataTable headOrg = this.GetHeadOrg();
			this.LoadHeadDtData(headDt, dynamicObjectCollection);
			foreach (DynamicObject current in dynamicObjectCollection)
			{
				DynamicObjectCollection dynamicObjectCollection2 = DBUtils.ExecuteDynamicObject(Context, string.Format("select F_PAEZ_OrgId from PAEZ_t_CustRFIDSCHEMEOrg where FID={0}", current["FID"]), null, null, CommandType.Text, new SqlParam[0]);
				DynamicObjectCollection dynamicObjectCollection3 = DBUtils.ExecuteDynamicObject(Context, string.Format("select FASSETTYPEID from PAEZ_t_CustRFIDSCHEMEAT where FID={0}", current["FID"]), null, null, CommandType.Text, new SqlParam[0]);
				DynamicObjectCollection dynamicObjectCollection4 = DBUtils.ExecuteDynamicObject(Context, string.Format("select FUSEDEPTID from PAEZ_t_CustRFIDSCHEMEDP where FID={0}", current["FID"]), null, null, CommandType.Text, new SqlParam[0]);
				DynamicObjectCollection dynamicObjectCollection5 = DBUtils.ExecuteDynamicObject(Context, string.Format("select FPOSITIONID from PAEZ_t_CustRFIDSCHEMEPO where FID={0}", current["FID"]), null, null, CommandType.Text, new SqlParam[0]);
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append("select\r\n                            0 FID\r\n                            ,0 FEntryID\r\n                            ,0 FSEQ\r\n                            ,0 FINITCHECKERID\t\t   --盘点人\r\n                            ,t1.FOWNERORGID FOWNERORGID--货主组织\r\n                            ,t1.FASSETTYPEID FASSETTYPEID--资产类别\r\n                            ,t1.FNUMBER FCARDNUMBER--卡片编码\r\n                            ,t2.FBARCODE FRFIDNO--RFID编码\r\n                            ,t2.FASSETNO FASSETNUMBER--资产编码\r\n                            ,t5.FNAME FASSETNAME            --资产名称\r\n                            ,t2.FSPECIFICATION FSPECIFICATION--规格型号\r\n                            ,t1.FASSETSTATUSID FASSETSTATUSID--资产状态\r\n                            ,t1.FUNITID FASSETUNITID--单位\r\n\r\n\r\n                            ,t3.FORGVAL FORGVAL--资产原值\r\n                            ,t3.FORGVAL-t6.FNETVALUE FACCUMDEPR           --累计折旧\r\n                            ,t6.FNETVALUE FBOOKVALUE           --资产净值\r\n                            ,t2.FQUANTITY FBOOKQTY\t\t   --账存数量\r\n                            ,0 FINITQTY\t\t   --盘点数量\r\n                            ,(t2.FQUANTITY*-1) FVARIANCE\t\t   --差异\r\n                            ,t2.FPOSITIONID FBOOKPOSITIONID\t\t   --账存资产位置\r\n                            ,t4.FUSEDEPTID\tFBOOKUSEDEPTID\t   --账存使用部门\r\n                            ,t4.FUSERID\tFBOOKUSERID\t   --账存使用人\r\n\r\n\r\n                            ,0 FINITPOSITIONID\t\t   --盘点位置\r\n                            ,0 FINITUSEDEPTID\t\t   --盘点使用部门\r\n                            ,null FINITDATE\t\t   --盘点日期\r\n                            ,'' FENTRYDESCRIPTION --备注\r\n                            ,0 FINITUSERID\t\t   --盘点使用人\r\n                            from t_fa_card t1\r\n                            inner join T_FA_CARDDETAIL t2 on t1.FALTERID=t2.FALTERID\r\n                            left join T_FA_FINANCE t3 on t1.FALTERID=t3.FALTERID and t3.FACCTPOLICYID=1--会计政策\r\n                            left join t_fa_allocation t4 on t1.FALTERID=t4.FALTERID and t4.FSEQ=1\r\n                            left join t_fa_card_l t5 on t1.FALTERID=t5.FALTERID and t5.FLOCALEID=2052\r\n                            left join v_fa_lastbalance t6 on t1.FALTERID=t6.FALTERID and t3.FFINANCEID=t6.FFINANCEID\r\n                            where t1.FISNEWREC=2");
				if (dynamicObjectCollection2.Count > 0)
				{
					List<string> list2 = new List<string>();
					foreach (DynamicObject current2 in dynamicObjectCollection2)
					{
						list2.Add(current2["F_PAEZ_OrgId"].ToString());
					}
					stringBuilder.Append(string.Format(" and t1.FASSETORGID in ({0})", string.Join(",", list2)));
				}
				if (dynamicObjectCollection3.Count<DynamicObject>() > 0)
				{
					List<string> list3 = new List<string>();
					foreach (DynamicObject current3 in dynamicObjectCollection3)
					{
						list3.Add(current3["FASSETTYPEID"].ToString());
					}
					stringBuilder.Append(string.Format(" and t1.FASSETTYPEID in ({0})", string.Join(",", list3)));
				}
				if (dynamicObjectCollection4.Count<DynamicObject>() > 0)
				{
					List<string> list4 = new List<string>();
					foreach (DynamicObject current4 in dynamicObjectCollection4)
					{
						list4.Add(current4["FUSEDEPTID"].ToString());
					}
					stringBuilder.Append(string.Format(" and t4.FUSEDEPTID in ({0})", string.Join(",", list4)));
				}
				if (dynamicObjectCollection5.Count<DynamicObject>() > 0)
				{
					List<string> list5 = new List<string>();
					foreach (DynamicObject current5 in dynamicObjectCollection5)
					{
						list5.Add(current5["FPOSITIONID"].ToString());
					}
					stringBuilder.Append(string.Format(" and t2.FPOSITIONID in ({0})", string.Join(",", list5)));
                }
                DynamicObjectCollection dynamicObjects = DBUtils.ExecuteDynamicObject(Context, stringBuilder.ToString(), null, null, CommandType.Text, new SqlParam[0]);
                this.LoadEntryDtData(entryDt, dynamicObjects, Convert.ToInt32(current["FID"]));
				this.LoadHeadOrgDtData(headOrg, dynamicObjectCollection2, Convert.ToInt32(current["FID"]));
			}
			DBUtils.BulkInserts(Context, headDt);
			DBUtils.BulkInserts(Context, entryDt);
			DBUtils.BulkInserts(Context, headOrg);
		}
		private DataTable GetHeadDt()
		{
			return new DataTable
			{
				TableName = "PAEZ_t_RFIDSCHEMERPT",
				Columns =
				{
					"FID",
					"FBILLNO",
					"FDOCUMENTSTATUS",
					"FNAME",
					"F_PAEZ_ORGID",
					"F_PAEZ_ORGID1",
					"FCREATORID",
					"FCREATEDATE",
					"FMODIFIERID",
					"FMODIFYDATE"
				}
			};
		}
		private DataTable GetEntryDt()
		{
			return new DataTable
			{
				TableName = "PAEZ_t_RFIDSCHEMERPTENTRY",
				Columns =
				{
					"FID",
					"FEntryID",
					"FSEQ",
					"FINITCHECKERID",
					"FOWNERORGID",
					"FASSETTYPEID",
					"FCARDNUMBER",
					"FRFIDNO",
					"FASSETNUMBER",
					"FASSETNAME",
					"FSPECIFICATION",
					"FASSETSTATUSID",
					"FASSETUNITID",
					"FORGVAL",
					"FACCUMDEPR",
					"FBOOKVALUE",
					"FBOOKQTY",
					"FINITQTY",
					"FVARIANCE",
					"FBOOKPOSITIONID",
					"FBOOKUSEDEPTID",
					"FBOOKUSERID",
					"FINITPOSITIONID",
					"FINITUSEDEPTID",
					"FINITDATE",
					"FENTRYDESCRIPTION",
					"FINITUSERID",
					"FISKEY"
				}
			};
		}
		private DataTable GetHeadOrg()
		{
			return new DataTable
			{
				TableName = "PAEZ_t_RFIDSCHEMERPTOrg",
				Columns =
				{
					"FPKID",
					"FID",
					"F_PAEZ_USEORGID"
				}
			};
		}
		private void LoadHeadDtData(DataTable dt, DynamicObjectCollection dynamicObjects)
		{
			if (dynamicObjects.Count > 0)
			{
				DateTime now = DateTime.Now;
				dt.BeginLoadData();
				foreach (DynamicObject current in dynamicObjects)
				{
					dt.LoadDataRow(new List<object>
					{
						current["FID"],
						current["FBILLNO"],
						"A",
						current["FName"],
						current["FOrgID"],
					   this.Context.CurrentOrganizationInfo.ID,
					  this.Context.UserId,
						now,
						0,
						now
					}.ToArray(), true) ;
				}
				dt.EndLoadData();
			}
		}
		private void LoadEntryDtData(DataTable dt, DynamicObjectCollection dynamicObjects, int FID)
		{
			if (dynamicObjects.Count > 0)
			{
				int num = 0;
				int[] sequenceInt = DBServiceHelper.GetSequenceInt32(Context, "PAEZ_t_RFIDSCHEMERPTENTRY", dynamicObjects.Count);
				dt.BeginLoadData();
				foreach (DynamicObject current in dynamicObjects)
				{
					dt.LoadDataRow(new List<object>
					{
						FID,
						sequenceInt[num],
						num + 1,
						current["FINITCHECKERID"],
						current["FOWNERORGID"],
						current["FASSETTYPEID"],
						current["FCARDNUMBER"],
						current["FRFIDNO"],
						current["FASSETNUMBER"].ToString(),
						current["FASSETNAME"].ToString(),
						current["FSPECIFICATION"].ToString(),
						current["FASSETSTATUSID"].ToString(),
						current["FASSETUNITID"],
						current["FORGVAL"],
						current["FACCUMDEPR"],
						current["FBOOKVALUE"],
						current["FBOOKQTY"],
						current["FINITQTY"],
						current["FVARIANCE"],
						current["FBOOKPOSITIONID"],
						current["FBOOKUSEDEPTID"],
						current["FBOOKUSERID"],
						current["FINITPOSITIONID"],
						current["FINITUSEDEPTID"],
						null,
						current["FENTRYDESCRIPTION"],
						current["FINITUSERID"],
						0
					}.ToArray(), true);
					num++;
				}
				dt.EndLoadData();
			}
		}
		private void LoadHeadOrgDtData(DataTable dt, DynamicObjectCollection dynamicObjects, int FID)
		{
			if (dynamicObjects.Count > 0)
			{
				int num = 0;
				int[] sequenceInt = DBServiceHelper.GetSequenceInt32(Context, "PAEZ_t_RFIDSCHEMERPTOrg", dynamicObjects.Count);
				dt.BeginLoadData();
				foreach (DynamicObject current in dynamicObjects)
				{
					dt.LoadDataRow(new List<object>
					{
						sequenceInt[num],
						FID,
						current["F_PAEZ_OrgId"]
					}.ToArray(), true);
					num++;
				}
				dt.EndLoadData();
			}
		}
	}
}
