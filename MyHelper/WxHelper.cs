using DeploymentRobotService.Models.WxModels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace DeploymentRobotService.MyHelper
{
    public class WxHelper
    {
        private static readonly HttpClient httpClient;
        static WxHelper()
        {
            httpClient = new HttpClient();
        }

        private string nowCorpId;
        private string nowCorpsecret;
        private int? nowAgentid;

        public string NowAccessToken { get; private set; }
        public WxHelper(string yourCorpId,string yourCorpsecret, int? yourAgentid = null)
        {
            nowCorpId = yourCorpId;
            nowCorpsecret = yourCorpsecret;
            nowAgentid = yourAgentid;
        }

        private async Task<AccessTokenIofo> GetToken()
        {
            HttpResponseMessage response = await httpClient.GetAsync(string.Format(@"https://qyapi.weixin.qq.com/cgi-bin/gettoken?corpid={0}&corpsecret={1}", nowCorpId, nowCorpsecret));
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch(Exception ex)
            {
                MyLogger.LogError("GetToken error", ex);
                return null;
            }
            string result = await response.Content.ReadAsStringAsync();
            return result.ToClassData<AccessTokenIofo>();
        }

        private async Task<bool> ReflushAccessTokenAsync()
        {
            AccessTokenIofo tempAccessTokenIofo = await GetToken();
            if (tempAccessTokenIofo != null && tempAccessTokenIofo.errcode == 0 && !string.IsNullOrEmpty(tempAccessTokenIofo.access_token))
            {
                NowAccessToken = System.Web.HttpUtility.HtmlEncode(tempAccessTokenIofo.access_token);
                return true;
            }
            else
            {
                NowAccessToken = null;
                return false;
            }
        }

        public string GetWxOauthRedirectUrl(string redirect_uri ,string state )
        {
            //http%3A%2F%2Fwx.lulianqi.com%2Fuser%2FWxOauth
            string redirectUrl = string.Format(@"https://open.weixin.qq.com/connect/oauth2/authorize?appid={0}&redirect_uri={1}&response_type=code&scope=snsapi_base&agentid={2}&state={3}#wechat_redirect", nowCorpId , HttpUtility.UrlEncode(redirect_uri), nowAgentid, HttpUtility.UrlEncode(state));
            return redirectUrl;
        }

        public async Task<T> GetWxBaseInfoAsync<T>(string url,bool isFilterErrcode = false , int retryTime = 1) where T:WxBaseInfo
        {
            Func<Task<T>> ReTrySendAsync = new Func<Task<T>>(async () =>
            {
                if (retryTime > 0)
                {
                    await Task.Delay(2000);
                    return await GetWxBaseInfoAsync<T>(url, isFilterErrcode, retryTime - 1);
                }
                else
                {
                    return default;
                }
            });

            if (NowAccessToken == null && !await ReflushAccessTokenAsync())
            {
                MyLogger.LogError("get AccessToken fail");
                return await ReTrySendAsync();
            }

            HttpResponseMessage response = await httpClient.GetAsync(string.Format("{0}&access_token={1}",url , NowAccessToken));
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                MyLogger.LogError("GetWxBaseInfoAsync error", ex);
                return await ReTrySendAsync();
            }
            string result = await response.Content.ReadAsStringAsync();
            T messageresponse = result.ToClassData<T>();
            if (messageresponse.errcode == 42001 || messageresponse.errcode == 40014 || messageresponse.errcode == 40001)
            {
                MyLogger.LogInfo("access_token expired and now will reflush accesstoken");
                NowAccessToken = null;
                return await ReTrySendAsync();
            }
            else if (messageresponse.errcode != 0 && isFilterErrcode)
            {
                return default;
            }
            else
            {
                return messageresponse;
            }
        }

        public async Task<Tuple<string,Stream>> GetWxTemporaryMedia(string mediaId, int retryTime = 1)
        {
            Func<Task<Tuple<string, Stream>>> ReTrySendAsync = new Func<Task<Tuple<string, Stream>>>(async () =>
            {
                if (retryTime > 0)
                {
                    await Task.Delay(2000);
                    return await GetWxTemporaryMedia(mediaId, retryTime - 1);
                }
                else
                {
                    return null;
                }
            });

            if (NowAccessToken == null && !await ReflushAccessTokenAsync())
            {
                MyLogger.LogError("get AccessToken fail");
                return await ReTrySendAsync();
            }

            HttpResponseMessage response = await httpClient.GetAsync(string.Format(@"https://qyapi.weixin.qq.com/cgi-bin/media/get?access_token={0}&media_id={1}", NowAccessToken, mediaId));
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                if(response.Content.Headers.ContentType.MediaType == "application/json")
                {
                    WxBaseInfo messageresponse = (await response.Content.ReadAsStringAsync()).ToClassData<WxBaseInfo>();
                    if (messageresponse.errcode == 42001 || messageresponse.errcode == 40014 || messageresponse.errcode == 40001)
                    {
                        MyLogger.LogInfo("access_token expired and now will reflush accesstoken");
                        NowAccessToken = null;
                        return await ReTrySendAsync();
                    }
                }

                string tempFileName = response.Content.Headers?.ContentDisposition?.FileName?.Trim('\"');
                if(string.IsNullOrEmpty(tempFileName))
                {
                    string tempMediaType = response.Content.Headers.ContentType?.MediaType??"unkonw";
                    int tempStartIndex = tempMediaType.LastIndexOf('/');
                    if(tempStartIndex>0)
                    {
                        tempMediaType = tempMediaType.Substring(tempStartIndex+1);
                        if(tempMediaType=="mpeg4")
                        {
                            tempMediaType = "mp4";
                        }
                    }
                    tempFileName = $"{DateTime.Now.ToString($"yyyy-MM-dd-hh-mm-ss-ffff")}.{tempMediaType}";
                }
                else if(tempFileName.StartsWith('.'))
                {
                    tempFileName = $"{DateTime.Now.ToString($"yyyy-MM-dd-hh-mm-ss-ffff")}.{tempFileName}";
                }
                else
                {
                    //Http head 只能支持ascii ，企微这里的中文其实是直接utf8 按ascii显示，一般http的库都会把head直接解析成string直接给开发者用，因为企微使用了utf8 ，标准库按RFC实现，用ascii转字符串
                    //还原时先把string还原成byte[]，不能直接使用ascii，因为ascii只用7位，那些乱码数据会用到8位，使用ascii转码数据会有损失
                    //所以还是使用unicode原成byte[]（string默认存储也正好用的unicode），因为原生数据用的ascii，所以高位字节一定是0x00，最后去掉这些0x00，重新用utf8转成string。
                    byte[] nameUnicodeBytes = System.Text.Encoding.Unicode.GetBytes(tempFileName);
                    byte[] nameAscBytes = new byte[tempFileName.Length];
                    for(int i=0; i< tempFileName.Length;i++)
                    {
                        nameAscBytes[i] = nameUnicodeBytes[i * 2];
                    }
                    tempFileName = System.Text.Encoding.UTF8.GetString(nameAscBytes);
                }
                //IEnumerable<string> contentHeaders;
                //if (response.Content.Headers.TryGetValues("Content-disposition", out contentHeaders))
                //{
                //    foreach (string disposition in contentHeaders)
                //    {
                //        int tempStartIndex = disposition.IndexOf("filename=\"");
                //        if (tempStartIndex >= 0)
                //        {
                //            tempStartIndex = tempStartIndex + 10;
                //            int tempEndIndex = disposition.IndexOf('"', tempStartIndex);
                //            tempFileName = disposition.Substring(tempStartIndex, tempEndIndex - tempStartIndex);
                //            break;
                //        }
                //    }
                //}
                return new Tuple<string, Stream>(tempFileName, await response.Content.ReadAsStreamAsync());
            }
            return default;
        }

        public async Task<string> GetUserInfoByCodeAsync(string code , int retryTime = 1)
        {
            Func<Task<string>> ReTrySendAsync = new Func<Task<string>>(async () =>
            {
                if (retryTime > 0)
                {
                    await Task.Delay(2000);
                    return await GetUserInfoByCodeAsync(code, retryTime - 1);
                }
                else
                {
                    return null;
                }
            });

            if (NowAccessToken == null && !await ReflushAccessTokenAsync())
            {
                MyLogger.LogError("get AccessToken fail");
                return await ReTrySendAsync();
            }

            HttpResponseMessage response = await httpClient.GetAsync(string.Format(@"https://qyapi.weixin.qq.com/cgi-bin/user/getuserinfo?access_token={0}&code={1}", NowAccessToken,code));
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                MyLogger.LogError("GetUserInfoByCodeAsync error", ex);
                return await ReTrySendAsync();
            }
            string result = await response.Content.ReadAsStringAsync();
            WxUserInfo messageresponse = result.ToClassData<WxUserInfo>();
            if (messageresponse.errcode == 0)
            {
                return messageresponse.UserId;
            }
            else if (messageresponse.errcode == 42001 || messageresponse.errcode == 40014 || messageresponse.errcode == 40001)
            {
                MyLogger.LogInfo("access_token expired and now will reflush accesstoken");
                NowAccessToken = null;
                return await ReTrySendAsync();
            }
            else
            {
                MyLogger.LogError(messageresponse.errcode + messageresponse.errmsg ?? "GetUserInfoByCodeAsync fail");
                return await ReTrySendAsync();
            }
        }

        public async Task<bool> SendMessageAsync(string touser, string content, int retryTime = 1, int? agentid = null)
        {
            Func<Task<bool>> ReTrySendAsync = new Func<Task<bool>>(async () =>
            {
                if (retryTime > 0)
                {
                    await Task.Delay(2000);
                    return await SendMessageAsync(touser, content, retryTime - 1, agentid);
                }
                else
                {
                    return false;
                }
             });

            WxTextMessageInfo wxTextMessageInfo = new WxTextMessageInfo() {
                touser = touser,
                text = new TextContent() { content = content },
                agentid = agentid == null ? nowAgentid : agentid ,
                msgtype= "text",
            };
            if(NowAccessToken==null && !await ReflushAccessTokenAsync())
            {
                MyLogger.LogError("get AccessToken fail");
                return await ReTrySendAsync();
            }
            HttpResponseMessage response = await httpClient.PostAsync(string.Format(@"https://qyapi.weixin.qq.com/cgi-bin/message/send?access_token={0}",NowAccessToken), new StringContent(wxTextMessageInfo.ToJson(), Encoding.UTF8, "application/json"));
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                MyLogger.LogError("SendMessageAsync error", ex);
                return await ReTrySendAsync();
            }
            string result = await response.Content.ReadAsStringAsync();
            SendMessageResponseInfo messageresponse = result.ToClassData<SendMessageResponseInfo>();
            if(messageresponse.errcode==0)
            {
                return true;
            }
            else if (messageresponse.errcode == 42001 || messageresponse.errcode == 40014 || messageresponse.errcode == 40001)
            {
                MyLogger.LogInfo("access_token expired and now will reflush accesstoken");
                NowAccessToken = null;
                return await ReTrySendAsync();
            }
            else
            {
                MyLogger.LogError(messageresponse.errcode + messageresponse.errmsg?? "SendMessageAsync fail");
                return await ReTrySendAsync();
            }
        }
    }
}
