using System.Threading.Tasks;

namespace TouhouCardEngine
{
    public abstract partial class Rule
    {
        public Rule()
        {
        }
        public abstract Task onGameInit(CardEngine game, GameOption options);
        public abstract Task onGameClose(CardEngine game);
        public abstract Task onGameRun(CardEngine game);
        public abstract Task onPlayerInit(CardEngine game, Player player);
        public abstract Task onPlayerExit(CardEngine game, Player player);
    }
}