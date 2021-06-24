using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using TouhouCardEngine.Interfaces;
namespace TouhouCardEngine
{
    /// <summary>
    /// 卡片定义，包含了一张卡的静态数据和效果逻辑。
    /// </summary>
    [Serializable]
    public class CardDefine : ICardDefine
    {
        #region 方法
        public CardDefine(int id, string type, Dictionary<string, object> props, GeneratedEffect[] effects)
        {
            _id = id;
            _type = type;
            if (props != null)
                _propDict = props;
            if (effects != null)
                _effectList.AddRange(effects);
        }
        public CardDefine(int id, string type) : this(id, type, null, null)
        {
        }
        /// <summary>
        /// 供序列化使用的默认构造器
        /// </summary>
        public CardDefine() : this(0, string.Empty, null, null)
        {
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
        public virtual void setProp<T>(string propName, T value)
        {
            if (propName == nameof(id))
                id = (int)(object)value;
            _propDict[propName] = value;
        }
        public virtual T getProp<T>(string propName)
        {
            if (_propDict.ContainsKey(propName) && _propDict[propName] is T)
                return (T)_propDict[propName];
            else
                return default;
        }
        public virtual string[] getPropNames()
        {
            return _propDict.Keys.ToArray();
        }
        public virtual bool hasProp(string propName)
        {
            return _propDict.ContainsKey(propName);
        }
        public object this[string propName]
        {
            get { return getProp<object>(propName); }
        }
        public virtual IEffect[] getEffects()
        {
            if (_effectList != null && _effectList.Count > 0)
                return _effectList.ToArray();
            return _runtimeEffects;
        }
        public virtual void setEffects(IEffect[] value)
        {
            if (value is GeneratedEffect[] generatedEffects)
            {
                _effectList.Clear();
                _effectList.AddRange(generatedEffects);
            }
            else
                _runtimeEffects = value;
        }
        public IActiveEffect getActiveEffect()
        {
            return getEffects().FirstOrDefault(e => e is IActiveEffect) as IActiveEffect;
        }
        public ITriggerEffect getEffectOn<T>(ITriggerManager manager) where T : IEventArg
        {
            return getEffects().FirstOrDefault(e => e is ITriggerEffect te && te.getEvents(manager).Contains(manager.getName<T>())) as ITriggerEffect;
        }
        public ITriggerEffect getEffectAfter<T>(ITriggerManager manager) where T : IEventArg
        {
            return getEffects().FirstOrDefault(e => e is ITriggerEffect te && te.getEvents(manager).Contains(manager.getNameAfter<T>())) as ITriggerEffect;
        }
        /// <summary>
        /// 将读取到的更新的卡牌数据合并到这个卡牌上来。
        /// </summary>
        /// <param name="newVersion"></param>
        public virtual void merge(CardDefine newVersion)
        {
            throw new NotImplementedException();
        }
        #endregion
        /// <summary>
        /// 卡片定义ID，这个ID应该是独特的并用于区分不同的卡片。
        /// </summary>
        [BsonIgnore]
        public virtual int id
        {
            get { return _id; }
            set { _id = value; }
        }
        [BsonElement]
        int _id;
        [BsonIgnore]
        public virtual string type
        {
            get { return _type; }
            set { _type = value; }
        }
        [BsonElement]
        string _type;
        [BsonElement]
        Dictionary<string, object> _propDict = new Dictionary<string, object>();
        [BsonElement]
        List<GeneratedEffect> _effectList = new List<GeneratedEffect>();
        [NonSerialized]
        IEffect[] _runtimeEffects;
    }
    /// <summary>
    /// 忽略这张卡
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class IgnoreCardDefineAttribute : Attribute
    {
    }
}