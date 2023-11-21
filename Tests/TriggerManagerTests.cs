using NUnit.Framework;
using System.Linq;
using UnityEngine;
using System.Threading.Tasks;
using System;
using System.Collections;
using UnityEngine.TestTools;
using System.Collections.Generic;
using TouhouCardEngine;
using TouhouCardEngine.Interfaces;

namespace Tests
{
    public class TriggerManagerTests
    {
        [Test]
        public void registerTest()
        {
            TriggerManager manager = new GameObject("TriggerManager").AddComponent<TriggerManager>();
            Trigger triggerA = new Trigger();
            Trigger triggerB = new Trigger();

            manager.register("BeforeTestEvent", triggerA);
            manager.register("BeforeTestEvent", triggerB);

            var triggers = manager.getTriggers("BeforeTestEvent");
            Assert.True(triggers.Contains(triggerA));
            Assert.True(triggers.Contains(triggerB));
        }
        [Test]
        public void doEventTest()
        {
            TriggerManager manager = new GameObject("TriggerManager").AddComponent<TriggerManager>();
            int a = 0;
            int b = 0;
            Trigger triggerA = new Trigger(action: args =>
            {
                a = (int)args[0];
                return Task.CompletedTask;
            });
            Trigger triggerB = new Trigger(action: args =>
            {
                b = (int)args[0];
                return Task.CompletedTask;
            });

            manager.register("BeforeTestEvent", triggerA);
            manager.register("BeforeTestEvent", triggerB);

            _ = manager.doEvent("BeforeTestEvent", 1);

            Assert.AreEqual(1, a);
            Assert.AreEqual(1, b);
        }
        [Test]
        public void removeTest()
        {
            TriggerManager manager = new GameObject("TriggerManager").AddComponent<TriggerManager>();
            Trigger triggerA = new Trigger();
            Trigger triggerB = new Trigger();

            manager.register("BeforeTestEvent", triggerA);

            Assert.True(manager.remove("BeforeTestEvent", triggerA));
            Assert.False(manager.remove("BeforeTestEvent", triggerB));
        }
        [Test]
        public void registerGenericTest()
        {
            TriggerManager manager = new GameObject("TriggerManager").AddComponent<TriggerManager>();
            Trigger<TestEventArg> trigger = new Trigger<TestEventArg>();

            manager.register(trigger);

            Assert.AreEqual(trigger, manager.getTriggers<TestEventArg>()[0]);
        }
        [Test]
        public void doEventGenericTest()
        {
            TriggerManager manager = new GameObject("TriggerManager").AddComponent<TriggerManager>();
            int value = 0;
            Trigger<TestEventArg> trigger = new Trigger<TestEventArg>(action: arg =>
            {
                value = arg.intValue;
                return Task.CompletedTask;
            });

            manager.register(trigger);

            _ = manager.doEvent(new TestEventArg() { intValue = 1 });

            Assert.AreEqual(1, value);
        }
        [Test]
        public void registerBeforeTest()
        {
            TriggerManager manager = new GameObject("TriggerManager").AddComponent<TriggerManager>();
            Trigger<TestEventArg> trigger = new Trigger<TestEventArg>();

            manager.registerBefore(trigger);
            manager.registerAfter(trigger);

            Assert.AreEqual(1, manager.getTriggersBefore<TestEventArg>().Length);
            Assert.AreEqual(trigger, manager.getTriggersBefore<TestEventArg>()[0]);
            Assert.AreEqual(1, manager.getTriggersAfter<TestEventArg>().Length);
            Assert.AreEqual(trigger, manager.getTriggersAfter<TestEventArg>()[0]);
        }
        [Test]
        public void doEventGenericPairedTest()
        {
            TriggerManager manager = new GameObject("TriggerManager").AddComponent<TriggerManager>();
            int intValue = 0;
            Trigger<TestEventArg> triggerA = new Trigger<TestEventArg>(action: arg =>
            {
                if (intValue == 0)
                    arg.intValue = 1;
                return Task.CompletedTask;
            });
            Trigger<TestEventArg> triggerB = new Trigger<TestEventArg>(action: arg =>
            {
                if (arg.intValue == 2)
                    intValue = 3;
                return Task.CompletedTask;
            });

            manager.registerBefore(triggerA);
            manager.registerAfter(triggerB);

            try
            {
                _ = manager.doEvent(new TestEventArg() { intValue = 1 }, arg =>
                {
                    if (arg.intValue == 1)
                        arg.intValue = 2;
                    return Task.CompletedTask;
                });
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            Assert.AreEqual(3, intValue);
        }
        [Test]
        public void triggerPriorTest()
        {
            TriggerManager manager = new GameObject("TriggerManager").AddComponent<TriggerManager>();
            Trigger[] triggers = new Trigger[10];
            int comparsion(ITrigger a, ITrigger b, IEventArg arg)
            {
                int indexA = triggers.Length - Array.IndexOf(triggers, a);
                int indexB = triggers.Length - Array.IndexOf(triggers, b);
                return indexA - indexB;
            }
            int intValue = 0;
            for (int i = 0; i < triggers.Length; i++)
            {
                int localI = i;
                triggers[9 - i] = new Trigger(action: (object[] args) =>
                {
                    if (intValue == localI)
                        intValue = localI + 1;
                    return Task.CompletedTask;
                }, comparsion: comparsion);
            }

            for (int i = 0; i < triggers.Length; i++)
            {
                manager.register("TestEvent", triggers[i]);
            }

            _ = manager.doEvent("TestEvent");

            Assert.AreEqual(10, intValue);
        }
        /// <summary>
        /// 混合两种，普通和泛型事件的测试。
        /// </summary>
        [Test]
        public void doEventComplexTest()
        {
            TriggerManager manager = new GameObject("TriggerManager").AddComponent<TriggerManager>();
            ITrigger[] triggers = new ITrigger[3];
            int comparsion(ITrigger a, ITrigger b, IEventArg arg)
            {
                int indexA = Array.IndexOf(triggers, a);
                int indexB = Array.IndexOf(triggers, b);
                return indexA - indexB;
            }
            int result = 0;

            triggers[0] = new Trigger(action: (object[] args) =>
            {
                result = 1;
                return Task.CompletedTask;
            }, comparsion: comparsion);
            manager.register("ObsolateTestEvent", triggers[0]);
            triggers[1] = new Trigger<TestEventArg>(action: arg =>
            {
                result *= arg.intValue;
                return Task.CompletedTask;
            }, comparsion: comparsion);
            manager.register(triggers[1] as Trigger<TestEventArg>);
            triggers[2] = new Trigger(action: (object[] args) =>
            {
                result += 10;
                return Task.CompletedTask;
            }, comparsion: comparsion);
            manager.register("TestEvent", triggers[2]);

            Task task = manager.doEvent(new string[] { "ObsolateTestEvent", "TestEvent" }, new TestEventArg() { intValue = 2 }, 1);

            if (task.Exception != null)
                Debug.LogError(task.Exception);
            Assert.AreEqual(12, result);
        }
        [UnityTest]
        public IEnumerator doEventComplexPairedTest()
        {
            TriggerManager manager = new GameObject("TriggerManager").AddComponent<TriggerManager>();
            ITrigger[] triggers = new ITrigger[4];
            int comparsion(ITrigger a, ITrigger b, IEventArg arg)
            {
                int indexA = Array.IndexOf(triggers, a);
                int indexB = Array.IndexOf(triggers, b);
                return indexA - indexB;
            }
            int result = 0;

            triggers[0] = new Trigger(action: async args =>
            {
                await Task.Delay(1);
                result = result * (int)args[0] + 2;
            }, comparsion: comparsion);
            manager.register("BeforeTestEvent", triggers[0]);
            triggers[1] = new Trigger<TestEventArg>(action: async arg =>
            {
                await Task.Delay(1);
                result = result * arg.intValue + 0;
            }, comparsion: comparsion);
            manager.registerBefore(triggers[1] as Trigger<TestEventArg>);
            triggers[2] = new Trigger<TestEventArg>(action: async arg =>
            {
                await Task.Delay(1);
                result = result * arg.intValue + 4;
            }, comparsion: comparsion);
            manager.registerAfter(triggers[2] as Trigger<TestEventArg>);
            triggers[3] = new Trigger(action: async args =>
            {
                await Task.Delay(1);
                result = result * (int)args[0] + 8;
            }, comparsion: comparsion);
            manager.register("AfterTestEvent", triggers[3]);

            object[] eventArgs = new object[] { 10 };
            Task task = manager.doEvent(new string[] { "BeforeTestEvent" }, new string[] { "AfterTestEvent" }, new TestEventArg() { intValue = 10 }, arg =>
            {
                arg.intValue *= 10;
                eventArgs[0] = 100;
                return Task.CompletedTask;
            }, eventArgs);

            yield return new WaitForSeconds(1);

            if (task.Exception != null)
                Debug.LogError(task.Exception);
            Assert.AreEqual(200408, result);
        }
        [Test]
        public void cancelEventTest()
        {
            TriggerManager manager = new GameObject("TriggerManager").AddComponent<TriggerManager>();
            bool a = false;
            manager.registerBefore(new Trigger<TestEventArg>(action: arg =>
            {
                a = true;
                arg.isCanceled = true;
                return Task.CompletedTask;
            }));
            bool b = false;
            manager.registerAfter(new Trigger<TestEventArg>(action: arg =>
            {
                b = true;
                return Task.CompletedTask;
            }));
            bool c = false;
            _ = manager.doEvent(new TestEventArg(), arg =>
            {
                c = true;
                return Task.CompletedTask;
            });

            Assert.True(a);
            Assert.False(b);
            Assert.False(c);
        }
        [Test]
        public void repeatEventTest()
        {
            TriggerManager manager = new GameObject("TriggerManager").AddComponent<TriggerManager>();
            manager.registerBefore(new Trigger<TestEventArg>(action: arg =>
            {
                arg.repeatTime = 3;
                return Task.CompletedTask;
            }));
            int i = 0;
            _ = manager.doEvent(new TestEventArg(), arg =>
            {
                i++;
                return Task.CompletedTask;
            });

            Assert.AreEqual(4, i);
        }
        [Test]
        public void replaceEventTest()
        {
            TriggerManager manager = new GameObject("TriggerManager").AddComponent<TriggerManager>();
            int value = 0;
            manager.registerBefore(new Trigger<TestEventArg>(action: arg1 =>
            {
                arg1.replaceAction(arg2 =>
                {
                    value = 2;
                    return Task.CompletedTask;
                });
                return Task.CompletedTask;
            }));

            _ = manager.doEvent(new TestEventArg(), arg3 =>
            {
                value = 1;
                return Task.CompletedTask;
            });

            Assert.AreEqual(2, value);
        }
        [Test]
        public void insertEventTest()
        {
            TriggerManager manager = new GameObject("TriggerManager").AddComponent<TriggerManager>();
            int value = 0;
            manager.register(new Trigger<TestEventArg>(action: arg1 =>
            {
                if (value == 0)
                    value = 1;
                manager.register(new Trigger<TestEventArg>(action: arg2 =>
                {
                    if (value == 1)
                        value = 2;
                    manager.register(new Trigger<TestEventArg>(action: arg3 =>
                    {
                        if (value == 2)
                            value = 3;
                        manager.registerBefore(new Trigger<TestEventArg>(action: arg4 =>
                        {
                            if (value == 3)
                                value = 4;
                            return Task.CompletedTask;
                        }));
                        return Task.CompletedTask;
                    }));
                    return Task.CompletedTask;
                }));
                return Task.CompletedTask;
            }));

            _ = manager.doEvent(new TestEventArg());

            Assert.AreEqual(3, value);
        }
        [Test]
        public void insertEventPairedTest()
        {
            TriggerManager manager = new GameObject("TriggerManager").AddComponent<TriggerManager>();
            int value = 0;
            manager.registerBefore(new Trigger<TestEventArg>(action: arg1 =>
            {
                if (value == 0)
                    value = 1;
                manager.registerBefore(new Trigger<TestEventArg>(action: arg2 =>
                {
                    if (value == 1)
                        value = 2;
                    manager.registerAfter(new Trigger<TestEventArg>(action: arg3 =>
                    {
                        if (value == 2)
                            value = 3;
                        manager.registerAfter(new Trigger<TestEventArg>(action: arg4 =>
                        {
                            if (value == 3)
                                value = 4;
                            return Task.CompletedTask;
                        }));
                        return Task.CompletedTask;
                    }));
                    return Task.CompletedTask;
                }));
                return Task.CompletedTask;
            }));

            Task task = manager.doEvent(new TestEventArg(), arg =>
            {
                return Task.CompletedTask;
            });

            if (task.Exception != null)
                Debug.LogError(task.Exception);
            Assert.AreEqual(4, value);
        }
        [Test]
        public void getChildEventTest()
        {
            TriggerManager manager = new GameObject("TriggerManager").AddComponent<TriggerManager>();
            TestEventArg e1 = new TestEventArg();
            TestEventArg e2 = new TestEventArg();
            TestEventArg e3 = new TestEventArg();
            _ = manager.doEvent(e1, arg =>
            {
                _ = manager.doEvent(e2);
                _ = manager.doEvent(e3);
                return Task.CompletedTask;
            });
            Assert.AreEqual(e1, manager.getRecordedEvents()[0]);
            Assert.AreEqual(e2, e1.getChildEvents(EventState.None)[0]);
            Assert.AreEqual(e3, e1.getChildEvents(EventState.None)[1]);
        }
        [Test]
        public void getParentEventTest()
        {
            ITriggerManager manager = new GameObject("TriggerManager").AddComponent<TriggerManager>();
            IEventArg e1 = new TestEventArg();
            IEventArg e2 = new TestEventArg();
            IEventArg e3 = new TestEventArg();
            getParentEventTest(manager, e1, e2, e3);
        }
        public static void getParentEventTest(ITriggerManager manager, IEventArg e1, IEventArg e2, IEventArg e3)
        {
            _ = manager.doEvent(e1, arg =>
            {
                _ = manager.doEvent(e2);
                _ = manager.doEvent(e3);
                return Task.CompletedTask;
            });
            Assert.AreEqual(e1, e2.parent);
            Assert.AreEqual(e1, e3.parent);
        }
        class TestEventArg : IEventArg
        {
            public IEventArg[] getAllChildEvents()
            {
                return childEventList.ToArray();
            }
            public IEventArg[] getChildEvents(EventState state)
            {
                return getAllChildEvents();
            }
            public object getVar(string varName)
            {
                if (varDict.TryGetValue(varName, out object value))
                    return value;
                else
                    return null;
            }
            public void setVar(string varName, object value)
            {
                varDict[varName] = value;
            }
            public void Record(IGame game, EventRecord record)
            {
            }
            public int intValue { get; set; } = 0;
            public bool isCanceled { get; set; } = false;
            public bool isCompleted { get; set; } = false;
            public EventRecord record { get; set; }
            public int repeatTime { get; set; } = 0;
            public Func<IEventArg, Task> action { get; set; }
            public string[] afterNames { get; set; }
            public object[] args { get; set; }
            public string[] beforeNames { get; set; }
            public int flowNodeId { get; set; }
            List<IEventArg> childEventList { get; } = new List<IEventArg>();
            public IEventArg parent { get; private set; }
            Dictionary<string, object> varDict { get; } = new Dictionary<string, object>();
            public EventState state { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public void setParent(IEventArg value)
            {
                parent = value;
                if (value is TestEventArg tea)
                    tea.childEventList.Add(this);
            }
        }
    }
}
