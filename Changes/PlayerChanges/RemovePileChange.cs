using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine.Histories
{
    public class RemovePileChange : PlayerChange
    {
        private Pile pile;
        public RemovePileChange(IChangeablePlayer target, Pile pile) : base(target)
        {
            this.pile = pile;
        }

        public override void applyFor(IChangeablePlayer changeable)
        {
            changeable.removePile(pile);
        }

        public override void revertFor(IChangeablePlayer changeable)
        {
            changeable.addPile(pile);
        }
    }
}
