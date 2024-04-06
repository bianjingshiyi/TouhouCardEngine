using MessagePack;
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
        public void notifyConnect()
        {
            if (source == null || destination == null)
                return;
            source.onConnected(this);
            destination.onConnected(this);
        }
        public void notifyDisconnect()
        {
            if (source == null || destination == null)
                return;
            source.onDisconnected(this);
            destination.onDisconnected(this);
        }
        public void traverse(Action<Node> action, HashSet<Node> traversedActionNodeSet = null)
        {
            if (source == null || destination == null)
                return;
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

        public override string ToString()
        {
            return $"{source}->{destination}";
        }
    }
    public class InvalidNodeConnection : NodeConnection
    {
        public InvalidNodeConnection(IPort srcPort, string srcPortName, int srcNodeId, IPort destPort, string destPortName, int destNodeId, int destParamIndex) : base(srcPort, destPort)
        {
            this.srcPortName = srcPortName;
            this.srcNodeId = srcNodeId;
            this.destPortName = destPortName;
            this.destNodeId = destNodeId;
            this.destParamIndex = destParamIndex;
        }
        public string srcPortName;
        public int srcNodeId;
        public string destPortName;
        public int destParamIndex;
        public int destNodeId;
    }
}
