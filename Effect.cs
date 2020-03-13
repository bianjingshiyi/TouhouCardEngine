using System;
using System.Linq;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;
namespace TouhouCardEngine
{
    /// <summary>
    /// 效果
    /// </summary>
    public abstract class Effect : IEffect
    {
        /// <summary>
        /// 效果的作用时机
        /// </summary>
        public abstract string trigger { get; }
        string[] IEffect.events
        {
            get { return new string[] { trigger }; }
        }
        /// <summary>
        /// 效果的作用域
        /// </summary>
        public abstract string pile { get; }
        string[] IEffect.piles
        {
            get { return new string[] { pile }; }
        }
        /// <summary>
        /// 检查效果能否发动
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="player"></param>
        /// <param name="card"></param>
        /// <returns></returns>
        public abstract bool checkCondition(CardEngine engine, Player player, Card card, object[] vars);
        bool IEffect.checkCondition(IGame game, IPlayer player, ICard card, object[] vars)
        {
            return checkCondition(game as CardEngine, player as Player, card as Card, vars);
        }
        /// <summary>
        /// 检查效果的目标是否合法
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="player"></param>
        /// <param name="card"></param>
        /// <param name="targets"></param>
        /// <returns></returns>
        public abstract bool checkTargets(CardEngine engine, Player player, Card card, object[] targets);
        bool IEffect.checkTarget(IGame game, IPlayer player, ICard card, object[] targets)
        {
            return checkTargets(game as CardEngine, player as Player, card as Card, targets);
        }
        public abstract Task executeAsync(CardEngine engine, Player player, Card card, object[] vars, object[] targets);
        Task IEffect.execute(IGame game, IPlayer player, ICard card, object[] vars, object[] targets)
        {
            return executeAsync(game as CardEngine, player as Player, card as Card, vars, targets);
        }
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
            executeAsync(engine, player, card, new object[0], targets);
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