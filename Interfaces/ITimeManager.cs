﻿using System;
namespace TouhouCardEngine.Interfaces
{
    public interface ITimeManager : IDisposable
    {
        ITimer startTimer(float time);
        bool cancel(ITimer timer);
    }
    public interface ITimer
    {
        float remainedTime { get; }
        float time { get; }
        event Action onExpired;
    }
}
