using System;
using System.Linq;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;
namespace TouhouCardEngine
{
    //public class BuiltinActionDefine : ActionDefine
    //{
    //    #region 方法
    //    public BuiltinActionDefine(Func<IGame, ICard, IBuff, IEventArg, object[], object[], Task<object[]>> action) : base()
    //    {
    //        this.action = action;
    //        consts = new ValueDefine[0];
    //    }
    //    public override Task<object[]> execute(IGame game, ICard card, IBuff buff, IEventArg eventArg, object[] args, object[] constValues)
    //    {
    //        return action(game, card, buff, eventArg, args, constValues);
    //    }
    //    #endregion
    //    Func<IGame, ICard, IBuff, IEventArg, object[], object[], Task<object[]>> action { get; }
    //    public override ValueDefine[] inputs { get; }
    //    public override ValueDefine[] consts { get; }
    //    public override ValueDefine[] outputs { get; }
    //}
    public class IntegerOperationActionDefine : ActionDefine
    {
        public IntegerOperationActionDefine() : base("IntegerOperation")
        {
            inputs = new ValueDefine[1]
            {
                new ValueDefine(typeof(int), "value", true, false)
            };
            consts = new ValueDefine[1]
            {
                new ValueDefine(typeof(IntegerOperator), "operator", false, false)
            };
            outputs = new ValueDefine[1]
            {
                new ValueDefine(typeof(int), "result", false, false)
            };
        }
        public override Task<object[]> execute(IGame game, ICard card, IBuff buff, IEventArg eventArg, Scope scope, object[] args, object[] constValues)
        {
            IntegerOperator op = (IntegerOperator)constValues[0];
            switch (op)
            {
                case IntegerOperator.add:
                    return Task.FromResult(new object[] { ((int[])args[0]).Sum(a => a) });
                case IntegerOperator.sub:
                    int[] numbers = (int[])args[0];
                    int result = numbers[0];
                    for (int i = 1; i < numbers.Length; i++)
                    {
                        result -= numbers[i];
                    }
                    return Task.FromResult(new object[] { result });
                case IntegerOperator.mul:
                    numbers = (int[])args[0];
                    result = 1;
                    for (int i = 0; i < numbers.Length; i++)
                    {
                        result *= numbers[i];
                    }
                    return Task.FromResult(new object[] { result });
                case IntegerOperator.div:
                    numbers = (int[])args[0];
                    result = numbers[0];
                    for (int i = 1; i < numbers.Length; i++)
                    {
                        result /= numbers[i];
                    }
                    return Task.FromResult(new object[] { result });
                default:
                    throw new InvalidOperationException("未知的操作符" + op);
            }
        }
        public override ValueDefine[] inputs { get; }
        public override ValueDefine[] consts { get; }
        public override ValueDefine[] outputs { get; }
    }
    public enum IntegerOperator
    {
        add,
        sub,
        mul,
        div
    }
}