using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeploymentRobotService.DeploymentService;
using DeploymentRobotService.Models.FsModels.MessageData;
using DeploymentRobotService.MyHelper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace DeploymentRobotService
{
    public class Program
    {
        public static void MainTest(string[] args)
        {
            //var x = DeploymentService.MyCommandLine.MyCmd.GetPassword(new string[] { "0ZhIaCthEX036IUJRPvrQQ==" });
            //var xx = DeploymentService.MyCommandLine.CmdHelper.GetCommandInfo("list -u fuxiao -p crm -n 10");
            //Console.WriteLine("__________________________");
            //DeploymentService.MyCommandLine.MyCmd.ListAliveRunners(new string[] { "--help" });

            //MyHelper.FsHelper fs = new MyHelper.FsHelper("https://open.feishu.cn", "cli_a2b6d9788bfcd00e", "nPI3UUNzLn2B7GadgEMqLcgM2TNavFiO");
            //MyHelper.FsHelper fs = new MyHelper.FsHelper("https://open.feishu.cn", "cli_a2b6e3a86af8100d", "i0xnaebNpfypQkftnmE9F7r14CFupS8j");
            //fs.GetUsersByDepartmentExAsync("0").GetAwaiter().GetResult();
            //PostMessage postMessage = new PostMessage() { content = new Content[][] { new Content[] { new Content() { tag = "a", href = "http://www.baidu.com", text = "baudu" }, new Content() { tag = "text", text = "123456" }, new Content() { tag = "text", text = "123456" }, new Content() { tag = "a", href = "http://www.baidu.com", text = "baudu" }, new Content() { tag = "text", text = "123456" }, } } };
            //fs.SendPostMessageAsync("oc_36258a855f8ed8c660b1f7debb05c87c", new PostMessageZh(postMessage), 1).GetAwaiter().GetResult();
            //FsRobotConnector.ConvertWxTextToFsPost("💥数据过多，仅为你展示了部分数据 🔗<a href=\"https://wx.lulianqi.com/deploymonitor/info/html/43b2156ea49c4917aef22f8b7edb3244\">查看全部</a>\r\nKubeSphereProjects\r\n【1】 [ai-crm 后端]   <a href=\"http://wx.lulianqi.com/action/ExecuteCommand?key=1\">发布</a>\r\n【2】 [aicrm 前端]   <a href=\"http://wx.lulianqi.com/action/ExecuteCommand?key=2\">发布</a>");
            //string openId1 = fs.GetOpenIdByMailOrMobileAsync("18667043129").GetAwaiter().GetResult();
            //string openId2 = fs.GetOpenIdByMailOrMobileAsync("15158155511").GetAwaiter().GetResult();


            MyHelper.WxHelper wx = new MyHelper.WxHelper("wwe4b0e6ba4b696f1d", "A7nIxvWAhxwhGjPuoIo5Y-QQSQIF2si92sFZcZQBRS4", 1000004);
            wx.SendMessageAsync("ceshihao|lijie|fuxiao", "$userName=lijie$ DeploymentRobotService is starting").Wait();

            wx = new MyHelper.WxHelper("ww3404061f78d46c04", "BSVvZhMaRpI4Xt8oOia-1Hz4pzIpJ7n9Cq1riGOsc0I", 1000164);
            wx.SendMessageAsync("fuxiao", "DeploymentRobotService is starting").Wait();
        }

        public static void Main(string[] args)
        {
            MainTest(null);
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>().UseUrls("http://*:5000");//http://*:5000;https://*:5001
                    //webBuilder.UseStartup<Startup>();
                })
            .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                    logging.AddDebug();
                    logging.AddEventSourceLogger();
                    logging.AddFile(o => o.RootPath = hostingContext.HostingEnvironment.ContentRootPath); // 3.1
                    //logging.AddFile(o => o.RootPath = AppContext.BaseDirectory);    // add save log file  2.1
                    logging.AddConsole(c => { c.TimestampFormat = "[yyyy:MM:dd-HH:mm:ss] "; });  // add console time only 3.0
                });
    }
}
