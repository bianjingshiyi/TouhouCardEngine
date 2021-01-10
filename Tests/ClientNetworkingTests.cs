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

namespace Tests
{
    public class ClientNetworkingTests
    {
       

        #region 局域网测试
        [UnityTest]
        public IEnumerator LANGetLocalPlayerTest()
        {
            yield return startNetworkAndAssert(10, startLANNetworking, getLocalPlayerAssert);
        }
        [UnityTest]
        public IEnumerator LANCreateRoomTest()
        {
            yield return startNetworkAndAssert(10, startLANNetworking, createRoomAssert);
        }
        [UnityTest]
        public IEnumerator LANRefreshRoomsTest()
        {
            yield return startNetworkAndAssert(10, startLANNetworking, refreshRoomsAssert);
        }
        [UnityTest]
        public IEnumerator LANGetRoomsTest()
        {
            yield return startNetworkAndAssert(10, startLANNetworking, getRoomsAssert);
        }
        [UnityTest]
        public IEnumerator LANJoinRoomTest()
        {
            yield return startNetworkAndAssert(10, startLANNetworking, joinRoomAssert);
        }
        #endregion
        #region 通用测试
        IEnumerator getLocalPlayerAssert(ClientNetworking[] clients)
        {
            var idSet = clients.Select(c => c.getLocalPlayerData().id);
            //确保每一个客户端网络都有独一无二的PlayerId
            Assert.AreEqual(idSet.Count(), idSet.Distinct().Count());
            yield break;
        }
        IEnumerator createRoomAssert(ClientNetworking[] clients)
        {
            RoomData[] rooms = new RoomData[clients.Length];
            for (int i = 0; i < clients.Length; i++)
            {
                int I = i;
                clients[i].onNewRoom += r => rooms[I] = r;
            }
            //一个人创建房间，发送广播通知给所有人
            yield return clients[0].createRoom(clients[0].getLocalPlayerData(), getPorts(clients)).wait();
            //所有人都会收到这个房间的新建通知
            yield return TestHelper.waitUntil(() => rooms.All(r => r != null), 5);
            Assert.True(rooms.All(r => r != null));
        }
        IEnumerator refreshRoomsAssert(ClientNetworking[] clients)
        {
            //第一个人创建房间，但是谁都不告诉
            RoomData[] rooms = new RoomData[clients.Length];
            Task<RoomData> rdt = clients[0].createRoom(clients[0].getLocalPlayerData(), new int[0]);
            yield return rdt.wait();
            clients[0].onGetRoomReq +=()=> rdt.Result;
            //所有人刷新房间，应该可以刷出来这个房间
            for (int i = 0; i < clients.Length; i++)
            {
                int I = i;
                clients[i].onUpdateRoom += r => rooms[I] = r;
                clients[i].refreshRooms(getPorts(clients));
            }
            yield return TestHelper.waitUntil(() => rooms.All(r => r != null), 5);
            Assert.True(rooms.All(r => r != null));
        }
        IEnumerator getRoomsAssert(ClientNetworking[] clients)
        {
            //一个人创建房间，发送广播通知给所有人
            yield return clients[0].createRoom(clients[0].getLocalPlayerData(), getPorts(clients)).wait();
            //第二个人创建房间，发送广播通知给所有人
            yield return clients[1].createRoom(clients[1].getLocalPlayerData(), getPorts(clients)).wait();
            //每一个人获取房间列表都能获取到两个创建的房间
            foreach (var client in clients)
            {
                Task<RoomData[]> roomsTask = client.getRooms();
                yield return roomsTask.wait(5);
                Assert.AreEqual(2, roomsTask.Result.Length);
            }
        }
        IEnumerator joinRoomAssert(ClientNetworking[] clients)
        {
            //所有人都会收到房间状态变化的消息
            RoomData[] updateRooms = new RoomData[clients.Length];
            for (int i = 0; i < clients.Length; i++)
            {
                int I = i;
                clients[i].onUpdateRoom += r => updateRooms[I] = r;
            }
            //一个人创建房间
            yield return clients[0].createRoom(clients[0].getLocalPlayerData(), getPorts(clients)).wait();
            //第二个人加入房间
            Task<RoomData[]> roomsTask = clients[1].getRooms();
            yield return roomsTask.wait();
            RoomData room = roomsTask.Result[0];
            regLANOnJoinRoomReq((clients[0] as LANNetworking), room);
            Assert.NotNull(room);
            Task<RoomData> roomTask = clients[1].joinRoom(room, clients[1].getLocalPlayerData());
            yield return roomTask.wait();
            //预期收到自己加入了房间，里面有两个人
            Assert.AreEqual(room.ID, roomTask.Result.ID);
            room = roomTask.Result;
            Assert.AreEqual(2, room.playerDataList.Count);
            Assert.AreEqual(clients[0].getLocalPlayerData().id, room.playerDataList[0].id);
            Assert.AreEqual(clients[1].getLocalPlayerData().id, room.playerDataList[1].id);
            //预期其他人看到房间更新了，里面有两个人
            yield return TestHelper.waitUntil(() => updateRooms.All(r => r != null), 5);
            Debug.Log(Thread.CurrentThread.ManagedThreadId);
            for (int i = 0; i < updateRooms.Length && updateRooms[i]!=null; i++)
            {
                var updateRoom = updateRooms[i];
                Assert.AreEqual(room.ID, updateRoom.ID);
                Assert.AreEqual(2, updateRoom.playerDataList.Count);
                updateRooms[i] = null;
            }
            //第三个人加入房间
            roomsTask = clients[2].getRooms();
            yield return roomsTask.wait();
            room = roomsTask.Result[0];
            roomTask = clients[2].joinRoom(room, clients[2].getLocalPlayerData());
            yield return roomTask.wait();
            //预期自己加入了房间，里面有3个人
            Assert.AreEqual(room.ID, roomTask.Result.ID);
            room = roomTask.Result;
            Assert.AreEqual(3, room.playerDataList.Count);
            Assert.AreEqual(clients[0].getLocalPlayerData().id, room.playerDataList[0].id);
            Assert.AreEqual(clients[1].getLocalPlayerData().id, room.playerDataList[1].id);
            Assert.AreEqual(clients[2].getLocalPlayerData().id, room.playerDataList[2].id);
            //预期其他人看见房间更新了，房间里的人可以看到细节，外面的人只能看到数量变化
            yield return TestHelper.waitUntil(() => updateRooms.All(r => r != null), 5);
            for (int i = 0; i < updateRooms.Length&&updateRooms[i]!=null; i++)
            {
                var updateRoom = updateRooms[i];
                Assert.AreEqual(room.ID, updateRoom.ID);
                Assert.AreEqual(3, updateRoom.playerDataList.Count);
                if (i < 3)
                {
                    Assert.AreEqual(clients[0].getLocalPlayerData().id, updateRoom.playerDataList[0].id);
                    Assert.AreEqual(clients[1].getLocalPlayerData().id, updateRoom.playerDataList[1].id);
                    Assert.AreEqual(clients[2].getLocalPlayerData().id, updateRoom.playerDataList[2].id);
                }
            }
        }
        IEnumerator addAIPlayerAssert(ClientNetworking[] clients)
        {
            //一个人创建房间
            yield return clients[0].createRoom(clients[0].getLocalPlayerData(), getPorts(clients)).wait();
        }
        IEnumerator startNetworkAndAssert(int count, Func<string, ClientNetworking> netStarter, Func<ClientNetworking[], IEnumerator> onAssert)
        {
            ClientNetworking[] clients = new ClientNetworking[count];
            Updater[] updaters = new Updater[count];
            for (int i = 0; i < count; i++)
            {
                string name = i == 0 ? "Local" : "Remote" + i;
                ClientNetworking client = netStarter(name);
                client.start();
                updaters[i] = new GameObject(name).AddComponent<Updater>();
                updaters[i].action = () => client.update();
                clients[i] = client;
            }
            yield return onAssert(clients);
            for (int i = 0; i < count; i++)
            {
                clients[i].Dispose();
                Object.Destroy(updaters[i].gameObject);
            }
        }
        int[] getPorts(ClientNetworking[] clients)
        {
            return clients.Select(c => c.port).ToArray();
        }
        #endregion
        #region 具体网络实现
        LANNetworking startLANNetworking(string name)
        {
            return new LANNetworking(new UnityLogger(name));
        }
        void regLANOnJoinRoomReq(LANNetworking lanNet, RoomData room) {
            lanNet.onJoinRoomReq += playerData => {
                room.playerDataList.Add(playerData);
                return room;
            };
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
            LANNetworking local = new LANNetworking(new UnityLogger("Local"));
            GameObject localUpdater = new GameObject("LocalUpdater");
            localUpdater.AddComponent<Updater>().action = () => local.net.PollEvents();
            local.start();
            LANNetworking remote = new LANNetworking(new UnityLogger("Remote"));
            GameObject remoteUpdater = new GameObject("RemoteUpdater");
            remoteUpdater.AddComponent<Updater>().action = () => remote.net.PollEvents();
            remote.start();
            flag = false;
            remote.addRPCMethod(this, GetType().GetMethod(nameof(setFlag)));
            local.invokeBroadcast(nameof(setFlag), new int[] { remote.port }, true);
            yield return TestHelper.waitUntil(() => flag, 5);
            Assert.True(flag);
        }
        public void setFlag(bool value)
        {
            flag = value;
        }
        bool flag = false;
        #endregion
    }
}
