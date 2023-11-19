namespace TouhouCardEngine.Histories
{
    public class CardMoveChange : CardChange
    {
        public Pile from;
        public int fromPos;
        public Pile to;
        public int toPos;
        public CardMoveChange(IChangeableCard target, Pile from, Pile to, int fromPos, int toPos) : base(target)
        {
            this.from = from;
            this.to = to;
            this.fromPos = fromPos;
            this.toPos = toPos;
        }

        public override void applyFor(IChangeableCard trackable)
        {
            trackable.moveTo(to, toPos);
        }
        public override void revertFor(IChangeableCard trackable)
        {
            trackable.moveTo(from, fromPos);
        }
    }
}
