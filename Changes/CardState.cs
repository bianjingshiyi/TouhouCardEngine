using System;

namespace TouhouCardEngine.Histories
{
    [Obsolete]
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