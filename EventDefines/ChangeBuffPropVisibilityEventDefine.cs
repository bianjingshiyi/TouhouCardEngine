using System.Threading.Tasks;
using TouhouCardEngine.Histories;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public class ChangeBuffPropVisibilityEventDefine : EventDefine, ICardEventDefine
    {
        public override Task execute(IEventArg arg)
        {
            var game = arg.game;
            var buff = arg.getVar<Buff>(VAR_BUFF);
            var propName = arg.getVar<string>(VAR_PROPERTY_NAME);
            var player = arg.getVar<Player>(VAR_PLAYER);
            var invisible = arg.getVar<bool>(VAR_INVISIBLE);

            var card = buff.card;
            if (card == null)
                return Task.CompletedTask;
            if (buff.setPropInvisible(propName, player, invisible))
            {
                game.triggers.addChange(new BuffPropVisibilityChange(card, buff.instanceID, propName, player, invisible));
            }
            return Task.CompletedTask;
        }
        public override string toString(EventArg arg)
        {
            var buff = arg.getVar<Buff>(VAR_BUFF);
            var card = buff.card;
            var propName = arg.getVar<string>(VAR_PROPERTY_NAME);
            var player = arg.getVar<Player>(VAR_PLAYER);
            var invisible = arg.getVar<bool>(VAR_INVISIBLE);
            return invisible ? $"将卡牌{card}的增益{buff}的属性{propName}对{player}隐藏" : $"将卡牌{card}的增益{buff}的属性{propName}对{player}显示";
        }
        public static Task<EventArg> doEvent(CardEngine game, Buff buff, string propName, Player player, bool invisible)
        {
            var define = game.getEventDefine<ChangeBuffPropVisibilityEventDefine>();
            var arg = new EventArg(game, define);
            arg.setVar(VAR_BUFF, buff);
            arg.setVar(VAR_PROPERTY_NAME, propName);
            arg.setVar(VAR_PLAYER, player);
            arg.setVar(VAR_INVISIBLE, invisible);
            return game.triggers.doEvent(arg);
        }
        ICard ICardEventDefine.getCard(IEventArg arg)
        {
            var buff = arg.getVar<Buff>(VAR_BUFF);
            return buff?.card;
        }
        public const string VAR_BUFF = "Buff";
        public const string VAR_PROPERTY_NAME = "PropertyName";
        public const string VAR_PLAYER = "Player";
        public const string VAR_INVISIBLE = "Invisible";
    }
}
