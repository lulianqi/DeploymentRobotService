using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeploymentRobotService.MyHelper
{
    public static class MyExtendHelper
    {
        /// <summary>
        /// 以指定字符串拼合List<string>
        /// </summary>
        /// <param name="lsStr">目标对象</param>
        /// <param name="splitStr">分割字符串</param>
        /// <returns>返回数据</returns>
        public static string MyToString(this List<string> lsStr, string splitStr)
        {
            string outStr = null;
            if (lsStr != null)
            {
                if (lsStr.Count > 5)
                {
                    StringBuilder SbOutStr = new StringBuilder(lsStr.Count * ((lsStr[0].Length > lsStr[4].Length ? lsStr[0].Length : lsStr[1].Length) + splitStr.Length));
                    foreach (string tempStr in lsStr)
                    {
                        SbOutStr.Append(tempStr);
                        if (splitStr != null)
                        {
                            SbOutStr.Append(splitStr);
                        }
                    }
                    outStr = SbOutStr.ToString();
                }
                else
                {
                    foreach (string tempStr in lsStr)
                    {
                        if (splitStr != null)
                        {
                            outStr += (tempStr + splitStr);
                        }
                        else
                        {
                            outStr += tempStr;
                        }
                    }
                }
            }
            return outStr;
        }
        public static int ByteLeng(this string str ,Encoding encoding = null)
        {
            if (encoding == null) encoding = Encoding.UTF8;
            return encoding.GetBytes(str).Length;
        }
        public static string ToJson(this object obj)
        {
            return JsonHelper.SerializeJson<object>(obj);
        }

        public static T ToClassData<T>(this string jsonStr)
        {
            try
            {
                return JsonHelper.DeserializeJson<T>(jsonStr);
            }
            catch
            {
                return default(T);
            }
        }

    }
}
