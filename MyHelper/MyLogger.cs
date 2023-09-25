using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeploymentRobotService.MyHelper
{
    public class MyLogger
    {
        public static ILogger Logger { get; set; }

        public static void LogInfo(string mes)
        {
            Logger?.LogInformation("[{0}] {1}", DateTime.Now, mes);
        }

        public static void LogWarning(string mes)
        {
            Logger?.LogWarning("[{0}] {1}", DateTime.Now, mes);
        }

        public static void LogError(string mes, Exception ex = null)
        {

            if (ex == null)
            {
                Logger?.LogError("[{0}] {1}", DateTime.Now, mes);
            }
            else
            {
                Logger?.LogError(ex, "[{0}] {1} {2}", DateTime.Now, mes, ex.Message);

            }

        }
    }
}
