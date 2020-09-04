using System.Net;
using System.Net.Sockets;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using TouhouCardEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System.Reflection;
using System.Threading;
using NitoriNetwork.Common;

namespace Tests
{
    public class NetworkTests
    {
        [UnityTest]
        public IEnumerator connectTest()
        {
            UnityLogger logger = new UnityLogger();
            HostManager host = new GameObject(nameof(HostManager)).AddComponent<HostManager>();
            host.logger = logger;
            host.start();
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
            Task task = client.join(host.ip, host.port);
            yield return new WaitUntil(() => task.IsCompleted);

            Assert.AreEqual(0, client.id);
            Assert.True(isConnected);
        }
        [UnityTest]
        public IEnumerator connectTimeoutTest()
        {
            UnityLogger logger = new UnityLogger();
            ClientManager client = new GameObject(nameof(ClientManager)).AddComponent<ClientManager>();
            client.timeout = 3;
            client.logger = logger;
            client.start();
            Task task = client.join(Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString(), 9050);
            yield return new WaitForSeconds(4);

            Assert.AreEqual(-1, client.id);
            Assert.True(task.IsCanceled);
        }
        [UnityTest]
        public IEnumerator connectInvalidTest()
        {
            UnityLogger logger = new UnityLogger();
            ClientManager client = new GameObject(nameof(ClientManager)).AddComponent<ClientManager>();
            client.timeout = 3;
            client.logger = logger;
            client.start();
            Task task = client.join(Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString(), 9050);
            Assert.Throws<InvalidOperationException>(() =>
            {
                client.join(Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString(), 9050);
            });
            yield break;
        }
        [UnityTest]
        public IEnumerator sendTest()
        {
            UnityLogger logger = new UnityLogger();
            HostManager host = new GameObject(nameof(HostManager)).AddComponent<HostManager>();
            host.logger = logger;
            host.start();
            ClientManager client = new GameObject(nameof(ClientManager)).AddComponent<ClientManager>();
            client.logger = logger;
            client.start();
            _ = client.join(Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString(), host.port);
            yield return new WaitForSeconds(.5f);
            Task<int> task = client.send(1);
            yield return new WaitUntil(() => task.IsCompleted);

            Assert.AreEqual(1, task.Result);
        }
        [UnityTest]
        public IEnumerator sendToMultiplayerTest()
        {
            UnityLogger logger = new UnityLogger();
            HostManager host = new GameObject(nameof(HostManager)).AddComponent<HostManager>();
            host.logger = logger;
            host.start();
            ClientManager[] clients = new ClientManager[5];
            Dictionary<int, object> receivedDic = new Dictionary<int, object>();
            for (int i = 0; i < clients.Length; i++)
            {
                clients[i] = new GameObject(nameof(ClientManager)).AddComponent<ClientManager>();
                int localI = i;
                clients[i].onReceive += (id, obj) =>
                {
                    receivedDic.Add(clients[localI].id, obj);
                    return Task.CompletedTask;
                };
                clients[i].logger = logger;
                clients[i].start();
                Task task = clients[i].join(Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString(), host.port);
                yield return new WaitUntil(() => task.IsCompleted);
            }
            Task<int> sendTask = clients[0].send(1);
            yield return new WaitUntil(() => sendTask.IsCompleted && clients.All(c => receivedDic.Keys.Contains(c.id)));

            Assert.True(sendTask.IsCompleted);
            Assert.AreEqual(1, sendTask.Result);
            Assert.True(clients.All(c => receivedDic.ContainsKey(c.id) && receivedDic[c.id] is int i && i == 1));
        }
        [UnityTest]
        public IEnumerator disconnectTest()
        {
            UnityLogger logger = new UnityLogger();
            HostManager host = new GameObject(nameof(HostManager)).AddComponent<HostManager>();
            host.logger = logger;
            host.start();
            ClientManager client = new GameObject(nameof(ClientManager)).AddComponent<ClientManager>();
            client.logger = logger;
            client.start();
            Task task = client.join(Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString(), host.port);
            yield return new WaitUntil(() => task.IsCompleted);
            bool isDisconnected = false;
            client.onDisconnect += onDisconnect;
            client.disconnect();
            void onDisconnect()
            {
                isDisconnected = true;
            }
            yield return new WaitForSeconds(.5f);

            Assert.True(isDisconnected);
        }
        /// <summary>
        /// 创建两个Host1,2和一个Client，Client先加入Host1，能正常收发数据，断开连接加入Host2，能与Host2正常收发数据，但是Host1接受不到数据，也不能向Host1发送。
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator changeHostTest()
        {
            UnityLogger logger = new UnityLogger();
            HostManager host1 = new GameObject(nameof(HostManager) + "1").AddComponent<HostManager>();
            host1.logger = logger;
            host1.start();
            HostManager host2 = new GameObject(nameof(HostManager) + "2").AddComponent<HostManager>();
            host2.logger = logger;
            host2.start();
            ClientManager client = new GameObject(nameof(ClientManager) + "0").AddComponent<ClientManager>();
            client.logger = logger;
            client.start();
            ClientManager client1 = new GameObject(nameof(ClientManager) + "1").AddComponent<ClientManager>();
            client1.logger = logger;
            client1.start();
            ClientManager client2 = new GameObject(nameof(ClientManager) + "2").AddComponent<ClientManager>();
            client2.logger = logger;
            client2.start();

            int recvCount = 0;
            bool flag = false;

            client1.onReceive += (id, data) => { Assert.AreEqual((int)data, 1); recvCount++; flag = true; return Task.CompletedTask; };
            client2.onReceive += (id, data) => { Assert.AreEqual((int)data, 2); recvCount++; flag = true; return Task.CompletedTask; };

            var task = client1.join("127.0.0.1", host1.port);
            yield return new WaitUntil(() => task.IsCompleted);
            task = client2.join("127.0.0.1", host2.port);
            yield return new WaitUntil(() => task.IsCompleted);

            task = client.join("127.0.0.1", host1.port);
            yield return new WaitUntil(() => task.IsCompleted);

            flag = false;
            task = client.send(1);
            yield return new WaitUntil(() => task.IsCompleted);
            yield return new WaitForSeconds(1f);
            if (!flag)
                throw new TimeoutException("Client1 接收超时。");

            client.disconnect();
            yield return new WaitForSeconds(0.5f);

            task = client.join("127.0.0.1", host2.port);
            yield return new WaitUntil(() => task.IsCompleted);

            flag = false;
            task = client.send(2);
            yield return new WaitUntil(() => task.IsCompleted);
            yield return new WaitForSeconds(1f);
            if (!flag)
                throw new TimeoutException("Client2 接收超时。");

            client.disconnect();

            Assert.AreEqual(recvCount, 2);
        }

