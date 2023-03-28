using System;
using System.Collections.Generic;
using System.Linq;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public class NodeConnection
    {
        public IPort source { get; protected set; }
        public IPort destination { get; protected set; }
        public PortType GetConnectionType()
        {
            if (source is IValuePort && destination is IValuePort)
            {
                return PortType.Value;
            }
            return PortType.Control;
        }
        public void traverse(Action<Node> action, HashSet<Node> traversedActionNodeSet = null)
        {
            if (GetConnectionType() == PortType.Value)
            {
                source.traverse(action, traversedActionNodeSet);
            }
            else
            {
                destination.traverse(action, traversedActionNodeSet);
            }
        }
        public NodeConnection(IPort source, IPort destination)
        {
            this.source = source;
            this.destination = destination;
        }
    }
    [Serializable]
    public class SerializableConnection
    {
        public SerializableConnection(NodeConnection connection)
        {
            sourceName = connection.source.define.name;
            sourceNodeId = connection.source.node.id;
            destName = connection.destination.define.name;
            destNodeId = connection.destination.node.id;
            if (connection.destination is ValueInput valueInput)
            {
                destParamIndex = valueInput.paramIndex + 1;
            }
        }
        public NodeConnection ToNodeConnection(ActionGraph graph)
        {
            var sourceNode = graph.nodes.First(n => n.id == sourceNodeId);
            var destNode = graph.nodes.First(n => n.id == destNodeId);
            var sourcePort = sourceNode.getOutputPort(sourceName);
            IPort destPort;
            if (destParamIndex > 0)
                destPort = destNode.getParamInputPort(destName, destParamIndex - 1);
            else
                destPort = destNode.getInputPort(destName);

            return new NodeConnection(sourcePort, destPort);
        }
        public string sourceName;
        public int sourceNodeId;
        public string destName;
        public int destParamIndex;
        public int destNodeId;
    }
}
