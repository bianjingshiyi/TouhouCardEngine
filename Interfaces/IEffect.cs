using System;
using System.Threading.Tasks;

namespace TouhouCardEngine.Interfaces
{
    public interface IEffect
    {
        [Obsolete]
        string[] events { get; }
        string[] getEvents(ITriggerManager manager);
        string[] piles { get; }
        bool checkCondition(IGame game, IPlayer player, ICard card, object[] vars);
        bool checkTarget(IGame game, IPlayer player, ICard card, object[] targets);
        Task execute(IGame game, IPlayer player, ICard card, object[] vars, object[] targets);
    }
}
