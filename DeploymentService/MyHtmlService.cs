using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeploymentRobotService.DeploymentService
{
    public class MyHtmlService
    {
        private const string outHtml = @"<!DOCTYPE html><html><head><title>{0}</title><meta charset=""utf-8""><meta name=""viewport"" content=""width=device-width, initial-scale=1, user-scalable=0""><!-- <link rel=""stylesheet"" type=""text/css"" href=""https://res.wx.qq.com/connect/zh_CN/htmledition/style/wap_err3696b4.css""> --><link rel=""stylesheet"" type=""text/css"" href=""https://res.wx.qq.com/open/libs/weui/0.4.1/weui.css""></head><body><div class=""weui_msg""><div class=""weui_icon_area""><i class=""{1} weui_icon_msg""></i></div><div class=""weui_text_area""><h4 class=""weui_msg_title"">{2}</h4></div><!-- <div style=""color: gray;font-size: 15px;margin-top: 5px;"">(e.FAYa0702d448)</div> --></div></body></html>";
        private const string showTextHtml = @"<!DOCTYPE html><html><head><meta charset=""utf-8""><title>{0}</title></head><body><p>{1}</p></body></html>";

        //public static async Task FillWxHtmlAsync(Microsoft.AspNetCore.Mvc.ControllerBase controller, string stateMessage, string additionMessage = null)
        public static ContentResult GetWxHtmlContent(Microsoft.AspNetCore.Mvc.ControllerBase controller, string stateMessage, string additionMessage = null)
        {
            if (!string.IsNullOrEmpty(stateMessage) && stateMessage.Contains('\n'))
            {
                stateMessage = stateMessage.Replace("\r\n", "<br />");
                stateMessage = stateMessage.Replace("\n", "<br />");
            }
            if (!string.IsNullOrEmpty(additionMessage) && additionMessage.Contains('\n'))
            {
                additionMessage = additionMessage.Replace("\r\n", "<br />");
                additionMessage = additionMessage.Replace("\n", "<br />");
            }
            string htmlStr = "CI终端";
            switch (stateMessage)
            {
                case "身份认值错误":
                case "用户信息错误":
                    htmlStr = string.Format(outHtml, "CI终端", "weui_icon_warn", stateMessage);
                    break;
                case "Running":
                    htmlStr = string.Format(outHtml, "CI终端", "weui_icon_success", string.Format("已经为您启动发布，请关注群消息中的发布动态<br />{0}", additionMessage ?? ""));
                    break;
                case "Canceling":
                    htmlStr = string.Format(outHtml, "CI终端", "weui_icon_success_circle", string.Format("正在为您取消正在进行的发布，请关注群消息中的发布动态<br />{0}", additionMessage ?? ""));
                    break;
                case "ErrorCommand":
                    htmlStr = string.Format(outHtml, "CI终端", "weui_icon_warn", "错误的指令");
                    break;
                case "cancle ok":
                    htmlStr = string.Format(outHtml, "CI终端", "weui_icon_success", "正在为您取消正在进行的发布，请关注群消息中的发布动态");
                    break;
                case "cancle fail":
                    htmlStr = string.Format(outHtml, "CI终端", "weui_icon_warn", "无法取消发布<br />没有找到项目，或指定项目已经发布完成");
                    break;
                case "您的账号没有权限进行发布操作":
                    htmlStr = string.Format(outHtml, "CI终端", "weui_icon__cancel", "您的账号没有权限进行发布操作");
                    break;
                default:
                    htmlStr = string.Format(outHtml, "CI终端", "weui_icon_info", stateMessage);
                    break;
            }
            return (controller.Content(htmlStr, "text/html", System.Text.Encoding.UTF8));
            /** old way (Chunked body did not terminate properly with 0-sized chunk. 动态分块响应可能无法正常关闭 )
            var data = System.Text.Encoding.UTF8.GetBytes(htmlStr);
            controller.Response.ContentType = @"text/html; charset=utf-8";
            await controller.Response.Body.WriteAsync(data, 0, data.Length);
            controller.Response.Body.Close();
            **/
        }

        public static ContentResult GetPojectsHtmlContent(Microsoft.AspNetCore.Mvc.ControllerBase controller, string projectStr)
        {
            string htmlStr = string.Format(showTextHtml, "工程列表", projectStr);
            return (controller.Content(htmlStr, "text/html", System.Text.Encoding.UTF8));
        }

    }


}
