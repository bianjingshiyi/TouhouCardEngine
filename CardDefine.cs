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
        public abstract IEffect[] effects { get; }
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
        public IEffect getEffectOn<T>(ITriggerManager manager) where T : IEventArg
        {
            return effects.FirstOrDefault(e => e.getEvents(manager).Contains(manager.getName<T>()));
        }
        public IEffect getEffectAfter<T>(ITriggerManager manager) where T : IEventArg
        {
            return effects.FirstOrDefault(e => e.getEvents(manager).Contains(manager.getNameAfter<T>()));
        }
    }
}