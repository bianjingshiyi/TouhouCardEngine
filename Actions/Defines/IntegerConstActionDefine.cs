using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;
namespace TouhouCardEngine
{
    public class IntegerConstActionDefine : ActionDefine
    {
        public IntegerConstActionDefine() : base("IntegerConst")
        {
            inputs = new ValueDefine[0];
            consts = new ValueDefine[1]
            {
                new ValueDefine(typeof(int), "value", false, false)
            };
            outputs = new ValueDefine[1]
            {
                new ValueDefine(typeof(int), "value", false, false)
            };
        }
        public override Task<object[]> execute(IGame game, ICard card, IBuff buff, IEventArg eventArg, Scope scope, object[] args, object[] constValues)
        {
            return Task.FromResult(constValues);
        }
        public override ValueDefine[] inputs { get; }
        public override ValueDefine[] consts { get; }
        public override ValueDefine[] outputs { get; }
    }
}