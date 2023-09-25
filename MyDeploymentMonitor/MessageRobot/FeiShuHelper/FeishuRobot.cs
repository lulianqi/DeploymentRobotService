
using MessageRobot.FeiShuHelper.Message;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MessageRobot.FeiShuHelper
{
    public class FeishuRobot
    {
        private HttpClient httpClient;

        public String WebhookUri { get; set; }
        
        public FeishuRobot(string webHook)
        {
            WebhookUri = webHook;
            httpClient = new HttpClient();
        }
        private void Report(string mes,bool isError = false)
        {
            Console.WriteLine(mes);
        }

        public async Task<bool> SendTextAsync(string yourText, List<string> yourMentioned_list = null)
        {
            return await SendMessageAsync(new TextMessage(yourText, yourMentioned_list));
        }

        public async Task<bool> SendMessageAsync(FsMessage fsMessage)
        {
            string  textMessage ;
            try
            {
                textMessage = Newtonsoft.Json.JsonConvert.SerializeObject(fsMessage);
            }
            catch(Exception ex)
            {
                Report(ex.Message);
                return false;
            }
            try
            {
                HttpResponseMessage response = await httpClient.PostAsync( WebhookUri, new StringContent(textMessage, Encoding.UTF8, "application/json"));
                string responseBody = await response.Content.ReadAsStringAsync();
                JObject jo =(JObject)JsonConvert.DeserializeObject(responseBody);

                if (jo["StatusCode"].Value<int>() != 0)
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
