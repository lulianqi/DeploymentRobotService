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
using System.IO;

namespace MyDeploymentMonitor.DeploymentHelper.DataHelper
{
    public class MyGitHelper
    {
        private HttpClient gitHttpClient;
        public MyGitHelper(string privateToken, string gitBaseAddress)
        {

            gitHttpClient = new HttpClient() { BaseAddress = new Uri(gitBaseAddress) };
            gitHttpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", privateToken);
        }

        private void ShowMes(string mes)
        {
            Console.WriteLine(mes);
        }

        private long NowTimeStamp { get { return (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000; } }

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

        public async Task<HttpResponseMessage> GetGitResponseMessageAsync(string uri)
        {
            HttpResponseMessage response = await gitHttpClient.GetAsync(uri);
            return response;
        }

        public async Task<string> GetGitFileContentString(string projectsId,string filePath ,string branch)
        {
            return null;
        }

        public async Task<Stream> GetGitFileContentStream(string uri)
        {
            HttpResponseMessage response = await gitHttpClient.GetAsync(uri);
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
            if (jo == null || jo["content"]!=null )
            {
                try
                {
                    byte[] outputb = Convert.FromBase64String(jo.Value<string>("content"));
                    return new MemoryStream(outputb);
                }
                catch
                {
                    ShowMes("Convert.FromBase64String fial");
                    return null;
                }
            }
            return null;
        }
    }
}
