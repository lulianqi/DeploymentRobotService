using System;
using System.Collections.Generic;
using System.Text;

namespace MyDeploymentMonitor.ExecuteHelper
{
    public enum ExecuteStatus
    {
        Triggered,          //开始被触发
        BuildQueued,        //build排队中
        BuildRunning,       //build执行中
        BuildTimeOut,       //build超时 【结束点】
        BuildStop,          //build停止 【结束点】
        BuildSuccess,       //build成功
        BuildFailed,        //build失败【结束点】
        BuildError,         //build错误（如服务异常）【结束点】
        BuildCancle,        //build被取消【结束点】
        LaunchQueued,       //launch排队中
        LanuchSkip,         //lanuch跳过【结束点】
        Launching,          //lanuch更新中
        LaunchSuccess,      //lanuch更新成功
        LaunchTimeOut,      //launch超时【结束点】
        LaunchFailed,       //launch失败【结束点】
        LaunchError,        //launch错误（如服务异常）【结束点】
        LaunchStop,         //launch停止【结束点】
        LaunchCancle        //launch被取消【结束点】
    }
}
