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
            CompareOperator op;
            if (constValues == null || constValues.Length < 1)
                op = CompareOperator.equals;
            else if (constValues[0] is CompareOperator cmpOp)
                op = cmpOp;
            else if (constValues[0] is int enumValue)
                op = (CompareOperator)enumValue;
            else
                op = CompareOperator.equals;
            switch (op)
            {
                case CompareOperator.equals:
                    return Task.FromResult(new object[] { args[0] != null ? args[0].Equals(args[1]) : args[1] == null });
                case CompareOperator.unequals:
                    return Task.FromResult(new object[] { args[0] != null ? !args[0].Equals(args[1]) : args[1] != null });
                case CompareOperator.greater:
                    {
                        return Task.FromResult(new object[]
                        {
                            args[0] is IComparable cmp1 && args[1] is IComparable cmp2 ?
                                cmp1.CompareTo(cmp2) > 0 :
                                false
                        });
                    }
                case CompareOperator.greaterEquals:
                    {
                        return Task.FromResult(new object[]
                        {
                            args[0] is IComparable cmp1 && args[1] is IComparable cmp2 ?
                                cmp1.CompareTo(cmp2) >= 0 :
                                false
                        });
                    }
                case CompareOperator.less:
                    {
                        return Task.FromResult(new object[]
                        {
                            args[0] is IComparable cmp1 && args[1] is IComparable cmp2 ?
                                cmp1.CompareTo(cmp2) < 0 :
                                false
                        });
                    }
                case CompareOperator.lessEquals:
                    {
                        return Task.FromResult(new object[]
                        {
                            args[0] is IComparable cmp1 && args[1] is IComparable cmp2 ?
                                cmp1.CompareTo(cmp2) <= 0 :
                                false
                        });
                    }
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
        unequals,
        greater,
        greaterEquals,
        less,
        lessEquals
    }
}