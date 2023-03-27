using System.Collections.Generic;
using System;

namespace TouhouCardEngine.Interfaces
{
    public interface ITraversable
    {
        void traverse(Action<IActionNode> action, HashSet<IActionNode> traversedActionNodeSet = null);
    }
}
