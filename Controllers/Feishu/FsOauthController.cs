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
    [Route("user/[controller]")]
    [ApiController]
    public class FsOauthController : ControllerBase
    {
        private readonly ILogger<FsOauthController> _logger;

        public FsOauthController(ILogger<FsOauthController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 企业微信oauth的回调 (注意这里的回调会将用户信息写入Session，写入的是回调的域名，所以后面如果要使用这个本地域名，也需要直接使用回调域名才能读取，
        /// 就是说不要用本地调试地址调试需要aoth的接口)
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<string>> Get()
        {
            string code = this.Request.Query["code"];
            if(string.IsNullOrEmpty(code))
            {
                _logger.LogError("can not get code");
                return StatusCode(500, "can not get code");
            }
            string userId = await ApplicationRobot.FsConnector.NowFsHelper.GetUserIdByCodeAsync(code);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("GetUserIdByCodeAsync fail");
                return StatusCode(500, "GetUserIdByCodeAsync fail");
            }
            HttpContext.Session.SetString("FsUserId", userId);
            string state = this.Request.Query["state"].ToString();
            state = HttpUtility.UrlDecode(state);
            if (state.StartsWith("ExecuteCommandController"))
            {
                string key = state.Remove(0, "ExecuteCommandController".Length);
                key = key.TrimStart(' ');
                //return new RedirectResult(redirectUrl);
                return this.Redirect(string.Format("/action/ExecuteCommand?{0}&isCallBack=true", key));
            }
            else if(state.StartsWith("CancleByKeyController"))
            {
                string key = state.Remove(0, "CancleByKeyController".Length);
                key = key.TrimStart(' ');
                //return new RedirectResult(redirectUrl);
                return this.Redirect(string.Format("/action/CancleByKey?isCallBack=true&{0}", key));
            }
            else if (state.StartsWith("ExecuteTokenController"))
            {
                string key = state.Remove(0, "ExecuteTokenController".Length);
                key = key.TrimStart(' ');
                //return new RedirectResult(redirectUrl);
                return this.Redirect(string.Format("/token/ExecuteToken?isCallBack=true&{0}", key));
            }
            else
            {
                return userId;
            }
        }
    }
}
