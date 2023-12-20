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
    public abstract class Port<TValidOtherPort> : IPort
        where TValidOtherPort : IPort
    {

        public Port(Node node, PortDefine define)
        {
            this.node = node;
            this.define = define;
        }
        NodeConnection IPort.connect(IPort other) => connect((TValidOtherPort)other);
        void IPort.onConnected(NodeConnection connection) => onConnected(connection);
        void IPort.onDisconnected(NodeConnection connection) => onDisconnected(connection);
        public virtual bool canConnectTo(IPort other)
        {
            if (other == null)
            {
                return false;
            }

            return node != null && // 该端口属于一个节点
                other.node != null && // 对方端口属于一个节点
                other.node != node && // 不能连接自己
                other.node.graph == node.graph && // 在同一节点图中
                other is TValidOtherPort; // 属于可连接端口
        }

        public PortType GetPortType()
        {
            return this is IValuePort ? PortType.Value : PortType.Control;
        }
        public abstract void traverse(Action<Node> action, HashSet<Node> traversedActionNodeSet = null);
        public abstract NodeConnection connect(TValidOtherPort other);
        protected virtual void onConnected(NodeConnection connection) { }
        protected virtual void onDisconnected(NodeConnection connection) { }

        public override string ToString()
        {
            return $"{node}({name})";
        }
        public abstract IEnumerable<NodeConnection> connections { get; }
        public PortDefine define { get; }
        public Node node { get; private set; }
        public string name => define?.name;
        public abstract bool isOutput { get; }
    }
    public class ValueInput : Port<ValueOutput>, IValuePort
    {
        public ValueInput(Node node, PortDefine define, int paramIndex = -1) : base(node, define)
        {
            this.paramIndex = paramIndex;
        }
        public override NodeConnection connect(ValueOutput other)
        {
            if (connections.Count() > 0)
                node.graph.disconnectAll(this);
            return new NodeConnection(other, this);
        }
        public override bool canConnectTo(IPort other)
        {
            return base.canConnectTo(other) && PortDefine.CanTypeConvert(define.type, other?.define?.type);
        }
        public override void traverse(Action<Node> action, HashSet<Node> traversedActionNodeSet = null)
        {
            foreach (var connection in connections)
            {
                connection.traverse(action, traversedActionNodeSet);
            }
        }
        protected override void onConnected(NodeConnection connection)
        {
            _connection = connection;
            _connnectedOutput = connection.source as ValueOutput;
        }
        protected override void onDisconnected(NodeConnection connection)
        {
            _connection = null;
            _connnectedOutput = null;
        }
        public ValueOutput getConnectedOutputPort()
        {
            return _connnectedOutput;
        }
        public override IEnumerable<NodeConnection> connections 
        {
            get 
            {
                if (_connection != null)
                    yield return _connection;
            }
        }
        private NodeConnection _connection;
        private ValueOutput _connnectedOutput;
        public int paramIndex;
        public override bool isOutput => false;
    }
    public class ValueOutput : Port<ValueInput>, IValuePort
    {
        public ValueOutput(Node node, PortDefine define) : base(node, define)
        {
        }
        public override NodeConnection connect(ValueInput other)
        {
            return new NodeConnection(this, other);
        }
        public override bool canConnectTo(IPort other)
        {
            return base.canConnectTo(other) && PortDefine.CanTypeConvert(define.type, other?.define?.type);
        }
        public override void traverse(Action<Node> action, HashSet<Node> traversedActionNodeSet = null)
        {
            node.traverse(action, traversedActionNodeSet);
        }
        public override IEnumerable<NodeConnection> connections => node?.graph?.connections.Where(c => c.source == this) ?? Enumerable.Empty<NodeConnection>();

        public override bool isOutput => true;
    }
    public class ControlInput : Port<ControlOutput>
    {
        public ControlInput(Node node, PortDefine define) : base(node, define)
        {

        }
        public override NodeConnection connect(ControlOutput other)
        {
            return new NodeConnection(other, this);
        }
        public override void traverse(Action<Node> action, HashSet<Node> traversedActionNodeSet = null)
        {
            node.traverse(action, traversedActionNodeSet);
        }
        public override IEnumerable<NodeConnection> connections =>
            node?.graph?.connections.Where(c => c.destination == this) ?? Enumerable.Empty<NodeConnection>();
        public override bool isOutput => false;
    }
    public class ControlOutput : Port<ControlInput>
    {
        public ControlOutput(Node node, PortDefine define) : base(node, define)
        {

        }
        public override NodeConnection connect(ControlInput other)
        {
            if (connections.Count() > 0)
                node.graph.disconnectAll(this);
            return new NodeConnection(this, other);
        }
        public override void traverse(Action<Node> action, HashSet<Node> traversedActionNodeSet = null)
        {
            foreach (var connection in connections)
            {
                connection.traverse(action, traversedActionNodeSet);
            }
        }

        protected override void onConnected(NodeConnection connection)
        {
            _connection = connection;
            _connnectedInput = connection.destination as ControlInput;
        }
        protected override void onDisconnected(NodeConnection connection)
        {
            _connection = null;
            _connnectedInput = null;
        }
        public ControlInput getConnectedInputPort()
        {
            return _connnectedInput;
        }
        public override IEnumerable<NodeConnection> connections
        {
            get
            {
                if (_connection != null)
                    yield return _connection;
            }
        }
        private NodeConnection _connection;
        private ControlInput _connnectedInput;
        public override bool isOutput => true;
    }
}
