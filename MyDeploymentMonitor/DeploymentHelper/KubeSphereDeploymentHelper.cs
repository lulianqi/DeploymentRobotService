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

namespace MyDeploymentMonitor.DeploymentHelper
{
    public class KubeSphereDeploymentHelper
    {
        protected string useName = "";
        protected string password = "";
        protected CookieContainer cookieContainer = new CookieContainer();
        protected HttpClientHandler handler;
        protected HttpClient httpClient;
        protected string kubeSphereBearerToken;
        protected string kubeSphereRefreshToken;


        public KubeSphereDeploymentHelper(string kubeSphereBaseAddress, string user, string pwd)
        {
            useName = user;
            password = pwd;
            handler = new HttpClientHandler() { UseDefaultCredentials = true, CookieContainer = cookieContainer };
            httpClient = new HttpClient(handler) { BaseAddress = new Uri(kubeSphereBaseAddress) };
            //httpClient.DefaultRequestHeaders.Add("Authorization", string.Format("Bearer {0}", kubeSphereBearerToken));
        }

        protected void ShowMes(string mes)
        {
            Console.WriteLine(mes);
        }

        protected long NowTimeStamp { get { return (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000; } }

        protected async Task<bool> LoginKubeSphereAsync()
        {
            try
            {
                HttpResponseMessage response = await httpClient.PostAsync("kapis/iam.kubesphere.io/v1alpha2/login", new StringContent(string.Format(@"{{""username"":""{0}"", ""password"":""{1}""}}", useName, password), Encoding.UTF8, @"application/json"));
                string responseBody = await response.Content.ReadAsStringAsync();
                JObject jo = null;
                try
                {
                    jo = (JObject)JsonConvert.DeserializeObject(responseBody);
                }
                catch
                {
                    ShowMes(string.Format("【{0}】 can not transition to JObject", responseBody));
                    return false;
                }
                if (jo == null || jo["access_token"] == null)
                {
                    ShowMes(string.Format("can not get access_token in 【{0}】", responseBody));
                    return false;
                }
                string access_token = jo["access_token"].Value<string>();
                kubeSphereBearerToken = access_token;
                cookieContainer.SetCookies(new Uri(httpClient.BaseAddress.ToString()), string.Format("token={0}", kubeSphereBearerToken));
            }
            catch (HttpRequestException e)
            {
                ShowMes(e.Message);
                return false;
            }
            catch (Exception e)
            {
                ShowMes(e.Message);
                return false;
            }
            return true;
        }

        public virtual async Task<JObject> GetDevopPipelines(string yourDevop ,int yourLimit)
        {
            string getDevopPipelinesUrl = string.Format(@"kapis/devops.kubesphere.io/v1alpha2/search?q=type:pipeline;organization:jenkins;pipeline:{0}/*;excludedFromFlattening:jenkins.branch.MultiBranchProject,hudson.matrix.MatrixProject&filter=no-folders&start=0&limit={1}", yourDevop, yourLimit);
            HttpResponseMessage response = await httpClient.GetAsync(getDevopPipelinesUrl);
            string responseBody = await response.Content.ReadAsStringAsync();
            if (responseBody.Contains("401 Unauthorized") || response.StatusCode== HttpStatusCode.Unauthorized || response.StatusCode== HttpStatusCode.Forbidden)
            {
                if (!await LoginKubeSphereAsync())
                {
                    ShowMes("Unauthorized and can not login");
                    return null;
                }
                response = await httpClient.GetAsync(getDevopPipelinesUrl);
                responseBody = await response.Content.ReadAsStringAsync();
            }
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
            return jo;
        }

        public virtual async Task<List<string>> GetDevops(int yourLimit)
        {
            string getDevopPipelinesUrl = string.Format($"kapis/tenant.kubesphere.io/v1alpha2/workspaces/-/devops?paging=limit%3D{yourLimit}%2Cpage%3D1");
            HttpResponseMessage response = await httpClient.GetAsync(getDevopPipelinesUrl);
            string responseBody = await response.Content.ReadAsStringAsync();
            if (responseBody.Contains("401 Unauthorized") || response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
            {
                if (!await LoginKubeSphereAsync())
                {
                    ShowMes("Unauthorized and can not login");
                    return null;
                }
                response = await httpClient.GetAsync(getDevopPipelinesUrl);
                responseBody = await response.Content.ReadAsStringAsync();
            }
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
            List<string> result = new List<string>();
            JArray devopsJArray = jo?["items"] as JArray;
            if (devopsJArray != null && devopsJArray.Count > 0)
            {
                foreach (JObject devop in devopsJArray)
                {
                    result.Add(devop["project_id"]?.Value<string>()??"error data");
                }
            }
            return result;
        }

        /// <summary>
        /// get the state of sevice
        /// </summary>
        /// <returns>is sevice running</returns>
        public virtual async Task<bool> GetSeviceStateAsync()
        {
            //使用 https://host/kapis/tenant.kubesphere.io/v1alpha2/workspaces 可以检查登录状态, https://host/dashboard 302到登录
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(string.Format("/kapis/tenant.kubesphere.io/v1alpha2/workspaces"));
                if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                {
                    if (!await LoginKubeSphereAsync())
                    {
                        ShowMes("Unauthorized and can not login");
                        return false;
                    }
                }
                else if (response.StatusCode != HttpStatusCode.OK)
                {
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                ShowMes(ex.Message);
            }
            return false;
        }

        private async Task<KeyValuePair<string, string>> GetCrumb()
        {
            KeyValuePair<string, string> crumbKp = new KeyValuePair<string, string>();
            HttpResponseMessage response = await httpClient.GetAsync(@"kapis/devops.kubesphere.io/v1alpha2/crumbissuer");
            string responseBody = await response.Content.ReadAsStringAsync();
            if (responseBody.Contains("401 Unauthorized"))
            {
                if (!await LoginKubeSphereAsync())
                {
                    ShowMes("Unauthorized and can not login");
                    return crumbKp;
                }
                response = await httpClient.GetAsync(@"kapis/devops.kubesphere.io/v1alpha2/crumbissuer");
                responseBody = await response.Content.ReadAsStringAsync();
            }
            JObject jo;
            try
            {
                jo = (JObject)JsonConvert.DeserializeObject(responseBody);
            }
            catch
            {
                ShowMes(string.Format("【{0}】 can not transition to JObject", responseBody));
                return crumbKp;
            }
            if (jo != null)
            {
                crumbKp = new KeyValuePair<string, string>(jo.Value<string>("crumbRequestField"), jo.Value<string>("crumb"));
            }
            return crumbKp;
        }

        private async Task<Dictionary<string, string>> GetRunConfig(string devop, string pipeline)
        {
            Dictionary<string, string> configDc = null;
            HttpResponseMessage response = await httpClient.GetAsync(string.Format(@"kapis/devops.kubesphere.io/v1alpha2/devops/{0}/pipelines/{1}/config", devop, pipeline));
            string responseBody = await response.Content.ReadAsStringAsync();
            if (responseBody.Contains("401 Unauthorized"))
            {
                if (!await LoginKubeSphereAsync())
                {
                    ShowMes("Unauthorized and can not login");
                    return configDc;
                }
                response = await httpClient.GetAsync(string.Format(@"kapis/devops.kubesphere.io/v1alpha2/devops/{0}/pipelines/{1}/config", devop, pipeline));
                responseBody = await response.Content.ReadAsStringAsync();
            }
            JObject jo;
            try
            {
                jo = (JObject)JsonConvert.DeserializeObject(responseBody);
            }
            catch
            {
                ShowMes(string.Format("【{0}】 can not transition to JObject", responseBody));
                return configDc;
            }
            if (jo != null && jo["pipeline"] != null && jo["pipeline"]["parameters"] != null && jo["pipeline"]["parameters"] is JArray)
            {
                configDc = new Dictionary<string, string>();
                foreach (JObject tempParameter in (JArray)jo["pipeline"]["parameters"])
                {
                    configDc.Add(tempParameter.Value<string>("name"), tempParameter.Value<string>("default_value"));
                }
            }
            return configDc;
        }

        //http://k8s.indata.cc/kapis/devops.kubesphere.io/v1alpha2/devops/project-5x3rzPzOq0kX/pipelines/ai-crm/runs/?start=0&limit=30
        private async Task<List<int>> GetRunningWorkflowAsync(string devop, string pipeline)
        {
            List<int> workflow = new List<int>();
            HttpResponseMessage response = await httpClient.GetAsync(string.Format(@"kapis/devops.kubesphere.io/v1alpha2/devops/{0}/pipelines/{1}/runs/?start=0&limit=20", devop, pipeline));
            string responseBody = await response.Content.ReadAsStringAsync();
            if (responseBody.Contains("401 Unauthorized"))
            {
                if (!await LoginKubeSphereAsync())
                {
                    ShowMes("Unauthorized and can not login");
                    return null;
                }
                response = await httpClient.GetAsync(string.Format(@"kapis/devops.kubesphere.io/v1alpha2/devops/{0}/pipelines/{1}/runs/?start=0&limit=20", devop, pipeline));
                responseBody = await response.Content.ReadAsStringAsync();
            }
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
            if (jo != null && jo["items"] != null &&  jo["items"] is JArray)
            {
                foreach (JObject item in (JArray)jo["items"])
                {
                    if (item.Value<string>("state") == "RUNNING" || item.Value<string>("state") == "QUEUED")
                    {
                        workflow.Add(item.Value<int>("id"));
                    }
                }
            }
            return workflow;
        }

        //http://k8s.indata.cc/kapis/devops.kubesphere.io/v1alpha2/devops/project-5x3rzPzOq0kX/pipelines/ai-crm/runs/733/stop/?blocking=true&timeOutInSecs=10
        private async Task<bool> CanceltDeployAsync(string devop, string pipeline , int id)
        {
            return (await httpClient.PostAsync(string.Format(@"kapis/devops.kubesphere.io/v1alpha2/devops/{0}/pipelines/{1}/runs/{2}/stop/?blocking=true&timeOutInSecs=10", devop, pipeline, id), new StringContent("{}", Encoding.UTF8, @"application/json"))).StatusCode == HttpStatusCode.OK;
        }

        
        public virtual async Task<bool> CanceltRunningRunsAsync(string devop, string pipeline , int? id =null)
        {
            List<int> workflow = await GetRunningWorkflowAsync(devop , pipeline);
            if (id != null)
            {
                if (workflow.Contains((int)id))
                {
                    _ = CanceltDeployAsync(devop, pipeline, (int)id).ContinueWith((result) => { if (!result.Result) { _ = CanceltDeployAsync(devop, pipeline, (int)id); } });
                    return true;
                }
            }
            else
            {
                if (workflow != null && workflow.Count > 0)
                {
                    foreach (var runId in workflow)
                    {
                        if (runId > 0)
                        {
                            _ = CanceltDeployAsync(devop, pipeline, runId).ContinueWith((result) => { if (!result.Result) { _ = CanceltDeployAsync(devop, pipeline, runId); } });
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="devop"></param>
        /// <param name="pipeline"></param>
        /// <param name="configDc"></param>
        /// <returns>item1:runId item2:message item3: run config</returns>
        public virtual async Task<Tuple<string, string, Dictionary<string, string>>> StartDeployAsync(string devop, string pipeline, Dictionary<string, string> configDc = null)
        {
            KeyValuePair<string, string> crumb = await GetCrumb();
            if (crumb.Key == null || crumb.Value == null)
            {
                ShowMes("can not GetCrumb");
                return new Tuple<string, string, Dictionary<string, string>>(null, "can not GetCrumb",null);
            }

            Dictionary<string, string> runConfigDc = await GetRunConfig(devop, pipeline);
            if (runConfigDc == null)
            {
                ShowMes("GetRunConfig empty");
                runConfigDc = new Dictionary<string, string>();
                //return new Tuple<string, string, Dictionary<string, string>>(null, "can not GetRunConfig",null);
            }
            if (configDc != null && configDc.Count > 0)
            {
                foreach (var tempConfig in configDc)
                {
                    if (runConfigDc.ContainsKey(tempConfig.Key))
                    {
                        runConfigDc[tempConfig.Key] = tempConfig.Value;
                    }
                }
            }

            Dictionary<string, string> parametersDc = new Dictionary<string, string>();
            JObject runBody = new JObject();
            JArray jArray = new JArray();
            foreach (var par in runConfigDc)
            {
                string tempKey = par.Key;
                string tempValue = par.Value.Contains('\n') ? par.Value.Substring(0, par.Value.IndexOf('\n')) : par.Value;
                parametersDc.Add(tempKey, tempValue);
                JObject tempPar = new JObject();
                tempPar.Add("name", tempKey);
                tempPar.Add("value", tempValue);
                jArray.Add(tempPar);
            }
            runConfigDc = parametersDc;
            runBody.Add("parameters", jArray);
            string runBodyStr = runBody.ToString(Newtonsoft.Json.Formatting.None, null);

            //HttpResponseMessage response = await httpClient.PostAsync(string.Format(@"kapis/devops.kubesphere.io/v1alpha2/devops/{0}/pipelines/{1}/runs", devop, pipeline), new StringContent(runBodyStr, Encoding.UTF8, @"application/json"));
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, string.Format(@"kapis/devops.kubesphere.io/v1alpha2/devops/{0}/pipelines/{1}/runs", devop, pipeline));
            request.Headers.Add(crumb.Key, crumb.Value);
            request.Content = new StringContent(runBodyStr, Encoding.UTF8, @"application/json");
            HttpResponseMessage response = await httpClient.SendAsync(request);

            string responseBody = await response.Content.ReadAsStringAsync();
            JObject jo = null;
            try
            {
                jo = (JObject)JsonConvert.DeserializeObject(responseBody);
            }
            catch
            {
                ShowMes(string.Format("【{0}】 can not transition to JObject", responseBody));
                return new Tuple<string, string, Dictionary<string, string>>(null, "transition to JObject fail", null);
            }
            if (jo == null || jo["id"] == null)
            {
                ShowMes(string.Format("can not get run id in 【{0}】", responseBody));
                return new Tuple<string, string, Dictionary<string, string>>(null, "get run id fail", null);
            }
            string runId = jo["id"].Value<string>();
            return new Tuple<string, string, Dictionary<string, string>>(runId, string.Format(@"{0}devops/{1}/pipelines/{2}/run/{3}/task-status", httpClient.BaseAddress.ToString(), devop, pipeline, runId) , runConfigDc);
        }

        public async Task<string> GetDeployResultAsync(string devop, string pipeline, string runId)
        {
            string result = null;
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(string.Format("devops.kubesphere.io/v1alpha2/devops/{0}/pipelines/{1}/runs/{2}/", devop, pipeline, runId));
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                JObject jo = null;
                jo = (JObject)JsonConvert.DeserializeObject(responseBody, new JsonSerializerSettings { DateTimeZoneHandling = DateTimeZoneHandling.Local });
                if (jo != null && jo["result"] != null)
                {
                    result = jo.Value<string>("result");
                }
                else
                {
                    result = "exception";
                }
            }
            catch (HttpRequestException e)
            {
                result = "exception";
                ShowMes(e.Message);
            }
            catch (Exception e)
            {
                result = "exception";
                ShowMes(string.Format(" can not get  with deploymentState", e.Message));
            }
            return result;
        }

        public async Task<CommitInfo> GetGitCommitInfoAsync(string devop, string pipeline, string runId)
        {
            CommitInfo commitInfo = new CommitInfo() { BuildResult = "exception" };
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(string.Format("kapis/devops.kubesphere.io/v1alpha2/devops/{0}/pipelines/{1}/runs/{2}/", devop, pipeline, runId));
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                JObject jo = null;
                jo = (JObject)JsonConvert.DeserializeObject(responseBody, new JsonSerializerSettings { DateTimeZoneHandling = DateTimeZoneHandling.Local });

                //UNKNOWN,SUCCESS,FAILURE,ABORTED
                if (jo != null && jo["result"] != null)
                {
                    commitInfo.BuildResult = jo.Value<string>("result");
                }
                //QUEUED,RUNNING,FINISHED
                if (jo != null && jo["state"] != null)
                {
                    commitInfo.BuildState = jo.Value<string>("state");
                }

                if (jo != null && jo["changeSet"] != null && jo["changeSet"] is JArray)
                {
                    commitInfo.CommitList = new List<KeyValuePair<string, string>>();
                    foreach (JObject tempChange in ((JArray)jo["changeSet"]))
                    {
                        string tempUser = tempChange["author"] != null ? tempChange["author"].Value<string>("fullName") : "unknow";
                        string tempMsg = tempChange.Value<string>("msg");
                        commitInfo.CommitList.Add(new KeyValuePair<string, string>(tempUser, tempMsg));
                    }

                }
                else
                {
                    ShowMes("can not find changeSet with GetGitCommitInfoAsync");
                }
            }
            catch (HttpRequestException e)
            {
                if(e.Message.Contains("401"))
                {
                    await LoginKubeSphereAsync();
                }
                ShowMes(e.Message);
            }
            catch (Exception e)
            {
                ShowMes(string.Format(" can not GetGitCommitInfoAsync {0}", e.Message));
            }
            return commitInfo;
        }

        public async Task<CommitInfo> GetRecentFailCommitAsync(string devop, string pipeline, string runId)
        {
            int nowId;
            CommitInfo resultCommitInfo = null;
            if (!int.TryParse(runId, out nowId))
            {
                ShowMes("runId is not ID type");
                return null;
            }
            List<CommitInfo> commitList = new List<CommitInfo>();
            for (int i = nowId - 1; i > ((nowId - 6 > 0) ? nowId - 6 : 0); i--)
            {
                CommitInfo tempCommitInfo = await GetGitCommitInfoAsync(devop, pipeline, i.ToString());
                if (tempCommitInfo.BuildResult == "SUCCESS")
                {
                    break;
                }
                if (tempCommitInfo.BuildResult == "RUNNING")
                {
                    break;
                }
                if (tempCommitInfo.BuildResult == "exception")
                {
                    continue;
                }
                commitList.Add(tempCommitInfo);
            }
            if (commitList.Count > 0)
            {
                resultCommitInfo = new CommitInfo() { CommitList = new List<KeyValuePair<string, string>>() };
                foreach (var tempCommitInfo in commitList)
                {
                    if (tempCommitInfo.CommitList != null && tempCommitInfo.CommitList.Count > 0)
                    {
                        foreach (var tempSingleCommit in tempCommitInfo.CommitList)
                        {
                            resultCommitInfo.CommitList.Add(tempSingleCommit);
                        }
                    }
                }
            }
            return resultCommitInfo;
        }

        public async Task<string> GetDeployErrorMessageAsync(string devop, string pipeline, string runId)
        {
            HttpResponseMessage response = await httpClient.GetAsync(string.Format(@"kapis/devops.kubesphere.io/v1alpha2/devops/{0}/pipelines/{1}/runs/{2}/log/?start=0", devop, pipeline, runId));
            string responseBody = response.StatusCode == HttpStatusCode.OK ? await response.Content.ReadAsStringAsync() : "GetDeployErrorMessageAsync fail";
            //if (responseBody.Contains("[ERROR]"))
            //{
            //    return responseBody.Substring(responseBody.IndexOf("[ERROR]"));
            //}
            return responseBody;
        }
    }
}
