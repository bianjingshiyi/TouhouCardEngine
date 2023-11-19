namespace TouhouCardEngine.Histories
{
    public class BuffPropChange : BuffChange
    {
        public string propName;
        public object beforeValue;
        public object value;
        public BuffPropChange(IChangeableBuff target, string propName, object beforeValue, object value) : base(target)
        {
            this.propName = propName;
            this.beforeValue = beforeValue;
            this.value = value;
        }

        public override void applyFor(IChangeableBuff trackable)
        {
            trackable.setProp(propName, value);
        }
        public override void revertFor(IChangeableBuff trackable)
        {
            trackable.setProp(propName, beforeValue);
        }
    }
}
