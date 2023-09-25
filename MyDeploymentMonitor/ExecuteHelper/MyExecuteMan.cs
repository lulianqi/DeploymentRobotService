using MyDeploymentMonitor.DeploymentHelper;
using MyDeploymentMonitor.DeploymentHelper.DataHelper;
using MessageRobot.FeiShuHelper;
using MyDeploymentMonitor.ShareData;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MessageRobot.WeChatHelper;
using MessageRobot.DingDingHelper;
using System.Linq;

namespace MyDeploymentMonitor.ExecuteHelper
{
    public static class MyExecuteMan
    {
        private static BambooDeploymentHelper bamboodeploymentMan = new BambooDeploymentHelper(MyConfiguration.UserConf.BambooUserName, MyConfiguration.UserConf.BambooUserPassword, MyConfiguration.UserConf.BambooBaseUrl);
        //private static RancherDeploymentHelper rancherDeploymentMan = new RancherDeploymentHelper("token-bj4lb:lnwsfjqrx9q5bps8fsrtmmttqw44chbqnqrrd8srzmrrfv7vl22ljb", @"https://rancher.indata.cc", "sTKLbMyJ6PS2PvQsetES", @"https://gitlab.indata.cc/");
        private static RancherDeploymentHelper rancherDeploymentMan = new RancherDeploymentHelper(MyConfiguration.UserConf.RancherBearerToken, MyConfiguration.UserConf.RancherUrl, MyConfiguration.UserConf.GitPrivateToken, MyConfiguration.UserConf.GitUrl);
        private static KubeSphereDeploymentHelper kubeSphereDeploymentMan = new KubeSphereDeploymentHelper(MyConfiguration.UserConf.KubeSphereBaseUrl, MyConfiguration.UserConf.KubeSphereUserName, MyConfiguration.UserConf.KubeSphereUserPassword);
        private static KubeSphereV3DeploymentHelper kubeSphereV3DeploymentMan = new KubeSphereV3DeploymentHelper(MyConfiguration.UserConf.KubeSphereV3BaseUrl, MyConfiguration.UserConf.KubeSphereV3UserName, MyConfiguration.UserConf.KubeSphereV3UserPassword);
        private static DingDingRobot dingdingRobot = null;
        private static MyGitHelper myGitClient = new MyGitHelper(MyConfiguration.UserConf.GitPrivateToken, MyConfiguration.UserConf.GitUrl);
        private static WeChatRobot winxinRobot = null;
        private static FeishuRobot feishuRobot = null;//这3个Robot 会在 InitConfigFileAsync 加载配置文件的时候初始化
        private static string feishuRobotChatId = MyConfiguration.UserConf.Robot.FeiShuRobotChatId;
        private static Dictionary<string, WeChatRobot> winxinRobotDc = null;
        private static Dictionary<string, FeishuRobot> feishuRobotDc = null;
        private static SortedDictionary<string, string> errorMessageDc = null;

        static MyExecuteMan()
        {
            bamboodeploymentMan.LoginBambooAsync().Wait();
            winxinRobotDc = new Dictionary<string, WeChatRobot>();
            feishuRobotDc = new Dictionary<string, FeishuRobot>();
            errorMessageDc = new SortedDictionary<string, string>();
        }

        /// <summary>
        /// 调试入口（仅用于测试）
        /// </summary>
        public static async Task DebugTest()
        {
            foreach (ExecuteStatus executeStatus in Enum.GetValues(typeof(ExecuteStatus)))
            {
                await Task.Delay(100);
                await MessagePushHelper.PushInterative(new DeploymentExecuteStatus()
                {
                    Devop = "流水线Devop",
                    Pipeline = "流水线Pipeline",
                    //Status = ExecuteStatus.BuildRunning,
                    Status= executeStatus,
                    RunId = "RunId",
                    Workloads = "Rancher Workloads",
                    FsApplicationChatId = "oc_36258a855f8ed8c660b1f7debb05c87c",
                    ExecuteUid = "12345677"
                });
                await Task.Delay(100);
            }
            
        }

        public static async Task<bool> ReInit()
        {
            bamboodeploymentMan = new BambooDeploymentHelper(MyConfiguration.UserConf.BambooUserName, MyConfiguration.UserConf.BambooUserPassword, MyConfiguration.UserConf.BambooBaseUrl);
            rancherDeploymentMan = new RancherDeploymentHelper(MyConfiguration.UserConf.RancherBearerToken, MyConfiguration.UserConf.RancherUrl, MyConfiguration.UserConf.GitPrivateToken, MyConfiguration.UserConf.GitUrl);
            kubeSphereDeploymentMan = new KubeSphereDeploymentHelper(MyConfiguration.UserConf.KubeSphereBaseUrl, MyConfiguration.UserConf.KubeSphereUserName, MyConfiguration.UserConf.KubeSphereUserPassword);
            kubeSphereV3DeploymentMan = new KubeSphereV3DeploymentHelper(MyConfiguration.UserConf.KubeSphereV3BaseUrl, MyConfiguration.UserConf.KubeSphereV3UserName, MyConfiguration.UserConf.KubeSphereV3UserPassword);
            dingdingRobot = dingdingRobot == null ? null : new DingDingRobot(dingdingRobot.WebhookUri, dingdingRobot.Secret);
            winxinRobot = winxinRobot == null ? null : new WeChatRobot(winxinRobot.WebhookUri);
            feishuRobot = feishuRobot == null ? null : new FeishuRobot(feishuRobot.WebhookUri);
            feishuRobotChatId = MyConfiguration.UserConf.Robot.FeiShuRobotChatId;
            winxinRobotDc = new Dictionary<string, WeChatRobot>();
            feishuRobotDc = new Dictionary<string, FeishuRobot>();
            FeiShuHandle.Init();
            return await bamboodeploymentMan.LoginBambooAsync();
        }

        public static void SetDingdingRobot(string webHook, string secret = null)
        {
            dingdingRobot = new DingDingRobot(webHook, secret);
        }

        public static void SetWinXinRobot(string webHook)
        {
            winxinRobot = new WeChatRobot(webHook);
        }

        public static void SetFeishuRobot(string webHook)
        {
            feishuRobot = new FeishuRobot(webHook);
        }

        public static async Task<Newtonsoft.Json.Linq.JObject> GetKubeSphereDevopPipelines(string yourDevop, int yourLimit)
        {
            return await kubeSphereDeploymentMan.GetDevopPipelines(yourDevop, yourLimit);
        }

        public static async Task<Newtonsoft.Json.Linq.JObject> GetKubeSphereV3DevopPipelines(string yourDevop, int yourLimit)
        {
            return await kubeSphereV3DeploymentMan.GetDevopPipelines(yourDevop, yourLimit);
        }

        public static async Task<List<string>> GetKubeSphereGetDevops(int yourLimit)
        {
            return await kubeSphereDeploymentMan.GetDevops(yourLimit);
        }

        public static async Task<List<string>> GetKubeSphereV3GetDevops(int yourLimit)
        {
            return await kubeSphereV3DeploymentMan.GetDevops(yourLimit);
        }
        public static async Task<List<BambooProjectInfo>> ScanPrivateBambooProjects()
        {
            return await bamboodeploymentMan.GetAllBambooProjectList();
        }

