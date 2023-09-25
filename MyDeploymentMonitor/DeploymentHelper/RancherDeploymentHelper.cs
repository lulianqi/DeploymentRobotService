using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using MyDeploymentMonitor.DeploymentHelper;
using MyDeploymentMonitor.WebHelper;
using MyDeploymentMonitor.DeploymentHelper.DataHelper;
using static MyDeploymentMonitor.ShareData.MonitorConf;

namespace MyDeploymentMonitor.DeploymentHelper
{
    public class RancherDeploymentHelper
    {
        private HttpClient httpClient;
        private HttpClient gitHttpClient;
        private string rancherBearerToken;


        public RancherDeploymentHelper(string bearerToken, string rancherBaseAddress, string privateToken, string gitBaseAddress)
        {
            HttpClientHandler httpclientHandler = new HttpClientHandler();
            httpclientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, error) => true;

            rancherBearerToken = bearerToken;
            httpClient = new HttpClient(httpclientHandler) { BaseAddress = new Uri(rancherBaseAddress) };
            httpClient.DefaultRequestHeaders.Add("Authorization", string.Format("Bearer {0}", bearerToken));

            gitHttpClient = new HttpClient() { BaseAddress = new Uri(gitBaseAddress) };
            gitHttpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", privateToken);
        }

        private void ShowMes(string mes)
        {
            Console.WriteLine(mes);
        }

        private long NowTimeStamp { get { return (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000; } }

        /// <summary>
        /// get the state of sevice
        /// </summary>
        /// <returns>is sevice running</returns>
        public async Task<bool> GetSeviceStateAsync()
        {
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(string.Format("/v3"));
                return true;
            }
            catch (Exception ex)
            {
                ShowMes(ex.Message);
            }
            return false;
        }


        public async Task<CommitInfo> GetCommitAsync(string rancherProjectId, string rancherPipelineId, string branch)
        {
            string gitRepository = await GetRepositoryPathAsync(rancherProjectId, rancherPipelineId);
            if (gitRepository == null)
            {
                ShowMes("can not GetRepositoryPathAsync");
                return null;
            }
            string lastCommitSHA = await GeLastCommitShaAsync(rancherProjectId, rancherPipelineId, branch);
            return await GetGitCommitInfoAsync(gitRepository, branch, lastCommitSHA);
        }

        public async Task<string> GetRepositoryPathAsync(string rancherProjectId, string rancherPipelineId)
        {
            HttpResponseMessage response = await httpClient.GetAsync(string.Format(@"v3/project/{0}/pipelines/{1}", rancherProjectId, rancherPipelineId));
            string responseBody = await response.Content.ReadAsStringAsync();
            JObject jo;
            try
            {
                jo = (JObject)JsonConvert.DeserializeObject(responseBody);
            }
            catch
            {
                ShowMes(string.Format("【{0}】 can not transition to JObject", responseBody));
                return null;

            }
            if (jo == null || jo["repositoryUrl"] == null)
            {
                ShowMes(string.Format("can not get repositoryUrl in 【{0}】", responseBody));
                return null;
            }
            string repositoryUrl = jo["repositoryUrl"].Value<string>();
            string repositoryPath = repositoryUrl.Remove(0, repositoryUrl.IndexOf('/', repositoryUrl.IndexOf(@"//") < 0 ? 0 : repositoryUrl.IndexOf(@"//") + 2) + 1);
            if (repositoryPath.EndsWith(".git")) repositoryPath = repositoryPath.Remove(repositoryPath.Length - 4, 4);
            return repositoryPath;
        }

        public async Task<string> GeLastCommitShaAsync(string rancherProjectId, string rancherPipelineId, string branch)
        {
            HttpResponseMessage response = await httpClient.GetAsync(string.Format(@"v3/project/{0}/pipelineExecutions?pipelineId={1}&sort=ended&order=desc&filters=true&branch={2}&executionState=Success&limit=1", rancherProjectId, rancherPipelineId, branch));
            string responseBody = await response.Content.ReadAsStringAsync();
            JObject jo = null;
            try
            {
                jo = (JObject)JsonConvert.DeserializeObject(responseBody);
            }
            catch
            {
                ShowMes(string.Format("【{0}】 can not transition to JObject", responseBody));
                return null;

            }
            if (jo == null || jo["data"] == null || !(jo["data"] is JArray))
            {
                ShowMes(string.Format("can not get CommitSHA list in 【{0}】", responseBody));
                return null;
            }
            if (((JArray)jo["data"]).Count > 0)
            {
                return ((JArray)jo["data"])[0]["commit"].Value<string>();
            }
            return null;
        }

