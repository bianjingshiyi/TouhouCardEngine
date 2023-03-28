using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;
namespace TouhouCardEngine
{
    public class LogicOperationActionDefine : ActionDefine
    {
        public LogicOperationActionDefine() : base("LogicOperation")
        {
            inputs = new PortDefine[]
            {
                enterPortDefine,
                PortDefine.Value(typeof(bool), "value")
            };
            consts = new PortDefine[1]
            {
                PortDefine.Const(typeof(LogicOperator), "operator")
            };
            outputs = new PortDefine[]
            {
                exitPortDefine,
                PortDefine.Value(typeof(bool), "result")
            };
            isParams = true;
        }
        public override async Task<ControlOutput> run(Flow flow, Node node)
        {
            var opObj = node.getConst("operator");
            LogicOperator op;

            if (opObj == null)
                // 因为LogicOperator的默认值是not（为0），所以在常量值为null时，将操作符默认值设置为not。
                op = LogicOperator.not;
            else if (opObj is LogicOperator lgcOp)
                op = lgcOp;
            else if (opObj is int enumValue)
                op = (LogicOperator)enumValue;
            else
                op = LogicOperator.and;

            var argPorts = node.getParamInputPorts("value");
            var values = new bool[argPorts.Length];
            for (int i = 0; i < argPorts.Length; i++)
            {
                values[i] = await flow.getValue<bool>(argPorts[i]);
            }

            bool result;
            if (values != null && values.Length > 0)
            {
                switch (op)
                {
                    case LogicOperator.not:
                        result = values.Length > 0 && values[0] is bool b ? !b : true;
                        break;
                    case LogicOperator.and:
                        result = true;
                        foreach (var value in values)
                        {
                            if (!value)
                            {
                                result = false;
                                break;
                            }
                        }
                        break;
                    case LogicOperator.or:
                        result = false;
                        foreach (var value in values)
                        {
                            if (value)
                            {
                                result = true;
                                break;
                            }
                        }
                        break;
                    default:
                        throw new InvalidOperationException("未知的操作符" + op);
                }
                flow.setValue(node.getOutputPort<ValueOutput>("result"), result);
            }
            else
                throw new ArgumentException("传入参数无法转化为bool[]");
            return node.getOutputPort<ControlOutput>(exitPortName);
        }
        public PortDefine[] inputs { get; }
        public PortDefine[] consts { get; }
        public PortDefine[] outputs { get; }

        public override IEnumerable<PortDefine> inputDefines => inputs;

        public override IEnumerable<PortDefine> constDefines => consts;

        public override IEnumerable<PortDefine> outputDefines => outputs;
    }
    public enum LogicOperator
    {
        not,
        and,
        or
    }
}