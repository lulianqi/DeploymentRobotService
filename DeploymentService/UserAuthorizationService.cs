using DeploymentRobotService.Models.FsModels;
using DeploymentRobotService.Models.WxModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace DeploymentRobotService.DeploymentService
{
    public class UserAuthorizationService
    {
        private const string checkUserUir = @"https://qyapi.weixin.qq.com/cgi-bin/user/get?userid={0}";

        /// <summary>
        /// 确认当前用户是否有当前应用权限 （只要是公司的企业微信都可以获取UserId，但是并不一定有应用权限，通过该函数可以进一步确认用户权限范围,通过isCheckApplication控制）
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="isCheckApplication">是否检查用于权限（默认检查）</param>
        /// <returns></returns>
        public static async Task<ObjectResult> CheckWxUserAsync(ControllerBase controller ,bool isCheckApplication = false)
        {
            MyHelper.MyLogger.LogInfo("CheckWxUserAsync");
            string session = controller.HttpContext.Session.GetString("UserId");
            Console.WriteLine("[CheckWxUserAsync] session is {0}", session);
            if (string.IsNullOrEmpty(session))
            {
                return new ObjectResult("身份认错误") { StatusCode = 402};
            }
            //如果不检查应用权限这里就可以直接返回
            if(!isCheckApplication)
            {
                return new ObjectResult(session) { StatusCode = 200 };
            }
            //通过GetWxBaseInfoAsync进一步检查用户是否拥有该应用的权限
            WxBaseInfo userBaseInfo = await ApplicationRobot.WxConnector.NowWxHelper.GetWxBaseInfoAsync<WxBaseInfo>(string.Format(checkUserUir, HttpUtility.UrlEncode(session)));
            if (userBaseInfo.errcode == 0)
            {
                return new ObjectResult(session) { StatusCode = 200 };
            }
            else if (userBaseInfo.errcode == 60011)
            {
                MyHelper.MyLogger.LogInfo(string.Format("errcode {0} no privilege to access {1}", userBaseInfo.errcode, session));
                return new ObjectResult("您的账号没有权限进行操作") { StatusCode = 401 };
            }
            else
            {
                MyHelper.MyLogger.LogInfo(string.Format("errcode {0} GetWxBaseInfoAsync {1}", userBaseInfo.errcode, session));
                return new ObjectResult("用户信息错误") { StatusCode = 402 };
            }
        }


        public static async Task<ObjectResult> CheckFsUserAsync(ControllerBase controller, bool isCheckApplication = false)
        {
            MyHelper.MyLogger.LogInfo("CheckFsUserAsync");
            string session = controller.HttpContext.Session.GetString("FsUserId");
            Console.WriteLine("[CheckFsUserAsync] session is {0}", session);
            if (string.IsNullOrEmpty(session))
            {
                return new ObjectResult("身份认错误") { StatusCode = 402 };
            }
            //如果不检查应用权限这里就可以直接返回
            if (!isCheckApplication)
            {
                return new ObjectResult(session) { StatusCode = 200 };
            }
            //通过GetWxBaseInfoAsync进一步检查用户是否拥有该应用的权限
            FsUserInfo userBaseInfo = await ApplicationRobot.FsConnector.NowFsHelper.GetUserInfo(HttpUtility.UrlEncode(session));
            if (userBaseInfo!=null)
            {
                return new ObjectResult(session) { StatusCode = 200 };
            }
            else
            {
                MyHelper.MyLogger.LogInfo($"[CheckFsUserAsync] fail FsUserId:{session}");
                return new ObjectResult("用户信息错误") { StatusCode = 402 };
            }
        }

    }
}
