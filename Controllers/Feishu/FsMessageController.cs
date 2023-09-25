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
using DeploymentRobotService.MyHelper.Cache;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DeploymentRobotService.Controllers
{
    [Route("feishu/[controller]")]
    [ApiController]
    public class FsMessageController : ControllerBase
    {
        private readonly ILogger<FsMessageController> _logger;


        public FsMessageController(ILogger<FsMessageController> logger)
        {
            _logger = logger;
        }



        [Route("interactive")]
        [HttpPost]
        public async Task<ActionResult> SendAsync([Required] FsSendInterativeInfo interativeInfo)
        {
            string nowMessageId = FsInteractiveUpdateCache.GetCache(interativeInfo.uuid);
            string newMessageId = null;
            //_logger.LogInformation($"FsSendInterativeInfo.UUID:{interativeInfo?.uuid}");
            if (!string.IsNullOrEmpty(nowMessageId))
            {
                newMessageId = await ApplicationRobot.FsConnector.NowFsHelper.UpdateInteractiveMessageAsync(interativeInfo.receive_id, nowMessageId, interativeInfo.content);
            }
            else
            {
                newMessageId = await ApplicationRobot.FsConnector.NowFsHelper.SendInteractiveMessageAsync(interativeInfo.receive_id, interativeInfo.content);
                if (!string.IsNullOrEmpty(interativeInfo.uuid) && !string.IsNullOrEmpty(newMessageId))
                {
                    FsInteractiveUpdateCache.AddCache(interativeInfo.uuid, newMessageId);
                }
            }
            if(string.IsNullOrEmpty(newMessageId))
            {
                return StatusCode(500, "send fail");
            }
            //处理加急逻辑
            if(interativeInfo.urgent_users?.Length>0)
            {
                await ApplicationRobot.FsConnector.NowFsHelper.MessageUrgentAsync(newMessageId, interativeInfo.urgent_users);
            }
            //_logger.LogInformation($"newMessageId:{newMessageId}");
            return StatusCode(200, newMessageId);
        }
    }
}
