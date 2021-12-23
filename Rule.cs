using System.Collections.Generic;
using System.Threading.Tasks;
using NitoriNetwork.Common;

namespace TouhouCardEngine
{
    public abstract partial class Rule
    {
        public List<CardDefine> cardList { get; } = new List<CardDefine>();
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
        public abstract Task onGameInit(CardEngine game, GameOption options, IRoomPlayer[] players);
        public abstract Task onGameRun(CardEngine game);
        public abstract Task onPlayerCommand(CardEngine game, Player player, CardEngine.CommandEventArg command);
        public abstract Task onGameClose(CardEngine game);
    }
}