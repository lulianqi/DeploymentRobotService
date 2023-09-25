using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using DeploymentRobotService.DeploymentService;
using DeploymentRobotService.Models.FsModels;
using DeploymentRobotService.MyHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DeploymentRobotService.Controllers
{
    [Route("feishu/[controller]")]
    [ApiController]
    public class FsGetUnionIdController : ControllerBase
    {
        private readonly ILogger<FsGetUnionIdController> _logger;


        public FsGetUnionIdController(ILogger<FsGetUnionIdController> logger)
        {
            _logger = logger;
        }



        [Route("byNick")]
        [HttpGet]
        public async Task<string> GetByNickAsync([Required] string nick)
        {
            KeyValuePair<string, FsUserInfo> user = ApplicationRobot.FsRobotBusinessData.FsUserDc.FirstOrDefault((userKv) => userKv.Value.enterprise_email == $"{nick}@-.com");
            return user.Value?.union_id ?? "";
        }

        [Route("byName")]
        [HttpGet]
        public async Task<string> GetByNameAsync([Required] string name)
        {
            KeyValuePair<string, FsUserInfo> user = ApplicationRobot.FsRobotBusinessData.FsUserDc.FirstOrDefault((userKv) => userKv.Value.name == name);
            return user.Value?.union_id ?? "";
        }

    }
}
