using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TouhouCardEngine.Interfaces
{
    public interface IActionNode : ITraversable
    {
        Task<ControlOutput> run(Flow flow);
        int id { get; }
        IEnumerable<IPort> outputPorts { get; }
        IEnumerable<IPort> inputPorts { get; }
        IDictionary<string, object> consts { get; }
        float posX { get; set; }
        float posY { get; set; }
        ActionGraph graph { get; set; }
        ISerializableNode ToSerializableNode();
    }

    public interface ISerializableNode
    {
        IActionNode ToActionNode();
    }
    public static class XActionNode
    {
        public static object getConst(this IActionNode node, string name)
        {
            return node.getConst<object>(name);
        }
        public static T getConst<T>(this IActionNode node, string name)
        {
            if (node == null || node.consts == null)
                return default;
            return node.consts.Where(p => p.Key == name).Select(p => p.Value).OfType<T>().FirstOrDefault();
        }
        public static ValueInput getParamInputPort(this IActionNode node, string name, int index)
        {
            return node.getParamInputPorts(name)?[index];
        }
        public static ValueInput[] getParamInputPorts(this IActionNode node, string name)
        {
            return node.inputPorts.OfType<ValueInput>().Where(p => p.name == name).ToArray();
        }
        public static IPort getInputPort(this IActionNode node, string name)
        {
            return node.inputPorts.FirstOrDefault(p => p.name == name);
        }
        public static IPort getOutputPort(this IActionNode node, string name)
        {
            return node.outputPorts.FirstOrDefault(p => p.name == name);
        }
        public static TPort getInputPort<TPort>(this IActionNode node, string name) where TPort : IPort
        {
            if (node.getInputPort(name) is TPort tport)
            {
                return tport;
            }
            return default;
        }
        public static TPort getOutputPort<TPort>(this IActionNode node, string name) where TPort : IPort
        {
            if (node.getOutputPort(name) is TPort tport)
            {
                return tport;
            }
            return default;
        }
        public static IPort getInputPortAt(this IActionNode node, int index)
        {
            return node.inputPorts.ElementAtOrDefault(index);
        }
        public static IPort getOutputPortAt(this IActionNode node, int index)
        {
            return node.outputPorts.ElementAtOrDefault(index);
        }
        public static TPort getInputPortAt<TPort>(this IActionNode node, int index) where TPort : IPort
        {
            if (node.getInputPortAt(index) is TPort tport)
            {
                return tport;
            }
            return default;
        }
        public static TPort getOutputPortAt<TPort>(this IActionNode node, int index) where TPort : IPort
        {
            if (node.getOutputPortAt(index) is TPort tport)
            {
                return tport;
            }
            return default;
        }

        public static IEnumerable<TPort> getInputPorts<TPort>(this IActionNode node) where TPort : IPort
        {
            return node.inputPorts.OfType<TPort>();
        }
        public static IEnumerable<TPort> getOutputPorts<TPort>(this IActionNode node) where TPort : IPort
        {
            return node.outputPorts.OfType<TPort>();
        }
    }

}