        private static async Task<bool> SendRobotMessageAsync(string text, string title = null, List<string> atDdList = null, List<string> atWxList = null, List<string> atWxPhoneList = null, string wxRobotUrl = null,string fsRobotUrl = null)
        {
            bool ddSucess = true;
            bool wxSucess = true; 
            bool fsSucess = true;
            WeChatRobot assignWinXinRobot = null;
            FeishuRobot assignFeishuRobot = null;
            //外部微信机器人
            if (!string.IsNullOrEmpty(wxRobotUrl))
            {
                if (!winxinRobotDc.ContainsKey(wxRobotUrl))
                {
                    lock (winxinRobotDc)
                    {
                        if(winxinRobotDc.Count>100)
                        {
                            winxinRobotDc.Clear();
                        }
                        winxinRobotDc.Add(wxRobotUrl, new WeChatRobot(wxRobotUrl));
                    }
                }
                assignWinXinRobot = winxinRobotDc[wxRobotUrl];
            }
            if (assignWinXinRobot != null)
            {
                if (title == null)
                {
                    wxSucess = await assignWinXinRobot.SendTextAsync(text, atWxList, atWxPhoneList);
                }
                else
                {
                    wxSucess = await assignWinXinRobot.SendMarkdownAsync(text);
                }
            }

            //外部飞书机器人
            if (!string.IsNullOrEmpty(fsRobotUrl))
            {
                if (!feishuRobotDc.ContainsKey(fsRobotUrl))
                {
                    lock (feishuRobotDc)
                    {
                        if (feishuRobotDc.Count > 100)
                        {
                            feishuRobotDc.Clear();
                        }
                        feishuRobotDc.Add(fsRobotUrl, new FeishuRobot(fsRobotUrl));
                    }
                }
                assignFeishuRobot = feishuRobotDc[fsRobotUrl];
            }
            if (assignFeishuRobot != null)
            {
                fsSucess = await assignFeishuRobot.SendTextAsync(text, await FeiShuHandle.GetUserOpenIdListAsync(atWxList));
            }
            //如果使用外部机器人将跳过默认配置的机器人
            if(!string.IsNullOrEmpty(wxRobotUrl) || !string.IsNullOrEmpty(fsRobotUrl))
            {
                return wxSucess && fsSucess;
            }

            //使用""表示通过web API 触发，跳过内部机器人推送 （使用null表示使用默认内部配置机器人）
            if(wxRobotUrl=="" || fsRobotUrl=="")
            {
                return false;
            }

            //内部机器人
            if (dingdingRobot == null && winxinRobot == null && feishuRobot ==null)
            {
                return false;
            }
            if (dingdingRobot != null)
            {
                if (title == null)
                {
                    ddSucess = await dingdingRobot.SendTextAsync(text, true, atDdList);
                }
                else
                {
                    ddSucess = await dingdingRobot.SendMarkdownAsync(title, text, true, atDdList);
                }
            }
            if (winxinRobot != null)
            {
                if (title == null)
                {
                    wxSucess = await winxinRobot.SendTextAsync(text, atWxList, atWxPhoneList);
                }
                else
                {
                    wxSucess = await winxinRobot.SendMarkdownAsync(text);
                }
            }
            if(feishuRobot !=null)
            {
                fsSucess = await feishuRobot.SendTextAsync(text, await FeiShuHandle.GetUserOpenIdListAsync(atWxList));
            }
            return ddSucess && wxSucess && fsSucess;
        }


        private static void ShowMessage(string mes, bool isWithTime = true)
        {
            if (isWithTime)
            {
                Console.WriteLine("【{0}】： {1}", DateTime.Now.ToString("HH:mm:ss"), mes);
            }
            else
            {
                Console.WriteLine(mes);
            }
        }

        private static string GetCommitStr(List<KeyValuePair<string, string>> commitList, string buildNum = null, bool isMarkdown = true)
        {
            if (commitList == null || commitList.Count == 0)
            {
                return null;
            }
            StringBuilder sbCommit = new StringBuilder();
            if (isMarkdown)
            {
                sbCommit.Append("##### 【commit】 ");
                if (buildNum != null) sbCommit.Append(buildNum);
                sbCommit.AppendLine();
            }
            foreach (var commit in commitList)
            {
                sbCommit.Append(string.Format(isMarkdown ? "- **{0}** > " :"●{0} > ", commit.Key));
                sbCommit.AppendLine(commit.Value);
            }
            //移除结尾newline
            if(commitList?.Count>0)
            {
                sbCommit.Remove(sbCommit.Length - Environment.NewLine.Length, Environment.NewLine.Length);
            }
            return sbCommit.Length > 0 ? sbCommit.ToString() : null;
        }

        private static List<string> GetCommitAtUser(List<KeyValuePair<string, string>> commitList, Dictionary<string, string> AtUserDc)
        {
            if (AtUserDc == null) return null;
            List<string> atUsers = new List<string>();
            Action<string> AddUserAction = new Action<string>((user) => { if (!atUsers.Contains(user)) atUsers.Add(user); });
            if (commitList == null || commitList.Count == 0)
            {
                return null;
            }
            foreach (var commit in commitList)
            {
                if (AtUserDc.ContainsKey(commit.Key))
                {
                    AddUserAction(AtUserDc[commit.Key]);
                }
            }
            return atUsers;
        }

        private static List<string> GetUnFindCommitAtUser(List<KeyValuePair<string, string>> commitList, Dictionary<string, string> AtUserDc)
        {
            List<string> unFindUser = GetCommitUser(commitList);
            if (AtUserDc != null && AtUserDc.Count > 0 && unFindUser.Count>0)
            {
                for(int i = unFindUser.Count-1; i>=0; i--)
                {
                    if(AtUserDc.ContainsKey(unFindUser[i]))
                    {
                        unFindUser.RemoveAt(i);
                    }
                }
            }
            return unFindUser;
        }

