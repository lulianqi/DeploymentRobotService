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
    [Route("runner/list")]
    [ApiController]
    public class RunerListController : ControllerBase
    {
        private readonly ILogger<RunerListController> _logger;

        public RunerListController(ILogger<RunerListController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public ActionResult<List<DeploymentRuner>> Get(string user = null,string key = null, string name = null, int maxCount = 0)
        {
            _logger.LogInformation(Request.QueryString.ToString());
            if (maxCount==0)
            {
                maxCount = 1000;
            }
            return ApplicationRobot.NowDeploymentQueue.GetRunerList(user,key, name, null, maxCount);     
        } 
    }
}
