using System;
using System.Collections.Generic;
using DeploymentRobotService.Models;

namespace DeploymentRobotService.DeploymentService
{
    public class OperationHistory
    {
        public static List<OperationInfo> OperationInfos { get; private set; }
        static OperationHistory()
        {
            OperationInfos = new List<OperationInfo>();
        }

        public static void AddOperation(string user,string text , string time ,string operationType = "WxCmd")
        {
            lock (OperationInfos)
            {
                OperationInfos.Add(new OperationInfo() { OperationType = operationType, OperationUser = user, OperationText = text, OperationTime = time });
                if (OperationInfos.Count > 1000)
                {

                    OperationInfos.RemoveRange(0, 200);

                }
            }
        }
    }
}
