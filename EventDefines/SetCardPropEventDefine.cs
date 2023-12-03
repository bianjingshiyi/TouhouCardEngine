using System;
using System.Threading.Tasks;
using TouhouCardEngine.Histories;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    [EventChildren(typeof(CardPropChangeEventDefine))]
    public class SetCardPropEventDefine : EventDefine, ICardEventDefine
    {
        public override async Task execute(IEventArg arg)
        {
            var game = arg.game;
            var argCard = arg.getVar<Card>(VAR_CARD);
            var argName = arg.getVar<string>(VAR_PROP_NAME);
            var argValueBefore = arg.getVar<object>(VAR_VALUE_BEFORE);
            var argValue = arg.getVar<object>(VAR_VALUE_AFTER);
            var beforeValueRaw = argCard.getProp(game, argName, true);

            argCard.setProp(argName, argValue);
            game.triggers.addChange(new CardPropChange(argCard, argName, beforeValueRaw, argValue));

            await CardPropChangeEventDefine.doEvent(game, argCard, argName, argValueBefore, argValue);

            game.logger?.logTrace("Game", $"{argCard}的属性{argName}=>{StringHelper.propToString(argValue)}");
        }
        [Obsolete]
        public override void Record(CardEngine game, EventArg arg, EventRecord record)
        {
            record.setCardState(VAR_CARD, arg.getVar<Card>(VAR_CARD));
            record.setVar(VAR_PROP_NAME, arg.getVar<string>(VAR_PROP_NAME));
            record.setVar(VAR_VALUE_BEFORE, arg.getVar<object>(VAR_VALUE_BEFORE));
            record.setVar(VAR_VALUE_AFTER, arg.getVar<object>(VAR_VALUE_AFTER));
        }
        ICard ICardEventDefine.getCard(IEventArg arg)
        {
            return arg.getVar<Card>(VAR_CARD);
        }
        public override string toString(EventArg arg)
        {
            var argCard = arg.getVar<Card>(VAR_CARD);
            var argName = arg.getVar<string>(VAR_PROP_NAME);
            var argValueBefore = arg.getVar<object>(VAR_VALUE_BEFORE);
            var argValue = arg.getVar<object>(VAR_VALUE_AFTER);
            return $"将{argCard}的属性{argName}从{argValueBefore}设置为{argValue}";
        }
        public static Task<EventArg> doEvent(CardEngine game, Card card, string name, object value)
        {
            var valueBefore = card.getProp(game, name);

            var define = game.getEventDefine<SetCardPropEventDefine>();
            var propChangeEvent = new EventArg(game, define);
            propChangeEvent.setVar(VAR_CARD, card);
            propChangeEvent.setVar(VAR_PROP_NAME, name);
            propChangeEvent.setVar(VAR_VALUE_BEFORE, valueBefore);
            propChangeEvent.setVar(VAR_VALUE_AFTER, value);
            return game.triggers.doEvent(propChangeEvent);
        }
        public const string VAR_CARD = "card";
        public const string VAR_VALUE_BEFORE = "beforeValue";
        public const string VAR_PROP_NAME = "propName";
        public const string VAR_VALUE_AFTER = "value";
    }
}
