using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeploymentRobotService.Models.WxModels
{
    public class WxUserInfo:WxBaseInfo
    {
        public string UserId { get; set; }
        public string DeviceId { get; set; }

    }
}
