using System;
using System.Collections.Generic;

namespace MyDeploymentMonitor.ExecuteHelper
{
    public class DeploymentExecuteStatus
    {
        public DeploymentExecuteStatus()
        {
            BuildDetailUri = "unknown status";
            LaunchDetailUri = "unknown status";
        }

        public object Tag { get; set; }
        /// <summary>
        /// 本次发布的UUID
        /// </summary>
        public string ExecuteUid { get; set; } = Guid.NewGuid().ToString("N");
        /// <summary>
        /// 执行器名称（兼容不同发布平台）
        /// </summary>
        public string ExecuteManName { get; set; }
        /// <summary>
        /// 操作者（谁触发了构建）
        /// </summary>
        public string Operator { get; set; } = "ci";
        /// <summary>
        /// 备注信息
        /// </summary>
        public string Remark { get; set; } = "";
        /// <summary>
        /// 需要被at的用户列表
        /// </summary>
        public List<string> AtList { get; set; }
        /// <summary>
        /// 流水线Devop
        /// </summary>
        public string Devop { get; set; }
        /// <summary>
        /// 流水线Pipeline
        /// </summary>
        public string Pipeline { get; set; }
        /// <summary>
        /// Rancher Workloads
        /// </summary>
        public string Workloads { get; set; }
        /// <summary>
        /// 更新状态
        /// </summary>
        public ExecuteStatus Status { get; set; }
        /// <summary>
        /// 触发构建时使用的参数
        /// </summary>
        public Dictionary<string, string> ConfigDc { get; set; } = new Dictionary<string, string>();
        /// <summary>
        /// 状态更新使用的企业微信机器人地址
        /// </summary>
        public string WxRobotUrl { get; set; }
        /// <summary>
        /// 状态更新使用的飞书机器人地址
        /// </summary>
        public string FsRobotUrl { get; set; }
        /// <summary>
        /// 状态更新使用的飞书应用群组ID
        /// </summary>
        public string FsApplicationChatId { get; set; }

        /// <summary>
        /// 该发布状态是否已经进行过飞书消息加急
        /// </summary>
        public bool FsIsHasUrgented { get; set; } = false;

        /// <summary>
        /// Build 编号
        /// </summary>
        public string RunId { get; set; }
        /// <summary>
        /// Build Commit更新记录
        /// </summary>
        public BuildCommit BuildCommitInfo { get; set; } = new BuildCommit();
        /// <summary>
        /// 更新中的时间线
        /// </summary>
        public DeploymentTimeline TimeLine { get; set; } = new DeploymentTimeline();
        
        private string _buildDetailUri;
        /// <summary>
        /// Build详情地址 （build系统的中本次build的地址）
        /// </summary>
        public string BuildDetailUri
        {
            get { return _buildDetailUri; }
            set
            {
                _buildDetailUri = value;
                if (!Uri.IsWellFormedUriString(_buildDetailUri, UriKind.RelativeOrAbsolute))
                {
                    _buildDetailUri = $"http://bing.com/search?q={System.Web.HttpUtility.UrlEncode(_buildDetailUri)}";
                }
            }
        }
        private string _launchDetailUri;
        /// <summary>
        /// Launch详情地址 （Launch系统的中本次Launch的地址）
        /// </summary>
        public string LaunchDetailUri
        {
            get { return _launchDetailUri; }
            set
            {
                _launchDetailUri = value;
                if (!Uri.IsWellFormedUriString(_launchDetailUri, UriKind.RelativeOrAbsolute))
                {
                    _launchDetailUri = $"http://bing.com/search?q={System.Web.HttpUtility.UrlEncode(_launchDetailUri)}";
                }
            }
        }
        /// <summary>
        /// 本次build的log
        /// </summary>
        public string BuildLog { get; set; }
        /// <summary>
        /// 本次launch的地址
        /// </summary>
        public string LaunchLog { get; set; }

    }
}
