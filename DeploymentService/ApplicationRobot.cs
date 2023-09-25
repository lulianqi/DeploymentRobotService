using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeploymentRobotService.Appsetting;
using DeploymentRobotService.Controllers;
using DeploymentRobotService.DeploymentService.MyCommandLine;
using DeploymentRobotService.Models.FsModels;
using DeploymentRobotService.MyHelper;
using MyDeploymentMonitor.ExecuteHelper;

namespace DeploymentRobotService.DeploymentService
{
    public class ApplicationRobot
    {
        public static class FsRobotBusinessData
        {
            private static Dictionary<string, FsUserInfo> _fsUserDc = new Dictionary<string, FsUserInfo>();
            /// <summary>
            /// user id 与 name 的字典（业务使用时不用判空，内部确保不为null）
            /// </summary>
            public static Dictionary<string, string> FsUserIdNameDc { get; private set; } = new Dictionary<string, string>();
            /// <summary>
            /// open id 与 user 的字典（业务使用时不用判空，内部确保不为null）
            /// </summary>
            public static Dictionary<string, FsUserInfo> FsUserDc
            {
                get { return _fsUserDc; }
                set
                {
                    if (value == null)
                    {
                        _fsUserDc.Clear();
                        FsUserIdNameDc.Clear();
                    }
                    else
                    {
                        _fsUserDc = value;
                        FsUserIdNameDc.Clear();
                        foreach (var user in _fsUserDc)
                        {
                            FsUserIdNameDc.TryAdd(user.Value.user_id, user.Value.name);
                        }
                    }
                }
            }

            /// <summary>
            /// 通过user id 查询 用户名（支持 user id 及 open id） 查询失败则返回原始值
            /// </summary>
            /// <param name="user_id"></param>
            /// <returns></returns>
            public static string GetUserNameById(string  user_id)
            {
                if (string.IsNullOrEmpty(user_id))
                {
                    return "";
                    //throw new ArgumentException($"'{nameof(user_id)}' cannot be null or empty.", nameof(user_id));
                }
                if(user_id.StartsWith("ou_") && FsUserDc.ContainsKey(user_id))
                {
                    return FsUserDc[user_id].name;
                }
                if(FsUserIdNameDc.ContainsKey(user_id))
                {
                    return FsUserIdNameDc[user_id];
                }
                return user_id;
            }

            /// <summary>
            ///  通过用户昵称 查找openid （查询失败，返回null）
            /// </summary>
            /// <param name="nickname"></param>
            /// <returns></returns>
            public static string GetOpenIdByName(string nickname)
            {
                if (string.IsNullOrEmpty(nickname))
                {
                    return null;
                }
                var findUser = FsUserDc.FirstOrDefault((kv) => kv.Value.name == nickname);
                return findUser.Value?.open_id;
            }
        }

        private const string ReplyDeploymentCommandFormatData = @"已经收到您的指令，正在为您准备发布【{0}】";
        private const string ReplyCancelDeploymentCommandFormatData = @"已经收到您的指令，正在为您暂停还未完成的发布【{0}】";
        private const string LongMeaasgeFormatStr = "💥数据过多，仅为你展示了部分数据 🔗<a href=\"{0}\">查看全部</a>\n{1}";

        private static string defaultDeploymentConfigfileName = "conf.json";
        public static ExecuteTokenDevice NowExecuteTokenDevice;
        public static DeploymentQueue NowDeploymentQueue { get; private set; }
        public static WxRobotConnector WxConnector { get; private set; }
        public static FsRobotConnector FsConnector { get; private set; }


        static ApplicationRobot()
        {
            NowExecuteTokenDevice = new ExecuteTokenDevice(ExplainCommand);
            WxConnector = new WxRobotConnector();
            FsConnector = new FsRobotConnector();
            NowDeploymentQueue = new DeploymentQueue();
        }

        public ApplicationRobot()
        {
        }

