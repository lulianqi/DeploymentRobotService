using DeploymentRobotService.MyHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeploymentRobotService.DeploymentService
{
    public class WxDeploymentMessageHelper
    {
        private WxHelper wxHelper;
        public const int maxByteLength = 2048;
        private const int maxTextLength = 1024;
        public WxDeploymentMessageHelper(string yourCorpId, string yourCorpsecret, int? yourAgentid )
        {
            wxHelper = new WxHelper(yourCorpId, yourCorpsecret, yourAgentid);
        }

        public WxDeploymentMessageHelper(WxHelper yourWxHelper)
        {
            wxHelper = yourWxHelper;
        }

        public async Task<bool> PushLongContent(string touser, string yourLongContent)
        {
            if(string.IsNullOrEmpty(yourLongContent))
            {
                return false;
            }
            if (Encoding.UTF8.GetBytes(yourLongContent).Length <= maxByteLength )
            {
                return await wxHelper.SendMessageAsync(touser, yourLongContent, 3);
            }
            else
            {
                System.IO.StringReader stringReader = new System.IO.StringReader(yourLongContent);
                string temLine = string.Empty;
                StringBuilder sb = new StringBuilder(maxByteLength);
                while ((temLine = stringReader.ReadLine()) != null)
                {
                    if (temLine.Length > maxTextLength)
                    {
                        temLine = temLine.Substring(0, maxTextLength);
                    }

                    if (sb.Length + temLine.Length > maxTextLength)
                    {
                        if (!await wxHelper.SendMessageAsync(touser, sb.ToString(), 1))
                        {
                            return false;
                        }
                        sb = new StringBuilder(temLine, maxTextLength);
                        sb.AppendLine();
                    }
                    else
                    {
                        sb.AppendLine(temLine);
                    }
                }
                if (sb.Length > 0)
                {
                    return await wxHelper.SendMessageAsync(touser, sb.ToString(), 1);
                }
            }
            return true;
        }
    }
}
