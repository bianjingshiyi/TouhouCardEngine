using System;
using System.Linq;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

using TouhouCardEngine;
using TouhouCardEngine.Interfaces;

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
            Assert.True(manager.answer(0, response));

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
            Assert.True(manager.answer(0, response));

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
            Assert.False(manager.answer(0, response));

            Assert.True(task.IsCompleted);
            Assert.False((task.Result as TestResponse).boolean);
            yield break;
        }
        [UnityTest]
        public IEnumerator askAnyTest()
        {
            AnswerManager manager = new GameObject("AnswerManager").AddComponent<AnswerManager>();
            TestRequest request = new TestRequest();

            var task = manager.askAny(new int[] { 1, 2, 3 }, request, 3, r => r is TestResponse tr && tr.boolean == true);
            Assert.False(manager.answer(3, new TestResponse() { boolean = false }));
            yield return new WaitForSeconds(1);
            TestResponse response = new TestResponse() { boolean = true };
            Assert.True(manager.answer(2, response));
            yield return new WaitForSeconds(1);
            Assert.False(manager.answer(1, response));

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
            Assert.False(manager.answer(1, new TestResponse() { boolean = true }));

            Assert.True(task.IsCompleted);
            TestResponse response = task.Result as TestResponse;
            Assert.AreEqual(false, response.boolean);
        }
        [UnityTest]
        public IEnumerator askAllTest()
        {
            AnswerManager manager = new GameObject("AnswerManager").AddComponent<AnswerManager>();
            TestRequest request = new TestRequest();

            var task = manager.askAll(new int[] { 1, 2, 3 }, request, 3);
            Assert.True(manager.answer(1, new TestResponse() { boolean = true }));
            yield return new WaitForSeconds(1);
            Assert.True(manager.answer(3, new TestResponse() { boolean = false }));
            yield return new WaitForSeconds(2);
            Assert.False(manager.answer(2, new TestResponse() { boolean = true }));

            Assert.True(task.IsCompleted);
            TestResponse[] responses = task.Result.Cast<TestResponse>().ToArray();
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
            Assert.True(manager.answer(0, responseC));
            TestResponse responseB = new TestResponse();
            Assert.True(manager.answer(0, responseB));
            yield return new WaitForSeconds(1);
            TestResponse responseA = new TestResponse();
            Assert.False(manager.answer(0, responseA));

            Assert.AreEqual(responseC, taskC.Result);
            Assert.AreEqual(responseB, taskB.Result);
            Assert.AreNotEqual(responseA, taskA.Result);
        }
        //TODO:插入询问，远程询问
        [Serializable]
        class TestRequest : IRequest
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
            public IResponse getDefaultResponse(IGame game)
            {
                return new TestResponse();
            }
        }
        [Serializable]
        class TestResponse : IResponse
        {
            [SerializeField]
            int _playerId;
            public int playerId
            {
                get { return _playerId; }
                set { _playerId = value; }
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
}
