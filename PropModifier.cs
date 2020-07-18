using TouhouCardEngine.Interfaces;
namespace TouhouCardEngine
{
    public abstract class PropModifier
    {
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
        public abstract PropModifier clone();
    }
    public abstract class PropModifier<T> : PropModifier
    {
        public abstract string propName { get; }
        public abstract T calc(IGame game, Card card, T value);
    }
}