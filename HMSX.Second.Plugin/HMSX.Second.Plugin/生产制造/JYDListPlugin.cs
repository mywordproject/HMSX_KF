using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.List.PlugIn;
using Kingdee.BOS.JSON;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.WebApi.FormService;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.生产制造
{
    [Description("检验单--批量修改")]
    //热启动,不用重启IIS
    [Kingdee.BOS.Util.HotUpdate]
    public class JYDListPlugin : AbstractListPlugIn
    {
        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);
            if (e.BarItemKey.Equals("SLSB_PLXG"))
            {
                if (Context.CurrentOrganizationInfo.ID == 100026)
                {
                    //选择的行,获取所有信息,放在listcoll里面
                    ListSelectedRowCollection listcoll = this.ListView.SelectedRowsInfo;
                    //定义一个字符串数组,接收分录FID的值
                    string[] listKey = listcoll.GetEntryPrimaryKeyValues();
                    if (listKey.Length == 0)
                    {
                        throw new KDBusinessException("", "请选择需要批量修改的行！");
                    }

                    foreach (var list in listcoll)
                    {

                        if (list.DataRow["FINSPECTORGID"].ToString() != "100026")
                        {
                            return;
                        }
                        if (list.DataRow["FDOCUMENTSTATUS"].ToString() == "C")
                        {
                            throw new KDBusinessException("", "批量修改，单据状态不能为已审核！");
                        }

                    }
                    DynamicFormShowParameter parameter = new DynamicFormShowParameter();
                    parameter.OpenStyle.ShowType = ShowType.Floating;
                    parameter.FormId = "PAEZ_PLXG";//PAEZ_PLXG正式；SLSB_PLXG测试
                    parameter.MultiSelect = false;
                    //获取返回的值
                    this.View.ShowForm(parameter, delegate (FormResult result)
                    {
                        string[] date = (string[])result.ReturnData;
                        if (date != null && date[0] == "1")
                        {
                            foreach (string key in listKey)
                            {
                                string upsql = $@"update T_QM_INSPECTBILLENTRY set F_260_BHGMS='{date[2]}' where FENTRYID={key}";
                                DBUtils.ExecuteDynamicObject(Context, upsql);
                            }
                            this.View.ShowMessage("批量修改成功！");
                            this.View.Refresh();
                        }
                        else if (date != null && date[0] == "2")
                        {
                            foreach (string key in listKey)
                            {
                                string cxsql = $@"select * from T_QM_IBPOLICYDETAIL_L WHERE FDetailID IN (SELECT FDetailID FROM T_QM_IBPOLICYDETAIL WHERE FENTRYID='{key}')";
                                var cx = DBUtils.ExecuteDynamicObject(Context, cxsql);
                                if (cx.Count > 0)
                                {
                                    string upsql = $@"update T_QM_IBPOLICYDETAIL_L set FMEMO='{date[3]}' where FDetailID IN (SELECT FDetailID FROM T_QM_IBPOLICYDETAIL WHERE FENTRYID={key})";
                                    DBUtils.ExecuteDynamicObject(Context, upsql);
                                }
                                else
                                {
                                    string pkidsql = $@"select top 1 * from T_QM_IBPOLICYDETAIL_L order by fpkid desc";
                                    var pkid = DBUtils.ExecuteDynamicObject(Context, pkidsql);
                                    string fdidsql = $@"SELECT FDetailID FROM T_QM_IBPOLICYDETAIL WHERE FENTRYID='{key}'";
                                    var fdid = DBUtils.ExecuteDynamicObject(Context, fdidsql);

                                    string insertsql = $@"insert into T_QM_IBPOLICYDETAIL_L values({Convert.ToInt32(pkid[0]["FPKID"].ToString()) + 1},{Convert.ToInt32(fdid[0]["FDETAILID"].ToString())},2052,'{date[3]}')";
                                    DBUtils.Execute(Context, insertsql);
                                }
                            }
                            this.View.ShowMessage("批量修改成功！");
                            this.View.Refresh();
                        }
                        else if (date != null && date[0] == "3")
                        {
                            foreach (var lists in listcoll)
                            {
                                string fid = "";
                                string fentryid = "";
                                List<string> FPolicyDetails = new List<string>();
                                if (lists.FieldValues.Count >= 2)
                                {
                                    try
                                    {
                                        fid = lists.FieldValues["FBillHead"].ToString();
                                        fentryid = lists.FieldValues["FEntity"].ToString();
                                        FPolicyDetails.Add(lists.FieldValues["FPolicyDetail"].ToString());
                                    }
                                    catch
                                    {
                                        string cxsql = $@"select c.* from T_QM_INSPECTBILL a
                                       inner join T_QM_INSPECTBILLENTRY b on a.fid=b.fid
                                       left join T_QM_IBPOLICYDETAIL c on b.FENTRYID=c.FENTRYID
                                       where a.FID={fid} and b.FENTRYID={fentryid}";
                                        var cxs = DBUtils.ExecuteDynamicObject(Context, cxsql);
                                        foreach (var cx in cxs)
                                        {
                                            FPolicyDetails.Add(cx["FDETAILID"].ToString());
                                        }
                                    }
                                    finally
                                    {
                                        JObject json = new JObject();
                                        json.Add("IsDeleteEntry", false);
                                        JObject model = new JObject();
                                        JArray FEntity = new JArray();
                                        JObject Entity = new JObject();
                                        Entity.Add("FENTRYID", fentryid);
                                        JArray FPolicyDetail = new JArray();
                                        foreach (var id in FPolicyDetails)
                                        {

                                            JObject FDetailID = new JObject();
                                            FDetailID.Add("FDetailID", id);
                                            FDetailID.Add("FPolicyStatus", date[4]);//状态
                                            FDetailID.Add("FUsePolicy", date[1]);//使用决策
                                            FPolicyDetail.Add(FDetailID);
                                        }

                                        Entity.Add("FPolicyDetail", FPolicyDetail);
                                        FEntity.Add(Entity);
                                        model.Add("FID", fid);
                                        JObject FSourceOrgId = new JObject();
                                        FSourceOrgId.Add("FNumber", 260);//来源组织
                                        model.Add("FSourceOrgId", FSourceOrgId);
                                        JObject FInspectOrgId = new JObject();
                                        FInspectOrgId.Add("FNumber", 260);//质检组织
                                        model.Add("FInspectOrgId  ", FInspectOrgId);
                                        model.Add("FEntity", FEntity);
                                        json.Add("Model", model);
                                        var results = WebApiServiceCall.Save(this.Context, "QM_InspectBill", json.ToString());
                                        bool isSuccess = Convert.ToBoolean(JObject.Parse(JsonConvert.SerializeObject(results))["Result"]["ResponseStatus"]["IsSuccess"].ToString());
                                        string c = KDObjectConverter.SerializeObject(results);
                                        if (isSuccess)
                                        {

                                        }
                                        else
                                        {
                                            string Errors = JObject.Parse(JsonConvert.SerializeObject(results))["Result"]["ResponseStatus"]["Errors"].ToString();
                                            var JErrors = JArray.Parse(Errors);
                                            string message = "";
                                            foreach (var Error in JErrors)
                                            {
                                                message += ((JObject)Error)["Message"].ToString() + ",";
                                            }
                                            throw new KDBusinessException("", message);
                                        }
                                    }
                                }

                                //string cxsql = $@"select FENTRYID,FINSPECTRESULT from T_QM_INSPECTBILLENTRY_A WHERE FENTRYID={key}";
                                //var CX = DBUtils.ExecuteDynamicObject(Context, cxsql);
                                //string upsql1 = $@"update T_QM_INSPECTBILLENTRY_A set FINSPECTRESULT='{date[4]}' where FENTRYID={key}";
                                //DBUtils.ExecuteDynamicObject(Context, upsql1);
                                //if (date[1] == "I" || date[1] == "K")
                                //{
                                //    string BLCLsql = $@"update T_QM_IBPOLICYDETAIL set FUSEPOLICY='{date[1]}',FSTATUS='{date[4]}',FISDEFECTPROCESS=1  where FENTRYID={key}";
                                //    DBUtils.ExecuteDynamicObject(Context, BLCLsql);
                                //}
                                //else
                                //{
                                //    string upsql = $@"update T_QM_IBPOLICYDETAIL set FUSEPOLICY='{date[1]}',FSTATUS='{date[4]}' ,FISDEFECTPROCESS=0  where FENTRYID={key}";
                                //    DBUtils.ExecuteDynamicObject(Context, upsql);
                                //}
                                //if (date[4] == "2" && CX[0]["FINSPECTRESULT"].ToString()=="1")
                                //{
                                //    string slsql = $@"update T_QM_INSPECTBILLENTRY set FQUALIFIEDQTY = 0,FBASEQUALIFIEDQTY=0,FBASEUNQUALIFIEDQTY=FQUALIFIEDQTY, FUNQUALIFIEDQTY = FQUALIFIEDQTY where FENTRYID = {key}";
                                //    DBUtils.ExecuteDynamicObject(Context, slsql);
                                //    string slsql1 = $@"update T_QM_INSPECTBILLENTRY_A set FBASEACCEPTQTY=0,FBASEWBINSPECTQTY=0 where FENTRYID = {key}";
                                //    DBUtils.ExecuteDynamicObject(Context, slsql1);
                                //}
                                //else if(date[4] == "1" && CX[0]["FINSPECTRESULT"].ToString() == "2")
                                //{
                                //    string slsql = $@"update T_QM_INSPECTBILLENTRY set FQUALIFIEDQTY = FUNQUALIFIEDQTY, FBASEQUALIFIEDQTY=FUNQUALIFIEDQTY,FUNQUALIFIEDQTY = 0,FBASEUNQUALIFIEDQTY=0  where FENTRYID = {key}";
                                //    DBUtils.ExecuteDynamicObject(Context, slsql);
                                //    string slsql1 = $@"update T_QM_INSPECTBILLENTRY_A set FBASEACCEPTQTY=FBASEINSPECTQTY,FBASEWBINSPECTQTY=FBASEINSPECTQTY where FENTRYID = {key}";
                                //    DBUtils.ExecuteDynamicObject(Context, slsql1);
                                //}
                            }
                            this.View.ShowMessage("批量修改成功！");
                            this.View.Refresh();
                        }
                        else if (date != null && date[0] == "4")
                        {
                            foreach (var list in listcoll)
                            {
                                string upsql1 = $@"update T_QM_INSPECTBILL set FINSPECTORID='{date[5]}' where FBILLNO='{list.DataRow["FBILLNO"].ToString()}'";
                                DBUtils.ExecuteDynamicObject(Context, upsql1);

                            }
                            this.View.ShowMessage("批量修改成功！");
                            this.View.Refresh();
                        }
                        else if (date != null && date[0] == "5")
                        {
                            foreach (string list in listKey)
                            {
                                string upsql1 = $@"update T_QM_INSPECTBILLENTRY set F_260_RBJSYCMS='{date[6]}' where FENTRYID={list}";
                                DBUtils.ExecuteDynamicObject(Context, upsql1);
                            }
                            this.View.ShowMessage("批量修改成功！");
                            this.View.Refresh();
                        }
                        else if (date != null && date[0] == "6")
                        {
                            foreach (var lists in listcoll)
                            {
                                if (lists.FieldValues.Count >= 2)
                                {
                                    string fid = lists.FieldValues["FBillHead"].ToString();
                                    string fentryid = lists.FieldValues["FEntity"].ToString();
                                    JObject json = new JObject();
                                    json.Add("IsDeleteEntry", false);
                                    JObject model = new JObject();
                                    JArray FEntity = new JArray();
                                    JObject Entity = new JObject();
                                    Entity.Add("FENTRYID", fentryid);
                                    Entity.Add("FInspectResult", date[7]);
                                    FEntity.Add(Entity);
                                    model.Add("FID", fid);
                                    JObject FSourceOrgId = new JObject();
                                    FSourceOrgId.Add("FNumber", 260);//来源组织
                                    model.Add("FSourceOrgId", FSourceOrgId);
                                    JObject FInspectOrgId = new JObject();
                                    FInspectOrgId.Add("FNumber", 260);//质检组织
                                    model.Add("FInspectOrgId  ", FInspectOrgId);
                                    model.Add("FEntity", FEntity);
                                    json.Add("Model", model);
                                    var results = WebApiServiceCall.Save(this.Context, "QM_InspectBill", json.ToString());
                                    bool isSuccess = Convert.ToBoolean(JObject.Parse(JsonConvert.SerializeObject(results))["Result"]["ResponseStatus"]["IsSuccess"].ToString());
                                    string c = KDObjectConverter.SerializeObject(results);
                                    if (isSuccess)
                                    {

                                    }
                                    else
                                    {
                                        string Errors = JObject.Parse(JsonConvert.SerializeObject(results))["Result"]["ResponseStatus"]["Errors"].ToString();
                                        var JErrors = JArray.Parse(Errors);
                                        string message = "";
                                        foreach (var Error in JErrors)
                                        {
                                            message += ((JObject)Error)["Message"].ToString() + ",";
                                        }
                                        throw new KDBusinessException("", message);
                                    }
                                }
                            }
                            this.View.ShowMessage("批量修改成功！");
                            this.View.Refresh();
                        }
                    });
                }
            }
        }

    }
}
