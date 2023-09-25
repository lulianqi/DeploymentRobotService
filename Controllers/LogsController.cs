using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using DeploymentRobotService.DeploymentService;
using DeploymentRobotService.MyHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DeploymentRobotService.Controllers
{
    [Route("deploymonitor/logs/{uid}")]
    [ApiController]
    public class LogsController : ControllerBase
    {
        private readonly ILogger<LogsController> _logger;

        public LogsController(ILogger<LogsController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public ActionResult<string> Get([FromRoute] string uid)
        {
            _logger.LogInformation(Request.QueryString.ToString());
            if (string.IsNullOrEmpty(uid))
            {
                return StatusCode(400, "not find your log uid");
            }
            string tempLogs = MyDeploymentMonitor.ExecuteHelper.MyExecuteMan.GetErrorLog(uid);
            if (string.IsNullOrEmpty(tempLogs))
            {
                return StatusCode(404, "not find your log ,or the log is expire");
            }
            return tempLogs;
        }
    }
}