        public async Task<CommitInfo> GetGitCommitInfoAsync(string project, string branch, string lastCommitSHA)
        {
            HttpResponseMessage response = await gitHttpClient.GetAsync(string.Format(@"api/v4/projects/{0}/repository/commits?ref_name={1}&per_page=20", System.Web.HttpUtility.UrlEncode(project), branch));
            string responseBody = await response.Content.ReadAsStringAsync();
            JArray jr = null;
            try
            {
                jr = (JArray)JsonConvert.DeserializeObject(responseBody);
            }
            catch
            {
                ShowMes(string.Format("【{0}】 can not transition to JArray", responseBody));
                return null;
            }
            if (jr == null || jr.Count > 0)
            {
                List<KeyValuePair<string, string>> CommitList = new List<KeyValuePair<string, string>>();
                foreach (JObject commit in jr)
                {
                    if (commit.Value<string>("id") == lastCommitSHA) break;
                    string author_name = commit.Value<string>("author_name");
                    string title = commit.Value<string>("title");
                    string message = commit.Value<string>("message");
                    string commitStr = null;
                    if (message.Contains(title) && (message.Length - title.Length) < 5)
                    {
                        commitStr = title;
                    }
                    else
                    {
                        commitStr = string.Format("{0}\r\n{1}", title, message);
                    }
                    CommitList.Add(new KeyValuePair<string, string>(author_name, commitStr));
                }
                return new CommitInfo() { BuildState = project, CommitList = CommitList, Branch = branch };
            }
            return null;
        }

        public async Task<string> StartDeployAsync(string rancherProjectId, string rancherPipelineId, string branch)
        {
            HttpResponseMessage response = await httpClient.PostAsync(string.Format(@"v3/project/{0}/pipelines/{1}?action=run", rancherProjectId, rancherPipelineId), new StringContent(string.Format(@"{{""branch"":""{0}""}}", branch), Encoding.UTF8, @"application/json"));
            string responseBody = await response.Content.ReadAsStringAsync();
            JObject jo = null;
            try
            {
                jo = (JObject)JsonConvert.DeserializeObject(responseBody);
            }
            catch
            {
                ShowMes(string.Format("【{0}】 can not transition to JObject", responseBody));
                return null;

            }
            if (jo == null || jo["run"] == null)
            {
                ShowMes(string.Format("can not get run id in 【{0}】", responseBody));
                return null;
            }
            string runId = jo["run"].Value<string>();

            if (jo["pipelineConfig"] != null && jo["pipelineConfig"]["stages"] is JArray)
            {
                StringBuilder sbStages = new StringBuilder(20);
                foreach (JObject tempState in ((JArray)jo["pipelineConfig"]["stages"]))
                {
                    sbStages.Append(">>");
                    sbStages.Append(tempState.Value<string>("name"));
                }
                ShowMes(sbStages.ToString());
            }
            else
            {
                ShowMes(string.Format("can not get  stages in 【run{0}】", runId));
            }

            return runId;
        }

