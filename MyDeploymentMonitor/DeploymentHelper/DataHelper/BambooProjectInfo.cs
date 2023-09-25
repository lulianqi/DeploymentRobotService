using System;
using System.Collections.Generic;
using System.Text;

namespace MyDeploymentMonitor.DeploymentHelper.DataHelper
{
    public class BambooProjectInfo
    {
        public string id { get; set; }
        public bool enabled { get; set; }
        public string projectName { get; set; }
        public string planName { get; set; }
        public string branchName { get; set; }
        public string description { get; set; }
    }
}
