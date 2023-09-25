using System;
using System.Collections.Generic;
using System.Text;

namespace MessageRobot.WeChatHelper.Message
{
    public class ImageMessage: WxMessage
    {
        public class ImageContent
        {
            public string base64 { get; set; }
            public string md5 { get; set; }

            public ImageContent(string yourBase64,string yourMd5)
            {
                base64 = yourBase64;
                md5 = yourMd5;
            }
        }

        public ImageContent image { get; set; }

        public ImageMessage(string yourBase64, string yourMd5)
        {
            msgtype = "image";
            image = new ImageContent(yourBase64, yourMd5);
        }
    }
}
