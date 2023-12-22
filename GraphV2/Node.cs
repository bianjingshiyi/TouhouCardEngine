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
        public abstract SerializableNode ToSerializableNode();
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

        #region 输入默认值
        public object getInputDefaultValue(string name, int paramIndex)
        {
            return getInputDefaultValue<object>(name, paramIndex);
        }
        public T getInputDefaultValue<T>(string name, int paramIndex)
        {
            if (inputDefaultValueList == null)
                return default;
            var defaultValue = inputDefaultValueList.FirstOrDefault(v => v.name == name && v.paramIndex == paramIndex);
            if (defaultValue == null)
                return default;
            if (!(defaultValue.value is T result))
                return default;
            return result;
        }
        public bool hasInputDefaultValue(string name, int paramIndex)
        {
            if (inputDefaultValueList == null)
                return false;
            var defaultValue = inputDefaultValueList.FirstOrDefault(v => v.name == name && v.paramIndex == paramIndex);
            if (defaultValue?.value == null)
                return false;
            return true;
        }
        public InputDefaultValue[] getParamInputDefaultValues(string name)
        {
            if (inputDefaultValueList == null)
                return Array.Empty<InputDefaultValue>();
            return inputDefaultValueList.Where(v => v.name == name).ToArray();
        }
        public object getInputDefaultValue(string name)
        {
            return getInputDefaultValue<object>(name);
        }
        public T getInputDefaultValue<T>(string name)
        {
            if (inputDefaultValueList == null)
                return default;
            var defaultValue = inputDefaultValueList.FirstOrDefault(v => v.name == name);
            if (defaultValue == null)
                return default;
            if (!(defaultValue.value is T result))
                return default;
            return result;
        }
        public bool hasInputDefaultValue(string name)
        {
            if (inputDefaultValueList == null)
                return false;
            var defaultValue = inputDefaultValueList.FirstOrDefault(v => v.name == name);
            if (defaultValue == null)
                return false;
            return true;
        }
        public void setInputDefaultValue(string name, object value)
        {
            var defaultValue = inputDefaultValueList.FirstOrDefault(v => v.name == name);
            if (defaultValue == null)
            {
                defaultValue = new InputDefaultValue(name, -1, value);
                inputDefaultValueList.Add(defaultValue);
            }
            else
            {
                defaultValue.value = value;
            }
            updateInputDefaultValue(name, -1, value);
        }
        public void setInputDefaultValue(string name, int paramIndex, object value)
        {
            var defaultValue = inputDefaultValueList.FirstOrDefault(v => v.name == name && v.paramIndex == paramIndex);
            if (defaultValue == null)
            {
                defaultValue = new InputDefaultValue(name, paramIndex, value);
                inputDefaultValueList.Add(defaultValue);
            }
            else
            {
                defaultValue.value = value;
            }
            updateInputDefaultValue(name, paramIndex, value);
        }
        public int removeInputDefaultValue(string name, int paramIndex = -1)
        {
            int count = inputDefaultValueList.RemoveAll(d => d.name == name && (paramIndex < 0 || d.paramIndex == paramIndex));
            if (count > 0)
            {
                updateInputDefaultValue(name, paramIndex, null);
            }
            return count;
        }
        #endregion

        #region 寻找端口
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

        #region 节点属性
        public void setProp(string name, object value)
        {
            propDict[name] = value;
        }
        public T getProp<T>(string name)
        {
            if (propDict.TryGetValue(name, out var value) && value is T tValue)
                return tValue;
            return default;
        }
        public string[] getPropNames()
        {
            return propDict.Keys.ToArray();
        }
        #endregion

        protected IPort getOrCreateInputPort(PortDefine portDefine)
        {
            var existing = inputList.FirstOrDefault(d => d != null && d.define.Equals(portDefine));
            if (existing != null)
                return existing;
            return portDefine.GetPortType() == PortType.Control ? new ControlInput(this, portDefine) : new ValueInput(this, portDefine);
        }
        protected IPort getOrCreateOutputPort(PortDefine portDefine)
        {
            var existing = outputList.FirstOrDefault(d => d != null && d.define.Equals(portDefine));
            if (existing != null)
                return existing;
            return portDefine.GetPortType() == PortType.Control ? new ControlOutput(this, portDefine) : new ValueOutput(this, portDefine);
        }
        protected virtual void updateInputDefaultValue(string name, int paramIndex, object value)
        {
        }

        /// <summary>
        /// 该动作的输出端口。
        /// </summary>
        protected List<IPort> outputList = new List<IPort>();
        /// <summary>
        /// 该动作的输入端口。
        /// </summary>
        protected List<IPort> inputList = new List<IPort>();
        /// <summary>
        /// 该动作的输入默认值列表。
        /// </summary>
        private List<InputDefaultValue> inputDefaultValueList = new List<InputDefaultValue>();
        public IEnumerable<IPort> outputPorts => outputList;
        public IEnumerable<IPort> inputPorts => inputList;
        public IEnumerable<InputDefaultValue> inputDefaultValues => inputDefaultValueList;
        public int id { get; set; }
        public float posX { get; set; }
        public float posY { get; set; }
        public ActionGraph graph { get; set; }
        internal Dictionary<string, object> propDict = new Dictionary<string, object>();
    }
    public abstract class SerializableNode
    {
        public SerializableNode(Node node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            id = node.id;
            posX = node.posX;
            posY = node.posY;
            inputDefaultValues = node.inputDefaultValues != null ? node.inputDefaultValues.Select(v => new SerializableInputDefaultValue(v)).ToList() : null;
            propDict = node.propDict != null ? new Dictionary<string, object>(node.propDict) : null;
        }
        public abstract Node ToNode(ActionGraph graph);
        protected void InitNode(Node node, ActionGraph graph, IEnumerable<InputDefaultValue> defaultValues = null)
        {
            node.graph = graph;
            if (propDict != null)
            {
                foreach (var pair in propDict)
                {
                    node.setProp(pair.Key, pair.Value);
                }
            }
            defaultValues = defaultValues ?? inputDefaultValues?.ConvertAll(s => s.deserialize());
            if (defaultValues != null)
            {
                foreach (var defaultValue in defaultValues)
                {
                    node.setInputDefaultValue(defaultValue.name, defaultValue.paramIndex, defaultValue.value);
                }
            }
        }
        public int id;
        public float posX;
        public float posY;
        public List<SerializableInputDefaultValue> inputDefaultValues;
        public Dictionary<string, object> propDict;
    }

}
