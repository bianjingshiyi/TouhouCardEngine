namespace TouhouCardEngine.Histories
{
    public class BuffPropVisibilityChange : CardChange
    {
        public int buffId;
        public string propName;
        public Player player;
        public bool invisible;
        public BuffPropVisibilityChange(IChangeableCard target, int buffId, string propName, Player player, bool invisible) : base(target)
        {
            this.buffId = buffId;
            this.propName = propName;
            this.player = player;
            this.invisible = invisible;
        }

        public override void applyFor(IChangeableCard trackable)
        {
            var buff = trackable.getBuff(buffId);
            buff.setPropInvisible(propName, player, invisible);
        }
        public override void revertFor(IChangeableCard trackable)
        {
            var buff = trackable.getBuff(buffId);
            buff.setPropInvisible(propName, player, !invisible);
        }
    }
}
