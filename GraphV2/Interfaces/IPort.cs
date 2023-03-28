using System.Collections.Generic;
using System.Linq;

namespace TouhouCardEngine.Interfaces
{
    public interface IPort : ITraversable
    {
        bool canConnectTo(IPort other);
        string name { get; }
        NodeConnection connect(IPort other);
        Node node { get; }
        IEnumerable<NodeConnection> connections { get; }
        PortDefine define { get; }
        bool isOutput { get; }
    }
    public interface IValuePort : IPort
    {
    }
    public static class XPort
    {

        public static int GetPortIndex(this IPort port)
        {
            var ports = port.isOutput ? port.node?.outputPorts : port.node?.inputPorts;
            if (ports == null)
            {
                return -1;
            }
            for (int i = 0; i < ports.Count(); i++)
            {
                if (ports.ElementAt(i) == port)
                    return i;
            }
            return -1;
        }
    }
}
