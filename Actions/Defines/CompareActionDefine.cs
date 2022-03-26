using System;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;
namespace TouhouCardEngine
{
    public class CompareActionDefine : ActionDefine
    {
        public CompareActionDefine() : base("Compare")
        {
            inputs = new ValueDefine[2]
            {
                new ValueDefine(typeof(object), "A", false, false),
                new ValueDefine(typeof(object), "B", false, false)
            };
            consts = new ValueDefine[1]
            {
                new ValueDefine(typeof(CompareOperator), "operator", false, false)
            };
            outputs = new ValueDefine[1]
            {
                new ValueDefine(typeof(bool), "result", false, false)
            };
        }
        public override Task<object[]> execute(IGame game, ICard card, IBuff buff, IEventArg eventArg, Scope scope, object[] args, object[] constValues)
        {
            CompareOperator op = (CompareOperator)constValues[0];
            switch (op)
            {
                case CompareOperator.equals:
                    return Task.FromResult(new object[] { args[0] == args[1] });
                case CompareOperator.unequals:
                    return Task.FromResult(new object[] { args[0] != args[1] });
                default:
                    throw new InvalidOperationException("未知的操作符" + op);
            }
        }
        public override ValueDefine[] inputs { get; }
        public override ValueDefine[] consts { get; }
        public override ValueDefine[] outputs { get; }
    }
    public enum CompareOperator
    {
        equals,
        unequals
    }
}