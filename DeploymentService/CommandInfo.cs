using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeploymentRobotService.DeploymentService
{
    public enum CommandType
    {
        UnKonwCommand = 0,
        DeploymentCommand = 1,
        ShowInfoCommand=2,
        SystemCommand=3,
        ToolkitCommand=4,
        ErrorCommand =5
    }

    public class CommandInfoAddition
    {
        public string Remark { get; set; }
    }
    public class CommandInfo
    {
        public CommandType NowCommandType { get; set; }
        public string NowCommandStr { get; set; }
        public string CommandReply { get; set; }
        public CommandInfoAddition CommandAddition { get; set; }
        public object Tag { get; set; }
    }
}
