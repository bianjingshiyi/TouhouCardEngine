using System.Net;
using RestSharp;
using RestSharp.Serialization;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using TouhouCardEngine;
using TouhouCardEngine.Shared;

namespace NitoriNetwork.Common
{
    /// <summary>
    /// 服务器客户端
    /// 提供了与服务器交互的基本API
    /// </summary>
    public class ServerClient
    {
        const string uaVersionKey = "zmcs";

        /// <summary>
        /// 用户Session
        /// </summary>
        public string UserSession { get; internal set; } = "";

        /// <summary>
        /// 当前用户的UID
        /// </summary>
        public int UID { get; internal set; } = 0;

        ILogger logger { get; }

        #region Basic_Behavior
        RestClient client { get; }

        string cookieFilePath { get; }

        string baseUri { get; }

        Dictionary<int, PublicBasicUserInfo> userInfoCache = new Dictionary<int, PublicBasicUserInfo>();

        /// <summary>
        /// 指定一个服务器初始化Client
        /// </summary>
        /// <param name="baseUri"></param>
        public ServerClient(string baseUri, string gameVersion = "1.0", string cookieFile = "", ILogger logger = null)
        {
            this.baseUri = baseUri;
            this.logger = logger;

            client = new RestClient(baseUri);
            client.UserAgent = uaVersionKey + "/" + gameVersion + " " + additionalUserAgent();
            cookieFilePath = cookieFile;
            client.AddDefaultHeader("Accept-Language", CultureInfo.CurrentCulture.Name);

            if (string.IsNullOrEmpty(cookieFile))
            {
                client.CookieContainer = new CookieContainer();
            }
            else
            {
                client.CookieContainer = CookieContainerExtension.ReadFrom(cookieFile);
                loadCookie();
            }

            client.ThrowOnDeserializationError = true;
            client.UseSerializer(
                () => new MongoDBJsonSerializer()
            );
        }

        /// <summary>
        /// 保存小饼干（？）
        /// </summary>
        void saveCookie()
        {
#if UNITY_EDITOR
            var cookies = client.CookieContainer.GetCookies(new Uri(baseUri));
            string cookieStr = "";
            foreach (Cookie item in cookies)
            {
                cookieStr += item.ToString() + "; ";
            }
            logger?.logTrace($"保存Cookie: {cookieStr}");
#endif
            if (!string.IsNullOrEmpty(cookieFilePath))
            {
                try
                {
                    client.CookieContainer.WriteTo(cookieFilePath);
                }
                catch (Exception e)
                {
                    logger?.logError("Cookie 保存出现问题: " + e.Message);
                }
            }
        }

        /// <summary>
        /// 从Cookie里面加载部分需要的数据
        /// </summary>
        void loadCookie()
        {
            var cookies = client.CookieContainer.GetCookies(new Uri(baseUri));
            foreach (Cookie cookie in cookies)
            {
                if (cookie.Name == "Session")
                {
                    UserSession = cookie.Value;
                } 
            }
        }

        /// <summary>
        /// 清空已有的用户鉴权用Cookie
        /// </summary>
        void clearUserCookie()
        {
            var cookies = client.CookieContainer.GetCookies(new Uri(baseUri));
            foreach (Cookie cookie in cookies)
            {
                if (cookie.Name == "Session" || cookie.Name == "Token")
                    cookie.Expired = true;
            }
        }

        /// <summary>
        /// 附加的UserAgent，用于统计
        /// </summary>
        /// <returns></returns>
        string additionalUserAgent()
        {
            StringBuilder sb = new StringBuilder();
            var os = Environment.OSVersion;
            sb.Append("OS/");
            sb.Append(os.VersionString.Replace(" ", "_"));
            sb.Append(" Lang/");
            var culture = CultureInfo.CurrentCulture;
            sb.Append(culture.Name);
            return sb.ToString();
        }

