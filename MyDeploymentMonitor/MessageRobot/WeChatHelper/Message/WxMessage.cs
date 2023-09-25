using System;
using System.Collections.Generic;
using System.Text;

namespace MessageRobot.WeChatHelper.Message
{
    public abstract class WxMessage
    {
        public String msgtype { get; set; } 
    }
    //public class AtContent { public List<String> atMobiles { get; set; } public AtContent(List<String> mobiles) { atMobiles = mobiles; } }
}
