namespace TouhouCardEngine.Histories
{
    public class RemoveBuffChange : CardChange
    {
        public Buff buff;
        public RemoveBuffChange(IChangeableCard target, Buff buff) : base(target)
        {
            this.buff = buff;
        }

        public override void applyFor(IChangeableCard trackable)
        {
            trackable.removeBuff(buff);
        }
        public override void revertFor(IChangeableCard trackable)
        {
            trackable.addBuff(buff);
        }
    }
}