        /// <summary>
        /// 资源错误处理
        /// </summary>
        /// <remarks>
        /// 当网络异常时，报对应异常错误；当HTTP代码不为200时报错。
        /// </remarks>
        /// <param name="response"></param>
        void errorHandler(IRestResponse response, IRestRequest request)
        {
            if (response.ErrorException != null)
            {
                throw new NetClientException(response.ErrorException, request.Resource);
            }
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new NetClientException(response.StatusCode, request?.Resource);
            }
        }
        /// <summary>
        /// API 错误处理。
        /// </summary>
        /// <remarks>
        /// 当网络异常时，报对应异常错误；
        /// 当HTTP代码不为200时，若 Message 不空，则报对应 Message，否则报HTTP错误。
        /// </remarks>
        /// <param name="response"></param>
        /// <param name="data"></param>
        void errorHandler(IRestResponse response, IExecuteResult data, IRestRequest request)
        {
            if (response.ErrorException != null)
            {
                throw new NetClientException(response.ErrorException, request.Resource);
            }
            if (response.StatusCode != HttpStatusCode.OK)
            {
                if (!string.IsNullOrEmpty(data.message))
                {
                    throw new NetClientException(data.message, request.Resource);
                }
                else
                {
                    throw new NetClientException(response.StatusCode, request.Resource);
                }
            }
        }

        #endregion
        #region Login
        /// <summary>
        /// 用户登录
        /// 需要先获取验证码图像
        /// </summary>
        /// <param name="user">用户名</param>
        /// <param name="pass">密码</param>
        /// <param name="captcha">验证码</param>
        /// <exception cref="NetClientException"></exception>
        /// <returns></returns>
        public bool Login(string user, string pass, string captcha)
        {
            // 防止重复登录
            if (UID != 0)
                clearUserCookie();

            RestRequest request = new RestRequest("/api/User/session", Method.POST);

            request.AddHeader("x-captcha", captcha);
            request.AddParameter("username", user);
            request.AddParameter("password", pass);

            var response = client.Execute<ExecuteResult<string>>(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    // 用户名/密码不正确
                    if (response.Data.code == ResultCode.Fail) return false;

                    throw new NetClientException(response.Data.message);
                }
                else
                {
                    throw new NetClientException(response.StatusDescription);
                }
            }

            if (response.Data.code != ResultCode.Success)
            {
                return false;
            }

