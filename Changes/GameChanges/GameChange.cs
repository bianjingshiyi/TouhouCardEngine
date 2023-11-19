using System;

namespace TouhouCardEngine.Histories
{
    public interface IChangeableGame : IChangeable
    {
        void setProp(string name, object value);
        void setRandomState(uint state);
        void setResponseRNGState(uint state);
        void addCard(Card card);
        void removeCard(int cardId);
        void setNextRandomInt(int[] results);
    }
    public abstract class GameChange : Change<IChangeableGame>
    {
        public GameChange(IChangeableGame target) : base(target) { }
    }
}
