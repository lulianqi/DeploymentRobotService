using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MyDeploymentMonitor.ExecuteHelper;

namespace DeploymentRobotService.Pages.PageHelper
{
    public class MessageProjectPredict
    {
        public class ProjectBuildInfo
        {
            public string ProjectName;
            public string BuildKey;
            public bool IsExactMatch { get; internal set; } = false;
            public bool HasBuild { get; private set; } = false;

            public void SetHasBuild()
            {
                this.HasBuild = true;
            }

            public void Build(string userFlag=null , string e =null , bool isForceBuild =false)
            {
                DeploymentService.OperationHistory.AddOperation(DeploymentService.ApplicationRobot.FsRobotBusinessData.GetUserNameById(userFlag), $"[{ProjectName}] build by feishu quick message", DateTime.Now.ToString("MM/dd HH:mm:ss"), "FsCmd");
                string tempBuildCmd = BuildKey;
                if (!string.IsNullOrEmpty(e))
                {
                    tempBuildCmd = $"{tempBuildCmd} -e {e}";
                }
                if(isForceBuild)
                {
                    tempBuildCmd = $"{tempBuildCmd} -f"; ;
                }
                string fsResponseMessage = DeploymentService.ApplicationRobot.ReplyApplicationCmd(DeploymentService.ApplicationRobot.FsConnector, tempBuildCmd, userFlag);
                _ = DeploymentService.ApplicationRobot.FsConnector.PushContent(userFlag, fsResponseMessage);
                HasBuild = true;
            }
        }

        public List<string> SourceWords { get; set; }
        public Dictionary<string, Dictionary<string, ProjectBuildInfo>> Projects { get; set; }
        public bool HasKeyTest { get; set; } = false;
        public bool HasKeyPre { get; set; } = false;
        public string UserFlag { get; set; }
        public string ErrorMessage { get; set; } = null;

        public string EnvironmentalParameter { get { return HasKeyPre ? "pre" : null; } }

        public MessageProjectPredict(string userFlag=null)
        {
            UserFlag = userFlag;
            SourceWords = new List<string>();
            Projects = new Dictionary<string, Dictionary<string, ProjectBuildInfo>>(); 
        }