            saveCookie();
            // 登录换取的是Token，我们需要Session
            GetSession();
            return true;
        }

        /// <summary>
        /// 用户登录
        /// 需要先获取验证码图像
        /// </summary>
        /// <param name="user">用户名</param>
        /// <param name="pass">密码</param>
        /// <param name="captcha">验证码</param>
        /// <exception cref="NetClientException"></exception>
        /// <returns></returns>
        public async Task<bool> LoginAsync(string user, string pass, string captcha)
        {
            // 防止重复登录
            if (UID != 0)
                clearUserCookie();

            RestRequest request = new RestRequest("/api/User/session", Method.POST);

            request.AddHeader("x-captcha", captcha);
            request.AddParameter("username", user);
            request.AddParameter("password", pass);

            var response = await client.ExecuteAsync<ExecuteResult<string>>(request);
            if (response.ErrorException != null)
            {
                throw new NetClientException(response.ErrorException, request.Resource);
            }
            if (response.StatusCode != HttpStatusCode.OK)
            {
                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    // 登录失败
                    if (response.Data.code == ResultCode.Fail) return false;

                    throw new NetClientException(response.Data.message);
                }
                else
                {
                    throw new NetClientException(response.StatusDescription);
                }
            }

            if (response.Data.code != ResultCode.Success)
            {
                return false;
            }

            saveCookie();
            // 登录换取的是Token，我们需要Session
            await GetSessionAsync();

            return true;
        }

        /// <summary>
        /// 获取Session
        /// </summary>
        /// <returns></returns>
        public string GetSession()
        {
            RestRequest request = new RestRequest("/api/User/session", Method.GET);

            var response = client.Execute<ExecuteResult<string>>(request);
            errorHandler(response, response.Data, request);

            // 更新暂存的Session
            // 虽然Cookie里面也能获取到，但是获取比较麻烦
            UserSession = response.Data.result;
            saveCookie();

            logger?.logTrace($"注册用户登录. Session: {UserSession}");

            // 更新当前登录用户信息
            GetUserInfo();

            return UserSession;
        }

        /// <summary>
        /// 获取Session
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetSessionAsync()
        {
            RestRequest request = new RestRequest("/api/User/session", Method.GET);

            var response = await client.ExecuteAsync<ExecuteResult<string>>(request);
            errorHandler(response, response.Data, request);

            // 更新暂存的Session
            // 虽然Cookie里面也能获取到，但是获取比较麻烦
            UserSession = response.Data.result;
            saveCookie();

            logger?.logTrace($"注册用户登录. Session: {UserSession}");

            // 更新当前登录用户信息
            await GetUserInfoAsync();

            return UserSession;
        }

        /// <summary>
        /// 游客登录
        /// </summary>
        /// <return>是否登录成功，失败则意味着服务器压力过大</return>
        public bool GuestLogin() 
        {
            RestRequest request = new RestRequest("/api/User/guest", Method.POST);

            var response = client.Execute<ExecuteResult<string>>(request);
            if (response.Data?.code == ResultCode.Fail)
                return false;

            errorHandler(response, response.Data, request);

            UserSession = response.Data.result;
            saveCookie();

            logger?.logTrace($"游客登录. Session: {UserSession}");

            GetUserInfo();

            return true;
        }

        /// <summary>
        /// 游客登录
        /// </summary>
        public async Task<bool> GuestLoginAsync() 
        {
            RestRequest request = new RestRequest("/api/User/guest", Method.POST);

            var response = await client.ExecuteAsync<ExecuteResult<string>>(request);
            if (response.Data?.code == ResultCode.Fail)
                return false;

            errorHandler(response, response.Data, request);

            UserSession = response.Data.result;
            saveCookie();

            logger?.logTrace($"游客登录. Session: {UserSession}");

            await GetUserInfoAsync();

            return true;
        }
        #endregion
        #region Register
        /// <summary>
        /// 注册用户
        /// 需要先获取验证码图像
        /// </summary>
        /// <param name="username"></param>
        /// <param name="mail"></param>
        /// <param name="password"></param>
        /// <param name="captcha"></param>
        /// <param name="nickname"></param>
        /// <returns></returns>
        /// <exception cref="NetClientException"></exception>
        public void Register(string username, string mail, string password, string nickname, string invite, string captcha)
        {
            RestRequest request = new RestRequest("/api/User", Method.POST);

            request.AddHeader("x-captcha", captcha);

            request.AddParameter("username", username);
            request.AddParameter("mail", mail);
            request.AddParameter("password", password);
            request.AddParameter("nickname", nickname);
            request.AddParameter("invite", invite);

            var response = client.Execute<ExecuteResult<string>>(request);

            errorHandler(response, response.Data, request);
        }

        /// <summary>
        /// 注册用户
        /// 需要先获取验证码图像
        /// </summary>
        /// <param name="username"></param>
        /// <param name="mail"></param>
        /// <param name="password"></param>
        /// <param name="captcha"></param>
        /// <param name="nickname"></param>
        /// <returns></returns>
        /// <exception cref="NetClientException"></exception>
        public async Task RegisterAsync(string username, string mail, string password, string nickname, string invite, string captcha)
        {
            RestRequest request = new RestRequest("/api/User", Method.POST);

            request.AddHeader("x-captcha", captcha);

            request.AddParameter("username", username);
            request.AddParameter("mail", mail);
            request.AddParameter("password", password);
            request.AddParameter("nickname", nickname);
            request.AddParameter("invite", invite);

            var response = await client.ExecuteAsync<ExecuteResult<string>>(request);

            errorHandler(response, response.Data, request);
        }
        #endregion
        #region Captcha
        /// <summary>
        /// 获取验证码图像
        /// </summary>
        /// <returns></returns>
        public byte[] GetCaptchaImage()
        {
            RestRequest request = new RestRequest("/api/Captcha/image", Method.GET);

            var response = client.Execute(request);
            errorHandler(response, request);

            return response.RawBytes;
        }

        /// <summary>
        /// 获取验证码的图像
        /// </summary>
        /// <returns></returns>
        public async Task<byte[]> GetCaptchaImageAsync()
        {
            RestRequest request = new RestRequest("/api/Captcha/image", Method.GET);

            var response = await client.ExecuteAsync(request);
            errorHandler(response, request);

            return response.RawBytes;
        }
        #endregion
        #region Room
        /// <summary>
        /// 创建一个房间
        /// </summary>
        /// <returns></returns>
        public LobbyRoomData CreateRoom(string name = "", string password = "")
        {
            RestRequest request = new RestRequest("/api/Room", Method.POST);
            request.AddParameter("name", name);
            request.AddParameter("password", password);
            var response = client.Execute<ExecuteResult<LobbyRoomData>>(request);

            errorHandler(response, response.Data, request);
            return response.Data.result;
        }

        /// <summary>
        /// 创建一个房间
        /// </summary>
        /// <returns></returns>
        public async Task<LobbyRoomData> CreateRoomAsync(string name = "", string password = "")
        {
            RestRequest request = new RestRequest("/api/Room", Method.POST);
            request.AddParameter("name", name);
            request.AddParameter("password", password);
            var response = await client.ExecuteAsync<ExecuteResult<LobbyRoomData>>(request);

            errorHandler(response, response.Data, request);
            return response.Data.result;
        }

        /// <summary>
        /// 获取房间信息
        /// </summary>
        /// <returns></returns>
        public LobbyRoomData[] GetRoomInfos()
        {
            RestRequest request = new RestRequest("/api/Room", Method.GET);
            var response = client.Execute<ExecuteResult<LobbyRoomData[]>>(request);

            errorHandler(response, response.Data, request);
            return response.Data.result;
        }

        /// <summary>
        /// 获取房间信息
        /// </summary>
        /// <returns></returns>
        public async Task<LobbyRoomData[]> GetRoomInfosAsync()
        {
            RestRequest request = new RestRequest("/api/Room", Method.GET);
            var response = await client.ExecuteAsync<ExecuteResult<LobbyRoomData[]>>(request);

            errorHandler(response, response.Data, request);
            return response.Data.result;
        }
        #endregion
        #region User
        /// <summary>
        /// 获取当前登录用户的信息
        /// </summary>
        /// <returns></returns>
        public PublicBasicUserInfo GetUserInfo()
        {
            RestRequest request = new RestRequest("/api/User/me", Method.GET);
            var response = client.Execute<ExecuteResult<PublicBasicUserInfo>>(request);

            errorHandler(response, response.Data, request);

            UID = response.Data.result.UID;
            userInfoCache[response.Data.result.UID] = response.Data.result;
            return response.Data.result;
        }

        /// <summary>
        /// 获取缓存的当前用户登录的信息
        /// </summary>
        /// <returns></returns>
        public PublicBasicUserInfo GetUserInfoCached()
        {
            return userInfoCache[UID];
        }

        /// <summary>
        /// 获取指定ID用户的用户信息
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public PublicBasicUserInfo GetUserInfo(int uid, bool refresh = false)
        {
            if (!refresh && userInfoCache.ContainsKey(uid))
            {
                return userInfoCache[uid];
            }

            RestRequest request = new RestRequest("/api/User/" + uid, Method.GET);
            var response = client.Execute<ExecuteResult<PublicBasicUserInfo>>(request);

            errorHandler(response, response.Data, request);

            userInfoCache[uid] = response.Data.result;
            return response.Data.result;
        }

        /// <summary>
        /// 获取当前登录用户的信息
        /// </summary>
        /// <returns></returns>
        public async Task<PublicBasicUserInfo> GetUserInfoAsync()
        {
            RestRequest request = new RestRequest("/api/User/me", Method.GET);
            var response = await client.ExecuteAsync<ExecuteResult<PublicBasicUserInfo>>(request);

            errorHandler(response, response.Data, request);

            UID = response.Data.result.UID;
            userInfoCache[response.Data.result.UID] = response.Data.result;
            return response.Data.result;
        }

        /// <summary>
        /// 获取指定用户的信息
        /// </summary>
        /// <returns></returns>
        public async Task<PublicBasicUserInfo> GetUserInfoAsync(int uid, bool refresh)
        {
            if (!refresh && userInfoCache.ContainsKey(uid))
            {
                return userInfoCache[uid];
            }

            RestRequest request = new RestRequest("/api/User/" + uid, Method.GET);
            var response = await client.ExecuteAsync<ExecuteResult<PublicBasicUserInfo>>(request);

            errorHandler(response, response.Data, request);

            userInfoCache[uid] = response.Data.result;
            return response.Data.result;
        }

        /// <summary>
        /// 注销
        /// </summary>
        public void Logout() 
        {
            RestRequest request = new RestRequest("/api/User/session", Method.DELETE);

            var response = client.Execute<ExecuteResult<string>>(request);
            errorHandler(response, response.Data, request);

            UID = 0;
            UserSession = "";
            saveCookie();
        }

        /// <summary>
        /// 注销
        /// </summary>
        /// <returns></returns>
        public async Task LogoutAsync() 
        {
            RestRequest request = new RestRequest("/api/User/session", Method.DELETE);

            var response = await client.ExecuteAsync<ExecuteResult<string>>(request);
            errorHandler(response, response.Data, request);

            UID = 0;
            UserSession = "";
            saveCookie();
        }
        #endregion
        #region Update
        /// <summary>
        /// 获取最新的版本更新信息
        /// </summary>
        /// <returns></returns>
        public ClientUpdateInfo GetLatestUpdate()
        {
            RestRequest request = new RestRequest("/api/Update/latest", Method.GET);
            var response = client.Execute<ExecuteResult<ClientUpdateInfo>>(request);

            errorHandler(response, response.Data, request);
            return response.Data.result;
        }

        /// <summary>
        /// 获取最新的版本更新信息
        /// </summary>
        /// <returns></returns>
        public async Task<ClientUpdateInfo> GetLatestUpdateAsync()
        {
            RestRequest request = new RestRequest("/api/Update/latest", Method.GET);
            var response = await client.ExecuteAsync<ExecuteResult<ClientUpdateInfo>>(request);

            if (response.ErrorException != null)
                throw response.ErrorException;

            errorHandler(response, response.Data, request);
            return response.Data.result;
        }

        /// <summary>
        /// 获取指定版本的更新信息
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        public ClientUpdateInfo GetUpdateByVersion(string version)
        {
            RestRequest request = new RestRequest("/api/Update/" + version, Method.GET);
            var response = client.Execute<ExecuteResult<ClientUpdateInfo>>(request);

            errorHandler(response, response.Data, request);
            return response.Data.result;
        }

        /// <summary>
        /// 获取指定版本的更新信息
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        public async Task<ClientUpdateInfo> GetUpdateByVersionAsync(string version)
        {
            RestRequest request = new RestRequest("/api/Update/" + version, Method.GET);
            var response = await client.ExecuteAsync<ExecuteResult<ClientUpdateInfo>>(request);

            errorHandler(response, response.Data, request);

            return response.Data.result;
        }

        /// <summary>
        /// 获取指定版本之后到最新版本的所有更新信息
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        public ClientUpdateInfo[] GetUpdateDeltaByVersion(string version)
        {
            RestRequest request = new RestRequest("/api/Update/" + version + "/delta", Method.GET);
            var response = client.Execute<ExecuteResult<ClientUpdateInfo[]>>(request);

            errorHandler(response, response.Data, request);
            return response.Data.result;
        }

        /// <summary>
        /// 获取指定版本之后到最新版本的所有更新信息
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        public async Task<ClientUpdateInfo[]> GetUpdateDeltaByVersionAsync(string version)
        {
            RestRequest request = new RestRequest("/api/Update/" + version + "/delta", Method.GET);
            var response = await client.ExecuteAsync<ExecuteResult<ClientUpdateInfo[]>>(request);

            errorHandler(response, response.Data, request);
            return response.Data.result;
        }

        #endregion
        #region RecoverPassword
        /// <summary>
        /// 请求找回密码
        /// </summary>
        /// <param name="mail"></param>
        /// <param name="captcha"></param>
        public void RecoverRequest(string mail, string captcha)
        {
            RestRequest request = new RestRequest("/api/User/recover/request", Method.POST);

            request.AddHeader("x-captcha", captcha);
            request.AddParameter("mail", mail);

            var response = client.Execute<ExecuteResult<string>>(request);
            errorHandler(response, response.Data, request);
        }
        /// <summary>
        /// 请求找回密码
        /// </summary>
        /// <param name="mail"></param>
        /// <param name="captcha"></param>
        /// <returns></returns>
        public async Task RecoverRequestAsync(string mail, string captcha)
        {
            RestRequest request = new RestRequest("/api/User/recover/request", Method.POST);

            request.AddHeader("x-captcha", captcha);
            request.AddParameter("mail", mail);

            var response = await client.ExecuteAsync<ExecuteResult<string>>(request);
            errorHandler(response, response.Data, request);
        }
        /// <summary>
        /// 请求找回密码
        /// </summary>
        /// <param name="mail"></param>
        /// <param name="captcha"></param>
        /// <param name="code"></param>
        /// <param name="password"></param>
        public void RecoverPassword(string mail, string code, string password, string captcha)
        {
            RestRequest request = new RestRequest("/api/User/recover", Method.POST);

            request.AddHeader("x-captcha", captcha);
            request.AddParameter("mail", mail);
            request.AddParameter("code", code);
            request.AddParameter("password", password);

            var response = client.Execute<ExecuteResult<string>>(request);
            errorHandler(response, response.Data, request);
        }

        /// <summary>
        /// 请求找回密码
        /// </summary>
        /// <param name="mail"></param>
        /// <param name="captcha"></param>
        /// <param name="code"></param>
        /// <param name="password"></param>
        public async Task RecoverPasswordAsync(string mail, string code, string password, string captcha)
        {
            RestRequest request = new RestRequest("/api/User/recover", Method.POST);

            request.AddHeader("x-captcha", captcha);
            request.AddParameter("mail", mail);
            request.AddParameter("code", code);
            request.AddParameter("password", password);

            var response = await client.ExecuteAsync<ExecuteResult<string>>(request);
            errorHandler(response, response.Data, request);
        }
        #endregion
        #region EULA
        /// <summary>
        /// 获取用户许可协议的HTML
        /// </summary>
        /// <returns></returns>
        public string GetEULA()
        {
            RestRequest request = new RestRequest("/api/EULA/EULA", Method.GET);
            var response = client.Execute(request);
            errorHandler(response, request);

            return response.Content;
        }
        /// <summary>
        /// 获取用户许可协议的HTML
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetEULAAsync()
        {
            RestRequest request = new RestRequest("/api/EULA/EULA", Method.GET);
            var response = await client.ExecuteAsync(request);
            errorHandler(response, request);

            return response.Content;
        }

        /// <summary>
        /// 获取用户许可协议的HTML
        /// </summary>
        /// <returns></returns>
        public string GetPrivacyPolicy()
        {
            RestRequest request = new RestRequest("/api/EULA/Privacy", Method.GET);
            var response = client.Execute(request);
            errorHandler(response, request);

            return response.Content;
        }
        /// <summary>
        /// 获取用户许可协议的HTML
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetPrivacyPolicyAsync()
        {
            RestRequest request = new RestRequest("/api/EULA/Privacy", Method.GET);
            var response = await client.ExecuteAsync(request);
            errorHandler(response, request);

            return response.Content;
        }
        #endregion
        #region UserDeck
        /// <summary>
        /// 设置用户卡组
        /// </summary>
        /// <param name="decks"></param>
        public void SetUserDecks(DeckDataItem[] decks)
        {
            RestRequest request = new RestRequest("/api/Deck", Method.POST);
            request.AddParameter("decks", decks.ToJson());

            var response = client.Execute<ExecuteResult>(request);

            errorHandler(response, response.Data, request);
        }

        /// <summary>
        /// 设置用户卡组
        /// </summary>
        /// <param name="decks"></param>
        public async Task SetUserDecksAsync(DeckDataItem[] decks)
        {
            RestRequest request = new RestRequest("/api/Deck", Method.POST);
            request.AddParameter("decks", decks.ToJson());

            var response = await client.ExecuteAsync<ExecuteResult>(request);

            errorHandler(response, response.Data, request);
        }

        /// <summary>
        /// 获取用户卡组
        /// </summary>
        /// <returns></returns>
        public DeckDataItem[] GetUserDecks()
        {
            RestRequest request = new RestRequest("/api/Deck", Method.GET);
            var response = client.Execute<ExecuteResult<DeckDataItem[]>>(request);

            errorHandler(response, response.Data, request);

            return response.Data.result;
        }

        /// <summary>
        /// 获取用户卡组
        /// </summary>
        /// <returns></returns>
        public async Task<DeckDataItem[]> GetUserDecksAsync()
        {
            RestRequest request = new RestRequest("/api/Deck", Method.GET);
            var response = await client.ExecuteAsync<ExecuteResult<DeckDataItem[]>>(request);

            errorHandler(response, response.Data, request);

            return response.Data.result;
        }

        #endregion
    }

    [System.Serializable]
    public class NetClientException : System.Exception
    {
        public string Url { get; }
        public NetClientException() { }
        public NetClientException(string message, string url = "") : base(message) 
        {
            Url = url;
        }
        public NetClientException(HttpStatusCode code, string url = "") : base($"HTTP {(int)code}: {code}") 
        {
            Url = url;
        }
        public NetClientException(Exception inner, string url = "") : base(inner.Message) 
        {
            Url = url;
        }
        protected NetClientException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public class MongoDBJsonSerializer : IRestSerializer
    {
        public string Serialize(object obj) => obj.ToJson();

        public string Serialize(Parameter bodyParameter) => Serialize(bodyParameter.Value);

        public T Deserialize<T>(IRestResponse response)
        {
            // UnityEngine.Debug.Log(response.Content);
            return BsonSerializer.Deserialize<T>(response.Content);
        }

        public string[] SupportedContentTypes { get; } =
        {
            "application/json", "text/json", "text/x-json", "text/javascript", "*+json", "text/plain"
        };

        public string ContentType { get; set; } = "application/json";

        public DataFormat DataFormat { get; } = DataFormat.Json;
    }

    static class CookieContainerExtension
    {
        public static void WriteTo(this CookieContainer container, string file)
        {
            using (Stream stream = File.Create(file))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, container);
            }
        }

        public static CookieContainer ReadFrom(string file)
        {
            try
            {
                using (Stream stream = File.Open(file, FileMode.Open))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    return (CookieContainer)formatter.Deserialize(stream);
                }
            }
            catch (Exception)
            {
                return new CookieContainer();
            }
        }
    }
}
