using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeploymentRobotService.DeploymentService.MyCommandLine
{
    public class MyCmd
    {

        /// <summary>
        /// [list]
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static CmdResult GetRunners(string[] args)
        {
            string result = null;
            bool isExecuted = false;
            var app = new CommandLineApplication();
            app.Description = "获取正在发布的项目列表";
            app.ExtendedHelpText = "命令行模式默认显示最近9条发布记录，可以通过-n指定最大数量";
            CommandOption myHelp = app.HelpOption();
            //var optionSubject = app.Option<int>("-n|--subject <SUBJECT>", "The subject", CommandOptionType.SingleOrNoValue).Accepts().Range(1,10000);
            var number = app.Option<int>("-n|--number <number>", "the max acount of runners", CommandOptionType.SingleOrNoValue);
            var userName = app.Option<string>("-u|--user <user>", "user name", CommandOptionType.SingleOrNoValue);
            var projectKey = app.Option<string>("-k|--project <project>", "project key", CommandOptionType.SingleOrNoValue);
            var projectName = app.Option<string>("-p|--project <project>", "project name (like)", CommandOptionType.SingleOrNoValue);
            var runState = app.Option<DeploymentRunerState?>("-s|--state <state>", "run state", CommandOptionType.SingleOrNoValue);
            //var temp= app.Argument<string>("vaule", "des", true);


            app.OnExecute(() =>
            {
                isExecuted = true;
                //var xx = number.Accepts().Range(1, 10000);
                var runers = ApplicationRobot.NowDeploymentQueue.GetRunerList(userName.ParsedValue, projectKey.ParsedValue, projectName.ParsedValue, runState.ParsedValue, number.ParsedValue);
                if (runers != null & runers.Count > 0)
                {
                    StringBuilder runersSb = new StringBuilder();
                    foreach (var runer in runers)
                    {
                        runersSb.AppendLine(runer.ToString());
                    }
                    result = runersSb.ToString();
                }
                return 0;
            });

            try
            {
                if ( app.Execute(args) == 0)
                {
                    CmdResultState cmdResultState = CmdResultState.Succeed;
                    if (string.IsNullOrEmpty(result) &&  myHelp.HasValue())
                    {
                        result = app.GetHelpText();
                        cmdResultState = CmdResultState.Help;
                    }
                    return new CmdResult() {  ResultState= cmdResultState, ResultText=result};
                }
                else
                {
                    return new CmdResult() { ResultState = CmdResultState.Fail, ResultText = "execute command fail" };
                }
            }
            //catch(McMaster.Extensions.CommandLineUtils.UnrecognizedCommandParsingException ex)
            //{
            //    Console.WriteLine(app.GetValidationResult()?.ToString());
            //}
            catch (Exception ex)
            {
                Console.WriteLine();
                return new CmdResult() { ResultState = CmdResultState.Error, ResultText = ex.Message };
            }
        }

        /// <summary>
        /// [show]
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static CmdResult GetProjects(string[] args)
        {
            string result = null;
            bool isExecuted = false;
            var app = new CommandLineApplication();
            app.Description = "获取发布列表";
            app.ExtendedHelpText = "默认显示显示KubeSphere项目即(show -ks)\n以.开头可以以缺省的方式快速执行该命令(.crm 与 show -ks -p crm 是等价的)";
            CommandOption myHelp = app.HelpOption();
            //myHelp.Description = "ExtendedHelpText";
            var projectName = app.Option<string>("-p|--project <project>", "project name (like)", CommandOptionType.SingleOrNoValue);
            var isShowKubeSphereProjects = app.Option("-k|--kubeSphere <kubeSphere>", "is show kubeSphereProjects", CommandOptionType.NoValue);
            var isShowBambooProjects = app.Option("-b|--bamboo <bamboo>", "is show bambooProjects", CommandOptionType.NoValue);
            var isShowRancherProjects = app.Option("-r|--rancher <rancher>", "is show rancherProjects", CommandOptionType.NoValue);
            var isShowScanProjects = app.Option("-s|--scan <scan>", "is show scan projects", CommandOptionType.NoValue);


            app.OnExecute(() =>
            {
                isExecuted = true;
                if (!isShowKubeSphereProjects.HasValue() && !isShowBambooProjects.HasValue() && !isShowRancherProjects.HasValue() && !isShowScanProjects.HasValue())
                {
                    //private/saas
                    result = MyDeploymentMonitor.ExecuteHelper.MyBuilder.ShowProjects(true,false,false,true, projectName.ParsedValue);
                    //result = MyDeploymentMonitor.ExecuteHelper.MyBuilder.ShowProjects(false, true, false, false, projectName.ParsedValue);
                }
                else
                {
                    result = MyDeploymentMonitor.ExecuteHelper.MyBuilder.ShowProjects(isShowKubeSphereProjects.HasValue(), isShowBambooProjects.HasValue(), isShowRancherProjects.HasValue(), isShowScanProjects.HasValue(), projectName.ParsedValue);
                }
                return 0;
            });

            try
            {
                if (app.Execute(args) == 0)
                {
                    CmdResultState cmdResultState = CmdResultState.Succeed;
                    if (string.IsNullOrEmpty(result) && myHelp.HasValue())
                    {
                        result = app.GetHelpText();
                        cmdResultState = CmdResultState.Help;
                    }
                    return new CmdResult() { ResultState = cmdResultState, ResultText = result };
                }
                else
                {
                    return new CmdResult() { ResultState = CmdResultState.Fail, ResultText = "execute command fail" };
                }
            }
            //catch(McMaster.Extensions.CommandLineUtils.UnrecognizedCommandParsingException ex)
            //{
            //    Console.WriteLine(app.GetValidationResult()?.ToString());
            //}
            catch (Exception ex)
            {
                Console.WriteLine();
                return new CmdResult() { ResultState = CmdResultState.Error, ResultText = ex.Message };
            }
        }

        /// <summary>
        /// [init]
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static CmdResult InitDeploymentMonitor(string[] args)
        {
            string result = null;
            bool isExecuted = false;
            var app = new CommandLineApplication();
            app.Description = "重新加载配置";
            app.ExtendedHelpText = "当发布列表有更新时可以通过init重新获取新列表";
            CommandOption myHelp = app.HelpOption();
            

            app.OnExecute(() =>
            {
                isExecuted = true;
                result = "开始初始化";
                return 0;
            });

            try
            {
                if (app.Execute(args) == 0)
                {
                    CmdResultState cmdResultState = CmdResultState.Succeed;
                    if (string.IsNullOrEmpty(result) && myHelp.HasValue())
                    {
                        result = app.GetHelpText();
                        cmdResultState = CmdResultState.Help;
                    }
                    return new CmdResult() { ResultState = cmdResultState, ResultText = result };
                }
                else
                {
                    return new CmdResult() { ResultState = CmdResultState.Fail, ResultText = "execute command fail" };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                return new CmdResult() { ResultState = CmdResultState.Error, ResultText = ex.Message };
            }
        }

        /// <summary>
        /// [pwd]
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static CmdResult GetPassword(string[] args)
        {
            string result = null;
            bool isExecuted = false;
            var app = new CommandLineApplication();
            app.Description = "解密CRM登录密码";
            app.ExtendedHelpText = "解密CRM登录密码 命令参数为密码秘文";
            CommandOption myHelp = app.HelpOption();
            var cipherdate = app.Argument<string>("cipherdate", "cipherdate in database", true);

            app.OnExecute(() =>
            {
                isExecuted = true;

                try
                {
                    byte[] bytes = MyCommonHelper.EncryptionHelper.MyAES.Decrypt(
                        Convert.FromBase64String(cipherdate.ParsedValue),
                        Encoding.UTF8.GetBytes("TaOXLwfQgDhFrqDC"),
                        Encoding.UTF8.GetBytes("70699475E1B6A03461E95F5E9D793E71".Substring(8, 16)),
                        System.Security.Cryptography.CipherMode.CBC, System.Security.Cryptography.PaddingMode.None);
                    result = Encoding.UTF8.GetString(bytes).TrimEnd((char)0x00);
                }
                catch (Exception ex)
                {
                    result = $"解密失败:{ex.ToString()}";
                }
                return 0;
            });

            try
            {
                if (app.Execute(args) == 0)
                {
                    CmdResultState cmdResultState = CmdResultState.Succeed;
                    if (string.IsNullOrEmpty(result) && myHelp.HasValue())
                    {
                        result = app.GetHelpText();
                        cmdResultState = CmdResultState.Help;
                    }
                    return new CmdResult() { ResultState = cmdResultState, ResultText = result };
                }
                else
                {
                    return new CmdResult() { ResultState = CmdResultState.Fail, ResultText = "execute command fail" };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                return new CmdResult() { ResultState = CmdResultState.Error, ResultText = ex.Message };
            }
        }

        /// <summary>
        /// [run]
        /// </summary>
        /// <param name="args"></param>
        /// <param name="nowRuner"></param>
        /// <returns></returns>
        public static CmdResult RunBuildEx(string[] args , DeploymentRuner nowRuner, IRobotConnector nowRobot)
        {
            string result = null;
            bool isExecuted = false;
            var app = new CommandLineApplication();
            app.Description = "执行发布命令";
            app.ExtendedHelpText = "run 命令为特殊缺省命令，默认可以不输入直接使用项目名称执行 即run -p a 与 a 是等价的（确保a不是其他命令名称）";
            CommandOption myHelp = app.HelpOption();
            var projectKey = app.Option<string>("-p|--project <project>", "project name key", CommandOptionType.SingleValue);
            var isForceBuild = app.Option("-f|--force <force>", "is force build", CommandOptionType.NoValue);
            var isCancelBuild = app.Option("-c|--cancel <cancel>", "cancel running runs", CommandOptionType.NoValue);
            var environment = app.Option<string>("-e|--environment <environment>", "environment [test/pre]", CommandOptionType.SingleOrNoValue);

            app.OnExecute(() =>
            {
                isExecuted = true;
                if (isCancelBuild.HasValue())
                {
                    _ = nowRuner.CancelBuildAsync();
                    if (!isForceBuild.HasValue())
                    {
                        result =  "Canceling" ;
                        return 0;
                    }
                }
                _ = nowRuner.BuildAsync(nowRobot,isForceBuild.HasValue(), environment.HasValue()? environment.ParsedValue:null);
                result = "Running";
                return 0;
            });

            try
            {
                if (app.Execute(args) == 0)
                {
                    CmdResultState cmdResultState = CmdResultState.Succeed;
                    if (string.IsNullOrEmpty(result) && myHelp.HasValue())
                    {
                        result = app.GetHelpText();
                        cmdResultState = CmdResultState.Help;
                    }
                    return new CmdResult() { ResultState = cmdResultState, ResultText = result };
                }
                else
                {
                    return new CmdResult() { ResultState = CmdResultState.Fail, ResultText = "execute command fail" };
                }
            }
            //catch(McMaster.Extensions.CommandLineUtils.UnrecognizedCommandParsingException ex)
            //{
            //    Console.WriteLine(app.GetValidationResult()?.ToString());
            //}
            catch (Exception ex)
            {
                Console.WriteLine();
                return new CmdResult() { ResultState = CmdResultState.Error, ResultText = ex.Message };
            }
        }

        /// <summary>
        /// [token]
        /// </summary>
        /// <param name="args"></param>
        /// <param name="nowRuner"></param>
        /// <returns></returns>
        public static CmdResult HandleToken(string[] args,string commandArgumentStr ,string owner,ExecuteTokenDevice executeTokenDevice)
        {
            string result = null;
            bool isExecuted = false;
            var app = new CommandLineApplication();
            app.Description = "临时发布Token操作";
            app.ExtendedHelpText = "临时发布Token的创建/禁用/查询 ，-l用于查询，-r用于禁用，默认为创建（-d及-t仅对创建生效）";
            CommandOption myHelp = app.HelpOption();
            var isList = app.Option("-l|--list <list>", "list all token", CommandOptionType.SingleOrNoValue);
            var isRemove = app.Option<string>("-r|--remove <remove>", "remove token", CommandOptionType.SingleValue);
            var validDay = app.Option<int>("-d|--day <day>", "valid time   0:That day  n:n day", CommandOptionType.SingleValue);
            var validTimes = app.Option<int>("-t|--times <times>", "max times can use of this token", CommandOptionType.SingleValue);

            app.OnExecute(() =>
            {
                isExecuted = true;
                if(isList.HasValue())
                {
                    result = executeTokenDevice.ToString(isList.Value());
                    if(string.IsNullOrEmpty(result))
                    {
                        result = "您还未创建任何发布Token";
                    }
                    return 0;
                }
                if (isRemove.HasValue())
                {
                    if(string.IsNullOrEmpty(isRemove.ParsedValue))
                    {
                        result = "just input token";
                        return -1;
                    }
                    bool isDel = executeTokenDevice.DisableExecuteToken(isRemove.ParsedValue);
                    if(isDel)
                    {
                        result = "success";
                        return 0;
                    }
                    else
                    {
                        result = "disable fail";
                        return -1;
                    }
                }
                if(string.IsNullOrEmpty(commandArgumentStr))
                {
                    result = "can not find command ,if you want creat token ,just try [token hi] hi is your command";
                    return -1;
                }
                string tempToken = executeTokenDevice.CreateExecuteToken(commandArgumentStr, validDay.ParsedValue, validTimes.ParsedValue>0? validTimes.ParsedValue:1, owner);
                result = tempToken;
                if (tempToken?.Length == 32)
                {
                    return 1;
                }
                return 0;
            });

            try
            {
                int tempExecuteResult = app.Execute(args);
                if (tempExecuteResult == 0 )
                {
                    CmdResultState cmdResultState = CmdResultState.Succeed;
                    if (string.IsNullOrEmpty(result) && myHelp.HasValue())
                    {
                        result = app.GetHelpText();
                        cmdResultState = CmdResultState.Help;
                    }
                    return new CmdResult() { ResultState = cmdResultState, ResultText = result };
                }
                else if (tempExecuteResult == 1) // 这里的返回值是OnExecute函数决定的（所有可能的值都是在OnExecute控制的）
                {
                    CmdResultState cmdResultState = CmdResultState.Succeed;
                    if (string.IsNullOrEmpty(result) && myHelp.HasValue())
                    {
                        result = app.GetHelpText();
                        cmdResultState = CmdResultState.Help;
                    }
                    return new CmdResult() { ResultState = cmdResultState, ResultText = result ,Flag="token" };
                }
                else
                {
                    return new CmdResult() { ResultState = CmdResultState.Fail, ResultText = $"execute command fail :{result}" };
                }
            }
            //catch(McMaster.Extensions.CommandLineUtils.UnrecognizedCommandParsingException ex)
            //{
            //    Console.WriteLine(app.GetValidationResult()?.ToString());
            //}
            catch (Exception ex)
            {
                Console.WriteLine();
                return new CmdResult() { ResultState = CmdResultState.Error, ResultText = ex.Message };
            }
        }

    }
}