        /// <summary>
        /// 创建一个Host和两个Client，Client1先加入Host，验证数据收发正常，断开连接，无法收发数据，Client2加入Host，验证数据收发正常，Client1无法收到数据。
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator changeClientTest()
        {
            UnityLogger logger = new UnityLogger();
            HostManager host = new GameObject(nameof(HostManager) + "1").AddComponent<HostManager>();
            host.logger = logger;
            host.start();
            ClientManager client = new GameObject(nameof(ClientManager) + "0").AddComponent<ClientManager>();
            client.logger = logger;
            client.start();
            ClientManager client1 = new GameObject(nameof(ClientManager) + "1").AddComponent<ClientManager>();
            client1.logger = logger;
            client1.start();
            ClientManager client2 = new GameObject(nameof(ClientManager) + "2").AddComponent<ClientManager>();
            client2.logger = logger;
            client2.start();

            int recvCount = 0;
            bool flag = false;

            client1.onReceive += (id, data) => { Assert.AreEqual((int)data, 1); recvCount++; flag = true; return Task.CompletedTask; };
            client2.onReceive += (id, data) => { Assert.AreEqual((int)data, 2); recvCount++; flag = true; return Task.CompletedTask; };

            var task = client.join("127.0.0.1", host.port);
            yield return new WaitUntil(() => task.IsCompleted);

            task = client1.join("127.0.0.1", host.port);
            yield return new WaitUntil(() => task.IsCompleted);

            flag = false;
            task = client.send(1);
            yield return new WaitUntil(() => task.IsCompleted);
            yield return new WaitForSeconds(1f);
            if (!flag)
                throw new TimeoutException("Client1 接收超时。");

            client1.disconnect();
            yield return new WaitForSeconds(0.5f);

            task = client2.join("127.0.0.1", host.port);
            yield return new WaitUntil(() => task.IsCompleted);

            flag = false;
            task = client.send(2);
            yield return new WaitUntil(() => task.IsCompleted);
            yield return new WaitForSeconds(1f);
            if (!flag)
                throw new TimeoutException("Client2 接收超时。");

            client2.disconnect();
        }
        /// <summary>
        /// 创建一个Host和一个Client，Client调用findRoom，没有回应结果。Host调用openRoom，开启局域网发现，Client.findRoom会返回Host的Room信息。
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator openRoomAndFindRoomTest()
        {
            UnityLogger logger = new UnityLogger();
            HostManager host = new GameObject(nameof(HostManager) + "1").AddComponent<HostManager>();
            host.logger = logger;
            host.start();

            ClientManager client = new GameObject(nameof(ClientManager) + "0").AddComponent<ClientManager>();
            client.logger = logger;
            client.start();

            bool roomOpened, flag = false;

            roomOpened = false;
            client.onRoomFound += (info) =>
            {
                Assert.True(roomOpened);
                flag = true;
            };
            client.findRoom(host.port);
            yield return new WaitForSeconds(1f);

            roomOpened = true;
            host.openRoom(new RoomInfo()
            {
                ip = "127.0.0.1",
                port = host.port,
                playerList = new List<RoomPlayerInfo>()
            });

            client.findRoom(host.port);
            yield return new WaitForSeconds(1);
            if (!flag)
                throw new TimeoutException("局域网发现超时");
        }

