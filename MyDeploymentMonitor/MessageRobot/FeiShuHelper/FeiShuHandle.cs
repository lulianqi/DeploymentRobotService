using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using MessageRobot.FeiShuHelper.Message;
using MyDeploymentMonitor.ExecuteHelper;

namespace MessageRobot.FeiShuHelper
{
    public class FeiShuHandle
    {
        private static HttpClient httpClient;
        private const string getOpenIdUrl = "http://deploymentrobotservice:5000/feishu/FsGetOpenId/byNick";
        private const string sendInteractiveUrl = "http://localhost:5000/feishu/FsMessage/interactive";
        private static Dictionary<string, string> FsNickOpenIdDc;

        static FeiShuHandle()
        {
            Init();
        }

        public static void Init()
        {
            httpClient = new HttpClient();
            FsNickOpenIdDc = new Dictionary<string, string>();
        }

        public static async ValueTask<string> GetUserOpenIdAsync(string nick)
        {
            //debug code only for test
            //if (nick == "fuxiao")
            //{
            //    return "ou_924bb7c3bf0e9583a7124b390b19b4a2";
            //}

            if (string.IsNullOrEmpty(nick))
            {
                throw new ArgumentException($"'{nameof(nick)}' cannot be null or empty.", nameof(nick));
            }
            string openId = null;
            if(FsNickOpenIdDc.ContainsKey(nick))
            {
                return FsNickOpenIdDc[nick];
            }
            try
            {
                //openId = await (await httpClient.GetAsync($"{getOpenIdUrl}?nick={nick}")).Content.ReadAsStringAsync();
                HttpResponseMessage httpResponseMessage = await httpClient.GetAsync($"{getOpenIdUrl}?nick={nick}");
                httpResponseMessage.EnsureSuccessStatusCode();
                openId =await httpResponseMessage.Content.ReadAsStringAsync();
                Console.WriteLine($"__________________\r\nopen id[{openId}] nick[{nick}]");
                if(!string.IsNullOrEmpty(openId))
                {
                    FsNickOpenIdDc.TryAdd(nick, openId);
                }
            }
            catch(Exception ex)
            {
                openId = "";
                Console.WriteLine($"[GetUserOpenIdAsync] error \r\n{ex}");
            }
            return openId;
        }

        /// <summary>
        /// 根据用户nick列表返回用户openid列表
        /// </summary>
        /// <param name="nicks"></param>
        /// <returns></returns>
        public static async ValueTask<List<string>> GetUserOpenIdListAsync(List<string> nicks ,bool retainUnknowUser = true)
        {
            if(!(nicks?.Count>0))
            {
                return null;
            }
            List<string> result = new List<string>();
            Console.WriteLine($"[debug info] [GetUserOpenIdListAsync] {nicks.ToStringDetail()}");
            foreach (var nick in nicks)
            {
                if(string.IsNullOrEmpty(nick))
                {
                    Console.WriteLine($"[GetUserOpenIdListAsync] find empty user in nicks ");
                    continue;
                }
                string tempOpenId = await GetUserOpenIdAsync(nick);
                if(string.IsNullOrEmpty(tempOpenId))
                {
                    if (retainUnknowUser)
                    {
                        result.Add($"@{nick}");
                    }
                }
                else
                {
                    result.Add(tempOpenId);
                }
            }
            return result;
        }


        /// <summary>
        /// 发送/更新卡片消息 （成功返回message id，失败返回null）
        /// </summary>
        /// <param name="fsSendInterativeInfo"></param>
        /// <returns></returns>
        public static async ValueTask<string> SendInteractiveMessage(FsSendInterativeInfo fsSendInterativeInfo )
        {
            if (fsSendInterativeInfo is null)
            {
                throw new ArgumentNullException(nameof(fsSendInterativeInfo));
            }
            HttpResponseMessage httpResponseMessage = await httpClient.PostAsync(sendInteractiveUrl, new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(fsSendInterativeInfo), System.Text.Encoding.UTF8, "application/json"));
            try
            {
                httpResponseMessage.EnsureSuccessStatusCode();
            }
            catch(Exception ex)
            {
                Console.WriteLine($"[SendInteractiveMessage] error \r\n{ex}");
                return null;
            }
            return await httpResponseMessage.Content.ReadAsStringAsync();
        }
    }
}
