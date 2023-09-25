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
using System.Threading;

namespace MyDeploymentMonitor.DeploymentHelper
{
    public class KubeSphereV3DeploymentHelper : KubeSphereDeploymentHelper
    {

        private FormUrlEncodedContent oauthFormContent;
        private List<KeyValuePair<string, string>> refreshTokenParameters;
        private Dictionary<Task, CancellationTokenSource> AliveRefreshTokenTasks;

        Task<bool> refreshTokenTask;

        public KubeSphereV3DeploymentHelper(string kubeSphereBaseAddress, string user, string pwd):base(kubeSphereBaseAddress, user, pwd)
        {
            AliveRefreshTokenTasks = new Dictionary<Task, CancellationTokenSource>();
            oauthFormContent = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("grant_type","password"),
                        new KeyValuePair<string, string>("username",user),
                        new KeyValuePair<string, string>("password",pwd),
                        new KeyValuePair<string, string>("client_id","kubesphere"),
                        new KeyValuePair<string, string>("client_secret","kubesphere")
                    });
            refreshTokenParameters = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("grant_type","refresh_token"),
                new KeyValuePair<string, string>("client_id","kubesphere"),
                new KeyValuePair<string, string>("client_secret","kubesphere"),
                new KeyValuePair<string, string>("refresh_token","")
            };
        }

        public async Task Test()
        {
            await LoginKubeSphereAsync();
            await GetRunConfig("test-k8s-nodejs-pipeline6p424", "by-planet");
        }


        private void CancleRefreshTokenTasks()
        {
            foreach (var tempAliveTask in AliveRefreshTokenTasks)
            {
                if (tempAliveTask.Key.IsCompleted || tempAliveTask.Key.IsCanceled || tempAliveTask.Key.IsFaulted)
                {
                    AliveRefreshTokenTasks.Remove(tempAliveTask.Key);
                }
                else
                {
                    tempAliveTask.Value.Cancel();
                }
            }
        }

        private async Task<bool> RunRefreshTokenTask(int intervalTime, CancellationToken cancellationToken)
        {
            await Task.Delay(intervalTime * 1000);
            if (cancellationToken.IsCancellationRequested)
            {
                ShowMes("RunRefreshTokenTask cancle");
                //简单地从委托中返回。 在许多情况下，这样已足够；但是，采用这种方式取消的任务实例会转换为 TaskStatus.RanToCompletion 状态，而不是 TaskStatus.Canceled 状态。
                //引发 OperationCanceledException ，并将其传递到在其上请求了取消的标记。 完成此操作的首选方式是使用 ThrowIfCancellationRequested 方法。 采用这种方式取消的任务会转换为 Canceled 状态，调用代码可使用该状态来验证任务是否响应了其取消请求。
                //cancellationToken.ThrowIfCancellationRequested();
                return false;
            }
            return await RefreshKsTokenAsync();
        }

        private async ValueTask<bool> RefreshKsTokenAsync()
        {
            if(string.IsNullOrEmpty(kubeSphereRefreshToken))
            {
                ShowMes("kubeSphereRefreshToken is empty in [RefreshKsTokenAsync]");
                return false;
            }
            refreshTokenParameters.Remove(refreshTokenParameters.FirstOrDefault((kvp) => kvp.Key == "refresh_token"));
            refreshTokenParameters.Add(new KeyValuePair<string, string>("refresh_token", kubeSphereRefreshToken));
            return await LoginKubeSphereAsync(new FormUrlEncodedContent(refreshTokenParameters));
        }

        protected async Task<bool> LoginKubeSphereAsync(FormUrlEncodedContent formContent = null)
        {
            try
            {
                //清除已经完成的Task，结束没有完成Task（新的RefreshKsTokenAsync启动，其他Task没有必要了）
                //不会取消执行自己的Task，因为IsCancellationRequested在之前就结束了
                CancleRefreshTokenTasks();
                httpClient.DefaultRequestHeaders.Authorization = null;
                HttpResponseMessage response = await httpClient.PostAsync("oauth/token", formContent??oauthFormContent);
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
                //httpClient.DefaultRequestHeaders.Add("Authorization", string.Format("Bearer {0}", kubeSphereBearerToken));
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", kubeSphereBearerToken);
                kubeSphereRefreshToken = jo["refresh_token"]?.Value<string>();
                int delayTime = jo["expires_in"]?.Value<int>() ?? 0;
                if ((int)(delayTime * 0.8) > 10)
                {
                    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                    Task<bool> tempRefreshTokenTask = RunRefreshTokenTask((int)(delayTime * 0.8), cancellationTokenSource.Token);
                    AliveRefreshTokenTasks.Add(tempRefreshTokenTask, cancellationTokenSource);
                }
                else
                {
                    ShowMes("delayTime is too short , not start RefreshToken Task");
                }
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

        /// <summary>
        /// get the state of sevice
        /// </summary>
        /// <returns>is sevice running</returns>
        public override async Task<bool> GetSeviceStateAsync()
        {
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(string.Format("/kapis/tenant.kubesphere.io/v1alpha2/workspaces"));
                bool? result = await CheckAuthorizationStatus(response);
                if(result == null)
                {
                    ShowMes("ks not in service");
                }
                return result == true;
            }
            catch (Exception ex)
            {
                ShowMes(ex.Message);
            }
            return false;
        }


        /// <summary>
        /// 确认请求返回的鉴权信息
        /// </summary>
        /// <param name="response">response</param>
        /// <param name="tryLogin">鉴权错误后，是否尝试自动登录</param>
        /// <returns>true：成功 false：权限错误401/403 null：其他错误</returns>
        private async Task<bool?> CheckAuthorizationStatus(HttpResponseMessage response , bool tryLogin = true)
        {
            bool? result = null;
            if(response==null)
            {
                return result;
            }
            if (response.StatusCode == HttpStatusCode.OK)
            {
                result = true;
            }
            else if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
            {
                result = false;
                if (tryLogin)
                {
                    if (await LoginKubeSphereAsync())
                    {
                        result = true;
                    }
                    else
                    {
                        ShowMes("Unauthorized and can not login");
                    }
                }

            }
            return result;
        }

        /// <summary>
        /// 获取指定devop流水线列表（https://ks.-inc.com/kapis/clusters/kubeshere/devops.kubesphere.io/v1alpha3/devops/test-k8s-nodejs-pipeline6p424/pipelines?page=1&limit=10）
        /// </summary>
        /// <param name="yourDevop"></param>
        /// <param name="yourLimit"></param>
        /// <returns></returns>
        public override async Task<JObject> GetDevopPipelines(string yourDevop, int yourLimit)
        {
            if(!(await GetSeviceStateAsync()))
            {
                ShowMes("GetSeviceStateAsync fail in GetDevopPipelines");
                //throw new Exception("GetSeviceStateAsync fail");
                return null;
            }
            string getDevopPipelinesUrl = $"kapis/clusters/kubeshere/devops.kubesphere.io/v1alpha3/devops/{yourDevop}/pipelines?page=1&limit={yourLimit}";
            HttpResponseMessage response = await httpClient.GetAsync(getDevopPipelinesUrl);
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
            return jo;
        }

        /// <summary>
        /// 获取所有Devops列表
        /// </summary>
        /// <param name="yourDevop"></param>
        /// <param name="yourLimit"></param>
        /// <returns></returns>
        public override async Task<List<string>> GetDevops(int yourLimit)
        {
            string getDevopPipelinesUrl = string.Format($"kapis/tenant.kubesphere.io/v1alpha2/workspaces/-/devops");
            HttpResponseMessage response = await httpClient.GetAsync(getDevopPipelinesUrl);
            string responseBody = await response.Content.ReadAsStringAsync();
            if((await CheckAuthorizationStatus(response))!=true)
            {
                response = await httpClient.GetAsync(getDevopPipelinesUrl);
                responseBody = await response.Content.ReadAsStringAsync();
            }
            List<string> result = new List<string>();
            try
            {
                JObject jo = (JObject)JsonConvert.DeserializeObject(responseBody);
                JArray devopsJArray = jo?["items"] as JArray;
                if (devopsJArray != null && devopsJArray.Count > 0)
                {
                    foreach (JObject devop in devopsJArray)
                    {
                        result.Add(devop["metadata"]?["name"]?.Value<string>() ?? "error data");
                    }
                }
            }
            catch(Exception ex)
            {
                ShowMes($"【{responseBody}】 {ex.ToString()}" );
                return null;
            }
            return result;
        }

        /// <summary>
        /// 获取指定流水线默认发布parameters
        /// </summary>
        /// <param name="devop"></param>
        /// <param name="pipeline"></param>
        /// <returns></returns>
        private async Task<Dictionary<string, string>> GetRunConfig(string devop, string pipeline)
        {
            Dictionary<string, string> configDc = null;
            //https://ks.--inc.com/kapis/clusters/kubeshere/devops.kubesphere.io/v1alpha3/devops/test-k8s-nodejs-pipeline6p424/pipelines/by-planet
            HttpResponseMessage response = await httpClient.GetAsync($"kapis/clusters/kubeshere/devops.kubesphere.io/v1alpha3/devops/{devop}/pipelines/{pipeline}");
            string responseBody = await response.Content.ReadAsStringAsync();
            JObject jo;
            try
            {
                jo = (JObject)JsonConvert.DeserializeObject(responseBody);
            }
            catch
            {
                ShowMes(string.Format("【{0}】 can not transition to JObject in GetRunConfig", responseBody));
                return configDc;
            }
            string jenkinsfileStr = jo?["spec"]?["pipeline"]?["jenkinsfile"].Value<string>();
            if(string.IsNullOrEmpty(jenkinsfileStr))
            {
                ShowMes(string.Format("【{0}】 can not get jenkinsfile in GetRunConfig", responseBody));
                return configDc;
            }
            //https://ks.--inc.com/kapis/clusters/kubeshere/devops.kubesphere.io/v1alpha2/tojson
            response = await httpClient.PostAsync("kapis/clusters/kubeshere/devops.kubesphere.io/v1alpha2/tojson", new FormUrlEncodedContent(new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("jenkinsfile", jenkinsfileStr) }));
            responseBody = await response.Content.ReadAsStringAsync();
            jo = null;
            try
            {
                jo = (JObject)JsonConvert.DeserializeObject(responseBody);
            }
            catch
            {
                ShowMes(string.Format("【{0}】 can not transition to JObject in GetRunConfig", responseBody));
                return configDc;
            }
            JArray parametersJArray = jo?["data"]?["json"]?["pipeline"]?["parameters"]?["parameters"] as JArray;
            if(parametersJArray!=null && parametersJArray.Count>0)
            {
                configDc = new Dictionary<string, string>();
                foreach (JObject tempParameter in parametersJArray)
                {
                    string tempName = null;
                    string tempDefaultValue = null;
                    if(tempParameter["arguments"]==null)
                    {
                        continue;
                    }
                    foreach (JObject tempParameterVaule in (JArray)tempParameter["arguments"])
                    {
                        if(tempParameterVaule["key"].Value<string>() == "name")
                        {
                            tempName = tempParameterVaule["value"]?["value"].Value<string>();
                        }
                        else if (tempParameterVaule["key"].Value<string>() == "defaultValue")
                        {
                            tempDefaultValue = tempParameterVaule["value"]?["value"].Value<string>();
                        }
                    }
                    if (!string.IsNullOrEmpty(tempName) && !string.IsNullOrEmpty(tempDefaultValue))
                    {
                        configDc.TryAdd(tempName, tempDefaultValue);
                    }
                }
            }
            return configDc;
        }

        /// <summary>
        /// 取消指定的任务的发布（使用基类中V2的API）
        /// </summary>
        /// <param name="devop"></param>
        /// <param name="pipeline"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public override async Task<bool> CanceltRunningRunsAsync(string devop, string pipeline, int? id = null)
        {
            if (await GetSeviceStateAsync())
            {
                return await base.CanceltRunningRunsAsync(devop, pipeline,id);
            }
            else
            {
                ShowMes("GetSeviceStateAsync fial in GetRunningWorkflowAsync");
                return false;
            }
        }

        /// <summary>
        /// 触发构建（使用V2的API）
        /// </summary>
        /// <param name="devop"></param>
        /// <param name="pipeline"></param>
        /// <param name="configDc"></param>
        /// <returns></returns>
        public override async Task<Tuple<string, string, Dictionary<string, string>>> StartDeployAsync(string devop, string pipeline, Dictionary<string, string> configDc = null)
        {
            if (!await GetSeviceStateAsync())
            {
                return new Tuple<string, string, Dictionary<string, string>>(null, "ks not in service", null);
            }

            Dictionary<string, string> runConfigDc = await GetRunConfig(devop, pipeline);
            if (runConfigDc == null)
            {
                ShowMes("GetRunConfig empty");
                runConfigDc = new Dictionary<string, string>();
                //return new Tuple<string, string, Dictionary<string, string>>(null, "can not GetRunConfig", null);
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

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, string.Format(@"kapis/devops.kubesphere.io/v1alpha2/devops/{0}/pipelines/{1}/runs", devop, pipeline));
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
            return new Tuple<string, string, Dictionary<string, string>>(runId, $"https://ks.--inc.com/byai/clusters/kubeshere/devops/{devop}/pipelines/{pipeline}/activity", runConfigDc);
        }
    }
}
