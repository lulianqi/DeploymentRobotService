using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DeploymentRobotService.DeploymentService.MyCommandLine
{
    //https://natemcmaster.github.io/CommandLineUtils/v2.4/api/McMaster.Extensions.CommandLineUtils.CommandLineApplication.html
    public class MySample
    {
        public int Run(string[] args)
        {
            return CommandLineApplication.Execute<MySample>(args);
        }


        [Option("-n")]
        [Range(0, 10)]
        [Required]
        public int Count { get; }

        private void OnExecute()
        {
            Console.WriteLine("CommandLineApplication " + Count);
        }
    }
}
