using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeploymentRobotService.Models.FsModels
{
    public class AccessTokenIofo: FsBaseInfo
    {
        public string access_token { get; set; }
        public string expire { get; set; }
        public string tenant_access_token { get; set; }

    }
}
