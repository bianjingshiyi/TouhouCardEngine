using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
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
            if (inputList != null)
            {
                foreach (var port in inputList)
                {
                    if (port == null)
                        continue;
                    port.traverse(action, traversedNodes);
                }
            }
            //遍历常量
            if (constList != null)
            {
                foreach (var cst in constList.Values)
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
            if (outputList != null)
            {
                foreach (var port in outputList)
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
            if (constList == null)
                return default;
            if (!constList.TryGetValue(name, out object value))
                return default;
            if (!(value is T result))
                return default;
            return result;
        }
        public ValueInput getParamInputPort(string name, int index)
        {
            int curIndex = 0;
            foreach (var port in inputList)
            {
                if (!(port.name == name && port is ValueInput valueInput))
                    continue;
                if (curIndex == index)
                {
                    return valueInput;
                }
                else
                {
                    curIndex++;
                }
            }
            return null;
        }
        public ValueInput[] getParamInputPorts(string name)
        {
            return inputList.OfType<ValueInput>().Where(p => p.name == name).ToArray();
        }
        public IPort getInputPort(string name)
        {
            foreach (var port in inputList)
            {
                if (port.name == name)
                {
                    return port;
                }
            }
            return null;
        }
        public IPort getOutputPort(string name)
        {
            foreach (var port in outputList)
            {
                if (port.name == name)
                {
                    return port;
                }
            }
            return null;
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
            if (index < 0 || index >= inputList.Count)
                return null;
            return inputList[index];
        }
        public IPort getOutputPortAt(int index)
        {
            if (index < 0 || index >= outputList.Count)
                return null;
            return outputList[index];
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
            foreach (var port in inputList)
            {
                if (port is TPort result)
                {
                    yield return result;
                }
            }
            yield break;
        }
        public IEnumerable<TPort> getOutputPorts<TPort>() where TPort : IPort
        {
            foreach (var port in outputList)
            {
                if (port is TPort result)
                {
                    yield return result;
                }
            }
            yield break;
        }
        #endregion
        #region 设置端口
        public void setConst(string name, object value)
        {
            if (!constList.ContainsKey(name))
            {
                constList.Add(name, value);
            }
            else
            {
                constList[name] = value;
            }
        }
        public ValueInput extendParamsPort(string name)
        {
            var ports = getParamInputPorts(name);
            var portDefine = ports.Select(p => p.define).FirstOrDefault();
            var count = ports.Count();
            var valueInput = new ValueInput(this, portDefine, count);
            inputList.Add(valueInput);
            return valueInput;
        }
        #endregion
        /// <summary>
        /// 该动作的输出端口。
        /// </summary>
        protected List<IPort> outputList = new List<IPort>();
        /// <summary>
        /// 该动作的输入端口。
        /// </summary>
        protected List<IPort> inputList = new List<IPort>();
        /// <summary>
        /// 该动作的常量列表。
        /// </summary>
        protected Dictionary<string, object> constList = new Dictionary<string, object>();
        public IEnumerable<IPort> outputPorts => outputList;
        public IEnumerable<IPort> inputPorts => inputList;
        public IDictionary<string, object> consts => constList;
        public int id { get; set; }
        public float posX { get; set; }
        public float posY { get; set; }
        public ActionGraph graph { get; set; }
    }
    public interface ISerializableNode
    {
        Node ToActionNode(ActionGraph graph);
    }

}
