using MessageRobot.FeiShuHelper;
using MessageRobot.FeiShuHelper.Message;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MyDeploymentMonitor.ExecuteHelper
{
    class MessagePushHelper
    {
        //0:工程名称、1:runid、2:触发时间、3:剩余时间、4:流水线、5:分支及是否预发、6:标准状态、7:触发人
        private static string buildQueuedBaseContent;
        //0:工程名称、1:runid、2:触发时间、3:剩余时间、4:流水线、5:分支及是否预发、6:标准状态、7:触发人、8:build详情链接、9:build image key、10:launch image key、11:commit
        private static string buildRunningBaseContent;
        private static string buildCancelBaseContent;
        //0:工程名称、1:runid、2:触发时间、3:总耗时时间[*]、4:流水线、5:分支及是否预发、6:标准状态、7:触发人、8:build详情链接、9:build image key、10:launch image key、11:commit、12:build 错误日志、13:build 错误详情链接
        private static string buildTimeoutBaseContent;
        private static string buildFailBaseContent;
        //0:工程名称、1:runid、2:触发时间、3:剩余时间、4:流水线、5:分支及是否预发、6:标准状态、7:触发人、8:build详情链接、9:build image key、10:launch image key、11:commit、12:launch详情链接
        private static string launchRunningBaseContent;
        private static string launchCancelBaseContent;
        //没有12
        private static string launchSkipBaseContent;
        //0:工程名称、1:runid、2:触发时间、3:总耗时时间[*]、4:流水线、5:分支及是否预发、6:标准状态、7:触发人、8:build详情链接、9:build image key、10:launch image key、11:commit、12:launch详情链接、13:launch错误日志、14:launch错误详情链接
        private static string launchTimeoutBaseContent;
        private static string launchFailBaseContent;
        private static string deploymentSuccessBaseContent;
        



        private static bool isPrepared = false;

        private class ScheduleImageKeyResult
        {
            public string BuildScheduleImageKey;
            public string LaunchScheduleImageKey;
        }

        static MessagePushHelper()
        {
            using StreamReader buildQueuedStreamReader = new StreamReader($"{AppDomain.CurrentDomain.BaseDirectory}ResourceFileData/BuildQueued.json", Encoding.UTF8);
            buildQueuedBaseContent = FormatBaseContent(buildQueuedStreamReader.ReadToEnd());

            using StreamReader buildRunningStreamReader = new StreamReader($"{AppDomain.CurrentDomain.BaseDirectory}ResourceFileData/BuildRunning.json", Encoding.UTF8);
            buildRunningBaseContent = FormatBaseContent(buildRunningStreamReader.ReadToEnd());

            using StreamReader buildCancleStreamReader = new StreamReader($"{AppDomain.CurrentDomain.BaseDirectory}ResourceFileData/BuildCancel.json", Encoding.UTF8);
            buildCancelBaseContent = FormatBaseContent(buildCancleStreamReader.ReadToEnd());

            using StreamReader buildTimeoutStreamReader = new StreamReader($"{AppDomain.CurrentDomain.BaseDirectory}ResourceFileData/BuildTimeout.json", Encoding.UTF8);
            buildTimeoutBaseContent = FormatBaseContent(buildTimeoutStreamReader.ReadToEnd());

            using StreamReader buildFailStreamReader = new StreamReader($"{AppDomain.CurrentDomain.BaseDirectory}ResourceFileData/BuildFail.json", Encoding.UTF8);
            buildFailBaseContent = FormatBaseContent(buildFailStreamReader.ReadToEnd());

            using StreamReader launchRunningStreamReader = new StreamReader($"{AppDomain.CurrentDomain.BaseDirectory}ResourceFileData/LaunchRunning.json", Encoding.UTF8);
            launchRunningBaseContent = FormatBaseContent(launchRunningStreamReader.ReadToEnd());

            using StreamReader launchCancleStreamReader = new StreamReader($"{AppDomain.CurrentDomain.BaseDirectory}ResourceFileData/LaunchCancel.json", Encoding.UTF8);
            launchCancelBaseContent = FormatBaseContent(launchCancleStreamReader.ReadToEnd());

            using StreamReader launchSkipStreamReader = new StreamReader($"{AppDomain.CurrentDomain.BaseDirectory}ResourceFileData/LaunchSkip.json", Encoding.UTF8);
            launchSkipBaseContent = FormatBaseContent(launchSkipStreamReader.ReadToEnd());

            using StreamReader launchTimeoutStreamReader = new StreamReader($"{AppDomain.CurrentDomain.BaseDirectory}ResourceFileData/LaunchTimeout.json", Encoding.UTF8);
            launchTimeoutBaseContent = FormatBaseContent(launchTimeoutStreamReader.ReadToEnd());

            using StreamReader launchFailStreamReader = new StreamReader($"{AppDomain.CurrentDomain.BaseDirectory}ResourceFileData/LaunchFail.json", Encoding.UTF8);
            launchFailBaseContent = FormatBaseContent(launchFailStreamReader.ReadToEnd());

            using StreamReader deploymentSuccessStreamReader = new StreamReader($"{AppDomain.CurrentDomain.BaseDirectory}ResourceFileData/DeploymentSuccess.json", Encoding.UTF8);
            deploymentSuccessBaseContent = FormatBaseContent(deploymentSuccessStreamReader.ReadToEnd());

            isPrepared = true;
        }

        /// <summary>
        /// 将json资源文件格式化为StringFoemat需要的格式
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private static string FormatBaseContent(string content)
        {
            content = content.Replace("{", "{{");
            content = content.Replace("}", "}}");
            for(int i =0;i<20;i++)
            {
                content = content.Replace($"{{{{{i}}}}}", $"{{{i}}}");
            }
            return content;
        }

        private static string FormartFeishuMeaasgeContent(string message)
        {
            //https://en.wikipedia.org/wiki/ASCII 这里还有其他更多需要处理的转译符，这里没有全部处理，因为业务上那些字符不会出现
            //return message?.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t").Replace("\v","\\v").Replace("\x1b", "\\e");
            return (JsonConvert.ToString(message)).Trim('\"');
        }

        /// <summary>
        /// 获取正确的image key
        /// </summary>
        /// <param name="deploymentExecuteStatus"></param>
        /// <returns></returns>
        private static ScheduleImageKeyResult GetScheduleImageKey(DeploymentExecuteStatus deploymentExecuteStatus)
        {
            ScheduleImageKeyResult scheduleImageKeyResult = new ScheduleImageKeyResult()
            {
                BuildScheduleImageKey = ResourceFileData.FsImgeKeyResource.LoadProgressImageKeys[102],
                LaunchScheduleImageKey = ResourceFileData.FsImgeKeyResource.LoadProgressImageKeys[102]
            };
            if (deploymentExecuteStatus.Status== ExecuteStatus.BuildRunning||
                deploymentExecuteStatus.Status == ExecuteStatus.BuildStop || 
                deploymentExecuteStatus.Status == ExecuteStatus.BuildCancle)
            {
                int tempSchedule = deploymentExecuteStatus.TimeLine.GetBuildSchedule();
                if(tempSchedule < 0)
                {
                    scheduleImageKeyResult.BuildScheduleImageKey = ResourceFileData.FsImgeKeyResource.LoadProgressImageKeys[102];
                }
                else if (tempSchedule < 100)
                {
                    scheduleImageKeyResult.BuildScheduleImageKey = ResourceFileData.FsImgeKeyResource.LoadProgressImageKeys[tempSchedule];
                }
                else if(tempSchedule < 115)
                {
                    scheduleImageKeyResult.BuildScheduleImageKey = ResourceFileData.FsImgeKeyResource.LoadProgressImageKeys[99];
                }
                else
                {
                    scheduleImageKeyResult.BuildScheduleImageKey = ResourceFileData.FsImgeKeyResource.LoadProgressImageKeys[101];
                }
            }
            else if (deploymentExecuteStatus.Status == ExecuteStatus.BuildError || 
                deploymentExecuteStatus.Status == ExecuteStatus.BuildFailed)
            {
                scheduleImageKeyResult.BuildScheduleImageKey = ResourceFileData.FsImgeKeyResource.LoadProgressImageKeys[104];
            }
            else if (deploymentExecuteStatus.Status == ExecuteStatus.BuildSuccess)
            {
                scheduleImageKeyResult.BuildScheduleImageKey = ResourceFileData.FsImgeKeyResource.LoadProgressImageKeys[100];
            }
            else if(deploymentExecuteStatus.Status == ExecuteStatus.BuildTimeOut)
            {
                scheduleImageKeyResult.BuildScheduleImageKey = ResourceFileData.FsImgeKeyResource.LoadProgressImageKeys[103];
            }
            else if(deploymentExecuteStatus.Status == ExecuteStatus.LanuchSkip||
                deploymentExecuteStatus.Status == ExecuteStatus.LaunchQueued)
            {
                scheduleImageKeyResult.BuildScheduleImageKey = ResourceFileData.FsImgeKeyResource.LoadProgressImageKeys[100];
            }
            else if (deploymentExecuteStatus.Status == ExecuteStatus.Launching||
                deploymentExecuteStatus.Status == ExecuteStatus.LaunchStop||
                deploymentExecuteStatus.Status == ExecuteStatus.LaunchCancle)
            {
                scheduleImageKeyResult.BuildScheduleImageKey = ResourceFileData.FsImgeKeyResource.LoadProgressImageKeys[100];
                int tempSchedule = deploymentExecuteStatus.TimeLine.GetLaunchSchedule();
                if (tempSchedule < 0)
                {
                    scheduleImageKeyResult.LaunchScheduleImageKey = ResourceFileData.FsImgeKeyResource.LoadProgressImageKeys[102];
                }
                else if (tempSchedule < 100)
                {
                    scheduleImageKeyResult.LaunchScheduleImageKey = ResourceFileData.FsImgeKeyResource.LoadProgressImageKeys[tempSchedule];
                }
                else if (tempSchedule < 115)
                {
                    scheduleImageKeyResult.LaunchScheduleImageKey = ResourceFileData.FsImgeKeyResource.LoadProgressImageKeys[99];
                }
                else
                {
                    scheduleImageKeyResult.LaunchScheduleImageKey = ResourceFileData.FsImgeKeyResource.LoadProgressImageKeys[101];
                }
            }
            else if (deploymentExecuteStatus.Status == ExecuteStatus.LaunchError ||
               deploymentExecuteStatus.Status == ExecuteStatus.LaunchFailed)
            {
                scheduleImageKeyResult.BuildScheduleImageKey = ResourceFileData.FsImgeKeyResource.LoadProgressImageKeys[100];
                scheduleImageKeyResult.LaunchScheduleImageKey = ResourceFileData.FsImgeKeyResource.LoadProgressImageKeys[104];
            }
            else if (deploymentExecuteStatus.Status == ExecuteStatus.LaunchSuccess)
            {
                scheduleImageKeyResult.BuildScheduleImageKey = ResourceFileData.FsImgeKeyResource.LoadProgressImageKeys[100];
                scheduleImageKeyResult.LaunchScheduleImageKey = ResourceFileData.FsImgeKeyResource.LoadProgressImageKeys[100];
            }
            else if (deploymentExecuteStatus.Status == ExecuteStatus.LaunchTimeOut)
            {
                scheduleImageKeyResult.BuildScheduleImageKey = ResourceFileData.FsImgeKeyResource.LoadProgressImageKeys[100];
                scheduleImageKeyResult.LaunchScheduleImageKey = ResourceFileData.FsImgeKeyResource.LoadProgressImageKeys[103];
            }
            return scheduleImageKeyResult;
        }

        /// <summary>
        /// 通过BuildCommit生成Feishu可用的显示内容（带缓存，限制10行，超过添加详情链接）
        /// </summary>
        /// <param name="buildCommit"></param>
        /// <returns></returns>
        public static async ValueTask<string> GetFsCommitContentByBuildCommitAsync(BuildCommit buildCommit)
        {
            if (!(buildCommit.CommitList?.Count > 0))
            {
                buildCommit.ShowCommitContentCache.ContentString = "";
            }
            else if (buildCommit.ShowCommitContentCache.LastCommitCount != buildCommit.CommitList.Count)
            {
                buildCommit.ShowCommitContentCache.LastCommitCount = buildCommit.CommitList.Count;
                string tempCommitStr = buildCommit.GetCommitStr(true);
                Tuple<string, string> tempContentTuple = MyExecuteMan.AnalysisLongMessage(tempCommitStr,10,null,true);
                if(string.IsNullOrEmpty(tempContentTuple.Item2))
                {
                    buildCommit.ShowCommitContentCache.ContentString = tempContentTuple.Item1;
                }
                else
                {
                    buildCommit.ShowCommitContentCache.ContentString = $"{tempContentTuple.Item1}\n ... ...\n🔗 [查看更多]({tempContentTuple.Item2})";
                }
                List<string> nowUsers = buildCommit.GetCommitUsers();
                if (nowUsers?.Count > 0)
                {
                    StringBuilder sbCommitContent = new StringBuilder(buildCommit.ShowCommitContentCache.ContentString);
                    sbCommitContent.AppendLine();
                    foreach (string at in await MessageRobot.FeiShuHelper.FeiShuHandle.GetUserOpenIdListAsync(nowUsers))
                    {
                        if (string.IsNullOrEmpty(at))
                        {
                            continue;
                        }
                        if (at.StartsWith('@'))
                        {
                            //发送卡片消息如果id没有飞书会直接报错，不带id 即可
                            //sbCommitContent.Append($"<at id=>{at.TrimStart('@')}</at>");
                            sbCommitContent.Append($"<at >{at}</at>");
                        }
                        else
                        {
                            sbCommitContent.Append($"<at id={at}></at>");
                        }
                    };
                    buildCommit.ShowCommitContentCache.ContentString = sbCommitContent.ToString();
                }
            }
            else
            {
                //使用历史缓存
                return buildCommit.ShowCommitContentCache.ContentString;
            }
            return buildCommit.ShowCommitContentCache.ContentString;
        }

        /// <summary>
        /// 获取可读的CommitContent
        /// </summary>
        /// <param name="deploymentExecuteStatus"></param>
        /// <returns></returns>
        private static async ValueTask<string> GetFsCommitContentAsync(DeploymentExecuteStatus deploymentExecuteStatus)
        {
            if (deploymentExecuteStatus is null)
            {
                throw new ArgumentNullException(nameof(deploymentExecuteStatus));
            }
            string result = "正在获取......";
            if(deploymentExecuteStatus.BuildCommitInfo!=null)
            {
                string tempCommit = await GetFsCommitContentByBuildCommitAsync(deploymentExecuteStatus.BuildCommitInfo);
                tempCommit = FormartFeishuMeaasgeContent(tempCommit);
                //tempCommit = tempCommit.Replace(@"\", @"\\").Replace(@"""", @"\""");
                if (string.IsNullOrEmpty(tempCommit))
                {
                    if(deploymentExecuteStatus.Status == ExecuteStatus.BuildTimeOut ||
                       deploymentExecuteStatus.Status == ExecuteStatus.BuildStop ||
                       deploymentExecuteStatus.Status == ExecuteStatus.BuildSuccess ||
                       deploymentExecuteStatus.Status == ExecuteStatus.BuildFailed ||
                       deploymentExecuteStatus.Status == ExecuteStatus.BuildError ||
                       deploymentExecuteStatus.Status == ExecuteStatus.BuildCancle ||
                       deploymentExecuteStatus.Status == ExecuteStatus.LaunchQueued||
                       deploymentExecuteStatus.Status == ExecuteStatus.LanuchSkip ||
                       deploymentExecuteStatus.Status == ExecuteStatus.Launching ||
                       deploymentExecuteStatus.Status == ExecuteStatus.LaunchSuccess ||
                       deploymentExecuteStatus.Status == ExecuteStatus.LaunchTimeOut ||
                       deploymentExecuteStatus.Status == ExecuteStatus.LaunchFailed ||
                       deploymentExecuteStatus.Status == ExecuteStatus.LaunchError ||
                       deploymentExecuteStatus.Status == ExecuteStatus.LaunchStop ||
                       deploymentExecuteStatus.Status == ExecuteStatus.LaunchCancle)
                    {
                        result = "**未能获取任何Commit更新**";
                    }
                }
                else
                {
                    result = tempCommit;
                }
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="deploymentExecuteStatus"></param>
        /// <returns></returns>
        private static async ValueTask<List<string>> GetUrgentUsersAsync(DeploymentExecuteStatus deploymentExecuteStatus)
        {
            if(deploymentExecuteStatus.Status== ExecuteStatus.BuildTimeOut||
               deploymentExecuteStatus.Status == ExecuteStatus.BuildError ||
               deploymentExecuteStatus.Status == ExecuteStatus.BuildFailed ||
               deploymentExecuteStatus.Status == ExecuteStatus.LaunchTimeOut ||
               deploymentExecuteStatus.Status == ExecuteStatus.LaunchError ||
               deploymentExecuteStatus.Status == ExecuteStatus.LaunchFailed)
            {
                List<string> tempUsers =  deploymentExecuteStatus.BuildCommitInfo?.GetCommitUsers();
                List<string> resultUsers = await FeiShuHandle.GetUserOpenIdListAsync(tempUsers, false);
                if(resultUsers?.Count>0)
                {
                    return resultUsers;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取卡片Content
        /// </summary>
        /// <param name="deploymentExecuteStatus"></param>
        /// <returns></returns>
        private static async ValueTask<string> CreatInterativeContent(DeploymentExecuteStatus deploymentExecuteStatus)
        {
            if (deploymentExecuteStatus is null)
            {
                throw new ArgumentNullException(nameof(deploymentExecuteStatus));
            }
            string nowFormatConten;

            switch (deploymentExecuteStatus.Status)
            {
                case ExecuteStatus.Triggered:
                case ExecuteStatus.BuildQueued:
                    nowFormatConten = buildQueuedBaseContent;
                    return string.Format(nowFormatConten,
                        deploymentExecuteStatus.Pipeline, 
                        deploymentExecuteStatus.RunId,
                        DeploymentTimeline.GetTimeStr(deploymentExecuteStatus.TimeLine.StartDeploymentTime),
                        deploymentExecuteStatus.TimeLine.GetPredictFinishTimeStr(),
                        deploymentExecuteStatus.Devop,
                        deploymentExecuteStatus.Remark,
                        deploymentExecuteStatus.Status.ToString(),
                        deploymentExecuteStatus.Operator);
                case ExecuteStatus.BuildRunning:
                case ExecuteStatus.BuildSuccess:
                case ExecuteStatus.BuildCancle:
                case ExecuteStatus.BuildStop:
                    nowFormatConten = (deploymentExecuteStatus.Status == ExecuteStatus.BuildRunning|| deploymentExecuteStatus.Status == ExecuteStatus.BuildSuccess) ? buildRunningBaseContent:buildCancelBaseContent;
                    ScheduleImageKeyResult nowScheduleImageKeyResult = GetScheduleImageKey(deploymentExecuteStatus);
                    return string.Format(nowFormatConten,
                        deploymentExecuteStatus.Pipeline,
                        deploymentExecuteStatus.RunId,
                        DeploymentTimeline.GetTimeStr(deploymentExecuteStatus.TimeLine.StartDeploymentTime),
                        deploymentExecuteStatus.TimeLine.GetPredictFinishTimeStr(),
                        deploymentExecuteStatus.Devop,
                        deploymentExecuteStatus.Remark,
                        deploymentExecuteStatus.Status.ToString(),
                        deploymentExecuteStatus.Operator,
                        deploymentExecuteStatus.BuildDetailUri,
                        nowScheduleImageKeyResult.BuildScheduleImageKey,
                        nowScheduleImageKeyResult.LaunchScheduleImageKey,
                        await GetFsCommitContentAsync(deploymentExecuteStatus));
                case ExecuteStatus.BuildTimeOut:
                case ExecuteStatus.BuildError:
                case ExecuteStatus.BuildFailed:
                    nowFormatConten = deploymentExecuteStatus.Status == ExecuteStatus.BuildTimeOut ? buildTimeoutBaseContent : buildFailBaseContent;
                    nowScheduleImageKeyResult = GetScheduleImageKey(deploymentExecuteStatus);
                    Tuple<string, string> errorLogTuple = MyExecuteMan.AnalysisLongMessage(deploymentExecuteStatus.BuildLog, 500, "error");
                    string showErrorLog = errorLogTuple.Item1 ?? "can not get any log";
                    showErrorLog = FormartFeishuMeaasgeContent(showErrorLog);
                    string errorLogUrl = errorLogTuple.Item2 ?? "https://cn.bing.com/search?q=no+any+logs";
                    return string.Format(nowFormatConten,
                        deploymentExecuteStatus.Pipeline,
                        deploymentExecuteStatus.RunId,
                        DeploymentTimeline.GetTimeStr(deploymentExecuteStatus.TimeLine.StartDeploymentTime),
                        deploymentExecuteStatus.TimeLine.GetActualFinishTimeStr(),
                        deploymentExecuteStatus.Devop,
                        deploymentExecuteStatus.Remark,
                        deploymentExecuteStatus.Status.ToString(),
                        deploymentExecuteStatus.Operator,
                        deploymentExecuteStatus.BuildDetailUri,
                        nowScheduleImageKeyResult.BuildScheduleImageKey,
                        nowScheduleImageKeyResult.LaunchScheduleImageKey,
                        await GetFsCommitContentAsync(deploymentExecuteStatus),
                        showErrorLog,
                        errorLogUrl);
                case ExecuteStatus.Launching:
                case ExecuteStatus.LaunchQueued:
                case ExecuteStatus.LaunchCancle:
                case ExecuteStatus.LaunchStop:
                    nowFormatConten = (deploymentExecuteStatus.Status == ExecuteStatus.Launching || deploymentExecuteStatus.Status == ExecuteStatus.LaunchQueued) ? launchRunningBaseContent : launchCancelBaseContent;
                    nowScheduleImageKeyResult =  GetScheduleImageKey(deploymentExecuteStatus);
                    return string.Format(nowFormatConten,
                        deploymentExecuteStatus.Pipeline,
                        deploymentExecuteStatus.RunId,
                        DeploymentTimeline.GetTimeStr(deploymentExecuteStatus.TimeLine.StartDeploymentTime),
                        deploymentExecuteStatus.TimeLine.GetPredictFinishTimeStr(),
                        deploymentExecuteStatus.Devop,
                        deploymentExecuteStatus.Remark,
                        deploymentExecuteStatus.Status.ToString(),
                        deploymentExecuteStatus.Operator,
                        deploymentExecuteStatus.BuildDetailUri,
                        nowScheduleImageKeyResult.BuildScheduleImageKey,
                        nowScheduleImageKeyResult.LaunchScheduleImageKey,
                        await GetFsCommitContentAsync(deploymentExecuteStatus),
                        deploymentExecuteStatus.LaunchDetailUri);
                case ExecuteStatus.LaunchTimeOut:
                case ExecuteStatus.LaunchError:
                case ExecuteStatus.LaunchFailed:
                    nowFormatConten = deploymentExecuteStatus.Status == ExecuteStatus.LaunchTimeOut? launchTimeoutBaseContent : launchFailBaseContent;
                    nowScheduleImageKeyResult = GetScheduleImageKey(deploymentExecuteStatus);
                    errorLogTuple = MyExecuteMan.AnalysisLongMessage(deploymentExecuteStatus.LaunchLog,500,"error");
                    showErrorLog = errorLogTuple.Item1 ?? "can not get any log";
                    showErrorLog = FormartFeishuMeaasgeContent(showErrorLog);
                    errorLogUrl = errorLogTuple.Item2 ?? "https://cn.bing.com/search?q=no+any+logs";
                    return string.Format(nowFormatConten,
                        deploymentExecuteStatus.Pipeline,
                        deploymentExecuteStatus.RunId,
                        DeploymentTimeline.GetTimeStr(deploymentExecuteStatus.TimeLine.StartDeploymentTime),
                        deploymentExecuteStatus.TimeLine.GetActualFinishTimeStr(),
                        deploymentExecuteStatus.Devop,
                        deploymentExecuteStatus.Remark,
                        deploymentExecuteStatus.Status.ToString(),
                        deploymentExecuteStatus.Operator,
                        deploymentExecuteStatus.BuildDetailUri,
                        nowScheduleImageKeyResult.BuildScheduleImageKey,
                        nowScheduleImageKeyResult.LaunchScheduleImageKey,
                        await GetFsCommitContentAsync(deploymentExecuteStatus),
                        deploymentExecuteStatus.LaunchDetailUri,
                        showErrorLog,
                        errorLogUrl);
                case ExecuteStatus.LaunchSuccess:
                    nowFormatConten = deploymentSuccessBaseContent;
                    nowScheduleImageKeyResult = GetScheduleImageKey(deploymentExecuteStatus);
                    return string.Format(nowFormatConten,
                        deploymentExecuteStatus.Pipeline,
                        deploymentExecuteStatus.RunId,
                        DeploymentTimeline.GetTimeStr(deploymentExecuteStatus.TimeLine.StartDeploymentTime),
                        deploymentExecuteStatus.TimeLine.GetActualFinishTimeStr(),
                        deploymentExecuteStatus.Devop,
                        deploymentExecuteStatus.Remark,
                        deploymentExecuteStatus.Status.ToString(),
                        deploymentExecuteStatus.Operator,
                        deploymentExecuteStatus.BuildDetailUri,
                        nowScheduleImageKeyResult.BuildScheduleImageKey,
                        nowScheduleImageKeyResult.LaunchScheduleImageKey,
                        await GetFsCommitContentAsync(deploymentExecuteStatus),
                        deploymentExecuteStatus.LaunchDetailUri);
                case ExecuteStatus.LanuchSkip:
                    nowFormatConten = launchSkipBaseContent;
                    nowScheduleImageKeyResult = GetScheduleImageKey(deploymentExecuteStatus);
                    return string.Format(nowFormatConten,
                        deploymentExecuteStatus.Pipeline,
                        deploymentExecuteStatus.RunId,
                        DeploymentTimeline.GetTimeStr(deploymentExecuteStatus.TimeLine.StartDeploymentTime),
                        deploymentExecuteStatus.TimeLine.GetActualFinishTimeStr(),
                        deploymentExecuteStatus.Devop,
                        deploymentExecuteStatus.Remark,
                        deploymentExecuteStatus.Status.ToString(),
                        deploymentExecuteStatus.Operator,
                        deploymentExecuteStatus.BuildDetailUri,
                        nowScheduleImageKeyResult.BuildScheduleImageKey,
                        nowScheduleImageKeyResult.LaunchScheduleImageKey,
                        await GetFsCommitContentAsync(deploymentExecuteStatus));
                default:
                    Console.WriteLine("unknow deploymentExecuteStatus");
                    break;

            }
            return default;
        }

        /// <summary>
        /// 推送、跟新应用卡片（内容通过DeploymentExecuteStatus推断）
        /// </summary>
        /// <param name="deploymentExecuteStatus"></param>
        /// <returns></returns>
        public static async ValueTask PushInterative(DeploymentExecuteStatus deploymentExecuteStatus)
        {
            if(!isPrepared)
            {
                return;
            }
            if (deploymentExecuteStatus is null)
            {
                throw new ArgumentNullException(nameof(deploymentExecuteStatus));
            }
            if(string.IsNullOrEmpty(deploymentExecuteStatus.FsApplicationChatId))
            {
                return;
            }
            FsSendInterativeInfo fsSendInterativeInfo = new FsSendInterativeInfo()
            {
                uuid = deploymentExecuteStatus.ExecuteUid,
                receive_id = deploymentExecuteStatus.FsApplicationChatId
            };
            //获取消息内容
            fsSendInterativeInfo.content =await CreatInterativeContent(deploymentExecuteStatus);
            //确认消息加急
            if (!deploymentExecuteStatus.FsIsHasUrgented)
            {
                List<string> urgentUsers = await GetUrgentUsersAsync(deploymentExecuteStatus);
                if(urgentUsers?.Count>0)
                {
                    fsSendInterativeInfo.urgent_users = urgentUsers.ToArray();
                    deploymentExecuteStatus.FsIsHasUrgented = true;//确保不会多次反复加急
                }
            }
            await FeiShuHandle.SendInteractiveMessage(fsSendInterativeInfo);
        }

    }
}
