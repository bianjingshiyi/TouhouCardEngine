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
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

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
        public void serializeObjectArrayTest()
        {
            object[] array = new object[] { 1 };
            StringWriter writer = new StringWriter();
            JsonSerializer serializer = new JsonSerializer();
            serializer.Serialize(writer, array);
            Debug.Log(writer.ToString());
        }
        [Test]
        public void cardDefineSerializeTest()
        {
            object cards =
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
            });
            //new CardDefine[]
            //{
            //    new CardDefine(0, "Spell", new Dictionary<string, object>()
            //    {
            //        { "cost", 5 },
            //        { "tags", new string[] { "Fire" } }
            //    }, new GeneratedEffect[]
            //    {
            //        new GeneratedEffect(null, new TargetChecker[]
            //        {
            //            new TargetChecker("Character", null, "必须以角色为目标")
            //        }, new ActionNode("Damage", new ActionValueRef[]
            //        {
            //            new ActionValueRef(new ActionNode("GetTarget", new object[] { "Target" })),
            //            new ActionValueRef(new ActionNode("GetVariable", new object[] { "Card" })),
            //            new ActionValueRef(new ActionNode("GetSpellDamage", new ActionValueRef[]
            //            {
            //                new ActionValueRef(new ActionNode("GetOwner", new ActionValueRef[]
            //                {
            //                    new ActionValueRef(new ActionNode("GetVariable", new object[] { "Card" }))
            //                })),
            //                new ActionValueRef(new ActionNode("IntegerConst", new object[] { 7 }))
            //            }))
            //        }), new string[0])
            //    })
            //};
            StringWriter writer = new StringWriter();
            JsonSerializer serializer = new JsonSerializer();
            serializer.Error += Serializer_Error;
            serializer.Serialize(writer, cards);
            object dObj = serializer.Deserialize(new StringReader(writer.ToString()), cards.GetType());
            StringWriter dWriter = new StringWriter();
            serializer.Serialize(dWriter, dObj);
            Assert.AreEqual(writer.ToString(), dWriter.ToString());
            Debug.Log(dWriter.ToString());
        }

        private void Serializer_Error(object sender, Newtonsoft.Json.Serialization.ErrorEventArgs e)
        {
            Debug.LogError(e.CurrentObject + "序列化发生异常：" + e.ErrorContext.Error);
        }
    }
}
