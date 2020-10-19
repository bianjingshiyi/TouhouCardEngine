using UnityEngine;
using TouhouCardEngine.Interfaces;
using System.Collections.Generic;
using System;
using System.Linq;
namespace TouhouCardEngine
{
    public class TimeManager : MonoBehaviour, ITimeManager, IDisposable
    {
        [SerializeField]
        List<Timer> _timerList = new List<Timer>();
        private void Update()
        {
            HashSet<Timer> updatedTimers = new HashSet<Timer>();
            while (true)
            {
                var timer = _timerList.FirstOrDefault(t => !updatedTimers.Contains(t));
                if (timer == null)
                    break;
                if (timer.startTime + timer.time <= Time.time)
                {
                    timer.expire();
                    _timerList.Remove(timer);
                }
                updatedTimers.Add(timer);
            }
        }
        public ITimer startTimer(float time)
        {
            Timer timer = new Timer(Time.time, time);
            _timerList.Add(timer);
            return timer;
        }

        public bool cancel(ITimer timer)
        {
            if (timer is Timer t && _timerList.Contains(t))
            {
                _timerList.Remove(t);
                return true;
            }
            else
                return false;
        }

        public void Dispose()
        {

        }
    }
    [Serializable]
    public class Timer : ITimer
    {
        [SerializeField]
        float _startTime;
        [SerializeField]
        float _time;
        public float startTime
        {
            get { return _startTime; }
        }
        public float time
        {
            get { return _time; }
        }
        public float remainedTime
        {
            get { return startTime + time - Time.time; }
        }
        public Timer(float startTime, float time)
        {
            _startTime = startTime;
            _time = time;
        }
        public void expire()
        {
            onExpired?.Invoke();
        }
        public event System.Action onExpired;
    }
}
