using DeploymentRobotService.MyHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeploymentRobotService.DeploymentService
{
    public class DeploymentQueue
    {
        private const int maxDeploymentRunerList = 300;
        public List<DeploymentRuner> DeploymentRunerList = new List<DeploymentRuner>();
        private Dictionary<string, UserFavoriteProjects> UserFavoriteProjectDc = new Dictionary<string, UserFavoriteProjects>();

        private void DealUserFavoriteProject(string userName ,string projectKey , string projectName)
        {
            if(string.IsNullOrEmpty(userName))
            {
                MyLogger.LogError("DealUserFavoriteProject error userName is null");
                return;
            }
            if(UserFavoriteProjectDc.ContainsKey(userName))
            {
                UserFavoriteProjectDc[userName].AddProject(projectKey, projectName);
            }
            else
            {
                UserFavoriteProjects tempUserFavoriteProjects = new UserFavoriteProjects(userName);
                tempUserFavoriteProjects.AddProject(projectKey, projectName);
                UserFavoriteProjectDc.Add(userName, tempUserFavoriteProjects);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public void AddRunner(DeploymentRuner yourRuner)
        {
            int removeCount = DeploymentRunerList.Count - maxDeploymentRunerList;
            if (removeCount>20)
            {
                DeploymentRunerList.RemoveRange(0, 20);
            }
            DeploymentRunerList.Add(yourRuner);
            DealUserFavoriteProject(yourRuner.DeploymentUser, yourRuner.DeploymentKey, yourRuner.DeploymentProjectName);
        }

        public List<DeploymentRuner>  GetRunerList(string user = null,string key =null ,string name =null , DeploymentRunerState? state =null , int maxCount = 20)
        {
            if(maxCount<1)
            {
                maxCount = 9;
            }
            return DeploymentRunerList.Where<DeploymentRuner>((runner) => (user == null || runner.DeploymentUser == user) & (key == null || runner.DeploymentKey == key)  & (name == null || runner.DeploymentProjectName.Contains(name))& (state==null || runner.RunerState==state)).TakeLast(maxCount).Reverse().ToList();
        }

        public List<DeploymentRuner> GetAliveRuner(string name )
        {
            //FindAll like where().ToList() but FindAll only for list where is for IEnumerable
            return DeploymentRunerList.FindAll((runner)=>runner.DeploymentProjectName==name & runner.RunerState == DeploymentRunerState.Running).Reverse<DeploymentRuner>().ToList();
        }

        public string GetUserFavoriteProject(string userName)
        {
            if (UserFavoriteProjectDc.ContainsKey(userName))
            {
                return UserFavoriteProjectDc[userName].ToString();
            }
            return "请先使用CI终端发布任意项目";
        }

        public bool UpdateUserFavoriteProjectKeys()
        {
            string sourceProjectStr = MyDeploymentMonitor.ExecuteHelper.MyBuilder.ShowProjects(true, false, false, true, null);
            if (string.IsNullOrEmpty(sourceProjectStr))
            {
                MyLogger.LogError("UpdateUserFavoriteProjectKeys error that sourceProjectStr is null");
                return false;
            }
            string[] ContentLines = sourceProjectStr.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            Dictionary<string, string> kepProjectDc = new Dictionary<string, string>();
            try
            {
                foreach (string line in ContentLines)
                {
                    if (line.StartsWith('【'))
                    {
                        int tempEnd = line.IndexOf('】');
                        string tempKey;
                        string tempValue;
                        if (tempEnd > 0)
                        {
                            tempKey = line.Substring(1, tempEnd - 1);
                            tempValue = line.Substring(tempEnd + 3, line.Length - tempEnd - 5);
                            if (kepProjectDc.ContainsKey(tempKey)) continue;
                            kepProjectDc.Add(tempKey, tempValue);
                        }
                        else
                        {
                            MyLogger.LogError("UpdateUserFavoriteProjectKeys error that tempEnd < 0");
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                MyLogger.LogError($"UpdateUserFavoriteProjectKeys error that {ex.ToString()}");
                return false;
            }
            if (kepProjectDc.Count > 0)
            {
                foreach (var ufp in UserFavoriteProjectDc)
                {
                    ufp.Value.RefreshUserFavoriteProjects(kepProjectDc);
                }
            }
            return true;
        }

    }
}
