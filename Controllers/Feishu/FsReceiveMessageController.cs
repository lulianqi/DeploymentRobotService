using System;
using System.Collections.Generic;
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
    public class FsReceiveMessageController : ControllerBase
    {
        private readonly ILogger<FsReceiveMessageController> _logger;

        public FsReceiveMessageController(ILogger<FsReceiveMessageController> logger)
        {
            _logger = logger;
        }

        public class FsVerificationInfo
        {
            public string challenge { get; set; }
            public string token { get; set; }
            public string type { get; set; }
        }

        //[HttpPost("verification")]
        //[HttpPost]
        public async Task<ActionResult<string>> GetFsVerifyCallback(FsVerificationInfo fsVerificationInfo)
        {
            //Console.WriteLine( this.Request.Body.Position);
            return $"{{\"challenge\": \"{fsVerificationInfo.challenge}\"}}";
        }

        [HttpPost]
        public async Task<ActionResult<string>> GetApplicationMessageCallback(FsEventBaseInfo fsVerificationInfo)
        {
            //Console.WriteLine(this.Request.Body.Position);
            Console.WriteLine(fsVerificationInfo.@event.ToString());
            _logger.LogInformation($"[Fs MessageCallback] {fsVerificationInfo.ToJson()}");
            //如果不是message不处理
            if (fsVerificationInfo.header.event_type == "im.message.receive_v1")
            {
                _logger.LogInformation(fsVerificationInfo.@event.ToString());
                JObject jo = (JObject)JsonConvert.DeserializeObject(fsVerificationInfo.@event.ToString());
                if(jo==null)
                {
                    _logger.LogError("[Fs MessageCallback] event info error");
                    goto End;
                }
                
                //获取消息内容
                string messageUserId = jo["sender"]?["sender_id"]?["user_id"]?.Value<string>();
                string messageContent = ((JObject)JsonConvert.DeserializeObject(jo["message"]?["content"]?.Value<string>() ?? ""))?["text"]?.Value<string>();
                if(string.IsNullOrEmpty(messageUserId) || string.IsNullOrEmpty(messageContent))
                {
                    _logger.LogError("[Fs MessageCallback] event info error messageContent/messageUserId IsNullOrEmpty");
                    goto End;
                }
                //确认消息是不是过期的重复消息
                string createTimeStr = jo["message"]?["create_time"]?.Value<string>();
                long createTime;
                if (!string.IsNullOrEmpty(createTimeStr) && long.TryParse(createTimeStr, out createTime))
                {
                    long intervalTime = ((DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000) - createTime;
                    if (intervalTime > 5 * 60 * 1000)
                    {
                        _logger.LogWarning("[Fs MessageCallback] An expired message was received");
                        _ = ApplicationRobot.FsConnector.PushContent(messageUserId, $"⚠收到5分钟前的消息指令：[{messageContent}]\r\n如果您依然希望执行该命令请重新发送该指令");
                        goto End;
                    }
                }
                else
                {
                    _logger.LogWarning("[Fs MessageCallback] create_time is null");
                }
                //回应
                OperationHistory.AddOperation(ApplicationRobot.FsRobotBusinessData.GetUserNameById(messageUserId), messageContent, DateTime.Now.ToString("MM/dd HH:mm:ss"),"FsCmd");
                string fsResponseMessage = ApplicationRobot.ReplyApplicationCmd(ApplicationRobot.FsConnector, messageContent, messageUserId);
                _= ApplicationRobot.FsConnector.PushContent( messageUserId, fsResponseMessage);
            }

        End:
            return StatusCode(200);
        }

    }
}