        /// <summary>
        /// client.join(room)，host没有开房则请求没有回应。
        /// host.createRoom,client.findRoom,client.joinRoom(room)，则加入房间成功，触发client.onJoinRoom(room)事件和host.onClientJoin(player)事件。
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator joinRoomTest()
        {
            UnityLogger logger = new UnityLogger();
            HostManager host = new GameObject(nameof(HostManager)).AddComponent<HostManager>();
            host.logger = logger;
            host.start();

            ClientManager client = new GameObject(nameof(ClientManager)).AddComponent<ClientManager>();
            client.logger = logger;
            client.start();

            RoomPlayerInfo playerInfo = new RoomPlayerInfo()
            {
                name = "测试名字"
            };
            RoomInfo info = new RoomInfo() { ip = "127.0.0.1", port = host.port, playerList = new List<RoomPlayerInfo>() };

            bool hostRoomCreated = false, joinSuccessHost = false, joinSuccessClient = false;
            bool joinLock = false;

            host.onPlayerJoin += (p) =>
            {
                joinSuccessHost = true;
                Assert.True(hostRoomCreated);
                Assert.AreEqual(p.name, playerInfo.name);
            };
            client.onRoomFound += async (r) =>
            {
                if (!joinLock)
                {
                    joinLock = true;
                    await client.joinRoom(r, playerInfo);
                }
            };

            client.onJoinRoom += (p) =>
            {
                joinSuccessClient = true;
                Assert.AreEqual(p.port, info.port);
            };

            var task = client.joinRoom(info, playerInfo);
            yield return new WaitUntil(() => task.IsCompleted);
            yield return new WaitForSeconds(0.5f);

            host.openRoom(info);
            hostRoomCreated = true;

            client.findRoom(host.port);
            yield return new WaitForSeconds(1f);

            if (!joinSuccessClient)
                throw new TimeoutException("客户端加入超时。");
            if (!joinSuccessHost)
                throw new TimeoutException("服务端加入超时。");
        }
        /// <summary>
        /// 加入房间后，client.quitRoom，触发client.onQuitRoom(room)事件和host.onClientQuit(player)事件。
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator clientQuitRoomTest()
        {
            UnityLogger logger = new UnityLogger();
            HostManager host = new GameObject(nameof(HostManager)).AddComponent<HostManager>();
            host.logger = logger;
            host.start();

            ClientManager client = new GameObject(nameof(ClientManager)).AddComponent<ClientManager>();
            client.logger = logger;
            client.start();

            RoomPlayerInfo playerInfo = new RoomPlayerInfo() { name = "测试名字" };
            RoomInfo roomInfo = new RoomInfo() { ip = "127.0.0.1", port = host.port };

            host.openRoom(roomInfo);
            yield return new WaitForSeconds(0.5f);

            var task = client.joinRoom(roomInfo, playerInfo);
            yield return new WaitUntil(() => task.IsCompleted);

            bool quitHost = false, quitClient = false, roomJoined = false;

            client.onQuitRoom += () =>
            {
                quitClient = true;
            };
            host.onPlayerQuit += (p) =>
            {
                Assert.AreEqual(p.name, playerInfo.name);
                quitHost = true;
            };
            client.onJoinRoom += (room) =>
            {
                roomJoined = true;
            };

            yield return new WaitUntil(() => roomJoined);
            client.quitRoom();

            yield return new WaitForSeconds(1);

            if (!quitClient)
                throw new TimeoutException("客户端退出超时。");
            if (!quitHost)
                throw new TimeoutException("服务端退出超时。");
        }
        /// <summary>
        /// 加入房间后，host.closeRoom，触发client.onQuitRoom(room)事件。
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator hostCloseRoomTest()
        {
            HostManager host;
            ClientManager client;
            createHostClient(out host, out client);

            RoomPlayerInfo playerInfo = new RoomPlayerInfo() { name = "测试名字" };
            RoomInfo roomInfo = new RoomInfo() { ip = "127.0.0.1", port = host.port };

            host.openRoom(roomInfo);
            yield return new WaitForSeconds(0.5f);

            var task = client.joinRoom(roomInfo, playerInfo);
            yield return new WaitUntil(() => task.IsCompleted);

            bool quitClient = false;
            client.onQuitRoom += () =>
            {
                quitClient = true;
            };

            host.closeRoom();
            yield return new WaitForSeconds(1);

            if (!quitClient)
                throw new TimeoutException("客户端退出超时。");
        }

