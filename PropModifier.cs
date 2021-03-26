using TouhouCardEngine.Interfaces;
using System.Threading.Tasks;
namespace TouhouCardEngine
{
    public abstract class PropModifier : IPropModifier
    {
        public abstract string propName { get; protected set; }
        public virtual bool checkCondition(IGame game, Card card)
        {
            return true;
        }
        public virtual Task beforeAdd(IGame game, Card card)
        {
            return Task.CompletedTask;
        }
        public virtual Task afterAdd(IGame game, Card card)
        {
            return Task.CompletedTask;
        }
        public virtual Task beforeRemove(IGame game, Card card)
        {
            return Task.CompletedTask;
        }
        public virtual Task afterRemove(IGame game, Card card)
        {
            return Task.CompletedTask;
        }
        public abstract object calc(IGame game, Card card, object value);
        public abstract PropModifier clone();
    }
    public abstract class PropModifier<T> : PropModifier
    {
        public override string propName { get; protected set; }
        public T value { get; protected set; }
        public PropModifier(string propName, T value)
        {
            this.propName = propName;
            this.value = value;
        }
        protected PropModifier(PropModifier<T> origin)
        {
            propName = origin.propName;
            value = origin.value;
        }
        /// <summary>
        /// 设置修改器的修改值。
        /// </summary>
        /// <param name="value"></param>
        public virtual Task setValue(IGame game, Card card, T value)
        {
            if (Equals(this.value, value))
            {
                return Task.CompletedTask;
            }
            object beforeValue = card.getProp(game, propName);
            this.value = value;
            return game.triggers.doEvent(new Card.PropChangeEventArg()
            {
                game = game,
                card = card,
                propName = propName,
                beforeValue = beforeValue,
                value = card.getProp(game, propName)
            },arg => Task.CompletedTask);
        }
        public sealed override object calc(IGame game, Card card, object value)
        {
            if (value is T t)
                return calc(game, card, t);
            else
                return value;
        }
        public abstract T calc(IGame game, Card card, T value);
    }
}