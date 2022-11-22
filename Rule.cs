using System.Collections.Generic;
using System.Threading.Tasks;
using NitoriNetwork.Common;

namespace TouhouCardEngine
{
    public abstract partial class Rule
    {
        public Rule(Dictionary<long, Dictionary<int, CardDefine>> cards, Dictionary<long, Dictionary<int, BuffDefine>> buffs)
        {
            cardDict = cards;
            buffDict = buffs;
        }
        public abstract Task onGameInit(CardEngine game, GameOption options, IRoomPlayer[] players);
        public abstract Task onGameRun(CardEngine game);
        public abstract Task onGameStart(CardEngine game);
        public abstract Task onPlayerInit(CardEngine game, Player player);
        public abstract Task onPlayerCommand(CardEngine game, Player player, CardEngine.CommandEventArg command);
        public abstract Task onGameClose(CardEngine game);
        public Dictionary<long, Dictionary<int, CardDefine>> cardDict;
    }
}