using System;
using System.Collections.Generic;
using System.Text;

namespace MessageRobot.DingDingHelper.Message
{
    public class TextMessage: DMessage
    {
        public class TextContent { public String content { get; set; } public TextContent(string yourContent) { content = yourContent; } }
        public TextContent text { get; set; }
        public TextMessage(string mes,bool atAll = false , List<String> atMobiles =null)
        {
            msgtype = "text";
            text = new TextContent(mes);
            isAtAll = atAll;
            at = (atMobiles?.Count ?? 0) > 0 ? new AtContent(atMobiles) : null;
        }
    }
}
