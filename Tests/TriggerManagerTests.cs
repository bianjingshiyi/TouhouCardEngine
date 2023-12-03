using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using TouhouCardEngine;
using TouhouCardEngine.Histories;
using TouhouCardEngine.Interfaces;
using UnityEngine;

namespace Tests
{
    public class TriggerManagerTests
    {
        [Test]
        public void registerTest()
        {
            TriggerManager manager = new GameObject("TriggerManager").AddComponent<TriggerManager>();
            TestTrigger triggerA = new TestTrigger();
            TestTrigger triggerB = new TestTrigger();

            manager.register("BeforeTestEvent", triggerA);
            manager.register("BeforeTestEvent", triggerB);

            var triggers = manager.getTriggers("BeforeTestEvent");
            Assert.True(triggers.Contains(triggerA));
            Assert.True(triggers.Contains(triggerB));
        }
        public class TestTrigger : Trigger<TestEventArg>
        {
            public Func<IEventArg, bool> condition { get; set; }
            public IEventArg eventArg;
            public TestTrigger(Func<IEventArg, bool> condition = null, IEventArg eventArg = null)
            {
                this.condition = condition;
                this.eventArg = eventArg;
            }
            public override bool checkCondition(TestEventArg arg)
            {
                if (condition != null)
                    return condition.Invoke(arg);
                else
                    return true;
            }
            public override Task invoke(TestEventArg arg)
            {
                if (eventArg != null)
                    eventArg.action?.Invoke(eventArg);
                return Task.CompletedTask;
            }
        }
        public class TestEventArg : IEventArg
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
            public string[] getVarNames()
            {
                return varDict.Keys.ToArray();
            }
            public void Record(IGame game, EventRecord record)
            {
            }
            public void addChange(Change change)
            {
                _changes.Add(change);
            }
            public Change[] getChanges()
            {
                return _changes.ToArray();
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
            public EventState state { get; set; }
            public IGame game { get; set; }
            private List<Change> _changes = new List<Change>();

            public void setParent(IEventArg value)
            {
                parent = value;
                if (value is TestEventArg tea)
                    tea.childEventList.Add(this);
            }
        }
    }
}
