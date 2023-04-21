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
        /// 检查效果能否发动
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="player"></param>
        /// <param name="card"></param>
        /// <returns></returns>
        public abstract bool checkCondition(CardEngine engine, Player player, Card card, Buff buff, object[] vars);
        bool ITriggerEffect.checkCondition(IGame game, ICard card, IBuff buff, object[] vars)
        {
            return checkCondition(game as CardEngine, null, card as Card, buff as Buff, vars);
        }
        /// <summary>
        /// 检查效果的目标是否合法
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="player"></param>
        /// <param name="card"></param>
        /// <param name="targets"></param>
        /// <returns></returns>
        public abstract bool checkTargets(CardEngine engine, Player player, Card card, Buff buff, object[] targets);
        bool ITriggerEffect.checkTargets(IGame game, ICard card, IBuff buff, object[] vars, object[] targets)
        {
            return checkTargets(game as CardEngine, null, card as Card, buff as Buff, targets);
        }
        public abstract Task execute(CardEngine engine, Player player, Card card, Buff buff, object[] vars, object[] targets);
        Task ITriggerEffect.execute(IGame game, ICard card, IBuff buff, object[] vars, object[] targets)
        {
            return execute(game as CardEngine, null, card as Card, buff as Buff, vars, targets);
        }
        /// <summary>
        /// 发动效果
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="player"></param>
        /// <param name="card"></param>
        /// <param name="targets"></param>
        [Obsolete]
        public virtual void execute(CardEngine engine, Player player, Card card, Buff buff, object[] targets)
        {
            execute(engine, player, card, buff, new object[0], targets);
        }
        /// <summary>
        /// 检查效果目标是否合法
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="card"></param>
        /// <param name="targetCards"></param>
        /// <returns></returns>
        [Obsolete]
        public virtual bool checkTarget(CardEngine engine, Player player, Card card, Buff buff, Card[] targetCards)
        {
            return checkTargets(engine, player, card, buff, targetCards);
        }
        /// <summary>
        /// 执行效果
        /// </summary>
        /// <param name="engine"></param>
        [Obsolete]
        public virtual void execute(CardEngine engine, Player player, Card card, Buff buff, Card[] targetCards)
        {
            execute(engine, player, card, buff, targetCards.Cast<object>().ToArray());
        }
        public async Task onEnable(IGame game, ICard card, IBuff buff)
        {
            foreach (TriggerTime time in triggerTimes)
            {
                Trigger trigger = new Trigger(action: args =>
                {
                    if ((this as ITriggerEffect).checkCondition(game, card, buff, args))
                        return (this as ITriggerEffect).execute(game, card, buff, args, new object[0]);
                    else
                        return Task.CompletedTask;
                });
                await card.setProp(game, getTriggerName(game, card, buff, time), trigger);
                game.triggers.registerDelayed(time.getEventName(game.triggers), trigger);
            }
        }
        public Task onDisable(IGame game, ICard card, IBuff buff)
        {
            foreach (TriggerTime time in triggerTimes)
            {
                Trigger trigger = card.getProp<Trigger>(game, getTriggerName(game, card, buff, time));
                game.triggers.remove(trigger);
            }
            return Task.CompletedTask;
        }
        private string getTriggerName(IGame game, ICard card, IBuff buff, TriggerTime time)
        {
            return (buff != null ? buff.instanceID.ToString() : string.Empty) + "Effect" + Array.IndexOf(card.define.getEffects(), this) + time.getEventName(game.triggers);
        }
    }
    public class EffectPropertyInfo
    {
        public EffectPropertyInfo(Type type, string name)
        {
            this.type = type;
            this.name = name;
        }
        public string name;
        public Type type;
    }
}