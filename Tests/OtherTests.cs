using LiteNetLib;
using LiteNetLib.Utils;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using TouhouCardEngine;
using UnityEngine;
using UnityEngine.TestTools;
using MongoDB.Bson.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using System.Threading.Tasks;
using MessagePack;
using System.Text;

namespace Tests
{
    public class OtherTests
    {
        [UnityTest]
        public IEnumerator liteNetBroadcastTest()
        {
            EventBasedNetListener listener1 = new EventBasedNetListener();
            listener1.NetworkReceiveUnconnectedEvent += onNetworkReceiveUnconnectedEvent;
            NetManager net1 = new NetManager(listener1)
            {
                UnconnectedMessagesEnabled = true,
                BroadcastReceiveEnabled = true
            };
            Assert.True(net1.Start());
            EventBasedNetListener listener2 = new EventBasedNetListener();
            NetManager net2 = new NetManager(listener2)
            {
                UnconnectedMessagesEnabled = true,
                BroadcastReceiveEnabled = true
            };
            Assert.True(net2.Start());
            NetDataWriter writer = new NetDataWriter();
            writer.Put("Success");
            net2.SendBroadcast(writer, net1.LocalPort);
            bool result = false;
            yield return new WaitForSeconds(5);
            net1.PollEvents();
            void onNetworkReceiveUnconnectedEvent(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
            {
                if (reader.GetString() == "Success")
                    result = true;
            }
            Assert.True(result);
        }
        [Test]
        public void getTaskResultTest()
        {
            object obj = Task.CompletedTask;
            if (obj is Task task)
            {
                Debug.Log(task.GetType().Name);
            }
        }

        [Test]
        public void msgpackSerdeTest()
        {
            var room = new RoomData("1111-2222-3333-4444")
            {
                ownerId = 1,
                maxPlayerCount = 2,
            };
            var player = new RoomPlayerData("Player", RoomPlayerType.human)
            {
                id = 1,
            };
            player.propDict["defref"] = new ObjectProxy(new DefineReference[] { 
                new DefineReference(1000, 2000),
            });
            room.playerDataList.Add(player);

            room.setProp("RandomSeed", 0);
            room.setProp("SortedPlayers", new int[] { 1 });
            room.setProp("Shuffle", false);
            room.setProp("RoomName", "TestRoom");
            room.setProp("CardPools", new ObjectProxy(new CardPoolInfoPack[] { 
                new CardPoolInfoPack(0x7FF0AAAA55550101, "CardPool1", 233)
            }));

            var binaryBytes = MessagePackSerializer.Serialize(room);
            var jsonString = room.ToJson();
            var jsonBinaryCnt = Encoding.UTF8.GetByteCount(jsonString);

            Debug.Log($"msgpack: {MessagePackSerializer.ConvertToJson(binaryBytes)}");
            Debug.Log($"json: {jsonString}");
            Debug.Log($"msgpack {binaryBytes.Length} bytes, json {jsonBinaryCnt} bytes");

            var deserialized = MessagePackSerializer.Deserialize<RoomData>(binaryBytes);
            Debug.Log(deserialized.ToJson());
        }
    }
}
