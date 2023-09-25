using MessageRobot.DingDingHelper.Message;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MessageRobot.DingDingHelper
{
    public class DingDingRobot
    {
        private HttpClient httpClient;
        private const int maxTextLength = 2000;
        public String WebhookUri { get; set; }
        public String Secret { get; set; }

        private long NowTimeStamp { get { return (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000; } }
        public DingDingRobot(string webHook, string secret = null)
        {
            WebhookUri = webHook;
            Secret = secret;
            httpClient = new HttpClient();
        }
        private void Report(string mes, bool isError = false)
        {
            Console.WriteLine(mes);
        }
        private string GetDingdingSign(long nowTimeStamp)
        {
            if (Secret == null)
            {
                return null;
            }
            byte[] hashValue;
            string stringToSign = string.Format("{0}\n{1}", nowTimeStamp, Secret);
            using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Secret)))
            {
                hashValue = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
            }
            return System.Web.HttpUtility.UrlEncode(Convert.ToBase64String(hashValue));
        }
        private string GetDingDingdingSignStr()
        {
            if (Secret == null)
            {
                return null;
            }
            long timeStamp = NowTimeStamp;
            return string.Format("&timestamp={0}&sign={1}", timeStamp, GetDingdingSign(timeStamp));
        }

        public async Task<bool> SendMarkdownAsync(string title, string text, bool isAtALL = false, List<String> atMobiles = null)
        {
            return await SendMessageAsync(new MarkdownMessage(title, text, isAtALL, atMobiles));
        }

        public async Task<bool> SendTextAsync(string mes, bool isAtALL = false, List<String> atMobiles = null)
        {
            if (mes.Length <= maxTextLength)
            {
                return await SendMessageAsync(new TextMessage(mes, isAtALL, atMobiles));
            }
            else
            {
                System.IO.StringReader stringReader = new System.IO.StringReader(mes);
                string temLine = string.Empty;
                StringBuilder sb = new StringBuilder(maxTextLength);
                while ((temLine = stringReader.ReadLine()) != null)
                {
                    if (temLine.Length > maxTextLength)
                    {
                        temLine = temLine.Substring(0, maxTextLength);
                    }

                    if (sb.Length + temLine.Length > maxTextLength)
                    {
                        if (!await SendMessageAsync(new TextMessage(sb.ToString(), isAtALL, atMobiles)))
                        {
                            return false;
                        }
                        sb = new StringBuilder(temLine, maxTextLength);
                        sb.AppendLine();
                    }
                    else
                    {
                        sb.AppendLine(temLine);
                    }
                }
                if (sb.Length > 0)
                {
                    return await SendMessageAsync(new TextMessage(sb.ToString(), isAtALL, atMobiles));
                }
            }
            return true;
        }

        private async Task<bool> SendMessageAsync(DMessage dMessage)
        {

            string textMessage;
            try
            {
                textMessage = Newtonsoft.Json.JsonConvert.SerializeObject(dMessage);
            }
            catch (Exception ex)
            {
                Report(ex.Message);
                return false;
            }
            try
            {
                HttpResponseMessage response = await httpClient.PostAsync(Secret == null ? WebhookUri : WebhookUri + GetDingDingdingSignStr(), new StringContent(textMessage, Encoding.UTF8, "application/json"));
                string responseBody = await response.Content.ReadAsStringAsync();
                JObject jo = (JObject)JsonConvert.DeserializeObject(responseBody);

                if (jo["errcode"].Value<int>() != 0)
                {
                    Report(responseBody);
                    return false;
                }

            }
            catch (Exception e)
            {
                Report(e.Message);
                return false;
            }
            return true;

        }

    }
}