        private static List<string> GetCommitUser(List<KeyValuePair<string, string>> commitList)
        {
            List<string> users = new List<string>();
            if (commitList != null && commitList.Count > 0)
            {
                foreach (var commit in commitList)
                {
                    if (!users.Contains(commit.Key))
                    {
                        users.Add(commit.Key);
                    }
                }
            }
            return users;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        private static string AddErrorLog(string logs)
        {
            
            if (!string.IsNullOrEmpty(logs) && !string.IsNullOrEmpty(MyConfiguration.UserConf.LogBaseUrl))
            {
                if(errorMessageDc.ContainsValue(logs))
                {
                    return errorMessageDc.FirstOrDefault(kv => kv.Value == logs).Key;
                }
                string uid = Guid.NewGuid().ToString("N");
                if(errorMessageDc.ContainsKey(uid))
                {
                    uid = Guid.NewGuid().ToString("N");
                }
                // error Dictionary can not keep order
                if (errorMessageDc.Count>200)
                {
                    string[] cdDelAr = new string[50];
                    int cdDelArIndex = 0;
                    foreach (var tempKv in errorMessageDc)
                    {
                        cdDelAr[cdDelArIndex] = tempKv.Key;
                        cdDelArIndex++;
                        if(cdDelArIndex>49)
                        {
                            break;
                        }
                    }
                    foreach(string tempRemoveUid in cdDelAr)
                    {
                        errorMessageDc.Remove(tempRemoveUid);
                    }
                }
                errorMessageDc.Add(uid, logs);
                return uid;
            }
            return null;
        }

        /// <summary>
        /// 处理长文本（超过设定限制，返回需要内容及全部内容的详情链接）
        /// </summary>
        /// <param name="message">原始消息</param>
        /// <param name="limitLeng">限制长度（或限制行数，通过isLimitLine区分）</param>
        /// <param name="locationStr">显示内容的开始标记（使用行数限制时无效）</param>
        /// <param name="isLimitLine">是否为限制行数（默认false表表示使用长度限制）</param>
        /// <returns></returns>
        public static Tuple<string, string> AnalysisLongMessage(string message, int limitLeng = 800, string locationStr = "[ERROR]" ,bool isLimitLine = false)
        {
            string showLog = null;
            string logUri = null;
            string logUid = null;
            if (string.IsNullOrEmpty(message))
            {
                showLog = "can not get the error logs";
                //logUid = AddErrorLog(errorLog);
            }
            //使用行限制
            else if(isLimitLine)
            {
                if(message.Contains(Environment.NewLine))
                {
                    int startIndex = -1;
                    int hitCount = 0;
                    bool needShowAll = limitLeng <= 0;
                    while(hitCount < limitLeng)
                    {
                        int tempIndex = message.IndexOf(Environment.NewLine, startIndex+1);
                        if (tempIndex < 0)
                        {
                            needShowAll = true;
                            break;
                        }
                        else
                        {
                            startIndex = tempIndex;
                            hitCount++;
                        }
                    }
                    if(needShowAll)
                    {
                        showLog = message;
                    }
                    else
                    {
                        logUid = AddErrorLog(message);
                        if(startIndex>1)
                        {
                            showLog = message.Substring(0, startIndex - 1);
                        }
                        else
                        {
                            showLog = message;
                        }
                    }
                }
                else
                {
                    showLog = message;
                }
            }
            //使用字节长度限制
            else
            {
                if (message.Length > limitLeng)
                {
                    logUid = AddErrorLog(message);
                }
                string showLogs = message;
                if (showLogs.Length > limitLeng && showLogs.Contains(locationStr))
                {
                    showLogs = message.Substring(message.IndexOf(locationStr));
                    if (showLogs.Length > limitLeng)
                    {
                        showLogs = showLogs.Substring(0, limitLeng);
                    }
                }
                else if (showLogs.Length > limitLeng)
                {
                    showLogs = showLogs.Substring(message.Length - limitLeng);
                }
                showLog = showLogs;
            }
            if (!string.IsNullOrEmpty(logUid))
            {
                logUri = string.Format("{0}/{1}", MyConfiguration.UserConf.LogBaseUrl, logUid);
            }
            return new Tuple<string, string>(showLog, logUri);
        }

        private static string DealErrorLog(string logs ,int logLeng = 800 ,string locationStr = "[ERROR]")
        {
            Tuple<string, string> tupleReult = AnalysisLongMessage(logs, logLeng, locationStr);
            string showLogs = tupleReult.Item1;
            string logUid = tupleReult.Item2;
            if (logUid != null)
            {
                string logUrl = string.Format("{0}/{1}", MyConfiguration.UserConf.LogBaseUrl, logUid);
                showLogs = string.Format("{0}\r\n\r\n📋👇点击以下链接查看错误详情👇\r\n👉{1}",showLogs, logUrl);
            }
            return showLogs;
        }

        public static async Task<System.IO.Stream> GetMyGitFileContentStreamAsync(string uri)
        {
            if (string.IsNullOrEmpty(uri)) return null;
            return await myGitClient.GetGitFileContentStream(uri);
        }

        public static string GetErrorLog(string uid)
        {
            return errorMessageDc.ContainsKey(uid) ? errorMessageDc[uid] : null;
        }

        public static void ClearCurrentConsoleLine()
        {
            //Getting the current cursor position (if it hasn't been cached) on Unix requires writing to stdout and reading from stdin, so if there's a pending ReadLine from stdin in progress, getting the current cursor position synchronizes with it.
            int nowPosition = Console.CursorTop - 1;
            Console.SetCursorPosition(0, nowPosition);
            Console.Write(new string(' ', Console.WindowWidth)); //mac 执行完成后 Console.CursorTop 是下一行，而windows Console.CursorTop还是没有变 （而实际上是下一行）
            Console.SetCursorPosition(0, nowPosition);
        }

        public static void ClearCurrentConsoleLineEx(int consoleWidth = 0)
        {
            consoleWidth = consoleWidth == 0 ? Console.WindowWidth : consoleWidth;
            StringBuilder sb = new StringBuilder(consoleWidth * 5);
            for (int i = 0; i < consoleWidth; i++)
            {
                sb.Append('\b');
            }
            for (int i = 0; i < consoleWidth; i++)
            {
                sb.Append(' ');
            }
            for (int i = 0; i < consoleWidth; i++)
            {
                sb.Append('\b');
            }
            Console.Write(sb.ToString());
        }

        public static async Task<DeploymentResult> DeploymentForBambooAsync(bool needCommit, string yourBuildKey = null)
        {
            //goto lable;
            if (!await bamboodeploymentMan.GetSeviceStateAsync())
            {
                ShowMessage("bamboo is not in sevice");
                return DeploymentResult.Failed;
            }
            var startBuildResult = await bamboodeploymentMan.TriggerManualBuildAsync(yourBuildKey);
            string buildNum = startBuildResult.Key;
            if (buildNum == null)
            {
                return DeploymentResult.Failed;
            }
            ShowMessage(string.Format("{0} start build", buildNum));
            Console.Write("0%");
            int flgGetBuildStatus = 0;
            string nowBuildStatus = null;
            //show status for build
            while (flgGetBuildStatus < 5)
            {
                await Task.Delay(1000);
                nowBuildStatus = await bamboodeploymentMan.GetBuildProgressAsync(buildNum);
                ClearCurrentConsoleLineEx();
                //ClearCurrentConsoleLine();
                if (string.IsNullOrEmpty(nowBuildStatus))
                {
                    flgGetBuildStatus++;
                    ShowMessage(string.Format("{0} can not GetBuildStatusAsync", buildNum));
                }
                else if (nowBuildStatus == "ok")
                {
                    flgGetBuildStatus = 5;
                    ShowMessage(string.Format("{0} build complete", buildNum));
                }
                else
                {
                    flgGetBuildStatus = 0;
                    Console.Write(nowBuildStatus);
                }
            }
            if (string.IsNullOrEmpty(nowBuildStatus))
            {
                ShowMessage(string.Format("{0} build fail", buildNum));
            }

            CommitInfo commitInfo = await bamboodeploymentMan.GetBuildCommitAsync(buildNum);
            List<KeyValuePair<string, string>> commitList = commitInfo.CommitList;
            string nowCommit = GetCommitStr(commitList, buildNum);
            if (string.IsNullOrEmpty(nowCommit))
            {
                ShowMessage("no commit", false);
                if (needCommit)
                {
                    return DeploymentResult.Failed;
                }
            }
            ShowMessage(nowCommit, false);
            await SendRobotMessageAsync(nowCommit, "commit");
            //lable:
            List<string> tempDdAts = GetCommitAtUser(commitList, MyConfiguration.UserConf.Robot?.DdAtPhone);
            //List<string> tempWxAts = GetCommitAtUser(commitList, MyConfiguration.UserConf.Robot?.WxAtName);
            List<string> tempWxPhoneAts = GetCommitAtUser(commitList, MyConfiguration.UserConf?.Robot.WxAtPhone);
            List<string> tempWxAts = GetUnFindCommitAtUser(commitList, MyConfiguration.UserConf?.Robot.WxAtPhone);

            if (commitInfo.BuildState == "was successful ")
            {
                ShowMessage(string.Format("start deployment"));
                await SendRobotMessageAsync(string.Format("【{0}】 start deployment", yourBuildKey), null, tempDdAts, tempWxAts, tempWxPhoneAts);
            }
            else
            {
                ShowMessage(string.Format("build fial [{0}]", commitInfo.BuildState), false);
                await SendRobotMessageAsync(string.Format("💔💔💔💔💔💔💔💔💔💔💔💔\r\n【{0}】 buid fail [{1}]\r\n💔💔💔💔💔💔💔💔💔💔💔💔", yourBuildKey, commitInfo.BuildState), null, tempDdAts, tempWxAts, tempWxPhoneAts);
                await SendRobotMessageAsync(await bamboodeploymentMan.GetBuildLogs(buildNum));
                return DeploymentResult.Failed;
            }


            List<KeyValuePair<string, string>> environmentList = await bamboodeploymentMan.GetDeployEnvironments(yourBuildKey);
            if (environmentList != null && environmentList.Count > 0)
            {
                string promoteVersion = null;
                foreach (KeyValuePair<string, string> tempEnvironment in environmentList)
                {
                    ShowMessage("deploy start");
                    string tempDeploymentResultId = null;
                    await SendRobotMessageAsync(string.Format("[{0}]\r\n[{1}] deploy start", tempEnvironment.Value ?? yourBuildKey, DateTime.Now.ToString("HH:mm:ss")));
                    if (promoteVersion == null)
                    {
                        string[] tempDeployRusult = await bamboodeploymentMan.StartDeployAsync(yourBuildKey, tempEnvironment.Key);
                        if (tempDeployRusult == null || tempDeployRusult.Length < 2 || tempDeployRusult[0] == "0")
                        {
                            ShowMessage(string.Format("deployment fial that environmentList is error"));
                            await SendRobotMessageAsync(string.Format("【{0}】 deployment fial", yourBuildKey));
                            return DeploymentResult.Failed;
                        }
                        promoteVersion = tempDeployRusult[1];
                        tempDeploymentResultId = tempDeployRusult[0];
                    }
                    else
                    {
                        tempDeploymentResultId = await bamboodeploymentMan.StartPromoteDeployAsync(promoteVersion, tempEnvironment.Key);
                    }

                    if (await WaitDeployForBambooAsync(tempDeploymentResultId))
                    {
                        ShowMessage("deploy succeed");
                        await SendRobotMessageAsync(string.Format("[{0}]\r\n[{1}] deploy succeed", tempEnvironment.Value ?? yourBuildKey, DateTime.Now.ToString("HH:mm:ss")));
                        return DeploymentResult.Succeed;
                    }
                    else
                    {
                        ShowMessage("deploy fail");
                        await SendRobotMessageAsync(string.Format("[{0}]\r\n[{1}] deploy fail", tempEnvironment.Value ?? yourBuildKey, DateTime.Now.ToString("HH:mm:ss")));
                        return DeploymentResult.Failed;
                    }
                }
            }
            else
            {
                ShowMessage(string.Format("deployment fial that not find any deploy enviroment"));
                await SendRobotMessageAsync(string.Format("【{0}】 deployment fial", yourBuildKey));
            }
            return DeploymentResult.Succeed;
        }

        public static async Task<DeploymentResult> DeploymentForPrivateBambooAsync(bool needCommit, string yourBuildKey , string workloadPath =null, string image = null, Dictionary<string, string> configDc = null, Action<string, string> pushMessageAction = null, string wxRobotUrl = null, bool isPushStartMessage = true)
        {
            if (!await bamboodeploymentMan.GetSeviceStateAsync())
            {
                ShowMessage("bamboo is not in sevice");
                return DeploymentResult.Failed;
            }

            DateTime startTime = DateTime.Now;
            BambooProjectInfo bambooProjectInfo = await bamboodeploymentMan.GetBambooProjectInfo(yourBuildKey);
            string appTag = await bamboodeploymentMan.GetBambooScriptEnvironmentVariable(yourBuildKey);

            string nowBambooProjectInfoStr = string.Format("  ◎ plan: {0}\r\n  ◎ project: {1}\r\n  ◎ branch: {2}\r\n  ◎ imageTag: {3}", bambooProjectInfo.planName ?? "null", bambooProjectInfo.projectName ?? "null", bambooProjectInfo.branchName??"null", appTag??"null");

            var startBuildResult = await bamboodeploymentMan.TriggerManualBuildAsync(yourBuildKey);
            string buildNum = startBuildResult.Key;
            if (buildNum == null)
            {
                return DeploymentResult.Failed;
            }
            await SendRobotMessageAsync(string.Format("【{0}】({1}) start build\r\n{2}", yourBuildKey, buildNum, nowBambooProjectInfoStr), null, null, null, null);
            pushMessageAction?.Invoke(startBuildResult.Value, buildNum??"null");
            ShowMessage(string.Format("{0} start build", buildNum));
            Console.Write("0%");
            int flgGetBuildStatus = 0;
            string nowBuildStatus = null;
            //show status for build
            while (flgGetBuildStatus < 5)
            {
                await Task.Delay(3000);
                nowBuildStatus = await bamboodeploymentMan.GetBuildProgressAsync(buildNum);
                ClearCurrentConsoleLineEx();
                //ClearCurrentConsoleLine();
                if (string.IsNullOrEmpty(nowBuildStatus))
                {
                    flgGetBuildStatus++;
                    ShowMessage(string.Format("{0} can not GetBuildStatusAsync", buildNum));
                }
                else if (nowBuildStatus == "ok")
                {
                    flgGetBuildStatus = 5;
                    ShowMessage(string.Format("{0} build complete", buildNum));
                }
                else
                {
                    flgGetBuildStatus = 0;
                    Console.Write(nowBuildStatus);
                }
            }
            if (string.IsNullOrEmpty(nowBuildStatus))
            {
                ShowMessage(string.Format("{0} build fail", buildNum));
            }

            CommitInfo commitInfo = await bamboodeploymentMan.GetBuildCommitAsync(buildNum);
            List<KeyValuePair<string, string>> commitList = commitInfo.CommitList;
            string nowCommit = GetCommitStr(commitList, buildNum ,false);
            if (string.IsNullOrEmpty(nowCommit))
            {
                nowCommit = "◎no commit";
                if (needCommit)
                {
                    ShowMessage("not find commit so break", false);
                    return DeploymentResult.Failed;
                }
            }
            ShowMessage(nowCommit, false);
            //await SendRobotMessageAsync(nowCommit, "commit");
            //lable:
            List<string> tempDdAts = GetCommitAtUser(commitList, MyConfiguration.UserConf.Robot?.DdAtPhone);
            //List<string> tempWxAts = GetCommitAtUser(commitList, MyConfiguration.UserConf.Robot?.WxAtName);
            List<string> tempWxPhoneAts = GetCommitAtUser(commitList, MyConfiguration.UserConf?.Robot.WxAtPhone);
            List<string> tempWxAts = GetUnFindCommitAtUser(commitList, MyConfiguration.UserConf?.Robot.WxAtPhone);

            if (commitInfo.BuildState == "was successful ")
            {
                ShowMessage(string.Format("start deployment"));
                await SendRobotMessageAsync(string.Format("【{0}】({1}) build sucess\r\n ➤ start deployment\r\n{2}", yourBuildKey,buildNum, nowCommit), null, tempDdAts, tempWxAts, tempWxPhoneAts);
            }
            else if(commitInfo.BuildState == "did not complete ")
            {
                ShowMessage("build aborted", false);
                await SendRobotMessageAsync(string.Format("⛔⛔⛔⛔⛔⛔⛔⛔⛔⛔⛔⛔\r\n【{0}】({1}) buid aborted\r\n{4}\r\nBuildState : [{2}]\r\n{3}\r\n⛔⛔⛔⛔⛔⛔⛔⛔⛔⛔⛔⛔", yourBuildKey, buildNum, commitInfo.BuildState, nowCommit, nowBambooProjectInfoStr), null, tempDdAts, tempWxAts, tempWxPhoneAts, wxRobotUrl);
                return DeploymentResult.Cancel;
            }
            else
            {
                ShowMessage(string.Format("build fial [{0}]", commitInfo.BuildState), false);
                string tempBambooErrorLog = await bamboodeploymentMan.GetBuildLogs(buildNum);
                tempBambooErrorLog = DealErrorLog(tempBambooErrorLog, 400 , "error");
                await SendRobotMessageAsync(string.Format("💔💔💔💔💔💔💔💔💔💔💔💔\r\n【{0}】({1}) buid fail\r\n{5}\r\nBuildState : [{2}]\r\n{3}\r\n{4}\r\n💔💔💔💔💔💔💔💔💔💔💔💔", yourBuildKey, buildNum,commitInfo.BuildState, nowCommit, tempBambooErrorLog, nowBambooProjectInfoStr), null, tempDdAts, tempWxAts, tempWxPhoneAts);
                return DeploymentResult.Failed;
            }

            //rancher
            if(!string.IsNullOrEmpty( MyConfiguration.UserConf.PrivateBambooProjectBaseWorkloadPath)&& workloadPath=="auto")
            {
                workloadPath = string.Format("{0}:{1}", MyConfiguration.UserConf.PrivateBambooProjectBaseWorkloadPath, bambooProjectInfo.projectName?? "NullProjectName");
            }

            if (!string.IsNullOrEmpty(workloadPath))
            {
                string tempUpdataInfo = await rancherDeploymentMan.GetWorkloadUpdataInfo(workloadPath, image);
                if (tempUpdataInfo == null)
                {
                    ShowMessage("not find docker container in rancher", false);
                    await SendRobotMessageAsync(string.Format("【{0}】 ({1}) build success {2}\r\n{5}\r\n  ➤ not find docker container in rancher\r\n  ◴  [ {3} > {4} ]", yourBuildKey, buildNum, "", startTime.ToString("HH:mm:ss"), (startTime - DateTime.Now).ToString(@"hh\:mm\:ss"), nowBambooProjectInfoStr), null, tempDdAts, tempWxAts, tempWxPhoneAts, wxRobotUrl);
                }
                else
                {
                    string nowImage = await rancherDeploymentMan.UpdataWorkload(workloadPath, tempUpdataInfo) ?? "can not get image";
                    string workloads = workloadPath;
                    string tempWorkloadKey = workloads.Remove(0, workloads.LastIndexOf(':') + 1);

                    string nowRancherInfoStr = string.Format("  ◎ workload: {0}\r\n  ◎ image: {1}", tempWorkloadKey, nowImage);
                    ShowMessage("GetWorkloadState", false);
                    string workloadState = null;
                    for (int i = 0; i < 180; i++)
                    {
                        await Task.Delay(5000);
                        workloadState = await rancherDeploymentMan.GetWorkloadState(workloads);
                        if (workloadState == null)
                        {
                            await Task.Delay(5000);
                            workloadState = (await rancherDeploymentMan.GetWorkloadState(workloads)) ?? await rancherDeploymentMan.GetWorkloadState(workloads);
                        }
                        if (workloadState == null)
                        {
                            break;
                        }
                        if (workloadState == "active")
                        {
                            break;
                        }
                        if (workloadState == "NotFound")
                        {
                            break;
                        }
                        if (i == 120)
                        {
                            await SendRobotMessageAsync(string.Format("🕝🕝🕝🕝🕝🕝🕝🕝🕝🕝🕝🕝\r\n【{0}】 ({1}) build success\r\n{2}\r\ndocker container update more than 10 minutes \r\n ◎ UpdateState : [{3}]\r\n🕝🕝🕝🕝🕝🕝🕝🕝🕝🕝🕝🕝\r\n", yourBuildKey, buildNum, nowRancherInfoStr, workloadState), null, tempDdAts, tempWxAts, tempWxPhoneAts, wxRobotUrl);
                        }
                    }

                    if (workloadState == null)
                    {
                        ShowMessage("can not get docker container update state", false);
                        await SendRobotMessageAsync(string.Format("💔【{0}】 ({1}) build success \r\n{5}\r\n  ➤ can not get docker container update state\r\n{2}\r\n  ◴  [ {3} > {4} ]", yourBuildKey, buildNum, nowRancherInfoStr, startTime.ToString("HH:mm:ss"), (startTime - DateTime.Now).ToString(@"hh\:mm\:ss"), nowBambooProjectInfoStr), null, tempDdAts, tempWxAts, tempWxPhoneAts, wxRobotUrl);
                        return DeploymentResult.Failed;
                    }
                    else if (workloadState == "active")
                    {
                        ShowMessage("docker container update complete", false);
                        await SendRobotMessageAsync(string.Format("【{0}】 ({1}) build success \r\n{5}\r\n  ➤ docker container update complete\r\n{2}\r\n  ◴  [ {3} > {4} ]", yourBuildKey, buildNum, nowRancherInfoStr, startTime.ToString("HH:mm:ss"), (startTime - DateTime.Now).ToString(@"hh\:mm\:ss"), nowBambooProjectInfoStr), null, tempDdAts, tempWxAts, tempWxPhoneAts, wxRobotUrl);
                    }
                    else if (workloadState == "NotFound")
                    {
                        ShowMessage("not find docker container (may don't need)", false);
                        await SendRobotMessageAsync(string.Format("💔【{0}】 ({1}) build success \r\n{5}\r\n  ➤ not find docker container\r\n{2}\r\n  ◴  [ {3} > {4} ]", yourBuildKey, buildNum, nowRancherInfoStr, startTime.ToString("HH:mm:ss"), (startTime - DateTime.Now).ToString(@"hh\:mm\:ss"), nowBambooProjectInfoStr), null, tempDdAts, tempWxAts, tempWxPhoneAts, wxRobotUrl);
                    }
                    else
                    {
                        ShowMessage("docker container update time out", false);
                        string tempErrorLog;
                        try
                        {
                            tempErrorLog = await rancherDeploymentMan.GetWorkloadErrorPodLog(workloads);
                            tempErrorLog = DealErrorLog(tempErrorLog);
                        }
                        catch (Exception ex)
                        {
                            ShowMessage(ex.ToString(), false);
                            tempErrorLog = "Can not get error logs with exception";
                        }
                        await SendRobotMessageAsync(string.Format("💔💔💔💔💔💔💔💔💔💔💔💔\r\n【{0}】 ({1}) build success \r\n{7}\r\n  ➤ docker container update time out \r\n{2}\r\n ◎ UpdateState : [{3}] \r\n  ◴  [ {4} > {5} ]\r\n💔💔💔💔💔💔💔💔💔💔💔💔\r\n{6}", yourBuildKey, buildNum, nowRancherInfoStr, workloadState, startTime.ToString("HH:mm:ss"), (startTime - DateTime.Now).ToString(@"hh\:mm\:ss"), tempErrorLog, nowBambooProjectInfoStr), null, tempDdAts, tempWxAts, tempWxPhoneAts, wxRobotUrl);
                        return DeploymentResult.Timeout;
                    }
                }
            }
            else
            {
                ShowMessage("not find docker container (may don't need)", false);
                await SendRobotMessageAsync(string.Format("【{0}】 ({1}) build success {2}\r\n{5}\r\n  ➤ not config docker container\r\n  ◴  [ {3} > {4} ]", yourBuildKey, buildNum, "", startTime.ToString("HH:mm:ss"), (startTime - DateTime.Now).ToString(@"hh\:mm\:ss"), nowBambooProjectInfoStr), null, tempDdAts, tempWxAts, tempWxPhoneAts, wxRobotUrl);
            }
            return DeploymentResult.Succeed;
        }

        public static async Task<bool> CancelDeploymentForPrivateBambooAsync(string buildNum)
        {
            return await bamboodeploymentMan.CanceltBuildAsync(buildNum);
        }

        private static async Task<bool> WaitDeployForBambooAsync(string deploymentResultId)
        {
            for (int i = 0; i < 100; i++)
            {
                await Task.Delay(5000);
                if (await bamboodeploymentMan.GetDeployStatusAsync(deploymentResultId) == "SUCCESS")
                {
                    return true;
                }
            }
            return false;
        }

        public static async Task<DeploymentResult> DeploymentForRancherAsync(bool needCommit, string rancherProjectId, string rancherPipelineId, string branch = "master", string rancherProjectName = null)
        {
            if (!await rancherDeploymentMan.GetSeviceStateAsync())
            {
                ShowMessage("rancher is not in sevice");
                return DeploymentResult.Failed;
            }
            if (string.IsNullOrEmpty(rancherProjectId) || string.IsNullOrEmpty(rancherPipelineId))
            {
                ShowMessage("RancherProjectId or RancherPipelineId is null");
                return DeploymentResult.Failed;
            }
            CommitInfo commitInfo = await rancherDeploymentMan.GetCommitAsync(rancherProjectId, rancherPipelineId, branch);
            //if(!(commitInfo!=null && commitInfo.CommitList?.Count>0))// null > 0 is false
            //{
            //    if(needCommit)
            //    {
            //        ShowMessage("this rancherProject is no commit");
            //        return false;
            //    }
            //}

            List<KeyValuePair<string, string>> commitList = commitInfo?.CommitList;
            string nowCommit = GetCommitStr(commitList, string.Format("{0} ({1})", commitInfo?.BuildState ?? "nukonw project", commitInfo?.Branch ?? "nukonw branch"));
            if (string.IsNullOrEmpty(nowCommit))
            {
                ShowMessage("no commit", false);
                if (needCommit)
                {
                    return DeploymentResult.Failed;
                }
            }
            else
            {
                ShowMessage(nowCommit, false);
                await SendRobotMessageAsync(nowCommit, "commit");
            }

            List<string> tempDdAts = GetCommitAtUser(commitList, MyConfiguration.UserConf.Robot?.DdAtPhone);
            //List<string> tempWxAts = GetCommitAtUser(commitList, MyConfiguration.UserConf.Robot?.WxAtName);
            List<string> tempWxPhoneAts = GetCommitAtUser(commitList, MyConfiguration.UserConf?.Robot.WxAtPhone);
            List<string> tempWxAts = GetUnFindCommitAtUser(commitList, MyConfiguration.UserConf?.Robot.WxAtPhone);

            string runId = await rancherDeploymentMan.StartDeployAsync(rancherProjectId, rancherPipelineId, branch);
            if (string.IsNullOrEmpty(runId))
            {
                ShowMessage(string.Format("deployment fial [{0}]", commitInfo?.BuildState ?? rancherPipelineId), false);
                await SendRobotMessageAsync(string.Format("💔💔💔💔💔💔💔💔💔💔💔💔\r\ndeployment fail [{0}]\r\n[{1}]\r\n💔💔💔💔💔💔💔💔💔💔💔💔", commitInfo?.BuildState ?? rancherPipelineId, rancherProjectName ?? ""), null, tempDdAts, tempWxAts, tempWxPhoneAts);
                return DeploymentResult.Failed;
            }
            RancherDeployState firstRancherDeployState = await rancherDeploymentMan.GetDeployStatusAsync(rancherProjectId, rancherPipelineId, runId);
            StringBuilder stringBuilder = new StringBuilder(string.Format("【{0}】({1}) start deployment\r\n[{2}]\r\n", commitInfo?.BuildState ?? rancherPipelineId, runId, rancherProjectName ?? ""));
            if (firstRancherDeployState.Stages?.Count > 0)
            {
                foreach (var tempState in firstRancherDeployState.Stages)
                {
                    stringBuilder.AppendLine(string.Format("<{0}>:{1}", tempState.Name, tempState.State));
                }
            }

            ShowMessage(string.Format("start deployment"));
            await SendRobotMessageAsync(stringBuilder.ToString(), null, tempDdAts, tempWxAts, tempWxPhoneAts);

            RancherDeployState resultRancherDeployState = await WaitDeployForRancherAsync(rancherProjectId, rancherPipelineId, runId);

            stringBuilder.Clear();
            stringBuilder = new StringBuilder(string.Format("【{0}】（{1}） deployment {2}\r\n[{3}]\r\n", commitInfo?.BuildState, runId, resultRancherDeployState.ExecutionState == "Success" ? "complete" : resultRancherDeployState.ExecutionState, rancherProjectName ?? ""));
            if (resultRancherDeployState.Stages?.Count > 0)
            {
                foreach (var tempState in resultRancherDeployState.Stages)
                {
                    stringBuilder.AppendLine(string.Format("<{0}>:{1}   [{2} - {3}]", tempState.Name, tempState.State, tempState.Start, tempState.End));
                }
            }

            if (resultRancherDeployState.ExecutionState == "Success")
            {
                ShowMessage("deployment complete");
                await SendRobotMessageAsync(stringBuilder.ToString(), null, tempDdAts, tempWxAts, tempWxPhoneAts);
            }
            else
            {
                ShowMessage("deployment fial");
                string nowErrorMes = string.Empty;
                for (int i = 0; i < resultRancherDeployState.Stages.Count; i++)
                {
                    if (resultRancherDeployState.Stages[i].State != "Success")
                    {
                        nowErrorMes = await rancherDeploymentMan.GetDeployMessageAsync(rancherProjectId, rancherPipelineId, runId, i.ToString());
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(nowErrorMes) && nowErrorMes.Length > 1500)
                {
                    nowErrorMes = nowErrorMes.Substring(nowErrorMes.Length - 1500);
                }
                await SendRobotMessageAsync(string.Format("💔💔💔💔💔💔💔💔💔💔💔💔\r\n{0}\r\n💔💔💔💔💔💔💔💔💔💔💔💔\r\n\r\n{1}", stringBuilder.ToString(), nowErrorMes ?? "can not get error log in rancher"), null, tempDdAts, tempWxAts, tempWxPhoneAts);
                return DeploymentResult.Failed;
            }
            return DeploymentResult.Succeed;
        }

        private static async Task<RancherDeployState> WaitDeployForRancherAsync(string rancherProjectId, string rancherPipelineId, string runId)
        {
            RancherDeployState nowRancherDeployState = null;
            int retryTime = 5;
            StringBuilder stringBuilder = new StringBuilder(100);
            ShowMessage("WaitDeployForRancherAsync");
            for (int i = 0; i < 150; i++)
            {
                await Task.Delay(5000);
                nowRancherDeployState = await rancherDeploymentMan.GetDeployStatusAsync(rancherProjectId, rancherPipelineId, runId);
                if (nowRancherDeployState.ExecutionState == "unknow")
                {
                    retryTime--;
                    if (retryTime < 0)
                    {
                        return nowRancherDeployState;
                    }
                    continue;
                }
                retryTime = 5;

                stringBuilder.Clear();
                if (nowRancherDeployState.Stages?.Count > 0)
                {
                    foreach (var tempState in nowRancherDeployState.Stages)
                    {
                        stringBuilder.Append(string.Format("<{0}>:{1} [{2} - {3}]", tempState.Name, tempState.State, tempState.Start, tempState.End));
                    }
                }
                ClearCurrentConsoleLineEx(300);
                Console.Write(stringBuilder.ToString());
                if (nowRancherDeployState.ExecutionState == "Success" || nowRancherDeployState.ExecutionState == "Failed" || nowRancherDeployState.ExecutionState == "Aborted")
                {
                    return nowRancherDeployState;
                }
            }
            nowRancherDeployState.ExecutionState = "timeout";
            return nowRancherDeployState;
        }

        private static async Task<DeploymentResult> BaseDeploymentForKubesphereAsync(KubeSphereDeploymentHelper nowKubeSphereDeployment,bool needCommit, string devop, string pipeline, string workloads, string triggerUser = null,Dictionary<string, string> configDc = null, Action<string, string> pushMessageAction = null, string wxRobotUrl = null, string fsRobotUrl = null, string fsChatId =null, bool isPushStartMessage = true)
        {
            if (!await nowKubeSphereDeployment.GetSeviceStateAsync())
            {
                ShowMessage("KubeSphere is not in sevice");
                return DeploymentResult.UnStart;
            }
            if (string.IsNullOrEmpty(devop) || string.IsNullOrEmpty(pipeline))
            {
                ShowMessage("devop or pipeline is null");
                return DeploymentResult.UnStart;
            }

            DateTime startTime = DateTime.Now;
            if(fsChatId==null)
            {
                fsChatId = feishuRobotChatId;
            }
            DeploymentExecuteStatus deploymentExecuteStatus = new DeploymentExecuteStatus()
            {
                Status = ExecuteStatus.Triggered,
                Devop = devop,
                Pipeline = pipeline,
                Workloads = workloads,
                Operator = triggerUser??"ci",
                ConfigDc = configDc,
                WxRobotUrl = wxRobotUrl,
                FsRobotUrl = fsRobotUrl,
                FsApplicationChatId = fsChatId,
                Remark = configDc.ToStringDetail() ?? ""
            };
            ExecuteTimePredict.FillPredictTime(deploymentExecuteStatus);

            Func<ValueTask> PushFeishuCard = new Func<ValueTask>(async() => {
                await MessagePushHelper.PushInterative(deploymentExecuteStatus);
            });

            //Func<ValueTask> PushFeishuCard = new Func<ValueTask>( () => {
            //    return MessagePushHelper.PushInterative(deploymentExecuteStatus);
            //});

            var startResult = await nowKubeSphereDeployment.StartDeployAsync(devop, pipeline, configDc);
            string runId = startResult.Item1;
            deploymentExecuteStatus.ConfigDc = startResult.Item3;
            deploymentExecuteStatus.BuildDetailUri = startResult.Item2;

            if (string.IsNullOrEmpty(runId))
            {
                ShowMessage(string.Format("deployment fial [{0}]", pipeline), false);
                deploymentExecuteStatus.Status = ExecuteStatus.BuildError;
                deploymentExecuteStatus.TimeLine.EndDeploymentTime = DeploymentTimeline.GetTimestamp();
                await PushFeishuCard();
                await SendRobotMessageAsync(string.Format("💔💔💔💔💔💔💔💔💔💔💔💔\r\ndeployment fail [{0}]\r\n[{1}]\r\n💔💔💔💔💔💔💔💔💔💔💔💔", devop, pipeline), null, null, null, null, wxRobotUrl, fsRobotUrl);
                return DeploymentResult.Failed;
            }
            else
            {
                deploymentExecuteStatus.RunId = runId;
                deploymentExecuteStatus.Status = ExecuteStatus.BuildQueued;
                deploymentExecuteStatus.TimeLine.StartBuildTime = DeploymentTimeline.GetTimestamp();
                if (isPushStartMessage)
                {
                    await SendRobotMessageAsync(string.Format("【{0}】 ({1}) start build {2}", pipeline, runId, configDc.ToStringDetail() ?? ""), null, null, null, null, wxRobotUrl, fsRobotUrl);
                }
                await PushFeishuCard();
                //pushMessageAction(startResult.Item2, runId)
                pushMessageAction?.Invoke(startResult.Item2, runId);
            }

            CommitInfo commitInfo = new CommitInfo() { BuildResult = "UNSTART" }; 
            bool isGetCommit = false;
            List<string> tempDdAts = null;
            List<string> tempWxAts = null;
            List<string> tempWxPhoneAts = null;

            for (int i = 0; i < 180; i++)
            {
                await Task.Delay(5000);
                commitInfo = await nowKubeSphereDeployment.GetGitCommitInfoAsync(devop, pipeline, runId);
                //获取本次commit信息，如果发现commit进入并确认前面有没有失败的发布的commit，如果没有commit，后面逻辑会直接去寻找前面失败的发布commit
                if (!isGetCommit && commitInfo.CommitList != null && commitInfo.CommitList.Count > 0)
                {
                    isGetCommit = true;
                    var tempFialCommit = await nowKubeSphereDeployment.GetRecentFailCommitAsync(devop, pipeline, runId);
                    if (tempFialCommit != null && tempFialCommit.CommitList != null && tempFialCommit.CommitList.Count > 0)
                    {
                        foreach (var tempKvp in tempFialCommit.CommitList)
                        {
                            commitInfo.CommitList.Add(tempKvp);
                        }
                    }
                    string nowCommit = GetCommitStr(commitInfo.CommitList, null, false);
                    tempDdAts = GetCommitAtUser(commitInfo.CommitList, MyConfiguration.UserConf.Robot?.DdAtPhone);
                    //tempWxAts = GetCommitAtUser(commitInfo.CommitList, MyConfiguration.UserConf.Robot?.WxAtName);
                    tempWxPhoneAts = GetCommitAtUser(commitInfo.CommitList, MyConfiguration.UserConf?.Robot.WxAtPhone);
                    //tempWxAts =GetCommitUser(commitInfo.CommitList).RemoveAll((string obj) => commitInfo.CommitList.Exists((KeyValuePair<string, string> kv) => kv.Key == obj));
                    tempWxAts = GetUnFindCommitAtUser(commitInfo.CommitList, MyConfiguration.UserConf?.Robot.WxAtPhone);
                    ShowMessage(nowCommit, false);
                    deploymentExecuteStatus.BuildCommitInfo.CommitList = commitInfo.CommitList;
                    deploymentExecuteStatus.BuildCommitInfo.Branch = commitInfo.Branch;
                    deploymentExecuteStatus.AtList = tempWxAts;
                    await PushFeishuCard();
                    await SendRobotMessageAsync(string.Format("【{0}】 ({1}) commit {2}\r\n{3}", pipeline, runId, configDc.ToStringDetail() ?? "", nowCommit), null, tempDdAts, tempWxAts, tempWxPhoneAts, wxRobotUrl, fsRobotUrl);
                }
                if (commitInfo.BuildState == "QUEUED")
                {
                    deploymentExecuteStatus.Status = ExecuteStatus.BuildQueued;
                    await PushFeishuCard();
                }
                else if (commitInfo.BuildState == "RUNNING")
                {
                    deploymentExecuteStatus.Status = ExecuteStatus.BuildRunning;
                    if(deploymentExecuteStatus.TimeLine.EndBuildQueueTime==0)
                    {
                        deploymentExecuteStatus.TimeLine.EndBuildQueueTime = DeploymentTimeline.GetTimestamp();
                    }
                    await PushFeishuCard();
                }
                else if (commitInfo.BuildState == "FINISHED")
                {
                    break;
                }
                else
                {
                    ShowMessage($"[unknow BuildState] : {commitInfo.BuildState}");
                }

                if (i == 120)
                {
                    await SendRobotMessageAsync(string.Format("🕝🕝🕝🕝🕝🕝🕝🕝🕝🕝🕝🕝\r\n【{0}】 ({1}) has build more than 10 minutes \r\n ◎ BuildState : [{2}]\r\n🕝🕝🕝🕝🕝🕝🕝🕝🕝🕝🕝🕝", pipeline, runId, commitInfo.BuildState ?? "UNKNOWN"), null, tempDdAts, tempWxAts, tempWxPhoneAts, wxRobotUrl, fsRobotUrl);
                }
                
            }

            //如果前面没有发现commit这里寻找临近失败的构建里的包含的commit，如果都没有则为no commit
            string moreCommit = null;
            if (isGetCommit == false)
            {
                var tempFialCommit = await nowKubeSphereDeployment.GetRecentFailCommitAsync(devop, pipeline, runId);
                if (tempFialCommit != null && tempFialCommit.CommitList != null && tempFialCommit.CommitList.Count > 0)
                {
                    moreCommit = GetCommitStr(tempFialCommit.CommitList, null, false);
                    tempDdAts = GetCommitAtUser(tempFialCommit.CommitList, MyConfiguration.UserConf.Robot?.DdAtPhone);
                    tempWxPhoneAts = GetCommitAtUser(tempFialCommit.CommitList, MyConfiguration.UserConf?.Robot.WxAtPhone);
                    tempWxAts = GetUnFindCommitAtUser(tempFialCommit.CommitList, MyConfiguration.UserConf?.Robot.WxAtPhone);
                    ShowMessage(moreCommit, false);
                    deploymentExecuteStatus.BuildCommitInfo.CommitList = tempFialCommit.CommitList;
                    deploymentExecuteStatus.BuildCommitInfo.Branch = commitInfo.Branch;
                    deploymentExecuteStatus.AtList = tempWxAts;
                    await PushFeishuCard();
                }
                else
                {
                    ShowMessage("no commit", false);
                }
            }

            //结束Build
            if (commitInfo.BuildState != "FINISHED") // 超时
            {
                ShowMessage("time out", false);
                string tempErrorLog = await nowKubeSphereDeployment.GetDeployErrorMessageAsync(devop, pipeline, runId);
                string tempShowErrorLog = DealErrorLog(tempErrorLog);
                deploymentExecuteStatus.Status = ExecuteStatus.BuildTimeOut;
                deploymentExecuteStatus.TimeLine.EndBuildTime = DeploymentTimeline.GetTimestamp();
                deploymentExecuteStatus.TimeLine.EndDeploymentTime = DeploymentTimeline.GetTimestamp();
                deploymentExecuteStatus.BuildLog = tempErrorLog;
                await PushFeishuCard();
                await SendRobotMessageAsync(string.Format("💔💔💔💔💔💔💔💔💔💔💔💔\r\n【{0}】 ({1}) build is timeout\r\nBuildState : [{2}]\r\n{3}💔💔💔💔💔💔💔💔💔💔💔💔\r\n{4}", pipeline, runId, commitInfo.BuildState ?? "UNKNOWN", moreCommit ?? "", tempShowErrorLog), null, tempDdAts, tempWxAts, tempWxPhoneAts, wxRobotUrl);
                return DeploymentResult.Timeout;
            }

            if (commitInfo.BuildResult == "SUCCESS") // 成功  "ABORTED" 被取消  "UNKNOWN" 正在进行
            {
                ShowMessage("build SUCCESS", false);
                if (isGetCommit == false)
                {
                    string tempEndStr = string.Format("  ➤ build sucess \r\n{0}", string.IsNullOrEmpty(workloads) ?
                           string.Format("  ➤ complete\r\n  ◴  [ {0} > {1} ]", startTime.ToString("HH:mm:ss"), (startTime - DateTime.Now).ToString(@"hh\:mm\:ss")) :
                           "  ➤ then will update the docker container");
                    if (!string.IsNullOrEmpty(moreCommit))
                    {
                        await SendRobotMessageAsync(string.Format("【{0}】 ({1}) commit {2}\r\n{3}\r\n{4}", pipeline, runId, configDc.ToStringDetail() ?? "", moreCommit, tempEndStr), null, tempDdAts, tempWxAts, tempWxPhoneAts, wxRobotUrl, fsRobotUrl);
                    }
                    else
                    {
                        ShowMessage("no commit", false);
                        await SendRobotMessageAsync(string.Format("【{0}】 ({1}) commit {2}\r\n  ◎no commit\r\n{3}", pipeline, runId, configDc.ToStringDetail() ?? "", tempEndStr), null, tempDdAts, tempWxAts, tempWxPhoneAts, wxRobotUrl, fsRobotUrl);
                    }
                }
                deploymentExecuteStatus.Status = ExecuteStatus.BuildSuccess;
                deploymentExecuteStatus.TimeLine.EndBuildTime = DeploymentTimeline.GetTimestamp();
                await PushFeishuCard();

                //开始Launch（更新容器）
                if (!string.IsNullOrEmpty(workloads))
                {
                    bool isTry_APPNAME = false;
                    string tempWorkloadKey = workloads.Remove(0, workloads.LastIndexOf(':') + 1);
                    ShowMessage("GetWorkloadState", false);
                    deploymentExecuteStatus.TimeLine.StartLaunchTime = DeploymentTimeline.GetTimestamp();
                    deploymentExecuteStatus.LaunchDetailUri = rancherDeploymentMan.GetWorkloadDetailUri(workloads);
                    string workloadState = null;
                    for (int i = 0; i < 180; i++)
                    {
                        await Task.Delay(5000);
                        workloadState = await rancherDeploymentMan.GetWorkloadState(workloads);
                        if (workloadState == null)
                        {
                            await Task.Delay(5000);
                            workloadState = (await rancherDeploymentMan.GetWorkloadState(workloads)) ?? await rancherDeploymentMan.GetWorkloadState(workloads);
                        }
                        if (workloadState == null)
                        {
                            break;
                        }
                        if (workloadState == "active")
                        {
                            break;
                        }
                        if (workloadState == "NotFound")
                        {
                            if (isTry_APPNAME)
                            {
                                break;
                            }
                            else
                            {
                                isTry_APPNAME = true;
                                if (startResult.Item3 != null && startResult.Item3.ContainsKey("APP_NAME"))
                                {
                                    tempWorkloadKey = startResult.Item3["APP_NAME"];
                                    workloads = workloads.Remove(workloads.LastIndexOf(':') + 1) + tempWorkloadKey;
                                    ShowMessage($"updata workloads with RunConfig : {workloads}");
                                    deploymentExecuteStatus.LaunchDetailUri = rancherDeploymentMan.GetWorkloadDetailUri(workloads);
                                }
                                else
                                {
                                    ShowMessage("not find workloads and can not get RunConfig from 「StartDeployAsync」");
                                    break;
                                }
                            }

                        }
                        if (i == 120)
                        {
                            await SendRobotMessageAsync(string.Format("🕝🕝🕝🕝🕝🕝🕝🕝🕝🕝🕝🕝\r\n【{0}】 ({1}) build success\r\n【{2}】docker container update more than 10 minutes \r\n ◎ UpdateState : [{3}]\r\n🕝🕝🕝🕝🕝🕝🕝🕝🕝🕝🕝🕝\r\n", pipeline, runId, tempWorkloadKey, workloadState), null, tempDdAts, tempWxAts, tempWxPhoneAts, wxRobotUrl, fsRobotUrl);
                        }
                        deploymentExecuteStatus.Status = ExecuteStatus.Launching;
                        await PushFeishuCard();
                    }

                    if (workloadState == null)
                    {
                        ShowMessage("can not get docker container update state", false);
                        deploymentExecuteStatus.Status = ExecuteStatus.LaunchError;
                        deploymentExecuteStatus.TimeLine.EndLaunchTime = DeploymentTimeline.GetTimestamp();
                        deploymentExecuteStatus.TimeLine.EndDeploymentTime = DeploymentTimeline.GetTimestamp();
                        await PushFeishuCard();
                        await SendRobotMessageAsync(string.Format("💔【{0}】 ({1}) build success {2}\r\n  ➤ can not get docker container update state\r\n  ◴  [ {3} > {4} ]", pipeline, runId, configDc.ToStringDetail() ?? "", startTime.ToString("HH:mm:ss"), (startTime - DateTime.Now).ToString(@"hh\:mm\:ss")), null, tempDdAts, tempWxAts, tempWxPhoneAts, wxRobotUrl, fsRobotUrl);
                        return DeploymentResult.Failed;
                    }
                    else if (workloadState == "active")
                    {
                        ShowMessage("docker container update complete", false);
                        deploymentExecuteStatus.Status = ExecuteStatus.LaunchSuccess;
                        deploymentExecuteStatus.TimeLine.EndLaunchTime = DeploymentTimeline.GetTimestamp();
                        deploymentExecuteStatus.TimeLine.EndDeploymentTime = DeploymentTimeline.GetTimestamp();
                        await PushFeishuCard();
                        await SendRobotMessageAsync(string.Format("【{0}】 ({1}) build success {2}\r\n  ➤ docker container update complete\r\n  ◴  [ {3} > {4} ]", pipeline, runId, configDc.ToStringDetail() ?? "", startTime.ToString("HH:mm:ss"), (startTime - DateTime.Now).ToString(@"hh\:mm\:ss")), null, tempDdAts, tempWxAts, tempWxPhoneAts, wxRobotUrl, fsRobotUrl);
                    }
                    else if (workloadState == "NotFound")
                    {
                        ShowMessage("not find docker container (may don't need)", false);
                        deploymentExecuteStatus.Status = ExecuteStatus.LanuchSkip;
                        deploymentExecuteStatus.TimeLine.EndLaunchTime = DeploymentTimeline.GetTimestamp();
                        await PushFeishuCard();
                        await SendRobotMessageAsync(string.Format("【{0}】 ({1}) build success {2}\r\n  ➤ not find docker container\r\n  ◴  [ {3} > {4} ]", pipeline, runId, configDc.ToStringDetail() ?? "", startTime.ToString("HH:mm:ss"), (startTime - DateTime.Now).ToString(@"hh\:mm\:ss")), null, tempDdAts, tempWxAts, tempWxPhoneAts, wxRobotUrl, fsRobotUrl);
                    }
                    else
                    {
                        ShowMessage("docker container update time out", false);
                        string tempErrorLog = "";
                        string tempShowErrorLog = "";
                        try
                        {
                            tempErrorLog = await rancherDeploymentMan.GetWorkloadErrorPodLog(workloads);
                            tempShowErrorLog = DealErrorLog(tempErrorLog);
                        }
                        catch (Exception ex)
                        {
                            ShowMessage(ex.ToString(), false);
                            tempShowErrorLog = "Get error logs fail with exception";
                        }
                        deploymentExecuteStatus.Status = ExecuteStatus.LaunchTimeOut;
                        deploymentExecuteStatus.TimeLine.EndLaunchTime = DeploymentTimeline.GetTimestamp();
                        deploymentExecuteStatus.TimeLine.EndDeploymentTime = DeploymentTimeline.GetTimestamp();
                        deploymentExecuteStatus.LaunchLog = tempErrorLog;
                        await PushFeishuCard();
                        await SendRobotMessageAsync(string.Format("💔💔💔💔💔💔💔💔💔💔💔💔\r\n【{0}】 ({1}) build success {2}\r\n  ➤ docker container update time out \r\n ◎ UpdateState : [{3}] \r\n  ◴  [ {4} > {5} ]\r\n💔💔💔💔💔💔💔💔💔💔💔💔\r\n{6}", pipeline, runId, configDc.ToStringDetail() ?? "", workloadState, startTime.ToString("HH:mm:ss"), (startTime - DateTime.Now).ToString(@"hh\:mm\:ss"), tempErrorLog), null, tempDdAts, tempWxAts, tempWxPhoneAts, wxRobotUrl, fsRobotUrl);
                        return DeploymentResult.Timeout;
                    }
                }

                else
                {
                    ShowMessage("not find docker container (may don't need)", false);
                    deploymentExecuteStatus.Status = ExecuteStatus.LanuchSkip;
                    await PushFeishuCard();
                    await SendRobotMessageAsync(string.Format("【{0}】 ({1}) build success {2}\r\n  ➤ not find docker container\r\n  ◴  [ {3} > {4} ]", pipeline, runId, configDc.ToStringDetail() ?? "", startTime.ToString("HH:mm:ss"), (startTime - DateTime.Now).ToString(@"hh\:mm\:ss")), null, tempDdAts, tempWxAts, tempWxPhoneAts, wxRobotUrl, fsRobotUrl);
                }
            }

            else if (commitInfo.BuildResult == "FAILURE") //失败
            {
                string tempErrorLog = await nowKubeSphereDeployment.GetDeployErrorMessageAsync(devop, pipeline, runId);
                string tempShowErrorLog = DealErrorLog(tempErrorLog);
                ShowMessage("build fail", false);
                deploymentExecuteStatus.Status = ExecuteStatus.BuildFailed;
                deploymentExecuteStatus.TimeLine.EndBuildTime = DeploymentTimeline.GetTimestamp();
                deploymentExecuteStatus.TimeLine.EndDeploymentTime = DeploymentTimeline.GetTimestamp();
                deploymentExecuteStatus.BuildLog = tempErrorLog;
                await PushFeishuCard();
                await SendRobotMessageAsync(string.Format("💔💔💔💔💔💔💔💔💔💔💔💔\r\n【{0}】 ({1}) build is failed\r\nBuildState : [{2}]\r\n{3}💔💔💔💔💔💔💔💔💔💔💔💔\r\n{4}", pipeline, runId, commitInfo.BuildState ?? "UNKNOWN", moreCommit ?? "", tempShowErrorLog), null, tempDdAts, tempWxAts, tempWxPhoneAts, wxRobotUrl, fsRobotUrl);
                return DeploymentResult.Failed;
            }
            else if (commitInfo.BuildResult == "ABORTED") //取消
            {
                ShowMessage("build aborted", false);
                deploymentExecuteStatus.Status = ExecuteStatus.BuildCancle;
                deploymentExecuteStatus.TimeLine.EndBuildTime = DeploymentTimeline.GetTimestamp();
                deploymentExecuteStatus.TimeLine.EndDeploymentTime = DeploymentTimeline.GetTimestamp();
                await PushFeishuCard();
                await SendRobotMessageAsync(string.Format("⛔⛔⛔⛔⛔⛔⛔⛔⛔⛔⛔⛔\r\n【{0}】 ({1}) build is {2}\r\nBuildState : [{3}]\r\n{4}⛔⛔⛔⛔⛔⛔⛔⛔⛔⛔⛔⛔", pipeline, runId, commitInfo.BuildResult ?? "UNKNOWN", commitInfo.BuildState ?? "UNKNOWN", moreCommit ?? ""), null, tempDdAts, tempWxAts, tempWxPhoneAts, wxRobotUrl, fsRobotUrl);
                return DeploymentResult.Cancel;
            }
            else
            {
                ShowMessage("build error", false);
                deploymentExecuteStatus.Status = ExecuteStatus.BuildError;
                deploymentExecuteStatus.TimeLine.EndBuildTime = DeploymentTimeline.GetTimestamp();
                deploymentExecuteStatus.TimeLine.EndDeploymentTime = DeploymentTimeline.GetTimestamp();
                await PushFeishuCard();
                await SendRobotMessageAsync(string.Format("💔💔💔💔💔💔💔💔💔💔💔💔\r\n【{0}】 ({1}) build is {2}\r\nBuildState : [{3}]\r\n{4}💔💔💔💔💔💔💔💔💔💔💔💔", pipeline, runId, commitInfo.BuildResult ?? "UNKNOWN", commitInfo.BuildState ?? "UNKNOWN", moreCommit ?? ""), null, tempDdAts, tempWxAts, tempWxPhoneAts, wxRobotUrl, fsRobotUrl);
                return DeploymentResult.Failed;
            }
            ShowMessage("build complete", false);
            deploymentExecuteStatus.TimeLine.EndDeploymentTime = DeploymentTimeline.GetTimestamp();
            await PushFeishuCard();
            ExecuteTimePredict.UpdatePredictTime(deploymentExecuteStatus);
            return DeploymentResult.Succeed;
        }

        public static async Task<DeploymentResult> DeploymentForKubesphereAsync(bool needCommit, string devop, string pipeline, string workloads, string triggerUser = null,Dictionary<string, string> configDc = null, Action<string ,string> pushMessageAction = null ,string wxRobotUrl = null,string fsRobotUrl = null, string fsChatId = null, bool isPushStartMessage =true)
        {
            return await BaseDeploymentForKubesphereAsync(kubeSphereDeploymentMan, needCommit, devop, pipeline, workloads,triggerUser, configDc, pushMessageAction, wxRobotUrl, fsRobotUrl,fsChatId, isPushStartMessage);
        }

        public static async Task<DeploymentResult> DeploymentForKubesphereV3Async(bool needCommit, string devop, string pipeline, string workloads, string triggerUser = null,Dictionary<string, string> configDc = null, Action<string, string> pushMessageAction = null, string wxRobotUrl = null, string fsRobotUrl = null, string fsChatId = null, bool isPushStartMessage = true)
        {
            return await BaseDeploymentForKubesphereAsync(kubeSphereV3DeploymentMan, needCommit, devop, pipeline, workloads,triggerUser, configDc, pushMessageAction, wxRobotUrl, fsRobotUrl,fsChatId, isPushStartMessage);
        }

        public static async Task<bool> CancelDeploymentForKubesphereAsync(string devop, string pipeline, int? id = null)
        {
            return await kubeSphereDeploymentMan.CanceltRunningRunsAsync(devop, pipeline , id);
        }

        public static async Task<bool> CancelDeploymentForKubesphereV3Async(string devop, string pipeline, int? id = null)
        {
            return await kubeSphereV3DeploymentMan.CanceltRunningRunsAsync(devop, pipeline, id);
        }

        public static async Task<bool> RedeployForRancherAsync(string project, string workloads)
        {
            return await rancherDeploymentMan.RedeployPipeline(project, workloads);
        }

    }

}


