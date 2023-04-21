using System;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace TouhouCardEngine
{
    [Serializable]
    public class GeneratedBuffDefine : BuffDefine
    {
        #region 公有方法
        #region 构造方法
        public GeneratedBuffDefine(int id, IEnumerable<PropModifier> propModifiers = null, IEnumerable<GeneratedEffect> effects = null)
        {
            _id = id;
            if (propModifiers != null)
                propModifierList.AddRange(propModifiers);
            if (effects != null)
                effectList.AddRange(effects);
        }
        public GeneratedBuffDefine()
        {
        }
        #endregion
        public override async Task onEnable(CardEngine game, Card card, Buff buff)
        {
            for (int i = 0; i < effectList.Count; i++)
            {
                await effectList[i].onEnable(game, card, buff);
            }
        }
        public override async Task onDisable(CardEngine game, Card card, Buff buff)
        {
            for (int i = 0; i < effectList.Count; i++)
            {
                await effectList[i].onEnable(game, card, buff);
            }
        }
        public void setId(int id)
        {
            _id = id;
        }
        public override int id => _id;
        public override string ToString()
        {
            return string.Intern(string.Format("Buff<{0}>", id));
        }

        #endregion
        #region 属性字段
        public List<PropModifier> propModifierList = new List<PropModifier>();
        public List<GeneratedEffect> effectList = new List<GeneratedEffect>();
        private int _id;
        #endregion
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
            effects = buffDefine.effectList != null ?
                buffDefine.effectList.ConvertAll(e => e?.Serialize()) :
                new List<SerializableEffect>();
        }
        #endregion
        public GeneratedBuffDefine toGeneratedBuffDefine(ActionDefineFinder defineFinder, EventTypeInfoFinder eventFinder, TypeFinder typeFinder = null)
        {
            GeneratedBuffDefine generatedBuffDefine = new GeneratedBuffDefine();
            generatedBuffDefine.setId(id);
            generatedBuffDefine.propModifierList = propModifierList;
            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i] == null)
                    continue;
                try
                {
                    generatedBuffDefine.effectList.Add(effects[i].Deserialize(defineFinder, eventFinder));
                }
                catch (Exception e)
                {
                    throw new FormatException("反序列化增益定义" + id + "的效果" + i + "失败", e);
                }
            }
            return generatedBuffDefine;
        }
        #endregion
        #region 属性字段
        public int id;
        public List<PropModifier> propModifierList;
        public List<SerializableEffect> effects;
        [Obsolete]
        public List<SerializableGeneratedEffect> effectList;
        #endregion
    }
}