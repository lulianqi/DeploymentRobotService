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
    [Route("action/redeploy")]
    [ApiController]
    public class RedeployByNameController : ControllerBase
    {
        private readonly ILogger<RedeployByNameController> _logger;

        public RedeployByNameController(ILogger<RedeployByNameController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<string>> Post([FromQuery,FromForm]string pipeline)
        {
            _logger.LogInformation("{0} start Redeploy", pipeline);
            OperationHistory.AddOperation("Auth", $"Redeploy:{pipeline}", DateTime.Now.ToString("MM/dd HH:mm:ss"), "BuildByNameController");
            string project = "c-fj8rb:p-6czkl";
            string workloads = $"deployment:p-6czkl-pipeline:{pipeline}";
            bool isRedeploy = await MyDeploymentMonitor.ExecuteHelper.MyBuilder.Redeploy(project, workloads);
            if (isRedeploy) return StatusCode(200);
            else return StatusCode(501);
        }

    }
}
