using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyDeploymentMonitor.ShareData;

namespace MyDeploymentMonitor.ExecuteHelper
{
    /// <summary>
    /// MyBuilder中开放操作能力用于暴露给其他引用系统直接调用（如DeploymentRobotService）
    /// </summary>
    public class MyBuilder
    {
        public class ProjectFindResult
        {
            private Dictionary<string, string> _kubeSphereProjects;
            private Dictionary<string, string> _kubeSphereDevopScanProjects;
            private Dictionary<string, string> _kubeSphereV3DevopScanProjects;
            private Dictionary<string, string> _bambooProjects;
            private Dictionary<string, string> _privateBambooProjects;
            private Dictionary<string, string> _rancherProjects;

            public Dictionary<string, string> KubeSphereProjects { get { return _kubeSphereProjects; } private set { _kubeSphereProjects = value; } }
            public Dictionary<string, string> KubeSphereDevopScanProjects { get { return _kubeSphereDevopScanProjects; } private set { _kubeSphereDevopScanProjects = value; } }
            public Dictionary<string, string> KubeSphereV3DevopScanProjects { get { return _kubeSphereV3DevopScanProjects; } private set { _kubeSphereV3DevopScanProjects = value; } }
            public Dictionary<string, string> BambooProjects { get { return _bambooProjects; } private set { _bambooProjects = value; } }
            public Dictionary<string, string> PrivateBambooProjects { get { return _privateBambooProjects; } private set { _privateBambooProjects = value; } }
            public Dictionary<string, string> RancherProjects { get { return _rancherProjects; } private set { _rancherProjects = value; } }

            public bool HasAnyResult
            {
                get {
                    return KubeSphereProjects?.Count > 0 ||
                        KubeSphereDevopScanProjects?.Count > 0 ||
                        KubeSphereV3DevopScanProjects?.Count > 0 ||
                        BambooProjects?.Count > 0 ||
                        PrivateBambooProjects?.Count > 0 ||
                        RancherProjects?.Count > 0;
                }
            }

            private bool AddProject(ref Dictionary<string, string> projectDc, string key, string value)
            {
                if (projectDc == null)
                {
                    projectDc = new Dictionary<string, string>();
                }
                return projectDc.TryAdd(key, value);
            }

            public bool AddKubeSphereProject(string projectNmae, string buildKey)
            {
                return AddProject(ref _kubeSphereProjects, projectNmae, buildKey);
            }
            public bool AddKubeSphereDevopScanProject(string projectNmae, string buildKey)
            {
                return AddProject(ref _kubeSphereDevopScanProjects, projectNmae, buildKey);
            }
            public bool AddKubeSphereV3DevopScanProject(string projectNmae, string buildKey)
            {
                return AddProject(ref _kubeSphereV3DevopScanProjects, projectNmae, buildKey);
            }
            public bool AddBambooProject(string projectNmae, string buildKey)
            {
                return AddProject(ref _bambooProjects, projectNmae, buildKey);
            }
            public bool AddPrivateBambooProject(string projectNmae, string buildKey)
            {
                return AddProject(ref _privateBambooProjects, projectNmae, buildKey);
            }
            public bool AddRancherProject(string projectNmae, string buildKey)
            {
                return AddProject(ref _rancherProjects, projectNmae, buildKey);
            }
        }

        public MyBuilder()
        {
        }

