using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeploymentRobotService.Models.FsModels
{
    public class FsGroupInfo
    {
        public string avatar { get; set; }
        public string chat_id { get; set; }
        public string description { get; set; }
        public bool external { get; set; }
        public string name { get; set; }
        public string owner_id { get; set; }
        public string owner_id_type { get; set; }
        public string tenant_key { get; set; }
    }

}
