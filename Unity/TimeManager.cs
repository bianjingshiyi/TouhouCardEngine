using UnityEngine;
using TouhouCardEngine.Interfaces;
using System.Collections.Generic;
using System;

namespace TouhouCardEngine
{
    public class TimeManager : MonoBehaviour, ITimeManager
    {
        [SerializeField]
        List<Timer> _timerList = new List<Timer>();
        private void Update()
        {
            for (int i = 0; i < _timerList.Count; i++)
            {
                var timer = _timerList[i];
                if (timer.startTime + timer.time <= Time.time)
                {
                    timer.expire();
                    _timerList.RemoveAt(i);
                    i--;
                }
            }
        }
        public ITimer startTimer(float time)
        {
            Timer timer = new Timer(Time.time, time);
            _timerList.Add(timer);
            return timer;
        }
    }
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
        public Timer(float startTime, float time)
        {
            _startTime = startTime;
            _time = time;
        }
        public void expire()
        {
            onExpired?.Invoke();
        }
        public event Action onExpired;
    }
}
