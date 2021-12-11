using TouhouCardEngine.Interfaces;
using System.Threading.Tasks;
namespace TouhouCardEngine
{
    public abstract class CExpr<T>
    {
        public abstract T getValue(IGame game, IPlayer player, ICard card, IBuff buff);
    }
    public class CConst<T> : CExpr<T>
    {
        T value { get; }
        public CConst(T value)
        {
            this.value = value;
        }
        public override T getValue(IGame game, IPlayer player, ICard card, IBuff buff)
        {
            return value;
        }
    }
    public class PropHook<T> : PropModifier<T>
    {
        public CExpr<Card> hookCard { get; }
        public string hookPropName { get; }
        public delegate T CalcDelegate(IGame game, Card card, T originValue, T value);
        CalcDelegate onCalc { get; }
        public PropHook(string targetPropName, string hookPropName, Card hookCard = null, CalcDelegate onCalc = null)
        {
            propertyName = targetPropName;
            value = default;
            this.hookCard = new CConst<Card>(hookCard);
            this.hookPropName = hookPropName;
            this.onCalc = onCalc;
        }
        public PropHook(string targetPropName, string hookPropName, CExpr<Card> hookCard, CalcDelegate onCalc = null)
        {
            propertyName = targetPropName;
            value = default;
            this.hookCard = hookCard;
            this.hookPropName = hookPropName;
            this.onCalc = onCalc;
        }
        PropHook(PropHook<T> origin)
        {
            propertyName = origin.getPropName();
            value = origin.value;
            hookPropName = origin.hookPropName;
        }
        Trigger<IPropChangeEventArg> trigger { get; set; }
        public override async Task beforeAdd(IGame game, Card card)
        {
            Card hookedCard = hookCard.getValue(game, null, card, null) ?? card;
            await setValue(game, card, hookedCard.getProp<T>(game, hookPropName));
            await base.beforeAdd(game, card);
        }
        public override Task afterAdd(IGame game, Card card)
        {
            if (trigger != null)
            {
                game.triggers.removeAfter(trigger);
                trigger = null;
            }
            trigger = new Trigger<IPropChangeEventArg>(arg =>
            {
                Card hookedCard = hookCard.getValue(game, null, card, null) ?? card;
                if (arg.card == hookCard && arg.propName == hookPropName)
                    return setValue(game, card, (T)arg.value);
                return Task.CompletedTask;
            });
            game.triggers.registerAfter(trigger);
            return base.afterAdd(game, card);
        }
        public override T calc(IGame game, Card card, T value)
        {
            if (onCalc != null)
                return onCalc(game, card, value, this.value);
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