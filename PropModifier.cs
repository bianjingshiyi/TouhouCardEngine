using TouhouCardEngine.Interfaces;
namespace TouhouCardEngine
{
    public abstract class PropModifier
    {
        public abstract string propName { get; }
        public virtual bool checkCondition(IGame game, Card card)
        {
            return true;
        }
        public virtual void beforeAdd(IGame game, Card card)
        {
        }
        public virtual void afterAdd(IGame game, Card card)
        {
        }
        public virtual void beforeRemove(IGame game, Card card)
        {
        }
        public virtual void afterRemove(IGame game, Card card)
        {
        }
        public abstract object calc(IGame game, Card card, object value);
        public abstract PropModifier clone();
    }
    public abstract class PropModifier<T> : PropModifier
    {
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