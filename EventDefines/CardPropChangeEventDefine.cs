using System;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public class CardPropChangeEventDefine : EventDefine, ICardEventDefine
    {
        public override Task execute(IEventArg arg)
        {
            var game = arg.game;
            var argCard = arg.getVar<Card>(VAR_CARD);
            var argName = arg.getVar<string>(VAR_PROP_NAME);
            var argValue = arg.getVar<object>(VAR_VALUE_AFTER);

            game.logger?.logTrace("Game", $"{argCard}的属性{argName}=>{StringHelper.propToString(argValue)}");
            return Task.CompletedTask;
        }
        [Obsolete]
        public override void Record(CardEngine game, EventArg arg, EventRecord record)
        {
            record.setCardState(VAR_CARD, arg.getVar<Card>(VAR_CARD));
            record.setVar(VAR_PROP_NAME, arg.getVar<string>(VAR_PROP_NAME));
            record.setVar(VAR_VALUE_BEFORE, arg.getVar<object>(VAR_VALUE_BEFORE));
            record.setVar(VAR_VALUE_AFTER, arg.getVar<object>(VAR_VALUE_AFTER));
        }
        public override string toString(EventArg arg)
        {
            var argCard = arg.getVar<Card>(VAR_CARD);
            var argName = arg.getVar<string>(VAR_PROP_NAME);
            var argValueBefore = arg.getVar<object>(VAR_VALUE_BEFORE);
            var argValue = arg.getVar<object>(VAR_VALUE_AFTER);
            return $"{argCard}的属性{argName}从{argValueBefore}变为了{argValue}";
        }
        public static Task<EventArg> doEvent(CardEngine game, Card card, string name, object valueBefore, object value)
        {
            var define = game.getEventDefine<CardPropChangeEventDefine>();
            var propChangeEvent = new EventArg(game, define);
            propChangeEvent.setVar(VAR_CARD, card);
            propChangeEvent.setVar(VAR_PROP_NAME, name);
            propChangeEvent.setVar(VAR_VALUE_BEFORE, valueBefore);
            propChangeEvent.setVar(VAR_VALUE_AFTER, value);
            return game.triggers.doEvent(propChangeEvent);
        }
        ICard ICardEventDefine.getCard(IEventArg arg)
        {
            return arg.getVar<Card>(VAR_CARD);
        }
        public const string VAR_CARD = "card";
        public const string VAR_VALUE_BEFORE = "beforeValue";
        public const string VAR_PROP_NAME = "propName";
        public const string VAR_VALUE_AFTER = "value";
    }

}
