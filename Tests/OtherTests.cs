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
    }
}
