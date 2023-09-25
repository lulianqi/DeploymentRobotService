using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeploymentRobotService.Models.FsModels
{
    public class FsBaseInfo
    {
        public int code { get; set; }
        public string msg { get; set; }

    }

    public class FsBaseInfo<T>: FsBaseInfo
    {
        public T data { get; set; }
    }
}
