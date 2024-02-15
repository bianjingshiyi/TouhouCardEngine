using System.Net;
using RestSharp;
using System.Globalization;
using TouhouCardEngine.Shared;
using System.Threading.Tasks;
using System;
using System.Linq;
using MongoDB.Bson.Serialization.Attributes;

namespace NitoriNetwork.Common
{
    /// <summary>
    /// 认证服务器客户端
    /// </summary>
    public class KratosClient
    {
        ILogger logger { get; }
        RestClient client { get; }

        public string baseUri { get; private set; }

        public string SessionToken { get; private set; }

        /// <summary>
        /// 认证服务器
        /// </summary>
        /// <param name="baseUri"></param>
        public KratosClient(string baseUri, string userAgent, ILogger logger = null)
        {
            this.baseUri = baseUri;
            this.logger = logger;

            client = new RestClient(baseUri);
            client.UserAgent = userAgent;
            client.AddDefaultHeader("Accept-Language", CultureInfo.CurrentCulture.Name);

            client.ThrowOnDeserializationError = true;
            client.UseSerializer(
                () => new MongoDBJsonSerializer()
            );
        }
        #region Structs
        #region Common
        [Serializable]
        [BsonIgnoreExtraElements]
        class FlowResponse
        {
            [BsonElement("id")]
            public string flow { get; set; }
        }

        /// <summary>
        /// 用户信息
        /// </summary>
        [Serializable]
        [BsonIgnoreExtraElements]
        public class UserTraits
        {
            /// <summary>
            /// 用户邮箱，登录用
            /// </summary>
            public string email { get; set; }

            /// <summary>
            /// 用户昵称
            /// </summary>
            public string nickname { get; set; }

            public UserTraits(string email = "", string nickname = "")
            {
                this.email = email;
                this.nickname = nickname;
            }
        }
        [Serializable]
        [BsonIgnoreExtraElements]
        public class Message
        {
            public Int64 id { get; set; }
            /// <summary>
            /// The message text. Written in american english.
            /// </summary>
            public string text { get; set; }
            /// <summary>
            /// The message type. info, error or success
            /// </summary>
            public string type { get; set; }
        }
        [Serializable]
        [BsonIgnoreExtraElements]
        public class APIError
        {
            public Int64 code { get; set; }

            public string message { get; set; }
        }
        [Serializable]
        [BsonIgnoreExtraElements]
        public class UIContainer
        {
            public Message[] messages { get; set; }
        }

        [Serializable]
        [BsonIgnoreExtraElements]
        public class UserMetadata
        {
            public string steamid { get; set; }
        }

        [Serializable]
        [BsonIgnoreExtraElements]
        public class Identity
        {
            /// <summary>
            /// identity's unique identifier.
            /// </summary>
            public string id { get; set; }

            public string schema_id { get; set; }

            public string schema_url { get; set; }

            /// <summary>
            /// The state can either be active or inactive.
            /// </summary>
            public string state { get; set; }

            public UserTraits traits { get; set; }

            public UserMetadata metadata_public { get; set; }
        }

        [Serializable]
        [BsonIgnoreExtraElements]
        public class Session
        {
            /// <summary>
            /// Active state. If false the session is no longer active.
            /// </summary>
            public bool active { get; set; }

            /// <summary>
            /// Session ID. (uuid)
            /// </summary>
            public string id { get; set; }

            /// <summary>
            /// An identity represents a (human) user in Ory.
            /// </summary>
            public Identity identity { get; set; }
        }

        public class SessionResponse : Session
        {
            public APIError error { get; set; }
        }

        public abstract class MethodRequest
        {
            public string method { get; set; }

            public MethodRequest(string method)
            {
                this.method = method;
            }
        }

        [Serializable]
        [BsonIgnoreExtraElements]
        public class CommonResponse
        {
            public UIContainer ui { get; set; }

            public APIError error { get; set; }
        }
        #endregion
        #region 登录
        public abstract class LoginRequest : MethodRequest
        {
            public LoginRequest(string method) : base(method)
            {
            }
        }

        public class PasswordLoginRequest : LoginRequest
        {
            public string identifier { get; set; }
            public string password { get; set; }

            public PasswordLoginRequest(string id, string password) : base("password")
            {
                identifier = id;
                this.password = password;
            }
        }

        public class SteamLoginRequest : LoginRequest
        {
            public string provider { get; set; }
            public string ticket { get; set; }

            public SteamLoginRequest(string provider, string ticket) : base("steam")
            {
                this.provider = provider;
                this.ticket = ticket;
            }
        }

        [Serializable]
        [BsonIgnoreExtraElements]
        public class LoginResponse : CommonResponse
        {
            public Session session { get; set; }

            /// <summary>
            /// The Session Token
            /// 
            /// A session token is equivalent to a session cookie, but it can be sent in the HTTP Authorization Header.
            /// The session token is only issued for API flows, not for Browser flows!
            /// </summary>
            public string session_token { get; set; }
        }
        #endregion
        #region 注册
        public abstract class RegistrationRequest : MethodRequest
        {
            public UserTraits traits { get; set; }
            public RegistrationRequest(string method, UserTraits traits) : base(method)
            {
                this.traits = traits;
            }
        }

