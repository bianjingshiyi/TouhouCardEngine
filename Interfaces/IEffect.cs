using System.Threading.Tasks;

namespace TouhouCardEngine.Interfaces
{
    public interface IEffect
    {
        Task onEnable(IGame game, ICard card, IBuff buff);
        Task onDisable(IGame game, ICard card, IBuff buff);
    }
    public interface IPassiveEffect : IEffect
    {
    }
    public interface IEventEffect : IPassiveEffect
    {
        bool checkCondition(IGame game, ICard card, IBuff buff, IEventArg arg);
        Task execute(IGame game, ICard card, IBuff buff, IEventArg arg);
    }
    public interface IActiveEffect : IEffect
    {
        bool checkCondition(IGame game, ICard card, IBuff buff, IEventArg eventArg);
        Task execute(IGame game, ICard card, IBuff buff, IEventArg eventArg);
    }
    public interface IPileRangedEffect : IPassiveEffect
    {
        string[] piles { get; }
    }
}
