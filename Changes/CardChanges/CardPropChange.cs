namespace TouhouCardEngine.Histories
{
    public class CardPropChange : CardChange
    {
        public string propName;
        public object beforeValue;
        public object value;
        public CardPropChange(IChangeableCard target, string propName, object beforeValue, object value) : base(target)
        {
            this.propName = propName;
            this.beforeValue = beforeValue;
            this.value = value;
        }

        public override void applyFor(IChangeableCard trackable)
        {
            trackable.setProp(propName, value);
        }
        public override void revertFor(IChangeableCard trackable)
        {
            trackable.setProp(propName, beforeValue);
        }
    }
}