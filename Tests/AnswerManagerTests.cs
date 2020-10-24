using System;
using System.Linq;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Net;
using System.Net.Sockets;
using TouhouCardEngine;
using TouhouCardEngine.Interfaces;
using System.Threading.Tasks;

namespace Tests
{
    public class AnswerManagerTests
    {
        [UnityTest]
        public IEnumerator askAndGetRequestTest()
        {
            AnswerManager manager = new GameObject("AnswerManager").AddComponent<AnswerManager>();
            TestRequest request = new TestRequest();

            _ = manager.ask(0, request, 3);

            Assert.AreEqual(request, manager.getLastRequest(0));

            yield break;
        }
        [UnityTest]
        public IEnumerator getRemainedTimeTest()
        {
            AnswerManager manager = new GameObject("AnswerManager").AddComponent<AnswerManager>();
            TestRequest request = new TestRequest();

            _ = manager.ask(0, request, 3);

            yield return new WaitForSeconds(1);

            Assert.True(1 < manager.getRemainedTime(request) && manager.getRemainedTime(request) <= 2);

            yield return new WaitForSeconds(3);

            Assert.AreEqual(0, manager.getRemainedTime(request));
        }
        [UnityTest]
        public IEnumerator timeOutTest()
        {
            AnswerManager manager = new GameObject("AnswerManager").AddComponent<AnswerManager>();
            TestRequest request = new TestRequest();

            var task = manager.ask(0, request, 3);

            yield return new WaitForSeconds(3);

            Assert.True(task.IsCompleted);
        }
        [UnityTest]
        public IEnumerator answerTest()
        {
            AnswerManager manager = new GameObject("AnswerManager").AddComponent<AnswerManager>();
            TestRequest request = new TestRequest();

            var task = manager.ask(0, request, 3);
            TestResponse response = new TestResponse();
            Assert.True(manager.answer(0, response).Result);

            Assert.True(task.IsCompleted);
            Assert.AreEqual(response, task.Result);
            yield break;
        }
        [UnityTest]
        public IEnumerator delayedAnswerTest()
        {
            AnswerManager manager = new GameObject("AnswerManager").AddComponent<AnswerManager>();
            TestRequest request = new TestRequest();

            var task = manager.ask(0, request, 3);
            TestResponse response = new TestResponse();
            yield return new WaitForSeconds(1);
            Assert.True(manager.answer(0, response).Result);

            Assert.True(task.IsCompleted);
            Assert.AreEqual(response, task.Result);
            yield break;
        }
        [UnityTest]
        public IEnumerator timeOutAnswerTest()
        {
            AnswerManager manager = new GameObject("AnswerManager").AddComponent<AnswerManager>();
            TestRequest request = new TestRequest();

            var task = manager.ask(0, request, 3);
            TestResponse response = new TestResponse() { boolean = true };
            yield return new WaitForSeconds(3);
            Assert.False(manager.answer(0, response).Result);

            Assert.True(task.IsCompleted);
            Assert.False((task.Result as TestResponse).boolean);
            yield break;
        }
        [UnityTest]
        public IEnumerator askAnyTest()
        {
            AnswerManager manager = new GameObject("AnswerManager").AddComponent<AnswerManager>();
            TestRequest request = new TestRequest();

            var task = manager.askAny(new int[] { 1, 2, 3 }, request, 3, r => r is TestResponse tr && tr.boolean == true);//询问123，必须回应tr且boolean为true
            Assert.False(manager.answer(3, new TestResponse() { boolean = false }).Result);//3回复false，不通过
            yield return new WaitForSeconds(1);
            TestResponse response = new TestResponse() { boolean = true };
            Assert.True(manager.answer(2, response).Result);//2回复true，通过
            yield return new WaitForSeconds(1);
            Assert.False(manager.answer(1, response).Result);//1回复true，但是已经有2回复了

            Assert.True(task.IsCompleted);
            Assert.AreEqual(response, task.Result);
        }
        [UnityTest]
        public IEnumerator askAnyTimeoutTest()
        {
            AnswerManager manager = new GameObject("AnswerManager").AddComponent<AnswerManager>();
            TestRequest request = new TestRequest();

            var task = manager.askAny(new int[] { 1, 2, 3 }, request, 3, r => r is TestResponse tr && tr.boolean == true);
            yield return new WaitForSeconds(3);
            Assert.False(manager.answer(1, new TestResponse() { boolean = true }).Result);

            Assert.True(task.IsCompleted);
            Assert.Null(task.Result);
            //TestResponse response = task.Result as TestResponse;
            //Assert.AreEqual(false, response.boolean);
        }
        [UnityTest]
        public IEnumerator askAllTest()
        {
            AnswerManager manager = new GameObject("AnswerManager").AddComponent<AnswerManager>();
            TestRequest request = new TestRequest();

            var task = manager.askAll(new int[] { 1, 2, 3 }, request, 3);
            Assert.True(manager.answer(1, new TestResponse() { boolean = true }).Result);
            yield return new WaitForSeconds(1);
            Assert.True(manager.answer(3, new TestResponse() { boolean = false }).Result);
            yield return new WaitForSeconds(2);
            Assert.False(manager.answer(2, new TestResponse() { boolean = true }).Result);

            Assert.True(task.IsCompleted);
            TestResponse[] responses = task.Result.Values.Cast<TestResponse>().ToArray();
            Assert.AreEqual(1, responses[0].playerId);
            Assert.AreEqual(true, responses[0].boolean);
            Assert.AreEqual(2, responses[1].playerId);
            Assert.AreEqual(false, responses[1].boolean);
            Assert.AreEqual(3, responses[2].playerId);
            Assert.AreEqual(false, responses[2].boolean);
        }
        [UnityTest]
        public IEnumerator multiAskTest()
        {
            AnswerManager manager = new GameObject("AnswerManager").AddComponent<AnswerManager>();

            TestRequest requestA = new TestRequest();
            var taskA = manager.ask(0, requestA, 3);
            yield return new WaitForSeconds(2);
            TestRequest requestB = new TestRequest();
            var taskB = manager.ask(0, requestB, 3);
            yield return new WaitForSeconds(2);
            TestRequest requestC = new TestRequest();
            var taskC = manager.ask(0, requestC, 3);

            yield return new WaitForSeconds(2);
            TestResponse responseC = new TestResponse();
            Assert.True(manager.answer(0, responseC).Result);
            TestResponse responseB = new TestResponse();
            Assert.True(manager.answer(0, responseB).Result);
            yield return new WaitForSeconds(1);
            TestResponse responseA = new TestResponse();
            Assert.False(manager.answer(0, responseA).Result);

            Assert.AreEqual(responseC, taskC.Result);
            Assert.AreEqual(responseB, taskB.Result);
            Assert.AreNotEqual(responseA, taskA.Result);
        }
        [UnityTest]
        public IEnumerator remoteUnaskedAnswer()
        {
            UnityLogger logger = new UnityLogger();
            HostManager host = new GameObject(nameof(HostManager)).AddComponent<HostManager>();
            host.logger = logger;
            host.start();
            ClientManager c1 = new GameObject(nameof(ClientManager)).AddComponent<ClientManager>();
            c1.logger = logger;
            c1.start();
            _ = c1.join(Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString(), host.port);
            AnswerManager a1 = new GameObject(nameof(AnswerManager)).AddComponent<AnswerManager>();
            a1.client = c1;
            yield return new WaitForSeconds(.5f);
            ClientManager c2 = new GameObject(nameof(ClientManager)).AddComponent<ClientManager>();
            c2.logger = logger;
            c2.start();
            _ = c2.join(Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString(), host.port);
            AnswerManager a2 = new GameObject(nameof(AnswerManager)).AddComponent<AnswerManager>();
            a2.client = c2;
            AnswerManager a3 = new GameObject(nameof(AnswerManager)).AddComponent<AnswerManager>();
            yield return new WaitForSeconds(.5f);

            bool r1 = false;
            bool r2 = false;
            bool r3 = false;
            a1.onResponse += onAnswer1;
            a2.onResponse += onAnswer2;
            a3.onResponse += onAnswer3;
            a1.unaskedAnswer(0, new TestResponse() { boolean = true });
            yield return new WaitForSeconds(.5f);
            void onAnswer1(IResponse response)
            {
                r1 = (response as TestResponse).boolean;
            }
            void onAnswer2(IResponse response)
            {
                r2 = (response as TestResponse).boolean;
            }
            void onAnswer3(IResponse response)
            {
                r3 = (response as TestResponse).boolean;
            }

            Assert.True(r1);
            Assert.True(r2);
            Assert.False(r3);
        }
        [UnityTest]
        public IEnumerator remoteAskAllTest()
        {
            UnityLogger logger = new UnityLogger();
            HostManager host = new GameObject(nameof(HostManager)).AddComponent<HostManager>();
            host.logger = logger;
            host.start();
            ClientManager c1 = new GameObject(nameof(ClientManager)).AddComponent<ClientManager>();
            c1.logger = logger;
            c1.start();
            _ = c1.join(Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString(), host.port);
            AnswerManager a1 = new GameObject(nameof(AnswerManager)).AddComponent<AnswerManager>();
            a1.client = c1;
            yield return new WaitForSeconds(.5f);
            ClientManager c2 = new GameObject(nameof(ClientManager)).AddComponent<ClientManager>();
            c2.logger = logger;
            c2.start();
            _ = c2.join(Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString(), host.port);
            AnswerManager a2 = new GameObject(nameof(AnswerManager)).AddComponent<AnswerManager>();
            a2.client = c2;
            AnswerManager a3 = new GameObject(nameof(AnswerManager)).AddComponent<AnswerManager>();
            yield return new WaitForSeconds(.5f);

            TestRequest request = new TestRequest();
            var task1 = a1.askAll(new int[] { c1.id, c2.id }, request, 3);
            var task2 = a2.askAll(new int[] { c1.id, c2.id }, request, 3);
            var task3 = a3.askAll(new int[] { c1.id, c2.id }, request, 3);
            a1.answer(c1.id, new TestResponse() { integer = 0 });
            yield return new WaitForSeconds(.5f);
            a2.answer(c2.id, new TestResponse() { integer = 1 });
            yield return new WaitForSeconds(.5f);

            Assert.True(task1.IsCompleted);
            foreach (var p in task1.Result)
            {
                Debug.Log("玩家" + p.Key + "：" + p.Value + "(PlayerID:" + p.Value.playerId + "，Value:" + (p.Value as TestResponse).integer + ")");
                Assert.AreEqual(p.Key, p.Value.playerId);
            }
            Assert.True(task2.IsCompleted);
            foreach (var p in task2.Result)
            {
                Debug.Log("玩家" + p.Key + "：" + p.Value + "(PlayerID:" + p.Value.playerId + "，Value:" + (p.Value as TestResponse).integer + ")");
                Assert.AreEqual(p.Key, p.Value.playerId);
            }
            Assert.False(task3.IsCompleted);
        }
        [UnityTest]
        public IEnumerator cancelTest()
        {
            AnswerManager manager = new GameObject("AnswerManager").AddComponent<AnswerManager>();

            TestRequest request = new TestRequest();
            var task = manager.ask(0, request, 5);
            yield return new WaitForSeconds(1);
            manager.cancel(request);

            Assert.True(task.IsCompleted);
            Assert.IsInstanceOf<TestResponse>(task.Result);
        }
        [UnityTest]
        public IEnumerator cancelRequestsTest()
        {
            AnswerManager manager = new GameObject("AnswerManager").AddComponent<AnswerManager>();

            TestRequest[] requests = new TestRequest[5];
            Task[] tasks = new Task[5];
            for (int i = 0; i < 5; i++)
            {
                requests[i] = new TestRequest();
                tasks[i] = manager.ask(0, requests[i], 5);
            }
            yield return new WaitForSeconds(1);
            manager.cancel(requests);

            for (int i = 0; i < tasks.Length; i++)
            {
                Assert.True(tasks[i].IsCompleted);
            }
        }
    }
    [Serializable]
    public class TestRequest : IRequest
    {
        [SerializeField]
        int[] _playersId;
        public int[] playersId
        {
            get { return _playersId; }
            set { _playersId = value; }
        }
        [SerializeField]
        bool _isAny;
        public bool isAny
        {
            get { return _isAny; }
            set { _isAny = value; }
        }
        [SerializeField]
        float _timeout;
        public float timeout
        {
            get { return _timeout; }
            set { _timeout = value; }
        }
        public bool isValidResponse(IResponse response)
        {
            return response is TestResponse;
        }
        public IResponse getDefaultResponse(IGame game, int playerId)
        {
            return new TestResponse();
        }
    }
    [Serializable]
    public class TestResponse : IResponse
    {
        [SerializeField]
        int _playerId;
        public int playerId
        {
            get { return _playerId; }
            set { _playerId = value; }
        }
        [SerializeField]
        bool _isUnasked;
        public bool isUnasked
        {
            get { return _isUnasked; }
            set { _isUnasked = value; }
        }
        [SerializeField]
        float _remainedTime = 0;
        public float remainedTime
        {
            get { return _remainedTime; }
            set { _remainedTime = value; }
        }
        [SerializeField]
        int _integer = 0;
        public int integer
        {
            get { return _integer; }
            set { _integer = value; }
        }
        [SerializeField]
        bool _boolean = false;
        public bool boolean
        {
            get { return _boolean; }
            set { _boolean = value; }
        }
    }
}
