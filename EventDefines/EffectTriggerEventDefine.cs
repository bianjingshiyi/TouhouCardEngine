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
            var eventArg = arg.getVar<EventArg>(VAR_EVENT_ARG);
            var effectRef = arg.getVar<EffectReference>(VAR_EFFECT_REF);
            var portName = arg.getVar<string>(VAR_PORT_NAME);

            IEffect effect = effectRef.getEffect();

            if (effect is ITriggerEventEffect triggerEffect)
            {
                var effectEnv = new EffectEnv(game, card, effectRef.cardDefine, effectRef.buff, eventArg, effect);
                await triggerEffect.runEffect(effectEnv, portName);
            }
        }
        [Obsolete]
        public override void Record(CardEngine game, EventArg arg, EventRecord record)
        {
            record.setCardState(VAR_CARD, arg);
            record.setVar(VAR_EVENT_ARG, arg);
            record.setVar(VAR_EFFECT_REF, arg);
            record.setVar(VAR_PORT_NAME, arg);
        }
        public override string toString(EventArg arg)
        {
            var card = arg.getVar<Card>(VAR_CARD);
            var effectRef = arg.getVar<EffectReference>(VAR_EFFECT_REF);
            var portName = arg.getVar<string>(VAR_PORT_NAME);
            return $"卡牌{card}触发效果{effectRef}的{portName}";
        }
        public static Task<EventArg> doEvent(EffectEnv env, string portName)
        {
            var game = env.game;
            var define = game.getEventDefine<EffectTriggerEventDefine>();
            var arg = new EventArg(game, define);
            arg.setVar(VAR_CARD, env.card);
            arg.setVar(VAR_EVENT_ARG, env.eventArg);
            arg.setVar(VAR_EFFECT_REF, EffectReference.fromEnv(env));
            arg.setVar(VAR_PORT_NAME, portName);
            return game.triggers.doEvent(arg);
        }
        public const string VAR_CARD = "Card";
        public const string VAR_EVENT_ARG = "EventArg";
        public const string VAR_EFFECT_REF = "EffectReference";
        public const string VAR_PORT_NAME = "PortName";
    }
}
