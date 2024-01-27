using System.Collections.Generic;
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
        #endregion
    }
}