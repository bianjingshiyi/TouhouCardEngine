using NitoriNetwork.Common;

namespace TouhouCardEngine
{
    public struct CardState
    {
        public Card card;
        public int stateIndex;

        public CardState(Card card, int stateIndex)
        {
            this.card = card;
            this.stateIndex = stateIndex;
        }
    }

}