        /// <summary>
        /// 邮箱-密码注册
        /// </summary>
        public class PasswordRegistrationRequest : RegistrationRequest
        {
            public string password { get; set; }
            /// <summary>
            /// 邮箱-密码注册
            /// </summary>
            /// <param name="email">登录邮箱</param>
            /// <param name="password">登录密码</param>
            /// <param name="nickname">昵称</param>
            public PasswordRegistrationRequest(string email, string password, string nickname = "") : base("password", new UserTraits(email, nickname))
            {
                this.password = password;
            }
        }

        /// <summary>
        /// Steam 注册
        /// </summary>
        public class SteamRegistrationRequest : RegistrationRequest
        {
            public string provider { get; set; }
            public string ticket { get; set; }
            /// <summary>
            /// Steam注册
            /// </summary>
            /// <param name="provider">Steam APP 提供者</param>
            /// <param name="ticket">Steam 认证 Ticket </param>
            /// <param name="nickname">昵称</param>
            public SteamRegistrationRequest(string provider, string ticket, string nickname = "") : base("steam", new UserTraits("", nickname))
            {
                this.provider = provider;
                this.ticket = ticket;
            }
        }
        [Serializable]
        [BsonIgnoreExtraElements]
        public class RegistrationResponse : CommonResponse
        {
            public Session session { get; set; }

            public Identity identity { get; set; }

            /// <summary>
            /// The Session Token
            /// This field is only set when the session hook is configured as a post-registration hook.
            /// </summary>
            public string session_token { get; set; }
        }
        #endregion

        #region Setting
        public abstract class SettingRequest : MethodRequest
        {
            public SettingRequest(string method) : base(method)
            {
            }
        }
        public class PasswordSettingRequest : SettingRequest
        {
            public string password { get; set; }
            public PasswordSettingRequest(string password) : base("password")
            {
                this.password = password;
            }
        }
        public class ProfileSettingRequest : SettingRequest
        {
            public UserTraits traits { get; set; }
            public ProfileSettingRequest(UserTraits traits) : base("profile")
            {
                this.traits = traits;
            }
        }
        public class SteamSettingRequest : SettingRequest
        {
            public string link { get; set; }
            public string unlink { get; set; }
            public string ticket { get; set; }

            SteamSettingRequest(string link, string unlink, string ticket) : base("steam")
            {
                this.link = link;
                this.unlink = unlink;
                this.ticket = ticket;
            }

            /// <summary>
            /// 新增Steam绑定
            /// </summary>
            /// <param name="provider"></param>
            /// <param name="ticket"></param>
            /// <returns></returns>
            public static SteamSettingRequest Link(string provider, string ticket)
            {
                return new SteamSettingRequest(provider, "", ticket);
            }
            /// <summary>
            /// 取消Steam绑定
            /// </summary>
            /// <returns></returns>
            public static SteamSettingRequest Unlink()
            {
                return new SteamSettingRequest("", "unlink", "");
            }
        }

        [Serializable]
        [BsonIgnoreExtraElements]
        public class SettingResponse : CommonResponse
        {
            public Identity identity { get; set; }
            public string state { get; set; }
        }
        #endregion

        /// <summary>
        /// 发送账户找回邮件
        /// </summary>
        public class EmailRecoveryRequest : MethodRequest
        {
            public string email { get; set; }
            public EmailRecoveryRequest(string email) : base("link")
            {
                this.email = email;
            }
        }

        [Serializable]
        [BsonIgnoreExtraElements]
        public class RecoveryResponse : CommonResponse
        {
            public string state { get; set; }
        }

        #endregion

        void addHeader(RestRequest request)
        {
            if (!string.IsNullOrEmpty(SessionToken))
                request.AddHeader("X-Session-Token", SessionToken);
        }

        /// <summary>
        /// 创建登录Flow
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NetClientException"></exception>
        public async Task<string> CreateLoginFlow()
        {
            RestRequest request = new RestRequest("/self-service/login/api", Method.GET);
            var response = await client.ExecuteAsync<FlowResponse>(request);
            if (response.ErrorException != null)
            {
                throw new NetClientException(response.ErrorException, request.Resource);
            }
            return response.Data.flow;
        }

        /// <summary>
        /// 创建配置Flow
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NetClientException"></exception>
        public async Task<string> CreateSettingFlow()
        {
            RestRequest request = new RestRequest("/self-service/settings/api", Method.GET);
            addHeader(request);
            var response = await client.ExecuteAsync<FlowResponse>(request);
            if (response.ErrorException != null)
            {
                throw new NetClientException(response.ErrorException, request.Resource);
            }
            return response.Data.flow;
        }

        /// <summary>
        /// 创建注册Flow
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NetClientException"></exception>
        public async Task<string> CreateRegistrationFlow()
        {
            RestRequest request = new RestRequest("/self-service/registration/api", Method.GET);
            var response = await client.ExecuteAsync<FlowResponse>(request);
            if (response.ErrorException != null)
            {
                throw new NetClientException(response.ErrorException, request.Resource);
            }
            return response.Data.flow;
        }

