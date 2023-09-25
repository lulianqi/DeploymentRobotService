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
    [Route("test/[controller]")]
    [ApiController]
    public class DebugController : ControllerBase
    {
        private readonly ILogger<DebugController> _logger;

        public DebugController(ILogger<DebugController> logger)
        {
            _logger = logger;
        }

        private static async Task DelayAsync()
        {
            Console.WriteLine("DelayAsync");
            await Task.Delay(1000);
        }
        // This method causes a deadlock when called in a GUI or ASP.NET context.
        public static void Test()
        {
            // Start the delay.
            var delayTask = DelayAsync();
            // Wait for the delay to complete.
            Console.WriteLine("start Wait");
            delayTask.Wait();
            Console.WriteLine("end Wait");
        }

        [HttpGet]
        public ActionResult Get()
        {
            Test();
            return StatusCode(200, "ok");
        }
    }
}