        /// <summary>
        /// 初始化MyDeploymentMonitor，或更新配置
        /// </summary>
        /// <param name="fileName">配置文件文件名（文件需要在执行目录/Properties/下）</param>
        /// <returns></returns>
        public static async Task<bool> InitMyDeployment(string fileName = null)
        {
            if (fileName == null)
            {
                fileName = defaultDeploymentConfigfileName;
            }
            else
            {
                defaultDeploymentConfigfileName = fileName;
            }
            //if(await MyBambooMonitor.ShareData.MyConfiguration.InitConfigFileAsync(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "/Properties/"+fileName))
            if (await MyDeploymentMonitor.ShareData.MyConfiguration.InitConfigFileAsync(System.IO.Directory.GetCurrentDirectory() + "/Properties/" + fileName))
            {
                try
                {
                    FsRobotBusinessData.FsUserDc = await FsConnector?.NowFsHelper?.GetUsersByDepartmentExAsync();
                }
                catch(Exception ex)
                {
                    MyLogger.LogError($"FsConnector?.NowFsHelper?.GetUsersByDepartmentExAsync() Exception with {ex}");
                    MyLogger.LogInfo("尝试GetUsersByDepartmentAsync低性能版本");
                    FsRobotBusinessData.FsUserDc = await FsConnector?.NowFsHelper?.GetUsersByDepartmentAsync();
                }
                await FsConnector?.NowFsHelper?.GetChatGroupsAsync();
                await ExecuteTimePredict.UploadData();
                return true;
            }
            MyLogger.LogError("MyBambooMonitor.ShareData.MyConfiguration.InitConfigFileAsync fail");
            return false;
        }

