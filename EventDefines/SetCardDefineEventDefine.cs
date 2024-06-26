using System;
using System.Threading.Tasks;
using TouhouCardEngine.Histories;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public class SetCardDefineEventDefine : EventDefine, ICardEventDefine
    {
        public SetCardDefineEventDefine()
        {
            beforeVariableInfos = new EventVariableInfo[]
            {
                new EventVariableInfo() { name = VAR_CARD, type = typeof(Card) },
                new EventVariableInfo() { name = VAR_BEFORE_DEFINE, type = typeof(CardDefine) },
                new EventVariableInfo() { name = VAR_AFTER_DEFINE, type = typeof(CardDefine) },
            };
            afterVariableInfos = new EventVariableInfo[]
            {
                new EventVariableInfo() { name = VAR_CARD, type = typeof(Card) },
                new EventVariableInfo() { name = VAR_BEFORE_DEFINE, type = typeof(CardDefine) },
                new EventVariableInfo() { name = VAR_AFTER_DEFINE, type = typeof(CardDefine) },
            };
        }
        public override async Task execute(IEventArg arg)
        {
            var game = arg.game;
            var argCard = arg.getVar<Card>(VAR_CARD);
            var argBeforeDefine = arg.getVar<CardDefine>(VAR_BEFORE_DEFINE);
            var argAfterDefine = arg.getVar<CardDefine>(VAR_AFTER_DEFINE);

            //禁用之前的所有效果
            foreach (var effect in argCard.define.getEffects())
            {
                await effect.disable(game, argCard, null);
            }
            //更换define
            argCard.setDefine(argAfterDefine);
            game.triggers.addChange(new SetCardDefineChange(argCard, argBeforeDefine, argAfterDefine));
            //激活效果
            foreach (var effect in argCard.define.getEffects())
            {
                await effect.updateEnable(game, argCard, null);
            }
        }
        [Obsolete]
        public override void Record(CardEngine game, EventArg arg, EventRecord record)
        {
            record.setVar(VAR_BEFORE_DEFINE, arg.getVar<CardDefine>(VAR_BEFORE_DEFINE));
            record.setCardState(VAR_CARD, arg.getVar<Card>(VAR_CARD));
            record.setVar(VAR_AFTER_DEFINE, arg.getVar<CardDefine>(VAR_AFTER_DEFINE));
        }
        public override string toString(EventArg arg)
        {
            var argCard = arg.getVar<Card>(VAR_CARD);
            var argBeforeDefine = arg.getVar<CardDefine>(VAR_BEFORE_DEFINE);
            var argAfterDefine = arg.getVar<CardDefine>(VAR_AFTER_DEFINE);
            return $"将{argCard}的卡牌定义从{argBeforeDefine}设置为{argAfterDefine}";
        }
        public static Task<EventArg> doEvent(CardEngine game, Card card, CardDefine cardDefine)
        {
            var beforeDefine = card.define;
            var define = game.getEventDefine<SetCardDefineEventDefine>();
            var arg = new EventArg(game, define);
            arg.setVar(VAR_CARD, card);
            arg.setVar(VAR_BEFORE_DEFINE, beforeDefine);
            arg.setVar(VAR_AFTER_DEFINE, cardDefine);
            return game.triggers.doEvent(arg);
        }
        ICard ICardEventDefine.getCard(IEventArg arg)
        {
            return arg.getVar<Card>(VAR_CARD);
        }
        public const string VAR_BEFORE_DEFINE = "原卡牌定义";
        public const string VAR_CARD = "卡牌";
        public const string VAR_AFTER_DEFINE = "卡牌定义";
    }
}
