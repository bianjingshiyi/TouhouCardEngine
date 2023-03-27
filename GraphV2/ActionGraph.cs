using System;
using System.Collections.Generic;
using System.Linq;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public delegate ActionDefine ActionDefineFinder(string name);
    public delegate Type TypeFinder(string name);
    public class ActionGraph
    {
        #region 公共方法

        #region 构造函数
        public ActionGraph()
        {
            nodes = new List<IActionNode>();
            connections = new List<NodeConnection>();
        }
        #endregion

        #region 创建/移除节点
        public bool removeNode(IActionNode node)
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
                return connection;
            }
            return null;
        }
        public bool disconnect(NodeConnection connection)
        {
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
        public int disconnectAll(IActionNode node)
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
        public IEnumerable<NodeConnection> getNodeConnections(IActionNode node)
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
        #endregion 查找

        #endregion 公共方法

        #region 内部方法
        internal void AddNodes(IEnumerable<IActionNode> nodes)
        {
            this.nodes.AddRange(nodes);
        }
        internal void AddConnections(IEnumerable<NodeConnection> connections)
        {
            this.connections.AddRange(connections);
        }
        #endregion 内部方法

        #region 私有方法
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
        public List<IActionNode> nodes { get; private set; }
        public List<NodeConnection> connections { get; private set; }
    }
}
