using System;
using System.Collections.Generic;
using System.Text;

namespace MessageRobot.WeChatHelper.Message
{
    public class NewsMessage : WxMessage
    {
        public class NewsContent
        {
            public string title { get; set; }
            public string description { get; set; }
            public string url { get; set; }
            public string picurl { get; set; }

            public NewsContent(string yourTitle,string yourUrl,string yourDescription=null, string yourPicurl=null)
            {
                title = yourTitle;
                description = yourDescription;
                url = yourUrl;
                picurl = yourPicurl;
            }
        }

        public class NewsList
        {
            public List<NewsContent> articles { get; set; }

            public NewsList(List<NewsContent> yourArticles)
            {
                articles = yourArticles;
            }
        }

        public NewsList news { get; set; }

        public NewsMessage(string yourTitle, string yourUrl, string yourDescription = null, string yourPicurl = null)
        {
            msgtype = "news";
            news =new NewsList(new List<NewsContent>() { new NewsContent(yourTitle, yourUrl, yourDescription, yourPicurl) });
        }
    }
}
