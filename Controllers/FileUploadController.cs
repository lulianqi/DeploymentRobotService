using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using Aliyun.OSS;
using DeploymentRobotService.DeploymentService;
using DeploymentRobotService.MyHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DeploymentRobotService.Controllers
{
    [Route("tool/{controller}")]
    [ApiController]
    public class FileUploadController : ControllerBase
    {
        private readonly ILogger<LogsController> _logger;

        public FileUploadController(ILogger<LogsController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        [HttpPut]
        [HttpPost("{path}")]
        [HttpPut("{path}")]
        public ActionResult<string> Post(string path)
        {
            List<string> result = new List<string>();
            if(Request.Form.Files?.Count>0)
            {
                path = path?.Trim('/');
                if (string.IsNullOrEmpty(path))
                {
                    path = null;
                }
                else
                {
                    path =$"wxci/{System.Net.WebUtility.UrlDecode(path)}" ;
                }
                foreach (var fi in Request.Form.Files)
                {
                    result.Add(AliOssHelper.PutFile(fi.OpenReadStream(), fi.FileName, path) ?? "File Upload Error");
                }
            }
            return result.ToJson();
        }
    }
}
