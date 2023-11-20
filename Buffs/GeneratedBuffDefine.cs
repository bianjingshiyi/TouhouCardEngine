using System;
using System.Collections.Generic;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    [Serializable]
    public class GeneratedBuffDefine : BuffDefine
    {
        public GeneratedBuffDefine(int id, IEnumerable<PropModifier> modifiers = null, IEnumerable<GeneratedEffect> effects = null) : 
            base(id, modifiers)
        {
            if (effects != null)
                effectList.AddRange(effects);
        }
        public GeneratedBuffDefine(long cardPoolId, int id, IEnumerable<PropModifier> modifiers = null, IEnumerable<GeneratedEffect> effects = null) :
            base(id, modifiers)
        {
            this.cardPoolId = cardPoolId;
            if (effects != null)
                effectList.AddRange(effects);
        }

        public override IEffect[] getEffects()
        {
            return effectList.ToArray();
        }

        public List<GeneratedEffect> effectList = new List<GeneratedEffect>();
    }
    [Serializable]
    public class SerializableBuffDefine
    {
        #region 公有方法
        #region 构造方法
        public SerializableBuffDefine(GeneratedBuffDefine buffDefine)
        {
            if (buffDefine == null)
                throw new ArgumentNullException(nameof(buffDefine));
            id = buffDefine.id;
            propModifierList = buffDefine.propModifierList != null ? buffDefine.propModifierList : new List<PropModifier>();
            existLimitList = buffDefine.existLimitList != null ? buffDefine.existLimitList : new List<BuffExistLimitDefine>();
            effects = buffDefine.effectList != null ?
                buffDefine.effectList.ConvertAll(e => e?.Serialize()) :
                new List<SerializableEffect>();
        }
        #endregion
        public GeneratedBuffDefine toGeneratedBuffDefine(INodeDefiner definer)
        {
            GeneratedBuffDefine generatedBuffDefine = new GeneratedBuffDefine(id, propModifierList ?? new List<PropModifier>());
            generatedBuffDefine.existLimitList = existLimitList ?? new List<BuffExistLimitDefine>();
            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i] == null)
                    continue;
                try
                {
                    generatedBuffDefine.effectList.Add(effects[i].Deserialize(definer));
                }
                catch (Exception e)
                {
                    throw new FormatException($"反序列化增益定义{id}的效果{i}失败", e);
                }
            }
            return generatedBuffDefine;
        }
        #endregion
        #region 属性字段
        public int id;
        public List<PropModifier> propModifierList;
        public List<BuffExistLimitDefine> existLimitList;
        public List<SerializableEffect> effects;
        [Obsolete]
        public List<SerializableGeneratedEffect> effectList;
        #endregion
    }
}