using NUnit.Framework;
using TouhouCardEngine;
using NitoriNetwork.Common;

namespace Tests
{
    public class RPCTests
    {
        /// <summary>
        /// 最简单的测试
        /// </summary>
        [Test]
        public void ExecuteSpecMethodTest()
        {
            var env = new RPCExecutor();

            var cla = new TestClassA();
            env.AddSingleton(cla);
            env.AddTargetMethod<TestClassA>(x => x.Add1(1));

            var req = new RPCRequest(typeof(int), nameof(TestClassA.Add1), 1);
            var result = env.Invoke(req);

            Assert.AreEqual(2, result);
        }

        /// <summary>
        /// 测试 AddTargetObjectTest 是否正确的添加了方法和对应的引用
        /// </summary>
        [Test]
        public void AddTargetObjectTest()
        {
            var env = new RPCExecutor();

            var cla = new TestClassA();
            env.AddTargetObject(cla);

            var req = new RPCRequest(typeof(int), nameof(TestClassA.Add1), 1);
            var result = env.Invoke(req);

            Assert.AreEqual(2, result);
        }

        /// <summary>
        /// 局部注入测试
        /// </summary>
        [Test]
        public void ScopeInjectionTest()
        {
            var env = new RPCExecutor();

            var cla = new TestClassA();
            env.AddTargetObject(cla);

            var req = new RPCRequest(typeof(int), nameof(TestClassA.Add2), 1);
            var result = env.Invoke(req, new object[] { new TestClassB(2) });

            Assert.AreEqual(3, result);
        }

        /// <summary>
        /// 全局注入测试
        /// </summary>
        [Test]
        public void GlobalInjectionTest()
        {
            var env = new RPCExecutor();

            var cla = new TestClassA();
            env.AddTargetObject(cla);

            env.AddSingleton(new TestClassC(3));

            var req = new RPCRequest(typeof(int), nameof(TestClassA.Add3), 1);
            var result = env.Invoke(req, new object[] { new TestClassB(2) });

            Assert.AreEqual(6, result);
        }

        class TestClassA
        {
            public int Add1(int val)
            {
                return val + 1;
            }
            public int Add2(TestClassB env, int val)
            {
                return env.Val + val;
            }
            public int Add3(TestClassC env1, TestClassB env2, int val)
            {
                return env1.Val + env2.Val + val;
            }
        }

        class TestClassB
        {
            public int Val;
            public TestClassB(int v) { Val = v; }
        }

        class TestClassC
        {
            public int Val;
            public TestClassC(int v) { Val = v; }
        }
    }
}
