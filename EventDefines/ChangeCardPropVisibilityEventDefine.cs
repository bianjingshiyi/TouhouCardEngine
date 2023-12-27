using System.Threading.Tasks;
using TouhouCardEngine.Histories;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public class ChangeCardPropVisibilityEventDefine : EventDefine, ICardEventDefine
    {
        public override Task execute(IEventArg arg)
        {
            var game = arg.game;
            var card = arg.getVar<Card>(VAR_CARD);
            var propName = arg.getVar<string>(VAR_PROPERTY_NAME);
            var player = arg.getVar<Player>(VAR_PLAYER);
            var invisible = arg.getVar<bool>(VAR_INVISIBLE);

            if (card.setPropInvisible(propName, player, invisible))
            {
                game.triggers.addChange(new CardPropVisibilityChange(card, propName, player, invisible));
            }
            return Task.CompletedTask;
        }
        public override string toString(EventArg arg)
        {
            var card = arg.getVar<Card>(VAR_CARD);
            var propName = arg.getVar<string>(VAR_PROPERTY_NAME);
            var player = arg.getVar<Player>(VAR_PLAYER);
            var invisible = arg.getVar<bool>(VAR_INVISIBLE);
            return invisible ? $"将卡牌{card}的属性{propName}对{player}隐藏" : $"将卡牌{card}的属性{propName}对{player}显示";
        }
        public static Task<EventArg> doEvent(CardEngine game, Card card, string propName, Player player, bool invisible)
        {
            var define = game.getEventDefine<ChangeCardPropVisibilityEventDefine>();
            var arg = new EventArg(game, define);
            arg.setVar(VAR_CARD, card);
            arg.setVar(VAR_PROPERTY_NAME, propName);
            arg.setVar(VAR_PLAYER, player);
            arg.setVar(VAR_INVISIBLE, invisible);
            return game.triggers.doEvent(arg);
        }
        ICard ICardEventDefine.getCard(IEventArg arg)
        {
            var card = arg.getVar<Card>(VAR_CARD);
            return card;
        }
        public const string VAR_CARD = "Card";
        public const string VAR_PROPERTY_NAME = "PropertyName";
        public const string VAR_PLAYER = "Player";
        public const string VAR_INVISIBLE = "Invisible";
    }
}