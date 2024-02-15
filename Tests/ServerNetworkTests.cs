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
        /// <summary>
        /// 用户登录测试
        /// </summary>
        [Test]
        public async void UserLogin()
        {
            var serverClient = await getServetClient();
            var captcha = serverClient.GetCaptchaImage();
            // 你应该在这里转换为图像，然后输入正确的验证码
            // 测试服务器不会验证验证码是否正确
            try
            {
                await serverClient.RegisterViaPassword("test1@igsk.fun", "123456", "TestUser1");
            }
            catch (Exception) { }

            Assert.True(string.IsNullOrEmpty(await serverClient.LoginViaPassword("test1@igsk.fun", "654321")));
            var success = await serverClient.LoginViaPassword("test1@igsk.fun", "123456");
            Assert.False(string.IsNullOrEmpty(success));
            Assert.False(string.IsNullOrEmpty(serverClient.UserSession));
            Assert.NotZero(serverClient.UID);
        }
        async Task<ServerClient> getServetClient()
        {
            ServerClient serverClient = new ServerClient("http://localhost:50112");
            await serverClient.NewKratosClient();
            return serverClient;
        }
        async Task tryLogin(ServerClient serverClient)
        {
            try
            {
                await serverClient.RegisterViaPassword("test1@igsk.fun", "123456", "TestUser1");
            }
            catch { }
            var session = await serverClient.LoginViaPassword("test1@igsk.fun", "123456");
            await serverClient.LoginByKratosAsync(session);
        }

        /// <summary>
        /// 创建房间测试
        /// </summary>
        [Test]
        public async void RoomCreate()
        {
            var serverClient = await getServetClient();
            // 先登录
            await tryLogin(serverClient);

            var room = serverClient.CreateRoom();
            var rooms = serverClient.GetRoomInfos();

            Assert.NotNull(room);
            Assert.NotNull(rooms);
            Assert.AreEqual(1, rooms.Where(x => x.RoomID == room.RoomID).Count());
        }

        [Test]
        public async void VersionInfoTest()
        {
            var serverClient = await getServetClient();
            // 最新版本
            var latest = serverClient.GetLatestUpdate();
            Assert.NotNull(latest);

            // 指定版本
            var spec = serverClient.GetUpdateByVersion(latest.Version);
            Assert.NotNull(spec);

            Assert.AreEqual(latest.Version, spec.Version);
            Assert.AreEqual(latest.Date, spec.Date);
            Assert.AreEqual(latest.Android?.PackageUrl, spec.Android?.PackageUrl);
            Assert.AreEqual(latest.Win64?.PackageUrl, spec.Win64?.PackageUrl);

            // 版本列表
            var list = serverClient.GetUpdateDeltaByVersion(spec.Version);
            Assert.IsEmpty(list);
        }

        [Test]
        public async void DeckTests()
        {
            var serverClient = await getServetClient();

            await tryLogin(serverClient);

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

        async Task<string> PrepareSession()
        {
            ServerClient serverClient = new ServerClient("http://localhost:50112");
            await serverClient.NewKratosClient();
            try
            {
                await serverClient.RegisterViaPassword("test1@igsk.fun", "123456", "TestUser1");
            }
            catch { }
            var session = await serverClient.LoginViaPassword("test1@igsk.fun", "123456");
            await serverClient.LoginByKratosAsync(session);
            return await serverClient.GetSessionAsync();
        }

        /// <summary>
        /// 资源上传测试
        /// </summary>
        [Test]
        public async void TestResourceUpload()
        {
            string session = await PrepareSession();
            var client = new ResourceClient("http://localhost:50112/api/UserRes", session);

            client.UploadResource(ResourceType.CardDefine, "test_card", Encoding.UTF8.GetBytes("test_card"));
        }

        /// <summary>
        /// 资源存在性测试
        /// </summary>
        [Test]
        public async void TestResourceExists()
        {
            string session = await PrepareSession();
            var client = new ResourceClient("http://localhost:50112/api/UserRes", session);

            var exists = client.ResourceExists(ResourceType.CardDefine, "test_fake_card");
            Assert.False(exists);
        }

        /// <summary>
        /// 资源批量存在性测试
        /// </summary>
        [Test]
        public async void TestResourceExistsBatch()
        {
            string session = await PrepareSession();
            var client = new ResourceClient("http://localhost:50112/api/UserRes", session);

            var exists = client.ResourceExistsBatch(new Tuple<ResourceType, string>[] {
                new Tuple<ResourceType, string>(ResourceType.CardDefine, "test_fake_card"),
                new Tuple<ResourceType, string>(ResourceType.CardDefine, "test_fake_card2"),
            });
            Assert.AreEqual(2, exists.Length);
            Assert.False(exists[0]);
            Assert.False(exists[1]);
        }

        /// <summary>
        /// 资源下载测试
        /// </summary>
        [Test]
        public async void TestResourceDownload()
        {
            string session = await PrepareSession();
            var client = new ResourceClient("http://localhost:50112/api/UserRes", session);

            var bytes = Encoding.UTF8.GetBytes("test_card");
            client.UploadResource(ResourceType.CardDefine, "test_card", bytes);
            var downloaded = client.GetResource(ResourceType.CardDefine, "test_card");

            Assert.AreEqual(bytes, downloaded);
        }

    }
}
