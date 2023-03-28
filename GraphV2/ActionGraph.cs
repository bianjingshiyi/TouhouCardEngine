using System;
using System.Collections.Generic;
using System.Linq;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public delegate ActionDefine ActionDefineFinder(string name);
    public delegate EventDefine EventTypeInfoFinder(string name);
    public delegate Type TypeFinder(string name);
    public class ActionGraph
    {
        #region 公共方法

        #region 构造函数
        public ActionGraph()
        {
            nodes = new List<Node>();
            connections = new List<NodeConnection>();
        }
        #endregion

        #region 创建/移除节点
        public ActionNode createActionNode(string defineName, float posX = 0, float posY = 0)
        {
            int id = getUniqueNodeId();
            var node = new ActionNode(id, defineName);
            node.posX = posX;
            node.posY = posY;
            node.graph = this;
            nodes.Add(node);
            updateSize();
            return node;
        }
        public ActionNode createActionNode(ActionDefine define, float posX = 0, float posY = 0)
        {
            var node = createActionNode(define?.defineName, posX, posY);
            if (define != null)
            {
                node.define = define;
                node.Define();
            }
            return node;
        }
        public TriggerEntryNode createTriggerEntryNode(string eventName, float posX = 0, float posY = 0)
        {
            int id = getUniqueNodeId();
            var node = new TriggerEntryNode(id, eventName);
            node.posX = posX;
            node.posY = posY;
            node.graph = this;
            nodes.Add(node);
            updateSize();
            return node;
        }
        public TriggerEntryNode createTriggerEntryNode(EventDefine eventInfo, float posX = 0, float posY = 0)
        {
            var node = createTriggerEntryNode(eventInfo?.eventName, posX, posY);
            if (eventInfo != null)
            {
                node.define = eventInfo;
                node.Define();
            }
            return node;
        }
        public GeneratedActionEntryNode createActionDefineEntryNode(GeneratedActionDefine define, float posX = 0, float posY = 0)
        {
            int id = getUniqueNodeId();
            var node = new GeneratedActionEntryNode(id, define);
            node.posX = posX;
            node.posY = posY;
            node.graph = this;
            nodes.Add(node);
            node.Define();
            updateSize();
            return node;
        }
        public GeneratedActionReturnNode createActionDefineExitNode(GeneratedActionDefine define, float posX = 0, float posY = 0)
        {
            int id = getUniqueNodeId();
            var node = new GeneratedActionReturnNode(id, define);
            node.posX = posX;
            node.posY = posY;
            node.graph = this;
            nodes.Add(node);
            node.Define();
            updateSize();
            return node;
        }
        public bool removeNode(Node node)
        {
            if (nodes.Remove(node))
            {
                disconnectAll(node);
                updateSize();
                return true;
            }
            return false;
        }
        public void Clear()
        {
            nodes.Clear();
            connections.Clear();
        }
        #endregion

        #region 连接
        public NodeConnection connect(IPort port1, IPort port2)
        {
            if (port1.canConnectTo(port2) && !isConnected(port1, port2))
            {
                var connection = port1.connect(port2);
                connections.Add(connection);
                UpdateParamsInputs(connection.destination.node as ActionNode);
                return connection;
            }
            return null;
        }
        public bool disconnect(NodeConnection connection)
        {
            UpdateParamsInputs(connection.destination.node as ActionNode);
            return connections.Remove(connection);
        }
        public bool disconnect(IPort port1, IPort port2)
        {
            var connection = getConnection(port1, port2);
            if (connection != null)
            {
                return connections.Remove(connection);
            }
            return false;
        }
        public int disconnectAll(IPort port)
        {
            return connections.RemoveAll(c => c.source == port || c.destination == port);
        }
        public int disconnectAll(Node node)
        {
            var count = 0;
            foreach (var input in node.inputPorts)
            {
                count = disconnectAll(input);
            }
            foreach (var output in node.outputPorts)
            {
                count = disconnectAll(output);
            }
            return count;
        }
        public NodeConnection getConnection(IPort port1, IPort port2)
        {
            return connections.SingleOrDefault(c => (c.source == port1 && c.destination == port2) || (c.source == port2 && c.destination == port1));
        }
        public IEnumerable<NodeConnection> getNodeConnections(Node node)
        {
            HashSet<NodeConnection> connections = new HashSet<NodeConnection>();
            foreach (var input in node.inputPorts)
            {
                foreach (var connection in input.connections)
                {
                    connections.Add(connection);
                }
            }
            foreach (var output in node.outputPorts)
            {
                foreach (var connection in output.connections)
                {
                    connections.Add(connection);
                }
            }
            return connections.ToArray();
        }
        public bool isConnected(IPort port1, IPort port2)
        {
            return getConnection(port1, port2) != null;
        }
        #endregion 连接

        #region 查找
        public ActionNode findActionNode(string defineName)
        {
            return nodes.OfType<ActionNode>().FirstOrDefault(n => n.defineName == defineName);
        }
        public TriggerEntryNode findTriggerEntryNode(string eventName)
        {
            return nodes.OfType<TriggerEntryNode>().FirstOrDefault(n => n.eventName == eventName);
        }
        #endregion 查找

        #region 定义节点
        /// <summary>
        /// 使用动作定义/事件类型信息定义所有节点/触发器，以创建它们的端点和常量。
        /// </summary>
        /// <param name="defineFinder"></param>
        /// <param name="eventTypeInfoFinder"></param>
        public void DefineNodes(ActionDefineFinder defineFinder, EventTypeInfoFinder eventTypeInfoFinder)
        {
            foreach (var node in nodes)
            {
                if (node is ActionNode action)
                {
                    action.define = defineFinder?.Invoke(action.defineName);
                    action.Define();
                }
                if (node is TriggerEntryNode trigger)
                {
                    trigger.define = eventTypeInfoFinder?.Invoke(trigger.eventName);
                    trigger.Define();
                }
            }
        }
        /// <summary>
        /// 定义自定义动作内的入口节点和返回节点。
        /// </summary>
        /// <param name="actionDefine"></param>
        public void DefineGeneratedActionDefine(GeneratedActionDefine actionDefine)
        {
            foreach (var node in actionDefine.graph.nodes)
            {
                if (node is IDefineNode<GeneratedActionDefine> genNode)
                {
                    genNode.define = actionDefine;
                    genNode.Define();
                }
            }
        }
        #endregion

        #endregion 公共方法

        #region 内部方法
        internal void AddNodes(IEnumerable<Node> nodes)
        {
            this.nodes.AddRange(nodes);
        }
        internal void AddConnections(IEnumerable<NodeConnection> connections)
        {
            this.connections.AddRange(connections);
        }
        #endregion 内部方法

        #region 私有方法
        private void UpdateParamsInputs(ActionNode node)
        {
            if (node == null)
                return;
            node.Define();
        }
        private void updateSize()
        {
            //计算输入的宽度
            float maxX = 0;
            float minX = 0;
            float maxY = 0;
            float minY = 0;
            foreach (var node in nodes)
            {
                maxX = Math.Max(node.posX, maxX);
                minX = Math.Min(node.posX, minX);
                maxY = Math.Max(node.posY, maxY);
                minY = Math.Min(node.posY, minY);
            }
            width = maxX - minX;
            height = maxY - minY;
        }
        private int getUniqueNodeId()
        {
            int id = 1;
            while (nodes.Count(n => n.id == id) > 0)
            {
                id++;
            }
            return id;
        }
        #endregion 私有方法
        public float width { get; private set; }
        public float height { get; private set; }
        public List<Node> nodes { get; private set; }
        public List<NodeConnection> connections { get; private set; }
    }
    [Serializable]
    public sealed class SerializableActionNodeGraph
    {
        #region 公有方法
        public SerializableActionNodeGraph(ActionGraph graph)
        {
            if (graph == null)
                throw new ArgumentNullException(nameof(graph));
            nodes.AddRange(graph.nodes.ConvertAll(n => n.ToSerializableNode()));
            connections.AddRange(graph.connections.ConvertAll(c => new SerializableConnection(c)));
        }
        public ActionGraph toActionGraph(ActionDefineFinder defineFinder, EventTypeInfoFinder eventInfoFinder)
        {
            ActionGraph graph = new ActionGraph();
            graph.AddNodes(GetNodes());
            graph.DefineNodes(defineFinder, eventInfoFinder);
            graph.AddConnections(GetConnections(graph));
            return graph;
        }
        public Node[] GetNodes()
        {
            return nodes.ConvertAll(n => n.ToActionNode()).ToArray();
        }
        public NodeConnection[] GetConnections(ActionGraph graph)
        {
            return connections.ConvertAll(n => n.ToNodeConnection(graph)).ToArray();
        }
        #endregion
        #region 属性字段
        public List<ISerializableNode> nodes = new List<ISerializableNode>();
        public List<SerializableConnection> connections = new List<SerializableConnection>();
        #endregion
    }
}
