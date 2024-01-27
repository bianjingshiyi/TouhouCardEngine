using System;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public class EffectReference
    {
        public EffectReference(BuffDefine buffDefine, CardDefine cardDefine, int effectIndex)
        {
            this.buffDefine = buffDefine;
            this.cardDefine = cardDefine;
            this.effectIndex = effectIndex;
        }
        public Effect getEffect()
        {
            if (effectIndex < 0)
                return null;
            if (cardDefine == null && buffDefine == null)
                return null;
            return buffDefine != null ? buffDefine.getEffects()[effectIndex] : cardDefine.getEffects()[effectIndex];
        }
        public static EffectReference fromCardDefine(CardDefine cardDefine, Effect effect)
        {
            var defineEffects = cardDefine.getEffects();
            var effectIndex = Array.IndexOf(defineEffects, effect);
            return new EffectReference(null, cardDefine, effectIndex);
        }
        public static EffectReference fromBuffDefine(BuffDefine buffDefine, Effect effect)
        {
            var buffEffects = buffDefine.getEffects();
            var effectIndex = Array.IndexOf(buffEffects, effect);
            return new EffectReference(buffDefine, null, effectIndex);
        }
        public static EffectReference fromAny(CardDefine cardDefine, BuffDefine buffDefine, Effect effect)
        {
            if (buffDefine != null)
            {
                return fromBuffDefine(buffDefine, effect);
            }
            return fromCardDefine(cardDefine, effect);
        }
        public static EffectReference fromEnv(EffectEnv env)
        {
            var game = env.game;
            if (env.effect.buffDefineRef != null)
            {
                var buffDefine = game.getBuffDefine(env.effect.buffDefineRef);
                return fromBuffDefine(buffDefine, env.effect);
            }
            else
            {
                var cardDefine = game.getDefine(env.effect.cardDefineRef);
                return fromCardDefine(cardDefine, env.effect);
            }
        }
        public override string ToString()
        {
            return buffDefine != null ? $"增益{buffDefine}的第{effectIndex}个效果" : $"卡牌定义{cardDefine}的第{effectIndex}个效果";
        }
        public CardDefine cardDefine;
        public BuffDefine buffDefine;
        public int effectIndex;
    }
    [Serializable]
    public class SerializableEffectReference
    {
        public SerializableEffectReference(EffectReference effectRef)
        {
            cardDefineRef = effectRef.cardDefine?.getDefineRef();
            buffDefineRef = effectRef.buffDefine?.getDefineRef();
            effectIndex = effectRef.effectIndex;
        }
        public EffectReference toReference(CardEngine game)
        {
            var cardDefine = cardDefineRef != null ? game.getDefine(cardDefineRef) : null;
            var buffDefine = buffDefineRef != null ? game.getBuffDefine(buffDefineRef) : null;
            return new EffectReference(buffDefine, cardDefine, effectIndex);
        }
        public DefineReference cardDefineRef;
        public DefineReference buffDefineRef;
        public int effectIndex;
    }
}
