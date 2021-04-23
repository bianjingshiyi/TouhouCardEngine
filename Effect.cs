using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
                Trigger trigger = new Trigger(args =>
                {
                    if ((this as ITriggerEffect).checkCondition(game, card, buff, args))
                        return (this as ITriggerEffect).execute(game, card, buff, args, new object[0]);
                    else
                        return Task.CompletedTask;
                });
                await card.setProp(game, getTriggerName(game, card, buff, time), trigger);
                game.triggers.register(time.getEventName(game.triggers), trigger);
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
            return (buff != null ? buff.instanceID.ToString() : string.Empty) + "Effect" + Array.IndexOf(card.define.effects, this) + time.getEventName(game.triggers);
        }
    }
    [Serializable]
    public class GeneratedEffect : IActiveEffect
    {
        #region 方法
        public Task execute(IGame game, ICard card, IBuff buff, IEventArg eventArg)
        {
            return game.doAction(card, buff, eventArg, action);
        }
        bool IActiveEffect.checkCondition(IGame game, ICard card, object[] vars)
        {
            throw new NotImplementedException();
        }

        Task IActiveEffect.execute(IGame game, ICard card, object[] vars, object[] targets)
        {
            return execute(game, card, null, vars != null && vars.Length > 0 ? (IEventArg)vars[0] : null);
        }
        #endregion
        #region 属性字段
        ActionNode action { get; }
        #endregion
    }
    [Serializable]
    public class ActionNode
    {
        public string defineName { get; set; }
        public ActionNode next { get; set; }
        public ActionVarRef[] inputs { get; set; }
        public ActionVarRef[] outputs { get; set; }
        public object[] consts { get; set; }
    }
    public class ActionVarRef
    {
        public string varName { get; set; }
        public int index { get; set; }
    }
    public abstract class ActionDefine
    {
        #region 方法
        public abstract Task<object[]> execute(IGame game, ICard card, IBuff buff, IEventArg eventArg, object[] args, object[] constValues);
        #endregion
        #region 属性字段
        public abstract ValueDefine[] inputs { get; }
        public abstract ValueDefine[] consts { get; }
        public abstract ValueDefine[] outputs { get; }
        #endregion
    }
    public struct ValueDefine
    {
        public Type type { get; set; }
        public string name { get; set; }
        public bool isParams { get; set; }
    }
    public class BuiltinActionDefine : ActionDefine
    {
        #region 方法
        public BuiltinActionDefine(Func<IGame, ICard, IBuff, IEventArg, object[], object[], Task<object[]>> action) : base()
        {
            this.action = action;
            consts = new ValueDefine[0];
        }
        public override Task<object[]> execute(IGame game, ICard card, IBuff buff, IEventArg eventArg, object[] args, object[] constValues)
        {
            return action(game, card, buff, eventArg, args, constValues);
        }
        #endregion
        Func<IGame, ICard, IBuff, IEventArg, object[], object[], Task<object[]>> action { get; }
        public override ValueDefine[] inputs { get; }
        public override ValueDefine[] consts { get; }
        public override ValueDefine[] outputs { get; }
    }
    public class IntegerBinaryOperationActionDefine : ActionDefine
    {
        public IntegerBinaryOperationActionDefine()
        {
            inputs = new ValueDefine[1]
            {
                new ValueDefine()
                {
                    name = "value",
                    type = typeof(int),
                    isParams = true
                }
            };
            consts = new ValueDefine[1]
            {
                new ValueDefine()
                {
                    name = "operator",
                    type = typeof(BinaryOperator)
                }
            };
            outputs = new ValueDefine[1]
            {
                new ValueDefine()
                {
                    name = "result",
                    type = typeof(int)
                }
            };
        }
        public override Task<object[]> execute(IGame game, ICard card, IBuff buff, IEventArg eventArg, object[] args, object[] constValues)
        {
            BinaryOperator op = (BinaryOperator)constValues[0];
            switch (op)
            {
                case BinaryOperator.add:
                    return Task.FromResult(new object[] { ((int[])args[0]).Sum(a => a) });
                default:
                    throw new InvalidOperationException("未知的操作符" + op);
            }
        }
        public override ValueDefine[] inputs { get; }
        public override ValueDefine[] consts { get; }
        public override ValueDefine[] outputs { get; }
    }
    public class IntegerConstActionDefine : ActionDefine
    {
        public IntegerConstActionDefine()
        {
            inputs = new ValueDefine[0];
            consts = new ValueDefine[1]
            {
                new ValueDefine()
                {
                    name = "value",
                    type = typeof(int)
                }
            };
            outputs = new ValueDefine[1]
            {
                new ValueDefine()
                {
                    name = "value",
                    type = typeof(int)
                }
            };
        }
        public override Task<object[]> execute(IGame game, ICard card, IBuff buff, IEventArg eventArg, object[] args, object[] constValues)
        {
            return Task.FromResult(constValues);
        }
        public override ValueDefine[] inputs { get; }
        public override ValueDefine[] consts { get; }
        public override ValueDefine[] outputs { get; }
    }
    public enum BinaryOperator
    {
        add,
        sub,
        mul,
        div
    }
}