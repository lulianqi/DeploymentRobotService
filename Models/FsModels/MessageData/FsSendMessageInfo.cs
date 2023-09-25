using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeploymentRobotService.Models.FsModels
{
    public class FsSendMessageInfo
    {
        public string receive_id { get; set; }
        public string content { get; set; }
        public string msg_type { get; set; }

    }
}
