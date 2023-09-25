using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using DeploymentRobotService.DeploymentService;
using DeploymentRobotService.Models;
using DeploymentRobotService.MyHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;

namespace DeploymentRobotService.Controllers
{
    [Route("action/build")]
    [ApiController]
    public class BuildByNameController : ControllerBase
    {
        private readonly ILogger<BuildByNameController> _logger;

        public BuildByNameController(ILogger<BuildByNameController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        [Authorize]
        public ActionResult<string> Post(BuildKubesInfo buildKubesInfo)
        {
            _logger.LogInformation("BuildKubesInfo {0}",buildKubesInfo.ToJson());
            string nowWorkload = null;
            if (!string.IsNullOrEmpty(buildKubesInfo.workloads))
            {
                nowWorkload = buildKubesInfo.workloads;
            }
            else if(!string.IsNullOrEmpty(buildKubesInfo.workloadfix))
            {
                nowWorkload = string.Format("{0}:{1}", buildKubesInfo.workloadfix, buildKubesInfo.pipeline);
            }
            _logger.LogInformation("{0} start build", buildKubesInfo.pipeline);
            OperationHistory.AddOperation("Auth", $"pipeline:{buildKubesInfo.pipeline}", DateTime.Now.ToString("MM/dd HH:mm:ss"), "BuildByNameController");
            MyDeploymentMonitor.ExecuteHelper.MyBuilder.BuildByName(buildKubesInfo.devop, buildKubesInfo.pipeline, nowWorkload, buildKubesInfo.configs, null, buildKubesInfo.wxrobot??"" , buildKubesInfo.fsrobot ?? "", buildKubesInfo.fschatid ?? "", buildKubesInfo.isPushStartMessage).ContinueWith((isSucceed) =>
            {
                _logger.LogInformation("{0} build {1}", buildKubesInfo.pipeline, isSucceed.Result.ToString());
            });
            return StatusCode(200);
        }


        [HttpPost("v2")]
        [Authorize]
        public ActionResult<string> OAuthTestPost(BuildKubesInfo buildKubesInfo)
        {
            _logger.LogInformation("OAuthTestPost[V2]");
            return Post(buildKubesInfo);
        }
    }
}
