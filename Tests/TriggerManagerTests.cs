﻿using System;
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

            var triggerTime = new EventTriggerTime(new EventReference(0, "TestEvent"), EventTriggerTimeType.Before);
            manager.register(triggerTime, triggerA);
            manager.register(triggerTime, triggerB);

            var triggers = manager.getTriggers(triggerTime);
            Assert.True(triggers.Contains(triggerA));
            Assert.True(triggers.Contains(triggerB));
        }
        public class TestTrigger : Trigger
        {
            public Func<IEventArg, bool> condition { get; set; }
            public IEventArg eventArg;
            public TestTrigger(Func<IEventArg, bool> condition = null, IEventArg eventArg = null) : base()
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
            public override int getPriority() => 0;
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
