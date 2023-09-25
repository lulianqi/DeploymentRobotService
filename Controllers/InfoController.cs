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
    [Route("deploymonitor/info/{uid}")]
    [Route("deploymonitor/info/html/{uid}")]
    [ApiController]
    public class InfoController : ControllerBase
    {
        private static SortedDictionary<string, string> infoMessageDc = null;
        private readonly ILogger<InfoController> _logger;

        static InfoController()
        {
            infoMessageDc = new SortedDictionary<string, string>();
        }

        public static string AddInfoMessage(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                string uid = Guid.NewGuid().ToString("N");
                if (infoMessageDc.ContainsKey(uid))
                {
                    uid = Guid.NewGuid().ToString("N");
                }
                // error Dictionary can not keep order
                if (infoMessageDc.Count > 100)
                {
                    string[] cdDelAr = new string[50];
                    int cdDelArIndex = 0;
                    foreach (var tempKv in infoMessageDc)
                    {
                        cdDelAr[cdDelArIndex] = tempKv.Key;
                        cdDelArIndex++;
                        if (cdDelArIndex > 50)
                        {
                            break;
                        }
                    }
                    foreach (string tempRemoveUid in cdDelAr)
                    {
                        infoMessageDc.Remove(tempRemoveUid);
                    }
                }
                infoMessageDc.Add(uid, message);
                return uid;
            }
            return null;
        }

        public static string GetInfoMessage(string uid)
        {
            return infoMessageDc.ContainsKey(uid) ? infoMessageDc[uid] : null;
        }

        public static string GetInfoUrl(string uuid)
        {
            return string.Format("https://{0}/deploymonitor/info/html/{1}", Appsetting.WxConfig.OAuthDomain, uuid);
        }

        public InfoController(ILogger<InfoController> logger)
        {
            _logger = logger;
        }       

        [HttpGet]
        public ActionResult<string> Get([FromRoute] string uid)
        {
            _logger.LogInformation(Request.QueryString.ToString());
            if (string.IsNullOrEmpty(uid))
            {
                return StatusCode(400, "not find your message uid");
            }
            string tempInfo = GetInfoMessage(uid);
            if (string.IsNullOrEmpty(tempInfo))
            {
                return StatusCode(404, "not find your message ,or the message is expire");
            }
            if(this.Request.Path.Value.StartsWith("/deploymonitor/info/html"))
            {
                tempInfo = tempInfo.Replace(Environment.NewLine, "<br>");
                return MyHtmlService.GetPojectsHtmlContent(this, tempInfo);
            }
            return tempInfo;
        }

    }
}
