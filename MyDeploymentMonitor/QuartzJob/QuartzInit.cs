using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MyDeploymentMonitor.QuartzJob
{
    class QuartzInit
    {
        public static async Task<bool> InitAsync()
        {
            try
            {
                ISchedulerFactory sf = new StdSchedulerFactory();
                IScheduler scheduler = await sf.GetScheduler();
                Console.WriteLine("start the schedule");
                await scheduler.Start();

                //0 0 * * * ? *   //"0 0/30 * * * ? *"
                Console.WriteLine("creat trigger");
                ITrigger trigger = TriggerBuilder.Create()
                            .WithCronSchedule(ShareData.MyConfiguration.UserConf.Scheduler.Cron)
                            //.WithSimpleSchedule(x => x.WithIntervalInSeconds(2).RepeatForever())
                            .Build();

                Console.WriteLine("creat job");
                IJobDetail jobDetail = JobBuilder.Create<DeploymentJob>()
                            .WithIdentity("job", "group")
                            .Build();

                await scheduler.Clear();
                await scheduler.ScheduleJob(jobDetail, trigger);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
    }
}
