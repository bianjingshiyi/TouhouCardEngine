using System;
using System.Threading.Tasks;

namespace TouhouCardEngine.Interfaces
{
    public interface IEffect
    {
    }
    public interface IPassiveEffect : IEffect
    {
        string[] piles { get; }
        Task onEnable(IGame game, ICard card, IBuff buff);
        Task onDisable(IGame game, ICard card, IBuff buff);
    }
    public interface ITriggerEffect : IPassiveEffect
    {
        [Obsolete]
        string[] events { get; }
        string[] getEvents(ITriggerManager manager);
        bool checkCondition(IGame game, ICard card, object[] vars);
        bool checkTargets(IGame game, ICard card, object[] vars, object[] targets);
        Task execute(IGame game, ICard card, object[] vars, object[] targets);
    }
    public interface IActiveEffect : IEffect
    {
        bool checkCondition(IGame game, ICard card, object[] vars);
        Task execute(IGame game, ICard card, object[] vars, object[] targets);
    }
    public interface ITargetEffect : IActiveEffect
    {
        bool checkTargets(IGame game, ICard card, object[] vars, object[] targets);
    }
}
