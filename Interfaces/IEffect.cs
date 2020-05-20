using System;
using System.Threading.Tasks;

namespace TouhouCardEngine.Interfaces
{
    public interface IEffect
    {
        string[] piles { get; }
        void register(IGame game, ICard card);
        void unregister(IGame game, ICard card);
    }
    public interface ITriggerEffect : IEffect
    {
        [Obsolete]
        string[] events { get; }
        string[] getEvents(ITriggerManager manager);
        bool checkCondition(IGame game, ICard card, object[] vars);
        Task execute(IGame game, ICard card, object[] vars, object[] targets);
    }
    public interface IActiveEffect : ITriggerEffect
    {
        bool checkTarget(IGame game, ICard card, object[] vars, object[] targets);
    }
}