        /// <summary>
        /// 通过SourceWords更新Projects内容（调用前请确保SourceWord已经填充）
        /// 搜索结果依次取KubeSphereV3DevopScanProjects，KubeSphereDevopScanProjects，KubeSphereProjects并且仅取一组数据
        /// </summary>
        public void UpdataProjects()
        {
            Func<Dictionary<string, string>,string, Dictionary< string, ProjectBuildInfo >, Dictionary<string, ProjectBuildInfo>> GetProjectBuildInfoDc = new Func<Dictionary<string, string>, string, Dictionary<string, ProjectBuildInfo>, Dictionary<string, ProjectBuildInfo>>((sourceDc  ,searchWord , existedProjectDc) =>
            {
                Dictionary<string, ProjectBuildInfo> pgDc = existedProjectDc ?? new Dictionary<string, ProjectBuildInfo>();
                if(sourceDc!=null)
                {
                    foreach(var onePj in sourceDc)
                    {
                        pgDc.TryAdd(onePj.Key, new ProjectBuildInfo()
                        {
                             ProjectName = onePj.Key,
                             BuildKey= onePj.Value,
                             IsExactMatch= onePj.Key.Split(' ')[0] == searchWord
                        });
                    }
                }
                return pgDc;
            });

            Projects.Clear();
            if(SourceWords?.Count>0)
            {
                foreach (string tempWord in SourceWords)
                {
                    if(Projects.ContainsKey(tempWord))
                    {
                        continue;
                    }
                    MyBuilder.ProjectFindResult projectFindResult;
                    MyBuilder.ShowProjects(out projectFindResult, true, false, false, true, tempWord);
                    if(projectFindResult.HasAnyResult)
                    {
                        Dictionary<string, ProjectBuildInfo> tempProjectDc=null;
                        if (projectFindResult.KubeSphereV3DevopScanProjects?.Count > 0)
                        {
                            tempProjectDc = GetProjectBuildInfoDc(projectFindResult.KubeSphereV3DevopScanProjects, tempWord , tempProjectDc);
                        }
                        if (projectFindResult.KubeSphereDevopScanProjects?.Count > 0)
                        {
                            tempProjectDc = GetProjectBuildInfoDc(projectFindResult.KubeSphereDevopScanProjects, tempWord, tempProjectDc);
                        }
                        if (projectFindResult.KubeSphereProjects?.Count > 0)
                        {
                            tempProjectDc = GetProjectBuildInfoDc(projectFindResult.KubeSphereProjects, tempWord, tempProjectDc);
                        }

                        if(tempProjectDc?.Count>0)
                        {
                            Projects.Add(tempWord, tempProjectDc);
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 尝试构建所有Projects，如果每个Project只有一个匹配项目，或有精确匹配项目则直接触发构建(如果多个关键字预测最佳项目是同一个项目则只触发一次)，否则跳过
        /// </summary>
        /// <returns>是否全部都直接触发了构建</returns>
        public bool TryBuildAll()
        {
            bool isAllBuild = true;
            if(Projects?.Count>0)
            {
                List<string> buildProjects = new List<string>();
                foreach (var pkv in Projects)
                {
                    KeyValuePair<string, ProjectBuildInfo> tempExactMatch = default;
                    if (pkv.Value.Count==1)
                    {
                        tempExactMatch = pkv.Value.First();
                    }
                    else
                    {
                        tempExactMatch = pkv.Value.FirstOrDefault((value) => value.Value.IsExactMatch);
                    }

                    if (tempExactMatch.Key != null)
                    {
                        if(buildProjects.Contains(tempExactMatch.Key))
                        {
                            tempExactMatch.Value.SetHasBuild();
                            continue;
                        }
                        buildProjects.Add(tempExactMatch.Key);
                        tempExactMatch.Value.Build(UserFlag, EnvironmentalParameter);
                    }
                    else
                    {
                        isAllBuild = false;
                    }
                }
            }
            else
            {
                return false;
            }
            return isAllBuild;
        }

        /// <summary>
        /// 是否获取任何可发布工程
        /// </summary>
        /// <returns></returns>
        public bool HasAnyProject()
        {
            return ! (Projects.Count == 0);
        }

        /// <summary>
        /// 检测所有预测工程是否都已经完成了发布
        /// </summary>
        /// <returns></returns>
        public bool HasBuildAllCheck()
        {
            if(!HasAnyProject())
            {
                return false;
            }
            foreach (var pj in Projects)
            {
                bool tempBuilded = false;
                foreach (var p in pj.Value)
                {
                    if (p.Value.HasBuild)
                    {
                        tempBuilded = true;
                        break;
                    }
                }
                if (tempBuilded == false)
                {
                    return false;
                }
            }
            return true;
        }


        /// <summary>
        /// 获取对已经完成构建的文本回复
        /// </summary>
        /// <returns></returns>
        public string GetBuildFeedback()
        {
            if(Projects.Count==0)
            {
                return "未获取任何待发布工程";
            }
            StringBuilder sb = new StringBuilder();
            List<string> buildProjects = new List<string>();
            foreach (var pj in Projects)
            {
                foreach(var p in pj.Value)
                {
                    if(p.Value.HasBuild)
                    {
                        if(buildProjects.Contains(p.Value.ProjectName))
                        {
                            continue;
                        }
                        buildProjects.Add(p.Value.ProjectName);
                        sb.AppendLine($"●「{p.Value.ProjectName}」");
                    }
                }
            }
            if(sb.Length>0)
            {
                sb.Append($"已触发{(HasKeyPre ? "预发" : "")}构建，请关注飞书群中动态卡片信息");
                return sb.ToString();
            }
            else
            {
                return "暂未发布任何工程";
            }
        }

        /// <summary>
        /// 通过messagea内容创建一个MessageProjectPredict，并自动根据message内容填充SourceWords（静态方法）
        /// </summary>
        /// <param name="messagea"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static MessageProjectPredict GetMessageProjectPredict(string messagea)
        {
            if (messagea is null)
            {
                throw new ArgumentNullException(nameof(messagea));
            }
            Regex myRegex = new Regex("[a-zA-Z0-9-_]+");
            MatchCollection mMactchCol = myRegex.Matches(messagea);
            MessageProjectPredict messageProjectPredict = new MessageProjectPredict();
            foreach (Match mMatch in mMactchCol)
            {
                if(mMatch.Value=="test" || mMatch.Value == "test-k8s")
                {
                    messageProjectPredict.HasKeyTest = true;
                    continue;
                }
                else if (mMatch.Value == "pre" || mMatch.Value == "pre-k8s")
                {
                    messageProjectPredict.HasKeyPre = true;
                    continue;
                }
                else if(mMatch.Value.Length<3)
                {
                    continue;
                }
                messageProjectPredict.SourceWords.Add(mMatch.Value);
            }

            if(!messageProjectPredict.HasKeyTest && messagea.Contains("测试"))
            {
                messageProjectPredict.HasKeyTest = true;
            }
            if (!messageProjectPredict.HasKeyPre && messagea.Contains("预发"))
            {
                messageProjectPredict.HasKeyPre = true;
            }
            return messageProjectPredict;
        }
    }
}
