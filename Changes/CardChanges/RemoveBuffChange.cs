namespace TouhouCardEngine.Histories
{
    public class RemoveBuffChange : CardChange
    {
        public Buff buff;
        public RemoveBuffChange(IChangeableCard target, Buff buff) : base(target)
        {
            this.buff = buff.clone();
        }

        public override void applyFor(IChangeableCard trackable)
        {
            trackable.removeBuff(buff.instanceID);
        }
        public override void revertFor(IChangeableCard trackable)
        {
            trackable.addBuff(buff.clone());
        }
    }
}
