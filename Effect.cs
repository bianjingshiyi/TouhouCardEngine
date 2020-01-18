using System;
using System.Linq;
using System.Threading.Tasks;
namespace TouhouCardEngine
{
    /// <summary>
    /// 效果
    /// </summary>
    public abstract class Effect
    {
        /// <summary>
        /// 效果的作用时机
        /// </summary>
        public abstract string trigger { get; }
        /// <summary>
        /// 效果的作用域
        /// </summary>
        public abstract string pile { get; }
        /// <summary>
        /// 检查效果能否发动
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="player"></param>
        /// <param name="card"></param>
        /// <returns></returns>
        public abstract bool checkCondition(CardEngine engine, Player player, Card card);
        /// <summary>
        /// 检查效果的目标是否合法
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="player"></param>
        /// <param name="card"></param>
        /// <param name="targets"></param>
        /// <returns></returns>
        public abstract bool checkTargets(CardEngine engine, Player player, Card card, object[] targets);
        public abstract Task executeAsync(CardEngine engine, Player player, Card card, object[] targets);
        /// <summary>
        /// 发动效果
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="player"></param>
        /// <param name="card"></param>
        /// <param name="targets"></param>
        [Obsolete]
        public virtual void execute(CardEngine engine, Player player, Card card, object[] targets)
        {
            executeAsync(engine, player, card, targets);
        }
        /// <summary>
        /// 检查效果目标是否合法
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="card"></param>
        /// <param name="targetCards"></param>
        /// <returns></returns>
        [Obsolete]
        public virtual bool checkTarget(CardEngine engine, Player player, Card card, Card[] targetCards)
        {
            return checkTargets(engine, player, card, targetCards);
        }
        /// <summary>
        /// 执行效果
        /// </summary>
        /// <param name="engine"></param>
        [Obsolete]
        public virtual void execute(CardEngine engine, Player player, Card card, Card[] targetCards)
        {
            execute(engine, player, card, targetCards.Cast<object>().ToArray());
        }
    }
}