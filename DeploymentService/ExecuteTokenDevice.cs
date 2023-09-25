using DeploymentRobotService.Appsetting;
using DeploymentRobotService.DeploymentService.MyCommandLine;
using DeploymentRobotService.MyHelper;
using MyDeploymentMonitor.ExecuteHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeploymentRobotService.DeploymentService
{
    public class ExecuteTokenDevice
    {
        Dictionary<string, ExecuteToken> executeTokenDc;
        private const int _maxExecuteTokenCount = 1000;
        private Func<IRobotConnector ,string, string, CommandInfo> _executeCmdFunc;

        public ExecuteTokenDevice(Func<IRobotConnector ,string, string, CommandInfo> ExecuteCmdFunc = null)
        {
            executeTokenDc = new Dictionary<string, ExecuteToken>();
            _executeCmdFunc = ExecuteCmdFunc;
        }

        private void ClearDictionary()
        {
            foreach(var token in executeTokenDc)
            {
                if(token.Value.IsExpiration)
                {
                    executeTokenDc.Remove(token.Key);
                }
            }
            if(executeTokenDc.Count> _maxExecuteTokenCount*0.8)
            {
                executeTokenDc.Clear();
                Console.WriteLine("trigger _maxExecuteTokenCount*0.8 =》executeTokenDc.Clear()");
            }
        }

        public string CreateExecuteToken(string command, int keepDay, int validDay, string owner)
        {
            if (keepDay > 6 || keepDay < 0)
            {
                return "请调整有效期到7天以内";
            }
            if(string.IsNullOrEmpty(command))
            {
                return "未发现有效的Token命令";
            }
            string token = Guid.NewGuid().ToString("N");
            if(executeTokenDc.Count>_maxExecuteTokenCount)
            {
                ClearDictionary();
            }
            executeTokenDc.Add(token, new ExecuteToken(command, keepDay, validDay,owner));
            return token;
        }

        public bool DisableExecuteToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentNullException("token");
            }
            if (executeTokenDc.ContainsKey(token) && executeTokenDc[token].IsEnable)
            {
                executeTokenDc[token].IsEnable = false;
                return true;
            }
            return false;
        }

        public CommandInfo ExecuteToken(IRobotConnector nowRobot,string token,string user="token", Func<IRobotConnector,string, string, CommandInfo> ExecuteCmdFunc=null)
        {
            if (ExecuteCmdFunc==null)
            {
                ExecuteCmdFunc =  _executeCmdFunc;
                if(ExecuteCmdFunc==null)
                    throw new ArgumentNullException("ExecuteCmdFunc");
            }
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentNullException("token");
            }
            if(!executeTokenDc.ContainsKey(token))
            {
                return new CommandInfo() 
                {
                    NowCommandType = CommandType.ToolkitCommand,
                    CommandReply = "非法的执行Token" 
                };
            }
            string nowCommand = executeTokenDc[token].Execute();
            CommandInfo commandInfo;
            if (string.IsNullOrEmpty(nowCommand))
            {
                commandInfo = new CommandInfo()
                {
                    NowCommandType = CommandType.ToolkitCommand,
                    CommandReply = "构建Token已过期或已被禁用"
                };
            }
            else
            {
                commandInfo = ExecuteCmdFunc(nowRobot ,nowCommand, user);
            }
            if (commandInfo.NowCommandType == CommandType.DeploymentCommand)
            {
                commandInfo.CommandReply = $"{commandInfo.CommandReply}\n🚀{{ProjectName}}\n\n{executeTokenDc[token].ToString()}";
            }
            else
            {
                commandInfo.CommandReply = $"{commandInfo.CommandReply}\n\n{executeTokenDc[token].ToString()}";
            }
            return commandInfo;
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach(var token in executeTokenDc)
            {
                stringBuilder.AppendLine($"🔰{token.Key}\r\n{token.Value}");
            }
            return stringBuilder.ToString();
        }

        public  string ToString(string searchKey)
        {
            if(string.IsNullOrEmpty(searchKey))
            {
                return ToString();
            }
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var token in executeTokenDc)
            {
                if (token.Value.Owner.Contains(searchKey) || token.Value.ExecuteCommand.Contains(searchKey) || token.Key.Contains(searchKey))
                {
                    stringBuilder.AppendLine($"🔰{token.Key}\r\n{token.Value}");
                }
            }
            return stringBuilder.ToString();
        }
    }
}
