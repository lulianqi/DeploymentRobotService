using MessageRobot.WeChatHelper.Message;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MessageRobot.WeChatHelper
{
    public class WeChatRobot
    {
        private HttpClient httpClient;

        private const int maxTextLength = 2000;

        public String WebhookUri { get; set; }
        
        public WeChatRobot(string webHook)
        {
            WebhookUri = webHook;
            httpClient = new HttpClient();
        }
        private void Report(string mes,bool isError = false)
        {
            Console.WriteLine(mes);
        }

        //public async Task<bool> SendImageAsync(Image image)
        //{
        //    throw new NotImplementedException();
        //}


        public async Task<bool> SendMarkdownAsync(string mes)
        {
            return await SendMessageAsync(new MarkdownMessage(mes));
        }
        public async Task<bool> SendTextAsync(string mes, List<string> yourMentioned_list = null, List<string> yourMentioned_mobile_list = null)
        {
            if (mes.Length <= maxTextLength)
            {
                return await SendMessageAsync(new TextMessage(mes, yourMentioned_list, yourMentioned_mobile_list));
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
                        if(!await SendMessageAsync(new TextMessage(sb.ToString(), yourMentioned_list, yourMentioned_mobile_list)))
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
                    return await SendMessageAsync(new TextMessage(sb.ToString(), yourMentioned_list, yourMentioned_mobile_list));
                }
            }
            return true;
        }

        public async Task<bool> SendNewsAsync(string yourTitle, string yourUrl, string yourDescription = null, string yourPicurl = null)
        {
            return await SendMessageAsync(new NewsMessage(yourTitle, yourUrl, yourDescription, yourPicurl));
        }

        private async Task<bool> SendMessageAsync(WxMessage wxMessage)
        {

            string  textMessage ;
            try
            {
                textMessage = Newtonsoft.Json.JsonConvert.SerializeObject(wxMessage);
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