        private static void createHostClient(out HostManager host, out ClientManager client)
        {
            UnityLogger logger = new UnityLogger();
            host = new GameObject(nameof(HostManager)).AddComponent<HostManager>();
            host.logger = logger;
            host.start();

            client = new GameObject(nameof(ClientManager)).AddComponent<ClientManager>();
            client.logger = logger;
            client.start();
        }

        /// <summary>
        /// 当client在房间中的时候，当有其他client加入和退出的时候，应该会触发onRoomInfoUpdate事件。
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator roomInfoUpdateTest_WhenPlayerJoinAndQuit()
        {
            HostManager host;
            ClientManager client1, client2;
            createHostClient12(out host, out client1, out client2);

            createRoomPlayer12(host, out RoomPlayerInfo playerInfo1, out RoomPlayerInfo playerInfo2, out RoomInfo roomInfo);

            host.openRoom(roomInfo);
            yield return new WaitForSeconds(0.5f);

            var task = client1.joinRoom(roomInfo, playerInfo1);
            yield return new WaitUntil(() => task.IsCompleted);

            bool updateTrigger = false;

            client1.onRoomInfoUpdate += (before, now) =>
            {
                Assert.True(now.playerList.Where(p => p.name == playerInfo2.name).Count() > 0);
                updateTrigger = true;
            };

            task = client2.joinRoom(roomInfo, playerInfo2);
            yield return new WaitUntil(() => task.IsCompleted);

            yield return new WaitForSeconds(1);

            if (!updateTrigger)
                throw new TimeoutException("信息更新超时。");
        }

