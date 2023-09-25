using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeploymentRobotService.DeploymentService
{
    public class    UserFavoriteProjects
    {
        private const int MaxCount = 10;
        public string UserName { get; private set; }
        private Dictionary<string, string> ProjectDc;
        private Dictionary<string, int> ProjectWeight;

        public UserFavoriteProjects(string userNmae)
        {
            UserName = userNmae;
            ProjectDc = new Dictionary<string, string>(MaxCount);
            ProjectWeight = new Dictionary<string, int>(MaxCount);
        }

        //lock
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public void AddProject(string key,string projectName)
        {
            if(!ProjectDc.ContainsKey(key))
            {
                if(ProjectDc.Count< MaxCount)
                {
                    ProjectDc.Add(key, projectName);
                    ProjectWeight.Add(key, 1);
                }
                else
                {
                    string minKey = null;
                    foreach(var tempKv in ProjectWeight)
                    {
                        if(minKey==null)
                        {
                            minKey = tempKv.Key;
                        }
                        else
                        {
                            if(ProjectWeight[minKey]> tempKv.Value)
                            {
                                minKey = tempKv.Key;
                            }
                        }
                    }
                    ProjectDc.Remove(minKey);
                    ProjectWeight.Remove(minKey);
                    ProjectDc.Add(key, projectName);
                    ProjectWeight.Add(key, 1);
                }
            }
            else
            {
                ProjectWeight[key]++;
            }
        }

        public void RefreshUserFavoriteProjects(Dictionary<string, string> kepProjectDc)
        {
            List<KeyValuePair<string,string>> tempAddList = new List<KeyValuePair<string,string>>();
            foreach(var pKv in ProjectDc)
            {
                if(!kepProjectDc.ContainsKey(pKv.Key))
                {
                    //当前发布项目已经被移除（历史记录并不移除）
                    continue;
                }
                if(pKv.Value != kepProjectDc[pKv.Key])
                {
                    foreach(var tempKv in kepProjectDc)
                    {
                        if (tempKv.Value == pKv.Value)
                        {
                            tempAddList.Add(new KeyValuePair<string, string>(tempKv.Key, tempKv.Value));
                            ProjectDc.Remove(pKv.Key);
                            if (ProjectWeight.ContainsKey(pKv.Key))
                            {
                                int oldTimes = ProjectWeight[pKv.Key];
                                ProjectWeight.Remove(pKv.Key);
                                if(ProjectWeight.ContainsKey(tempKv.Key))
                                {
                                    ProjectWeight.Add(System.Guid.NewGuid().ToString("N"), ProjectWeight[tempKv.Key]);
                                    ProjectWeight.Remove(tempKv.Key);
                                }
                                ProjectWeight.Add(tempKv.Key, oldTimes);
                            }
                            break;
                        }
                    }
                }
            }
            if(tempAddList.Count>0)
            {
                foreach(var lKv in tempAddList)
                {
                    if(ProjectDc.ContainsKey(lKv.Key))
                    {
                        MyHelper.MyLogger.LogError("find same key in RefreshUserFavoriteProjects");
                        continue;
                    }
                    ProjectDc.Add(lKv.Key,lKv.Value);
                }
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("Your favorite projects");
            sb.AppendLine();
            if (ProjectDc!=null && ProjectDc.Count>0)
            {
                foreach (var tempProject in ProjectDc)
                {
                    //【{0}】 [{1}]
                    sb.Append('【');
                    sb.Append(tempProject.Key);
                    sb.Append("】 [");
                    sb.Append(tempProject.Value);
                    sb.AppendLine("]");
                }
                if (sb.Length > 24)
                {
                    sb.Remove(sb.Length - Environment.NewLine.Length, Environment.NewLine.Length);
                }
            }
            return sb.ToString();
        }
    }
}
