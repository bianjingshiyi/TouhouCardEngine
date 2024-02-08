using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TouhouCardEngine.Histories;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    [EventChildren(typeof(CardPropChangeEventDefine))]
    public class RemoveBuffEventDefine : EventDefine, ICardEventDefine
    {
        public RemoveBuffEventDefine()
        {
            beforeVariableInfos = new EventVariableInfo[]
            {
                new EventVariableInfo() { name = VAR_CARD, type = typeof(Card) },
                new EventVariableInfo() { name = VAR_BUFF, type = typeof(Buff) },
            };
            afterVariableInfos = new EventVariableInfo[]
            {
                new EventVariableInfo() { name = VAR_CARD, type = typeof(Card) },
                new EventVariableInfo() { name = VAR_BUFF, type = typeof(Buff) },
                new EventVariableInfo() { name = VAR_REMOVED, type = typeof(bool) },
            };
        }
        public override async Task execute(IEventArg arg)
        {
            var game = arg.game;
            var argCard = arg.getVar<Card>(VAR_CARD);
            var argBuff = arg.getVar<Buff>(VAR_BUFF);
            if (argCard.containsBuff(argBuff))
            {
                // 如果有属性修正器的属性和Buff关联，记录与其有关的卡牌属性的值。
                var modifiers = argBuff.getModifiers();
                List<CardModifierState> modifierStates = new List<CardModifierState>();
                foreach (var modifier in modifiers)
                {
                    var state = new CardModifierState(game, modifier, argCard, argBuff);
                    modifierStates.Add(state);
                    // 调用BeforeRemove。
                    await modifier.beforeRemove(game, argCard, argBuff);
                }

                // 移除增益
                game?.logger?.logTrace("Buff", $"{argCard}移除增益{argBuff}");
                argCard.removeBuff(argBuff);
                game.triggers.addChange(new RemoveBuffChange(argCard, argBuff));

                // 禁用增益
                await argBuff.disable(game, argCard);
                arg.setVar(VAR_REMOVED, true);

                foreach (var state in modifierStates)
                {
                    var modifier = state.modifier;
                    // 调用AfterRemove。
                    await modifier.afterRemove(game, argCard, argBuff);
                    // 更新卡牌属性。
                    await modifier.updateCardProp(game, argCard, state.cardBeforeProperty);
                }
            }
        }
        [Obsolete]
        public override void Record(CardEngine game, EventArg arg, EventRecord record)
        {
            record.setCardState(VAR_CARD, arg.getVar<Card>(VAR_CARD));
            record.setVar(VAR_BUFF, arg.getVar<Buff>(VAR_BUFF));
        }
        public static Task<EventArg> doEvent(CardEngine game, Card card, Buff buff)
        {
            var define = game.getEventDefine<RemoveBuffEventDefine>();
            var propChangeEvent = new EventArg(game, define);
            propChangeEvent.setVar(VAR_CARD, card);
            propChangeEvent.setVar(VAR_BUFF, buff);
            return game.triggers.doEvent(propChangeEvent);
        }
        ICard ICardEventDefine.getCard(IEventArg arg)
        {
            return arg.getVar<Card>(VAR_CARD);
        }
        public override string toString(EventArg arg)
        {
            var argCard = arg.getVar<Card>(VAR_CARD);
            var argBuff = arg.getVar<Buff>(VAR_BUFF);
            return $"移除{argCard}的增益{argBuff}";
        }
        public const string VAR_CARD = "卡牌";
        public const string VAR_BUFF = "增益";
        public const string VAR_REMOVED = "是否成功";
    }
}
