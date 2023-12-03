using System;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public class EffectTriggerEventDefine : EventDefine
    {
        public override async Task execute(IEventArg arg)
        {
            var game = arg.game;
            var card = arg.getVar<Card>(VAR_CARD);
            var buff = arg.getVar<Buff>(VAR_BUFF);
            var eventArg = arg.getVar<EventArg>(VAR_EVENT_ARG);
            var effectIndex = arg.getVar<int>(VAR_EFFECT_INDEX);
            var triggerType = arg.getVar<string>(VAR_PORT_NAME);

            IEffect effect = getEffect(card, buff, effectIndex);

            if (effect is ITriggerEventEffect triggerEffect)
            {
                await triggerEffect.runEffect(game, card, buff, eventArg, triggerType);
            }
        }
        [Obsolete]
        public override void Record(CardEngine game, EventArg arg, EventRecord record)
        {
            record.setCardState(VAR_CARD, arg.getVar<Card>(VAR_CARD));
            record.setVar(VAR_BUFF, arg.getVar<Buff>(VAR_BUFF));
            record.setVar(VAR_EVENT_ARG, arg.getVar<Buff>(VAR_EVENT_ARG));
            record.setVar(VAR_EFFECT_INDEX, arg.getVar<int>(VAR_EFFECT_INDEX));
            record.setVar(VAR_PORT_NAME, arg.getVar<string>(VAR_PORT_NAME));
        }
        public override string toString(EventArg arg)
        {
            var card = arg.getVar<Card>(VAR_CARD);
            var buff = arg.getVar<Buff>(VAR_BUFF);
            var effectIndex = arg.getVar<int>(VAR_EFFECT_INDEX);
            var portName = arg.getVar<string>(VAR_PORT_NAME);
            if (buff != null)
                return $"卡牌{card}触发增益{buff}的第{effectIndex}个效果的{portName}";
            return $"卡牌{card}触发第{effectIndex}个效果的{portName}";
        }
        public static Task<EventArg> doEvent(CardEngine game, Card card, Buff buff, EventArg eventArg, ITriggerEventEffect effect, string portName)
        {
            var define = game.getEventDefine<EffectTriggerEventDefine>();
            var effectIndex = getEffectIndex(card, buff, effect);
            var arg = new EventArg(game, define);
            arg.setVar(VAR_CARD, card);
            arg.setVar(VAR_BUFF, buff);
            arg.setVar(VAR_EVENT_ARG, eventArg);
            arg.setVar(VAR_EFFECT_INDEX, effectIndex);
            arg.setVar(VAR_PORT_NAME, portName);
            return game.triggers.doEvent(arg);
        }
        public static IEffect getEffect(EventArg arg)
        {
            var card = arg.getVar<Card>(VAR_CARD);
            var buff = arg.getVar<Buff>(VAR_BUFF);
            var effectIndex = arg.getVar<int>(VAR_EFFECT_INDEX);
            return getEffect(card, buff, effectIndex);
        }
        private static IEffect getEffect(Card card, Buff buff, int effectIndex)
        {
            if (buff != null)
                return buff.getEffects()[effectIndex];
            return card.define.getEffects()[effectIndex];
        }
        private static int getEffectIndex(Card card, Buff buff, IEffect effect)
        {
            if (buff != null)
                return Array.IndexOf(buff.getEffects(), effect);
            return Array.IndexOf(card.define.getEffects(), effect);
        }
        public const string VAR_CARD = "Card";
        public const string VAR_BUFF = "Buff";
        public const string VAR_EVENT_ARG = "EventArg";
        public const string VAR_EFFECT_INDEX = "EffectIndex";
        public const string VAR_PORT_NAME = "PortName";
    }
}
