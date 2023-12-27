namespace TouhouCardEngine.Histories
{
    public class CardPropVisibilityChange : CardChange
    {
        public string propName;
        public Player player;
        public bool invisible;
        public CardPropVisibilityChange(IChangeableCard target, string propName, Player player, bool invisible) : base(target)
        {
            this.propName = propName;
            this.player = player;
            this.invisible = invisible;
        }

        public override void applyFor(IChangeableCard trackable)
        {
            trackable.setPropInvisible(propName, player, invisible);
        }
        public override void revertFor(IChangeableCard trackable)
        {
            trackable.setPropInvisible(propName, player, !invisible);
        }
    }
}
