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

namespace DeploymentRobotService.Controllers
{
    [Route("")]
    [ApiController]
    public class WxVerifyController : ControllerBase
    {
       
        [HttpGet]
        public async Task<ActionResult<string>> GetWxVerifyCallback(string msg_signature, string timestamp,string nonce, string echostr)
        {
            if(msg_signature==null|| timestamp==null  || nonce ==null || echostr==null)
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
            //string sVerifyEchoStr = HttpUtility.UrlDecode(echostr);
            string sVerifyEchoStr = echostr;
            string sEchoStr = "";
            int ret = wxcpt.VerifyURL(sVerifyMsgSig, sVerifyTimeStamp, sVerifyNonce, sVerifyEchoStr, ref sEchoStr);
            if (ret != 0)
            {
                System.Console.WriteLine("ERR: VerifyURL fail, ret: " + ret);
                return "";
            }
            System.Console.WriteLine("ERR: VerifyURL ok, ret: " + ret);
            return sEchoStr;
        }

    }
}
