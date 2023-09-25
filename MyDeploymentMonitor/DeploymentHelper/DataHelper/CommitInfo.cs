using System;
using System.Collections.Generic;

namespace MyDeploymentMonitor.DeploymentHelper.DataHelper
{
    public class CommitInfo
    {
        public List<KeyValuePair<string, string>> CommitList { get; set; }
        public string BuildState { get; set; }
        public string BuildResult { get; set; }
        public string Branch { get; set; }
    }
}
