using System;
using System.Collections.Generic;
using System.Text;

namespace MessageRobot.DingDingHelper.Message
{
    public class MarkdownMessage : DMessage
    {
        public class MarkdownContent { public String title { get; set; } public String text { get; set; } public MarkdownContent(string yourTitle,string yourText) { title=yourTitle; text = yourText; } }
        public MarkdownContent markdown { get; set; }
        public MarkdownMessage(string title,string text, bool atAll = false, List<String> atMobiles = null)
        {
            msgtype = "markdown";
            markdown = new MarkdownContent(title,text);
            isAtAll = atAll;
            at = (atMobiles?.Count ?? 0) > 0 ? new AtContent(atMobiles) : null;
        }

    }
}
