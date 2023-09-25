using System;
using System.Collections.Generic;
using System.Text;

namespace MyDeploymentMonitor.DeploymentHelper.DataHelper
{
    public class RancherDeployState
    {
        public class Stage
        {
            public string Name { get; set; }

            public string State { get; set; }

            public string Start { get; set; }

            public string End { get; set; }

        }
        public List<Stage> Stages { get; set; }
        public string ExecutionState { get; set; }
    }
}
