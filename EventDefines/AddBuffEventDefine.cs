using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TouhouCardEngine.Histories;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    [EventChildren(typeof(CardPropChangeEventDefine))]
    public class AddBuffEventDefine : EventDefine, ICardEventDefine
    {
        public AddBuffEventDefine()
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
            };
        }
        public override async Task execute(IEventArg arg)
        {
            var game = arg.game;
            var argCard = arg.getVar<Card>(VAR_CARD);
            var argBuff = arg.getVar<Buff>(VAR_BUFF);

            // 如果有属性修正器的属性和Buff关联，记录与其有关的卡牌属性的值。
            var modifiers = argBuff.getModifiers();
            List<CardModifierState> modifierStates = new List<CardModifierState>();
            foreach (var modifier in modifiers)
            {
                var state = new CardModifierState(game, modifier, argCard, argBuff);
                modifierStates.Add(state);
                // 调用beforeAdd。
                await modifier.beforeAdd(game, argCard, argBuff);
            }

            // 添加增益。
            game?.logger?.logTrace("Buff", $"{argCard}获得增益{argBuff}");
            var buffs = argCard.getBuffs();
            var buffId = (buffs.Length > 0 ? buffs.Max(b => b.instanceID) : 0) + 1;
            argBuff.card = argCard;
            argBuff.instanceID = buffId;
            argCard.addBuff(argBuff);
            game.triggers.addChange(new AddBuffChange(argCard, argBuff));

            // 启用增益。
            await argBuff.enable(game, argCard);

            // 更新与该增益属性名绑定的修改器的值。
            foreach (var state in modifierStates)
            {
                var modifier = state.modifier;
                // 调用afterAdd。
                await modifier.afterAdd(game, argCard, argBuff);
                // 更新卡牌属性变动，以及修改器的值。
                await modifier.updateValue(game, argCard, argBuff, state.cardBeforeProperty, state.modifierBeforeValue);
            }
        }
        [Obsolete]
        public override void Record(CardEngine game, EventArg arg, EventRecord record)
        {
            record.setCardState(VAR_CARD, arg.getVar<Card>(VAR_CARD));
            record.setVar(VAR_BUFF, arg.getVar<Buff>(VAR_BUFF));
        }
        public override string toString(EventArg arg)
        {
            var argCard = arg.getVar<Card>(VAR_CARD);
            var argBuff = arg.getVar<Buff>(VAR_BUFF);
            return $"为{argCard}添加增益{argBuff}";
        }
        public static Task<EventArg> doEvent(CardEngine game, Card card, Buff buff)
        {
            var define = game.getEventDefine<AddBuffEventDefine>();
            var propChangeEvent = new EventArg(game, define);
            propChangeEvent.setVar(VAR_CARD, card);
            propChangeEvent.setVar(VAR_BUFF, buff);
            return game.triggers.doEvent(propChangeEvent);
        }
        ICard ICardEventDefine.getCard(IEventArg arg)
        {
            return arg.getVar<Card>(VAR_CARD);
        }
        public const string VAR_CARD = "卡牌";
        public const string VAR_BUFF = "增益";
    }
}
