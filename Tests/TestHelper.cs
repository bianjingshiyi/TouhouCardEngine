using UnityEngine;
using System.Threading.Tasks;
using BJSYGameCore;
using System;

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
            return new WaitUntil(() => timer.isExpired() || condition());
        }
    }
}
