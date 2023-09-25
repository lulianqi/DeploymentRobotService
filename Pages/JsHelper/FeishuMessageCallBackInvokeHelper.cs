using DeploymentRobotService.Pages.PageHelper;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DeploymentRobotService.Pages.JsHelper
{
    public class FeishuMessageCallBackInvokeHelper
    {

        /// <summary>
        /// 是否已经对原始消息进行了默认发布
        /// </summary>
        public bool IsHaveDealBuildMessage { get; set; } = false;
        /// <summary>
        /// 获取发布原始消息详情
        /// </summary>
        public BuildMessageInfo NowBuildMessageInfo { get;private set; }
        /// <summary>
        /// 获取消息中包含的项目 （触发DealBuildMessageCaller后设置该属性）
        /// </summary>
        public MessageProjectPredict NowMessageProjectPredict { get; private set; }
        private AntDesign.MessageService _messageService;
        public delegate void delegateInvokeHelperStatusChange(int mesType,string message=null);
        public event delegateInvokeHelperStatusChange OnFeishuMessageCallBackInvokeStatusChange;


        public FeishuMessageCallBackInvokeHelper(AntDesign.MessageService message)
        {
            _messageService = message;
        }

        /// <summary>
        /// 处理发布消息，默认由JS直接调用，并执行TryBuildAll
        /// </summary>
        /// <param name="buildMessageInfo"></param>
        [JSInvokable("DealBuildMessage")]
        public void DealBuildMessageCaller(BuildMessageInfo buildMessageInfo)
        {
            NowBuildMessageInfo = buildMessageInfo;
            if(!string.IsNullOrEmpty(NowBuildMessageInfo.trigger))
            {
                NowBuildMessageInfo.trigger = DeploymentService.ApplicationRobot.FsRobotBusinessData.GetOpenIdByName(NowBuildMessageInfo.trigger);
            }
            if(NowBuildMessageInfo.content!=null)
            {
                NowMessageProjectPredict = MessageProjectPredict.GetMessageProjectPredict(NowBuildMessageInfo.content);
            }
            else
            {
                NowMessageProjectPredict = new MessageProjectPredict() { ErrorMessage = "不支持的消息类型" };
            }
            NowMessageProjectPredict.UserFlag = NowBuildMessageInfo.trigger;  //"ou_924bb7c3bf0e9583a7124b390b19b4a2"
            NowMessageProjectPredict.UpdataProjects();
            bool isBuildAll =  NowMessageProjectPredict.TryBuildAll();
            IsHaveDealBuildMessage = true;
            if(OnFeishuMessageCallBackInvokeStatusChange!=null)
            {
                OnFeishuMessageCallBackInvokeStatusChange.Invoke(0);
            }
        }

        [JSInvokable("ShowErrorInfo")]
        public void ShowErrorInfo(string mes)
        {
            Console.WriteLine($"-----------------{mes}-----------------");
            _messageService?.Warn(mes);
        }

        [JSInvokable("ReportJsFuncStatus")]
        public void ReportJsFuncStatus(int mesType , string mes)
        {
            if (OnFeishuMessageCallBackInvokeStatusChange != null)
            {
                OnFeishuMessageCallBackInvokeStatusChange.Invoke(mesType, mes);
            }
        }
    }

}