        /// <summary>
        /// 创建找回Flow
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NetClientException"></exception>
        public async Task<string> CreateRecoveryFlow()
        {
            RestRequest request = new RestRequest("/self-service/recovery/api", Method.GET);
            var response = await client.ExecuteAsync<FlowResponse>(request);
            if (response.ErrorException != null)
            {
                throw new NetClientException(response.ErrorException, request.Resource);
            }
            return response.Data.flow;
        }

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="loginFlow">登录Flow</param>
        /// <param name="req">登录凭证</param>
        /// <returns></returns>
        /// <exception cref="NetClientException"></exception>
        public async Task<string> UpdateLoginFlow(string loginFlow, MethodRequest req)
        {
            RestRequest request = new RestRequest("/self-service/login", Method.POST);
            request.AddQueryParameter("flow", loginFlow);
            request.AddJsonBody(req);

            var response = await client.ExecuteAsync<LoginResponse>(request);
            handleError(response, response.Data, request.Resource);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                SessionToken = response.Data.session_token;
                return SessionToken;
            }

            logger.log(response.Data.ToString());
            throw new NetClientException("internal error");
        }

        void handleError(IRestResponse resp, CommonResponse data, string resource = "")
        {
            if (resp.ErrorException != null)
                throw new NetClientException(resp.ErrorException, resource);

            // 参数错误
            if (resp.StatusCode == HttpStatusCode.BadRequest)
                throw new NetClientException(String.Join(';', data.ui.messages.Select(x => x.text)));

            if (data.error != null)
                throw new NetClientException(data.error.message);
        }

        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="registrationFlow"></param>
        /// <param name="req"></param>
        /// <returns></returns>
        /// <exception cref="NetClientException"></exception>
        public async Task<string> UpdateRegistrationFlow(string registrationFlow, RegistrationRequest req)
        {
            RestRequest request = new RestRequest("/self-service/registration", Method.POST);
            request.AddQueryParameter("flow", registrationFlow);
            request.AddJsonBody(req);

            var response = await client.ExecuteAsync<RegistrationResponse>(request);
            handleError(response, response.Data, request.Resource);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                SessionToken = response.Data.session_token;
                return SessionToken;
            }

            logger.log(response.Data.ToString());
            throw new NetClientException("internal error");
        }

        class LogoutRequest
        {
            public string session_token { get; set; }

            public LogoutRequest(string sessionToken)
            {
                session_token = sessionToken;
            }
        }

        /// <summary>
        /// 登出
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Logout()
        {
            RestRequest request = new RestRequest("/self-service/logout/api", Method.DELETE);
            request.AddJsonBody(new LogoutRequest(SessionToken));
            var response = await client.ExecuteAsync(request);
            if (response.ErrorException != null)
                throw new NetClientException(response.ErrorException.Message);

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                SessionToken = "";
                return true;
            }
            return false;
        }
    
        /// <summary>
        /// 更新用户信息
        /// </summary>
        /// <param name="settingFlow"></param>
        /// <param name="req"></param>
        /// <returns></returns>
        /// <exception cref="NetClientException"></exception>
        public async Task<bool> UpdateSettingFlow(string settingFlow, SettingRequest req)
        {
            RestRequest request = new RestRequest("/self-service/settings", Method.POST);
            addHeader(request);
            request.AddQueryParameter("flow", settingFlow);
            request.AddJsonBody(req);

            var response = await client.ExecuteAsync<SettingResponse>(request);
            handleError(response, response.Data, request.Resource);

            if (response.StatusCode == HttpStatusCode.OK)
                return response.Data.state == "success";

            logger.log(response.Data.state);
            throw new NetClientException("internal error");
        }
    
        /// <summary>
        /// 发送找回密码邮件
        /// </summary>
        /// <param name="recoveryFlow"></param>
        /// <param name="req"></param>
        /// <returns></returns>
        /// <exception cref="NetClientException"></exception>
        public async Task<bool> UpdateRecoveryFlow(string recoveryFlow, EmailRecoveryRequest req)
        {
            RestRequest request = new RestRequest("/self-service/recovery", Method.POST);
            request.AddQueryParameter("flow", recoveryFlow);
            request.AddJsonBody(req);

            var response = await client.ExecuteAsync<RecoveryResponse>(request);
            handleError(response, response.Data, request.Resource);

            if (response.StatusCode == HttpStatusCode.OK)
                return response.Data.state == "sent_email";

            logger.log(response.Data.state);
            throw new NetClientException("internal error");
        }

        /// <summary>
        /// 检查当前用户信息
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NetClientException"></exception>
        public async Task<Session> WhoAmI()
        {
            RestRequest request = new RestRequest("/sessions/whoami", Method.GET);
            var response = await client.ExecuteAsync<SessionResponse>(request);

            if (response.ErrorException != null)
                throw new NetClientException(response.ErrorException, request.Resource);

            if (response.IsSuccessful)
                return response.Data;

            if (response.Data.error != null)
                throw new NetClientException(response.Data.error.message);

            logger.log(response.Data.ToString());
            throw new NetClientException("internal error");
        }
    }
}
