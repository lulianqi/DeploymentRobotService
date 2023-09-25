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
using MyDeploymentMonitor.DeploymentHelper.DataHelper;

namespace MyDeploymentMonitor.DeploymentHelper
{
    public class BambooDeploymentHelper
    {
        private string baseUrl = "";
        private string useName = "";
        private string password = "";
        private CookieContainer cookieContainer = new CookieContainer();
        private HttpClientHandler handler;
        private HttpClient httpClient;

        private String atl_token = "";
        public String BuildKey { get; set; } = "BUSCARD-TEST16";

        public BambooDeploymentHelper(string user, string pwd, string url)
        {
            baseUrl = url;
            useName = user;
            password = pwd;
            handler = new HttpClientHandler() { UseDefaultCredentials = true, CookieContainer = cookieContainer };
            httpClient = new HttpClient(handler) { BaseAddress = new Uri(baseUrl) };   //new HttpClient(new HttpClientHandler { UseCookies = false }); 设置不使用CookieContainer  就可以直接在header里面.Add("Cookie", cookie);了
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            httpClient.DefaultRequestHeaders.Add("Tester", "fuxiao_robot");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json, text/javascript, */*; q=0.01");
            CookieCollection cookieCollection = new CookieCollection();
            cookieCollection.Add(new Cookie("atl.xsrf.token", "58bf6d42f3a8cb28a70be197a6b23ef00d36c19f"));
            cookieCollection.Add(new Cookie("bamboo.dash.display.toggles", "buildQueueActions-actions-queueControl"));
            cookieCollection.Add(new Cookie("JSESSIONID", "2E203BD1505651CB2EF600086554EB82"));
            cookieCollection.Add(new Cookie("seraph.bamboo", "72417371%3A8cf2dd83a144e5e161664929770c8b987661f399"));
            cookieCollection.Add(new Cookie("atlassian.bamboo.dashboard.tab.selected", "myTab"));
            cookieCollection.Add(new Cookie("BAMBOO-BUILD-FILTER", "LAST_25_BUILDS"));
            cookieCollection.Add(new Cookie("BAMBOO-MAX-DISPLAY-LINES", "25"));

            cookieContainer.Add(new Uri(baseUrl), cookieCollection);
        }

        private void ShowMes(string mes ,Exception ex=null)
        {
            Console.WriteLine(mes);
            if(ex!=null)
            {
                Console.WriteLine(ex.ToString());
            }

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
                HttpResponseMessage response = await httpClient.GetAsync(string.Format("rest/api/latest/server?_={0} ", NowTimeStamp));
                if(response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (await LoginBambooAsync())
                    {
                        response = await httpClient.GetAsync(string.Format("rest/api/latest/server?_={0} ", NowTimeStamp));
                    }
                }
                if (JsonConvert.DeserializeObject<JObject>(await response.Content.ReadAsStringAsync())["state"].Value<string>() == "RUNNING")
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                ShowMes(ex.Message);
            }
            return false;
        }

        /// <summary>
        /// login your Bamboo
        /// </summary>
        /// <param name="yourName">user name</param>
        /// <param name="yourPwd">password</param>
        /// <returns>is login succeed</returns>
        public async Task<bool> LoginBambooAsync(string yourName = null, string yourPwd = null)
        {
            if (string.IsNullOrEmpty(yourName)) yourName = useName;
            if (string.IsNullOrEmpty(yourPwd)) yourPwd = password;

            atl_token = string.Format("00000000{0}", Guid.NewGuid().ToString("N"));
            cookieContainer.SetCookies(new Uri(baseUrl), string.Format("atl.xsrf.token={0}", atl_token));

            try
            {
                HttpResponseMessage response = await httpClient.PostAsync("/userlogin.action ", new FormUrlEncodedContent(new List<KeyValuePair<string, string>>(){
                new KeyValuePair<string, string>("os_destination", "/start.action"),
                new KeyValuePair<string, string>("os_username", yourName),
                new KeyValuePair<string, string>("os_password", yourPwd),
                new KeyValuePair<string, string>("os_cookie", "true"),
                new KeyValuePair<string, string>("checkBoxFields", "os_cookie"),
                new KeyValuePair<string, string>("save", "Log in"),
                new KeyValuePair<string, string>("atl_token", atl_token)
                }));
                response.EnsureSuccessStatusCode();
                if (response.RequestMessage.RequestUri.Segments[1] != "allPlans.action")
                {
                    ShowMes("LoginBamboo fail");
                    return false;
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

        public async Task<BambooProjectInfo> GetBambooProjectInfo(string yourBuildKey ,bool isRetry = false)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            BambooProjectInfo bambooProjectInfo = new BambooProjectInfo();
            try
            {
                response = await httpClient.GetAsync(string.Format("rest/api/latest/search/branches?includeMasterBranch=true&masterPlanKey={0}&start-index=0&max-results=1&_={1} ", yourBuildKey, NowTimeStamp));
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                JObject jo = null;
                try
                {
                    jo = (JObject)JsonConvert.DeserializeObject(responseBody);
                }
                catch
                {
                    ShowMes(string.Format("【{0}】 can not transition to JObject", responseBody));
                }
                bambooProjectInfo.id = jo["searchResults"][0]["searchEntity"]["id"]?.ToString();
                bambooProjectInfo.enabled = jo["searchResults"][0]["searchEntity"].Value<bool>("enabled");
                bambooProjectInfo.projectName = jo["searchResults"][0]["searchEntity"]["projectName"]?.ToString();
                bambooProjectInfo.planName = jo["searchResults"][0]["searchEntity"]["planName"]?.ToString();
                bambooProjectInfo.branchName = jo["searchResults"][0]["searchEntity"]["branchName"]?.ToString();
                bambooProjectInfo.description = jo["searchResults"][0]["searchEntity"]["description"]?.ToString();
                return bambooProjectInfo;
            }
            catch(HttpRequestException ex)
            {
                if((int)response.StatusCode<500 && isRetry == false)
                {
                    if(await LoginBambooAsync())
                    {
                        return await GetBambooProjectInfo(yourBuildKey, true);
                    }
                }
                ShowMes(ex.Message);
            }
            catch(Exception ex)
            {
                ShowMes(ex.Message);
            }
            return default;
        }

        public async Task<List<BambooProjectInfo>> GetBambooProjectInfos(string yourBuildKey,int maxCount, bool isRetry = false)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            List<BambooProjectInfo> bambooProjectInfoList = new List<BambooProjectInfo>();
            try
            {
                response = await httpClient.GetAsync(string.Format("rest/api/latest/search/branches?includeMasterBranch=true&masterPlanKey={0}&start-index=0&max-results={1}&_={2} ", yourBuildKey, maxCount, NowTimeStamp));
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                JObject jo = null;
                try
                {
                    jo = (JObject)JsonConvert.DeserializeObject(responseBody);
                }
                catch
                {
                    ShowMes(string.Format("【{0}】 can not transition to JObject", responseBody));
                }
                if (jo["searchResults"] is JArray)
                {
                    //bambooProjectInfo.id = jo["searchResults"][0]["searchEntity"]["id"]?.ToString();
                    //bambooProjectInfo.projectName = jo["searchResults"][0]["searchEntity"]["projectName"]?.ToString();
                    //bambooProjectInfo.planName = jo["searchResults"][0]["searchEntity"]["planName"]?.ToString();
                    //bambooProjectInfo.branchName = jo["searchResults"][0]["searchEntity"]["branchName"]?.ToString();
                    //bambooProjectInfo.description = jo["searchResults"][0]["searchEntity"]["description"]?.ToString();
                    foreach(JObject tempJProject in (JArray)jo["searchResults"])
                    {
                        bambooProjectInfoList.Add(new BambooProjectInfo()
                        {
                            id = tempJProject["searchEntity"]["id"]?.ToString(),
                            enabled = tempJProject["searchEntity"].Value<bool>("enabled"),
                            projectName = tempJProject["searchEntity"]["projectName"]?.ToString(),
                            planName = tempJProject["searchEntity"]["planName"]?.ToString(),
                            branchName = tempJProject["searchEntity"]["branchName"]?.ToString(),
                            description = tempJProject["searchEntity"]["description"]?.ToString()
                        }); ;
                    }
                }
                return bambooProjectInfoList;
            }
            catch (HttpRequestException ex)
            {
                if ((int)response.StatusCode < 500 && isRetry == false)
                {
                    if (await LoginBambooAsync())
                    {
                        return await GetBambooProjectInfos(yourBuildKey, maxCount, true);
                    }
                }
                ShowMes(ex.Message);
            }
            catch (Exception ex)
            {
                ShowMes(ex.Message);
            }
            return default;
        }

        public async Task<List<BambooProjectInfo>> GetAllBambooProjectList(int maxCount = 1000)
        {
            List<BambooProjectInfo> projectListResult = new List<BambooProjectInfo>();
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                response = await httpClient.PostAsync((@"allPlansSnippet.action"), new FormUrlEncodedContent(new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("decorator", "nothing"), new KeyValuePair<string, string>("confirm", "true"), new KeyValuePair<string, string>("pageSize", maxCount.ToString()) }));
                response.EnsureSuccessStatusCode();
                HtmlAgilityPack.HtmlDocument htmlDocument = new HtmlAgilityPack.HtmlDocument();
                //htmlDocument.LoadHtml(await response.Content.ReadAsStringAsync());
                htmlDocument.Load(await response.Content.ReadAsStreamAsync());
                HtmlNode htmlNode = htmlDocument.DocumentNode;
                ////*[@id="dashboard"]
                HtmlNodeCollection projectNodes = htmlNode.SelectNodes(@"//*[@id=""dashboard""]");
                if (projectNodes != null && projectNodes.Count > 0)
                {
                    var trNodes = projectNodes[0].ChildNodes.Where<HtmlNode>((node) => node.Name == "tbody" && node.GetAttributeValue("class", null) == "project");
                    if (trNodes.Any<HtmlNode>())
                    {
                        //Project
                        foreach (HtmlNode tempNode in trNodes)
                        {
                            var tempInnerProjectNodes = tempNode.ChildNodes.Where<HtmlNode>((node) => node.Name == "tr");
                            if (tempInnerProjectNodes != null)
                            {
                                //Plan
                                foreach (HtmlNode nowTempTdNode in tempInnerProjectNodes)
                                {
                                    HtmlNode tempTdBuildNode = nowTempTdNode.ChildNodes.FirstOrDefault<HtmlNode> ((node) => node.GetAttributeValue("class", null) == "build");
                                    if(tempTdBuildNode!=null && tempTdBuildNode.ChildNodes!=null&& tempTdBuildNode.ChildNodes.Count>0)
                                    {
                                        HtmlNode tempTdABuildNode= tempTdBuildNode.ChildNodes.FirstOrDefault<HtmlNode>((node) => node.Name=="a" &&  node.GetAttributeValue("href", null) != null);
                                        string tempBuildHref = tempTdABuildNode.GetAttributeValue("href", null);
                                        if (string.IsNullOrEmpty(tempBuildHref) || !tempBuildHref.StartsWith("/browse/"))
                                        {
                                            ShowMes(string.Format("find error build key {0}", tempBuildHref ?? "null"));
                                            continue;
                                        }
                                        string tempMasterBuildKey = tempBuildHref.Remove(0, 8);// Remove"/browse/"
                                        List<BambooProjectInfo> tempBambooProjectInfoList = await GetBambooProjectInfos(tempMasterBuildKey, 20);
                                        if (tempBambooProjectInfoList==null || tempBambooProjectInfoList.Count==0)
                                        {
                                            ShowMes(string.Format("GetBambooProjectInfos fail with build key {0}", tempBuildHref));
                                            continue;
                                        }
                                        ShowMes(string.Format("GetBambooProjectInfos complete with build key {0}", tempBuildHref));
                                        projectListResult.AddRange(tempBambooProjectInfoList);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (HttpRequestException e)
            {
                ShowMes(e.Message,e);
                return null;
            }
            catch (Exception e)
            {
                ShowMes(e.Message,e);
                return null;
            }
            return projectListResult;
        }

        public async Task<string> GetBambooScriptEnvironmentVariable(string plankey)
        {
            if(string.IsNullOrEmpty(plankey))
            {
                return null;
            }
            while(plankey[plankey.Length-1]>=0x30 && plankey[plankey.Length - 1]<=0x39)
            {
                plankey = plankey.Remove(plankey.Length - 1);
                if (plankey == "") return null;
            }
            


            string variable = null;
            string[] taskIds = new string[] { "3", "1", "2", "4", "5" };
            foreach(var taskId in taskIds)
            {
                variable = await GetBambooScriptEnvironmentVariable(plankey, taskId);
                if (variable != null) break;
            }
            if(variable!=null && variable.Contains("&quot;"))
            {
                string appName = null;
                string tag = null;
                int startIndex = variable.IndexOf("&quot;");
                int endIndex = variable.IndexOf("&quot;", startIndex+6);
                if(startIndex>0&& endIndex> startIndex)
                {
                     appName = variable.Substring(startIndex+6, endIndex - startIndex -6);
                }
                startIndex = variable.IndexOf("&quot;", endIndex+6);
                endIndex = variable.IndexOf("&quot;", startIndex + 6);
                if (startIndex > 0 && endIndex > startIndex)
                {
                     tag = variable.Substring(startIndex+6, endIndex - startIndex -6);
                }
                if(!string.IsNullOrEmpty(appName)&& !string.IsNullOrEmpty(tag))
                {
                    return string.Format("{0}:{1}", appName,tag);
                }
            }
            return variable;
        }

        public async Task<string> GetBambooScriptEnvironmentVariable(string plankey ,string taskId)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            string variable = null;
            try
            {
                response = await httpClient.GetAsync(string.Format(@"build/admin/edit/editTask.action?planKey={0}-JOB1&taskId={1}&decorator=nothing&confirm=true&_={2}", plankey, taskId, NowTimeStamp));
                response.EnsureSuccessStatusCode();
                HtmlAgilityPack.HtmlDocument htmlDocument = new HtmlAgilityPack.HtmlDocument();
                //htmlDocument.LoadHtml(await response.Content.ReadAsStringAsync());
                htmlDocument.Load(await response.Content.ReadAsStreamAsync());
                HtmlNode htmlNode = htmlDocument.DocumentNode;
                ////*[@id="dashboard"]
                HtmlNodeCollection environmentVariablesNodes = htmlNode.SelectNodes(@"//*[@id=""environmentVariables""]");
                if(environmentVariablesNodes!=null&& environmentVariablesNodes.Count>0)
                {
                     variable =environmentVariablesNodes[0].GetAttributeValue("value", null);
                }
            }
            catch (HttpRequestException e)
            {
                ShowMes(e.Message,e);
            }
            catch (Exception e)
            {
                ShowMes(e.Message, e);
            }
            return variable;
        }


        /// <summary>
        /// Build your project by buildKey
        /// </summary>
        /// <param name="yourBuildKey">buildKey</param>
        /// <returns>buildNum null is fail</returns>
        public async Task<KeyValuePair<string, string>> TriggerManualBuildAsync(string yourBuildKey = null)
        {
            yourBuildKey = yourBuildKey ?? BuildKey;
            string buildNum = null;
            HttpResponseMessage response = null;
            try
            {
                response = await httpClient.PostAsync(string.Format("/build/admin/triggerManualBuild.action?buildKey={0}", yourBuildKey), new FormUrlEncodedContent(new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("atl_token", atl_token) }));
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException e)
            {
                ShowMes(e.Message, e);
            }
            catch (Exception e)
            {
                ShowMes(e.Message, e);
            }
            try
            {
                buildNum = response?.RequestMessage.RequestUri.Segments[2];
            }
            catch
            {
                ShowMes("can not get buildNum");
            }
            return new KeyValuePair<string, string>(buildNum, string.Format(@"{0}browse/{1}", httpClient.BaseAddress.ToString(), buildNum??"null"));
        }

        public async Task<bool> CanceltBuildAsync(string buildNum)
        {
            if ((await GetBuildCommitAsync(buildNum)).BuildState.StartsWith("is building"))
            {
                return (await httpClient.PostAsync(string.Format(@"build/admin/ajax/stopPlan.action?planResultKey={0}", buildNum), null)).StatusCode == HttpStatusCode.OK;
            }
            return false;
        }

        /// <summary>
        /// get BuildStatus  [eg: http://ci.indata.cc/rest/api/latest/result/status/BUSCARD-TEST16-374?expand=stages.stage.results.result&_=1576581609028 ]
        /// </summary>
        /// <param name="buildNum">buildNum like BUSCARD-TEST16-374</param>
        /// <returns>null is fail / num% is progress / ok is complete</returns>
        public async Task<string> GetBuildProgressAsync(string buildNum)
        {
            string buildStatus = null;
            HttpResponseMessage response = null;
            try
            {
                response = await httpClient.GetAsync(string.Format("rest/api/latest/result/status/{0}?expand=stages.stage.results.result&_={1}", buildNum, NowTimeStamp));
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                JObject jo = null;
                try
                {
                    jo = (JObject)JsonConvert.DeserializeObject(responseBody);
                }
                catch
                {
                    ShowMes(string.Format("【{0}】 can not transition to JObject", responseBody));
                }
                if (jo == null || jo["progress"]["percentageCompletedPretty"] == null)
                {
                    ShowMes(string.Format("can not get progress in 【{0}】", responseBody));
                }
                else
                {
                    buildStatus = jo["progress"]["percentageCompletedPretty"].Value<string>();
                }
            }
            catch (HttpRequestException e)
            {

                if (response != null && (int)response.StatusCode == 404 && (await response.Content.ReadAsStringAsync()).Contains("not building"))
                {
                    buildStatus = "ok";
                }
                else
                {
                    ShowMes(e.Message, e);
                }
            }
            catch (Exception e)
            {
                ShowMes(e.Message, e);
            }
            return buildStatus;
        }

        /// <summary>
        /// get the commit by buildNum [eg:http://ci.indata.cc/browse/BUSCARD-TEST16-374]
        /// </summary>
        /// <param name="buildNum">buildNum</param>
        /// <returns>commit or null</returns>
        public async Task<CommitInfo> GetBuildCommitAsync(string buildNum)
        {
            List<KeyValuePair<string, string>> nowCommits = null;
            string nowBuildState = null;
            HttpResponseMessage response = null;
            try
            {
                //HttpResponseMessage response = await httpClient.GetAsync(string.Format("browse/{0}/commit?_={1}", buildNum, NowTimeStamp));
                response = await httpClient.GetAsync(string.Format("browse/{0}", buildNum));
                response.EnsureSuccessStatusCode();
                HtmlAgilityPack.HtmlDocument htmlDocument = new HtmlAgilityPack.HtmlDocument();
                //htmlDocument.LoadHtml(await response.Content.ReadAsStringAsync());
                htmlDocument.Load(await response.Content.ReadAsStreamAsync());
                HtmlNode htmlNode = htmlDocument.DocumentNode;
                nowBuildState = htmlNode.SelectNodes(@"//*[@id=""sr-build""]/h2/span[3]")?[0]?.InnerText;
                HtmlNodeCollection commitNodes = htmlNode.SelectNodes(@"//*[@id=""changesSummary""]/table/tbody");
                if (commitNodes != null && commitNodes.Count > 0)
                {
                    nowCommits = new List<KeyValuePair<string, string>>();
                    var trNodes = commitNodes[0].ChildNodes.Where<HtmlNode>((node) => node.Name == "tr");
                    if (trNodes.Any<HtmlNode>())
                    {
                        foreach (HtmlNode tempNode in trNodes)
                        {
                            var author = tempNode.ChildNodes.Where<HtmlNode>((node) => node.GetAttributeValue("class", null) == "author");
                            var commit_message = tempNode.ChildNodes.Where<HtmlNode>((node) => node.GetAttributeValue("class", null) == "commit-message");

                            List<HtmlNode> authorList = author.ToList();
                            List<HtmlNode> commitList = commit_message.ToList();

                            if (authorList != null && commitList != null && authorList.Count == 1 && commitList.Count == 1)
                            {
                                //sbCommit.Append(authorList[0].Element("a")?.InnerText ?? "null");
                                nowCommits.Add(new KeyValuePair<string, string>(authorList[0].InnerText.Trim(new char[] { '\n', '\r', ' ' }).Split(' ')[0], System.Web.HttpUtility.HtmlDecode(commitList[0].InnerText.Trim(new char[] { '\n', '\r', ' ' }))));
                            }
                            else
                            {
                                nowCommits.Add(new KeyValuePair<string, string>("", "error commit format in the html"));
                            }
                        }
                    }
                }

            }
            catch (HttpRequestException e)
            {
                ShowMes(response == null ? e.Message : await response.Content.ReadAsStringAsync().ConfigureAwait(false));
            }
            catch (Exception e)
            {
                ShowMes(e.Message, e);
            }
            return new CommitInfo() { CommitList = nowCommits, BuildState = nowBuildState };
        }

        public async Task<string> GetBuildLogs(string buildNum)
        {
            string logs = null;
            HttpResponseMessage response = null;
            try
            {
                response = await httpClient.GetAsync(string.Format("browse/{0}/log", buildNum));
                response.EnsureSuccessStatusCode();
                HtmlAgilityPack.HtmlDocument htmlDocument = new HtmlAgilityPack.HtmlDocument();
                htmlDocument.Load(await response.Content.ReadAsStreamAsync());
                HtmlNode htmlNode = htmlDocument.DocumentNode;
                HtmlNode logDetailNode = htmlNode.SelectSingleNode(@"//*[@id=""job-logs""]/tbody/tr[1]/td[3]/a[2]");
                if(logDetailNode!=null)
                {
                    string logDetailHref = logDetailNode.GetAttributeValue("href", null);
                    if (logDetailHref != null)
                    {
                        response = await httpClient.GetAsync(string.Format("browse/{0}/log", buildNum));
                        response.EnsureSuccessStatusCode();
                        logs = await response.Content.ReadAsStringAsync();
                    }
                }
                if (logs == null)
                {

                    HtmlNode logNode = htmlNode.SelectSingleNode(@"//*[@id=""job-logs""]/tbody/tr[2]/td/div");
                    if (logNode != null)
                    {
                        logs = System.Web.HttpUtility.HtmlDecode(logNode.InnerText);
                        System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"(\r\n +\r\n)");
                        logs = regex.Replace(logs, "\r\n");
                        regex = new System.Text.RegularExpressions.Regex(@"(\r\n){2,}");
                        logs = regex.Replace(logs, "\r\n");
                    }
                }
            }
            catch (HttpRequestException e)
            {
                ShowMes(response == null ? e.Message : await response.Content.ReadAsStringAsync().ConfigureAwait(false));
            }
            catch (Exception e)
            {
                ShowMes(e.Message, e);
            }
            return logs ?? "can not get build logs";
        }


        private async Task<int> GetPlanIdAsync(string yourBuildKey)
        {
            int planId = 0;
            HttpResponseMessage response = await httpClient.GetAsync(string.Format("rest/api/latest/deploy/project/forPlan?planKey={0}&decorator=nothing&confirm=true&_={1}", yourBuildKey, NowTimeStamp));
            string responseBody = await response.Content.ReadAsStringAsync();
            JArray joAr = null;
            try
            {
                joAr = (JArray)JsonConvert.DeserializeObject(responseBody);
            }
            catch
            {
                ShowMes(string.Format("【{0}】 can not transition to JObject", responseBody));
            }
            if (joAr == null || joAr.Count != 1)
            {
                ShowMes(string.Format("get plan id fial with【{0}】", responseBody));
            }
            else
            {
                planId = ((JObject)joAr[0])["id"]?.Value<int>() ?? 0;
            }
            if (planId <= 0)
            {
                planId = 0;
            }
            return planId;
        }

        /// <summary>
        /// get deploy environment list (you may use 2 or more environment)
        /// </summary>
        /// <param name="yourBuildKey"></param>
        /// <returns></returns>
        public async Task<List<KeyValuePair<string, string>>> GetDeployEnvironments(string yourBuildKey = null)
        {
            yourBuildKey = yourBuildKey ?? BuildKey;
            List<KeyValuePair<string, string>> deployEnvironmentList = new List<KeyValuePair<string, string>>();
            HttpResponseMessage response = null;
            int planId = await GetPlanIdAsync(yourBuildKey);
            if (planId <= 0)
            {
                ShowMes("GetPlanIdAsync fail");
                return null;
            }
            //get environmentId
            response = await httpClient.GetAsync(string.Format("deploy/viewDeploymentProjectEnvironments.action?id={0}", planId));
            try
            {
                HtmlAgilityPack.HtmlDocument htmlDocument = new HtmlAgilityPack.HtmlDocument();
                //htmlDocument.LoadHtml(await response.Content.ReadAsStringAsync());
                htmlDocument.Load(await response.Content.ReadAsStreamAsync());
                HtmlNode htmlNode = htmlDocument.DocumentNode;
                //environmentId = htmlNode.SelectSingleNode(@"/html/body/div/section/header/div/div[2]/div/div[1]/div/div/ul/li/a").Attributes["id"].Value.Remove(0, 7);

                HtmlNode deployEnvironmentsNode = htmlNode.SelectSingleNode(@"//*[@id=""deploy-environments-list""]/div/div/ul");
                if (deployEnvironmentsNode == null || deployEnvironmentsNode.ChildNodes.Count <= 0)
                {
                    ShowMes("not find any deploy environment node");
                    return null;
                }
                foreach (HtmlNode tempNode in deployEnvironmentsNode.ChildNodes)
                {
                    deployEnvironmentList.Add(new KeyValuePair<string, string>(tempNode.SelectSingleNode(@"a").Attributes["id"].Value.Remove(0, 7), tempNode.InnerText));
                }
                return deployEnvironmentList;
            }
            catch (Exception ex)
            {
                ShowMes(string.Format("can not get environmentId in html /r/n{0}", ex.Message));
            }
            return null;
        }


        /// <summary>
        /// Deploy your project by BuildKey (may throw Exception )
        /// </summary>
        /// <param name="yourBuildKey">BuildKey</param>
        /// <returns>deploymentResult array , if return null means deploy not start </returns>
        public async Task<string[]> StartDeployAsync(string yourBuildKey = null, string environmentId = null)
        {
            string[] deployResult = new string[] { null, null };
            yourBuildKey = yourBuildKey ?? BuildKey;
            string deploymentResultId = null;
            string resultKey = null;
            string nextVersionName = null;

            HttpResponseMessage response = null;
            string responseBody = null;
            JArray joAr = null;
            //get planId
            int planId = await GetPlanIdAsync(yourBuildKey);
            if (planId <= 0)
            {
                throw new Exception("GetPlanIdAsync fail");
            }


            try
            {
                //get possibleResults  http://ci.indata.cc/rest/api/latest/deploy/preview/possibleResults?searchTerm=&start-index=0&max-results=10&deploymentProjectId=4718603&planKey=BUSCARD-TEST16&_=1577805001587 
                response = await httpClient.GetAsync(string.Format("rest/api/latest/deploy/preview/possibleResults?searchTerm=&start-index=0&max-results=10&deploymentProjectId={0}&planKey={1}&_={2}", planId, yourBuildKey, NowTimeStamp));
                responseBody = await response.Content.ReadAsStringAsync();
                joAr = (JArray)JsonConvert.DeserializeObject(responseBody);
                resultKey = ((JObject)joAr[0])["planResultKey"]["key"]?.Value<string>() ?? null;
            }
            catch (HttpRequestException ex)
            {
                ShowMes(ex.Message);
            }
            catch
            {
                ShowMes(string.Format("can not get [planResultKey][key] in JObject with【{0}】", responseBody));
            }

            if (resultKey == null)
            {
                ShowMes(string.Format("can not get possibleResults"));
            }
            else
            {
                // get nextVersionName 
                response = await httpClient.GetAsync(string.Format("rest/api/latest/deploy/preview/versionName?deploymentProjectId={0}&resultKey={1}&_={2} ", planId, resultKey, NowTimeStamp));
                responseBody = await response.Content.ReadAsStringAsync();
                try
                {
                    nextVersionName = JsonConvert.DeserializeObject<JObject>(responseBody)["nextVersionName"].Value<string>();
                }
                catch
                {
                    ShowMes(string.Format("can not get nextVersionName in JObject with【{0}】", responseBody));
                }
            }

            if (nextVersionName != null && environmentId == null)
            {
                List<KeyValuePair<string, string>> environmentList = await GetDeployEnvironments(yourBuildKey);
                if (environmentList != null && environmentList.Count > 0)
                {
                    environmentId = environmentList[0].Key;
                }
            }

            Func<Task> ExecuteManualDeployment = async () =>
            {
                //execute Deployment http://ci.indata.cc/deploy/executeManualDeployment.action 
                response = await httpClient.PostAsync(string.Format("deploy/executeManualDeployment.action"), new FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                    new KeyValuePair<string, string>("environmentId", environmentId) ,
                    new KeyValuePair<string, string>("releaseTypeOption", "CREATE") ,
                    new KeyValuePair<string, string>("newReleaseBranchKey", yourBuildKey) ,
                    new KeyValuePair<string, string>("newReleaseBuildResult", resultKey) ,
                    new KeyValuePair<string, string>("versionName", nextVersionName) ,
                    new KeyValuePair<string, string>("promoteReleaseBranchKey", "") ,
                    new KeyValuePair<string, string>("promoteVersion", "MyBambooMonitor") ,//test-449
                    new KeyValuePair<string, string>("save", "Start deployment") ,
                    new KeyValuePair<string, string>("atl_token", atl_token)
                }));

                try
                {
                    response.EnsureSuccessStatusCode();
                    //http://ci.indata.cc/deploy/viewDeploymentResult.action?deploymentResultId=72256409 
                    //deploymentResultId = response.RequestMessage.RequestUri.Segments[5];
                    //deploymentResultId = response.RequestMessage.RequestUri.Query[0].ToString();
                    string tempQuery = response.RequestMessage.RequestUri.Query;
                    if (string.IsNullOrEmpty(tempQuery) && response.RequestMessage.RequestUri.Segments[2] == "executeManualDeployment.action")
                    {
                        deploymentResultId = "0"; // This release version is already in use, please select another.
                    }
                    else if (tempQuery.StartsWith("?deploymentResultId="))
                    {
                        deploymentResultId = tempQuery.Remove(0, "?deploymentResultId=".Length);
                        int tempResultId;
                        if (!int.TryParse(deploymentResultId, out tempResultId))
                        {
                            deploymentResultId = null;
                            throw new Exception(string.Format("{0} can not use as deploymentResultId", deploymentResultId));
                        }
                    }
                    else
                    {
                        throw new Exception("nuknow error in ExecuteManualDeployment");
                    }
                }
                catch (HttpRequestException e)
                {
                    ShowMes(e.Message);
                }
                catch
                {
                    ShowMes(string.Format("can not get deploymentResultId with {0}", response.RequestMessage.RequestUri.ToString()));
                }
            };

            if (nextVersionName != null && environmentId != null)
            {
                await ExecuteManualDeployment();
                if (deploymentResultId == "0")
                {
                    nextVersionName = "X_" + nextVersionName;
                    await ExecuteManualDeployment();
                }
            }
            else
            {
                throw new Exception("can not get nextVersionName or environmentId");
            }
            deployResult[0] = deploymentResultId;
            deployResult[1] = nextVersionName;
            return deployResult;
        }

        /// <summary>
        /// StartDeploy and releaseTypeOption= PROMOTE
        /// </summary>
        /// <param name="promoteVersion"></param>
        /// <param name="environmentId"></param>
        /// <returns>null mean fail </returns>
        public async Task<string> StartPromoteDeployAsync(string promoteVersion, string environmentId)
        {
            string deploymentResultId = null;
            HttpResponseMessage response = await httpClient.PostAsync(string.Format("deploy/executeManualDeployment.action"), new FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                    new KeyValuePair<string, string>("environmentId", environmentId) ,
                    new KeyValuePair<string, string>("releaseTypeOption", "PROMOTE") ,
                    new KeyValuePair<string, string>("promoteVersion", promoteVersion) ,
                    new KeyValuePair<string, string>("save", "Start deployment") ,
                    new KeyValuePair<string, string>("atl_token", atl_token)
                }));

            try
            {
                response.EnsureSuccessStatusCode();
                string tempQuery = response.RequestMessage.RequestUri.Query;
                if (tempQuery.StartsWith("?deploymentResultId="))
                {
                    deploymentResultId = tempQuery.Remove(0, "?deploymentResultId=".Length);
                    int tempResultId;
                    if (!int.TryParse(deploymentResultId, out tempResultId))
                    {
                        deploymentResultId = null;
                    }
                }
            }
            catch (HttpRequestException e)
            {
                ShowMes(e.Message);
            }
            catch
            {
                ShowMes(string.Format("can not get deploymentResultId with {0}", response.RequestMessage.RequestUri.ToString()));
            }
            return deploymentResultId;
        }

        /// <summary>
        /// get the deploy status by deploymentResultId
        /// </summary>
        /// <param name="deploymentResultId">deploymentResultId</param>
        /// <returns>"SUCCESS"is mean ok</returns>
        public async Task<string> GetDeployStatusAsync(string deploymentResultId)
        {
            string deploymentState = null;
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(string.Format("http://ci.indata.cc/rest/api/latest/deploy/result/{0}?includeLogs=true&max-results=50&_={1} ", deploymentResultId, NowTimeStamp));
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                JObject jo = null;
                jo = (JObject)JsonConvert.DeserializeObject(responseBody);
                deploymentState = jo["deploymentState"].Value<string>();
            }
            catch (HttpRequestException e)
            {
                ShowMes(e.Message);
            }
            catch (Exception e)
            {
                ShowMes(string.Format(" can not get  with deploymentState", e.Message));
            }
            return deploymentState;
        }
    }
}
