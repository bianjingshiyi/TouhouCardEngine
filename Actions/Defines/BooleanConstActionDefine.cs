using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;
namespace TouhouCardEngine
{
    public class BooleanConstActionDefine : ActionDefine
    {
        public BooleanConstActionDefine() : base("BooleanConst")
        {
            inputs = new ValueDefine[0];
            consts = new ValueDefine[1]
            {
                new ValueDefine(typeof(bool), "value", false, false)
            };
            outputs = new ValueDefine[1]
            {
                new ValueDefine(typeof(bool), "value", false, false)
            };
        }
        public override Task<object[]> execute(IGame game, ICard card, IBuff buff, IEventArg eventArg, Scope scope, object[] args, object[] constValues)
        {
            if (constValues.Length > 0 && constValues[0] == null)
            {
                constValues[0] = false;
            }
            return Task.FromResult(constValues);
        }
        public override ValueDefine[] inputs { get; }
        public override ValueDefine[] consts { get; }
        public override ValueDefine[] outputs { get; }
    }
}