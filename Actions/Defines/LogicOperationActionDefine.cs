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
            LogicOperator op = constValues != null && constValues.Length > 0 && constValues[0] is LogicOperator lgcOp ? lgcOp : LogicOperator.and;
            switch (op)
            {
                case LogicOperator.not:
                    return Task.FromResult(new object[] { !(bool)args[0] });
                case LogicOperator.and:
                    foreach (var value in args.Cast<bool>())
                    {
                        if (value == false)
                            return Task.FromResult(new object[] { false });
                    }
                    return Task.FromResult(new object[] { true });
                case LogicOperator.or:
                    foreach (var value in args.Cast<bool>())
                    {
                        if (value == true)
                            return Task.FromResult(new object[] { true });
                    }
                    return Task.FromResult(new object[] { false });
                default:
                    throw new InvalidOperationException("未知的操作符" + op);
            }
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