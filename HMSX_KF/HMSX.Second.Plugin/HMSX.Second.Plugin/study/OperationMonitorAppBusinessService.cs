using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.Enums;
using Kingdee.BOS.Core.Log;
using Kingdee.BOS.Util;
using System.Collections.Generic;
using System.ComponentModel;
namespace Jac.XkDemo.BOS.App.PlugIn.Service
{    /// <summary>    /// 【表单服务】操作监控服务    /// </summary>   
    [Description("【表单服务】操作监控服务"), HotUpdate]    
    public class OperationMonitorAppBusinessService : AbstractAppBusinessService   
    {  
        #region 重载函数        
        /// <summary>        
        /// 重载是否允许在IDE中设定执行时机：本服务不允许在IDE设定时间点，服务要求必须在操作后执行       
        /// /// </summary>        
        public override bool SupportActionPoint       
        {            
            get { return false; }       
        }        
        /// <summary>  重载执行时间点：设定本服务仅在操作后执行  /// </summary>        
        public override int ActionPoint        
        {            
            get { return (int)BOSEnums.Enu_ServiceActionPoint.AfterOperation; }        
        }       
        /// <summary> 是否允许批量执行？本服务允许批量执行； </summary>        
       public override bool RequestBatchProcess        
        {            
            get { return true; }       
        }        
        /// <summary>  添加本服务必须加载的字段        /// 
        /// </summary>  <param name="fieldKeys"></param>       
        public override void PreparePropertys(List<string> fieldKeys)        
        {            
            // TODO:fieldKeys.Add("???")       
         }        
        /// <summary>        /// 服务执行函数：在允许批量执行时，本函数不会被调用       
        /// /// </summary>        /// <param name="e"></param>        
        public override void DoAction(AppBusinessServiceArgs e)        
        {            // TODO: 本服务允许批量执行，本函数不会被调用，无需实现       
         }        
        /// <summary>        /// 服务执行函数：在允许批量执行时，本函数被调用       
        /// /// </summary>        /// <param name="e"></param>        
        public override void DoActionBatch(AppBusinessServiceArgs e)        
        {            
            // TODO           
             if (this.FormOperation.Operation.EqualsIgnoreCase("Delete"))           
              {                
                // 执行删除操作后写入上机日志               
                             var logs = new List<LogObject>();               
                var billName = this.BusinessInfo.GetForm().Name;                
                var billNoField = this.BusinessInfo.GetBillNoField();                
                foreach (var dataEntity in e.DataEntities)              
                {                    
                    var log = new LogObject();                    
                    log.pkValue = dataEntity.DataEntity[0].ToString();                   
                    log.Description = string.Format("[{0}{1}]被{2}删掉啦！", billName, dataEntity[billNoField.PropertyName], Context.UserName);    
                    log.OperateName = "表单服务保存上机日志";                   
                    log.ObjectTypeId = this.BusinessInfo.GetForm().Id;                  
                    log.SubSystemId = this.BusinessInfo.GetForm().SubsysId;                
                    log.Environment = OperatingEnvironment.BizOperate;                  
                    logs.Add(log);                
                }                
                ServiceFactory.GetLogService(Context).BatchWriteLog(this.Context, logs);           
            }           
             // TODO       
          }        
        #endregion 重载函数   
    }
}


