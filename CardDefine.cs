using System.Linq;
using TouhouCardEngine.Interfaces;
namespace TouhouCardEngine
{
    /// <summary>
    /// 卡片定义，包含了一张卡的静态数据和效果逻辑。
    /// </summary>
    public abstract class CardDefine : ICardDefine
    {
        /// <summary>
        /// 卡片定义ID，这个ID应该是独特的并用于区分不同的卡片。
        /// </summary>
        public abstract int id { get; set; }
        public abstract CardDefineType type { get; }
        public abstract Effect[] effects { get; }
        IEffect[] ICardDefine.effects
        {
            get { return effects; }
        }
        public object this[string propName]
        {
            get { return getProp<object>(propName); }
        }
        public virtual T getProp<T>(string propName)
        {
            if (propName == nameof(id))
                return (T)(object)id;
            else
                return default(T);
        }
        public abstract string isUsable(CardEngine engine, Player player, Card card);
        public Effect getEffectOn<T>() where T : IEventArg
        {
            return effects.FirstOrDefault(e => e.triggerTimes.Any(t => t is On<T>));
        }
        public Effect getEffectAfter<T>() where T : IEventArg
        {
            return effects.FirstOrDefault(e => e.triggerTimes.Any(t => t is After<T>));
        }
    }
}