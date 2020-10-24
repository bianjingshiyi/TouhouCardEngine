using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using TouhouCardEngine;

namespace Tests
{
    public class TimeManagerTests
    {
        [UnityTest]
        public IEnumerator startTimerTest()
        {
            TimeManager manager = new GameObject(nameof(TimeManager)).AddComponent<TimeManager>();
            bool flag = false;
            var timer = manager.startTimer(.5f);
            timer.onExpired += () =>
            {
                flag = true;
            };
            Assert.AreEqual(.5f, timer.time);

            Assert.False(flag);
            yield return new WaitForSeconds(.5f);
            Assert.True(flag);
        }
    }
}
