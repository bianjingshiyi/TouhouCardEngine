namespace TouhouCardEngine.Histories
{
    public class CardVisibilityChange : CardChange
    {
        public Player player;
        public bool visible;
        public CardVisibilityChange(IChangeableCard target, Player player, bool visible) : base(target)
        {
            this.player = player;
            this.visible = visible;
        }

        public override void applyFor(IChangeableCard trackable)
        {
            trackable.setVisible(player, visible);
        }
        public override void revertFor(IChangeableCard trackable)
        {
            trackable.setVisible(player, !visible);
        }
    }
}