        /// <summary>
        /// 解释命令（部分执行也会在解释中完成）
        /// </summary>
        /// <param name="yourCommand"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public static CommandInfo ExplainCommand(IRobotConnector nowRobot ,string yourCommand, string user = null)
        {
            CommandInfo commandInfo = new CommandInfo() { NowCommandType = CommandType.UnKonwCommand };
            CommandLineInfo commandLineInfo = CmdHelper.GetCommandInfo(yourCommand);
            commandInfo.NowCommandStr = commandLineInfo?.CommandName;
            switch (commandInfo.NowCommandStr)
            {
                case null:
                    commandInfo.CommandReply = "can not find your command";
                    break;
                case "hi":
                    commandInfo.NowCommandType = CommandType.SystemCommand;
                    commandInfo.CommandReply = "hi i am the service";
                    break;
                case "init":
                    commandInfo.NowCommandType = CommandType.SystemCommand;
                    CmdResult cmdResult = MyCmd.InitDeploymentMonitor(commandLineInfo.CommandOption);
                    commandInfo.CommandReply = cmdResult?.ResultText;
                    if (cmdResult?.ResultState == CmdResultState.Succeed)
                    {
                        InitMyDeployment().ContinueWith((isSucceed) => { NowDeploymentQueue.UpdateUserFavoriteProjectKeys(); _ = nowRobot.PushContent(user, isSucceed.Result ? "Successful initialization" : "Initialization failure"); });
                    }
                    break;
                case "show":
                    commandInfo.NowCommandType = CommandType.ShowInfoCommand;
                    //commandInfo.AdditionCommandValue = MyBuilder.ShowProjects();
                    commandInfo.CommandReply = MyCmd.GetProjects(commandLineInfo.CommandOption)?.ResultText;
                    break;
                case "list":
                    commandInfo.NowCommandType = CommandType.ShowInfoCommand;
                    //var runers =  MyDeployment.NowDeploymentQueue.GetRunerList(null, null, 10);
                    var runers = MyCmd.GetRunners(commandLineInfo.CommandOption);
                    commandInfo.CommandReply = runers?.ResultText ?? "not any task has found";
                    break;
                case "pwd":
                    commandInfo.NowCommandType = CommandType.ShowInfoCommand;
                    var passwd = MyCmd.GetPassword(CmdHelper.GetArgumentAndOption(commandLineInfo));
                    commandInfo.CommandReply = passwd?.ResultText ?? "can not get the passwd";
                    break;
                case "token":
                    commandInfo.NowCommandType = CommandType.ToolkitCommand;
                    var token = MyCmd.HandleToken(commandLineInfo.CommandOption, commandLineInfo.CommandArgument, user ?? "unknown", NowExecuteTokenDevice);
                    if ((token?.Flag as string) == "token")
                    {
                        token.ResultText = $"{RobotConfig.ExecuteTokenLink}?token={token.ResultText}&appChannel={nowRobot.AppChannel}";
                    }
                    commandInfo.CommandReply = token?.ResultText ?? "can not get the token";
                    break;
                case "test":
                    commandInfo.NowCommandType = CommandType.SystemCommand;
                    break;
                case "run":
                    string runKey = null;
                    if (commandLineInfo.CommandOption != null)
                    {
                        foreach (var arg in commandLineInfo.CommandOption)
                        {
                            if (arg.StartsWith("-p"))
                            {
                                runKey = arg.Remove(0, 2).Trim(' ');
                            }
                        }
                        string tempRunName = MyBuilder.CkeckBuildKey(runKey);
                        //if (tempRunName == null)
                        if (runKey != null && tempRunName == null)
                        {
                            commandInfo.NowCommandType = CommandType.ErrorCommand;
                            commandInfo.CommandReply = "can not find the project key with that parameter -p (just see show)";
                        }
                        else
                        {
                            commandInfo.NowCommandType = CommandType.DeploymentCommand;
                            DeploymentRuner nowRuner = new DeploymentRuner()
                            {
                                DeploymentKey = runKey ?? "error DeploymentKey",
                                DeploymentUser = user ?? "unknown",
                                DeploymentProjectName = tempRunName ?? "error DeploymentProjectName"
                            };
                            commandInfo.CommandReply = MyCmd.RunBuildEx(commandLineInfo.CommandOption, nowRuner ,nowRobot)?.ResultText;
                            commandInfo.Tag = nowRuner;
                        }
                    }
                    else
                    {
                        commandInfo.NowCommandType = CommandType.ErrorCommand;
                        commandInfo.CommandReply = "run command parameter error (just see run --help)";
                    }
                    break;
                default:
                    string tempBuildName = MyBuilder.CkeckBuildKey(commandInfo.NowCommandStr);
                    if (tempBuildName != null)
                    {
                        commandInfo.NowCommandType = CommandType.DeploymentCommand;
                        commandInfo.CommandReply = tempBuildName;
                        string[] tempProjectArr = new string[] { "-p " + commandInfo.NowCommandStr };

                        if (commandLineInfo.CommandOption == null || commandLineInfo.CommandOption.Length == 0)
                        {
                            commandLineInfo.CommandOption = tempProjectArr;
                        }
                        else
                        {
                            //oldArray.CopyTo(newArray, 0);
                            commandLineInfo.CommandOption = tempProjectArr.Concat(commandLineInfo.CommandOption).ToArray();
                        }
                        DeploymentRuner nowRuner = new DeploymentRuner()
                        {
                            DeploymentKey = commandInfo.NowCommandStr,
                            DeploymentUser = user ?? "unknown",
                            DeploymentProjectName = tempBuildName
                        };
                        commandInfo.CommandReply = MyCmd.RunBuildEx(commandLineInfo.CommandOption, nowRuner,nowRobot)?.ResultText;
                        commandInfo.Tag = nowRuner;
                    }
                    else if (commandInfo.NowCommandStr == "." || commandInfo.NowCommandStr == "。")
                    {
                        commandInfo.NowCommandType = CommandType.ShowInfoCommand;
                        commandInfo.CommandReply = NowDeploymentQueue.GetUserFavoriteProject(user);
                    }
                    else if (commandInfo.NowCommandStr.StartsWith('.') || commandInfo.NowCommandStr.StartsWith('。'))
                    {
                        commandInfo.NowCommandType = CommandType.ShowInfoCommand;
                        string[] tempProjectArr = new string[] { "-p " + commandInfo.NowCommandStr.Remove(0, 1) + (string.IsNullOrEmpty(commandLineInfo.CommandArgument) ? "" : " " + commandLineInfo.CommandArgument) };
                        if (commandLineInfo.CommandOption == null || commandLineInfo.CommandOption.Length == 0)
                        {
                            commandLineInfo.CommandOption = tempProjectArr;
                        }
                        else
                        {
                            commandLineInfo.CommandOption = tempProjectArr.Concat(commandLineInfo.CommandOption).ToArray();
                        }
                        commandInfo.CommandReply = MyCmd.GetProjects(commandLineInfo.CommandOption)?.ResultText;
                    }
                    break;
            }
            return commandInfo;
        }

