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
using Action = System.Action;
using TAction = TouhouCardEngine.Action;
namespace Tests
{
    public class SyncTriggerSystemTests
    {
        [Test]
        public void doTaskTest()
        {
            SyncTriggerSystem system = new SyncTriggerSystem();
            int i = 0, j = 0, k = 0;
            system.doTask(new ActionCollection()
            {
                new CAction(()=>i=1),
                new CAction(()=>j=2),
                new CAction(()=>k=3)
            });

            Assert.AreEqual(1, i);
            Assert.AreEqual(2, j);
            Assert.AreEqual(3, k);
        }
        [Test]
        public void pauseAndResumeTest()
        {
            SyncTriggerSystem system = new SyncTriggerSystem();
            int i = 0;
            var task = system.doTask(new ActionCollection()
            {
                new CAction(()=>i++),
                new CAction(()=>i++),
                new CAction(()=>system.currentTask.pause()),
                new CAction(()=>i++)
            });

            Assert.AreEqual(SyncTaskState.paused, task.state);
            Assert.AreEqual(2, i);

            system.currentTask.resume();

            Assert.AreEqual(3, i);
        }
    }
    class CAction : TAction
    {
        Action _action;
        public CAction(Action action)
        {
            _action = action;
        }
        public override void execute()
        {
            _action?.Invoke();
        }
    }
}
