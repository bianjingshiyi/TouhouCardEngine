using UnityEngine;
using System.Threading.Tasks;
using BJSYGameCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Tests
{
    static class TestHelper
    {
        public static WaitUntil waitTask(Task task, float timeout = 30)
        {
            Timer timer = null;
            if (timeout > 0)
            {
                timer = new Timer() { duration = timeout };
                timer.start();
            }
            return new WaitUntil(() =>
            {
                if (timer != null && timer.isExpired())
                    return false;
                if (task.IsCompleted)
                    return true;
                if (task.IsFaulted)
                {
                    Debug.LogError("等待任务失败");
                    return true;
                }
                if (task.IsCanceled)
                {
                    Debug.LogWarning("等待任务被取消");
                    return true;
                }
                if (task.Exception != null)
                {
                    Debug.LogError("等待任务发生异常：" + task.Exception);
                    return true;
                }
                return false;
            });
        }
        /// <summary>
        /// 用于在协程中等待任务完成。
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        /// <example>
        /// yield return task.wait()
        /// </example>
        public static WaitUntil wait(this Task task, float timeout = 30)
        {
            return waitTask(task, timeout);
        }
        public static WaitUntil waitUntil(Func<bool> condition, float timeout)
        {
            Timer timer = new Timer() { duration = timeout };
            timer.start();
            return new WaitUntil(() =>
            {
                if (timer.isExpired())
                {
                    Debug.LogError("测试等待超时");
                    throw new TimeoutException();
                }
                return condition();
            });
        }
        public static IEnumerator waitUntilEventTrig<T>(T target, Action<T, Action> regAction, Func<IEnumerator> trigAction)
        {
            bool flag = false;
            regAction(target, () => flag = true);
            yield return trigAction();
            yield return waitUntil(() => flag, 5);
        }
        public static IEnumerator waitUntilAllEventTrig<T>(IEnumerable<T> targets, Action<T, Action> reg, Func<IEnumerator> trigAction, float timeout = 5)
        {
            Dictionary<T, bool> flagDict = new Dictionary<T, bool>();
            foreach (var target in targets)
            {
                flagDict[target] = false;
                reg(target, () => flagDict[target] = true);
            }
            //做一些会引发事件的事情
            yield return trigAction();
            //预期所有目标都会触发事件
            yield return waitUntil(() => targets.All(t => flagDict[t]), timeout);
        }
    }
}
