using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TouhouCardEngine.Interfaces;
using UnityEngine;

namespace TouhouCardEngine
{
    partial class SyncTriggerSystem
    {
        #region 公共成员
        public SyncTask doEvent(EventContext context, IEnumerable<SyncAction> actions)
        {
            return doTask(context, new ActionCollection()
                .append(_doTrigLoopBfrAction)
                .append(_gotoBfrLoopAction)
                .append(actions)
                .append(_doTrigLoopAftAction)
                .append(_gotoAftLoopAction));
        }
        public SyncTask doEvent(EventContext context, params Action<CardEngine>[] actions)
        {
            return doEvent(context, actions.Select(a => new ALambda(a)));
        }
        public SyncTask doTrigger(SyncTrigger trigger)
        {
            return doTask(trigger.actions);
        }
        public void regTrigBfr(string eventName, SyncTrigger trigger)
        {
            if (!_triggerLinkDict.TryGetValue(getBfrTimeName(eventName), out var list))
            {
                list = new LinkedList<SyncTrigger>();
                _triggerLinkDict.Add(getBfrTimeName(eventName), list);
            }
            list.AddLast(trigger);
        }
        public IEnumerable<SyncTrigger> getTrigBfr(string eventName)
        {
            if (_triggerLinkDict.TryGetValue(getBfrTimeName(eventName), out var list))
                return list;
            else
                return Enumerable.Empty<SyncTrigger>();
        }
        public bool unregTrigBfr(string eventName, SyncTrigger trigger)
        {
            if (_triggerLinkDict.TryGetValue(getBfrTimeName(eventName), out var list))
                return list.Remove(trigger);
            else
                return false;
        }
        public void regTrigAft(string eventName, SyncTrigger trigger)
        {
            if (!_triggerLinkDict.TryGetValue(getAftTimeName(eventName), out var list))
            {
                list = new LinkedList<SyncTrigger>();
                _triggerLinkDict.Add(getAftTimeName(eventName), list);
            }
            list.AddLast(trigger);
        }
        public IEnumerable<SyncTrigger> getTrigAft(string eventName)
        {
            if (_triggerLinkDict.TryGetValue(getAftTimeName(eventName), out var list))
                return list;
            else
                return Enumerable.Empty<SyncTrigger>();
        }
        public bool unregTrigAft(string eventName, SyncTrigger trigger)
        {
            if (_triggerLinkDict.TryGetValue(getAftTimeName(eventName), out var list))
                return list.Remove(trigger);
            else
                return false;
        }
        #endregion
        #region 私有成员
        string getBfrTimeName(string eventName)
        {
            return "Before" + eventName;
        }
        string getAftTimeName(string eventName)
        {
            return "After" + eventName;
        }
        void initDoEventActions()
        {
            _doTrigLoopBfrAction = new ADoTrigLoopBfr();
            _gotoBfrLoopAction = new AGoto(_doTrigLoopBfrAction);
            _doTrigLoopAftAction = new ADoTrigLoopAft();
            _gotoAftLoopAction = new AGoto(_doTrigLoopAftAction);
        }
        abstract class ADoTrigLoop : SyncAction
        {
            public override void execute(CardEngine game)
            {
                //执行一个触发器
                //获取已经执行的触发器集合
                HashSet<SyncTrigger> executedTriggerSet = game.trigger.currentTask.context.getVar<HashSet<SyncTrigger>>(nameof(executedTriggerSet));
                if (executedTriggerSet == null)
                {
                    executedTriggerSet = new HashSet<SyncTrigger>();
                    game.trigger.currentTask.context.setVar(nameof(executedTriggerSet), executedTriggerSet);
                }
                //获取触发器集合
                var triggers = getTriggers(game);
                triggers = triggers.Where(trigFilter);
                bool trigFilter(SyncTrigger t)
                {
                    return !executedTriggerSet.Contains(t) &&//这个触发器没有被执行过
                        (t.condition == null || t.condition.evaluate(game));//这个触发器符合条件
                }
                triggers = triggers.OrderByDescending(t => t.getPrior != null ? t.getPrior.evaluate(game) : 0);//按照优先级升序排列
                //获取当前要执行的触发器
                var trigger = triggers.FirstOrDefault();
                if (trigger != null)
                {
                    //有的话就执行触发器内容
                    game.trigger.doTask(game.trigger.currentTask.context, trigger.actions);
                    executedTriggerSet.Add(trigger);
                }
                else
                {
                    //没有的话就退出循环
                    game.trigger.currentTask.curActionIndex = game.trigger.currentTask.actions.indexOf(this) + 1;
                }
            }
            protected abstract IEnumerable<SyncTrigger> getTriggers(CardEngine game);
        }
        class ADoTrigLoopBfr : ADoTrigLoop
        {
            protected override IEnumerable<SyncTrigger> getTriggers(CardEngine game)
            {
                return game.trigger.getTrigBfr(game.trigger.currentTask.context.name);
            }
        }
        class ADoTrigLoopAft : ADoTrigLoop
        {
            protected override IEnumerable<SyncTrigger> getTriggers(CardEngine game)
            {
                return game.trigger.getTrigAft(game.trigger.currentTask.context.name);
            }
        }
        ADoTrigLoopBfr _doTrigLoopBfrAction;
        AGoto _gotoBfrLoopAction;
        ADoTrigLoopAft _doTrigLoopAftAction;
        AGoto _gotoAftLoopAction;
        Dictionary<string, LinkedList<SyncTrigger>> _triggerLinkDict = new Dictionary<string, LinkedList<SyncTrigger>>();
        #endregion
    }
    public class EventContext : IDictionary<string, object>
    {
        #region 公共成员
        public EventContext(string name)
        {
            this.name = name;
        }
        public void Add(string key, object value)
        {
            varDict.Add(key, value);
        }
        public void setVar(string varName, object value)
        {
            varDict[varName] = value;
        }
        public bool hasVar(string varName)
        {
            return varDict.ContainsKey(varName);
        }
        public T getVar<T>(string varName)
        {
            if (varDict.TryGetValue(varName, out var v) && v is T t)
                return t;
            else
                return default;
        }
        public object this[string key] { get => varDict[key]; set => varDict[key] = value; }
        public string name;
        public Dictionary<string, object> varDict = new Dictionary<string, object>();
        #endregion
        #region 私有成员
        ICollection<string> IDictionary<string, object>.Keys => varDict.Keys;
        ICollection<object> IDictionary<string, object>.Values => varDict.Values;
        int ICollection<KeyValuePair<string, object>>.Count => varDict.Count;
        bool ICollection<KeyValuePair<string, object>>.IsReadOnly => ((ICollection<KeyValuePair<string, object>>)varDict).IsReadOnly;
        bool IDictionary<string, object>.ContainsKey(string key)
        {
            return varDict.ContainsKey(key);
        }
        bool IDictionary<string, object>.Remove(string key)
        {
            return varDict.Remove(key);
        }
        bool IDictionary<string, object>.TryGetValue(string key, out object value)
        {
            return varDict.TryGetValue(key, out value);
        }
        void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item)
        {
            ((ICollection<KeyValuePair<string, object>>)varDict).Add(item);
        }
        void ICollection<KeyValuePair<string, object>>.Clear()
        {
            varDict.Clear();
        }
        bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item)
        {
            return ((ICollection<KeyValuePair<string, object>>)varDict).Contains(item);
        }
        void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, object>>)varDict).CopyTo(array, arrayIndex);
        }
        bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item)
        {
            return ((ICollection<KeyValuePair<string, object>>)varDict).Remove(item);
        }
        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        {
            return varDict.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return varDict.GetEnumerator();
        }
        #endregion
    }
    public class SyncTrigger
    {
        public SyncFunc<int> getPrior { get; set; }
        public SyncFunc<bool> condition { get; set; }
        public ActionCollection actions { get; set; }
        public SyncTrigger(SyncFunc<int> getPrior, SyncFunc<bool> condition, ActionCollection actions)
        {
            this.getPrior = getPrior;
            this.condition = condition;
            this.actions = actions;
        }
        public SyncTrigger(Func<CardEngine, int> getPrior = null, Func<CardEngine, bool> condition = null, params Action<CardEngine>[] actions) : this(
            getPrior != null ? new FLambda<int>(getPrior) : null,
            condition != null ? new FLambda<bool>(condition) : null,
            new ActionCollection(actions))
        {
        }
        public SyncTrigger(Func<CardEngine, int> getPrior, params Action<CardEngine>[] actions) : this(getPrior, null, actions)
        {
        }
        public SyncTrigger(Func<CardEngine, bool> condition, params Action<CardEngine>[] actions) : this(null, condition, actions)
        {
        }
        public SyncTrigger(params Action<CardEngine>[] actions) : this(null, null, actions)
        {
        }
    }
    public abstract class SyncFunc<T>
    {
        public abstract T evaluate(CardEngine game);
    }
    public class FLambda<T> : SyncFunc<T>
    {
        public Func<CardEngine, T> func;
        public FLambda(Func<CardEngine, T> func)
        {
            this.func = func;
        }
        public override T evaluate(CardEngine game)
        {
            if (func == null)
                return default;
            return func(game);
        }
        public static readonly FLambda<T> Default = new FLambda<T>(null);
    }
}