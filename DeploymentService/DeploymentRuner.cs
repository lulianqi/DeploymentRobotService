using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeploymentRobotService.MyHelper;
using MyDeploymentMonitor;
using MyDeploymentMonitor.ExecuteHelper;

namespace DeploymentRobotService.DeploymentService
{
    public enum DeploymentRunerState
    {
        NotStart,
        Running,
        Failed,
        Cancelled,
        TimeOut,
        Sucessed
    }

    public class DeploymentRuner
    {
        private const string pushBuildUriFormatStr = "🚀已经开始发布，请关注群消息中的发布动态\n<a href=\"{0}\">发布日志</a>    <a href=\"{1}\">取消</a>";
        private const string findAliveRunnerFormatStr = "🕓[{0}]还有未完成的发布\n{1}🔹如果您仍想强制发布请使用-f参数重新执行";
        private const string runerToStringFormatStr = "🚩项目：{0}\n状态：{3} [ {1}>{2} ]\n发布人：{4} <a href=\"{5}\">发布日志</a>";
        private const string buildSucessFormatStr = "💚{0}发布完成\n<a href=\"{1}\">发布日志</a>";
        private const string buildFailFormatStr = "💔{0}发布失败\n<a href=\"{1}\">发布日志</a>";
        private const string buildCancelFormatStr = "⛔{0}发布被取消\n<a href=\"{1}\">发布日志</a>";
        private const string buildUnKnowFormatStr = "🚫{0}发布状态未知\n<a href=\"{1}\">发布日志</a>";
        private const string buildTimeOutFormatStr = "⏰{0}发布超时\n<a href=\"{1}\">发布日志</a>";

        private Dictionary<DeploymentRunerState, string> RunerStateFormatStrDc = new Dictionary<DeploymentRunerState, string>() {
            {DeploymentRunerState.NotStart,buildUnKnowFormatStr },
            {DeploymentRunerState.Running,buildUnKnowFormatStr },
            {DeploymentRunerState.Failed,buildFailFormatStr },
            {DeploymentRunerState.Cancelled,buildCancelFormatStr },
            {DeploymentRunerState.TimeOut,buildTimeOutFormatStr },
            {DeploymentRunerState.Sucessed,buildSucessFormatStr }
        };


        public DateTime DeploymentTime { get;private set; }
        public TimeSpan? DeploymentElapsedTime { get; private set; }
        public DeploymentRunerState RunerState { get; private set; } = DeploymentRunerState.NotStart;
        private string _deploymentUser;
        public string DeploymentUser { get { return _deploymentUser; } set { DisplayDeploymentUserName = ApplicationRobot.FsRobotBusinessData.GetUserNameById(value); _deploymentUser = value; } }
        public string DeploymentKey { get; set; }
        public string DeploymentProjectName { get; set; }
        public string DeploymentProjectResource { get; set; }
        public string DisplayDeploymentUserName { get;private set; }
    
        public DeploymentRuner()
        {
            DeploymentTime = DateTime.Now;
            RunerState = DeploymentRunerState.NotStart;
        }

        public async Task<bool> BuildAsync(IRobotConnector nowRobot=null , bool forceBuild = true ,string env =null)
        {
            if(!forceBuild)
            {
                var runningList = ApplicationRobot.NowDeploymentQueue.GetAliveRuner(DeploymentProjectName);
                if(runningList!=null & runningList.Count>0)
                {
                    StringBuilder tempProjects = new StringBuilder();
                    foreach (var project in runningList)
                    {
                        tempProjects.AppendLine(project.ToString());
                    }
                    _ = nowRobot?.PushContent(DeploymentUser, string.Format(findAliveRunnerFormatStr, DeploymentProjectName, tempProjects.ToString()));
                    return false;
                }
            }
            Dictionary<string, string> configDc = null;
            if(env!=null)
            {
                configDc = new Dictionary<string, string>();
                configDc.Add("ENV", env);
                configDc.Add("API_ENV", env);
            }
            RunerState = DeploymentRunerState.Running;
            ApplicationRobot.NowDeploymentQueue.AddRunner(this);
            try
            {
                DeploymentResult buildResult = await MyBuilder.BuildByKey(DeploymentKey, DisplayDeploymentUserName, configDc, new Action<string, string>((content, buildId) =>
                {
                    DeploymentProjectResource = content;
                    //_ = MyDeployment.PushContent(DeploymentUser, content.StartsWith("http") ? string.Format(pushBuildUriFormatStr, content) : content);
                    if (content.StartsWith("http"))
                    {
                        string cancelUrl = $"{Appsetting.RobotConfig.CancleLink}?key={DeploymentKey}&id={buildId}&appChannel={nowRobot.AppChannel}";
                        _ = nowRobot?.PushContent(DeploymentUser, string.Format(pushBuildUriFormatStr, content, cancelUrl));
                    }
                    else
                    {
                        _ = nowRobot?.PushContent(DeploymentUser, content);
                    }
                }));
                RunerState = buildResult == DeploymentResult.Cancel ? DeploymentRunerState.Cancelled : buildResult == DeploymentResult.Timeout ? DeploymentRunerState.TimeOut : buildResult == DeploymentResult.Succeed ? DeploymentRunerState.Sucessed : DeploymentRunerState.Failed;
            }
            catch(Exception ex)
            {
                MyLogger.LogError("【BuildAsync】Exception", ex);
                RunerState = DeploymentRunerState.Failed;
            }
            if (string.IsNullOrEmpty(DeploymentProjectResource)) DeploymentProjectResource = "无法获取状态";

            await nowRobot?.PushContent(DeploymentUser, string.Format(RunerStateFormatStrDc[RunerState] , DeploymentProjectName, DeploymentProjectResource));
            DeploymentElapsedTime = DateTime.Now - DeploymentTime;
            return RunerState == DeploymentRunerState.Sucessed;
        }

        public async Task CancelBuildAsync()
        {
            await MyBuilder.CancelByKey(DeploymentKey);
        }

        public override string ToString()
        {
            string tempRunerState = RunerState.ToString();
            switch( RunerState)
            {
                case DeploymentRunerState.NotStart:
                    tempRunerState ="💛" + tempRunerState;
                    break;
                case DeploymentRunerState.Running:
                    tempRunerState = "💙" + tempRunerState;
                    break;
                case DeploymentRunerState.Sucessed:
                    tempRunerState = "💚" + tempRunerState;
                    break;
                case DeploymentRunerState.Failed:
                    tempRunerState = "💗" + tempRunerState;
                    break;
                case DeploymentRunerState.TimeOut:
                    tempRunerState = "⏰" + tempRunerState;
                    break;
                case DeploymentRunerState.Cancelled:
                    tempRunerState = "⛔" + tempRunerState;
                    break;
                default:
                    tempRunerState = "❤" + tempRunerState;
                    break;
            }
            return string.Format(runerToStringFormatStr, DeploymentProjectName, DeploymentTime.ToString("MM/dd HH:mm:ss"), DeploymentElapsedTime == null ? "未完成" : DeploymentElapsedTime?.ToString(@"hh\:mm\:ss"), tempRunerState, DisplayDeploymentUserName, DeploymentProjectResource);
        }
    }
}
