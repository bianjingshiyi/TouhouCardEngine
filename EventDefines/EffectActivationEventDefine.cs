using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public class EffectActivationEventDefine : EventDefine, IEffectEventDefine
    {
        public EffectActivationEventDefine() 
        {
            beforeVariableInfos = new EventVariableInfo[]
            {
                new EventVariableInfo() { name = VAR_CARD, type = typeof(Card)},
                new EventVariableInfo() { name = VAR_BUFF, type = typeof(Buff)},
                new EventVariableInfo() { name = VAR_EFFECT, type = typeof(Effect)},
            };
            afterVariableInfos = new EventVariableInfo[]
            {
                new EventVariableInfo() { name = VAR_CARD, type = typeof(Card)},
                new EventVariableInfo() { name = VAR_BUFF, type = typeof(Buff)},
                new EventVariableInfo() { name = VAR_EFFECT, type = typeof(Effect)},
            };
        }
        public override async Task execute(IEventArg arg)
        {
            var game = arg.game;
            var card = arg.getVar<Card>(VAR_CARD);
            var buff = arg.getVar<Buff>(VAR_BUFF);
            var effect = arg.getVar<Effect>(VAR_EFFECT);
            var enable = arg.getVar<bool>(VAR_ENABLE);
            var effectEnv = new EffectEnv(game, card, buff, arg as EventArg, effect);

            if (enable)
            {
                await effect.onEnableInternal(effectEnv);
                // 设置该Effect已被启用。
                card.enableEffect(buff, effect);
            }
            else
            {
                // 设置该Effect已被禁用。
                card.disableEffect(buff, effect);
                await effect.onDisableInternal(effectEnv);
            }
        }
        public override string toString(EventArg arg)
        {
            var card = arg.getVar<Card>(VAR_CARD);
            var effect = arg.getVar<Effect>(VAR_EFFECT);
            var enable = arg.getVar<bool>(VAR_ENABLE);
            return enable ? $"卡牌{card}的效果{effect}被启用" : $"卡牌{card}的效果{effect}被禁用";
        }
        public static Task<EventArg> doEvent(CardEngine game, Card card, Buff buff, Effect effect, bool enable)
        {
            var define = game.getEventDefine<EffectActivationEventDefine>();
            var arg = new EventArg(game, define);
            arg.setVar(VAR_CARD, card);
            arg.setVar(VAR_EFFECT, effect);
            arg.setVar(VAR_BUFF, buff);
            arg.setVar(VAR_ENABLE, enable);
            return game.triggers.doEvent(arg);
        }
        Card IEffectEventDefine.getCard(EventArg arg)
        {
            return arg.getVar<Card>(VAR_CARD);
        }
        Effect IEffectEventDefine.getEffect(EventArg arg)
        {
            return arg.getVar<Effect>(VAR_EFFECT);
        }
        public const string VAR_PLAYER = "Player";
        public const string VAR_CARD = "Card";
        public const string VAR_BUFF = "Buff";
        public const string VAR_EFFECT = "Effect";
        public const string VAR_ENABLE = "Enable";
    }
}
