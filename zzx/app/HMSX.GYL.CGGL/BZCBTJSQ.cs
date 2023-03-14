using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.ComponentModel;

namespace HMSX.GYL.CGGL
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("标准成本调价申请")]
    public class BZCBTJSQ : AbstractBillPlugIn
    {
        private bool isget = false;
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            long orid = this.Context.CurrentOrganizationInfo.ID;
            if (e.Field.Key == "F_260_WLBM" && orid == 100026)
            {
                try
                {
                    DynamicObject wlobj = (DynamicObject)this.View.Model.GetValue("F_260_WLBM", e.Row);
                    string wlbm = wlobj["Number"].ToString();
                    string sql = $@"/*dialect*/select top 1 case when a.FLCBZMBJ=0 then a.FNPIYPMBJ else a.FLCBZMBJ end 标准成本,isnull(HL.FEXCHANGERATE,1) 最新汇率,
isnull(b.FEXCHANGERATE,1) 当期汇率,a.F_260_BASEBB 币别 from HMD_t_Cust100157 a
left join T_BD_Rate b on a.FCREATEDATE between b.FBEGDATE and DATEADD(day,1,b.FENDDATE) and b.FCYTOID=1 
						and b.FCYFORID=a.F_260_BASEBB and b.FRATETYPEID=1 and b.FFORBIDSTATUS='A' and b.FDOCUMENTSTATUS='C'
left join(select FCYFORID,FEXCHANGERATE,ROW_NUMBER() over(partition by FCYFORID order by FENDDATE DESC) 顺序 
		from T_BD_Rate where FCYTOID=1 and FRATETYPEID=1 and FFORBIDSTATUS='A' and FDOCUMENTSTATUS='C') HL 
		on a.F_260_BASEBB=HL.FCYFORID and 顺序=1
where a.FFORBIDSTATUS='A' and a.FDOCUMENTSTATUS='C' and a.F_HMD_BASEWLBM={Convert.ToInt64(wlobj["Id"])} and a.FLCBZMBJ+a.FNPIYPMBJ>0
order by a.FCREATEDATE DESC";
                    DynamicObjectCollection obj = DBUtils.ExecuteDynamicObject(this.Context, sql);
                    if (obj.Count > 0)
                    {
                        this.View.Model.SetValue("F_PAEZ_BZCB", obj[0]["标准成本"], e.Row);
                        this.View.Model.SetValue("FBZCBBB", obj[0]["币别"], e.Row);
                        this.View.Model.SetValue("FBZCBHL", obj[0]["当期汇率"], e.Row);
                        this.View.Model.SetValue("FGYSXDJHL", obj[0]["最新汇率"], e.Row);
                    }
                    if (wlbm.StartsWith("260.02") || wlbm.StartsWith("260.03"))
                    {
                        this.View.Model.SetItemValueByNumber("F_PAEZ_CPBM", "260.02." + wlbm.Substring(7, 5), e.Row);
                    }
                }
                catch { return; }

            }
            else if (e.Field.Key == "F_PAEZ_CPBM" && orid == 100026)
            {
                try
                {
                    long cpid = Convert.ToInt64(((DynamicObject)this.View.Model.GetValue("F_PAEZ_CPBM", e.Row))["Id"]);
                    long zid = Convert.ToInt64(((DynamicObject)this.View.Model.GetValue("F_260_WLBM", e.Row))["Id"]);
                    string sql = $@"/*dialect*/select 标准成本,当期汇率1,币别1,客户,单价,当期汇率2,币别2,最新汇率 from
--标准成本
(select top 1 case when a.FLCBZMBJ=0 then a.FNPIYPMBJ else a.FLCBZMBJ end 标准成本,isnull(b.FEXCHANGERATE,1) 当期汇率1,isnull(HL.FEXCHANGERATE,1) 最新汇率,
a.F_260_BASEBB 币别1,a.F_HMD_BASEWLBM 物料 from HMD_t_Cust100157 a
left join T_BD_Rate b on a.FCREATEDATE between b.FBEGDATE and DATEADD(day,1,b.FENDDATE) and b.FCYTOID=1 and b.FCYFORID=a.F_260_BASEBB and b.FRATETYPEID=1
left join(select FCYFORID,FEXCHANGERATE,ROW_NUMBER() over(partition by FCYFORID order by FENDDATE DESC) 顺序 
		from T_BD_Rate where FCYTOID=1 and FRATETYPEID=1 and FFORBIDSTATUS='A' and FDOCUMENTSTATUS='C') HL 
		on a.F_260_BASEBB=HL.FCYFORID and 顺序=1
where a.FFORBIDSTATUS='A' and a.FDOCUMENTSTATUS='C' and a.F_HMD_BASEWLBM={cpid} and a.FLCBZMBJ+a.FNPIYPMBJ>0
order by a.FCREATEDATE DESC) A
full join
--销售价
(select top 1 FMATERIALID 物料,FCUSTID 客户,FPRICE 单价,isnull(h.FEXCHANGERATE,1) 当期汇率2,cw.FSETTLECURRID 币别2 from T_SAL_ORDER a
inner join T_SAL_ORDERFIN cw on a.FID=cw.FID
inner join T_SAL_ORDERENTRY b on a.FID=b.FID
inner join T_SAL_ORDERENTRY_F c on b.FENTRYID=c.FENTRYID
left join T_BD_Rate h on a.FCREATEDATE between h.FBEGDATE and DATEADD(day,1,h.FENDDATE) and h.FCYTOID=1 and h.FCYFORID=cw.FSETTLECURRID and h.FRATETYPEID=1
where FSALEORGID=100026 and a.F_260_SFNPI='批量' and a.FDOCUMENTSTATUS='C' and F_260_BASEXXDDLX=0 and FMATERIALID={cpid} and DATEDIFF(MONTH,a.FDATE,GETDATE())<=6
order by ROW_NUMBER() over(partition by FCUSTID order by FDATE DESC),FPRICE*isnull(h.FEXCHANGERATE,1))B on A.物料=B.物料";
                    DynamicObjectCollection obj = DBUtils.ExecuteDynamicObject(this.Context, sql);
                    if (obj.Count > 0)
                    {
                        this.View.Model.SetValue("F_PAEZ_GGQCPBZJ", obj[0]["标准成本"], e.Row);
                        this.View.Model.SetValue("FCPBZJBB", obj[0]["币别1"], e.Row);
                        this.View.Model.SetValue("FCPBZJHL", obj[0]["当期汇率1"], e.Row);
                        this.View.Model.SetValue("FCPBZJBWB", Convert.ToDouble(obj[0]["标准成本"]) * Convert.ToDouble(obj[0]["当期汇率1"]), e.Row);
                        this.View.Model.SetValue("F_PAEZ_ZDSJKH", obj[0]["客户"], e.Row);
                        this.View.Model.SetValue("F_PAEZ_CPXSJ", obj[0]["单价"], e.Row);
                        this.View.Model.SetValue("FCPXSJBB", obj[0]["币别2"], e.Row);
                        this.View.Model.SetValue("FCPXSHL", obj[0]["当期汇率2"], e.Row);
                        this.View.Model.SetValue("FCPXSJBWB", Convert.ToDouble(obj[0]["单价"]) * Convert.ToDouble(obj[0]["当期汇率2"]), e.Row);
                        this.View.Model.SetValue("FGGHCPBZJHL", obj[0]["最新汇率"], e.Row);                        
                    }
                    //获取用量
                    string ylsql = $"/*dialect*/select FID from T_ENG_BOM where FDOCUMENTSTATUS='C' and FFORBIDSTATUS = 'A' and FMATERIALID={cpid} order by FCREATEDATE DESC";
                    DynamicObjectCollection objs = DBUtils.ExecuteDynamicObject(this.Context, ylsql);
                    foreach (DynamicObject bom in objs)
                    {
                        long bomid = Convert.ToInt64(bom["FID"]);
                        double YL = Recursion(bomid, cpid, zid);
                        if (isget)
                        {
                            this.View.Model.SetValue("FYLBL", YL, e.Row);
                            isget = false;
                            break;
                        }
                    }
                }
                catch { return; }
            }
        }
        //递归
        private double Recursion(long bomid, long fid, long zid)
        {
            double fz = 1;
            string bomsql;
            if (bomid > 0)
            {
                bomsql = $@"/*dialect*/select b.FMATERIALID 子物料,FBOMID 子BOM,FNUMERATOR 分子
                from T_ENG_BOM a inner join T_ENG_BOMCHILD b on a.FID=b.FID
                where a.FDOCUMENTSTATUS='C' and FFORBIDSTATUS = 'A' and a.FID={bomid}";
            }
            else
            {
                bomsql = $@"/*dialect*/select b.FMATERIALID 子物料,FBOMID 子BOM,FNUMERATOR 分子
                from (select top 1 FID from T_ENG_BOM where FDOCUMENTSTATUS='C' and FFORBIDSTATUS = 'A' and FMATERIALID={fid} order by FCREATEDATE DESC) a 
                inner join T_ENG_BOMCHILD b on a.FID=b.FID";
            }
            DynamicObjectCollection objects = DBUtils.ExecuteDynamicObject(this.Context, bomsql);
            foreach (DynamicObject obj in objects)
            {
                long zwl = Convert.ToInt64(obj["子物料"]);
                long zbom = Convert.ToInt64(obj["子BOM"]);
                fz = Convert.ToDouble(obj["分子"]);
                if (zid == zwl)
                {
                    isget = true;
                    break;
                }
                else
                {
                    fz *= Recursion(zbom, zwl, zid);
                }
                if (isget) { break; }
            }
            return fz;
        }
    }
}