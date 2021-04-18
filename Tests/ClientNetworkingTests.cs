using NitoriNetwork.Common;
using NUnit.Framework;
using System;
using System.Collections;
using TouhouCardEngine;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using BJSYGameCore;
namespace Tests
{
    public class ClientNetworkingTests
    {
        #region 局域网测试
        [UnityTest]
        public IEnumerator LANGetLocalPlayerTest()
        {
            yield return startNetworkAndAssert(10, startLANClient, getLocalPlayerAssert);
        }
        [UnityTest]
        public IEnumerator LANCreateRoomTest()
        {
            yield return startNetworkAndAssert(10, startLANClient, createRoomAssert);
        }
        //[UnityTest]
        //public IEnumerator LANRefreshRoomsTest()
        //{
        //    yield return startNetworkAndAssert(10, startLANNetworking, refreshRoomsAssert);
        //}
        [UnityTest]
        public IEnumerator LANGetRoomsTest()
        {
            yield return startNetworkAndAssert(10, startLANClient, getRoomsAssert);
        }
        [UnityTest]
        public IEnumerator LANJoinRoomTest()
        {
            yield return startNetworkAndAssert(10, startLANClient, joinRoomAssert);
        }
        [UnityTest]
        public IEnumerator LANRoomAddAIPlayerTest()
        {
            yield return startNetworkAndAssert(10, startLANClient, addAIPlayerAssert);
        }
        [UnityTest]
        public IEnumerator LANRoomSetPropTest()
        {
            yield return startNetworkAndAssert(10, startLANClient, setRoomPropAssert);
        }
        [UnityTest]
        public IEnumerator LANRoomPlayerSetPropTest()
        {
            yield return startNetworkAndAssert(10, startLANClient, setPlayerPropAssert);
        }
        [UnityTest]
        public IEnumerator LANQuitRoomTest()
        {
            yield return startNetworkAndAssert(10, startLANClient, quitRoomAssert);
        }
        #endregion
        #region 通用测试
        IEnumerator getLocalPlayerAssert(ClientLogic[] clients)
        {
            var idSet = clients.Select(c => c.getLocalPlayerData().id);
            //确保每一个客户端网络都有独一无二的PlayerId
            Assert.AreEqual(idSet.Count(), idSet.Distinct().Count());
            yield break;
        }
        IEnumerator createRoomAssert(ClientLogic[] clients)
        {
            //一个人创建房间，发送广播通知给所有人
            yield return clients[0].createOnlineRoom().wait();
            //所有人都会收到这个房间的新建通知
            yield return TestHelper.waitUntil(() =>
            {
                var room = clients[0].room;
                if (room == null)
                    return false;
                return clients.All(c => c == clients[0] || c.roomList.Any(r => r.Value.RoomID == room.ID));
            }, 5);
        }
        //IEnumerator refreshRoomsAssert(ClientNetworking[] clients)
        //{
        //    RoomData[] rooms = new RoomData[clients.Length];
        //    //第一个人创建房间，但是谁都不告诉
        //    Task<RoomData> rdt = clients[0].createRoom(clients[0].getLocalPlayerData(), new int[0]);
        //    yield return rdt.wait();
        //    clients[0].onGetRoomReq += () => rdt.Result;
        //    //所有人刷新房间，应该可以刷出来这个房间
        //    for (int i = 0; i < clients.Length; i++)
        //    {
        //        int I = i;
        //        clients[i].onUpdateRoom += r => rooms[I] = r;
        //        clients[i].refreshRooms(getPorts(clients));
        //    }
        //    yield return TestHelper.waitUntil(() => rooms.All(r => r != null), 5);
        //    Assert.True(rooms.All(r => r != null));
        //}
        /// <summary>
        /// 获取当前所连接网络中所有创建的房间，可以立刻返回，也可以在不超时的范围内通过onNewRoom返回。
        /// </summary>
        /// <param name="clients"></param>
        /// <returns></returns>
        IEnumerator getRoomsAssert(ClientLogic[] clients)
        {
            //bool result = true;
            //List<RoomData>[] rooms = new List<RoomData>[clients.Length];
            //for (int i = 0; i < clients.Length; i++)
            //{
            //    int I = i;
            //    rooms[I] = new List<RoomData>();
            //    clients[I].onNewRoomNtf += r => rooms[I].Add(r);
            //}
            //一个人创建房间，发送广播通知给所有人
            yield return clients[0].createOnlineRoom().wait();
            //第二个人创建房间，发送广播通知给所有人
            yield return clients[1].createOnlineRoom().wait();
            //每一个人获取房间列表都能获取到两个创建的房间
            yield return TestHelper.waitUntil(() =>
            {
                // 每个玩家是看不到自己创建的房间的，这里应该是1才对
                return clients.All(c => c.roomList.Count == 1);
            }, 5);
            //foreach (var client in clients)
            //{
            //    Task<RoomData[]> roomsTask = client.getRooms();
            //    yield return roomsTask.wait(5);
            //    if (roomsTask.Result.Length != 2)
            //        result = false;
            //}
            //if (result)
            //    yield break;
            //yield return new WaitForSeconds(5);
            //Assert.True(rooms.All(l => l.Count == 2));
        }
        //IEnumerator joinRoomAssert(ClientLogic[] clients)
        //{
        //    //所有人都会收到房间状态变化的消息
        //    RoomData[] updateRooms = new RoomData[clients.Length];
        //    for (int i = 0; i < clients.Length; i++)
        //    {
        //        int I = i;
        //        clients[i].onUpdateRoom += r => updateRooms[I] = r;
        //    }
        //    //一个人创建房间
        //    yield return clients[0].createOnlineRoom().wait();
        //    //第二个人加入房间
        //    Task<RoomData[]> roomsTask = clients[1].getRooms();
        //    yield return roomsTask.wait();
        //    RoomData room = roomsTask.Result[0];
        //    //regLANOnJoinRoomReq(clients[0] as LANNetworking, room);
        //    Assert.NotNull(room);
        //    Task<RoomData> roomTask = clients[1].joinRoom(room.ID, clients[1].getLocalPlayerData());
        //    yield return roomTask.wait();
        //    //预期收到自己加入了房间，里面有两个人
        //    Assert.AreEqual(room.ID, roomTask.Result.ID);
        //    room = roomTask.Result;
        //    Assert.AreEqual(2, room.playerDataList.Count);
        //    Assert.AreEqual(clients[0].getLocalPlayerData().id, room.playerDataList[0].id);
        //    Assert.AreEqual(clients[1].getLocalPlayerData().id, room.playerDataList[1].id);
        //    //预期其他人看到房间更新了，里面有两个人
        //    yield return TestHelper.waitUntil(() => updateRooms.All(r => r != null), 5);
        //    Debug.Log(Thread.CurrentThread.ManagedThreadId);
        //    for (int i = 0; i < updateRooms.Length && updateRooms[i] != null; i++)
        //    {
        //        var updateRoom = updateRooms[i];
        //        Assert.AreEqual(room.ID, updateRoom.ID);
        //        Assert.AreEqual(2, updateRoom.playerDataList.Count);
        //        updateRooms[i] = null;
        //    }
        //    //第三个人加入房间
        //    roomsTask = clients[2].getRooms();
        //    yield return roomsTask.wait();
        //    room = roomsTask.Result[0];
        //    roomTask = clients[2].joinRoom(room.ID, clients[2].getLocalPlayerData());
        //    yield return roomTask.wait();
        //    //预期自己加入了房间，里面有3个人
        //    Assert.AreEqual(room.ID, roomTask.Result.ID);
        //    room = roomTask.Result;
        //    Assert.AreEqual(3, room.playerDataList.Count);
        //    Assert.AreEqual(clients[0].getLocalPlayerData().id, room.playerDataList[0].id);
        //    Assert.AreEqual(clients[1].getLocalPlayerData().id, room.playerDataList[1].id);
        //    Assert.AreEqual(clients[2].getLocalPlayerData().id, room.playerDataList[2].id);
        //    //预期其他人看见房间更新了，房间里的人可以看到细节，外面的人只能看到数量变化
        //    yield return TestHelper.waitUntil(() => updateRooms.All(r => r != null), 5);
        //    for (int i = 0; i < updateRooms.Length && updateRooms[i] != null; i++)
        //    {
        //        var updateRoom = updateRooms[i];
        //        Assert.AreEqual(room.ID, updateRoom.ID);
        //        Assert.AreEqual(3, updateRoom.playerDataList.Count);
        //        if (i < 3)
        //        {
        //            Assert.AreEqual(clients[0].getLocalPlayerData().id, updateRoom.playerDataList[0].id);
        //            Assert.AreEqual(clients[1].getLocalPlayerData().id, updateRoom.playerDataList[1].id);
        //            Assert.AreEqual(clients[2].getLocalPlayerData().id, updateRoom.playerDataList[2].id);
        //        }
        //    }
        //}
        IEnumerator joinRoomAssert(ClientLogic[] clients)
        {
            //一个人创建房间
            yield return clients[0].createOnlineRoom().wait();
            //第二个人加入房间
            yield return TestHelper.waitUntil(() =>
            {
                return clients[1].roomList.Count > 0;
            }, 5);
            var roomInfo = clients[1].roomList.First().Value;
            Assert.NotNull(roomInfo);

            Task<bool> boolTask = clients[1].joinRoom(roomInfo.RoomID);
            yield return boolTask.wait();
            //预期收到自己加入了房间，里面有两个人
            Assert.True(boolTask.Result);
            Assert.AreEqual(roomInfo.RoomID, clients[1].room.ID);
            Assert.NotNull(clients[1].localPlayer);
            var roomData = clients[1].room;
            Assert.AreEqual(2, roomData.playerDataList.Count);
            Assert.AreEqual(clients[0].getLocalPlayerData().id, roomData.playerDataList[0].id);
            Assert.AreEqual(clients[1].getLocalPlayerData().id, roomData.playerDataList[1].id);
            //预期其他人看到房间更新了，里面有两个人
            yield return TestHelper.waitUntil(() => clients.All(c =>
            {
                var rd = c.roomList.First().Value;
                return roomData.ID == rd.RoomID && rd.PlayerCount == 2;
            }), 5);
            //第三个人加入房间
            yield return TestHelper.waitUntil(() => clients[2].roomList.Count > 0, 5);
            roomInfo = clients[2].roomList.First().Value;
            yield return clients[2].joinRoom(roomData.ID).wait();
            //预期自己加入了房间，里面有3个人
            Assert.AreEqual(roomData.ID, clients[2].room.ID);
            roomData = clients[2].room;
            Assert.AreEqual(3, roomData.playerDataList.Count);
            Assert.AreEqual(clients[0].getLocalPlayerData().id, roomData.playerDataList[0].id);
            Assert.AreEqual(clients[1].getLocalPlayerData().id, roomData.playerDataList[1].id);
            Assert.AreEqual(clients[2].getLocalPlayerData().id, roomData.playerDataList[2].id);
            //预期其他人看见房间更新了，房间里的人可以看到细节，外面的人只能看到数量变化
            yield return TestHelper.waitUntil(() => clients.All(c =>
            {
                var rd = c.roomList.First().Value;
                return roomData.ID == rd.RoomID && rd.PlayerCount == 3;
            }), 5);
        }
        IEnumerator addAIPlayerAssert(ClientLogic[] clients)
        {
            //一个人创建房间
            yield return clients[0].createOnlineRoom().wait();
            //另一个人加入房间
            yield return TestHelper.waitUntil(() => clients[1].roomList.Count > 0, 5);
            yield return clients[1].joinRoom(clients[1].roomList.First().Value.RoomID).wait();
            //向房间中添加AI玩家
            yield return TestHelper.waitUntilAllEventTrig(clients,
                (c, a) => c.LANNetwork.OnRoomPlayerDataChanged += (ps) => a(),
                () => clients[0].addAIPlayer().wait());
            //所有人都会收到
            Assert.AreEqual(3, clients[1].room.playerDataList.Count);
            foreach (var client in clients)
            {
                var room = client.roomList.First().Value;
                Assert.AreEqual(3, room.PlayerCount);
                Assert.AreEqual(RoomPlayerType.ai, client.room.playerDataList[2].type);
            }
        }
        IEnumerator setRoomPropAssert(ClientLogic[] clients)
        {
            yield return createAndJoinRoom(clients);
            //更改房间属性
            int randomSeed = DateTime.Now.GetHashCode();
            yield return TestHelper.waitUntilAllEventTrig(clients,
                (c, a) => c.LANNetwork.OnRoomDataChange += (rd) => a(),
                () => clients[0].setRoomProp("randomSeed", randomSeed).wait());
            //所有人都会收到房间属性更改
            foreach (var client in clients)
            {
                Assert.AreEqual(randomSeed, client.room.getProp<int>("randomSeed"));
            }
        }
        /// <summary>
        /// 一个人创建房间，另一个人加入房间
        /// </summary>
        /// <param name="clients"></param>
        /// <returns></returns>
        IEnumerator createAndJoinRoom(ClientLogic[] clients)
        {
            yield return TestHelper.waitUntilEventTrig(clients[1],
                (c, a) => c.onRoomListChange += r => { if (r.Count > 0) a(); },
                () => clients[0].createOnlineRoom().wait());
            yield return clients[1].joinRoom(clients[1].roomList.First().Value.RoomID).wait();
        }
        IEnumerator setPlayerPropAssert(ClientLogic[] clients)
        {
            yield return createAndJoinRoom(clients);
            //玩家2设置自己的属性，预期房间里的所有人都能收到属性改变
            yield return TestHelper.waitUntilAllEventTrig(clients.Take(2),
                (c, a) => c.LANNetwork.OnRoomDataChange += (ps) => a(),
                () => clients[1].setPlayerProp("deckCount", 30).wait(), 10);
            for (int i = 0; i < clients.Length; i++)
            {
                if (i < 2)
                    Assert.AreEqual(30, clients[i].room.playerDataList[1].propDict["deckCount"]);
                else
                {
                    var roomData = clients[i].room;
                    Assert.False(roomData.playerDataList[1].propDict.ContainsKey("deckCount"));
                }
            }
        }
        IEnumerator quitRoomAssert(ClientLogic[] clients)
        {
            yield return createAndJoinRoom(clients);
            foreach (var client in clients)
            {
                Assert.AreEqual(2, client.room.playerDataList.Count);
            }
            //玩家2退出房间，所有人都可以看到房间中人数的减少
            yield return TestHelper.waitUntilAllEventTrig(clients,
                (c, a) => c.LANNetwork.OnRoomPlayerDataChanged += (ps) => a(),
                () => clients[1].quitRoom().wait());
            Assert.Null(clients[1].room);
            foreach (var client in clients)
            {
                Assert.AreEqual(1, client.room.playerDataList.Count);
            }
            //玩家1退出房间，所有人都可以看到房间中人无了
            yield return TestHelper.waitUntilAllEventTrig(clients.Skip(1),
                (c, a) => c.LANNetwork.OnRoomPlayerDataChanged += s => a(),
                () => clients[0].quitRoom().wait());
            Assert.Null(clients[0].room);
            foreach (var client in clients)
            {
                Assert.AreEqual(0, client.roomList.Count);
            }
        }
        //IEnumerator startNetworkAndAssert(int count, Func<string, IClientNetworking> netStarter, Func<IClientNetworking[], IEnumerator> onAssert)
        //{
        //    IClientNetworking[] clients = new IClientNetworking[count];
        //    Updater[] updaters = new Updater[count];
        //    for (int i = 0; i < count; i++)
        //    {
        //        string name = i == 0 ? "Local" : "Remote" + i;
        //        IClientNetworking client = netStarter(name);
        //        if (!client.isRunning)
        //            client.start();
        //        updaters[i] = new GameObject(name).AddComponent<Updater>();
        //        updaters[i].action = () => client.update();
        //        clients[i] = client;
        //    }
        //    for (int i = 0; i < clients.Length; i++)
        //    {
        //        clients[i].broadcastPorts = getPorts(clients);
        //    }
        //    yield return onAssert(clients);
        //    for (int i = 0; i < count; i++)
        //    {
        //        clients[i].Dispose();
        //        Object.Destroy(updaters[i].gameObject);
        //    }
        //}
        IEnumerator startNetworkAndAssert(int count, Func<string, ClientLogic> netStarter, Func<ClientLogic[], IEnumerator> onAssert)
        {
            ClientLogic[] clients = new ClientLogic[count];
            Updater[] updaters = new Updater[count];
            for (int i = 0; i < count; i++)
            {
                string name = i == 0 ? "Local" : "Remote" + i;
                ClientLogic client = netStarter(name);
                updaters[i] = new GameObject(name).AddComponent<Updater>();
                updaters[i].action = () => client.update();
                clients[i] = client;
            }
            for (int i = 0; i < clients.Length; i++)
            {
                clients[i].LANNetwork.broadcastPorts = getPorts(clients);
            }
            yield return onAssert(clients);
            for (int i = 0; i < count; i++)
            {
                clients[i].Dispose();
                Object.Destroy(updaters[i].gameObject);
            }
        }
        int[] getPorts(CommonClientNetwokingV3[] clients)
        {
            return clients.Select(c => c.Port).ToArray();
        }
        int[] getPorts(ClientLogic[] clients)
        {
            return clients.Select(c => c.curNetwork.Port).ToArray();
        }
        #endregion
        #region 具体网络实现
        LANNetworking startLANNetworking(string name)
        {
            //客户端逻辑是客户端网络实现不可分割的一部分，没办法了。
            ClientLogic client = new ClientLogic(name, logger: new UnityLogger(name));
            client.switchNetToLAN();
            return client.LANNetwork;
        }
        ClientLogic startLANClient(string name)
        {
            //客户端逻辑是客户端网络实现不可分割的一部分，没办法了。
            ClientLogic client = new ClientLogic(name, logger: new UnityLogger(name));
            client.switchNetToLAN();
            return client;
        }
        ClientNetworking startClientNetworking(string name)
        {
            throw new NotImplementedException();
        }
        #endregion
        #region 网络基础功能
        /// <summary>
        /// RPC广播
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator invokeBroadcastTest()
        {
            LANNetworking local = new LANNetworking("Local");
            GameObject localUpdater = new GameObject("LocalUpdater");
            localUpdater.AddComponent<Updater>().action = () => local.net.PollEvents();
            local.start();
            LANNetworking remote = new LANNetworking("Remote");
            GameObject remoteUpdater = new GameObject("RemoteUpdater");
            remoteUpdater.AddComponent<Updater>().action = () => remote.net.PollEvents();
            remote.start();
            local.broadcastPorts = new int[] { remote.Port };
            flag = false;
            remote.addRPCMethod(this, nameof(setFlag));
            local.invokeBroadcast(nameof(setFlag), true);
            yield return TestHelper.waitUntil(() => flag, 5);
            Assert.True(flag);
        }
        IEnumerator startNetworkingAndAssert(int count, Func<LANNetworking[], TestRPCTarget[], IEnumerator> onAssert)
        {
            LANNetworking[] networkings = new LANNetworking[count];
            Updater[] updaters = new Updater[count];
            TestRPCTarget[] targets = new TestRPCTarget[count];
            for (int i = 0; i < networkings.Length; i++)
            {
                LANNetworking networking = new LANNetworking("Remote" + i);
                networkings[i] = networking;
                updaters[i] = new GameObject("Remote" + i + "Updater").AddComponent<Updater>();
                updaters[i].action = () => networking.net.PollEvents();
                networking.start();
                targets[i] = new TestRPCTarget();
                networking.addRPCMethod(targets[i], nameof(TestRPCTarget.setFlag));
            }
            for (int i = 0; i < networkings.Length; i++)
            {
                networkings[i].broadcastPorts = getPorts(networkings);
            }
            yield return onAssert(networkings, targets);
        }
        class TestRPCTarget
        {
            public bool flag = false;
            public bool setFlag(bool value)
            {
                flag = value;
                return flag;
            }
        }
        void setFlag(bool value)
        {
            flag = value;
        }
        bool flag = false;
        #endregion
    }
}
