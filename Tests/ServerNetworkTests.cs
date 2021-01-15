using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using TouhouCardEngine;
using System.Threading.Tasks;
using NitoriNetwork.Common;
using System.Linq;
using System;
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
            _ = serverClient.Login("testuser1", "123456", "xxxx");
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
            Assert.AreEqual(1, rooms.Where(x => x.id == room.id).Count());
        }
        public event System.Action action;
        [UnityTest]
        public IEnumerator ConnectServerTest()
        {
            UnityLogger logger = new UnityLogger();
            ClientManager client = new GameObject(nameof(ClientManager)).AddComponent<ClientManager>();
            client.logger = logger;
            bool isConnected = false;
            client.onConnected += () =>
            {
                isConnected = true;
                Debug.Log("测试连接成功");
                return Task.CompletedTask;
            };
            client.start();
            action = null;
            tryLogin();
            var room = serverClient.CreateRoom();

            Task task = client.JoinServer(room.ip, room.port, serverClient.UserSession, room.id);
            yield return new WaitUntil(() => task.IsCompleted);

            Assert.AreEqual(0, client.id);
            Assert.True(isConnected);
        }

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
    }
}
