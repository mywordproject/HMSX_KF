using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace HMSX.GYL.KCGL
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("双新操作权限控制")]
    public class ZXBDCZQXKZ: AbstractOperationServicePlugIn
    {
        List<string> sjs = new List<string>();
        List<Dictionary<string, string>> zds=new List<Dictionary<string, string>>();
        //字段去重类
        private class zdCompareByBS : IEqualityComparer<Dictionary<string,string>>
        {
            public bool Equals(Dictionary<string, string> x, Dictionary<string, string> y)
            {
                if (x == null || y == null){return false;}
                if (x["标识"] == y["标识"]){return true;}
                else { return false; }
            }
            public int GetHashCode(Dictionary<string, string> obj)
            {
                if (obj == null){return 0;}
                else{return obj["标识"].GetHashCode();}
            }
        }
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            if (this.Context.CurrentOrganizationInfo.ID == 100026)
            {
                string sql = $@"/*dialect*/select vi.fmasterid 数据,yw.FQXX 权限,zd.FZDBS 标识,zd.FSTSX 实体属性,zd.FORMM 实体名 
                    from PAEZ_t_Cust100383 a
                    --用户对应数据范围
                    inner join PAEZ_t_Cust_Entry100437 yh on a.FID=yh.FID
                    inner join PAEZ_t_Cust_Entry100438 sj on yh.FEntryID=sj.FEntryID
                    inner join V_SXV0001 vi on vi.fformid=a.FSJLX and vi.fitemid=sj.FSJZ
                    --表单及控制字段
                    inner join PAEZ_t_Cust_Entry100439 yw on yw.FID=a.FID
                    inner join PAEZ_t_Cust_Entry100440 zd on zd.FEntryID=yw.FEntryID
                    where yh.FUSER={this.Context.UserId} and yw.FDJM='{this.BusinessInfo.GetForm().Id}'";
                DynamicObjectCollection objs = DBUtils.ExecuteDynamicObject(this.Context, sql);
                if (objs.Count > 0)
                {                    
                    List<string> sj = new List<string>();
                    List<Dictionary<string, string>> zd = new List<Dictionary<string, string>>();
                    foreach (DynamicObject obj in objs)
                    {
                        if (!obj["权限"].ToString().Contains(","+this.FormOperation.OperationName)) 
                        {
                            throw new Exception("您没有"+ this.FormOperation.OperationName+"权限!");
                        }
                        Dictionary<string, string> zdxx = new Dictionary<string, string>();
                        sj.Add(obj["数据"].ToString());
                        zdxx.Add("标识", obj["标识"].ToString());
                        zdxx.Add("实体属性", obj["实体属性"].ToString());
                        zdxx.Add("实体名", obj["实体名"].ToString());
                        zd.Add(zdxx);
                    }
                    sjs = sj.Distinct().ToList();
                    zds = zd.Distinct(new zdCompareByBS()).ToList();
                    foreach (Dictionary<string, string> zda in zds)
                    {
                        e.FieldKeys.Add(zda["标识"]);
                    }
                }               
            }
        }
        public override void BeginOperationTransaction(BeginOperationTransactionArgs e)
        {
            base.BeginOperationTransaction(e);
            if(this.Context.CurrentOrganizationInfo.ID==100026 && sjs.Count>0 && zds.Count > 0)
            {
                List<string> ckj = new List<string>();
                bool sf= true;
                foreach(Dictionary<string, string> zd in zds)
                {
                    string stm = zd["实体名"];
                    string stsx = zd["实体属性"];
                    List<string> ck = new List<string>();
                    foreach(DynamicObject entity in e.DataEntitys)
                    {
                        //单据头字段
                        if (stm== entity.DynamicObjectType.Name)
                        {
                            var obj = entity[stsx];
                            string id = obj == null ? "0" : ((DynamicObject)obj)["Id"].ToString();
                            if (!sjs.Contains(id))
                            {
                                string name = ((DynamicObject)obj)["Name"].ToString();
                                ck.Add(name);
                            }
                        }
                        //单据体字段
                        else
                        {
                            DynamicObjectCollection entrys = (DynamicObjectCollection)entity[stm];
                            foreach (DynamicObject entry in entrys)
                            {
                                var obj = entry[stsx];
                                string id = obj == null ? "0" : ((DynamicObject)obj)["Id"].ToString();
                                if (!sjs.Contains(id))
                                {
                                    string name = ((DynamicObject)obj)["Name"].ToString();
                                    ck.Add(name);
                                }
                            }
                        }
                    }
                    if (ck.Count == 0)
                    {
                        sf = false;
                        break;
                    }
                    else
                    {
                        ckj=ckj.Union(ck).ToList();
                    }
                }
                if (sf)
                {
                    string msg = "";
                    foreach(string err in ckj)
                    {
                        msg += err + "、";
                    }
                    throw new Exception($"您没有{msg.Substring(0,msg.Length-1)}的数据{this.FormOperation.OperationName}权限！");
                }
            }
        }
    }
}
