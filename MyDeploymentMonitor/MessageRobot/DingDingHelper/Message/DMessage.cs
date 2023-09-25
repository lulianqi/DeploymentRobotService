using System;
using System.Collections.Generic;
using System.Text;

namespace MessageRobot.DingDingHelper.Message
{
    public abstract class DMessage
    {
        public String msgtype { get; set; } 
        public AtContent at { get; set; }
        public bool isAtAll { get; set; } = false;

    }
    public class AtContent { public List<String> atMobiles { get; set; } public AtContent(List<String> mobiles) { atMobiles = mobiles; } }
}