        public static string CkeckBuildKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }
            if (MyConfiguration.UserConf.KubeSphereProjects != null && MyConfiguration.UserConf.KubeSphereProjects.ContainsKey(key))
            {
                return (MyConfiguration.UserConf.KubeSphereProjects[key].ProjectName);
            }
            if (MyConfiguration.UserConf.KubeSphereDevopScanProjects != null && MyConfiguration.UserConf.KubeSphereDevopScanProjects.ContainsKey(key))
            {
                return (MyConfiguration.UserConf.KubeSphereDevopScanProjects[key].ProjectName);
            }
            if (MyConfiguration.UserConf.KubeSphereV3DevopScanProjects != null && MyConfiguration.UserConf.KubeSphereV3DevopScanProjects.ContainsKey(key))
            {
                return (MyConfiguration.UserConf.KubeSphereV3DevopScanProjects[key].ProjectName);
            }
            else if (MyConfiguration.UserConf.BambooProjects != null && MyConfiguration.UserConf.BambooProjects.ContainsKey(key))
            {
                return (MyConfiguration.UserConf.BambooProjects[key].ProjectName);
            }
            else if (MyConfiguration.UserConf.PrivateBambooProjects != null && MyConfiguration.UserConf.PrivateBambooProjects.ContainsKey(key))
            {
                return (MyConfiguration.UserConf.PrivateBambooProjects[key].ProjectName);
            }
            else if (MyConfiguration.UserConf.RancherProjects != null && MyConfiguration.UserConf.RancherProjects.ContainsKey(key))
            {
                return (MyConfiguration.UserConf.RancherProjects[key].ProjectName);
            }
            else
            {
                return null;
            }
        }

        public static string GetShortCode(string projectName)
        {
            if (!string.IsNullOrEmpty(projectName) && MyConfiguration.UserConf.KubeSphereProjects != null)
            {
                //return (MyConfiguration.UserConf.KubeSphereProjects.FirstOrDefault((kv) => kv.Value.ProjectName == projectName)).Key;
                foreach (var projectItem in MyConfiguration.UserConf.KubeSphereProjects)
                {
                    if (projectItem.Value.ProjectName == projectName)
                    {
                        return projectItem.Key;
                    }
                }
            }
            return null;
        }

        public static string ShowProjects(bool isShowKubeSphereProjects = true, bool isShowBambooProjects = true, bool isShowRancherProjects = true, bool isShowScanProjects = true, string projectName = null)
        {
            return ShowProjects(out _, isShowKubeSphereProjects, isShowBambooProjects, isShowRancherProjects, isShowScanProjects, projectName);
        }

        public static string ShowProjects(out ProjectFindResult projectFindResult, bool isShowKubeSphereProjects = true, bool isShowBambooProjects = true, bool isShowRancherProjects = true, bool isShowScanProjects = true, string projectName = null)
        {
            bool isHasAnyProject = false;
            StringBuilder projectsSb = new StringBuilder();
            projectFindResult = new ProjectFindResult();
            if (MyConfiguration.UserConf.KubeSphereProjects != null && MyConfiguration.UserConf.KubeSphereProjects.Count > 0 && isShowKubeSphereProjects)
            {
                isHasAnyProject = true;
                projectsSb.AppendLine("KubeSphereProjects");
                foreach (var tempNode in MyConfiguration.UserConf.KubeSphereProjects)
                {
                    //string.Format("【{0,-4}】 [{1}] "
                    if (string.IsNullOrEmpty(projectName) || tempNode.Value.ProjectName.MyContains(projectName))
                    {
                        string tempKey = tempNode.Key;
                        string tempName = string.IsNullOrEmpty(tempNode.Value.ProjectName) ? tempNode.Value.PipelineId : tempNode.Value.ProjectName;
                        projectsSb.AppendLine(string.Format("【{0}】 [{1}] ", tempKey, tempName));
                        projectFindResult.AddKubeSphereProject(tempName, tempKey);
                    }
                    //projectsSb.AppendLine(string.Format("【{0}】 [{1}] ", tempNode.Key, string.IsNullOrEmpty(tempNode.Value.ProjectName) ? tempNode.Value.PipelineId : tempNode.Value.ProjectName));

                }
            }
            if (MyConfiguration.UserConf.KubeSphereDevopScanProjects != null && MyConfiguration.UserConf.KubeSphereDevopScanProjects.Count > 0 && isShowKubeSphereProjects && isShowScanProjects)
            {
                isHasAnyProject = true;
                projectsSb.AppendLine("KubeSphereDevopScanProjects");
                foreach (var tempNode in MyConfiguration.UserConf.KubeSphereDevopScanProjects)
                {
                    if (string.IsNullOrEmpty(projectName) || tempNode.Value.ProjectName.MyContains(projectName))
                    {
                        string tempKey = tempNode.Key;
                        string tempName = string.IsNullOrEmpty(tempNode.Value.ProjectName) ? tempNode.Value.PipelineId : tempNode.Value.ProjectName;
                        projectsSb.AppendLine(string.Format("【{0}】 [{1}] ", tempKey, tempName));
                        projectFindResult.AddKubeSphereDevopScanProject(tempName, tempKey);
                    }
                    //projectsSb.AppendLine(string.Format("【{0}】 [{1}] ", tempNode.Key, string.IsNullOrEmpty(tempNode.Value.ProjectName) ? tempNode.Value.PipelineId : tempNode.Value.ProjectName));
                }
            }

            if (MyConfiguration.UserConf.KubeSphereV3DevopScanProjects != null && MyConfiguration.UserConf.KubeSphereV3DevopScanProjects.Count > 0 && isShowKubeSphereProjects && isShowScanProjects)
            {
                isHasAnyProject = true;
                projectsSb.AppendLine("KubeSphereV3DevopScanProjects");
                foreach (var tempNode in MyConfiguration.UserConf.KubeSphereV3DevopScanProjects)
                {
                    if (string.IsNullOrEmpty(projectName) || tempNode.Value.ProjectName.MyContains(projectName))
                    {
                        string tempKey = tempNode.Key;
                        string tempName = string.IsNullOrEmpty(tempNode.Value.ProjectName) ? tempNode.Value.PipelineId : tempNode.Value.ProjectName;
                        projectsSb.AppendLine(string.Format("【{0}】 [{1}] ", tempKey, tempName));
                        projectFindResult.AddKubeSphereV3DevopScanProject(tempName, tempKey);
                    }
                    //projectsSb.AppendLine(string.Format("【{0}】 [{1}] ", tempNode.Key, string.IsNullOrEmpty(tempNode.Value.ProjectName) ? tempNode.Value.PipelineId : tempNode.Value.ProjectName));
                }
            }

            if (MyConfiguration.UserConf.BambooProjects != null && MyConfiguration.UserConf.BambooProjects.Count > 0 && isShowBambooProjects)
            {
                isHasAnyProject = true;
                projectsSb.AppendLine("BambooProjects");
                foreach (var tempNode in MyConfiguration.UserConf.BambooProjects)
                {
                    if (string.IsNullOrEmpty(projectName) || tempNode.Value.ProjectName.MyContains(projectName))
                    {
                        string tempKey = tempNode.Key;
                        string tempName = tempNode.Value.ProjectName;
                        projectsSb.AppendLine(string.Format("【{0}】 [{1}] [{2}] [{3}]", tempNode.Key, tempNode.Value.ProjectName, tempNode.Value.ProjectKey, tempNode.Value.IsNeedCommit ? "deploy when new commit" : "deploy not care commit"));
                        projectFindResult.AddBambooProject(tempName, tempKey);
                    }
                    //projectsSb.AppendLine(string.Format("【{0}】 [{1}] [{2}] [{3}]", tempNode.Key, tempNode.Value.ProjectName, tempNode.Value.ProjectKey, tempNode.Value.IsNeedCommit ? "deploy when new commit" : "deploy not care commit"));
                }
            }

            if (MyConfiguration.UserConf.PrivateBambooProjects != null && MyConfiguration.UserConf.PrivateBambooProjects.Count > 0 && isShowBambooProjects)
            {
                isHasAnyProject = true;
                projectsSb.AppendLine("PrivateBambooProjects");
                foreach (var tempNode in MyConfiguration.UserConf.PrivateBambooProjects)
                {
                    if (string.IsNullOrEmpty(projectName) || tempNode.Value.ProjectName.MyContains(projectName))
                    {
                        string tempKey = tempNode.Key;
                        string tempName = tempNode.Value.ProjectName;
                        projectsSb.AppendLine(string.Format("【{0}】 [{1}] [{2}]", tempNode.Key, tempNode.Value.ProjectName, tempNode.Value.BambooProject));
                        projectFindResult.AddPrivateBambooProject(tempName, tempKey);
                    }
                    //projectsSb.AppendLine(string.Format("【{0}】 [{1}] [{2}]", tempNode.Key, tempNode.Value.ProjectName, tempNode.Value.BambooProject));
                }
            }

            if (MyConfiguration.UserConf.RancherProjects != null && MyConfiguration.UserConf.RancherProjects.Count > 0 && isShowRancherProjects)
            {
                isHasAnyProject = true;
                projectsSb.AppendLine("RancherProjects");
                foreach (var tempNode in MyConfiguration.UserConf.RancherProjects)
                {
                    if (string.IsNullOrEmpty(projectName) || tempNode.Value.ProjectName.MyContains(projectName))
                    {
                        string tempKey = tempNode.Key;
                        string tempName = tempNode.Value.ProjectName;
                        projectsSb.AppendLine(string.Format("【{0}】 [{1}] [branch:{2}] [{3}]", tempNode.Key, tempNode.Value.ProjectName, tempNode.Value.Branch, tempNode.Value.IsNeedCommit ? "deploy when new commit" : "deploy not care commit"));
                        projectFindResult.AddRancherProject(tempName, tempKey);
                    }
                    //projectsSb.AppendLine(string.Format("【{0}】 [{1}] [branch:{2}] [{3}]", tempNode.Key, tempNode.Value.ProjectName, tempNode.Value.Branch, tempNode.Value.IsNeedCommit ? "deploy when new commit" : "deploy not care commit"));
                }
            }
            if (!isHasAnyProject)
            {
                projectsSb.Append("not find any project");
            }
            return projectsSb.ToString().TrimEnd('\n').TrimEnd('\r');
        }

        public static async Task<DeploymentResult> BuildByKey(string key, string triggerUser = null, Dictionary<string, string> configDc = null, Action<string, string> pushMessageAction = null)
        {
            //Conf 文件中配置的指定流水线 （后面的执行器，通过扫描配置，将弃用这种配置方式）
            if (MyConfiguration.UserConf.KubeSphereProjects != null && MyConfiguration.UserConf.KubeSphereProjects.ContainsKey(key))
            {
                Console.WriteLine(MyConfiguration.UserConf.KubeSphereProjects[key].ProjectName);
                try
                {
                    string tempWorkloadId = MyConfiguration.UserConf.KubeSphereProjects[key].WorkloadId;
                    bool isUsePreDevopId = false;
                    if (configDc != null && configDc.ContainsKey("ENV"))
                    {
                        if (configDc["ENV"] == "pre")
                        {
                            tempWorkloadId = MyConfiguration.UserConf.KubeSphereProjects[key].WorkloadPreId;
                            isUsePreDevopId = !string.IsNullOrEmpty(MyConfiguration.UserConf.KubeSphereProjects[key].DevopPreId);
                        }
                    }
                    else if (configDc != null && configDc.ContainsKey("API_ENV"))
                    {
                        if (configDc["API_ENV"] == "pre")
                        {
                            tempWorkloadId = MyConfiguration.UserConf.KubeSphereProjects[key].WorkloadPreId;
                            isUsePreDevopId = !string.IsNullOrEmpty(MyConfiguration.UserConf.KubeSphereProjects[key].DevopPreId);
                        }
                    }

                    if (GetIsUseKubesphereV3Deployment(MyConfiguration.UserConf.KubeSphereProjects[key].DevopId))
                    {
                        return await ExecuteHelper.MyExecuteMan.DeploymentForKubesphereV3Async(MyConfiguration.UserConf.KubeSphereProjects[key].IsNeedCommit, isUsePreDevopId ? MyConfiguration.UserConf.KubeSphereProjects[key].DevopPreId : MyConfiguration.UserConf.KubeSphereProjects[key].DevopId, MyConfiguration.UserConf.KubeSphereProjects[key].PipelineId, tempWorkloadId, triggerUser, configDc, pushMessageAction);
                    }
                    else
                    {
                        return await ExecuteHelper.MyExecuteMan.DeploymentForKubesphereAsync(MyConfiguration.UserConf.KubeSphereProjects[key].IsNeedCommit, isUsePreDevopId ? MyConfiguration.UserConf.KubeSphereProjects[key].DevopPreId : MyConfiguration.UserConf.KubeSphereProjects[key].DevopId, MyConfiguration.UserConf.KubeSphereProjects[key].PipelineId, tempWorkloadId, triggerUser, configDc, pushMessageAction);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            else if (MyConfiguration.UserConf.KubeSphereDevopScanProjects != null && MyConfiguration.UserConf.KubeSphereDevopScanProjects.ContainsKey(key))
            {
                Console.WriteLine(MyConfiguration.UserConf.KubeSphereDevopScanProjects[key].ProjectName);
                try
                {
                    string tempWorkloadId = MyConfiguration.UserConf.KubeSphereDevopScanProjects[key].WorkloadId;
                    bool isUsePreDevopId = false;
                    if (configDc != null && configDc.ContainsKey("ENV"))
                    {
                        if (configDc["ENV"] == "pre")
                        {
                            tempWorkloadId = MyConfiguration.UserConf.KubeSphereDevopScanProjects[key].WorkloadPreId;
                            isUsePreDevopId = !string.IsNullOrEmpty(MyConfiguration.UserConf.KubeSphereDevopScanProjects[key].DevopPreId);
                        }
                    }
                    else if (configDc != null && configDc.ContainsKey("API_ENV"))
                    {
                        if (configDc["API_ENV"] == "pre")
                        {
                            tempWorkloadId = MyConfiguration.UserConf.KubeSphereDevopScanProjects[key].WorkloadPreId;
                            isUsePreDevopId = !string.IsNullOrEmpty(MyConfiguration.UserConf.KubeSphereDevopScanProjects[key].DevopPreId);
                        }
                    }
                    return await ExecuteHelper.MyExecuteMan.DeploymentForKubesphereAsync(MyConfiguration.UserConf.KubeSphereDevopScanProjects[key].IsNeedCommit, isUsePreDevopId ? MyConfiguration.UserConf.KubeSphereDevopScanProjects[key].DevopPreId : MyConfiguration.UserConf.KubeSphereDevopScanProjects[key].DevopId, MyConfiguration.UserConf.KubeSphereDevopScanProjects[key].PipelineId, tempWorkloadId, triggerUser, configDc, pushMessageAction);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            else if (MyConfiguration.UserConf.KubeSphereV3DevopScanProjects != null && MyConfiguration.UserConf.KubeSphereV3DevopScanProjects.ContainsKey(key))
            {
                Console.WriteLine(MyConfiguration.UserConf.KubeSphereV3DevopScanProjects[key].ProjectName);
                try
                {
                    string tempWorkloadId = MyConfiguration.UserConf.KubeSphereV3DevopScanProjects[key].WorkloadId;
                    bool isUsePreDevopId = false;
                    if (configDc != null && configDc.ContainsKey("ENV"))
                    {
                        if (configDc["ENV"] == "pre")
                        {
                            tempWorkloadId = MyConfiguration.UserConf.KubeSphereV3DevopScanProjects[key].WorkloadPreId;
                            isUsePreDevopId = !string.IsNullOrEmpty(MyConfiguration.UserConf.KubeSphereV3DevopScanProjects[key].DevopPreId);
                        }
                    }
                    else if (configDc != null && configDc.ContainsKey("API_ENV"))
                    {
                        if (configDc["API_ENV"] == "pre")
                        {
                            tempWorkloadId = MyConfiguration.UserConf.KubeSphereV3DevopScanProjects[key].WorkloadPreId;
                            isUsePreDevopId = !string.IsNullOrEmpty(MyConfiguration.UserConf.KubeSphereV3DevopScanProjects[key].DevopPreId);
                        }
                    }
                    return await ExecuteHelper.MyExecuteMan.DeploymentForKubesphereV3Async(MyConfiguration.UserConf.KubeSphereV3DevopScanProjects[key].IsNeedCommit, isUsePreDevopId ? MyConfiguration.UserConf.KubeSphereV3DevopScanProjects[key].DevopPreId : MyConfiguration.UserConf.KubeSphereV3DevopScanProjects[key].DevopId, MyConfiguration.UserConf.KubeSphereV3DevopScanProjects[key].PipelineId, tempWorkloadId, triggerUser, configDc, pushMessageAction);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            else if (MyConfiguration.UserConf.BambooProjects != null && MyConfiguration.UserConf.BambooProjects.ContainsKey(key))
            {
                Console.WriteLine(MyConfiguration.UserConf.BambooProjects[key].ProjectName);
                try
                {
                    return await ExecuteHelper.MyExecuteMan.DeploymentForBambooAsync(MyConfiguration.UserConf.BambooProjects[key].IsNeedCommit, MyConfiguration.UserConf.BambooProjects[key].ProjectKey);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            else if (MyConfiguration.UserConf.PrivateBambooProjects != null && MyConfiguration.UserConf.PrivateBambooProjects.ContainsKey(key))
            {
                Console.WriteLine(MyConfiguration.UserConf.PrivateBambooProjects[key].ProjectName);
                try
                {
                    return await ExecuteHelper.MyExecuteMan.DeploymentForPrivateBambooAsync(MyConfiguration.UserConf.PrivateBambooProjects[key].IsNeedCommit, MyConfiguration.UserConf.PrivateBambooProjects[key].BambooProject, MyConfiguration.UserConf.PrivateBambooProjects[key].RancherWorkload, MyConfiguration.UserConf.PrivateBambooProjects[key].RancherImage, configDc, pushMessageAction);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            else if (MyConfiguration.UserConf.RancherProjects != null && MyConfiguration.UserConf.RancherProjects.ContainsKey(key))
            {
                Console.WriteLine(MyConfiguration.UserConf.RancherProjects[key].ProjectName);
                try
                {
                    return await ExecuteHelper.MyExecuteMan.DeploymentForRancherAsync(MyConfiguration.UserConf.RancherProjects[key].IsNeedCommit, MyConfiguration.UserConf.RancherProjects[key].ProjectId, MyConfiguration.UserConf.RancherProjects[key].PipelineId, MyConfiguration.UserConf.RancherProjects[key].Branch, MyConfiguration.UserConf.RancherProjects[key].ProjectName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            else
            {
                Console.WriteLine("unknow command");
            }
            return DeploymentResult.Failed;
        }

        public static async Task<bool> CancelByKey(string key, string id = null)
        {
            if (MyConfiguration.UserConf.KubeSphereProjects != null && MyConfiguration.UserConf.KubeSphereProjects.ContainsKey(key))
            {
                Console.WriteLine("cancle build [{0}]", MyConfiguration.UserConf.KubeSphereProjects[key].ProjectName);
                try
                {
                    int? nowId = int.Parse(id);
                    if (GetIsUseKubesphereV3Deployment(MyConfiguration.UserConf.KubeSphereProjects[key].DevopId))
                    {
                        return await ExecuteHelper.MyExecuteMan.CancelDeploymentForKubesphereV3Async(MyConfiguration.UserConf.KubeSphereProjects[key].DevopId, MyConfiguration.UserConf.KubeSphereProjects[key].PipelineId, nowId);
                    }
                    else
                    {
                        return await ExecuteHelper.MyExecuteMan.CancelDeploymentForKubesphereAsync(MyConfiguration.UserConf.KubeSphereProjects[key].DevopId, MyConfiguration.UserConf.KubeSphereProjects[key].PipelineId, nowId);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            else if (MyConfiguration.UserConf.KubeSphereDevopScanProjects != null && MyConfiguration.UserConf.KubeSphereDevopScanProjects.ContainsKey(key))
            {
                Console.WriteLine("cancle build [{0}]", MyConfiguration.UserConf.KubeSphereDevopScanProjects[key].ProjectName);
                try
                {
                    int? nowId = int.Parse(id);
                    return await ExecuteHelper.MyExecuteMan.CancelDeploymentForKubesphereAsync(MyConfiguration.UserConf.KubeSphereDevopScanProjects[key].DevopId, MyConfiguration.UserConf.KubeSphereDevopScanProjects[key].PipelineId, nowId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            else if (MyConfiguration.UserConf.KubeSphereV3DevopScanProjects != null && MyConfiguration.UserConf.KubeSphereV3DevopScanProjects.ContainsKey(key))
            {
                Console.WriteLine("cancle build [{0}]", MyConfiguration.UserConf.KubeSphereV3DevopScanProjects[key].ProjectName);
                try
                {
                    int? nowId = int.Parse(id);
                    return await ExecuteHelper.MyExecuteMan.CancelDeploymentForKubesphereV3Async(MyConfiguration.UserConf.KubeSphereV3DevopScanProjects[key].DevopId, MyConfiguration.UserConf.KubeSphereV3DevopScanProjects[key].PipelineId, nowId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            else if (MyConfiguration.UserConf.PrivateBambooProjects != null && MyConfiguration.UserConf.PrivateBambooProjects.ContainsKey(key))
            {
                Console.WriteLine("cancle build [{0}]", MyConfiguration.UserConf.PrivateBambooProjects[key].ProjectName);
                try
                {
                    return await ExecuteHelper.MyExecuteMan.CancelDeploymentForPrivateBambooAsync(id);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            else
            {
                Console.WriteLine("can not find KubeSphere runs to cancel");
            }
            return false;
        }


        /// <summary>
        /// 通过devop预测发布工程IsUseKubesphereV3Deployment
        /// </summary>
        /// <param name="devop"></param>
        /// <returns></returns>
        private static bool GetIsUseKubesphereV3Deployment(string devop)
        {
            bool isUseKubesphereV3Deployment = true;
            if (MyConfiguration.UserConf?.KubeSphereDevops?.Count > 0)
            {
                foreach (MonitorConf.KubeSphereDevop scanOps in MyConfiguration.UserConf.KubeSphereDevops)
                {
                    if (scanOps.DevopId == devop)
                    {
                        isUseKubesphereV3Deployment = false;
                        break;
                    }
                }
            }
            if (isUseKubesphereV3Deployment && MyConfiguration.UserConf.KubeSphereAllDevopNames.Contains(devop))
            {
                isUseKubesphereV3Deployment = false;
            }
            return isUseKubesphereV3Deployment;
        }

        /// <summary>
        /// BuildByName （只能发Kubesphere工程）
        /// </summary>
        /// <param name="devop"></param>
        /// <param name="pipeline"></param>
        /// <param name="workloads"></param>
        /// <param name="configDc"></param>
        /// <param name="pushMessageAction"></param>
        /// <param name="wxRobotUrl"></param>
        /// <param name="fsRobotUrl"></param>
        /// <param name="isPushStartMessage"></param>
        /// <returns></returns>
        public static async Task<DeploymentResult> BuildByName(string devop, string pipeline, string workloads, Dictionary<string, string> configDc = null, Action<string, string> pushMessageAction = null, string wxRobotUrl = null, string fsRobotUrl = null, string fsChatId = null, bool isPushStartMessage = true)
        {
            try
            {
                bool isUseKubesphereV3Deployment = true;
                if (MyConfiguration.UserConf?.KubeSphereDevops?.Count > 0)
                {
                    foreach (MonitorConf.KubeSphereDevop scanOps in MyConfiguration.UserConf.KubeSphereDevops)
                    {
                        if (scanOps.DevopId == devop)
                        {
                            isUseKubesphereV3Deployment = false;
                            break;
                        }
                    }
                }
                if (isUseKubesphereV3Deployment && MyConfiguration.UserConf.KubeSphereAllDevopNames.Contains(devop))
                {
                    isUseKubesphereV3Deployment = false;
                }
                if (isUseKubesphereV3Deployment)
                {
                    return await ExecuteHelper.MyExecuteMan.DeploymentForKubesphereV3Async(false, devop, pipeline, workloads, "api", configDc, pushMessageAction, wxRobotUrl, fsRobotUrl, fsChatId, isPushStartMessage);
                }
                return await ExecuteHelper.MyExecuteMan.DeploymentForKubesphereAsync(false, devop, pipeline, workloads, "api", configDc, pushMessageAction, wxRobotUrl, fsRobotUrl, fsChatId, isPushStartMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return DeploymentResult.Failed;
        }

        public static async Task<bool> Redeploy(string project, string workloads)
        {
            return await ExecuteHelper.MyExecuteMan.RedeployForRancherAsync(project, workloads);
        }

    }
}
