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
using MyDeploymentMonitor.ExecuteHelper;

namespace DeploymentRobotService.Controllers
{
    [Route("action/[controller]")]
    [ApiController]
    public class CancleByKeyController : ControllerBase
    {
        private readonly ILogger<CancleByKeyController> _logger;

        private const string checkUserUir = @"https://qyapi.weixin.qq.com/cgi-bin/user/get?userid={0}";

        public CancleByKeyController(ILogger<CancleByKeyController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Produces("text/html")]
        public async Task<ActionResult> GetAsync([Required]string key, [Required] string id, string appChannel = "wx", bool isCallBack = false)
        {
            ObjectResult tempResult = (ObjectResult)(await PostAsync(key, id , appChannel)).Result;
            if(tempResult.StatusCode==402 & !isCallBack)
            {
                string redirectUrl;
                if (appChannel=="fs")
                {
                    redirectUrl = ApplicationRobot.FsConnector.GetOauthRedirectUrl($"CancleByKeyController key={HttpUtility.UrlEncode(key)}&id={id}&appChannel={appChannel}");
                }
                else
                {
                    redirectUrl = ApplicationRobot.WxConnector.GetOauthRedirectUrl($"CancleByKeyController key={HttpUtility.UrlEncode(key)}&id={id}&appChannel={appChannel}");
                }
                return new RedirectResult(redirectUrl);
            }
            ContentResult contentResult =  MyHtmlService.GetWxHtmlContent(this, tempResult.Value as string);
            contentResult.StatusCode = tempResult.StatusCode;
            return contentResult;
        }


        [HttpPost]
        public async Task<ActionResult<string>> PostAsync([Required][FromForm]string key , [Required][FromForm] string id , string appChannel)
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
            string showUser = appChannel == "fs" ? ApplicationRobot.FsRobotBusinessData.GetUserNameById(user) : user;
            _logger.LogInformation("CancleByKeyController : key {0} ; id {1} ; user {2})", key, id, user);
            OperationHistory.AddOperation(showUser, string.Format("key:{0} ; id{1}", key, id), DateTime.Now.ToString("MM/dd HH:mm:ss"), "CancleByKeyController");
            if ( await MyBuilder.CancelByKey(key , id))
            {
                return StatusCode(200, "cancle ok");
            }
            else
            {
                return StatusCode(400, "cancle fail");
            }
        }
    }
}
