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
        public class TestTrigger : Trigger
        {
            public Func<IEventArg, bool> condition { get; set; }
            public IEventArg eventArg;
            public TestTrigger(Func<IEventArg, bool> condition = null, IEventArg eventArg = null)
            {
                this.condition = condition;
                this.eventArg = eventArg;
            }
            public override bool checkCondition(IEventArg arg)
            {
                if (condition != null)
                    return condition.Invoke(arg);
                else
                    return true;
            }
            public override Task invoke(IEventArg arg)
            {
                if (eventArg != null)
                    eventArg.execute();
                return Task.CompletedTask;
            }
        }
        class TestEventDefine : EventDefine
        {
            public TestEventDefine(Action<IEventArg> action)
            {
                this.action = action;
            }
            public override Task execute(IEventArg arg)
            {
                action?.Invoke(arg);
                return Task.CompletedTask;
            }
            [Obsolete]
            public override void Record(CardEngine game, EventArg arg, EventRecord record)
            {
            }
            public IGame game { get; set; }
            public Action<IEventArg> action;
        }
    }
}
