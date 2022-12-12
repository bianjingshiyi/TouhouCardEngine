using System;
using System.Linq;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;
namespace TouhouCardEngine
{
    public class LogicOperationActionDefine : ActionDefine
    {
        public LogicOperationActionDefine() : base("LogicOperation")
        {
            inputs = new ValueDefine[1]
            {
                new ValueDefine(typeof(bool), "value", true, false)
            };
            consts = new ValueDefine[1]
            {
                new ValueDefine(typeof(LogicOperator), "operator", false, false)
            };
            outputs = new ValueDefine[1]
            {
                new ValueDefine(typeof(bool), "result", false, false)
            };
        }
        public override Task<object[]> execute(IGame game, ICard card, IBuff buff, IEventArg eventArg, Scope scope, object[] args, object[] constValues)
        {
            LogicOperator op;
            if (constValues == null || constValues.Length < 1 || constValues[0] == null)
                op = LogicOperator.not;
            else if (constValues[0] is LogicOperator lgcOp)
                op = lgcOp;
            else if (constValues[0] is int enumValue)
                op = (LogicOperator)enumValue;
            else
                op = LogicOperator.and;
            if (args != null && args.Length > 0 && args[0] is bool[] values)
            {
                switch (op)
                {
                    case LogicOperator.not:
                        return Task.FromResult(new object[] { values.Length > 0 && values[0] is bool b ? !b : true });
                    case LogicOperator.and:
                        foreach (var value in values)
                        {
                            if (value == false)
                                return Task.FromResult(new object[] { false });
                        }
                        return Task.FromResult(new object[] { true });
                    case LogicOperator.or:
                        foreach (var value in values)
                        {
                            if (value == true)
                                return Task.FromResult(new object[] { true });
                        }
                        return Task.FromResult(new object[] { false });
                    default:
                        throw new InvalidOperationException("未知的操作符" + op);
                }
            }
            else
                throw new ArgumentException("传入参数无法转化为bool[]");
        }
        public override ValueDefine[] inputs { get; }
        public override ValueDefine[] consts { get; }
        public override ValueDefine[] outputs { get; }
    }
    public enum LogicOperator
    {
        not,
        and,
        or
    }
}