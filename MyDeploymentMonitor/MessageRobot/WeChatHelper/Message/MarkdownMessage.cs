using System;
using System.Collections.Generic;
using System.Text;

namespace MessageRobot.WeChatHelper.Message
{
    public class MarkdownMessage: WxMessage
    {
        public class MarkdownContent
        {
            public String content { get; set; }
            public MarkdownContent(string yourCoutent)
            {
                content = yourCoutent;
            }
        }

        public MarkdownContent markdown { get; set; }
        public MarkdownMessage(string mes)
        {
            msgtype = "markdown";
            markdown = new MarkdownContent(mes);
        }
    }
}
