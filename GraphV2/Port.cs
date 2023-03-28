using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public abstract class Port<TValidOtherPort> : IPort
        where TValidOtherPort : IPort
    {

        public Port(Node node, PortDefine define)
        {
            this.node = node;
            this.define = define;
        }
        NodeConnection IPort.connect(IPort other) => connect((TValidOtherPort)other);
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

        public abstract void traverse(Action<Node> action, HashSet<Node> traversedActionNodeSet = null);
        public abstract NodeConnection connect(TValidOtherPort other);

        public static bool CanTypeConvert(Type inputType, Type outputType)
        {
            if (inputType == null || outputType == null)
                return false;
            //如果输入类型是ActionValueRef，那么无论输出类型是什么都可以，至少暂时还不方便做ActionValueRef的类型检查
            if (inputType == typeof(ActionValueRef))
            {
                return true;
            }
            //类型之间可以相互转化
            if (outputType.IsAssignableFrom(inputType) || inputType.IsAssignableFrom(outputType))
            {
                return true;
            }
            //在包装成数组之后可以相互转化
            if (inputType.IsArray && (outputType.IsAssignableFrom(inputType.GetElementType()) || inputType.GetElementType().IsAssignableFrom(outputType)))
            {
                return true;
            }
            //在拆包成对象之后可以相互转化
            if (outputType.IsArray && (outputType.GetElementType().IsAssignableFrom(inputType) || inputType.IsAssignableFrom(outputType.GetElementType())))
            {
                return true;
            }
            return false;
        }
        public override string ToString()
        {
            return name;
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
            return new NodeConnection(PortType.Value, other, this);
        }
        public override bool canConnectTo(IPort other)
        {
            return base.canConnectTo(other) && CanTypeConvert(define.type, other?.define?.type);
        }
        public override void traverse(Action<Node> action, HashSet<Node> traversedActionNodeSet = null)
        {
            foreach (var connection in connections)
            {
                connection.traverse(action, traversedActionNodeSet);
            }
        }
        public override IEnumerable<NodeConnection> connections => node?.graph?.connections.OfType<NodeConnection>().Where(c => c.destination == this) ?? Enumerable.Empty<NodeConnection>();

        public ValueOutput getConnectedOutputPort()
        {
            return connections.FirstOrDefault()?.source as ValueOutput;
        }
        public int paramIndex;
        public override bool isOutput => false;
    }
    public class ValueOutput : Port<ValueInput>, IValuePort
    {
        public ValueOutput(Node node, PortDefine define, Func<Flow, Task<object>> getValueFunc) : base(node, define)
        {
            this.getValueFunc = getValueFunc;
        }
        public override NodeConnection connect(ValueInput other)
        {
            return new NodeConnection(PortType.Value, this, other);
        }
        public override bool canConnectTo(IPort other)
        {
            return base.canConnectTo(other) && CanTypeConvert(define.type, other?.define?.type);
        }
        public override void traverse(Action<Node> action, HashSet<Node> traversedActionNodeSet = null)
        {
            node.traverse(action, traversedActionNodeSet);
        }
        public override IEnumerable<NodeConnection> connections => node?.graph?.connections.OfType<NodeConnection>().Where(c => c.source == this) ?? Enumerable.Empty<NodeConnection>();

        public Task<object> getValue(Flow flow)
        {
            return getValueFunc?.Invoke(flow);
        }
        private Func<Flow, Task<object>> getValueFunc;
        public override bool isOutput => true;
    }
    public class ControlInput : Port<ControlOutput>
    {
        public ControlInput(Node node, PortDefine define) : base(node, define)
        {

        }
        public override NodeConnection connect(ControlOutput other)
        {
            return new NodeConnection(PortType.Control, other, this);
        }
        public override void traverse(Action<Node> action, HashSet<Node> traversedActionNodeSet = null)
        {
            node.traverse(action, traversedActionNodeSet);
        }
        public override IEnumerable<NodeConnection> connections =>
            node?.graph?.connections.OfType<NodeConnection>().Where(c => c.destination == this) ?? Enumerable.Empty<NodeConnection>();
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
            return new NodeConnection(PortType.Control, this, other);
        }
        public override void traverse(Action<Node> action, HashSet<Node> traversedActionNodeSet = null)
        {
            foreach (var connection in connections)
            {
                connection.traverse(action, traversedActionNodeSet);
            }
        }
        public override IEnumerable<NodeConnection> connections =>
            node?.graph?.connections.OfType<NodeConnection>().Where(c => c.source == this) ?? Enumerable.Empty<NodeConnection>();

        public ControlInput getConnectedInputPort()
        {
            return connections.FirstOrDefault()?.destination as ControlInput;
        }
        public override bool isOutput => true;
    }
}
