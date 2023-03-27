using System;
using System.Collections.Generic;
using System.Linq;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public enum PortType
    {
        Control,
        Value
    }
    public class NodeConnection
    {
        public IPort source { get; protected set; }
        public IPort destination { get; protected set; }
        public PortType type { get; private set; }

        public void traverse(Action<IActionNode> action, HashSet<IActionNode> traversedActionNodeSet = null)
        {
            if (type == PortType.Control)
            {
                destination.traverse(action, traversedActionNodeSet);
            }
            else
            {
                source.traverse(action, traversedActionNodeSet);
            }
        }
        public NodeConnection(PortType type, IPort source, IPort destination)
        {
            this.type = type;
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
            type = (int)connection.type;
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

            return new NodeConnection((PortType)type, sourcePort, destPort);
        }
        public string sourceName;
        public int sourceNodeId;
        public string destName;
        public int destParamIndex;
        public int destNodeId;
        public int type;
    }
}
