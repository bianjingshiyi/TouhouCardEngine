using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TouhouCardEngine.Interfaces
{
    public abstract class Node : ITraversable
    {
        public abstract Task<ControlOutput> run(Flow flow);
        public abstract ISerializableNode ToSerializableNode();
        public virtual void traverse(Action<Node> action, HashSet<Node> traversedNodes = null)
        {
            if (action == null)
                return;
            if (traversedNodes == null)
                traversedNodes = new HashSet<Node>();
            else if (traversedNodes.Contains(this))
                return;
            traversedNodes.Add(this);
            action(this);
            //遍历输入
            if (inputPorts != null)
            {
                foreach (var port in inputPorts)
                {
                    if (port == null)
                        continue;
                    port.traverse(action, traversedNodes);
                }
            }
            //遍历常量
            if (consts != null)
            {
                foreach (var cst in consts.Values)
                {
                    if (cst == null)
                        continue;
                    if (cst is ITraversable traversable)
                    {
                        traversable.traverse(action, traversedNodes);
                    }
                }
            }
            //遍历后续
            if (outputPorts != null)
            {
                foreach (var port in outputPorts)
                {
                    if (port == null)
                        continue;
                    port.traverse(action, traversedNodes);
                }
            }
        }
        #region 寻找端口

        public object getConst(string name)
        {
            return getConst<object>(name);
        }
        public T getConst<T>(string name)
        {
            if (consts == null)
                return default;
            return consts.Where(p => p.Key == name).Select(p => p.Value).OfType<T>().FirstOrDefault();
        }
        public ValueInput getParamInputPort(string name, int index)
        {
            return inputPorts.OfType<ValueInput>().Where(p => p.name == name).ElementAtOrDefault(index);
        }
        public ValueInput[] getParamInputPorts(string name)
        {
            return inputPorts.OfType<ValueInput>().Where(p => p.name == name).ToArray();
        }
        public IPort getInputPort(string name)
        {
            return inputPorts.FirstOrDefault(p => p.name == name);
        }
        public IPort getOutputPort(string name)
        {
            return outputPorts.FirstOrDefault(p => p.name == name);
        }
        public TPort getInputPort<TPort>(string name) where TPort : IPort
        {
            if (getInputPort(name) is TPort tport)
            {
                return tport;
            }
            return default;
        }
        public TPort getOutputPort<TPort>(string name) where TPort : IPort
        {
            if (getOutputPort(name) is TPort tport)
            {
                return tport;
            }
            return default;
        }
        public IPort getInputPortAt(int index)
        {
            return inputPorts.ElementAtOrDefault(index);
        }
        public IPort getOutputPortAt(int index)
        {
            return outputPorts.ElementAtOrDefault(index);
        }
        public TPort getInputPortAt<TPort>(int index) where TPort : IPort
        {
            if (getInputPortAt(index) is TPort tport)
            {
                return tport;
            }
            return default;
        }
        public TPort getOutputPortAt<TPort>(int index) where TPort : IPort
        {
            if (getOutputPortAt(index) is TPort tport)
            {
                return tport;
            }
            return default;
        }

        public IEnumerable<TPort> getInputPorts<TPort>() where TPort : IPort
        {
            return inputPorts.OfType<TPort>();
        }
        public IEnumerable<TPort> getOutputPorts<TPort>() where TPort : IPort
        {
            return outputPorts.OfType<TPort>();
        }
        #endregion

        public abstract IEnumerable<IPort> outputPorts { get; }
        public abstract IEnumerable<IPort> inputPorts { get; }
        public abstract IDictionary<string, object> consts { get; }
        public int id { get; internal set; }
        public float posX { get; set; }
        public float posY { get; set; }
        public ActionGraph graph { get; set; }
    }
    public interface IDefineNode<TDefine>
    {
        TDefine define { get; set; }
        void Define();
    }
    public interface ISerializableNode
    {
        Node ToActionNode();
    }

}
