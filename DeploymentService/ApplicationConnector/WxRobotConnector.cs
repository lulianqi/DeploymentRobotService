using DeploymentRobotService.Appsetting;
using DeploymentRobotService.DeploymentService.MyCommandLine;
using DeploymentRobotService.MyHelper;
using MyDeploymentMonitor.ExecuteHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeploymentRobotService.DeploymentService
{
    public class WxRobotConnector:IRobotConnector
    {
        private  WxDeploymentMessageHelper wxDeploymentMessageHelper;

        public WxHelper NowWxHelper { get; private set; }

        public int MaxMessageLeng { get; private set; } = WxDeploymentMessageHelper.maxByteLength*5;

        public string AppChannel { get; } = "wx";

        public WxRobotConnector()
        {
            NowWxHelper = new WxHelper(WxConfig.CorpID, WxConfig.Corpsecret, WxConfig.Agentid);
            wxDeploymentMessageHelper = new WxDeploymentMessageHelper(NowWxHelper);
        }

        


        /// <summary>
        /// 将消息推送给指定企业微信用户（需要用户在应用范围内）
        /// </summary>
        /// <param name="toUser"></param>
        /// <param name="yourContent"></param>
        /// <returns></returns>
        public  async Task<bool> PushContent(string toUser,string yourContent)
        {
            return await wxDeploymentMessageHelper.PushLongContent(toUser, yourContent);
        }

        /// <summary>
        /// 为项目生成一个带操作连接的新文本描述
        /// </summary>
        /// <param name="Projects"></param>
        /// <returns></returns>
        public string AddActionForGetProjectResult(string Projects)
        {
            if (string.IsNullOrEmpty(Projects)) return "";
            StringBuilder sb = new StringBuilder(Projects.Length*4);
            string buildUrl = string.Format("{0}?key=", RobotConfig.BuildLink);
            string tempKey = null;
            int tempEnd = 0;
            string[] ContentLines = Projects.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            foreach(string line in ContentLines)
            {
                if(line.StartsWith('【'))
                {
                    tempEnd = line.IndexOf('】');
                    if(tempEnd>0)
                    {
                        //<a href=\"{0}\">发布</a>
                        tempKey = line.Substring(1, tempEnd-1);
                        sb.Append(line);
                        sb.Append("  <a href=\"");
                        sb.Append(buildUrl);
                        sb.Append(System.Web.HttpUtility.UrlEncode(tempKey));
                        sb.AppendLine("\">发布</a>");
                    }
                    else
                    {
                        sb.AppendLine(line);
                    }
                }
                else
                {
                    sb.AppendLine(line);
                }
            }
            if(sb.Length> Environment.NewLine.Length)
            {
                sb.Remove(sb.Length - Environment.NewLine.Length, Environment.NewLine.Length);
            }
            return sb.ToString();
        }

        /// <summary>
        /// 返回企业微信回调地址(eg:"CancleByKeyControllerkey={0}&id={1}")
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public  string GetOauthRedirectUrl( string state = null)
        {
            return NowWxHelper.GetWxOauthRedirectUrl(string.Format(@"{0}/user/WxOauth", WxConfig.OAuthDomain), state?? "state");
        }
    }
}
