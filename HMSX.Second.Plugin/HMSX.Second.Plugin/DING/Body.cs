using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMSX.Second.Plugin
{
    public class Body
    {
        public int errcode { get; set; }
        public string errmsg { get; set; }
        public Process_Instance process_instance { get; set; }
        public string request_id { get; set; }
    }

    public class Process_Instance
    {
        public Form_Component_Values[] form_component_values { get; set; }
        public string create_time { get; set; }
        public object[] attached_process_instance_ids { get; set; }
        public string[] cc_userids { get; set; }
        public string originator_dept_name { get; set; }
        public string originator_userid { get; set; }
        public string title { get; set; }
        public string finish_time { get; set; }
        public string result { get; set; }
        public string originator_dept_id { get; set; }
        public string business_id { get; set; }
        public Task[] tasks { get; set; }
        public string biz_action { get; set; }
        public Operation_Records[] operation_records { get; set; }
        public string status { get; set; }
    }

    public class Form_Component_Values
    {
        public string component_type { get; set; }
        public string name { get; set; }
        public string id { get; set; }
        public string value { get; set; }
    }

    public class Task
    {
        public string task_status { get; set; }
        public string create_time { get; set; }
        public string activity_id { get; set; }
        public string task_result { get; set; }
        public string userid { get; set; }
        public string finish_time { get; set; }
        public string taskid { get; set; }
        public string url { get; set; }
    }

    public class Operation_Records
    {
        public string date { get; set; }
        public string operation_type { get; set; }
        public string operation_result { get; set; }
        public string userid { get; set; }
        public string remark { get; set; }
    }
    public class Dept_order_listItem
    {
        /// <summary>
        /// 
        /// </summary>
        public int dept_id { get; set; }

    }
    public class Value
    {
        /// <summary>
        /// 非生产型员工
        /// </summary>
        public string text { get; set; }

    }

    public class Ext_attrsItem
    {
        /// <summary>
        /// 
        /// </summary>
        public string code { get; set; }

        /// <summary>
        /// 人员类别
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Value value { get; set; }

    }
    public class Leader_in_deptItem
    {
        /// <summary>
        /// 
        /// </summary>
        public int dept_id { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string leader { get; set; }

    }
    public class Role_listItem
    {
        /// <summary>
        /// 宏明双新
        /// </summary>
        public string group_name { get; set; }

        /// <summary>
        /// 非生产部门人员
        /// </summary>
        public string name { get; set; }

    }
    public class Union_emp_ext
    {
    }
    public class Result
    {
        /// <summary>
        /// 
        /// </summary>
        public string active { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string admin { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string avatar { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string boss { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public List<int> dept_id_list { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public List<Dept_order_listItem> dept_order_list { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string email { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string exclusive_account { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public List<Ext_attrsItem> ext_attrs { get; set; }

        /// <summary>
        /// {"用工性质":"正式员工","集团工龄":"2.50","本次加入集团日期":"2020-11-09","社会工龄":"3.870000","入司日期":"2020-11-09","人员类别":"非生产型员工","身份证号码":"513701199709260132","参加工作日期":"2019-06-26","工作场所":"66666","首次加入集团日期":"2020-11-09"}
        /// </summary>
        public string extension { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string hide_mobile { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string job_number { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public List<Leader_in_deptItem> leader_in_dept { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string mobile { get; set; }

        /// <summary>
        /// 成奕宏
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string org_email { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string real_authed { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string remark { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public List<Role_listItem> role_list { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string senior { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string state_code { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string telephone { get; set; }

        /// <summary>
        /// 信息技术工程师
        /// </summary>
        public string title { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Union_emp_ext union_emp_ext { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string unionid { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string userid { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string work_place { get; set; }

    }
    public class YHXX
    {
        /// <summary>
        /// 
        /// </summary>
        public int errcode { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string errmsg { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Result result { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string request_id { get; set; }

    }
}

