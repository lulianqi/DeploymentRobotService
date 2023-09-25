using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MyDeploymentMonitor.ShareData
{
    [DataContract]
    public class MonitorConf
    {
        [DataContract]
        public class SchedulerConf
        {
            [DataMember(Name = "cron", IsRequired = true)]
            public string Cron { get; set; }

            [DataMember(Name = "schedulerProject", IsRequired = true)]
            public Dictionary<string, string> Projects { get; set; }
        }

        [DataContract]
        public class BambooProject
        {
            [DataMember(Name = "key", IsRequired = true)]
            public string ProjectKey { get; set; }

            [DataMember(Name = "mark")]
            public string ProjectName { get; set; }

            [DataMember(Name = "needCommit")]
            public bool IsNeedCommit { get; set; }
        }

        [DataContract]
        public class PrivateBambooProject
        {
            [DataMember(Name = "bambooProject", IsRequired = true)]
            public string BambooProject { get; set; }

            [DataMember(Name = "rancherWorkload")]
            public string RancherWorkload { get; set; }

            [DataMember(Name = "rancherImage")]
            public string RancherImage { get; set; }

            [DataMember(Name = "mark")]
            public string ProjectName { get; set; }

            [DataMember(Name = "needCommit")]
            public bool IsNeedCommit { get; set; }
        }

        [DataContract]
        public class RancherProject
        {
            [DataMember(Name = "projectId", IsRequired = true)]
            public string ProjectId { get; set; }

            [DataMember(Name = "pipelineId", IsRequired = true)]
            public string PipelineId { get; set; }

            [DataMember(Name = "branch", IsRequired = true)]
            public string Branch { get; set; }

            [DataMember(Name = "mark")]
            public string ProjectName { get; set; }

            [DataMember(Name = "needCommit")]
            public bool IsNeedCommit { get; set; }
        }

        [DataContract]
        public class KubeSphereDevop
        {
            [DataMember(Name = "devopId", IsRequired = true)]
            public string DevopId { get; set; }

            [DataMember(Name = "devopPreId")]
            public string DevopPreId { get; set; }

            [DataMember(Name = "maxPipeline", IsRequired = true)]
            public int MaxPipeline { get; set; }

            [DataMember(Name = "workloadPrefix")]
            public string WorkloadPrefix { get; set; }

            [DataMember(Name = "workloadPrePrefix")]
            public string WorkloadPrePrefix { get; set; }

            [DataMember(Name = "keyPrefix", IsRequired = true)]
            public string KeyPrefix { get; set; }
            [DataMember(Name = "mark", IsRequired = true)]
            public string DevopIdMark { get; set; }

        }

        [DataContract]
        public class KubeSphereProject
        {
            [DataMember(Name = "devopId", IsRequired = true)]
            public string DevopId { get; set; }

            [DataMember(Name = "devopPreId")]
            public string DevopPreId { get; set; }

            [DataMember(Name = "pipelineId", IsRequired = true)]
            public string PipelineId { get; set; }

            [DataMember(Name = "workloadId")]
            public string WorkloadId { get; set; }

            [DataMember(Name = "workloadPreId")]
            public string WorkloadPreId { get; set; }

            [DataMember(Name = "mark")]
            public string ProjectName { get; set; }

            [DataMember(Name = "needCommit")]
            public bool IsNeedCommit { get; set; }
        }

        public class MessageRobot
        {
            [DataMember(Name = "ddRobot")]
            public String DingDingRobotUrl { get; set; }
            [DataMember(Name = "ddSecret")]
            public String DingDingRobotSecret { get; set; }

            [DataMember(Name = "wxRobot")]
            public String WinXinRobotUrl { get; set; }
            [DataMember(Name = "fsRobot")]
            public String FeiShuRobotUrl { get; set; }

            [DataMember(Name = "fsChatId")]
            public String FeiShuRobotChatId { get; set; }

            [DataMember(Name = "wxAtName")]
            public Dictionary<string, string> WxAtName { get; set; }
            [DataMember(Name = "wxAtPhone")]
            public Dictionary<string, string> WxAtPhone { get; set; }
            [DataMember(Name = "ddAtPhone")]
            public Dictionary<string, string> DdAtPhone { get; set; }

        }


        [DataMember(Name = "logUrl")]
        public String LogBaseUrl { get; set; }

        [DataMember(Name = "name", IsRequired = true)]
        public String BambooUserName { get; set; }

        [DataMember(Name = "password", IsRequired = true)]
        public String BambooUserPassword { get; set; }

        [DataMember(Name = "url", IsRequired = true)]
        public String BambooBaseUrl { get; set; }

        [DataMember(Name = "rancherBearerToken", IsRequired = true)]
        public String RancherBearerToken { get; set; }

        [DataMember(Name = "gitPrivateToken", IsRequired = true)]
        public String GitPrivateToken { get; set; }

        [DataMember(Name = "rancherUrl", IsRequired = true)]
        public String RancherUrl { get; set; }

        [DataMember(Name = "gitUrl", IsRequired = true)]
        public String GitUrl { get; set; }

        [DataMember(Name = "kubeSphereName", IsRequired = true)]
        public String KubeSphereUserName { get; set; }

        [DataMember(Name = "kubeSpherePassword", IsRequired = true)]
        public String KubeSphereUserPassword { get; set; }

        [DataMember(Name = "kubeSphereUrl", IsRequired = true)]
        public String KubeSphereBaseUrl { get; set; }

        [DataMember(Name = "kubeSphereV3Name", IsRequired = true)]
        public String KubeSphereV3UserName { get; set; }

        [DataMember(Name = "kubeSphereV3Password", IsRequired = true)]
        public String KubeSphereV3UserPassword { get; set; }

        [DataMember(Name = "kubeSphereV3Url", IsRequired = true)]
        public String KubeSphereV3BaseUrl { get; set; }

        [DataMember(Name = "kubeSphereExternalDataSource", IsRequired = false)]
        public String KubeSphereExternalDataSource { get; set; }

        [DataMember(Name = "privateBambooProjectExternalDataSource", IsRequired = false)]
        public String PrivateBambooExternalDataSource { get; set; }

        [DataMember(Name = "privateBambooProjectBaseWorkloadPath", IsRequired = false)]
        public String PrivateBambooProjectBaseWorkloadPath { get; set; }

        [DataMember(Name = "privateBambooScanProjectkeyPrefixKey", IsRequired = false)]
        public String PrivateBambooScanProjectkeyPrefixKey { get; set; }

        [DataMember(Name = "robot")]
        public MessageRobot Robot { get; set; }

        [DataMember(Name = "scheduler")]
        public SchedulerConf Scheduler { get; set; }

        [DataMember(Name = "bambooProjects")]
        public Dictionary<string, BambooProject> BambooProjects { get; set; }

        [DataMember(Name = "privateBambooProjects")]
        public Dictionary<string, PrivateBambooProject> PrivateBambooProjects { get; set; }

        [DataMember(Name = "rancherProjects")]
        public Dictionary<string, RancherProject> RancherProjects { get; set; }
        [DataMember(Name = "kubeSphereProjects")]
        public Dictionary<string, KubeSphereProject> KubeSphereProjects { get; set; } //Conf配置文件配置的KubeS流水线 （后面新增的执行器可能会弃用该配置方式，但该字典将用于git外部配置的加载存储）

        [DataMember(Name = "kubeSphereDevops")]
        public List<KubeSphereDevop> KubeSphereDevops { get; set; }  //KubeS扫描范围
        public Dictionary<string, KubeSphereProject> KubeSphereDevopScanProjects { get; set; }
        public List<string> KubeSphereAllDevopNames { get; set; } //发布用户/Token权限范围内的所有DevopNames列表


        [DataMember(Name = "kubeSphereV3Devops")]
        public List<KubeSphereDevop> KubeSphereV3Devops { get; set; }
        public Dictionary<string, KubeSphereProject> KubeSphereV3DevopScanProjects { get; set; }
        public List<string> KubeSphereV3AllDevopNames { get; set; }


    }
}
