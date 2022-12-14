using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using TouhouCardEngine;
using System.Threading.Tasks;
using NitoriNetwork.Common;
using System.Linq;
using System;
using System.Text;
namespace Tests
{
    public class ServerNetworkTests
    {
        ServerClient serverClient = new ServerClient("http://localhost:50112");
        /// <summary>
        /// 用户登录测试
        /// </summary>
        [Test]
        public void UserLogin()
        {
            var captcha = serverClient.GetCaptchaImage();
            // 你应该在这里转换为图像，然后输入正确的验证码
            // 测试服务器不会验证验证码是否正确
            try
            {
                serverClient.Register("testuser1", "test1@igsk.fun", "123456", "TestUser1", null, "xxxx");
            }
            catch (Exception e) { }

            Assert.False(serverClient.Login("testuser1", "654321", "xxxx"));
            var success = serverClient.Login("testuser1", "123456", "xxxx");
            Assert.True(success);
            Assert.False(string.IsNullOrEmpty(serverClient.UserSession));
            Assert.NotZero(serverClient.UID);
        }

        void tryLogin()
        {
            try
            {
                serverClient.Register("testuser1", "test1@igsk.fun", "123456", "TestUser1", null, "xxxx");
            }
            catch { }
            bool success = serverClient.Login("testuser1", "123456", "xxxx");
            if (!success)
            {
                throw new Exception("测试用账户无法登录，请确保测试用服务器数据库干净");
            }
            serverClient.GetSession();
        }

        /// <summary>
        /// 创建房间测试
        /// </summary>
        [Test]
        public void RoomCreate()
        {
            // 先登录
            tryLogin();

            var room = serverClient.CreateRoom();
            var rooms = serverClient.GetRoomInfos();

            Assert.NotNull(room);
            Assert.NotNull(rooms);
            Assert.AreEqual(1, rooms.Where(x => x.RoomID == room.RoomID).Count());
        }
        public event System.Action action;
        //[UnityTest]
        //public IEnumerator ConnectServerTest()
        //{
        //    UnityLogger logger = new UnityLogger();
        //    ClientManager client = new GameObject(nameof(ClientManager)).AddComponent<ClientManager>();
        //    client.logger = logger;
        //    bool isConnected = false;
        //    client.onConnected += () =>
        //    {
        //        isConnected = true;
        //        Debug.Log("测试连接成功");
        //        return Task.CompletedTask;
        //    };
        //    client.start();
        //    action = null;
        //    tryLogin();
        //    var room = serverClient.CreateRoom();

        //    Task task = client.JoinServer(room.IP, room.Port, serverClient.UserSession, room.RoomID);
        //    yield return new WaitUntil(() => task.IsCompleted);

        //    Assert.AreEqual(0, client.id);
        //    Assert.True(isConnected);
        //}

        [Test]
        public void VersionInfoTest()
        {
            // 最新版本
            var latest = serverClient.GetLatestUpdate();
            Assert.NotNull(latest);

            // 指定版本
            var spec = serverClient.GetUpdateByVersion(latest.Version);
            Assert.NotNull(spec);

            Assert.AreEqual(latest.Version, spec.Version);
            Assert.AreEqual(latest.Date, spec.Date);
            Assert.AreEqual(latest.DeltaPackageUrl, spec.DeltaPackageUrl);
            Assert.AreEqual(latest.FullPackageUrl, spec.FullPackageUrl);

            // 版本列表
            var list = serverClient.GetUpdateDeltaByVersion(spec.Version);
            Assert.IsEmpty(list);
        }

        [Test]
        public void DeckTests()
        {
            tryLogin();

            var decks = new DeckDataItem[] {
                new DeckDataItem()
                {
                     ID = 0,
                     Name = "测试卡组0",
                     Content = "AAAA",
                },
                new DeckDataItem()
                {
                     ID = 1,
                     Name = "测试卡组1",
                     Content = "BBBB",
                },
            };

            serverClient.SetUserDecks(decks);

            var getDecks = serverClient.GetUserDecks();

            Assert.AreEqual(decks[0].Name, getDecks[0].Name);
            Assert.AreEqual(decks[0].ID, getDecks[0].ID);
            Assert.AreEqual(decks[1].Name, getDecks[1].Name);
            Assert.AreEqual(decks[1].ID, getDecks[1].ID);
        }
    }

    public class WPClientTests
    {
        WordPressRestfulClient wpc = new WordPressRestfulClient("https://thg.igsk.fun");

        /// <summary>
        /// 用户登录测试
        /// </summary>
        [Test]
        public void GetPostsTest()
        {
            var posts = wpc.GetPosts(new int[] { 3 });
            Assert.Greater(posts.Length, 0);

            Debug.Log(posts[0].date);
        }
    }

    public class ResourceClientTests
    {
        ServerClient serverClient = new ServerClient("http://localhost:50112");

        string PrepareSession()
        {
            try
            {
                serverClient.Register("testuser1", "test1@igsk.fun", "123456", "TestUser1", null, "xxxx");
            }
            catch { }
            serverClient.Login("testuser1", "123456", "xxxx");
            return serverClient.GetSession();
        }

        /// <summary>
        /// 资源上传测试
        /// </summary>
        [Test]
        public void TestResourceUpload()
        {
            string session = PrepareSession();
            var client = new ResourceClient("http://localhost:50112/api/UserRes", session);

            client.UploadResource(ResourceType.CardDefine, "test_card", Encoding.UTF8.GetBytes("test_card"));
        }

        /// <summary>
        /// 资源存在性测试
        /// </summary>
        [Test]
        public void TestResourceExists()
        {
            string session = PrepareSession();
            var client = new ResourceClient("http://localhost:50112/api/UserRes", session);

            var exists = client.ResourceExists(ResourceType.CardDefine, "test_fake_card");
            Assert.False(exists);
        }

        /// <summary>
        /// 资源下载测试
        /// </summary>
        [Test]
        public void TestResourceDownload()
        {
            string session = PrepareSession();
            var client = new ResourceClient("http://localhost:50112/api/UserRes", session);

            var bytes = Encoding.UTF8.GetBytes("test_card");
            client.UploadResource(ResourceType.CardDefine, "test_card", bytes);
            var downloaded = client.GetResource(ResourceType.CardDefine, "test_card");

            Assert.AreEqual(bytes, downloaded);
        }

    }
}
