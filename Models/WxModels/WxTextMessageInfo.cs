using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeploymentRobotService.Models.WxModels
{
    public class TextContent { public string content { get; set; } }

    public class WxTextMessageInfo
    {
        public string touser { get; set; }
        public string toparty { get; set; }
        public string totag { get; set; }
        public string msgtype { get; set; }
        public int? agentid { get; set; }
        public TextContent text { get; set; }
        public int? safe { get; set; }
        public int? enable_duplicate_check { get; set; }
        public int? plicate_check_interval { get; set; }
    }
}
