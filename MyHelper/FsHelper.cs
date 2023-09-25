using DeploymentRobotService.Models.FsModels;
using DeploymentRobotService.Models.FsModels.MessageData;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
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
    public class FsHelper
    {
        public class JsSdkTicketManager
        {
            private string _ticket = null;
            private FsHelper _fsHelper;
            private DateTime _expireTime = DateTime.Now;

            public JsSdkTicketManager(FsHelper fsHelper)
            {
                _fsHelper = fsHelper;
            }

            public async ValueTask<string> GetTicket(bool ForceNewTicket = false)
            {
                if(string.IsNullOrEmpty(_ticket) || (_expireTime - DateTime.Now).TotalMinutes<25 || ForceNewTicket)
                {
                    FsBaseInfo<AccessTicketIofo> accessTicketIofo = await _fsHelper.SendFsBaseRequestAsync<FsBaseInfo<AccessTicketIofo>>("open-apis/jssdk/ticket/get", false, null, 2);
                    if (accessTicketIofo == null || accessTicketIofo.code != 0)
                    {
                        MyLogger.LogError($"[GetTicket] fail {accessTicketIofo.msg}");
                        _ticket = null;
                    }
                    else
                    {
                        _ticket = accessTicketIofo.data.ticket;
                        _expireTime = DateTime.Now.AddSeconds(double.Parse(accessTicketIofo.data.expire_in));
                    }
                }
                return _ticket;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="url">当前网页的 URL（可以为本地局域网网址），不包含 # 及其后面部分</param>
            /// <returns></returns>
            public async ValueTask<string> GetWebSignature(string url ,string timestamp, string nonceStr = "lulianqi")
            {
                string verifyStr = $"jsapi_ticket={await GetTicket()}&noncestr={nonceStr}&timestamp={timestamp}&url={url}";
                string shaSign = null;
                using (System.Security.Cryptography.SHA1 sha1 = System.Security.Cryptography.SHA1.Create())
                {
                    shaSign = BitConverter.ToString(sha1.ComputeHash(Encoding.UTF8.GetBytes(verifyStr))).Replace("-", "");
                }
                return shaSign.ToLower();
            }
        }

        private readonly HttpClient httpClient;
        private StringContent oauthContent;
        private string nowAppSecret;
        public string NowAppId { get; private set; }
        public JsSdkTicketManager JsTicketManager { get; private set; }
        public FsHelper(string apiBaseUrl, string appId, string appSecret)
        {
            httpClient = new HttpClient() { BaseAddress = new Uri(apiBaseUrl) };
            NowAppId = appId;
            nowAppSecret = appSecret;
            oauthContent = new StringContent($"{{\"app_id\": \"{NowAppId}\",\"app_secret\": \"{nowAppSecret}\"}}",Encoding.UTF8, "application/json");
            JsTicketManager = new JsSdkTicketManager(this);
        }

        public long GetTimeStamp()
        {
            long nowTimeStamp = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000;
            return nowTimeStamp;
        }

        private async Task<bool> GetToken()
        {
            HttpResponseMessage response = await httpClient.PostAsync("open-apis/auth/v3/tenant_access_token/internal", oauthContent);
            string result = await response.Content.ReadAsStringAsync();
            AccessTokenIofo accessTokenIofo = result.ToClassData<AccessTokenIofo>();
            if(accessTokenIofo.code !=0)
            {
                MyLogger.LogError($"[GetToken] code error \r\n{result}" );
                httpClient.DefaultRequestHeaders.Authorization = null;
                return false;
            }
            // fs http 400 ,也会有业务代码，EnsureSuccessStatusCode要后置
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                MyLogger.LogError("GetToken error", ex);
                httpClient.DefaultRequestHeaders.Authorization = null;
                return false;
            }
            MyLogger.LogInfo($"[GetToken] get new token \r\n{accessTokenIofo.tenant_access_token}");
            httpClient.DefaultRequestHeaders.Authorization= new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessTokenIofo.tenant_access_token);
            return true;
        }

        /// <summary>
        /// 推测UserIdType
        /// </summary>
        /// <param name="userId">user id</param>
        /// <returns>UserIdType</returns>
        private string GetUserIdType(string userId)
        {
            string receive_id_type;
            if (userId.StartsWith("ou_"))
            {
                receive_id_type = "open_id";
            }
            else if (userId.StartsWith("on_"))
            {
                receive_id_type = "union_id";
            }
            else if (userId.StartsWith("oc_"))
            {
                receive_id_type = "chat_id";
            }
            else if (userId.Contains("@"))
            {
                receive_id_type = "email";
            }
            else
            {
                receive_id_type = "user_id";
            }
            return receive_id_type;
        }

        public string GetFsOauthRedirectUrl(string redirect_uri, string state)
        {
            //https://open.feishu.cn/open-apis/authen/v1/index?redirect_uri={REDIRECT_URI}&app_id={APPID}&state={STATE}
            string redirectUrl = $"https://open.feishu.cn/open-apis/authen/v1/index?redirect_uri={HttpUtility.UrlEncode(redirect_uri)}&app_id={NowAppId}&state={HttpUtility.UrlEncode(state)}";
            return redirectUrl;
        }
        /// <summary>
        /// 基础请求
        /// </summary>
        /// <typeparam name="T">返回Data数据类型</typeparam>
        /// <param name="url">url</param>
        /// <param name="isFilterErrcode">是否过滤messageresponse.code，如果过滤当code不为0时进行retry逻辑</param>
        /// <param name="httpContent"></param>
        /// <param name="retryTime"></param>
        /// <param name="httpMethod"></param>
        /// <returns>返回数据，重试都失败可能返回null</returns>
        private async Task<T> SendFsBaseRequestAsync<T>(string url, bool isFilterErrcode = false, HttpContent httpContent=null , int retryTime = 1 , HttpMethod httpMethod = null) where T : FsBaseInfo
        {
            T messageresponse = null;

            Func<Task<T>> ReTrySendAsync = new Func<Task<T>>(async () =>
            {
                if (retryTime > 0)
                {
                    await Task.Delay(1000);
                    return await SendFsBaseRequestAsync<T>(url, isFilterErrcode, httpContent, retryTime - 1, httpMethod);
                }
                else
                {
                    //default;
                    return messageresponse;
                }
            });

            if (httpClient.DefaultRequestHeaders.Authorization == null && !await GetToken())
            {
                MyLogger.LogError("[SendFsBaseRequestAsync] get AccessToken fail");
                return await ReTrySendAsync();
            }

            HttpResponseMessage response = null;
            if(httpMethod !=null)
            {
                HttpRequestMessage httpRequestMessage = new HttpRequestMessage(httpMethod, url);
                if (httpContent != null)
                {
                    httpRequestMessage.Content = httpContent;
                }
                response = await httpClient.SendAsync(httpRequestMessage);
            }
            else if (httpContent == null)
            {
                response = await httpClient.GetAsync(url);
            }
            else
            {
                response = await httpClient.PostAsync(url,httpContent);
            }
            string result = await response.Content.ReadAsStringAsync();
            //MyLogger.LogInfo($"[SendFsBaseRequestAsync] response \r\n{result}");
            //如果NET版本高可以直接await httpClient.GetFromJsonAsync<Stock> 
            messageresponse = result.ToClassData<T>();
            if(messageresponse==null)//如result是"404 page not found"这种不是json格式反序列化出来就会为null
            {
                MyLogger.LogInfo($"[SendFsBaseRequestAsync] >ReTrySendAsync \r\n{result}");
                return await ReTrySendAsync();
            }
            if (messageresponse.code == 4001 || messageresponse.code == 99991663 || (messageresponse.msg??"").Contains("token invalid"))
            {
                MyLogger.LogInfo("[SendFsBaseRequestAsync] access_token expired and now will reflush accesstoken");
                httpClient.DefaultRequestHeaders.Authorization = null;
                return await ReTrySendAsync();
            }
            else if (messageresponse.code != 0 && isFilterErrcode)
            {
                MyLogger.LogInfo($"[SendFsBaseRequestAsync] >ReTrySendAsync \r\n{result}");
                return await ReTrySendAsync();
            }
            else
            {
                //因为http code 为400段，也会有实际含义业务code，所以不能在前面EnsureSuccessStatusCode
                //如果遇到没有业务code的返回，messageresponse.code默认会为0，所以要在最后的地方再EnsureSuccessStatusCode
                try
                {
                    response.EnsureSuccessStatusCode();
                }
                catch (Exception ex)
                {
                    MyLogger.LogError($"[SendFsBaseRequestAsync] error [{url}]\r\n[{(await httpContent?.ReadAsStringAsync())?? "null httpContent"}]\r\n{result}\r\n", ex);
                    return await ReTrySendAsync();
                }
                return messageresponse;
            }
        }

        /// <summary>
        /// 基础请求 (获取Fs的items数组，如果超过一页会主动取到最后一页为止只能用于飞书)
        /// 使用时不要url不要带page_token查询字符串，函数内部会处理page
        /// 返回数据不会为null（即使失败也会有返回0长度的List）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <param name="isFilterErrcode"></param>
        /// <param name="httpContent"></param>
        /// <param name="retryTime"></param>
        /// <returns></returns>
        private async Task<List<T>> GetFsItemAsync<T>(string url, bool isFilterErrcode = false, HttpContent httpContent = null, int retryTime = 1)
        {
            List<T> FsItemDataList = new List<T>();
            string page_token = "";
            string tempUriQueryStrncatString = url.Contains('?') ? "&" : "?";
            while (page_token != null)
            {
                FsBaseInfo<FsItemData<T>> tempItemDatas = await SendFsBaseRequestAsync<FsBaseInfo<FsItemData<T>>>($"{url}{tempUriQueryStrncatString}page_token={page_token}", isFilterErrcode, httpContent, retryTime);
                if (tempItemDatas == null)
                {
                    MyLogger.LogError("[GetFsItemAsync] error :tempItemDatas is null");
                    break;
                }
                if (tempItemDatas?.data?.items == null)
                {
                    MyLogger.LogInfo("[GetFsItemAsync] tempItemDatas?.data?.items is null");
                    break;
                }
                FsItemDataList.AddRange(tempItemDatas.data.items);
                page_token = null;
                if (tempItemDatas.data.has_more)
                {
                    page_token = tempItemDatas.data.page_token;
                    if (page_token == "") page_token = null;
                }
            }
            return FsItemDataList;
        }


        /// <summary>
        /// 获取单个用户信息
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<FsUserInfo> GetUserInfo(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException($"'{nameof(userId)}' cannot be null or empty.", nameof(userId));
            }
            string id_type = GetUserIdType(userId);
            FsBaseInfo<FsUserInfo> nowUserInfo = await SendFsBaseRequestAsync<FsBaseInfo<FsUserInfo>>($"open-apis/contact/v3/users/{userId}?user_id_type={id_type}", false, null, 2);
            return nowUserInfo?.data;
        }

        public async Task<string> GetUserIdByCodeAsync(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentException($"'{nameof(code)}' cannot be null or empty.", nameof(code));
            }
            StringContent sendMesContent = new StringContent($"{{\"grant_type\": \"authorization_code\",\"code\": \"{code}\"}}", Encoding.UTF8, "application/json");
            FsCommonInfo fsBaseInfo = await SendFsBaseRequestAsync<FsCommonInfo>("open-apis/authen/v1/access_token", false, sendMesContent, 2);
            if (fsBaseInfo == null || fsBaseInfo.code != 0)
            {
                MyLogger.LogError($"[GetUserIdByCodeAsync] fail");
            }
            JObject jo = fsBaseInfo.data as JObject;
            return jo?["user_id"]? .Value<string>(); ;
        }

        /// <summary>
        /// 通过电话或邮箱获取用户open id 
        /// </summary>
        /// <param name="e_mail"></param>
        /// <returns></returns>
        public async Task<string> GetOpenIdByMailOrMobileAsync(string e_mail)
        {
            JObject contentJo = new JObject();
            if(e_mail.Contains("@"))
            {
                contentJo.Add("emails", new JArray(e_mail));
            }
            else
            {
                contentJo.Add("mobiles", new JArray(e_mail));
            }
            StringContent sendMesContent = new StringContent(contentJo.ToString(), Encoding.UTF8, "application/json");
            FsCommonInfo fsBaseInfo = await SendFsBaseRequestAsync<FsCommonInfo>("open-apis/contact/v3/users/batch_get_id?user_id_type=open_id", false, sendMesContent, 2);
            if (fsBaseInfo == null || fsBaseInfo.code != 0)
            {
                MyLogger.LogError($"[GetOpenIdByMailAsync] fail");
            }
            JObject jo = fsBaseInfo.data as JObject;
            if(jo == null)
            {
                return null;
            }
            JArray ulArr = jo["user_list"] as JArray;
            if(ulArr == null || ulArr.Count==0)
            {
                return null;
            }
            return ulArr[0]?["user_id"]?.Value<string>();
        }

        /// <summary>
        /// 应用发送消息的统一入口
        /// </summary>
        /// <param name="touser">receive_id_type的值，填写对应的消息接收者id (use id/open id/union id、chat id 等可以使用的id都正常)</param>
        /// <param name="content"></param>
        /// <param name="msg_type">消息类型 包括：text、post、image、file、audio、media、sticker、interactive、share_chat、share_user等</param>
        /// <param name="retryTime"></param>
        /// <returns></returns>
        private async Task<bool> SendMessageAsync(string touser, string content ,string msg_type = "text", int retryTime = 1) 
        {
            if (string.IsNullOrEmpty(touser))
            {
                throw new ArgumentException($"'{nameof(touser)}' cannot be null or empty.", nameof(touser));
            }
            string receive_id_type = GetUserIdType(touser);
            FsSendMessageInfo fsSendMessageInfo = new FsSendMessageInfo() { msg_type = msg_type, receive_id = touser, content = content };
            StringContent sendMesContent = new StringContent(fsSendMessageInfo.ToJson(), Encoding.UTF8, "application/json");
            FsBaseInfo fsBaseInfo = await SendFsBaseRequestAsync<FsBaseInfo>($"open-apis/im/v1/messages?receive_id_type={receive_id_type}", false, sendMesContent, retryTime);
            if (fsBaseInfo == null || fsBaseInfo.code != 0)
            {
                MyLogger.LogError($"[SendMessageAsync] fail");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 发送文本消息
        /// </summary>
        /// <param name="touser"></param>
        /// <param name="content"></param>
        /// <param name="retryTime"></param>
        /// <returns></returns>
        public async Task<bool> SendTextMessageAsync(string touser, string content, int retryTime = 1)
        {
            JObject contentJo = new JObject();
            contentJo.Add("text", content);
            return await SendMessageAsync(touser, contentJo.ToString(),"text", retryTime);
        }

        /// <summary>
        /// 发送富文本消息
        /// </summary>
        /// <param name="touser"></param>
        /// <param name="postContent"></param>
        /// <param name="retryTime"></param>
        /// <returns></returns>
        public async Task<bool> SendPostMessageAsync(string touser, PostMessageZh postContent, int retryTime = 1)
        {
            JObject jo = (JObject)JToken.FromObject(postContent);
            return await SendMessageAsync(touser, jo.ToString(), "post", retryTime);
        }

        /// <summary>
        /// 发送卡片消息 (成功返回message_id，失败返回null)
        /// </summary>
        /// <param name="touser"></param>
        /// <param name="content"></param>
        /// <param name="retryTime"></param>
        /// <returns>message_id</returns>
        public async Task<string> SendInteractiveMessageAsync(string touser, string content, int retryTime = 1)
        {
            if (string.IsNullOrEmpty(touser))
            {
                throw new ArgumentException($"'{nameof(touser)}' cannot be null or empty.", nameof(touser));
            }
            string receive_id_type = GetUserIdType(touser);
            FsSendMessageInfo fsSendMessageInfo = new FsSendMessageInfo() { msg_type = "interactive", receive_id = touser, content = content };
            StringContent sendMesContent = new StringContent(fsSendMessageInfo.ToJson(), Encoding.UTF8, "application/json");
            FsBaseInfo<FsMessageResponseInfo> fsBaseInfo = await SendFsBaseRequestAsync<FsBaseInfo<FsMessageResponseInfo>>($"open-apis/im/v1/messages?receive_id_type={receive_id_type}", false, sendMesContent, retryTime);
            if (fsBaseInfo == null || fsBaseInfo.code != 0)
            {
                MyLogger.LogError($"[SendInteractiveMessageAsync] fail {fsBaseInfo.msg}");
                return null;
            }
            if(string.IsNullOrEmpty(fsBaseInfo.data.message_id))
            {
                MyLogger.LogError($"[SendInteractiveMessageAsync] fail ,can not get message_id");
                return null;
            }
            return fsBaseInfo.data.message_id;
        }

        /// <summary>
        /// 更新卡片消息
        /// </summary>
        /// <param name="touser"></param>
        /// <param name="message_id"></param>
        /// <param name="content"></param>
        /// <param name="retryTime"></param>
        /// <returns></returns>
        public async Task<string> UpdateInteractiveMessageAsync(string touser,string message_id, string content, int retryTime = 1)
        {
            if (string.IsNullOrEmpty(touser))
            {
                throw new ArgumentException($"'{nameof(touser)}' cannot be null or empty.", nameof(touser));
            }

            if (string.IsNullOrWhiteSpace(message_id))
            {
                throw new ArgumentException($"'{nameof(message_id)}' cannot be null or whitespace.", nameof(message_id));
            }

            string receive_id_type = GetUserIdType(touser);
            FsSendMessageInfo fsSendMessageInfo = new FsSendMessageInfo() { msg_type = "interactive", receive_id = touser, content = content };
            StringContent sendMesContent = new StringContent(fsSendMessageInfo.ToJson(), Encoding.UTF8, "application/json");
            FsBaseInfo fsBaseInfo = await SendFsBaseRequestAsync<FsBaseInfo>($"open-apis/im/v1/messages/{message_id}?receive_id_type={receive_id_type}", false, sendMesContent, retryTime,HttpMethod.Patch);
            if (fsBaseInfo == null || fsBaseInfo.code != 0)
            {
                MyLogger.LogError($"[UpdateInteractiveMessageAsync] fail {fsBaseInfo.msg}");
                return null;
            }
            return message_id;
        }

        /// <summary>
        /// 应用消息加急
        /// </summary>
        /// <param name="message_id"></param>
        /// <param name="user_id_list"></param>
        /// <returns></returns>
        public async ValueTask MessageUrgentAsync(string message_id ,params string[] user_id_list)
        {
            if(user_id_list?.Length==0)
            {
                return;
            }
            JObject contentJo = new JObject();
            contentJo.Add("user_id_list", new JArray(user_id_list));
            StringContent messageUrgentContent = new StringContent(contentJo.ToString(), Encoding.UTF8, "application/json");
            string receive_id_type = GetUserIdType(user_id_list[0]);
            FsBaseInfo fsBaseInfo = await SendFsBaseRequestAsync<FsBaseInfo>($"open-apis/im/v1/messages/{message_id}/urgent_app?user_id_type={receive_id_type}", false, messageUrgentContent, 1, HttpMethod.Patch);
            if (fsBaseInfo == null || fsBaseInfo.code != 0)
            {
                MyLogger.LogError($"[MessageUrgentAsync] fail {fsBaseInfo.msg}");
            }
        }

        /// <summary>
        /// 通过部门ID获取部门下成员的信息
        /// </summary>
        /// <param name="department_id"></param>
        /// <returns></returns>
        public async Task<Dictionary<string, FsUserInfo>> GetUsersByDepartmentAsync(string department_id = "0")
        {
            Dictionary<string, FsUserInfo> users = new Dictionary<string, FsUserInfo>();
            Dictionary<string, FsDepartmentInfo> departments = await GetDepartmentsAsync(department_id);
            departments.Add("0", new FsDepartmentInfo() {department_id = "0" ,status =new DepartmentStatus() { is_deleted=false} });
            if (departments.Count>0)
            {
                foreach (var departmentItemKv in departments)
                {
                    if(departmentItemKv.Value.status.is_deleted)
                    {
                        continue;
                    }
                    List<FsUserInfo> tempUsers = await GetFsItemAsync<FsUserInfo>($"open-apis/contact/v3/users/find_by_department?user_id_type=open_id&page_size=50&department_id_type=department_id&department_id={departmentItemKv.Value.department_id}",false,null,3);
                    foreach(var tempUserInfo in tempUsers)
                    {
                        if(tempUserInfo == null)
                        {
                            MyLogger.LogWarning($"[GetUsersByDepartmentAsync] find null item");
                            continue;
                        }
                        if(users.TryAdd(tempUserInfo.open_id, tempUserInfo))
                        {
                            MyLogger.LogWarning($"[GetUsersByDepartmentAsync] TryAdd fail with key:{tempUserInfo.open_id} name:{tempUserInfo.name}");
                        }
                    }
                }
            }
            MyLogger.LogInfo($"[GetUsersByDepartmentAsync] get {users.Count} uers");
            return users;
        }

        /// <summary>
        /// GetUsersByDepartmentAsync 的高并发版本，有较高效能，可能会触发风控，自动重试
        /// </summary>
        /// <param name="department_id"></param>
        /// <returns></returns>
        public async Task<Dictionary<string, FsUserInfo>> GetUsersByDepartmentExAsync(string department_id = "0")
        {
            Dictionary<string, FsUserInfo> users = new Dictionary<string, FsUserInfo>();
            Dictionary<string, FsDepartmentInfo> departments = await GetDepartmentsAsync(department_id);
            departments.Add("0", new FsDepartmentInfo() { department_id = "0", status = new DepartmentStatus() { is_deleted = false } });
            if (departments.Count > 0)
            {
                List<Task> getItemTaskList = new List<Task>();
                foreach (var departmentItemKv in departments)
                {
                    if (departmentItemKv.Value.status.is_deleted)
                    {
                        continue;
                    }
                    Task nowGetItemTask = GetFsItemAsync<FsUserInfo>($"open-apis/contact/v3/users/find_by_department?user_id_type=open_id&page_size=50&department_id_type=department_id&department_id={departmentItemKv.Value.department_id}",false,null,3).ContinueWith((tempUsers) =>
                    {
                        lock(users)
                        {
                            foreach (var tempUserInfo in tempUsers.Result)
                            {
                                if (tempUserInfo == null)
                                {
                                    MyLogger.LogWarning($"[GetUsersByDepartmentExAsync] find null item");
                                    continue;
                                }
                                if (users.TryAdd(tempUserInfo.open_id, tempUserInfo))
                                {
                                    MyLogger.LogWarning($"[GetUsersByDepartmentExAsync] TryAdd fail with key{tempUserInfo.open_id}");
                                }
                            }
                        }
                    });
                    getItemTaskList.Add(nowGetItemTask);
                    await Task.Delay(10); //可能触发飞书风控，要主动降低速率 99991400 请求过于频繁，请降低请求频次
                }
                if(getItemTaskList.Count>0)
                {
                    await Task.WhenAll(getItemTaskList.ToArray());
                }
            }
            MyLogger.LogInfo($"[GetUsersByDepartmentExAsync] get {users.Count} uers");
            return users;
        }

        /// <summary>
        /// 获取当前应用所在的群组列表
        /// </summary>
        /// <returns></returns>
        public async Task<List<FsGroupInfo>> GetChatGroupsAsync()
        {
            List<FsGroupInfo> fsGroupInfos = await GetFsItemAsync<FsGroupInfo>("open-apis/im/v1/chats");
            StringBuilder stringBuilder = new StringBuilder("[GetChatGroupsAsync] get chat " );
            foreach(FsGroupInfo fsGroupInfo in fsGroupInfos)
            {
                stringBuilder.Append(">");
                stringBuilder.Append(fsGroupInfo.name);
                stringBuilder.Append("-");
                stringBuilder.Append(fsGroupInfo.chat_id);
            }
            MyLogger.LogInfo(stringBuilder.ToString());
            return fsGroupInfos;
        }

        private async Task<Dictionary<string, FsDepartmentInfo>> GetDepartmentsAsync(string department_id="0")
        {
            Dictionary<string, FsDepartmentInfo> departments = new Dictionary<string, FsDepartmentInfo>();
            List<FsDepartmentInfo> items = await GetFsItemAsync<FsDepartmentInfo>($"open-apis/contact/v3/departments/{department_id}/children?fetch_child=true&page_size=50");
            foreach (var departmentItem in items)
            {
                if(departmentItem==null)
                {
                    MyLogger.LogWarning($"[GetDepartmentsAsync] find null item");
                    continue;
                }
                if (!departments.TryAdd(departmentItem.department_id, departmentItem))
                {
                    MyLogger.LogWarning($"[GetDepartmentsAsync] TryAdd fail with key{departmentItem.department_id}");
                }
            }
            return departments;
        }


    }
}
