using MessageRobot.DingDingHelper;
using MessageRobot.FeiShuHelper;
using MessageRobot.WeChatHelper;
using MyDeploymentMonitor.DeploymentHelper;
using MyDeploymentMonitor.ExecuteHelper;
using MyDeploymentMonitor.QuartzJob;
using MyDeploymentMonitor.ShareData;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyDeploymentMonitor
{
    class Program
    {
        //static void Main(string[] args)
        static async Task Main(string[] args)
        {
            string message = "\\-\"-\r-\n-\t-\v-\x1b";
            message = JsonConvert.ToString(message).Trim('\"');
            FeishuRobot feishuRobot = new FeishuRobot("https://open.feishu.cn/open-apis/bot/v2/hook/d699c84d-d379-41f9-ac72-a8120de09651");
            await feishuRobot.SendMessageAsync(new MessageRobot.FeiShuHelper.Message.TextMessage("hi fuxiao",new List<string>() { "ou_d462849de343be5dc77ebdb8c4819070", "ou_924bb7c3bf0e9583a7124b390b19b4a2" }));
            //await Test();
            DingDingRobot ddr = new DingDingRobot(@"https://oapi.dingtalk.com/robot/send?access_token=509c2d5fe81f1c996461909b041122f50b2bdf9d178f7079f90435a12af11b5c", "SEC10076211a1355b75ba34ceffb11da74c8ac9d4932d51f9a8d904014230bf68ed");
            WeChatRobot wxr = new WeChatRobot(@"https://qyapi.weixin.qq.com/cgi-bin/webhook/send?key=ab39eadb-3feb-42d8-aa5d-1bb426ca5e9b");
            _ = ddr.SendTextAsync("MyDeploymentMonitor launch", true, new List<string>() { "15158155511" });
            _ = wxr.SendTextAsync("MyDeploymentMonitor launch", new List<string>() { "fuxiao" }, new List<string>() { "15158155511" });

            //ShareData.MonitorConf conf = new ShareData.MonitorConf() { BambooUserName = "fuxiao", BambooUserPassword = "123", BambooBaseUrl = "url",
            //    Projects = new Dictionary<string, ShareData.MonitorConf.Project>() { { "key", new ShareData.MonitorConf.Project() { IsNeedCommit = false, ProjectKey = "prkey", ProjectName = "marck" } } },
            //    Scheduler = new ShareData.MonitorConf.SchedulerConf() { Cron = "*,d,d", Projects = new Dictionary<string, string>() { { "key", "vaule" } } }, 
            //     Robot=new ShareData.MonitorConf.MessageRobot() { WinXinRobotUrl="wx", DingDingRobotUrl="url", DingDingRobotSecret="dc", DdAtPhone=new Dictionary<string, string>() { {"key1","v" },{ "ke2","v"} }, WxAtName=new Dictionary<string, string>() { { "key1", "v" }, { "ke2", "v" } } , WxAtPhone = new Dictionary<string, string>() { { "key1", "v" }, { "ke2", "v" } } }
            //};

            Console.WriteLine("Deploy Monitor");
            if(await MyConfiguration.InitConfigFileAsync(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase+"/conf.json"))
            {
                Console.WriteLine("SystemInit Complete");
            }
            else
            {
                Console.WriteLine("SystemInit failed");
                return;
            }
            while (true)
            {

                string tempInput = Console.ReadLine();
                switch (tempInput)
                {
                    case "init":
                        if (await MyConfiguration.InitConfigFileAsync(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "/conf.json",false))
                        {
                            Console.WriteLine(await MyDeploymentMonitor.ExecuteHelper.MyExecuteMan.ReInit() ? "reInit seccess" : "reInit fail");
                        }
                        else
                        {
                            Console.WriteLine("SystemInitAsync fial that load conf.json error");
                        }
                        break;
                    case "show":
                        Console.WriteLine( ExecuteHelper.MyBuilder.ShowProjects());

                        break;
                    case "test":
                        await ExecuteHelper.MyExecuteMan.RedeployForRancherAsync("c-fj8rb:p-6czkl", "deployment:p-6czkl-pipeline:uitestforcrm");
                        //await ExecuteHelper.MyExecuteMan.ScanPrivateBambooProjects();
                        //await ExecuteHelper.MyExecuteMan.DeploymentForPrivateBambooAsync(false, "CRM-JSNX0", @"c-nj4bv:p-hnmbd/workloads/deployment:aicc-private:jingrobot-service");//CRM-JSNX0
                        //await ExecuteHelper.MyExecuteMan.DeploymentForKubesphereAsync(false, "project-MJJYNKLOXR5K", "aicrm", "deployment:p-6czkl-pipeline:aicrm");
                        //await ExecuteHelper.MyExecuteMan.DeploymentForKubesphereAsync(false, "project-5x3rzPzOq0kX", "business_card", "deployment:p-6czkl-pipeline:business-card");
                        //KubeSphereDeploymentHelper k8s = new KubeSphereDeploymentHelper(@"http://k8s.indata.cc", "fuxiao", "8118054");
                        //Console.WriteLine( await k8s.StartDeployAsync("project-5x3rzPzOq0kX","ai-crm"));
                        break;
                    default:
                        _= ExecuteHelper.MyBuilder.BuildByKey(tempInput);
                        break;
                }
                
            }

        }

        /*
        static async Task<bool> SystemInitAsync(string confPath , bool isInitQuartz = true)
        {
            Console.WriteLine("load user config");
            MyConfiguration.UserConf= MyConfiguration.DeserializeContractData<ShareData.MonitorConf>(confPath);
            if(MyConfiguration.UserConf==null)
            {
                Console.WriteLine("load conf.json failed");
                return false;
            }
            if(MyConfiguration.UserConf.BambooUserName==null || MyConfiguration.UserConf.BambooUserPassword==null || MyConfiguration.UserConf.BambooBaseUrl==null)
            {
                Console.WriteLine("user,password,url can not be null");
                return false;
            }
            if(MyConfiguration.UserConf.Robot!=null)
            {
                if (MyConfiguration.UserConf.Robot.WinXinRobotUrl != null)
                {
                    MyDeploymentMonitor.ExecuteHelper.MyExecuteMan.SetWinXinRobot(MyConfiguration.UserConf.Robot.WinXinRobotUrl);
                    Console.WriteLine("SetWinXinRobot complete");
                }
                if (MyConfiguration.UserConf.Robot.DingDingRobotUrl != null)
                {
                    MyDeploymentMonitor.ExecuteHelper.MyExecuteMan.SetDingdingRobot(MyConfiguration.UserConf.Robot.DingDingRobotUrl, MyConfiguration.UserConf.Robot.DingDingRobotSecret);
                    Console.WriteLine("SetDingdingRobot complete");
                }
            }
            if(MyConfiguration.UserConf.Scheduler!=null && MyConfiguration.UserConf.Scheduler.Cron!=null&& MyConfiguration.UserConf.Scheduler.Projects!=null && isInitQuartz)
            {
                if(await QuartzInit.InitAsync())
                {
                    Console.WriteLine("QuartzInit complete");
                }
                else
                {
                    Console.WriteLine("QuartzInit failed");
                    return false;
                }
            }
            return true;
        }
        */

        /*
        static void ShowProjects()
        {
            bool isHasAnyProject = false;
            if (MyConfiguration.UserConf.KubeSphereProjects != null && MyConfiguration.UserConf.KubeSphereProjects.Count > 0)
            {
                isHasAnyProject = true;
                Console.WriteLine("KubeSphereProjects");
                foreach (var tempNode in MyConfiguration.UserConf.KubeSphereProjects)
                {
                    Console.WriteLine(string.Format("【{0,-3}】 [{1}] [{2}] [{3}]", tempNode.Key, tempNode.Value.ProjectName, tempNode.Value.PipelineId, tempNode.Value.IsNeedCommit ? "deploy when new commit" : "deploy not care commit"));
                }
            }
            if (MyConfiguration.UserConf.BambooProjects != null && MyConfiguration.UserConf.BambooProjects.Count>0)
            {
                isHasAnyProject = true;
                Console.WriteLine("BambooProjects");
                foreach (var tempNode in MyConfiguration.UserConf.BambooProjects)
                {
                    Console.WriteLine(string.Format("【{0,-3}】 [{1}] [{2}] [{3}]", tempNode.Key, tempNode.Value.ProjectName, tempNode.Value.ProjectKey, tempNode.Value.IsNeedCommit? "deploy when new commit" : "deploy not care commit"));
                }
            }
            if (MyConfiguration.UserConf.RancherProjects != null && MyConfiguration.UserConf.RancherProjects.Count > 0)
            {
                isHasAnyProject = true;
                Console.WriteLine("RancherProjects");
                foreach (var tempNode in MyConfiguration.UserConf.RancherProjects)
                {
                    Console.WriteLine(string.Format("【{0,-3}】 [{1}] [branch:{2}] [{3}]", tempNode.Key, tempNode.Value.ProjectName, tempNode.Value.Branch, tempNode.Value.IsNeedCommit ? "deploy when new commit" : "deploy not care commit"));
                }
            }
            if(!isHasAnyProject)
            {
                Console.WriteLine("not find any project");
            }
        }
        */

        static async Task<int> Test()
        {
            Console.WriteLine(System.Threading.Thread.CurrentThread.ManagedThreadId);
            await Task.Delay(10);
            KubeSphereV3DeploymentHelper v3 = new KubeSphereV3DeploymentHelper("https://ks-apiserver.byai-inc.com", "fuxiao", "8118054");
            await v3.Test();
            //RancherDeploymentHelper rd = new RancherDeploymentHelper("token-bj4lb:lnwsfjqrx9q5bps8fsrtmmttqw44chbqnqrrd8srzmrrfv7vl22ljb", @"https://rancher.indata.cc", "sTKLbMyJ6PS2PvQsetES", @"https://gitlab.indata.cc/");
            //string errorLog = await rd.GetWorkloadErrorPodLog("c-fj8rb:p-6czkl/workloads/deployment:p-6czkl-pipeline:by-gateway-console");

            //string x = await rd.GeLastCommitShaAsync("c-fj8rb:p-6czkl","p-6czkl:p-f4tlv","test");
            //object z = await rd.GetGitCommitInfoAsync(@"indata/ai-crm", "test", "068ba0e17793d2b443173c9f05dc0033a3b4c473");
            //var y = rd.StartDeployAsync("c-fj8rb:p-6czkl", "p-6czkl:p-f4tlv", "test");

            //WebHelper.MyWebSocket ws = new WebHelper.MyWebSocket(@"wss://rancher.indata.cc/v3/projects/c-fj8rb:p-6czkl/pipelineExecutions/p-6czkl:p-f4tlv-137/log?stage=1&step=0");
            //await ws.OpenAsync();
            //Console.WriteLine(await ws.ReceiveMesAsync(20));
            return 0;
        }
    }
}