        [UnityTest]
        public IEnumerator removeTest()
        {
            HostManager host;
            ClientManager client1, client2;
            createHostClient12(out host, out client1, out client2);

            RoomPlayerInfo playerInfo1, playerInfo2;
            RoomInfo roomInfo;
            createRoomPlayer12(host, out playerInfo1, out playerInfo2, out roomInfo);

            host.openRoom(roomInfo);
            yield return new WaitForSeconds(0.5f);

            var task = client1.joinRoom(roomInfo, playerInfo1);
            yield return new WaitUntil(() => task.IsCompleted);

            task = client2.joinRoom(roomInfo, playerInfo2);
            yield return new WaitUntil(() => task.IsCompleted);

            yield return new WaitForSeconds(0.5f);

            bool updateTrigger = false;

            client1.onRoomInfoUpdate += (before, now) =>
            {
                Assert.True(now.playerList.Where(p => p.name == playerInfo2.name).Count() == 0);
                updateTrigger = true;
            };

            task = client1.invokeHost(RPCHelper.RemovePlayer(playerInfo2.PlayerID));
            yield return new WaitForSeconds(1);

            if (!updateTrigger)
                throw new TimeoutException("移除超时。");
        }

        private static void createRoomPlayer12(HostManager host, out RoomPlayerInfo playerInfo1, out RoomPlayerInfo playerInfo2, out RoomInfo roomInfo)
        {
            playerInfo1 = new RoomPlayerInfo() { name = "测试名字1" };
            playerInfo2 = new RoomPlayerInfo() { name = "测试名字2" };
            playerInfo2.PlayerID = playerInfo1.PlayerID + 1;

            roomInfo = new RoomInfo() { ip = "127.0.0.1", port = host.port, OwnerID = playerInfo1.PlayerID };
        }

        /// <summary>
        /// 当客户端请求更新用户信息时，应当触发roomInfoUpdate的事件
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator roomInfoUpdateTest_RequestUpdate()
        {
            HostManager host;
            ClientManager client1, client2;
            createHostClient12(out host, out client1, out client2);

            createRoomPlayer12(host, out RoomPlayerInfo playerInfo1, out RoomPlayerInfo playerInfo2, out RoomInfo roomInfo);

            host.openRoom(roomInfo);
            yield return new WaitForSeconds(0.5f);

            var task = client1.joinRoom(roomInfo, playerInfo1);
            yield return new WaitUntil(() => task.IsCompleted);

            task = client2.joinRoom(roomInfo, playerInfo2);
            yield return new WaitUntil(() => task.IsCompleted);

            yield return new WaitForSeconds(0.5f);

            bool updateTrigger = false;

            playerInfo2.name = "测试名字3";
            client1.onRoomInfoUpdate += (before, now) =>
            {
                var player = now.playerList.Where(p => p.RoomID == playerInfo2.RoomID).First();
                Assert.AreEqual(playerInfo2.name, player.name);
                updateTrigger = true;
            };
            task = client2.updatePlayerInfo(playerInfo2);
            yield return new WaitUntil(() => task.IsCompleted);

            yield return new WaitForSeconds(1);

            if (!updateTrigger)
                throw new TimeoutException("信息更新超时。");
        }
        private static void createHostClient12(out HostManager host, out ClientManager client1, out ClientManager client2)
        {
            UnityLogger logger = new UnityLogger();
            host = new GameObject(nameof(HostManager) + "1").AddComponent<HostManager>();
            host.logger = logger;
            host.start();
            client1 = new GameObject(nameof(ClientManager) + "1").AddComponent<ClientManager>();
            client1.logger = logger;
            client1.start();
            client2 = new GameObject(nameof(ClientManager) + "2").AddComponent<ClientManager>();
            client2.logger = logger;
            client2.start();
        }

