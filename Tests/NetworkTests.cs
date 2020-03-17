using System.Net;
using System.Net.Sockets;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using TouhouCardEngine;

namespace Tests
{
    public class NetworkTests
    {
        [UnityTest]
        public IEnumerator connectTest()
        {
            ServerManager server = new GameObject("ServerManager").AddComponent<ServerManager>();
            server.start(9050);
            ClientManager client = new GameObject("ClientManager").AddComponent<ClientManager>();
            client.start();
            Assert.AreEqual(-1, client.id);
            client.connect(Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString(), 9050);
            yield return new WaitForSeconds(1);
            Assert.AreEqual(0, client.id);
        }
    }
}
