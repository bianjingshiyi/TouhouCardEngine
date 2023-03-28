using System.Collections.Generic;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;
namespace TouhouCardEngine
{
    public class StringConstActionDefine : ActionDefine
    {
        public StringConstActionDefine() : base("StringConst")
        {
            inputs = new PortDefine[]
            {
                enterPortDefine,
            };
            consts = new PortDefine[1]
            {
                PortDefine.Const(typeof(string), "value", "Value")
            };
            outputs = new PortDefine[]
            {
                exitPortDefine,
                PortDefine.Value(typeof(string), "value", "Value")
            };
            category = "Const";
        }
        public override Task<ControlOutput> run(Flow flow, Node node)
        {
            flow.setValue(node.getOutputPort<ValueOutput>("value"), node.getConst("value"));
            return Task.FromResult<ControlOutput>(null);
        }
        public PortDefine[] inputs { get; }
        public PortDefine[] consts { get; }
        public PortDefine[] outputs { get; }

        public override IEnumerable<PortDefine> inputDefines => inputs;

        public override IEnumerable<PortDefine> constDefines => consts;

        public override IEnumerable<PortDefine> outputDefines => outputs;
    }
}