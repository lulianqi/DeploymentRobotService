using DeploymentRobotService.Appsetting;
using DeploymentRobotService.DeploymentService.MyCommandLine;
using DeploymentRobotService.Models.FsModels.MessageData;
using DeploymentRobotService.MyHelper;
using MyDeploymentMonitor.ExecuteHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeploymentRobotService.DeploymentService
{
    public class FsRobotConnector: IRobotConnector
    {
        public FsHelper NowFsHelper { get; private set; }

        //feishu 发送消息大小限制
        //文本消息请求体最大不能超过150KB
        //卡片及富文本消息请求体最大不能超过30KB

        public int MaxMessageLeng { get; private set; } = 30000;

        public string AppChannel { get; } = "fs";

        public FsRobotConnector()
        {
            NowFsHelper = new FsHelper(FsConfig.ApiBaseUrl,FsConfig.AppID,FsConfig.AppSecret);
        }

        /// <summary>
        /// 将企业微信应用发送给用户的消息转换为飞书的Post消息（企业微信表示<a>的形式与飞书不一样）
        /// </summary>
        /// <param name="wxText"></param>
        /// <returns></returns>
        private static PostMessage ConvertWxTextToFsPost(string wxText)
        {
            PostMessage postMessage = new PostMessage();
            Content[][] nowPostContents;
            string startATag = "<a href=\"";
            string endHrefTag = "\">";
            string endATag = "</a>";
            if (wxText.Contains(startATag) && wxText.Contains(endATag))
            {
                List<Content> nowContentItems = new List<Content>();
                int nowPosition = 0;
                int startA = wxText.IndexOf(startATag, nowPosition);

                while(startA>0)
                {
                    //Text
                    string tempTextStr = wxText.Substring(nowPosition, startA-nowPosition);
                    nowContentItems.Add(new Content() { tag = "text", text = tempTextStr });
                    //<a>
                    nowPosition = startA + startATag.Length;
                    int tempEndHref = wxText.IndexOf(endHrefTag, nowPosition);
                    if(tempEndHref<0)
                    {
                        MyLogger.LogWarning($"[ConvertWxTextToFsPost] error index of {endHrefTag} \r\n{wxText}");
                        nowContentItems.Add(new Content() { tag = "text", text = wxText.Substring(nowPosition) });
                        break;
                    }
                    string tempHrefStr= wxText.Substring(nowPosition, tempEndHref - nowPosition);
                    //feishu 里href如果不合会阻止消息发送
                    if (!Uri.IsWellFormedUriString(tempHrefStr, UriKind.RelativeOrAbsolute))
                    {
                        tempHrefStr = $"http://bing.com/search?q={System.Web.HttpUtility.UrlEncode(tempHrefStr)}";
                    }
                    nowPosition = tempEndHref + endHrefTag.Length;
                    int tempEndA = wxText.IndexOf(endATag, nowPosition);
                    if (tempEndA < 0)
                    {
                        MyLogger.LogWarning($"[ConvertWxTextToFsPost] error index of {endATag} \r\n{wxText}");
                        nowContentItems.Add(new Content() { tag = "text", text = wxText.Substring(nowPosition) });
                        break;
                    }
                    string tempANameStr = wxText.Substring(nowPosition, tempEndA - nowPosition);
                    nowPosition = tempEndA + endATag.Length;
                    nowContentItems.Add(new Content() { tag = "a",  href = tempHrefStr, text = tempANameStr });

                    //next
                    startA = wxText.IndexOf(startATag, nowPosition);
                    if(startA<0)
                    {
                        nowContentItems.Add(new Content() { tag = "text", text = wxText.Substring(nowPosition) });
                        break;
                    }
                }
                nowPostContents = new Content[][] { nowContentItems.ToArray<Content>() };
            }
            else
            {
                nowPostContents = new Content[][] { new Content[] { new Content() { tag = "text",  text = wxText } }  };
            }
            postMessage.content = nowPostContents;
            return postMessage;
        }


        /// <summary>
        /// 将消息推送给指定企业微信用户（需要用户在应用范围内）
        /// </summary>
        /// <param name="toUser"></param>
        /// <param name="yourContent"></param>
        /// <returns></returns>
        public async Task<bool> PushContent(string toUser, string yourContent)
        {
            if(yourContent.Contains("<a href=\""))
            {
                PostMessageZh postMessageZh = new PostMessageZh(ConvertWxTextToFsPost(yourContent));
                return await NowFsHelper.SendPostMessageAsync(toUser, postMessageZh);
            }
            else
            {
                return await NowFsHelper.SendTextMessageAsync(toUser, yourContent);
            }
        }

        /// <summary>
        /// 为项目生成一个带操作连接的新文本描述
        /// </summary>
        /// <param name="Projects"></param>
        /// <returns></returns>
        public string AddActionForGetProjectResult(string Projects)
        {
            if (string.IsNullOrEmpty(Projects)) return "";
            StringBuilder sb = new StringBuilder(Projects.Length * 4);
            string buildUrl = string.Format("{0}?appChannel=fs&key=", RobotConfig.BuildLink);
            string tempKey = null;
            int tempEnd = 0;
            string[] ContentLines = Projects.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in ContentLines)
            {
                if (line.StartsWith('【'))
                {
                    tempEnd = line.IndexOf('】');
                    if (tempEnd > 0)
                    {
                        //<a href=\"{0}\">发布</a>
                        tempKey = line.Substring(1, tempEnd - 1);
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
            if (sb.Length > Environment.NewLine.Length)
            {
                sb.Remove(sb.Length - Environment.NewLine.Length, Environment.NewLine.Length);
            }
            return sb.ToString();
        }

        /// <summary>
        /// 返回企业飞书回调地址
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public string GetOauthRedirectUrl(string state = null)
        {
            return NowFsHelper.GetFsOauthRedirectUrl($"{FsConfig.OAuthDomain}/user/FsOauth", state ?? "state");
        }
    }
}
