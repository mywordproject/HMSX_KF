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

}
