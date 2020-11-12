using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;
using TouhouCardEngine;
using TouhouCardEngine.Interfaces;
using System.Collections.Generic;
using UnityEngine.TestTools;
using System.Collections;
using UnityEngine;

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
        [Test]
        public void pauseAndResumeFromTaskTreeTest()
        {
            SyncTriggerSystem system = createSystem();
            SyncTask task = system.doTask("Use",
               useAction1,//在AskTask暂停了
               useAction2);
            void useAction1(CardEngine g1)
            {
                g1.trigger.doTask("Ask",
                    askAction1,
                    askAction2);
                void askAction1(CardEngine g2)
                {
                    g2.trigger.currentTask.parent.context["i"] = 1;
                }
                void askAction2(CardEngine g2)
                {
                    g2.trigger.pauseTask(g2.trigger.currentTask);
                }
            }
            void useAction2(CardEngine g1)
            {
                g1.trigger.currentTask.context["i"] = 2;
            }
            Assert.AreEqual(1, task.context["i"]);
            Assert.AreEqual(1, system.getPausedTasks().Count());
            system.resumeTask(system.getPausedTasks().First());
            Assert.AreEqual(2, task.context["i"]);
            Assert.AreEqual(0, system.getPausedTasks().Count());
        }
        [Test]
        public void pauseResumeFromOtherTaskTreeTest()
        {
            SyncTriggerSystem system = createSystem();
            SyncTask taskUse = system.doTask("Use",
                useAction1,
                useAction2);
            void useAction1(CardEngine g1)
            {
                g1.trigger.doTask("Ask",
                    askAction1,
                    askAction2);
                void askAction1(CardEngine g2)
                {
                    g2.trigger.currentTask.parent.context["i"] = 1;
                }
                void askAction2(CardEngine g2)
                {
                    g2.trigger.pauseTask(g2.trigger.currentTask);
                }
            }
            void useAction2(CardEngine g1)
            {
                int i = g1.trigger.currentTask.context.getVar<int>(nameof(i));
                g1.trigger.currentTask.context.setVar(nameof(i), i * 2);
            }
            Assert.AreEqual(1, taskUse.context["i"]);
            Assert.AreEqual(1, system.getPausedTasks().Count());
            SyncTask taskSkill = system.doTask("Skill",
                skillAction1,
                skillAction2,
                skillAction3);
            void skillAction1(CardEngine g1)
            {
                g1.trigger.getPausedTasks()[0].parent.context["i"] = 2;
            }
            void skillAction2(CardEngine g1)
            {
                g1.trigger.resumeTask(g1.trigger.getPausedTasks()[0]);
            }
            void skillAction3(CardEngine g1)
            {
                g1.trigger.currentTask.context["j"] = 3;
            }
            Assert.AreEqual(4, taskUse.context["i"]);
            Assert.AreEqual(0, system.getPausedTasks().Count());
            Assert.AreEqual(3, taskSkill.context["j"]);
        }
        private static SyncTriggerSystem createSystem()
        {
            CardEngine game = new CardEngine
            {
                time = new GameObject(nameof(TimeManager)).AddComponent<TimeManager>()
            };
            return game.trigger;
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
            Assert.AreEqual(0, system.getTrigBfr("Test").Count());
            Assert.AreEqual(0, system.getTrigAft("Test").Count());
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
        public void doTriggerTest()
        {
            SyncTriggerSystem system = createSystem();
            float f = 0;
            SyncTrigger trigger = new SyncTrigger(
                g1 => f += 1,
                g1 => f += 1);
            system.doTrigger(trigger);
            Assert.AreEqual(2, f);
        }
        [Test]
        public void doEventWithTriggerTest()
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

            Assert.AreEqual(2, b);
            Assert.AreEqual(1, a);
        }
        /// <summary>
        /// getPrior返回值越大的Trigger越先执行
        /// </summary>
        [Test]
        public void doEventWithPriorTrigTest()
        {
            SyncTriggerSystem system = createSystem();
            int i = 0;
            system.regTrigBfr("TestEvent", new SyncTrigger(game =>
            {
                return 1;
            }, game =>
            {
                if (i == 1)
                    i = 2;
            }));
            system.regTrigBfr("TestEvent", new SyncTrigger(game =>
            {
                return 2;
            }, game =>
            {
                if (i == 0)
                    i = 1;
            }));
            system.doEvent(new EventContext("TestEvent"), game =>
            {
                if (i == 2)
                    i = 3;
            });
            Assert.AreEqual(3, i);
        }
        [Test]
        public void askAndGetRequestsTest()
        {
            SyncTriggerSystem system = createSystem();
            SyncTask task = system.request(1, new EventContext("discover")
            {
                { "cards", new int[] { 1, 2, 3 } }
            }, float.MaxValue, null);
            Assert.AreEqual(SyncTaskState.paused, task.state);
            Assert.AreEqual(task, system.getAllRequestTasks()[0]);
        }
        [UnityTest]
        public IEnumerator getRemainedTimeTest()
        {
            SyncTriggerSystem system = createSystem();
            SyncTask task = system.request(1, new EventContext("discover")
            {
                { "cards", new int[] { 1, 2, 3 } }
            }, 3, ALambda.doNothing);

            yield return new WaitForSeconds(1);

            Assert.True(1 < system.getRemainedTime(task) && system.getRemainedTime(task) <= 2);

            yield return new WaitForSeconds(3);

            Assert.AreEqual(0, system.getRemainedTime(task));
        }
        [UnityTest]
        public IEnumerator timeoutTest()
        {
            SyncTriggerSystem system = createSystem();
            bool flag = false;
            SyncTask task = system.request(1, new EventContext("discover")
            {
                { "cards", new int[] { 1, 2, 3 } }
            }, 3, new ALambda(game => flag = true));

            yield return new WaitForSeconds(3);

            Assert.AreEqual(SyncTaskState.finished, task.state);
            Assert.AreEqual(0, system.getAllRequestTasks().Length);
            Assert.True(flag);
        }
        [Test]
        public void reponseTest()
        {
            SyncTriggerSystem system = createSystem();
            SyncTask task = system.request(1, new EventContext("discover")
            {
                { "cards", new int[] { 1, 2, 3 } }
            }, 3, ALambda.doNothing, new FLambda<bool>(game =>
            {
                EventContext context = game.trigger.currentTask.context;
                int[] cards = context.getVar<int[]>("cards");
                int card = context.getVar<int>("card");
                return cards.Contains(card);
            }));

            task = system.response(1, new EventContext("discover")
            {
                { "card", 4 }
            });
            Assert.AreEqual(SyncTaskState.paused, task.state);
            task = system.response(2, new EventContext("discover")
            {
                { "card", 3 }
            });
            Assert.AreEqual(SyncTaskState.paused, task.state);
            task = system.response(1, new EventContext("discover")
            {
                { "card", 3 }
            });
            Assert.AreEqual(SyncTaskState.finished, task.state);
        }
    }
}