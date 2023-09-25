using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
    [Route("")]
    [ApiController]
    public class WxReceiveMessageController : ControllerBase
    {
        private const string RespFormatData = @"<xml><ToUserName><![CDATA[{0}]]></ToUserName><FromUserName><![CDATA[{1}]]></FromUserName> <CreateTime>{2}</CreateTime><MsgType><![CDATA[text]]></MsgType><Content><![CDATA[{3}]]></Content></xml>";

        private long NowTimeStamp { get { return (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000; } }


        private readonly ILogger<WxReceiveMessageController> _logger;

        public WxReceiveMessageController(ILogger<WxReceiveMessageController> logger)
        {
            _logger = logger;
        }


        [HttpPost]
        //[Consumes("text/plain")]
        public async Task<ActionResult<string>> Post(string msg_signature, string timestamp, string nonce)
        {
            _logger.LogInformation("WxReceiveMessageController Get request msg_signature {0} timestamp {1} nonce {2}", msg_signature, timestamp, nonce);
            if (msg_signature == null || timestamp == null || nonce == null )
            {
                return StatusCode(400, "can not find all parameter");
            }
            string sToken = Appsetting.WxConfig.MessageToken;
            string sCorpID = Appsetting.WxConfig.CorpID;
            string sEncodingAESKey = Appsetting.WxConfig.MessageEncodingAESKey;

            Tencent.WXBizMsgCrypt wxcpt = new Tencent.WXBizMsgCrypt(sToken, sEncodingAESKey, sCorpID);

            string sVerifyMsgSig = HttpUtility.UrlDecode(msg_signature);
            string sVerifyTimeStamp = HttpUtility.UrlDecode(timestamp);
            string sVerifyNonce = HttpUtility.UrlDecode(nonce);
            var reader = new System.IO.StreamReader(Request.Body);
            string sReqData = HttpUtility.HtmlDecode(await reader.ReadToEndAsync());
            System.Console.WriteLine(sReqData);

            string sMsg = "";
            int ret = wxcpt.DecryptMsg(sVerifyMsgSig, sVerifyTimeStamp, sVerifyNonce, sReqData, ref sMsg);
            if (ret != 0)
            {
                System.Console.WriteLine("ERR: Decrypt Fail, ret: " + ret);
                return StatusCode(516);
            }
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(sMsg);
            System.Console.WriteLine(doc.InnerXml);
            XmlNode root = doc.FirstChild;

            string nowMsgType = root["MsgType"].InnerText;
            string toUserName = root["ToUserName"].InnerText;
            string fromUserName = root["FromUserName"].InnerText;
            _logger.LogInformation($"received MsgType {nowMsgType}");
            if (nowMsgType == "image" || nowMsgType == "video" || nowMsgType == "voice")
            {
               string mediaId = root["MediaId"].InnerText;
               _ = UploadWxMedia( mediaId,  fromUserName,  _logger);
               return StatusCode(200);
            }
            else if(nowMsgType=="text")
            {
                string content = root["Content"].InnerText;
                //return StatusCode(200);
                OperationHistory.AddOperation(fromUserName, content, DateTime.Now.ToString("MM/dd HH:mm:ss"));
                string wxResponseMessage = ApplicationRobot.ReplyApplicationCmd(ApplicationRobot.WxConnector,content, fromUserName);
                timestamp = NowTimeStamp.ToString();
                string sRespData = string.Format(RespFormatData, fromUserName, toUserName, timestamp, wxResponseMessage);
                string sEncryptMsg = ""; //xml格式的密文
                ret = wxcpt.EncryptMsg(sRespData, timestamp, nonce, ref sEncryptMsg);
                if (ret != 0)
                {
                    System.Console.WriteLine("ERR: EncryptMsg Fail, ret: " + ret);
                }
                //Response.ContentType = "text/xml";
                return sEncryptMsg;
            }
            else 
            {
                _logger.LogWarning("received envent or media MsgType");
                return StatusCode(200);
            }
            
        }


        static async Task<bool> UploadWxMedia(string mediaId,string fromUserName , ILogger _logger)
        {
            OperationHistory.AddOperation(fromUserName, $"UploadWxMedia:{mediaId}", DateTime.Now.ToString("MM/dd HH:mm:ss"));
            var mediaInfo = await ApplicationRobot.WxConnector.NowWxHelper.GetWxTemporaryMedia(mediaId,2);
            if (mediaInfo != null)
            {
                string url = AliOssHelper.PutFile(mediaInfo.Item2, mediaInfo.Item1, $"wxci/{fromUserName}");
                _logger.LogInformation($"PutFile sucesess {url}");
                await ApplicationRobot.WxConnector.PushContent(fromUserName, url).ContinueWith((isSucceed) => { if (!isSucceed.Result) MyLogger.LogError("PushContent for UploadWxMedia failed"); });
                return true;
            }
            else
            {
                _logger.LogError("GetWxTemporaryMedia fail");
                return false;
            }
        }
    }
}
