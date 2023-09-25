using System;
using System.Text.Json;
using Microsoft.JSInterop;

namespace DeploymentRobotService.Pages.JsHelper
{
    public class SendCardMessageInfo
    {
        public string receiver { get; set; }
        public string content { get; set; }
        public string chatId { get; set; }
        public  DotNetObjectReference<FeishuMessageCallBackInvokeHelper> dotnetHelper { get; set; }

        public override string ToString()
        {
            byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(this);
            return System.Text.Encoding.UTF8.GetString(jsonBytes);
        }
    }
}


