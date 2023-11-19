namespace TouhouCardEngine.Histories
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