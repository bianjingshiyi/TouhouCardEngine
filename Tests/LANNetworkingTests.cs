using NUnit.Framework;
using System.Collections;
using TouhouCardEngine;
using UnityEngine;
using UnityEngine.TestTools;
namespace Tests
{
    /// <summary>
    /// 测试RPC功能
    /// </summary>
    public class LANNetworkingTests
    {
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
            local.invokeBroadcast(nameof(setFlag), remote.port, true);
            yield return TestHelper.waitUntil(() => flag, 5);
            Assert.True(flag);
        }
        public void setFlag(bool value)
        {
            flag = value;
        }
        bool flag = false;
    }
}
