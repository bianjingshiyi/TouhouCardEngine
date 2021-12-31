using System.Collections.Generic;
using System.Threading.Tasks;
using NitoriNetwork.Common;

namespace TouhouCardEngine
{
    public abstract partial class Rule
    {
        public Rule(IEnumerable<CardDefine> cards, IEnumerable<BuffDefine> buffs)
        {
            cardList.AddRange(cards);
            if (buffs != null)
            {
                foreach (var buff in buffs)
                {
                    _buffDefineDict.Add(buff.getId(), buff);
                }
            }
        }
        public CardDefine getCardDefine(int cardDefineId)
        {
            for (int i = 0; i < cardList.Count; i++)
            {
                if (cardList[i].id == cardDefineId)
                    return cardList[i];
            }
            return null;
        }
        public abstract Task onGameInit(CardEngine game, GameOption options, IRoomPlayer[] players);
        public abstract Task onGameRun(CardEngine game);
        public abstract Task onGameStart(CardEngine game);
        public abstract Task onPlayerInit(CardEngine game, Player player);
        public abstract Task onPlayerCommand(CardEngine game, Player player, CardEngine.CommandEventArg command);
        public abstract Task onGameClose(CardEngine game);
        public List<CardDefine> cardList { get; } = new List<CardDefine>();
    }
}