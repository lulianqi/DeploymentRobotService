using System;
namespace DeploymentRobotService.DeploymentService
{
    public class ExecuteToken
    {
        /// <summary>
        /// Token 创建者
        /// </summary>
        public string Owner { get;private set; }
        /// <summary>
        /// Token 指向的执行命令
        /// </summary>
        public string ExecuteCommand { get;private set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; private set; }
        /// <summary>
        /// 有效期
        /// </summary>
        public DateTime ExpirationTime { get;private set; }
        /// <summary>
        /// 是否过期（更新不一定即时，true 一定过期，false 可能过期）
        /// </summary>
        public bool IsExpiration { get; private set; } = false;
        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnable { get; set; } = true;
        /// <summary>
        /// 剩余可用次数
        /// </summary>
        public int ResidueCount { get; private set; }
        /// <summary>
        /// 总共可用次数
        /// </summary>
        public int TotalCount { get; private set; }


        public ExecuteToken(string command , int keepDay,int validDay , string owner)
        {
            ExecuteCommand = command;
            CreateTime = DateTime.Now;
            ExpirationTime = DateTime.Today.AddHours(24- DateTime.Today.Hour).AddDays(keepDay);//不同平台DateTime.Today.Hour不一样，为了跨平台要算一次
            TotalCount = ResidueCount = validDay;
            Owner = owner;
            IsEnable = true;
        }

        public string Execute()
        {
            if(IsEnable && !IsExpiration)
            {
                if(ExpirationTime.CompareTo(DateTime.Now)>0)
                {
                    if(ResidueCount>0)
                    {
                        ResidueCount--;
                        //Execute
                        return ExecuteCommand;
                    }

                }
                else
                {
                    IsExpiration = true;
                }
            }
            return null;
        }

        public override string ToString()
        {
            return $"ExecuteCommand : {ExecuteCommand}\r\nOwner : {Owner}\r\nCreateTime : {CreateTime}\r\nExpirationTime : {ExpirationTime}\r\nIsEnable : {IsEnable}\r\nTotalCount : {TotalCount}\r\nResidueCount : {ResidueCount}";
        }
    }
}
