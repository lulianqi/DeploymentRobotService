using System;
using System.Text.Json;

namespace DeploymentRobotService.Pages.JsHelper
{
    public class BuildMessageInfo
    {
        public string sender { get; set; }
        public string content { get; set; }
        public string trigger { get; set; }
        public string chatId { get; set; }

        public override string ToString()
        {
            byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(this);
            return System.Text.Encoding.UTF8.GetString(jsonBytes);
        }
    }
}


