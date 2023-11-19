using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine.Histories
{
    public class AddPileChange : PlayerChange
    {
        private Pile pile;
        public AddPileChange(IChangeablePlayer target, Pile pile) : base(target)
        {
            this.pile = pile;
        }

        public override void applyFor(IChangeablePlayer changeable)
        {
            changeable.addPile(pile);
        }

        public override void revertFor(IChangeablePlayer changeable)
        {
            changeable.removePile(pile);
        }
    }
}
