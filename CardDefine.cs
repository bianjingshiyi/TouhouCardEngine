using System.Collections.Generic;
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
        public virtual int id { get; set; }
        public virtual string type { get; set; }
        public virtual IEffect[] effects { get; set; } = new IEffect[0];
        public object this[string propName]
        {
            get { return getProp<object>(propName); }
        }
        Dictionary<string, object> dicProp { get; } = new Dictionary<string, object>();
        public virtual void setProp<T>(string propName, T value)
        {
            if (propName == nameof(CardDefine.id))
                id = (int)(object)value;
            dicProp[propName] = value;
        }
        public virtual T getProp<T>(string propName)
        {
            if (dicProp.ContainsKey(propName) && dicProp[propName] is T)
                return (T)dicProp[propName];
            else
                return default;
        }
        public virtual string[] getPropNames()
        {
            return dicProp.Keys.ToArray();
        }
        public virtual bool hasProp(string propName)
        {
            return dicProp.ContainsKey(propName);
        }
        /// <summary>
        /// 将读取到的更新的卡牌数据合并到这个卡牌上来。
        /// </summary>
        /// <param name="newVersion"></param>
        public abstract void merge(CardDefine newVersion);
        public abstract string isUsable(CardEngine engine, Player player, Card card);
        public IActiveEffect getActiveEffect()
        {
            return effects.FirstOrDefault(e => e is IActiveEffect) as IActiveEffect;
        }
        public ITriggerEffect getEffectOn<T>(ITriggerManager manager) where T : IEventArg
        {
            return effects.FirstOrDefault(e => e is ITriggerEffect te && te.getEvents(manager).Contains(manager.getName<T>())) as ITriggerEffect;
        }
        public ITriggerEffect getEffectAfter<T>(ITriggerManager manager) where T : IEventArg
        {
            return effects.FirstOrDefault(e => e is ITriggerEffect te && te.getEvents(manager).Contains(manager.getNameAfter<T>())) as ITriggerEffect;
        }
        public override bool Equals(object obj)
        {
            if (obj is CardDefine other)
                return id == other.id;
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return id;
        }
    }

    /// <summary>
    /// 忽略这张卡
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class IgnoreCardDefineAttribute : System.Attribute
    {
    }
}