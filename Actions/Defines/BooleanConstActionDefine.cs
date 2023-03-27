using System.Collections.Generic;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;
namespace TouhouCardEngine
{
    public class BooleanConstActionDefine : ActionDefine
    {
        public BooleanConstActionDefine() : base("BooleanConst")
        {
            inputs = new PortDefine[]
            {
                enterPortDefine,
            };
            consts = new PortDefine[1]
            {
                PortDefine.Const(typeof(bool), "value", "Value")
            };
            outputs = new PortDefine[]
            {
                exitPortDefine,
                PortDefine.Value(typeof(bool), "return", "Value")
            };
            category = "Const";
        }

        public override Task<ControlOutput> run(Flow flow, IActionNode node)
        {
            var value = node.getConst("value");
            if (value == null)
            {
                value = false;
            }
            flow.setValue(node.getOutputPort<ValueOutput>("return"), value);
            return Task.FromResult<ControlOutput>(null);
        }

        public override IEnumerable<PortDefine> inputDefines => inputs;
        public override IEnumerable<PortDefine> constDefines => consts;
        public override IEnumerable<PortDefine> outputDefines => outputs;
        private PortDefine[] inputs { get; }
        private PortDefine[] consts { get; }
        private PortDefine[] outputs { get; }
    }
}