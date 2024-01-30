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
            var effect = arg.getVar<IEffect>(VAR_EFFECT);
            var effectBuff = arg.getVar<Buff>(VAR_EFFECT_BUFF);
            var portName = arg.getVar<string>(VAR_PORT_NAME);

            if (effect is ITriggerEventEffect triggerEffect)
            {
                var cardDefine = game.getDefine(effect.cardDefineRef);
                var effectEnv = new EffectEnv(game, card, cardDefine, effectBuff, eventArg, effect);
                await triggerEffect.runEffect(effectEnv, portName);
            }
        }
        public override string toString(EventArg arg)
        {
            var card = arg.getVar<Card>(VAR_CARD);
            var effect = arg.getVar<IEffect>(VAR_EFFECT);
            var portName = arg.getVar<string>(VAR_PORT_NAME);
            return $"卡牌{card}触发效果{effect}的{portName}";
        }
        public static Task<EventArg> doEvent(EffectEnv env, string portName)
        {
            var game = env.game;
            var define = game.getEventDefine<EffectTriggerEventDefine>();
            var arg = new EventArg(game, define);
            arg.setVar(VAR_CARD, env.card);
            arg.setVar(VAR_EVENT_ARG, env.eventArg);
            arg.setVar(VAR_EFFECT, env.effect);
            arg.setVar(VAR_EFFECT_BUFF, env.buff);
            arg.setVar(VAR_PORT_NAME, portName);
            return game.triggers.doEvent(arg);
        }
        public const string VAR_CARD = "Card";
        public const string VAR_EVENT_ARG = "EventArg";
        public const string VAR_EFFECT = "Effect";
        public const string VAR_EFFECT_BUFF = "EffectBuff";
        public const string VAR_PORT_NAME = "PortName";
    }
}
