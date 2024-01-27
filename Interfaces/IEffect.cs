using System.Threading.Tasks;

namespace TouhouCardEngine.Interfaces
{
    public interface IPassiveEffect
    {
    }
    public interface IEventEffect : IPassiveEffect
    {
        Task onTrigger(EffectEnv env);
    }
    public interface IActiveEffect
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
