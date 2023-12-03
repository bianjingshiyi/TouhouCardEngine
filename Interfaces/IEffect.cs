using System.Threading.Tasks;

namespace TouhouCardEngine.Interfaces
{
    public interface IEffect
    {
        Task onEnable(IGame game, ICard card, IBuff buff);
        Task onDisable(IGame game, ICard card, IBuff buff);
        bool checkCondition(IGame game, ICard card, IBuff buff, IEventArg arg);
        Task execute(IGame game, ICard card, IBuff buff, IEventArg arg);
    }
    public interface IPassiveEffect : IEffect
    {
    }
    public interface IEventEffect : IPassiveEffect
    {
        Task onTrigger(IGame game, ICard card, IBuff buff, IEventArg arg);
    }
    public interface IActiveEffect : IEffect
    {
    }
    /// <summary>
    /// 拥有牌堆生效区域的效果。
    /// </summary>
    public interface IPileRangedEffect : IPassiveEffect
    {
        string[] piles { get; }
    }
    /// <summary>
    /// 可以使用<see cref="EffectTriggerEventDefine"/>触发的效果。
    /// </summary>
    public interface ITriggerEventEffect : IEffect
    {
        Task runEffect(CardEngine game, Card card, Buff buff, EventArg arg, string portName);
    }
}