        public async Task<RancherDeployState> GetDeployStatusAsync(string rancherProjectId, string rancherPipelineId, string runId)
        {
            RancherDeployState rancherDeployState = new RancherDeployState() { ExecutionState = "unknow", Stages = new List<RancherDeployState.Stage>() };
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(string.Format("v3/project/{0}/pipelineExecutions?pipelineId={1}&run={2}", rancherProjectId, rancherPipelineId, runId));
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                JObject jo = null;
                jo = (JObject)JsonConvert.DeserializeObject(responseBody, new JsonSerializerSettings { DateTimeZoneHandling = DateTimeZoneHandling.Local });
                JArray jr = jo["data"].Value<JArray>();
                if (jr != null && jr.Count > 0)
                {
                    rancherDeployState.ExecutionState = jr[0].Value<String>("executionState");

                    if (jr[0]["pipelineConfig"] != null && jr[0]["pipelineConfig"]["stages"] is JArray)
                    {
                        foreach (JObject tempState in ((JArray)jr[0]["pipelineConfig"]["stages"]))
                        {
                            rancherDeployState.Stages.Add(new RancherDeployState.Stage() { Name = tempState.Value<string>("name") });
                        }
                    }

                    JArray stages = jr[0].Value<JArray>("stages");
                    for (int i = 0; i < (stages.Count > rancherDeployState.Stages.Count ? rancherDeployState.Stages.Count : stages.Count); i++)
                    {
                        rancherDeployState.Stages[i].State = stages[i].Value<string>("state");
                        rancherDeployState.Stages[i].Start = stages[i].Value<string>("started");
                        rancherDeployState.Stages[i].End = stages[i].Value<string>("ended");
                    }
                }
            }
            catch (HttpRequestException e)
            {
                rancherDeployState.ExecutionState = "exception";
                ShowMes(e.Message);
            }
            catch (Exception e)
            {
                rancherDeployState.ExecutionState = "exception";
                ShowMes(string.Format(" can not get  with deploymentState", e.Message));
            }
            return rancherDeployState;
        }

        public async Task<string> GetDeployMessageAsync(string rancherProjectId, string rancherPipelineId, string runId, string stage = "0")
        {
            MyWebSocket webSocket = new MyWebSocket(string.Format(@"wss://{0}/v3/projects/{1}/pipelineExecutions/{2}-{3}/log?stage={4}&step=0", httpClient.BaseAddress.Host, rancherProjectId, rancherPipelineId, runId, stage));
            webSocket.WebSocket.Options.SetRequestHeader("Authorization", string.Format("Bearer {0}", rancherBearerToken));
            try
            {
                await webSocket.OpenAsync(2);
            }
            catch (Exception ex)
            {
                ShowMes(ex.ToString());
                return null;
            }
            string DeployMessage = await webSocket.ReceiveMesAsync(5);
            _ = webSocket.Close();
            return DeployMessage;
        }

        public string GetWorkloadDetailUri(string workloads)
        {
            return $"{(httpClient?.BaseAddress).ToString().TrimEnd('/')}/p/{workloads}";
        }

        public async Task<string> GetWorkloadState(string workloads)
        {
            if (workloads == null)
            {
                return null;
            }
            //HttpResponseMessage response = await httpClient.GetAsync(string.Format(@"v3/project/c-fj8rb:p-6czkl/workloads/{0}", workloads));
            HttpResponseMessage response = await httpClient.GetAsync(string.Format(@"v3/project/{0}", workloads));
            string responseBody = await response.Content.ReadAsStringAsync();
            JObject jo;
            try
            {
                jo = (JObject)JsonConvert.DeserializeObject(responseBody);

                if (response.StatusCode == HttpStatusCode.NotFound && jo.Value<string>("code") == "NotFound")
                {
                    return "NotFound";
                }
            }
            catch
            {
                ShowMes(string.Format("【{0}】 can not transition to JObject", responseBody));
                return null;
            }
            if (jo != null)
            {
                return jo.Value<string>("state");
            }
            return null;
        }

        public async Task<string> GetWorkloadErrorPodLog(string workloads ,int retryTime = 1)
        {
            //get error pod
            string errorPod = null;
            if (workloads == null || !workloads.Contains(':') || !workloads.Contains('/'))
            {
                return null;
            }
            string tempCluster = workloads.Remove(workloads.IndexOf(':'));
            string tempProject = workloads.Remove(workloads.IndexOf('/'));
            string tempWorkload = workloads.Remove(0, workloads.LastIndexOf(':') + 1);
            string tempWorkloadId = workloads.Remove(0, workloads.LastIndexOf('/') + 1);

            //c-fj8rb:p-6czkl/workloads/deployment:p-6czkl-pipeline:by-gateway-console
            //v3/project/c-fj8rb:p-6czkl/pods?workloadId=deployment%3Ap-6czkl-pipeline%3Aby-gateway-console
            HttpResponseMessage response = await httpClient.GetAsync(string.Format(@"v3/project/{0}/pods?workloadId={1}", tempProject , System.Web.HttpUtility.UrlEncode(tempWorkloadId)));
            string responseBody = await response.Content.ReadAsStringAsync();
            JObject jo;
            try
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }
                jo = (JObject)JsonConvert.DeserializeObject(responseBody);
            }
            catch
            {
                ShowMes(string.Format("【{0}】 can not transition to JObject", responseBody));
                return null;
            }
            if (jo != null)
            {
                if(jo["data"] != null && jo["data"] is JArray)
                {
                    foreach (JObject tempPodInfo in ((JArray)jo["data"]))
                    {
                        if(tempPodInfo.Value<string>("state") != null && tempPodInfo.Value<string>("state")!= "running" && tempPodInfo.Value<string>("state") != "Removing")
                        {
                            //"id": "p-6czkl-pipeline:by-gateway-console-9749dcc56-598p5"
                            errorPod = tempPodInfo.Value<string>("id");
                            break;
                        }
                    }
                }
            }
            //get error log
            if(errorPod!=null)
            {
                //wss://rancher.indata.cc/k8s/clusters/c-fj8rb/api/v1/namespaces/p-6czkl-pipeline/pods/by-gateway-console-9749dcc56-598p5/log?container=by-gateway-console&tailLines=500&follow=true&timestamps=true&previous=false
                string tempNamespacesAndpods = errorPod.Replace(":", @"/pods/");
                string wssUrl = string.Format(@"wss://{0}/k8s/clusters/{1}/api/v1/namespaces/{2}/log?container={3}&tailLines=500&follow=true&timestamps=true&previous=false", httpClient.BaseAddress.Host, tempCluster, tempNamespacesAndpods, System.Web.HttpUtility.UrlEncode(tempWorkload));
                MyWebSocket webSocket = new MyWebSocket(wssUrl);
                webSocket.WebSocket.Options.SetRequestHeader("Authorization", string.Format("Bearer {0}", rancherBearerToken));
                try
                {
                    await webSocket.OpenAsync(2);
                }
                catch (Exception ex)
                {
                    ShowMes(string.Format("can not open ws with :{0}", wssUrl));
                    ShowMes(ex.ToString());
                    if(retryTime>0)
                    {
                        return await GetWorkloadErrorPodLog(workloads, --retryTime);
                    }
                    return null;
                }
                string DeployMessage = await webSocket.ReceiveMesAsync(2);
                _ = webSocket.Close();
                return DeployMessage;
            }
            return null;
        }

        public async Task<string> GetWorkloadUpdataInfo(string workloadPath , string image=null)
        {
            if(string.IsNullOrEmpty(workloadPath))
            {
                return null;
            }
            HttpResponseMessage response = await httpClient.GetAsync(string.Format(@"v3/project/{0}", workloadPath));
            string responseBody = await response.Content.ReadAsStringAsync();
            JObject jo;
            try
            {
                jo = (JObject)JsonConvert.DeserializeObject(responseBody);
                if (response.StatusCode == HttpStatusCode.NotFound && jo.Value<string>("code") == "NotFound")
                {
                    ShowMes(string.Format("not find your workloadPath with {0}", workloadPath));
                    return null;
                }
            }
            catch
            {
                ShowMes(string.Format("【{0}】 can not transition to JObject", responseBody));
                return null;
            }
            try
            {
                jo["annotations"]["cattle.io/timestamp"] = DateTime.Now;
            }
            catch (Exception ex)
            {
                ShowMes(string.Format("updata annotations timestamp fail\r\n{0}", ex.ToString()));
                return null;
            }
            if (jo != null && !string.IsNullOrEmpty(image))
            {
                try
                {
                    jo["containers"][0]["image"] = image;
                }
                catch(Exception ex)
                {
                    ShowMes(string.Format("replace image fail \r\n{0}", ex.ToString()));
                    return null;
                }
            }
            return jo.ToString();
        }

        public async Task<string> UpdataWorkload(string workloadPath, string updateInfo)
        {
            if (string.IsNullOrEmpty(workloadPath))
            {
                return null;
            }
            HttpResponseMessage response = await httpClient.PutAsync(string.Format(@"v3/project/{0}", workloadPath),new StringContent(updateInfo, Encoding.UTF8, @"application/json"));
            string responseBody = await response.Content.ReadAsStringAsync();
            JObject jo;
            string image =null;
            try
            {
                jo = (JObject)JsonConvert.DeserializeObject(responseBody);
            }
            catch
            {
                ShowMes(string.Format("【{0}】 can not transition to JObject", responseBody));
                return null;
            }

            if (jo != null)
            {
                try
                {
                    image = jo["containers"][0]["image"].ToString() ;
                }
                catch (Exception ex)
                {
                    ShowMes(string.Format("get image fail \r\n{0}", ex.ToString()));
                    return null;
                }
            }
            return image;
        }


        //https://rancher.indata.cc/v3/project/c-fj8rb:p-6czkl/workloads/deployment:p-6czkl-pipeline:uitestforcrm?action=redeploy
        /// <summary>
        /// 
        /// </summary>
        /// <param name="project">project(eg:c-fj8rb:p-6czkl)</param>
        /// <param name="workloads">workloads(eg:deployment:p-6czkl-pipeline:uitestforcrm)</param>
        /// <returns></returns>
        public async Task<bool> RedeployPipeline(string project ,string workloads)
        {
            HttpResponseMessage response = await httpClient.PostAsync($"/v3/project/{project}/workloads/{workloads}?action=redeploy",
                new StringContent("{}",Encoding.UTF8, "application/json"));
            return (response.StatusCode == HttpStatusCode.OK);
        }
    }
}
