using System;
using System.Linq;
using System.Threading.Tasks;
using TouhouCardEngine.Histories;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    [EventChildren(typeof(CardPropChangeEventDefine))]
    public class BuffPropertyChangeEventDefine : EventDefine, ICardEventDefine
    {
        public override async Task execute(IEventArg arg)
        {
            var game = arg.game;
            var argBuff = arg.getVar<Buff>(VAR_BUFF);
            var argPropName = arg.getVar<string>(VAR_PROPERTY_NAME);
            var argValue = arg.getVar<object>(VAR_VALUE);
            var beforeValue = arg.getVar<object>(VAR_VALUE_BEFORE_CHANGED);

            var card = argBuff.card;
            if (card == null)
                return;

            // 当Buff属性发生改变的时候，如果有属性修正器的属性和Buff关联，记录与其有关的卡牌属性的值。
            var modifiers = argBuff.getModifiers().Where(m => m.relatedPropName == argPropName);
            CardModifierState[] modiBeforeValues =
                modifiers.Select(m => new CardModifierState(game, m, card, argBuff)).ToArray();

            // 设置增益的值。
            argBuff.setProp(argPropName, argValue);
            game.triggers.addChange(new BuffPropChange(card, argBuff.instanceID, argPropName, beforeValue, argValue));
            game.logger?.logTrace("Game", $"{argBuff}的属性{argPropName}=>{StringHelper.propToString(argValue)}");

            // 更新与该增益属性名绑定的修改器的值。
            foreach (var state in modiBeforeValues)
            {
                var modifier = state.modifier;
                await modifier.updateValue(game, card, argBuff, state.cardBeforeProperty, state.modifierBeforeValue);
            }
        }
        [Obsolete]
        public override void Record(CardEngine game, EventArg arg, EventRecord record)
        {
            record.setVar(VAR_BUFF, arg.getVar<Buff>(VAR_BUFF));
            record.setVar(VAR_PROPERTY_NAME, arg.getVar<string>(VAR_PROPERTY_NAME));
            record.setVar(VAR_VALUE, arg.getVar<object>(VAR_VALUE));
            record.setVar(VAR_VALUE_BEFORE_CHANGED, arg.getVar<object>(VAR_VALUE_BEFORE_CHANGED));
        }
        public override string toString(EventArg arg)
        {
            var argBuff = arg.getVar<Buff>(VAR_BUFF);
            var argPropName = arg.getVar<string>(VAR_PROPERTY_NAME);
            var argValue = arg.getVar<object>(VAR_VALUE);
            var beforeValue = arg.getVar<object>(VAR_VALUE_BEFORE_CHANGED);
            return $"将增益{argBuff}的属性{argPropName}从{beforeValue}设置为{argValue}";
        }
        public static Task<EventArg> doEvent(CardEngine game, Buff buff, string propName, object value)
        {
            object valueBefore = buff.getProp(propName);
            var define = game.getEventDefine<BuffPropertyChangeEventDefine>();
            var arg = new EventArg(game, define);
            arg.setVar(VAR_BUFF, buff);
            arg.setVar(VAR_PROPERTY_NAME, propName);
            arg.setVar(VAR_VALUE, value);
            arg.setVar(VAR_VALUE_BEFORE_CHANGED, valueBefore);
            return game.triggers.doEvent(arg);
        }
        ICard ICardEventDefine.getCard(IEventArg arg)
        {
            var buff = arg.getVar<Buff>(VAR_BUFF);
            return buff?.card;
        }
        public const string VAR_BUFF = "Buff";
        public const string VAR_PROPERTY_NAME = "PropertyName";
        public const string VAR_VALUE = "Value";
        public const string VAR_VALUE_BEFORE_CHANGED = "ValueBeforeChanged";
    }
}
