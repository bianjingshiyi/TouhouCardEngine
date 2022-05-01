using System;
using System.Threading.Tasks;
namespace TouhouCardEngine
{
    [Serializable]
    public abstract class BuffDefine
    {
        #region 公有方法
        public abstract Task onEnable(CardEngine game, Card card, Buff buff);
        public abstract Task onDisable(CardEngine game, Card card, Buff buff);
        public abstract int getId();
        #endregion
    }
}