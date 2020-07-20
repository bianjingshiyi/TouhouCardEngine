using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;
namespace TouhouCardEngine
{
    public abstract class TriggerTime
    {
        public abstract string getEventName(ITriggerManager manager);

    }
    public class Before<T> : TriggerTime where T : IEventArg
    {
        public override string getEventName(ITriggerManager manager)
        {
            return manager.getNameBefore<T>();
        }
    }
    public class On<T> : TriggerTime where T : IEventArg
    {
        public override string getEventName(ITriggerManager manager)
        {
            return manager.getName<T>();
        }
    }
    public class After<T> : TriggerTime where T : IEventArg
    {
        public override string getEventName(ITriggerManager manager)
        {
            return manager.getNameAfter<T>();
        }
    }
    /// <summary>
    /// 效果
    /// </summary>
    public abstract class Effect : ITriggerEffect
    {
        /// <summary>
        /// 效果的作用时机
        /// </summary>
        [Obsolete]
        public virtual string trigger { get; } = null;
        public abstract TriggerTime[] triggerTimes { get; }
        string[] ITriggerEffect.events
        {
            get { return new string[] { trigger }; }
        }
        string[] ITriggerEffect.getEvents(ITriggerManager manager)
        {
            List<string> eventList = new List<string>();
            if (!string.IsNullOrEmpty(trigger))
                eventList.Add(trigger);
            foreach (var triggerTime in triggerTimes)
            {
                eventList.Add(triggerTime.getEventName(manager));
            }
            return eventList.ToArray();
        }
        /// <summary>
        /// 效果的作用域
        /// </summary>
        public abstract string pile { get; }
        string[] IPassiveEffect.piles
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
        bool ITriggerEffect.checkCondition(IGame game, ICard card, object[] vars)
        {
            return checkCondition(game as CardEngine, null, card as Card, vars);
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
        bool ITriggerEffect.checkTargets(IGame game, ICard card, object[] vars, object[] targets)
        {
            return checkTargets(game as CardEngine, null, card as Card, targets);
        }
        public abstract Task execute(CardEngine engine, Player player, Card card, object[] vars, object[] targets);
        Task ITriggerEffect.execute(IGame game, ICard card, object[] vars, object[] targets)
        {
            return execute(game as CardEngine, null, card as Card, vars, targets);
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
            execute(engine, player, card, new object[0], targets);
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
        public void onEnable(IGame game, ICard card, IBuff buff)
        {
            foreach (TriggerTime time in triggerTimes)
            {
                Trigger trigger = new Trigger(args =>
                {
                    if ((this as ITriggerEffect).checkCondition(game, card, args))
                        return (this as ITriggerEffect).execute(game, card, args, new object[0]);
                    else
                        return Task.CompletedTask;
                });
                card.setProp("Effect" + Array.IndexOf(card.define.effects, this) + time.getEventName(game.triggers), trigger);
                game.triggers.register(time.getEventName(game.triggers), trigger);
            }
        }
        public void onDisable(IGame game, ICard card, IBuff buff)
        {
            foreach (TriggerTime time in triggerTimes)
            {
                Trigger trigger = card.getProp<Trigger>(game, "Effect" + Array.IndexOf(card.define.effects, this) + time.getEventName(game.triggers));
                game.triggers.remove(trigger);
            }
        }
    }
}