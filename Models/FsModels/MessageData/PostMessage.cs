using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeploymentRobotService.Models.FsModels.MessageData
{
    public class PostMessageZh
    {
        public PostMessage zh_cn;

        public PostMessageZh(PostMessage postMessage)
        {
            zh_cn = postMessage;
        }
    }

    public class PostMessageEn
    {
        public PostMessage en_us;
        public PostMessageEn(PostMessage postMessage)
        {
            en_us = postMessage;
        }
    }

    public class PostMessage
    {
        public string title { get; set; }
        public Content[][] content { get; set; }
    }

    public class Content
    {
        /// <summary>
        /// text , a , at ,img
        /// </summary>
        public string tag { get; set; }  
        /// <summary>
        /// 文本
        /// </summary>
        public string text { get; set; } 
        /// <summary>
        /// 超链接
        /// </summary>
        public string href { get; set; }
        /// <summary>
        /// at 时使用
        /// </summary>
        public string user_id { get; set; } 
        /// <summary>
        /// at 时使用
        /// </summary>
        public string user_name { get; set; } 
        /// <summary>
        /// img 时使用
        /// </summary>
        public string image_key { get; set; } 
        /// <summary>
        /// img 时使用
        /// </summary>
        public int width { get; set; } 
        /// <summary>
        /// img 时使用
        /// </summary>
        public int height { get; set; } 
    }

}
