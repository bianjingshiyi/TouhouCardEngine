using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine.Histories
{
    public class CreateCardChange : GameChange
    {
        private Card card;
        public CreateCardChange(IChangeableGame target, Card card) : base(target)
        {
            this.card = card;
        }

        public override void applyFor(IChangeableGame changeable)
        {
            changeable.addCard(card);
        }

        public override void revertFor(IChangeableGame changeable)
        {
            changeable.removeCard(card.id);
        }
    }
}
