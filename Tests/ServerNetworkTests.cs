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
            serverClient.Register("testuser1", "test1@igsk.fun", "123456", "TestUser1", "xxxx");

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
                serverClient.Register("testuser1", "test1@igsk.fun", "123456", "TestUser1", "xxxx");
            }
            catch { }
            serverClient.Login("testuser1", "123456", "xxxx");
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
            Assert.AreEqual(1, rooms.Where(x => x.roomID == room.roomID).Count());
        }
        public event Action action;
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

            Task task = client.joinServer(room.ip, room.port, serverClient.UserSession, room.roomID);
            yield return new WaitUntil(() => task.IsCompleted);

            Assert.AreEqual(0, client.id);
            Assert.True(isConnected);
        }
    }
}
