﻿using System.Collections.Generic;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;
namespace TouhouCardEngine
{
    public class IntegerConstActionDefine : ActionDefine
    {
        public IntegerConstActionDefine() : base("IntegerConst", "IntegerConst")
        {
            inputs = new PortDefine[]
            {
                enterPortDefine,
            };
            consts = new PortDefine[1]
            {
                PortDefine.Const(typeof(int), "value")
            };
            outputs = new PortDefine[]
            {
                exitPortDefine,
                PortDefine.Value(typeof(int), "result", "value")
            };
            category = "Const";
        }
        public override Task<ControlOutput> run(Flow flow, Node node)
        {
            flow.setValue(node.getOutputPort<ValueOutput>("result"), node.getConst<int>("value"));
            return Task.FromResult(node.getOutputPort<ControlOutput>(exitPortName));
        }
        private PortDefine[] inputs { get; }
        private PortDefine[] consts { get; }
        private PortDefine[] outputs { get; }

        public override IEnumerable<PortDefine> inputDefines => inputs;

        public override IEnumerable<PortDefine> constDefines => consts;

        public override IEnumerable<PortDefine> outputDefines => outputs;
    }
}