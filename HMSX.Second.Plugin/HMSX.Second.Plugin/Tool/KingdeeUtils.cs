using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin.Tool
{
    public class KingdeeUtils : AbstractBillPlugIn
    {
        /// <summary>
        /// 获取单据字段
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        public string GetVlue(string Key)
        {
            string x = this.Model.GetValue(Key) == null ? "" : this.Model.GetValue(Key).ToString();
            return x;
        }
        public string GetVlue(string Key, int row)
        {
            string x = this.Model.GetValue(Key, row) == null ? "" : this.Model.GetValue(Key, row).ToString();
            return x;
        }
        /// <summary>
        /// 获取单据基础资料
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        public string GetVlueItem(string Key, string lx)
        {
            string x = "";
            if (this.Model.GetValue(Key) != null)
            {
                if (lx == "Id")
                {
                    x= ((DynamicObject)this.Model.GetValue(Key))["Id"].ToString();
                }
                else if (lx == "Number")
                {
                    x = ((DynamicObject)this.Model.GetValue(Key))["Number"].ToString();
                }
                if (lx == "Name")
                {
                    x = ((DynamicObject)this.Model.GetValue(Key))["Name"].ToString();
                }
            }
            return x;
        }
        public string GetVlueItem(string Key,int row, string lx)
        {
            string x = "";
            if (this.Model.GetValue(Key,row) != null)
            {
                if (lx == "Id")
                {
                    x = ((DynamicObject)this.Model.GetValue(Key,row))["Id"].ToString();
                }
                else if (lx == "Number")
                {
                    x = ((DynamicObject)this.Model.GetValue(Key,row))["Number"].ToString();
                }
                if (lx == "Name")
                {
                    x = ((DynamicObject)this.Model.GetValue(Key,row))["Name"].ToString();
                }
            }
            return x;
        }
    }
}
