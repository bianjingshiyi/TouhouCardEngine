using System.Collections.Generic;
using System;

namespace TouhouCardEngine.Interfaces
{
    public interface ITraversable
    {
        void traverse(Action<Node> action, HashSet<Node> traversedActionNodeSet = null);
    }
}
