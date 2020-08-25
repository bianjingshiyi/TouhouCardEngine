using TouhouCardEngine.Interfaces;
using System.Threading.Tasks;
namespace TouhouCardEngine
{
    public class PropHook<T> : PropModifier<T>
    {
        public Card hookCard { get; }
        public string hookPropName { get; }
        public delegate T CalcDelegate(IGame game, Card card, T value);
        CalcDelegate onCalc { get; }
        public PropHook(string targetPropName, string hookPropName, Card hookCard = null, CalcDelegate onCalc = null) : base(targetPropName, default)
        {
            propName = targetPropName;
            this.hookCard = hookCard;
            this.hookPropName = hookPropName;
            this.onCalc = onCalc;
        }
        PropHook(PropHook<T> origin) : base(origin)
        {
            propName = origin.propName;
            hookPropName = origin.hookPropName;
        }
        Trigger<ISetPropEventArg> trigger { get; set; }
        public override Task afterAdd(IGame game, Card card)
        {
            if (trigger != null)
            {
                game.triggers.removeAfter(trigger);
                trigger = null;
            }
            trigger = new Trigger<ISetPropEventArg>(arg =>
            {
                if (arg.card == (hookCard != null ? hookCard : card) && arg.propName == hookPropName)
                    return setValue(game, card, (T)arg.value);
                return Task.CompletedTask;
            });
            game.triggers.registerAfter(trigger);
            return base.afterAdd(game, card);
        }
        public override T calc(IGame game, Card card, T value)
        {
            if (onCalc != null)
                return onCalc(game, card, value);
            return this.value;
        }
        public override Task afterRemove(IGame game, Card card)
        {
            if (trigger != null)
            {
                game.triggers.removeAfter(trigger);
                trigger = null;
            }
            return base.afterRemove(game, card);
        }
        public override PropModifier clone()
        {
            return new PropHook<T>(this);
        }
    }
}