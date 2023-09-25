using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeploymentRobotService.DeploymentService.MyCommandLine
{
    public enum CmdResultState
    {
        Unknow,
        Cancel,
        Succeed,
        Fail,
        Help,
        Error
    }
    public class CmdResult
    {
        public object Flag { get; set; }
        public CmdResultState ResultState { get; set; }
        public string ResultText { get; set; }
    }
}
