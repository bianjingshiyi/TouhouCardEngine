using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;
using TouhouCardEngine;
using TouhouCardEngine.Interfaces;

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
            SyncTriggerSystem system = createSystem();
            int i = 0, j = 0, k = 0;
            var task = system.doTask(
                game => i = 1,
                game => j = 2,
                game => k = 3);
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
            SyncTriggerSystem system = createSystem();
            int i = 0;
            var task = system.doTask(
                game => i++,
                game => i++,
                game => game.trigger.pauseTask(game.trigger.currentTask),
                game => i++);

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
            SyncTriggerSystem system = createSystem();
            int i = 0;
            var task = system.doTask(
                g1 => g1.trigger.doTask(g2 => i++),
                g1 => g1.trigger.doTask(g2 => i++),
                g1 => g1.trigger.doTask(g2 => Assert.NotNull(g2.trigger.currentTask.parent)));
            Assert.AreEqual(3, task.getChildren().Length);
            Assert.AreEqual(2, i);
        }
        [Test]
        public void stopTaskTest()
        {
            SyncTriggerSystem system = createSystem();
            int i = 0;
            var task = system.doTask(
                game => i++,
                game => game.trigger.stopTask(game.trigger.currentTask),
                game => i++);
            Assert.AreEqual(SyncTaskState.finished, task.state);
            Assert.AreEqual(1, i);
        }
        [Test]
        public void resumeFromTaskTest()
        {
            SyncTriggerSystem system = createSystem();
            int i = 0;
            var task1 = system.doTask(
                game => game.trigger.pauseTask(game.trigger.currentTask),
                game =>
                {
                    Assert.AreEqual(1, game.trigger.currentTask.id);
                    Assert.AreEqual(0, game.trigger.getPausedTasks().Length);
                    Assert.AreEqual(1, game.trigger.getResumeTaskStack().Length);
                },
                game => i++);
            Assert.AreEqual(1, task1.id);
            Assert.AreEqual(SyncTaskState.paused, task1.state);
            Assert.AreEqual(0, i);

            var task2 = system.doTask(
                game => game.trigger.resumeTask(task1),
                game => Assert.AreEqual(2, game.trigger.currentTask.id),
                game => i++);
            Assert.AreEqual(2, task2.id);
            Assert.AreEqual(2, i);
        }
        private static SyncTriggerSystem createSystem()
        {
            return new CardEngine().trigger;
        }
        [Test]
        public void doEventTest()
        {
            SyncTriggerSystem system = createSystem();
            int i = 0;
            system.doEvent(new EventContext("Test") { { "i", 1 } }, game =>
            {
                i = game.trigger.currentTask.context.getVar<int>("i");
            });
            Assert.AreEqual(1, i);
        }
        [Test]
        public void regGetUnregTest()
        {
            SyncTriggerSystem system = createSystem();
            SyncTrigger t1 = new SyncTrigger();
            SyncTrigger t2 = new SyncTrigger();
            system.regTrigBfr("Test", t1);
            system.regTrigAft("Test", t2);
            Assert.AreEqual(1, system.getTrigBfr("Test").Count());
            Assert.AreEqual(t1, system.getTrigBfr("Test").First());
            Assert.AreEqual(1, system.getTrigAft("Test").Count());
            Assert.AreEqual(t2, system.getTrigAft("Test").First());
            system.unregTrigBfr("Test", t1);
            system.unregTrigAft("Test", t2);
            Assert.AreEqual(0, system.getTrigBfr("Test").Count());
            Assert.AreEqual(0, system.getTrigAft("Test").Count());
        }
        [Test]
        public void doEventWithTrigger()
        {
            SyncTriggerSystem system = createSystem();
            int a = 0, b = 0;
            system.regTrigBfr("TestEvent", new SyncTrigger(game =>
            {
                return game.trigger.currentTask.context.hasVar("b");
            }, game =>
            {
                b = game.trigger.currentTask.context.getVar<int>("b");
            }));
            system.regTrigAft("TestEvent", new SyncTrigger(game =>
            {
                return game.trigger.currentTask.context.hasVar("a");
            }, game =>
            {
                a = game.trigger.currentTask.context.getVar<int>("a");
            }));
            system.doEvent(new EventContext("TestEvent")
            {
                { "a", 1 },
                { "b", 2 }
            });
            Assert.AreEqual(1, a);
            Assert.AreEqual(2, b);
        }
        /// <summary>
        /// getPrior返回值越大的Trigger越先执行
        /// </summary>
        [Test]
        public void doEventWithPriorTrig()
        {
            SyncTriggerSystem system = createSystem();
            int i = 0;
            system.regTrigBfr("TestEvent", new SyncTrigger(game =>
            {
                return 2;
            }, game =>
            {
                if (i == 0)
                    i = 1;
            }));
            system.regTrigBfr("TestEvent", new SyncTrigger(game =>
            {
                return 1;
            }, game =>
            {
                if (i == 1)
                    i = 2;
            }));
            system.doEvent(new EventContext("TestEvent"), game =>
            {
                if (i == 2)
                    i = 3;
            });
            Assert.AreEqual(3, i);
        }
    }
}