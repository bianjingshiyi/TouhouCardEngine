using System.Threading.Tasks;
using TouhouCardEngine.Histories;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public class ChangeCardVisibilityEventDefine : EventDefine, ICardEventDefine
    {
        public override Task execute(IEventArg arg)
        {
            var game = arg.game;
            var card = arg.getVar<Card>(VAR_CARD);
            var player = arg.getVar<Player>(VAR_PLAYER);
            var visible = arg.getVar<bool>(VAR_VISIBLE);

            if (card.setVisible(player, visible))
            {
                game.triggers.addChange(new CardVisibilityChange(card, player, visible));
            }
            return Task.CompletedTask;
        }
        public override string toString(EventArg arg)
        {
            var card = arg.getVar<Card>(VAR_CARD);
            var player = arg.getVar<Player>(VAR_PLAYER);
            var visible = arg.getVar<bool>(VAR_VISIBLE);
            return visible ? $"将卡牌{card}对{player}显示" : $"将卡牌{card}对{player}隐藏";
        }
        public static Task<EventArg> doEvent(CardEngine game, Card card, Player player, bool visible)
        {
            var define = game.getEventDefine<ChangeCardVisibilityEventDefine>();
            var arg = new EventArg(game, define);
            arg.setVar(VAR_CARD, card);
            arg.setVar(VAR_PLAYER, player);
            arg.setVar(VAR_VISIBLE, visible);
            return game.triggers.doEvent(arg);
        }
        ICard ICardEventDefine.getCard(IEventArg arg)
        {
            var card = arg.getVar<Card>(VAR_CARD);
            return card;
        }
        public const string VAR_CARD = "Card";
        public const string VAR_PLAYER = "Player";
        public const string VAR_VISIBLE = "Visible";
    }
}
