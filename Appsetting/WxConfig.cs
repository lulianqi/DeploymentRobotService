using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeploymentRobotService.Appsetting
{
    public class WxConfig
    {
        public static string MessageToken { get; set; }
        public static string MessageEncodingAESKey { get; set; }
        public static string CorpID { get; set; }
        public static string Corpsecret { get; set; }
        public static int Agentid { get; set; }
        public static string OAuthDomain { get; set; }
        
    }
}
