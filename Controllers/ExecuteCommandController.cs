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
    [Route("action/[controller]")]
    [ApiController]
    public class ExecuteCommandController : ControllerBase
    {
        private readonly ILogger<ExecuteCommandController> _logger;

        private const string checkUserUir = @"https://qyapi.weixin.qq.com/cgi-bin/user/get?userid={0}";

        public ExecuteCommandController(ILogger<ExecuteCommandController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<string>> GetAsync([Required] string key, string appChannel = "wx", bool isCallBack =false)
        {
            ObjectResult tempResult = (ObjectResult)(await PostAsync(key , appChannel)).Result;
            if (tempResult.StatusCode == 402 & !isCallBack)
            {
                string redirectUrl;
                if (appChannel == "fs")
                {
                    //这里的key其实是command 可能会有需要urlencode的字符，这里需要先转义，后面这里url会整体当作一个参数，会再转义一遍，即这里的key要被2次转义
                    redirectUrl = ApplicationRobot.FsConnector.GetOauthRedirectUrl($"ExecuteCommandController key={HttpUtility.UrlEncode(key)}&appChannel={appChannel}");
                }
                else
                {
                    redirectUrl = ApplicationRobot.WxConnector.GetOauthRedirectUrl($"ExecuteCommandController key={HttpUtility.UrlEncode(key)}&appChannel={appChannel}");
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
        public async Task<ActionResult<string>> PostAsync([Required][FromForm] string key, string appChannel)
        {
            ObjectResult authorizationResult;
            IRobotConnector robotConnector;
            if (appChannel == "fs")
            {
                authorizationResult = await UserAuthorizationService.CheckFsUserAsync(this);
                robotConnector = ApplicationRobot.FsConnector;
            }
            else
            {
                authorizationResult = await UserAuthorizationService.CheckWxUserAsync(this);
                robotConnector = ApplicationRobot.WxConnector;
            }
            if (authorizationResult.StatusCode != 200)
            {
                return authorizationResult;
            }
            string user = authorizationResult.Value as string;
            _logger.LogInformation("ExecuteCommandController : key {0} ; user {2})", key, user);
            string showUser = appChannel == "fs" ? ApplicationRobot.FsRobotBusinessData.GetUserNameById(user) : user;
            OperationHistory.AddOperation(showUser, string.Format("key:{0}", key), DateTime.Now.ToString("MM/dd HH:mm:ss"), "ExecuteCommandController");
            //ExecuteCommand
            CommandInfo commandInfo = ApplicationRobot.ExplainCommand(robotConnector, key, user);
            if (commandInfo.NowCommandType == CommandType.DeploymentCommand)
            {
                string tempProjectName = (commandInfo.Tag as DeploymentRuner)?.DeploymentProjectName;
                return StatusCode(200,string.Format("{0} {1}", commandInfo.CommandReply,tempProjectName));
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
