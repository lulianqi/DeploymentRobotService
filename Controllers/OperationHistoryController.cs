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
    [Route("deploymonitor/[controller]")]
    [ApiController]
    public class OperationHistoryController : ControllerBase
    {
        private readonly ILogger<OperationHistoryController> _logger;

        public OperationHistoryController(ILogger<OperationHistoryController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public ActionResult<List<Models.OperationInfo>> Get()
        {
            return OperationHistory.OperationInfos;
        }
    }
}
