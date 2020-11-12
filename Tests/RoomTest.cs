using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NitoriNetwork.Common;
using NUnit.Framework;
using System;
using System.Collections;
using TouhouCardEngine;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class RoomTest
    {
        [Test]
        public void localRoomCreateTest()
        {
            createLocalRoomAndAssert(createRoomAssert);
        }
        void createLocalRoomAndAssert(Func<ClientLogic, IEnumerator> onAssert)
        {
            using (ClientLogic client = new ClientLogic(new UnityLogger("Room")))
            {
                client.createLocalRoom();
                onAssert(client);
            }
        }
        IEnumerator createRoomAssert(ClientLogic client)
        {
            Room room = client.room;
            Assert.AreEqual(1, room.getPlayers().Length);
            Assert.AreNotEqual(0, room.getPlayers()[0].id);
            Assert.AreNotEqual(RoomPlayerType.human, room.data.getPlayerData(room.getPlayers()[0].id).type);
            yield break;
        }
        [Test]
        public void localRoomAddAIPlayerTest()
        {
            createLocalRoomAndAssert(addAIPlayerAssert);
        }
        IEnumerator addAIPlayerAssert(ClientLogic client)
        {
            yield return client.roomAddAIPlayer().wait();
            Assert.AreEqual(2, client.room.getPlayers().Length);
            Assert.AreNotEqual(0, client.room.getPlayers()[0].id);
            Assert.AreEqual(RoomPlayerType.human, client.room.data.getPlayerData(client.room.getPlayers()[0].id).type);
            Assert.AreNotEqual(0, client.room.getPlayers()[1].id);
            Assert.AreEqual(RoomPlayerType.ai, client.room.data.getPlayerData(client.room.getPlayers()[1].id).type);
            yield break;
        }
        [Test]
        public void localRoomSetPropTest()
        {
            createLocalRoomAndAssert(setPropAssert);
        }

        IEnumerator setPropAssert(ClientLogic client)
        {
            Room room = client.room;
            room.setProp("key", "value");
            Assert.AreEqual("value", room.getProp<string>("key"));
            yield break;
        }

        [Test]
        public void localRoomSetPlayerPropTest()
        {
            createLocalRoomAndAssert(setPlayerPropAssert);
        }

        IEnumerator setPlayerPropAssert(ClientLogic client)
        {
            Room room = client.room;
            room.setPlayerProp(room.data.ownerId, "key", "value");
            Assert.AreEqual("value", room.getPlayerProp<string>(room.data.ownerId, "key"));
            yield break;
        }

        [Test]
        public void localRoomRemovePlayerTest()
        {
            createLocalRoomAndAssert(removePlayerAssert);
        }

        IEnumerator removePlayerAssert(ClientLogic client)
        {
            Room room = client.room;
            var player = new AIRoomPlayer();
            room.addPlayer(player, new RoomPlayerData("AI", RoomPlayerType.ai));
            room.removePlayer(player.id);
            Assert.Null(room.data.getPlayerData(player.id));
            yield break;
        }

        [Test]
        public void serializeTest()
        {
            RoomData data = new RoomData { ownerId = 1 };
            data.propDict.Add("randomSeed", 42);
            data.playerDataList.Add(new RoomPlayerData(1, "玩家", RoomPlayerType.human));
            data.playerDataList[0].propDict.Add("name", "you know who");

            string typeName = data.GetType().FullName;
            BsonDocument bsonDoc = data.ToBsonDocument();

            data = BsonSerializer.Deserialize(bsonDoc, TypeHelper.getType(typeName)) as RoomData;
            Assert.AreEqual(1, data.ownerId);
            Assert.AreEqual(42, data.propDict["randomSeed"]);
            Assert.AreEqual(1, data.playerDataList[0].id);
            Assert.AreEqual("玩家", data.playerDataList[0].name);
            Assert.AreEqual(RoomPlayerType.human, data.playerDataList[0].type);
            Assert.AreEqual("you know who", data.playerDataList[0].propDict["name"]);
        }
        [UnityTest]
        public IEnumerator LANRoomCreateTest()
        {
            yield return LANRoomCreateAndAssert(LANRoomCreateAssert);
        }
        IEnumerator LANRoomCreateAndAssert(Func<ClientLogic, IEnumerator> onAssert)
        {
            using (ClientLogic client = new ClientLogic(new UnityLogger("RoomLocal")))
            {
                client.switchNetToLAN();
                yield return client.createOnlineRoom().wait();
                yield return onAssert(client);
            }
        }
        IEnumerator LANRoomCreateAssert(ClientLogic client)
        {
            createRoomAssert(client);
            yield break;
        }
        [UnityTest]
        public IEnumerator LANRoomAddAIPlayerTest()
        {
            yield return LANRoomCreateAndAssert(addAIPlayerAssert);
        }
        [UnityTest]
        public IEnumerator LANRoomJoinTest()
        {
            yield return LANRoomCreate2AndAssert(LANRoomJoinAssert);
        }
        IEnumerator LANRoomCreate2AndAssert(Func<ClientLogic, ClientLogic, IEnumerator> onAssert)
        {
            using (ClientLogic client1 = new ClientLogic(new UnityLogger("RoomLocal")))
            {
                client1.switchNetToLAN();
                yield return client1.createOnlineRoom().wait();
                using (ClientLogic client2 = new ClientLogic(new UnityLogger("RoomRemote")))
                {
                    client2.switchNetToLAN();
                    var roomsTask = client2.getRooms();
                    yield return roomsTask.wait();
                    RoomData roomData = roomsTask.Result[0];
                    yield return client2.joinOnlineRoom(roomData).wait();
                    yield return onAssert(client1, client2);
                }
            }
        }
        IEnumerator LANRoomJoinAssert(ClientLogic client1, ClientLogic client2)
        {
            Assert.AreEqual(client1.room.getPlayers()[0].id, client1.room.data.ownerId);
            Assert.AreEqual(2, client1.room.getPlayers().Length);
            Assert.AreEqual(RoomPlayerType.human, client1.room.data.playerDataList[0].type);
            Assert.AreEqual(RoomPlayerType.human, client1.room.data.playerDataList[1].type);
            Assert.AreEqual(client2.room.getPlayers()[0].id, client2.room.data.ownerId);
            Assert.AreEqual(2, client2.room.getPlayers().Length);
            Assert.AreEqual(RoomPlayerType.human, client2.room.data.playerDataList[0].type);
            Assert.AreEqual(RoomPlayerType.human, client2.room.data.playerDataList[1].type);
            Assert.AreEqual(client1.room.data.ownerId, client2.room.data.ownerId);
            Assert.AreEqual(client1.room.getPlayers()[0].id, client2.room.getPlayers()[0].id);
            Assert.AreEqual(client1.room.getPlayers()[1].id, client2.room.getPlayers()[1].id);
            yield break;
        }
        //[Test]
        //public void rpcLocalTest()
        //{
        //    ServerNetworking server = new ServerNetworking(null);
        //    MethodInfo method = GetType().GetMethod(nameof(testRPCMethod));
        //    Assert.NotNull(method);
        //    server.addRPCMethod(method);
        //    var rpcMethod = server.getRPCMethod(nameof(testRPCMethod), 1, null, new RPCArg(), new RPCArg());
        //    Assert.NotNull(rpcMethod);
        //    Assert.AreEqual(method, rpcMethod);
        //}
        //public void testRPCMethod(int primArg, object objArg, RPCArgBase varArg, IRPCArg iArg)
        //{
        //    Debug.Log("参数：" + primArg + objArg + varArg + iArg);
        //}
        //public class RPCArgBase { }
        //public class RPCArg : RPCArgBase, IRPCArg { }
        //public interface IRPCArg { }
        ///// <summary>
        ///// 创建房间测试。
        ///// </summary>
        ///// <returns></returns>
        //[UnityTest]
        //public IEnumerator clientRoomCreateTest()
        //{
        //    yield return createClientRoomAndAssert(onAssert);
        //    IEnumerator onAssert(Room room)
        //    {
        //        createRoomAssert(room);
        //        yield break;
        //    }
        //}
        //IEnumerator createClientRoomAndAssert(Func<Room, IEnumerator> onAssert)
        //{
        //    UnityLogger logger = new UnityLogger("Room");
        //    using (ClientLogic client = new ClientLogic())
        //    {
        //        client.networking.start();
        //        new GameObject(nameof(client)).AddComponent<Updater>().action = () => client.networking.update();
        //        using (ServerLogic server = new ServerLogic(logger))
        //        {
        //            server.networking.start();
        //            new GameObject(nameof(server)).AddComponent<Updater>().action = () => server.networking.update();
        //            //连接到服务器
        //            yield return client.networking.connect(server.networking.ip, server.networking.port).wait();
        //            //创建房间
        //            yield return client.createOnlineRoom(new RoomPlayerData("玩家1", RoomPlayerType.human)).wait();
        //            yield return onAssert?.Invoke(client.room);
        //        }
        //    }
        //}
        //[UnityTest]
        //public IEnumerator clientRoomJoinTest()
        //{
        //    yield return createClient2RoomAndAssert(onAssert);
        //    IEnumerator onAssert(Room room1, Room room2)
        //    {
        //        Assert.AreEqual(2, room1.getPlayers().Length);
        //        Assert.AreEqual(2, room2.getPlayers().Length);
        //        yield break;
        //    }
        //}
        //IEnumerator createClient2RoomAndAssert(Func<Room, Room, IEnumerator> onAssert)
        //{
        //    UnityLogger logger = new UnityLogger("Room");
        //    using (HostNetworking server = new ServerNetworking(null, logger))
        //    {
        //        server.start();
        //        new GameObject(nameof(server)).AddComponent<Updater>().action = () => server.update();
        //        using (ClientNetworking client1 = new ClientNetworking(logger))
        //        {
        //            client1.start();
        //            new GameObject(nameof(client1)).AddComponent<Updater>().action = () => client1.update();
        //            using (ClientNetworking client2 = new ClientNetworking(logger))
        //            {
        //                client2.start();
        //                new GameObject(nameof(client2)).AddComponent<Updater>().action = () => client2.update();
        //                //连接到服务器
        //                yield return client1.connect(server.ip, server.port).wait();
        //                yield return client2.connect(server.ip, server.port).wait();
        //                //创建房间
        //                var task = client1.reqCreateRoom(new RoomPlayerData(0, "玩家1", RoomPlayerType.human));
        //                yield return task.wait();
        //                RoomData roomData = task.Result;
        //                OnlineRoom room1 = new OnlineRoom(client1, roomData);
        //                //加入房间
        //                task = client2.reqJoinRoom(roomData.id, new RoomPlayerData(0, "玩家2", RoomPlayerType.human));
        //                yield return task.wait();
        //                roomData = task.Result;
        //                OnlineRoom room2 = new OnlineRoom(client2, roomData);
        //                yield return onAssert?.Invoke(room1, room2);
        //            }
        //        }
        //    }
        //}
        //[UnityTest]
        //public IEnumerator clientRoomAddAIPlayerTest()
        //{
        //    yield return createClientRoomAndAssert(onAssert);
        //    IEnumerator onAssert(Room room)
        //    {
        //        yield return room.addAIPlayer().wait();
        //        addAIPlayerAssert(room);
        //    }
        //}
        class Updater : MonoBehaviour, IDisposable
        {
            public Action action;
            protected void Update()
            {
                action?.Invoke();
            }
            public void Dispose()
            {
                DestroyImmediate(gameObject);
            }
        }
    }
}
