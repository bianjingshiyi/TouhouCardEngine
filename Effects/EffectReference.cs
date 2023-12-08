using System;
using System.Linq;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public class EffectReference
    {
        public EffectReference(Buff buff, CardDefine cardDefine, int effectIndex)
        {
            this.buff = buff;
            this.cardDefine = cardDefine;
            this.effectIndex = effectIndex;
        }
        public IEffect getEffect()
        {
            if (effectIndex < 0)
                return null;
            if (cardDefine == null && buff == null)
                return null;
            return buff != null ? buff.getEffects()[effectIndex] : cardDefine.getEffects()[effectIndex];
        }
        public static EffectReference fromCardDefine(CardDefine cardDefine, IEffect effect)
        {
            var defineEffects = cardDefine.getEffects();
            var effectIndex = Array.IndexOf(defineEffects, effect);
            return new EffectReference(null, cardDefine, effectIndex);
        }
        public static EffectReference fromBuff(Buff buff, IEffect effect)
        {
            var buffEffects = buff.getEffects();
            var effectIndex = Array.IndexOf(buffEffects, effect);
            return new EffectReference(buff, null, effectIndex);
        }
        public static EffectReference fromAny(CardDefine cardDefine, Buff buff, IEffect effect)
        {
            if (buff != null)
            {
                return fromBuff(buff, effect);
            }
            return fromCardDefine(cardDefine, effect);
        }
        public static EffectReference fromEnv(EffectEnv env)
        {
            return fromAny(env.sourceCardDefine, env.buff, env.effect);
        }
        public override string ToString()
        {
            return buff != null ? $"卡牌{buff.card}的增益{buff}的第{effectIndex}个效果" : $"卡牌定义{cardDefine}的第{effectIndex}个效果";
        }
        public CardDefine cardDefine;
        public Buff buff;
        public int effectIndex;
    }
    [Serializable]
    public class SerializableEffectReference
    {
        public SerializableEffectReference(EffectReference effectRef)
        {
            cardDefineRef = effectRef.cardDefine?.getDefineRef();
            buffInstanceId = effectRef.buff?.instanceID ?? -1;
            cardId = effectRef.buff?.card?.id ?? -1;
            effectIndex = effectRef.effectIndex;
        }
        public EffectReference toReference(CardEngine game)
        {
            var cardDefine = game.getDefine(cardDefineRef);
            var card = game.getCard(cardId);
            var buff = card?.getBuffs()?.FirstOrDefault(b => b.instanceID == buffInstanceId);
            return new EffectReference(buff, cardDefine, effectIndex);
        }
        public DefineReference cardDefineRef;
        public int cardId;
        public int buffInstanceId;
        public int effectIndex;
    }
}
