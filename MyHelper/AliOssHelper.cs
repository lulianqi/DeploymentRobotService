using System;
using System.IO;
using System.Threading.Tasks;
using Aliyun.OSS;

namespace DeploymentRobotService.MyHelper
{
    public class AliOssHelper
    {
        public AliOssHelper()
        {
        }

        private static OssClient ossClient;

        static AliOssHelper()
        {
            ossClient = new OssClient(Appsetting.AliOssConfig.Endpoint, Appsetting.AliOssConfig.AccessKeyId, Appsetting.AliOssConfig.AccessKeySecret);
        }

        public static string PutFile(Stream fileStream, string fileNmae ,string ossPath= "wxci/file")
        {
            if(string.IsNullOrEmpty(ossPath))
            {
                ossPath = "wxci/file";
            }
            else
            {
                ossPath = ossPath.TrimEnd('/');
            }
            PutObjectResult putObjectResult= ossClient.PutObject("ai-crm-test", $"{ossPath}/{fileNmae}", fileStream);
            if(putObjectResult.HttpStatusCode == System.Net.HttpStatusCode.OK || putObjectResult.HttpStatusCode == System.Net.HttpStatusCode.Created)
            {
                Console.WriteLine($"Put object succeeded {fileNmae}");
                return $"{Appsetting.AliOssConfig.BaseFileUrl}/{ossPath}/{fileNmae}";
            }
            else
            {
                Console.WriteLine($"Put object fail {fileNmae} ");
                return putObjectResult.ToString();
            }
        }

    }
}
