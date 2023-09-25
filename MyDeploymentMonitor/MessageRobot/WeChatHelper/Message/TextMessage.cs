using System;
using System.Collections.Generic;
using System.Text;

namespace MessageRobot.WeChatHelper.Message
{
    public class TextMessage : WxMessage
    {
        public class TextContent
        {
            public String content { get; set; }
            public List<string> mentioned_list { get; set; }
            public List<string> mentioned_mobile_list { get; set; }
            public TextContent(string yourContent ,List<string> yourMentioned_list = null , List<string> yourMentioned_mobile_list =null)
            {
                content = yourContent;
                mentioned_list = yourMentioned_list;
                mentioned_mobile_list = yourMentioned_mobile_list;
            }
        }

        public TextContent text { get; set; }

        public TextMessage(string yourContent, List<string> yourMentioned_list = null, List<string> yourMentioned_mobile_list = null)
        {
            msgtype = "text";
            text = new TextContent(yourContent,yourMentioned_list,yourMentioned_mobile_list);
        }
    }
}
