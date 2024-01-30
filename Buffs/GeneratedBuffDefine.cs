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

        public override Effect[] getEffects()
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
            propModifierList = buffDefine.propModifierList != null 
                ? buffDefine.propModifierList.ConvertAll(p => p.serialize()) 
                : new List<SerializablePropModifier>();
            existLimitList = buffDefine.existLimitList != null 
                ? buffDefine.existLimitList.ConvertAll(p => new SerializableBuffExistLimitDefine(p)) 
                : new List<SerializableBuffExistLimitDefine>();
            effects = buffDefine.effectList != null ?
                buffDefine.effectList.ConvertAll(e => e?.Serialize()) :
                new List<SerializableEffect>();
            props = new Dictionary<string, object>();
            foreach (var propName in buffDefine.getPropNames())
            {
                props.Add(propName, buffDefine.getProp(propName));
            }
        }
        #endregion
        public GeneratedBuffDefine toGeneratedBuffDefine(INodeDefiner definer)
        {
            GeneratedBuffDefine generatedBuffDefine = new GeneratedBuffDefine(id, propModifierList.ConvertAll(p => p.deserialize()) ?? new List<PropModifier>());
            generatedBuffDefine.existLimitList = existLimitList?.ConvertAll(e => e.toDefine()) ?? new List<BuffExistLimitDefine>();
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
            if (props != null)
            {
                foreach (var pair in props)
                {
                    generatedBuffDefine.setProp(pair.Key, pair.Value);
                }
            }
            return generatedBuffDefine;
        }
        #endregion
        #region 属性字段
        public int id;
        public List<SerializablePropModifier> propModifierList;
        public List<SerializableBuffExistLimitDefine> existLimitList;
        public List<SerializableEffect> effects;
        public Dictionary<string, object> props;
        [Obsolete]
        public List<SerializableGeneratedEffect> effectList;
        #endregion
    }
}