namespace TouhouCardEngine
{
    public abstract class PropModifier
    {
        public virtual void beforeAdd(Card card)
        {
        }
        public virtual void afterAdd(Card card)
        {
        }
        public virtual void beforeRemove(Card card)
        {
        }
        public virtual void afterRemove(Card card)
        {
        }
        public abstract PropModifier clone();
    }
    public abstract class PropModifier<T> : PropModifier
    {
        public abstract string propName { get; }
        public abstract T calc(Card card, T value);
    }
}