        /// <summary>
        /// Host调用updateRoomInfo，预期Host.roomInfo与传入的roomInfo相同，并且所有Client收到onRoomInfoUpdate(roomInfo)事件
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator roomInfoUpdateTest()
        {
            return roomInfoUpdateTest_WhenPlayerJoinAndQuit();
        }
        /// <summary>
        /// 存在Host和ClientA,B，Host新建房间，A发现房间获得房间信息，A.checkRoomInfo返回房间信息不变，
        /// B加入房间A.checkRoomInfo发现房间里多了B，Host关闭房间，A.checkRoomInfo返回房间为空，表示Host关闭了房间
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator checkRoomInfoTest()
        {
            UnityLogger logger = new UnityLogger();
            HostManager host = new GameObject(nameof(HostManager) + "1").AddComponent<HostManager>();
            host.logger = logger;
            host.start();
            ClientManager client1 = new GameObject(nameof(ClientManager) + "1").AddComponent<ClientManager>();
            client1.logger = logger;
            client1.start();
            ClientManager client2 = new GameObject(nameof(ClientManager) + "2").AddComponent<ClientManager>();
            client2.logger = logger;
            client2.start();

            RoomPlayerInfo playerInfo1 = new RoomPlayerInfo() { name = "测试名字1" };
            RoomPlayerInfo playerInfo2 = new RoomPlayerInfo() { name = "测试名字2" };
            RoomInfo roomInfo = new RoomInfo() { ip = "127.0.0.1", port = host.port };
            RoomInfo roomInfo2 = new RoomInfo() { ip = "127.0.0.1", port = host.port };

            host.openRoom(roomInfo);
            yield return new WaitForSeconds(0.5f);

            bool roomFoundFlag = false;
            RoomInfo questRoomInfo = null;

            client1.onRoomFound += (info) =>
            {
                if (!roomFoundFlag)
                {
                    roomFoundFlag = true;
                    questRoomInfo = info;
                }
            };
            client1.findRoom(host.port);
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(roomFoundFlag, true);

            var task = client1.checkRoomInfo(questRoomInfo);
            yield return new WaitUntil(() => task.IsCompleted);
            Assert.NotNull(task.Result);
            Assert.AreEqual(questRoomInfo.playerList.Count, task.Result.playerList.Count);
            Assert.AreEqual(questRoomInfo.ip, task.Result.ip);
            Assert.AreEqual(questRoomInfo.port, task.Result.port);

            yield return new WaitForSeconds(0.5f);

            var task2 = client2.joinRoom(roomInfo2, playerInfo2);
            yield return new WaitUntil(() => task2.IsCompleted);
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(1, host.room.playerList.Count);

            task = client1.checkRoomInfo(questRoomInfo);
            yield return new WaitUntil(() => task.IsCompleted);
            Assert.NotNull(task.Result);
            Assert.AreEqual(1, task.Result.playerList.Count);
            Assert.AreEqual(playerInfo2.name, task.Result.playerList[0].name);

            host.closeRoom();
            yield return new WaitForSeconds(0.5f);

            task = client1.checkRoomInfo(questRoomInfo);
            yield return new WaitUntil(() => task.IsCompleted);
            Assert.Null(task.Result);
        }
        [Test]
        public void roomInfoSerializeTest()
        {
            RoomInfo roomInfo = new RoomInfo()
            {
                ip = "192.168.0.1",
                port = 9050,
                playerList = new List<RoomPlayerInfo>()
                {

                },
                runtimeDic = new Dictionary<string, object>()
                {
                    { "name", 1 }
                }
            };
            string json = roomInfo.serialize().ToJson();
            roomInfo = BsonSerializer.Deserialize<RoomInfo>(json).deserialize();

            Assert.AreEqual("192.168.0.1", roomInfo.ip);
            Assert.AreEqual(9050, roomInfo.port);
            Assert.AreEqual(0, roomInfo.playerList.Count);
            Assert.AreEqual(1, roomInfo.runtimeDic.Count);
            Assert.AreEqual("name", roomInfo.runtimeDic.FirstOrDefault().Key);
            object obj = roomInfo.getProp("name");
            Debug.Log(obj.GetType().Name);
            Assert.AreEqual(1, obj);
        }
        [UnityTest]
        public IEnumerator invokeTest()
        {
            createHostClient(out var host, out var client);

            client.addInvokeTarget(new InvokeTarget());

            Task task = client.join(host.ip, host.port);
            yield return waitTask(task);
            task = host.invoke<bool>(client.id, nameof(InvokeTarget.method), 1);
            yield return waitTask(task);
            Assert.AreEqual(true, (task as Task<bool>).Result);
        }
        [UnityTest]
        public IEnumerator invokeTest_Exception()
        {
            createHostClient(out var host, out var client);

            client.addInvokeTarget(new InvokeTarget());

            Task task = client.join(host.ip, host.port);
            yield return waitTask(task);
            task = host.invoke<object>(client.id, nameof(InvokeTarget.exception));
            yield return waitTask(task);
            Assert.True(task.IsFaulted);
        }
        [UnityTest]
        public IEnumerator invokeTest_Timeout()
        {
            createHostClient(out var host, out var client);

            host.timeout = 1;
            client.addInvokeTarget(new InvokeTarget());

            Task task = client.join(host.ip, host.port);
            yield return waitTask(task);
            task = host.invoke<object>(client.id, new RPCRequest(typeof(void),nameof(InvokeTarget.delay), 500));
            yield return waitTask(task);
            Assert.False(task.IsCanceled);
            task = host.invoke<object>(client.id, new RPCRequest(typeof(void), nameof(InvokeTarget.delay), 1001));
            yield return waitTask(task);
            Assert.True(task.IsCanceled);
        }
        [UnityTest]
        public IEnumerator invokeTest_Multi()
        {
            createHostClient(out var host, out var client);
            client.addInvokeTarget(new InvokeTarget());

            Task task = client.join(host.ip, host.port);
            yield return waitTask(task);
            List<Task> taskList = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                task = host.invoke<bool>(client.id, nameof(InvokeTarget.method), i);
                taskList.Add(task);
            }
            yield return new WaitUntil(() => taskList.All(t => t.IsCanceled || t.IsFaulted || t.IsCompleted));
            for (int i = 0; i < taskList.Count; i++)
            {
                if (i == 0)
                    Assert.False((taskList[i] as Task<bool>).Result);
                else
                    Assert.True((taskList[i] as Task<bool>).Result);
            }
        }
        class InvokeTarget
        {
            public bool method(int i)
            {
                return i > 0;
            }
            public void exception()
            {
                throw new Exception("哦艹");
            }
            public void delay(int msec)
            {
                Thread.Sleep(msec);
            }
        }
        private static WaitUntil waitTask(Task task)
        {
            return new WaitUntil(() => task.IsCompleted || task.IsCanceled || task.IsFaulted);
        }
        [UnityTest]
        public IEnumerator invokeAllTest()
        {
            createHostClient12(out var host, out var client1, out var client2);

            host.timeout = 1;
            client1.addInvokeTarget(new InvokeTarget());
            client2.addInvokeTarget(new InvokeTarget());

            Task task = client1.join(host.ip, host.port);
            yield return waitTask(task);
            task = client2.join(host.ip, host.port);
            yield return waitTask(task);
            task = host.invokeAll<bool>(new int[] { client1.id, client2.id }, nameof(InvokeTarget.method), 1);
            yield return waitTask(task);
            Dictionary<int, bool> result = (task as Task<Dictionary<int, bool>>).Result;
            Assert.AreEqual(2, result.Count);
            Assert.True(result[client1.id]);
            Assert.True(result[client2.id]);
        }
    }
}
