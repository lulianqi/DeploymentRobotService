using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace MessageRobot.FeiShuHelper.Message
{
    public class TextMessage : FsMessage
    {
        public class Content
        {
            public String text { get; set; }
            private List<string> mentioned_list { get; set; }

            public Content(string yourContent ,List<string> yourMentioned_list = null )
            {
                text = yourContent;
                if(yourMentioned_list?.Count>0)
                {
                    //<at user_id=\"ou_d462849de343be5dc77ebdb8c4819070\"></at>
                    StringBuilder tempAtSb = new StringBuilder(text); 
                    tempAtSb.AppendLine();
                    foreach (var at in yourMentioned_list)
                    {
                        if(string.IsNullOrEmpty(at))
                        {
                            continue;
                        }
                        if (at.StartsWith('@'))
                        {
                            tempAtSb.Append($"<at user_id=\"\">{at.TrimStart('@')}</at>");
                        }
                        else
                        {
                            tempAtSb.Append($"<at user_id=\"{at}\"></at>");
                        }
                    }
                    text = tempAtSb.ToString();
                }
            }
        }

        public string content { get; set; }

        public TextMessage(string yourContent,List<string> yourMentioned_list = null)
        {   
            msg_type = "text";
            JObject jo = (JObject)JToken.FromObject(new Content(yourContent, yourMentioned_list));
            content = jo.ToString();
        }
    }
}
