using System.Threading.Tasks;

namespace TouhouCardEngine.Interfaces
{
    public interface IEffect
    {
        DefineReference cardDefineRef { get; set; }
        DefineReference buffDefineRef { get; set; }
        Task enable(CardEngine game, Card card, Buff buff);
        Task disable(CardEngine game, Card card, Buff buff);
        Task onEnable(EffectEnv env);
        Task onDisable(EffectEnv env);
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
}
