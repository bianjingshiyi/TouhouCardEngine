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
            };
            client.start();
            Task task = client.join(Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString(), host.port);
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
            Assert.IsInstanceOf<TimeoutException>(task.Exception.InnerException);
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
            task = client.join(Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString(), 9050);

            Assert.IsInstanceOf<InvalidOperationException>(task.Exception.InnerException);
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
            yield return new WaitForSeconds(.5f);

            Assert.True(task.IsCompleted);
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
    }
}
