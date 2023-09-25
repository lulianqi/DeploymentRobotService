using System;
using System.Collections.Generic;

namespace DeploymentRobotService.Appsetting
{
    public class JwtConfig
    {
        //token是谁颁发的
        public static string Issuer { get; set; }
        //token可以给哪些客户端使用
        public static string Audience { get; set; }
        //加密的key
        public static string SecretKey { get; set; }

        public static List<string> OauthUsers { get; set; }
    }
}
