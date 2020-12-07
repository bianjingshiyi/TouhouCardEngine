using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NitoriNetwork.Common;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using TouhouCardEngine;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
namespace Tests
{
    public class ClientLogicTests
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
            yield return client.addAIPlayer().wait();
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
            yield return LANRoomCreate2AndAssert(LANRoomCreateAssert);
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
        IEnumerator LANRoomCreateAssert(ClientLogic client1, ClientLogic client2)
        {
            RoomData room = null;
            client2.onNewRoom += r => room = r;
            //客户端创建房间，并且广播新增房间信息
            yield return client1.createOnlineRoom(client2.port).wait();
            //client2应该会收到创建房间信息
            yield return TestHelper.waitUntil(() => room != null, 5);
            Assert.NotNull(room);
        }
        [UnityTest]
        public IEnumerator LANRoomAddAIPlayerTest()
        {
            yield return LANRoomCreateAndAssert(addAIPlayerAssert);
        }
        [UnityTest]
        public IEnumerator LANRoomGetRoomsTest()
        {
            yield return LANRoomCreate2AndAssert(LANRoomGetRoomsAssert);
        }
        IEnumerator LANRoomGetRoomsAssert(ClientLogic client1, ClientLogic client2)
        {
            RoomData room = null;
            client2.onUpdateRoom += r => room = r;
            //client1先创建房间，但是其实这个时候client2就应该收到消息，得到房间了
            yield return client1.createOnlineRoom(client2.port);
            //client2广播发现房间消息，会得到client1的回应，不过房间里面应该已经有了
            client2.refreshRooms(client1.port);
            yield return TestHelper.waitUntil(() => room != null, 5);
            Assert.NotNull(room);
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
                new GameObject("Client1Updater").AddComponent<Updater>().action = () => client1.update();
                client1.switchNetToLAN();
                yield return client1.createOnlineRoom().wait();
                using (ClientLogic client2 = new ClientLogic(new UnityLogger("RoomRemote")))
                {
                    new GameObject("Client2Updater").AddComponent<Updater>().action = () => client2.update();
                    client2.switchNetToLAN();
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
    }
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
