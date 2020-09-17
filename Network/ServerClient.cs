using System.Net;
using RestSharp;
using RestSharp.Serialization;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System.Threading.Tasks;

namespace NitoriNetwork.Common
{
    /// <summary>
    /// 服务器客户端
    /// 提供了与服务器交互的基本API
    /// </summary>
    public class ServerClient
    {
        const string Server = "https://serv.igsk.fun";
        const string ua = "ZMCS/1.0 NitoriNetwork/1.0";

        /// <summary>
        /// 用户Session
        /// </summary>
        public string UserSession { get; internal set; } = "";

        /// <summary>
        /// 用户UID
        /// </summary>
        public int UID { get; internal set; } = 0;

        RestClient client { get; }

        /// <summary>
        /// 使用默认服务器初始化客户端
        /// </summary>
        public ServerClient() : this(Server) { }

        /// <summary>
        /// 指定一个服务器初始化Client
        /// </summary>
        /// <param name="baseUri"></param>
        public ServerClient(string baseUri)
        {
            client = new RestClient(baseUri);
            client.UserAgent = ua;
            // todo: Cookie的序列化/反序列化
            client.CookieContainer = new CookieContainer();
            client.ThrowOnDeserializationError = true;
            client.UseSerializer(
                () => new MongoDBJsonSerializer()
            );
        }

        class ResponseData<T>
        {
            public int code;
            public string message;
            public T result;
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
        public bool Login(string user, string pass, string captcha)
        {
            RestRequest request = new RestRequest("/api/User/session", Method.POST);

            request.AddHeader("x-captcha", captcha);
            request.AddParameter("username", user);
            request.AddParameter("password", pass);

            var response = client.Execute<ResponseData<string>>(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    if (response.Data.code == 1)
                    {
                        return false;
                    }

                    throw new NetClientException(response.Data.message);
                }
                else
                {
                    throw new NetClientException(response.StatusDescription);
                }
            }

            if (response.Data.code != 0)
            {
                return false;
            }

            // 更新暂存的Session
            // 虽然Cookie里面也能获取到，但是获取比较麻烦
            UserSession = response.Data.result;
            UID = GetUID();

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
            RestRequest request = new RestRequest("/api/User/session", Method.POST);

            request.AddHeader("x-captcha", captcha);
            request.AddParameter("username", user);
            request.AddParameter("password", pass);

            var response = await client.ExecuteAsync<ResponseData<string>>(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    // 登录失败
                    if (response.Data.code == 1)
                    {
                        return false;
                    }

                    throw new NetClientException(response.Data.message);
                }
                else
                {
                    throw new NetClientException(response.StatusDescription);
                }
            }

            if (response.Data.code != 0)
            {
                return false;
            }

            // 更新暂存的Session
            // 虽然Cookie里面也能获取到，但是获取比较麻烦
            UserSession = response.Data.result;
            UID = await GetUIDAsync();

            return true;
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
        public void Register(string username, string mail, string password, string nickname, string captcha)
        {
            RestRequest request = new RestRequest("/api/User", Method.POST);

            request.AddHeader("x-captcha", captcha);

            request.AddParameter("username", username);
            request.AddParameter("mail", mail);
            request.AddParameter("password", password);
            request.AddParameter("nickname", nickname);

            var response = client.Execute<ResponseData<string>>(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    throw new NetClientException(response.Data.message);
                }
                else
                {
                    throw new NetClientException(response.StatusDescription);
                }
            }

            if (response.Data.code != 0)
            {
                throw new NetClientException(response.Data.message);
            }
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
        public async Task RegisterAsync(string username, string mail, string password, string nickname, string captcha)
        {
            RestRequest request = new RestRequest("/api/User", Method.POST);

            request.AddHeader("x-captcha", captcha);

            request.AddParameter("username", username);
            request.AddParameter("mail", mail);
            request.AddParameter("password", password);
            request.AddParameter("nickname", nickname);

            var response = await client.ExecuteAsync<ResponseData<string>>(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    throw new NetClientException(response.Data.message);
                }
                else
                {
                    throw new NetClientException(response.StatusDescription);
                }
            }

            if (response.Data.code != 0)
            {
                throw new NetClientException(response.Data.message);
            }
        }

        /// <summary>
        /// 获取验证码图像
        /// </summary>
        /// <returns></returns>
        public byte[] GetCaptchaImage()
        {
            RestRequest request = new RestRequest("/api/Captcha/image", Method.GET);

            var response = client.Execute(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new NetClientException(response.StatusDescription);
            }

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
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new NetClientException(response.StatusDescription);
            }

