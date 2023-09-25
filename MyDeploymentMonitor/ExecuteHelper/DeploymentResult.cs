using System;
using System.Collections.Generic;
using System.Text;

namespace MyDeploymentMonitor.ExecuteHelper
{
    public enum DeploymentResult
    {
        Succeed = 0,
        Failed = 1,
        Cancel = 2,
        UnStart = 3,
        Timeout = 4
    }
}
