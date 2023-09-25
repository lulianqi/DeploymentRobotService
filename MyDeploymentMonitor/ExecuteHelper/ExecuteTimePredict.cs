using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace MyDeploymentMonitor.ExecuteHelper
{
    public class PredictTimeData
    {
        /// <summary>
        /// 本次跟新总的预测耗时
        /// </summary>
        public int PredictTotalTime { get; set; }

        /// <summary>
        /// 本次Build的预测耗时
        /// </summary>
        public int PredictBuildElapsed { get; set; }
        /// <summary>
        /// 本次服务启动的预测耗时
        /// </summary>
        public int PredictLaunchElapsed { get; set; }
    }

    public class ExecuteTimePredict
    {
        private static Dictionary<string, PredictTimeData> TimePredictDc;
        private static HttpClient httpClient;

        private const string _loadExecuteTimePredictDataUrl = "http://api.lulianqi.com/api/TextGroup/24a11275d49f4be79b47e2132bce9379";

        static ExecuteTimePredict()
        {
            TimePredictDc = new Dictionary<string, PredictTimeData>();
            httpClient = new HttpClient();
        }

        public static async Task LoadData()
        {
            HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(_loadExecuteTimePredictDataUrl);
            if(httpResponseMessage.StatusCode== System.Net.HttpStatusCode.OK)
            {
                Dictionary<string, PredictTimeData> tempPredictDc = MyExtendHelper.DeserializeContractDataFromJsonString<Dictionary<string, PredictTimeData>>(await httpResponseMessage.Content.ReadAsStringAsync());
                if(tempPredictDc!=null)
                {
                    TimePredictDc = tempPredictDc;
                }
                else
                {
                    Console.WriteLine("ContractData fail in [ExecuteTimePredict-LoadData]");
                }
            }
            else
            {
                Console.WriteLine("get source data fail in [ExecuteTimePredict-LoadData]");

            }

        }

        public static async Task UploadData()
        {
            string tempUploadStr = MyExtendHelper.ObjectToJsonStr(TimePredictDc);
            HttpResponseMessage httpResponseMessage = await httpClient.PostAsync(_loadExecuteTimePredictDataUrl, new StringContent(tempUploadStr, System.Text.Encoding.UTF8));
            if (httpResponseMessage.StatusCode != System.Net.HttpStatusCode.OK)
            {
                Console.WriteLine("upload source data fail in [ExecuteTimePredict-UploadData]");
            }
        }

        private static void FillDefaultPredictTime(DeploymentExecuteStatus deploymentExecuteStatus)
        {
            deploymentExecuteStatus.TimeLine.PredictTotalElapsed = 60 + 60 + 10;
            deploymentExecuteStatus.TimeLine.PredictBuildElapsed = 60;
            deploymentExecuteStatus.TimeLine.PredictLaunchElapsed = 60;
        }

        public static void FillPredictTime(DeploymentExecuteStatus deploymentExecuteStatus)
        {
            if (deploymentExecuteStatus?.TimeLine is null)
            {
                throw new ArgumentNullException(nameof(deploymentExecuteStatus));
            }
            if(string.IsNullOrEmpty(deploymentExecuteStatus.Devop)||string.IsNullOrEmpty(deploymentExecuteStatus.Pipeline))
            {
                FillDefaultPredictTime(deploymentExecuteStatus);
                return;
            }
            string tempKey = $"Devop:[{deploymentExecuteStatus.Devop}]-Pipeline:[{deploymentExecuteStatus.Pipeline}]-Workloads:[{deploymentExecuteStatus.Workloads ?? ""}]";
            if(TimePredictDc.ContainsKey(tempKey))
            {
                deploymentExecuteStatus.TimeLine.PredictTotalElapsed = TimePredictDc[tempKey].PredictTotalTime;
                deploymentExecuteStatus.TimeLine.PredictBuildElapsed = TimePredictDc[tempKey].PredictBuildElapsed;
                deploymentExecuteStatus.TimeLine.PredictLaunchElapsed = TimePredictDc[tempKey].PredictLaunchElapsed;
                return;
            }
            FillDefaultPredictTime(deploymentExecuteStatus);
        }

        public static void UpdatePredictTime(DeploymentExecuteStatus deploymentExecuteStatus)
        {
            if (deploymentExecuteStatus?.TimeLine is null)
            {
                throw new ArgumentNullException(nameof(deploymentExecuteStatus));
            }
            if (string.IsNullOrEmpty(deploymentExecuteStatus.Devop) || string.IsNullOrEmpty(deploymentExecuteStatus.Pipeline))
            {
                return;
            }
            PredictTimeData tempPredictTime = new PredictTimeData();
            switch (deploymentExecuteStatus.Status)
            {
                case ExecuteStatus.LanuchSkip:
                case ExecuteStatus.LaunchCancle:
                case ExecuteStatus.LaunchError:
                case ExecuteStatus.LaunchFailed:
                case ExecuteStatus.LaunchStop:
                    tempPredictTime.PredictBuildElapsed = deploymentExecuteStatus.TimeLine.GetActualBuildTime();
                    tempPredictTime.PredictTotalTime = deploymentExecuteStatus.TimeLine.GetActualFinishTime();
                    break;
                case ExecuteStatus.LaunchSuccess:
                    tempPredictTime.PredictBuildElapsed = deploymentExecuteStatus.TimeLine.GetActualBuildTime();
                    tempPredictTime.PredictLaunchElapsed = deploymentExecuteStatus.TimeLine.GetActualLaunchTime();
                    tempPredictTime.PredictTotalTime = deploymentExecuteStatus.TimeLine.GetActualFinishTime();
                    break;
                default:
                    return;
            }
            string tempKey = $"Devop:[{deploymentExecuteStatus.Devop}]-Pipeline:[{deploymentExecuteStatus.Pipeline}]-Workloads:[{deploymentExecuteStatus.Workloads ?? ""}]";
            if (TimePredictDc.ContainsKey(tempKey))
            {
                if (tempPredictTime.PredictBuildElapsed > 0)
                {
                    TimePredictDc[tempKey].PredictBuildElapsed = tempPredictTime.PredictBuildElapsed;
                }
                if (tempPredictTime.PredictLaunchElapsed > 0)
                {
                    TimePredictDc[tempKey].PredictLaunchElapsed = tempPredictTime.PredictLaunchElapsed;
                }
                if (tempPredictTime.PredictTotalTime > 0)
                {
                    TimePredictDc[tempKey].PredictTotalTime = tempPredictTime.PredictTotalTime;
                }
            }
            else
            {
                TimePredictDc.Add(tempKey, tempPredictTime);
            }
        }
    }
}
