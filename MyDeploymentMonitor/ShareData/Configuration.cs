using MyDeploymentMonitor.DeploymentHelper;
using MyDeploymentMonitor.DeploymentHelper.DataHelper;
using MyDeploymentMonitor.ExecuteHelper;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using static MyDeploymentMonitor.ShareData.MonitorConf;

namespace MyDeploymentMonitor.ShareData
{
    public static class MyConfiguration
    {

        public static MonitorConf UserConf { get; set; }

        static MyConfiguration()
        {
            UserConf = new MonitorConf();
        }

        public static async Task<bool> InitConfigFileAsync(string confPath, bool isInitQuartz = true)
        {
            Console.WriteLine("load user config");
            MyConfiguration.UserConf = MyExtendHelper.DeserializeContractDataFromFilePath<ShareData.MonitorConf>(confPath);
            if (MyConfiguration.UserConf == null)
            {
                Console.WriteLine("load conf.json failed");
                return false;
            }
            if (MyConfiguration.UserConf.BambooUserName == null || MyConfiguration.UserConf.BambooUserPassword == null || MyConfiguration.UserConf.BambooBaseUrl == null)
            {
                Console.WriteLine("user,password,url can not be null");
                return false;
            }
            if (MyConfiguration.UserConf.Robot != null)
            {
                if (MyConfiguration.UserConf.Robot.WinXinRobotUrl != null)
                {
                    MyDeploymentMonitor.ExecuteHelper.MyExecuteMan.SetWinXinRobot(MyConfiguration.UserConf.Robot.WinXinRobotUrl);
                    Console.WriteLine("SetWinXinRobot complete");
                }
                if (MyConfiguration.UserConf.Robot.FeiShuRobotUrl != null)
                {
                    MyDeploymentMonitor.ExecuteHelper.MyExecuteMan.SetFeishuRobot(MyConfiguration.UserConf.Robot.FeiShuRobotUrl);
                    Console.WriteLine("FeiShuRobotUrl complete");
                }
                if (MyConfiguration.UserConf.Robot.DingDingRobotUrl != null)
                {
                    MyDeploymentMonitor.ExecuteHelper.MyExecuteMan.SetDingdingRobot(MyConfiguration.UserConf.Robot.DingDingRobotUrl, MyConfiguration.UserConf.Robot.DingDingRobotSecret);
                    Console.WriteLine("SetDingdingRobot complete");
                }
            }
            //External project
            MyConfiguration.UserConf.KubeSphereDevopScanProjects = await GetKubeSphereDevopScanProjects(MyConfiguration.UserConf.KubeSphereDevops , MyConfiguration.UserConf.KubeSphereExternalDataSource);
            MyConfiguration.UserConf.KubeSphereV3DevopScanProjects = await GetKubeSphereV3DevopScanProjects(MyConfiguration.UserConf.KubeSphereV3Devops, MyConfiguration.UserConf.KubeSphereExternalDataSource);

            MyConfiguration.UserConf.KubeSphereAllDevopNames = await MyExecuteMan.GetKubeSphereGetDevops(100);
            MyConfiguration.UserConf.KubeSphereV3AllDevopNames = await MyExecuteMan.GetKubeSphereV3GetDevops(100);


            Dictionary<string, PrivateBambooProject> tempExternalBambooProjects = await GetPrivateBambooProjects(MyConfiguration.UserConf.PrivateBambooExternalDataSource, MyConfiguration.UserConf.PrivateBambooScanProjectkeyPrefixKey);
            if (MyConfiguration.UserConf.PrivateBambooProjects == null) MyConfiguration.UserConf.PrivateBambooProjects = new Dictionary<string, PrivateBambooProject>();
            if(tempExternalBambooProjects!=null && tempExternalBambooProjects.Count>0)
            {
                foreach (var tempKp in tempExternalBambooProjects)
                {
                    MyConfiguration.UserConf.PrivateBambooProjects.MyAdd(tempKp.Key, tempKp.Value);
                }
            }

            if (MyConfiguration.UserConf.Scheduler != null && MyConfiguration.UserConf.Scheduler.Cron != null && MyConfiguration.UserConf.Scheduler.Projects != null && isInitQuartz)
            {
                if (await QuartzJob.QuartzInit.InitAsync())
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

        private static async Task<Dictionary<string, KubeSphereProject>> GetKubeSphereDevopScanProjects(List<KubeSphereDevop> yourKubeSphereDevops ,string appoinProjectsConfUrl = null)
        {
            Dictionary<string, KubeSphereProject> kubeSphereDevopScanProjectsDc = null;
            if (yourKubeSphereDevops!=null && yourKubeSphereDevops.Count>0)
            {
                kubeSphereDevopScanProjectsDc = new Dictionary<string, KubeSphereProject>();
                foreach (KubeSphereDevop devop in yourKubeSphereDevops)
                {
                    JObject jo =await MyDeploymentMonitor.ExecuteHelper.MyExecuteMan.GetKubeSphereDevopPipelines(devop.DevopId, devop.MaxPipeline);
                    if (jo != null && jo["items"] != null &&  jo["items"] is JArray)
                    {
                        JArray jPipelines = (JArray)jo["items"];
                        for (int i = 0; i < jPipelines.Count; i++)
                        {
                            string tempProjectName = jPipelines[i]["name"].Value<string>();
                            if (tempProjectName != null)
                            {
                                kubeSphereDevopScanProjectsDc.Add(devop.KeyPrefix + (i+1), new KubeSphereProject() {
                                    DevopId = devop.DevopId ,
                                    DevopPreId = devop.DevopPreId,
                                    PipelineId = tempProjectName ,
                                    WorkloadId = devop.WorkloadPrefix==null? null : string.Format("{0}:{1}", devop.WorkloadPrefix, tempProjectName),
                                    WorkloadPreId = devop.WorkloadPrePrefix == null ? null : string.Format("{0}:{1}", devop.WorkloadPrePrefix, tempProjectName),
                                    ProjectName = string.Format("{0} {1}",  tempProjectName ,devop.DevopIdMark) ,
                                    IsNeedCommit =false });
                            }
                            else
                            {
                                Console.WriteLine("an error KubeSphereProject JObject with\n" + jPipelines[i].ToString());
                            }
                        }
                    }
                }
            }
            if(appoinProjectsConfUrl !=null && kubeSphereDevopScanProjectsDc!=null && kubeSphereDevopScanProjectsDc.Count>0)
            {
                Dictionary<string, string> appointDc =await GetAppointScanProjects(appoinProjectsConfUrl);
                if(appointDc !=null && appointDc.Count>0)
                {
                    Dictionary<string, KubeSphereProject> appointProjectsDc = new Dictionary<string, KubeSphereProject>();
                    foreach(var tempAppoint in appointDc)
                    {
                        var find = kubeSphereDevopScanProjectsDc.FirstOrDefault<KeyValuePair<string, KubeSphereProject>>((project) => (tempAppoint.Value ==project.Value.PipelineId));
                        if(find.Key!=null)
                        {
                            appointProjectsDc.MyAdd(tempAppoint.Key, find.Value);
                        }
                    }
                    //if(appointProjectsDc.Count>0)
                    //{
                    //    foreach(var scan in kubeSphereDevopScanProjectsDc)
                    //    {
                    //        appointProjectsDc.MyAdd(scan.Key, scan.Value);
                    //    }
                    //}
                    //kubeSphereDevopScanProjectsDc = appointProjectsDc;

                    foreach (var scan in appointProjectsDc)
                    {
                        MyConfiguration.UserConf.KubeSphereProjects.MyAdd(scan.Key, scan.Value);
                    }
         
                }
            }
            return kubeSphereDevopScanProjectsDc;
        }

        private static async Task<Dictionary<string, KubeSphereProject>> GetKubeSphereV3DevopScanProjects(List<KubeSphereDevop> yourKubeSphereDevops, string appoinProjectsConfUrl = null)
        {
            Dictionary<string, KubeSphereProject> kubeSphereDevopScanProjectsDc = null;
            if (yourKubeSphereDevops != null && yourKubeSphereDevops.Count > 0)
            {
                kubeSphereDevopScanProjectsDc = new Dictionary<string, KubeSphereProject>();
                foreach (KubeSphereDevop devop in yourKubeSphereDevops)
                {
                    JObject jo = await MyDeploymentMonitor.ExecuteHelper.MyExecuteMan.GetKubeSphereV3DevopPipelines(devop.DevopId, devop.MaxPipeline);
                    if (jo != null && jo["items"] != null && jo["items"] is JArray)
                    {
                        JArray jPipelines = (JArray)jo["items"];
                        for (int i = 0; i < jPipelines.Count; i++)
                        {
                            string tempProjectName = jPipelines[i]["metadata"]?["name"].Value<string>();
                            if (tempProjectName != null)
                            {
                                kubeSphereDevopScanProjectsDc.Add(devop.KeyPrefix + (i + 1), new KubeSphereProject()
                                {
                                    DevopId = devop.DevopId,
                                    DevopPreId = devop.DevopPreId,
                                    PipelineId = tempProjectName,
                                    WorkloadId = devop.WorkloadPrefix == null ? null : string.Format("{0}:{1}", devop.WorkloadPrefix, tempProjectName),
                                    WorkloadPreId = devop.WorkloadPrePrefix == null ? null : string.Format("{0}:{1}", devop.WorkloadPrePrefix, tempProjectName),
                                    ProjectName = string.Format("{0} {1}", tempProjectName, devop.DevopIdMark),
                                    IsNeedCommit = false
                                });
                            }
                            else
                            {
                                Console.WriteLine("an error KubeSphereProject JObject with\n" + jPipelines[i].ToString());
                            }
                        }
                    }
                }
            }
            if (appoinProjectsConfUrl != null && kubeSphereDevopScanProjectsDc != null && kubeSphereDevopScanProjectsDc.Count > 0)
            {
                Dictionary<string, string> appointDc = await GetAppointScanProjects(appoinProjectsConfUrl);
                if (appointDc != null && appointDc.Count > 0)
                {
                    Dictionary<string, KubeSphereProject> appointProjectsDc = new Dictionary<string, KubeSphereProject>();
                    foreach (var tempAppoint in appointDc)
                    {
                        var find = kubeSphereDevopScanProjectsDc.FirstOrDefault<KeyValuePair<string, KubeSphereProject>>((project) => (tempAppoint.Value == project.Value.PipelineId));
                        if (find.Key != null)
                        {
                            appointProjectsDc.MyAdd(tempAppoint.Key, find.Value);
                        }
                    }
                    foreach (var scan in appointProjectsDc)
                    {
                        MyConfiguration.UserConf.KubeSphereProjects.MyAdd(scan.Key, scan.Value);
                    }

                }
            }
            return kubeSphereDevopScanProjectsDc;
        }


        /// <summary>
        /// 通过git文件读取，自定义配置键值对
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private static async Task<Dictionary<string, string>> GetAppointScanProjects(string filePath)
        {
            Dictionary<string, string> resultDc = new Dictionary<string, string>();
            try
            {
                Stream response = await MyDeploymentMonitor.ExecuteHelper.MyExecuteMan.GetMyGitFileContentStreamAsync(filePath);
                if (response!=null)
                {
                    //await response.Content.ReadAsStreamAsync()
                    using (StreamReader responseStreamReader = new StreamReader(response))
                    {
                        string tempConf = null;
                        while (responseStreamReader.Peek() >= 0)
                        {
                            tempConf = responseStreamReader.ReadLine();
                            if (tempConf == null)
                            {
                                continue;
                            }
                            tempConf = tempConf.Trim(' ');
                            if (tempConf.StartsWith('#') || !tempConf.Contains(' '))
                            {
                                continue;
                            }
                            int tempSpitIndex = tempConf.IndexOf(' ');
                            resultDc.MyAdd(tempConf.Substring(0, tempSpitIndex), tempConf.Substring(tempSpitIndex + 1).Trim(' '));
                        }
                    }
                }
                else
                {
                    Console.WriteLine("GetKubeSphereAppointScanProjects fialed");
                    return null;
                }
                
            }
            catch(Exception ex) 
            {
                Console.WriteLine(ex.Message);
                resultDc = null;
            }
            return resultDc;
        }

        private static async Task<Dictionary<string,PrivateBambooProject>> GetPrivateBambooProjects(string filePath ,string scanBambooPrefixKey = null)
        {
            if (string.IsNullOrEmpty(filePath)) return null;
            Dictionary<string, PrivateBambooProject> resultProjectDc = new Dictionary<string, PrivateBambooProject>();
            try
            {
                Stream response = await MyDeploymentMonitor.ExecuteHelper.MyExecuteMan.GetMyGitFileContentStreamAsync(filePath);
                StringBuilder jsonSb = new StringBuilder();
                if (response != null)
                {
                    //await response.Content.ReadAsStreamAsync()
                    using (StreamReader responseStreamReader = new StreamReader(response))
                    {
                        string tempConf = null;
                        while (responseStreamReader.Peek() >= 0)
                        {
                            tempConf = responseStreamReader.ReadLine();
                            if (tempConf == null)
                            {
                                continue;
                            }
                            tempConf = tempConf.Trim(' ');
                            if (tempConf.StartsWith('#') )
                            {
                                continue;
                            }
                            jsonSb.AppendLine(tempConf);
                        }
                    }
                    resultProjectDc = MyExtendHelper.DeserializeContractDataFromJsonString<Dictionary<string, PrivateBambooProject>>(jsonSb.ToString());
                }
                else
                {
                    Console.WriteLine("GetKubeSphereAppointScanProjects in git file fialed");
                    resultProjectDc = null;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                resultProjectDc = null;
            }

            if(!string.IsNullOrEmpty(scanBambooPrefixKey))
            {
                Console.WriteLine("scan bamboo");
                if (resultProjectDc == null) resultProjectDc = new Dictionary<string, PrivateBambooProject>();
                List<BambooProjectInfo> scanProjectList = await MyExecuteMan.ScanPrivateBambooProjects();
                if(scanProjectList!=null && scanProjectList.Count>0)
                {
                    for (int i = 0; i < scanProjectList.Count; i++)
                    {
                        BambooProjectInfo tempBambooProjectInfo = scanProjectList[i];
                        if(string.IsNullOrEmpty(tempBambooProjectInfo.id))
                        {
                            Console.WriteLine("find a error BambooProjectInfo");
                            continue;
                        }
                        if(tempBambooProjectInfo.enabled==false)
                        {
                            continue;
                        }
                        resultProjectDc.MyAdd(scanBambooPrefixKey + (i + 1), new PrivateBambooProject() {
                            IsNeedCommit=false,
                            RancherImage=null,
                            RancherWorkload= tempBambooProjectInfo.planName.Contains("江苏农信") ? "auto":null, //auto 既可以自动预测Rancher
                            BambooProject = tempBambooProjectInfo.id,
                            ProjectName= string.Format("{0} {1} {2}", tempBambooProjectInfo.projectName?? "NullProjectName", tempBambooProjectInfo.branchName??"NullBranchName", tempBambooProjectInfo.planName??"NullPlanName")
                        });
                    }
                }
            }
            return resultProjectDc;
        }

    }
}
