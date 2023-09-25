using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeploymentRobotService.Appsetting
{
    public class FsConfig
    {
        public static string ApiBaseUrl { get; set; }
        public static string AppID { get; set; }
        public static string AppSecret { get; set; }
        public static string OAuthDomain { get; set; }

        public static bool HasValue()
        {
            return !(string.IsNullOrEmpty(ApiBaseUrl) || string.IsNullOrEmpty(AppID) || string.IsNullOrEmpty(AppSecret));
        }
    }
}
