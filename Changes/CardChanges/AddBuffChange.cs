namespace TouhouCardEngine.Histories
{
    public class AddBuffChange : CardChange
    {
        public Buff buff;
        public AddBuffChange(IChangeableCard target, Buff buff) : base(target)
        {
            this.buff = buff.clone();
        }

        public override void applyFor(IChangeableCard trackable)
        {
            trackable.addBuff(buff.clone());
        }
        public override void revertFor(IChangeableCard trackable)
        {
            trackable.removeBuff(buff.instanceID);
        }
    }
}
