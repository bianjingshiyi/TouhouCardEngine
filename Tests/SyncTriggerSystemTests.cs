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
        /// <summary>
        /// 构建同步任务对象，执行动作列表
        /// </summary>
        [Test]
        public void doTaskTest()
        {
            SyncTriggerSystem system = new SyncTriggerSystem();
            int i = 0, j = 0, k = 0;
            var task = system.doTask(new ActionCollection()
            {
                new CAction(()=>i=1),
                new CAction(()=>j=2),
                new CAction(()=>k=3)
            });
            Assert.NotNull(task);

            Assert.AreEqual(1, i);
            Assert.AreEqual(2, j);
            Assert.AreEqual(3, k);
        }
        /// <summary>
        /// 如果暂停
        /// </summary>
        [Test]
        public void pauseAndResumeTest()
        {
            SyncTriggerSystem system = new SyncTriggerSystem();
            int i = 0;
            var task = system.doTask(new ActionCollection()
            {
                new CAction(()=>i++),
                new CAction(()=>i++),
                new CAction(()=>system.pauseTask(system.currentTask)),
                new CAction(()=>i++)
            });

            Assert.Null(system.currentTask);
            Assert.AreEqual(SyncTaskState.paused, task.state);
            Assert.AreEqual(task, system.getPausedTasks()[0]);
            Assert.AreEqual(2, i);

            system.resumeTask(task);

            Assert.AreEqual(SyncTaskState.finished, task.state);
            Assert.AreEqual(0, system.getPausedTasks().Length);
            Assert.AreEqual(3, i);
        }
        [Test]
        public void taskTreeTest()
        {
            SyncTriggerSystem system = new SyncTriggerSystem();
            int i = 0;
            var task = system.doTask(
                () => system.doTask(() => i++),
                () => system.doTask(() => i++),
                () => system.doTask(() => Assert.NotNull(system.currentTask.parent)));
            Assert.AreEqual(3, task.getChildren().Length);
            Assert.AreEqual(2, i);
        }
        [Test]
        public void stopTaskTest()
        {
            SyncTriggerSystem system = new SyncTriggerSystem();
            int i = 0;
            var task = system.doTask(
                () => i++,
                () => system.stopTask(system.currentTask),
                () => i++);
            Assert.AreEqual(SyncTaskState.finished, task.state);
            Assert.AreEqual(1, i);
        }
        [Test]
        public void resumeFromTaskTest()
        {
            SyncTriggerSystem system = new SyncTriggerSystem();
            int i = 0;
            var task1 = system.doTask(
                () => system.pauseTask(system.currentTask),
                () =>
                {
                    Assert.AreEqual(1, system.currentTask.id);
                    Assert.AreEqual(0, system.getPausedTasks().Length);
                    Assert.AreEqual(1, system.getResumeTaskStack().Length);
                },
                () => i++);
            Assert.AreEqual(1, task1.id);
            Assert.AreEqual(SyncTaskState.paused, task1.state);
            Assert.AreEqual(0, i);

            var task2 = system.doTask(
                () => system.resumeTask(task1),
                () => Assert.AreEqual(2, system.currentTask.id),
                () => i++);
            Assert.AreEqual(2, task2.id);
            Assert.AreEqual(2, i);
        }
    }
}