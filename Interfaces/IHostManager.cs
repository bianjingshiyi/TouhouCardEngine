using System;

namespace TouhouCardEngine.Interfaces
{
    public interface IHostManager
    {
        int port { get; }
        void start();
        void start(int port);
    }
}
