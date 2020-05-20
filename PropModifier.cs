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
    }
    public abstract class PropModifier<T> : PropModifier
    {
        public abstract string propName { get; }
        public abstract T calc(Card card, T value);
    }
    public class IntModifier : PropModifier
    {
        public string propName { get; }
        public enum Type
        {
            add,
            set,
            mul,
            div
        }
        public Type type { get; }
        public int value { get; }
        public IntModifier(string propName, int value)
        {
            this.propName = propName;
            type = Type.add;
            this.value = value;
        }
        int oldValue { get; set; }
        public override void beforeAdd(Card card)
        {
        }
        public override void afterAdd(Card card)
        {
            int value = card.getProp<int>(propName);
            oldValue = value;
            switch (type)
            {
                default:
                    value += this.value;
                    break;
            }
            card.setProp(propName, value);
        }
        public override void beforeRemove(Card card)
        {
            card.setProp(propName, oldValue);
        }
        public override void afterRemove(Card card)
        {
        }
    }
}