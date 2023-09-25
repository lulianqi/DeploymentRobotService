using System;
using System.Collections.Generic;
using System.Text;

namespace MyDeploymentMonitor.ExecuteHelper
{
    public class DeploymentTimeline
    {
        public static long GetTimestamp()
        {
            return (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
        }

        public static string GetTimeStr(long yourTime)
        {
            TimeSpan timeSpan = new TimeSpan(yourTime * 10000000);
            DateTime now = (new DateTime(1970, 1, 1)).Add(timeSpan).ToLocalTime(); ;
            return now.ToString("HH:mm ss");
        }

        /// <summary>
        /// 当前时间 （全部使用秒级时间戳）
        /// </summary>
        public long NowTime { get { return GetTimestamp(); } }
        /// <summary>
        /// 开始构建时的时间（构建时间包括Build及Launch时间）
        /// </summary>
        public long StartDeploymentTime { get; set; } = GetTimestamp();
        /// <summary>
        /// 结束构建的时间
        /// </summary>
        public long EndDeploymentTime { get; set; } 
        /// <summary>
        /// 开始执行Build的时间（因为构建可能会排队）
        /// </summary>
        public long StartBuildTime { get; set; }
        /// <summary>
        /// 开始执行Build的时间（因为构建可能会排队）
        /// </summary>
        public long EndBuildTime { get; set; }
        /// <summary>
        /// 结束Build 的排队时间点（此时开始执行build）
        /// </summary>
        public long EndBuildQueueTime { get; set; }
        /// <summary>
        /// 开始新服务启动的时间
        /// </summary>
        public long StartLaunchTime { get; set; }
        /// <summary>
        /// 服务启动完成的时间
        /// </summary>
        public long EndLaunchTime { get; set; }

        /// <summary>
        /// 本次更新总的的预测耗时
        /// </summary>
        public int PredictTotalElapsed { get; set; }
        /// <summary>
        /// 本次Build的预测耗时
        /// </summary>
        public int PredictBuildElapsed { get; set; }
        /// <summary>
        /// 本次服务启动的预测耗时
        /// </summary>
        public int PredictLaunchElapsed { get; set; }


        /// <summary>
        /// 获取Build进度百分比（可能会超过100，无法预测返回-1）
        /// </summary>
        /// <returns>进度百分比</returns>
        public int GetBuildSchedule()
        {
            if (EndBuildTime > 0)
            {
                return 100;
            }
            //StartBuildTime
            if (EndBuildQueueTime > 0 && NowTime > 0 && PredictBuildElapsed > 0)
            {
                int tempSchedule = (int)((NowTime - EndBuildQueueTime) * 100 / PredictBuildElapsed);
                if(tempSchedule<1)
                {
                    tempSchedule = 1;
                }
                return tempSchedule;
            }
            return -1;
        }

        /// <summary>
        /// 获取Launch进度百分比（可能会超过100）
        /// </summary>
        /// <returns></returns>
        public int GetLaunchSchedule()
        {
            if (EndLaunchTime > 0)
            {
                return 100;
            }
            if (StartLaunchTime > 0 && NowTime > 0 && PredictLaunchElapsed > 0)
            {
                int tempSchedule = (int)((NowTime - StartLaunchTime)*100 / PredictLaunchElapsed);
                if (tempSchedule < 1)
                {
                    tempSchedule = 1;
                }
                return tempSchedule;
            }
            return -1;
        }

        /// <summary>
        /// 获取更新·预计完成时间
        /// </summary>
        /// <returns></returns>
        public string GetPredictFinishTimeStr()
        {
            //[>0]:预计完成时间
            //[=0]:已经完成
            //[=-1]:即将完成（超过预定时间小于60s）
            //[=-2];无法预测（超过预定时间超过60s，很可能会超时或失败）
            int totalTime = 0;
            bool willAbove = false;
            if(PredictTotalElapsed < (PredictBuildElapsed + PredictLaunchElapsed))
            {
                PredictTotalElapsed = PredictBuildElapsed + PredictLaunchElapsed;
            }
            //还未启动Build
            if (StartBuildTime <= 0)
            {
                willAbove = true;
                totalTime = PredictTotalElapsed;
            }
            //已经结束Launch
            else if (EndLaunchTime > 0)
            {
                totalTime = 0;
            }
            //已经开始Bliud,但是还没有开始Launch
            else if (StartLaunchTime <= 0)
            {
                //build 处于queue状态
                if(! (EndBuildQueueTime>0))
                {
                    int tempRemainTolalTime = PredictTotalElapsed - (int)(NowTime - StartBuildTime);
                    if(tempRemainTolalTime<(PredictBuildElapsed + PredictLaunchElapsed))
                    {
                        totalTime = PredictBuildElapsed + PredictLaunchElapsed;
                        willAbove = true;
                    }
                    else
                    {
                        totalTime = tempRemainTolalTime;
                    }
                }
                else
                {
                    int tempRemainBuildTime = PredictBuildElapsed - (int)(NowTime - EndBuildQueueTime);
                    if (tempRemainBuildTime > 0)
                    {
                        totalTime = tempRemainBuildTime + PredictLaunchElapsed;
                    }
                    else
                    {
                        willAbove = true;
                        totalTime = PredictLaunchElapsed;
                    }
                }
            }
            //已经开Launch
            else
            {
                int tempRemainLaunchTime = PredictLaunchElapsed - (int)(NowTime - StartLaunchTime);
                if (tempRemainLaunchTime > 0)
                {
                    totalTime = tempRemainLaunchTime;
                }
                else if (tempRemainLaunchTime > -60)
                {
                    totalTime = -1;
                }
                else
                {
                    totalTime = -2;
                }
            }

            if (totalTime > 0)
            {
                TimeSpan timeSpan = new TimeSpan(0, 0, totalTime);
                if(willAbove)
                {
                    return $"预计 {(int)timeSpan.TotalMinutes}分{timeSpan.Seconds}秒+ 后完成更新";
                }
                return $"预计 {(int)timeSpan.TotalMinutes}分{timeSpan.Seconds}秒 后完成更新";
            }
            else if(totalTime==0)
            {
                return "已经完成更新";
            }
            else if(totalTime==-1)
            {
                return "即将完成更新";
            }
            else if(totalTime==-2)
            {
                return "更新耗时已经超过预期时间";
            }
            else
            {
                return "[unkonw time]";
            }
        }

        /// <summary>
        /// 获取实际build时间（失败返回0）(当前时间排出了排队时间)
        /// </summary>
        /// <returns></returns>
        public int GetActualBuildTime()
        {
            if (StartBuildTime > 0 && EndBuildTime > 0)
            {
                if(EndBuildQueueTime>0)
                {
                    return (int)(EndBuildTime - EndBuildQueueTime);
                }
                else
                {
                    return (int)(EndBuildTime - StartBuildTime);
                }
            }
            return 0;
        }

        /// <summary>
        /// 获取实际launch时间（失败返回0）
        /// </summary>
        /// <returns></returns>
        public int GetActualLaunchTime()
        {
            if (StartLaunchTime > 0 && EndLaunchTime > 0)
                return (int)(EndLaunchTime - StartLaunchTime);
            return 0;
        }

        /// <summary>
        /// 获取实际更新总时间（失败返回0）
        /// </summary>
        /// <returns></returns>
        public int GetActualFinishTime()
        {
            if (StartDeploymentTime > 0 && EndDeploymentTime > 0)
                return (int)(EndDeploymentTime - StartDeploymentTime);
            return 0;
        }

        /// <summary>
        /// 获取实际更新总时间（字符串表示形式）
        /// </summary>
        /// <returns></returns>
        public string GetActualFinishTimeStr()
        {
            int tempFinishTime = GetActualFinishTime();
            if(tempFinishTime>0)
            {
                TimeSpan timeSpan = new TimeSpan(0, 0, tempFinishTime);
                //如果直接使用Minutes 虽然也是整数，但超过60分钟就显示的不是预期了
                return $"{(int)timeSpan.TotalMinutes}分{timeSpan.Seconds}秒";
            }
            else
            {
                return "未完成";
            }

        }


    }
}
