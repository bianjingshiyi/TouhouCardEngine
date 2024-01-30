using System.Collections.Generic;
using System.Linq;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public abstract class BuffDefine
    {
        #region 公有方法

        #region 构造方法
        public BuffDefine(int id, IEnumerable<PropModifier> propModifiers = null)
        {
            this.id = id;
            if (propModifiers != null)
                propModifierList.AddRange(propModifiers);
        }
        public BuffDefine()
        {
        }
        #endregion

        #region 属性
        public bool hasProp(string name)
        {
            return propDict.ContainsKey(name);
        }
        public object getProp(string name)
        {
            if (propDict.TryGetValue(name, out var value))
                return value;
            return null;
        }
        public void setProp(string name, object value)
        {
            propDict[name] = value;
        }
        public bool removeProp(string name)
        {
            return propDict.Remove(name);
        }
        public T getProp<T>(string name)
        {
            if (getProp(name) is T tValue)
                return tValue;
            return default;
        }
        public string[] getPropNames()
        {
            return propDict.Keys.ToArray();
        }
        #endregion

        public abstract Effect[] getEffects();
        public override string ToString()
        {
            return string.Intern(string.Format("Buff<{0}>", id));
        }
        public DefineReference getDefineRef()
        {
            return new DefineReference(cardPoolId, id);
        }
        #endregion

        #region 属性字段
        public long cardPoolId;
        public int id { get; set; }
        public List<PropModifier> propModifierList = new List<PropModifier>();
        public List<BuffExistLimitDefine> existLimitList = new List<BuffExistLimitDefine>();
        private Dictionary<string, object> propDict = new Dictionary<string, object>();
        #endregion
    }
}