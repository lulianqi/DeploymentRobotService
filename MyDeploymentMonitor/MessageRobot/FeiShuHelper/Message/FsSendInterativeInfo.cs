using System;
using System.Collections.Generic;
using System.Text;

namespace MessageRobot.FeiShuHelper.Message
{
    public class FsSendInterativeInfo
    {
        /// <summary>
        /// 卡片消息的uuid（由发送放生成的id，处理中继会通过此id与飞书返回的id相对应关联）
        /// </summary>
        public string uuid { get; set; }

        /// <summary>
        /// 卡片消息的接收者
        /// </summary>
        public string receive_id { get; set; }

        /// <summary>
        /// 卡片消息内容实体
        /// </summary>
        public string content { get; set; }

        /// <summary>
        /// 卡片消息需要加急的用户列表（加急完成后请清除该状态，避免在跟新消息时反复加急）
        /// </summary>
        public string[] urgent_users { get; set; }
    }
}
