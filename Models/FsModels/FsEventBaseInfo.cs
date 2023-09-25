using System;
namespace DeploymentRobotService.Models.FsModels
{
    public class Header
    {
        /// <summary>
        /// 事件的唯一标识
        /// </summary>
        public string event_id { get; set; }
        /// <summary>
        /// 即Verification Token
        /// </summary>
        public string token { get; set; }
        /// <summary>
        /// 事件发送的时间
        /// </summary>
        public string create_time { get; set; }
        /// <summary>
        /// 事件类型 
        /// </summary>
        public string event_type { get; set; }
        /// <summary>
        /// 企业标识 
        /// </summary>
        public string tenant_key { get; set; }
        /// <summary>
        /// 应用ID
        /// </summary>
        public string app_id { get; set; }
    }

    public class FsEventBaseInfo
    {
        /// <summary>
        /// 事件格式的版本。无此字段的即为1.0
        /// </summary>
        public string schema { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Header header { get; set; }

        /// <summary>
        /// 不同事件此处数据不同 
        /// </summary>
        public virtual object  @event { get; set; }

    }
}
