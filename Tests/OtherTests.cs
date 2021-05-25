using LiteNetLib;
using LiteNetLib.Utils;
using MongoDB.Bson;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using TouhouCardEngine;
using UnityEngine;
using UnityEngine.TestTools;

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
        public void cardDefineSerializeTest()
        {
            CardDefine[] cards = new CardDefine[]
            {
                new CardDefine(0, "Spell", new Dictionary<string, object>()
                {
                    { "cost", 5 },
                    { "tags", new string[] { "Fire" } }
                }, new GeneratedEffect[]
                {
                    new GeneratedEffect(null, new TargetChecker[]
                    {
                        new TargetChecker("Character", null, "必须以角色为目标")
                    }, new ActionNode("Damage", new ActionValueRef[]
                    {
                        new ActionValueRef(new ActionNode("GetTarget", new object[] { "Target" })),
                        new ActionValueRef(new ActionNode("GetVariable", new object[] { "Card" })),
                        new ActionValueRef(new ActionNode("GetSpellDamage", new ActionValueRef[]
                        {
                            new ActionValueRef(new ActionNode("GetOwner", new ActionValueRef[]
                            {
                                new ActionValueRef(new ActionNode("GetVariable", new object[] { "Card" }))
                            })),
                            new ActionValueRef(new ActionNode("IntegerConst", new object[] { 7 }))
                        }))
                    }), new string[0])
                })
            };
            string json = cards.ToJson();
            Debug.Log(json);
        }
    }
}
