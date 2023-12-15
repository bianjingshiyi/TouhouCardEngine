using System.Threading.Tasks;

namespace TouhouCardEngine.Interfaces
{
    public interface IEffect
    {
        Task onEnable(CardEngine game, Card card, Buff buff);
        Task onDisable(CardEngine game, Card card, Buff buff);
        bool checkCondition(EffectEnv env);
        Task execute(EffectEnv env);
    }
    public interface IPassiveEffect : IEffect
    {
    }
    public interface IEventEffect : IPassiveEffect
    {
        Task onTrigger(EffectEnv env);
    }
    public interface IActiveEffect : IEffect
    {
    }
    /// <summary>
    /// 拥有牌堆生效区域的效果。
    /// </summary>
    public interface IPileRangedEffect : IPassiveEffect
    {
        string[] getPiles();
    }
    /// <summary>
    /// 可以使用<see cref="EffectTriggerEventDefine"/>触发的效果。
    /// </summary>
    public interface ITriggerEventEffect : IEffect
    {
        Task runEffect(EffectEnv env, string portName);
    }
}