            return response.RawBytes;
        }


        /// <summary>
        /// 创建一个房间
        /// </summary>
        /// <returns></returns>
        public ServerRoomInfo CreateRoom()
        {
            RestRequest request = new RestRequest("/api/Room", Method.POST);
            var response = client.Execute<ResponseData<ServerRoomInfo>>(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new NetClientException(response.StatusDescription);
            }
            if (response.Data.code != 0)
            {
                throw new NetClientException(response.Data.message);
            }
            return response.Data.result;
        }

        /// <summary>
        /// 创建一个房间
        /// </summary>
        /// <returns></returns>
        public async Task<ServerRoomInfo> CreateRoomAsync()
        {
            RestRequest request = new RestRequest("/api/Room", Method.POST);
            var response = await client.ExecuteAsync<ResponseData<ServerRoomInfo>>(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new NetClientException(response.StatusDescription);
            }
            if (response.Data.code != 0)
            {
                throw new NetClientException(response.Data.message);
            }
            return response.Data.result;
        }

        /// <summary>
        /// 获取房间信息
        /// </summary>
        /// <returns></returns>
        public ServerRoomInfo[] GetRoomInfos()
        {
            RestRequest request = new RestRequest("/api/Room", Method.GET);
            var response = client.Execute<ResponseData<ServerRoomInfo[]>>(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new NetClientException(response.StatusDescription);
            }
            if (response.Data.code != 0)
            {
                throw new NetClientException(response.Data.message);
            }
            return response.Data.result;
        }

        /// <summary>
        /// 获取房间信息
        /// </summary>
        /// <returns></returns>
        public async Task<ServerRoomInfo[]> GetRoomInfosAsync()
        {
            RestRequest request = new RestRequest("/api/Room", Method.GET);
            var response = await client.ExecuteAsync<ResponseData<ServerRoomInfo[]>>(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new NetClientException(response.StatusDescription);
            }
            if (response.Data.code != 0)
            {
                throw new NetClientException(response.Data.message);
            }
            return response.Data.result;
        }

        /// <summary>
        /// 获取自己的UID
        /// </summary>
        /// <returns></returns>
        int GetUID()
        {
            return GetUserInfo().uid;
        }

        /// <summary>
        /// 获取自己的UID
        /// </summary>
        /// <returns></returns>
        async Task<int> GetUIDAsync()
        {
            return (await GetUserInfoAsync()).uid;
        }

        /// <summary>
        /// 获取用户信息
        /// </summary>
        /// <returns></returns>
        public PublicUserInfo GetUserInfo()
        {
            RestRequest request = new RestRequest("/api/User/me", Method.GET);
            var response = client.Execute<ResponseData<PublicUserInfo>>(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new NetClientException(response.StatusDescription);
            }
            if (response.Data.code != 0)
            {
                throw new NetClientException(response.Data.message);
            }
            return response.Data.result;
        }

        /// <summary>
        /// 获取用户信息
        /// </summary>
        /// <returns></returns>
        public async Task<PublicUserInfo> GetUserInfoAsync()
        {
            RestRequest request = new RestRequest("/api/User/me", Method.GET);
            var response = await client.ExecuteAsync<ResponseData<PublicUserInfo>>(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new NetClientException(response.StatusDescription);
            }
            if (response.Data.code != 0)
            {
                throw new NetClientException(response.Data.message);
            }
            return response.Data.result;
        }

        /// <summary>
        /// 注销
        /// </summary>
        public void Logout()
        {
            UserSession = "";
            UID = 0;
            // todo: 发送给服务器注销
            client.CookieContainer = new CookieContainer();
        }

        /// <summary>
        /// 注销
        /// </summary>
        /// <returns></returns>
        public async Task LogoutAsync()
        {
            // 暂时没有异步的实现
            Logout();
        }
    }

    public class PublicUserInfo
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public int uid;
        /// <summary>
        /// 用户昵称
        /// </summary>
        public string name;
        /// <summary>
        /// 用户头像ID
        /// </summary>
        public string avatar;
    }

    /// <summary>
    /// 服务器房间信息
    /// </summary>
    public class ServerRoomInfo
    {
        /// <summary>
        /// 房间ID
        /// </summary>
        public string roomID;
        /// <summary>
        /// 服务器IP
        /// </summary>
        public string ip;
        /// <summary>
        /// 服务器端口
        /// </summary>
        public int port;
        /// <summary>
        /// 房主ID
        /// </summary>
        public int ownerID;
    }


    [System.Serializable]
    public class NetClientException : System.Exception
    {
        public NetClientException() { }
        public NetClientException(string message) : base(message) { }
        public NetClientException(string message, System.Exception inner) : base(message, inner) { }
        protected NetClientException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public class MongoDBJsonSerializer : IRestSerializer
    {
        public string Serialize(object obj) => obj.ToJson();

        public string Serialize(Parameter bodyParameter) => Serialize(bodyParameter.Value);

        public T Deserialize<T>(IRestResponse response) => BsonSerializer.Deserialize<T>(response.Content);

        public string[] SupportedContentTypes { get; } =
        {
        "application/json", "text/json", "text/x-json", "text/javascript", "*+json"
    };

        public string ContentType { get; set; } = "application/json";

        public DataFormat DataFormat { get; } = DataFormat.Json;
    }

}
