using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using DeploymentRobotService.DeploymentService;
using DeploymentRobotService.Models;
using DeploymentRobotService.Models.WxModels;
using DeploymentRobotService.MyHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DeploymentRobotService.Controllers
{
    [Route("token/[controller]")]
    [ApiController]
    public class ExecuteTokenController : ControllerBase
    {
        private readonly ILogger<ExecuteTokenController> _logger;

        public ExecuteTokenController(ILogger<ExecuteTokenController> logger)
        {
            _logger = logger;
        }


        [HttpGet]
        public async Task<ActionResult<string>> GetAsync([Required] string token, string appChannel = "wx", bool isCallBack =false)
        {
            ObjectResult tempResult = (ObjectResult)(await PostAsync(token, appChannel)).Result;
            if (tempResult.StatusCode == 402 & !isCallBack)
            {
                string redirectUrl;
                if (appChannel == "fs")
                {
                    redirectUrl = ApplicationRobot.FsConnector.GetOauthRedirectUrl($"ExecuteTokenController token={token}&appChannel={appChannel}");
                }
                else
                {
                    redirectUrl = ApplicationRobot.WxConnector.GetOauthRedirectUrl($"ExecuteTokenController token={token}&appChannel={appChannel}");
                }
                return new RedirectResult(redirectUrl);
            }
            string tempResultStr = tempResult.Value as string;
            string tempAdditionMessage = null;
            if (tempResultStr.StartsWith("Running"))
            {
                tempAdditionMessage = tempResultStr.Remove(0, "Running".Length + 1);
                tempResultStr = "Running";
            }
            if(tempResultStr.StartsWith("Canceling"))
            {
                tempAdditionMessage = tempResultStr.Remove(0, "Canceling".Length + 1);
                tempResultStr = "Canceling";
            }
            //await MyHtmlService.FillWxHtmlAsync(this, tempResultStr, tempAdditionMessage);
            ContentResult contentResult = MyHtmlService.GetWxHtmlContent(this, tempResultStr, tempAdditionMessage); ;
            contentResult.StatusCode = tempResult.StatusCode;
            return contentResult;
        }

        [HttpPost]
        public async Task<ActionResult<string>> PostAsync([Required][FromForm] string token ,string appChannel)
        {
            ObjectResult authorizationResult;
            if (appChannel == "fs")
            {
                authorizationResult = await UserAuthorizationService.CheckFsUserAsync(this);
            }
            else
            {
                authorizationResult = await UserAuthorizationService.CheckWxUserAsync(this);
            }
            if (authorizationResult.StatusCode != 200)
            {
                return authorizationResult;
            }
            string user = authorizationResult.Value as string;
            _logger.LogInformation("ExecuteTokenController : token {0} ; user {2})", token, user);
            string showUser = appChannel == "fs" ? ApplicationRobot.FsRobotBusinessData.GetUserNameById(user) : user;
            OperationHistory.AddOperation(showUser, string.Format("token:{0}", token), DateTime.Now.ToString("MM/dd HH:mm:ss"), "ExecuteTokenController");
            //ExecuteCommand
            CommandInfo commandInfo = ApplicationRobot.NowExecuteTokenDevice.ExecuteToken(ApplicationRobot.WxConnector, token);
            //CommandInfo commandInfo = MyDeployment.ExplainCommand(token, user);
            if (commandInfo.NowCommandType == CommandType.DeploymentCommand)
            {
                string tempProjectName = (commandInfo.Tag as DeploymentRuner)?.DeploymentProjectName;
                commandInfo.CommandReply = commandInfo.CommandReply.Replace("{ProjectName}", tempProjectName);
                return StatusCode(200, commandInfo.CommandReply);
            }
            else if (commandInfo.NowCommandType == CommandType.ErrorCommand || commandInfo.NowCommandType == CommandType.UnKonwCommand)
            {
                return StatusCode(400,"ErrorCommand");;
            }
            else
            {
                return StatusCode(200, commandInfo.CommandReply);
            }
        }
     }
}