        /// <summary>
        /// 回复用户指令，主要执行ExplainCommand，并将结果转换为可以直接被IM回复的文本
        /// </summary>
        /// <param name="nowRobot"></param>
        /// <param name="content"></param>
        /// <param name="fromUserName"></param>
        /// <returns></returns>
        public static string ReplyApplicationCmd(IRobotConnector nowRobot ,string content,string fromUserName )
        {
            string wxResponseMessage = "";
            CommandInfo commandInfo = ExplainCommand(nowRobot ,content, fromUserName);
            switch (commandInfo.NowCommandType)
            {
                case DeploymentService.CommandType.ToolkitCommand:
                case DeploymentService.CommandType.ShowInfoCommand:
                    string tempOriginalReply = commandInfo.CommandReply;
                    if (commandInfo.NowCommandStr.StartsWith("show") || commandInfo.NowCommandStr.StartsWith(".") || commandInfo.NowCommandStr.StartsWith("。"))
                    {
                        commandInfo.CommandReply = nowRobot.AddActionForGetProjectResult(commandInfo.CommandReply);
                    }

                    if (commandInfo.CommandReply.ByteLeng() > nowRobot.MaxMessageLeng)
                    {
                        string infoUrl = InfoController.GetInfoUrl(InfoController.AddInfoMessage(commandInfo.CommandReply));
                        wxResponseMessage = string.Format(LongMeaasgeFormatStr, infoUrl, commandInfo.CommandReply.Substring(0, WxDeploymentMessageHelper.maxByteLength));
                    }
                    else if (nowRobot is WxRobotConnector && commandInfo.CommandReply.ByteLeng() > WxDeploymentMessageHelper.maxByteLength)
                    {
                        wxResponseMessage = "正在为您检索信息";
                        _ = nowRobot.PushContent(fromUserName, commandInfo.CommandReply).ContinueWith((isSucceed) => { if (!isSucceed.Result) MyLogger.LogError("PushContent for ShowInfoCommand failed"); });
                    }
                    else
                    {
                        wxResponseMessage = commandInfo.CommandReply;
                    }
                    break;
                case DeploymentService.CommandType.DeploymentCommand:
                    DeploymentRuner nowRuner = (DeploymentRuner)commandInfo.Tag;
                    if (nowRuner.DeploymentUser == "unknown") nowRuner.DeploymentUser = fromUserName;
                    if (commandInfo.CommandReply == "Running")
                    {
                        wxResponseMessage = string.Format(ReplyDeploymentCommandFormatData, nowRuner.DeploymentProjectName);
                        //辅助提示可用的短码
                        string shortCode = MyDeploymentMonitor.ExecuteHelper.MyBuilder.GetShortCode(nowRuner.DeploymentProjectName);
                        if (!string.IsNullOrEmpty(shortCode) && shortCode != nowRuner.DeploymentKey)
                        {
                            wxResponseMessage = $"{wxResponseMessage}\n💬该工程可直接使用短码[{shortCode}]进行发布";
                        }

                    }
                    else if (commandInfo.CommandReply == "Canceling")
                    {
                        wxResponseMessage = string.Format(ReplyCancelDeploymentCommandFormatData, nowRuner.DeploymentProjectName);
                    }
                    else
                    {
                        wxResponseMessage = commandInfo.CommandReply;
                    }
                    //wxResponseMessage = commandInfo.AdditionCommandReply== "Running"?  string.Format(ReplyDeploymentCommandFormatData, nowRuner.DeploymentProjectName): commandInfo.AdditionCommandReply;
                    break;
                case DeploymentService.CommandType.SystemCommand:
                    wxResponseMessage = commandInfo.CommandReply;
                    break;
                case CommandType.ErrorCommand:
                    wxResponseMessage = commandInfo.CommandReply;
                    break;
                case DeploymentService.CommandType.UnKonwCommand:
                default:
                    wxResponseMessage = "unknown command\r\n" + (Appsetting.RobotConfig.HelpDoc == null ? "" : string.Format("<a href=\"{0}\">help document</a>", Appsetting.RobotConfig.HelpDoc));
                    break;
            }

            return wxResponseMessage;
        }

    }
}
