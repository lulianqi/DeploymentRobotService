using MyDeploymentMonitor.ShareData;
using Quartz;
using System;
using System.Threading.Tasks;

namespace MyDeploymentMonitor.QuartzJob
{
    class DeploymentJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            if (MyConfiguration.UserConf.Scheduler.Projects != null && MyConfiguration.UserConf.Scheduler.Projects.Count > 0)
            {
                Console.WriteLine("-----------------------DeploymentJob Start-------------------------");
                foreach (var deploayJobs in MyConfiguration.UserConf.Scheduler.Projects)
                {
                   _ = DeploymentOne(deploayJobs.Key);
                }
                Console.WriteLine("-----------------------DeploymentJob End-------------------------");
            }
        }

        private async Task DeploymentOne(string bluidKey)
        {
            try
            {
                await ExecuteHelper.MyBuilder.BuildByKey(bluidKey);
                //await MyBambooMonitor.ExecuteHelper.MyExecuteMan.DeploymentForRancherAsync(true, "c-fj8rb:p-6czkl", "p-6czkl:p-f4tlv", "test");
                //await MyBambooMonitor.ExecuteHelper.MyExecuteMan.DeploymentForRancherAsync(true, "c-fj8rb:p-6czkl", "p-6czkl:p-vmvf9", "test-k8s");
                //await MyBambooMonitor.ExecuteHelper.MyExecuteMan.DeploymentForRancherAsync(true, "c-fj8rb:p-6czkl", "p-6czkl:p-wst2w", "test-k8s");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
