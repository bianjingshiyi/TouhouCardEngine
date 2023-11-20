namespace TouhouCardEngine.Histories
{
    public class BuffPropChange : CardChange
    {
        public int buffId;
        public string propName;
        public object beforeValue;
        public object value;
        public BuffPropChange(IChangeableCard target, int buffId, string propName, object beforeValue, object value) : base(target)
        {
            this.buffId = buffId;
            this.propName = propName;
            this.beforeValue = beforeValue;
            this.value = value;
        }

        public override void applyFor(IChangeableCard target)
        {
            var buff = target.getBuff(buffId);
            buff.setProp(propName, value);
        }
        public override void revertFor(IChangeableCard target)
        {
            var buff = target.getBuff(buffId);
            buff.setProp(propName, beforeValue);
        }
    }
}
