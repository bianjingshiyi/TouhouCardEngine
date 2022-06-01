using System;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace TouhouCardEngine
{
    [Serializable]
    public class GeneratedBuffDefine : BuffDefine
    {
        #region 公有方法
        public GeneratedBuffDefine(int id, IEnumerable<PropModifier> propModifiers = null, IEnumerable<GeneratedEffect> effects = null)
        {
            this.id = id;
            if (propModifiers != null)
                propModifierList.AddRange(propModifiers);
            if (effects != null)
                effectList.AddRange(effects);
        }
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
        public override int getId()
        {
            return id;
        }
        public override string ToString()
        {
            return string.Intern(string.Format("Buff<{0}>", id));
        }

        #endregion
        #region 属性字段
        public int id = 0;
        public List<PropModifier> propModifierList = new List<PropModifier>();
        public List<GeneratedEffect> effectList = new List<GeneratedEffect>();
        #endregion
    }
    [Serializable]
    public class SerializableBuffDefine
    {
        #region 公有方法
        #region 构造方法
        public SerializableBuffDefine(GeneratedBuffDefine buffDefine)
        {
            id = buffDefine.id;
            propModifierList = buffDefine.propModifierList;
            effectList = buffDefine.effectList.ConvertAll(e => new SerializableEffect(e));
        }
        #endregion
        public GeneratedBuffDefine toGeneratedBuffDefine()
        {
            return new GeneratedBuffDefine(id, propModifierList, effectList.ConvertAll(e => e.toGeneratedEffect()));
        }
        #endregion
        #region 属性字段
        public int id;
        public List<PropModifier> propModifierList;
        public List<SerializableEffect> effectList;
        #endregion
    }
}