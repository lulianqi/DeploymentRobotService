using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeploymentRobotService.DeploymentService.MyCommandLine
{
    public class CommandLineInfo
    {
        //cmd v1 v2 -o1 vo CommandName：‘cmd’ CommandArgument：‘v1 v2’ CommandOption：‘-o1 vo’
        public string CommandName { get; set; }
        public string CommandArgument { get; set; }
        public string[] CommandOption { get; set; }
    }

    public class CmdHelper
    {
        private const string optionSplit = " -";
        private const char argumentSplit = ' ';

        public static CommandLineInfo GetCommandInfo(string cmd)
        {
            if(string.IsNullOrEmpty(cmd) || cmd.Trim()=="")
            {
                return null;
            }

            string name = null;
            string argument = null;
            string[] options = null;

            //先提取可能出现引号的参数（如果参数中含有-，需要将参数放在引号里）
            if(cmd.Contains('"'))
            {
                int tempQuotationMarkStart = cmd.IndexOf('"');
                if(tempQuotationMarkStart>0 && cmd[tempQuotationMarkStart-1]==' ')
                {
                    int tempQuotationMarkEnd = cmd.IndexOf('"', tempQuotationMarkStart+1);
                    if(tempQuotationMarkEnd>0)
                    {
                        argument = (cmd.Substring(tempQuotationMarkStart + 1, tempQuotationMarkEnd - tempQuotationMarkStart-1)).Trim();
                        cmd = cmd.Remove(tempQuotationMarkStart, tempQuotationMarkEnd - tempQuotationMarkStart+1);
                    }
                }
            }

            if (cmd.Contains(optionSplit))
            {
                name = cmd.Substring(0, cmd.IndexOf(optionSplit)).Trim();
                options = cmd.Remove(0, cmd.IndexOf(optionSplit) + optionSplit.Length).Split(optionSplit, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < options.Length; i++)
                {
                    options[i] = "-" + options[i].TrimEnd();
                }
            }
            else
            {
                name = cmd.Trim();
            }

            if (name.Contains(argumentSplit))
            {
                string tempName = name;
                int tempCmdIndex = tempName.IndexOf(argumentSplit);
                name = tempName.Substring(0, tempCmdIndex);
                if (argument == null)
                {
                    argument = tempName.Remove(0, tempCmdIndex + 1).Trim();
                }
            }
            return new CommandLineInfo() { CommandName = name, CommandArgument = argument, CommandOption = options };
        }
    
        public static string[] GetArgumentAndOption(CommandLineInfo yourCommandLineInfo)
        {
            if(yourCommandLineInfo.CommandOption == null || yourCommandLineInfo.CommandOption.Length==0)
            {
                return yourCommandLineInfo.CommandArgument == null ? null : new string[] { yourCommandLineInfo.CommandArgument };
            }
            else
            {
                if(string.IsNullOrEmpty(yourCommandLineInfo.CommandArgument))
                {
                    return yourCommandLineInfo.CommandOption;
                }
                else
                {
                    //Array.Resize(ref yourCommandLineInfo.CommandOption, yourCommandLineInfo.CommandOption.Length + 1);
                    string[] argumentAndOptionArr = new string[yourCommandLineInfo.CommandOption.Length + 1];
                    Array.Copy(yourCommandLineInfo.CommandOption,0, argumentAndOptionArr,1, yourCommandLineInfo.CommandOption.Length);
                    argumentAndOptionArr[0] = yourCommandLineInfo.CommandArgument;
                }
            }
            return default;
        }
    }
}
