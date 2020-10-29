using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public partial class SyncTriggerSystem
    {
        #region 公共成员
        /// <summary>
        /// 事件列表
        /// 总之需要一个用来存储事件的表
        /// 用来存储已经定义好的事件的
        /// </summary>
        public LinkedList<EventContext> eventList = new LinkedList<EventContext>();
        /// <summary>
        /// doEvent
        /// </summary>
        /// <param name="context">事件</param>
        /// <param name="actions">事件效果</param>
        /// <returns></returns>
        public SyncTask doEvent(EventContext context, params Action<CardEngine>[] actions)
        {
            
            //是否被取消
            if (context.hasVar(EventContext.IS_CANCEL) && context.getVar<bool>(EventContext.IS_CANCEL))
            {
                return null;
            }
            
            //是否效果被修改
            if(context.hasVar(EventContext.NEW_ACTIONS) && context.getVar<Action<CardEngine>[]>(EventContext.NEW_ACTIONS) != null)
            {
                doTask(context, actions);
            }
          
            return doTask(context, actions);

        }

        /// <summary>
        /// “在事件之前”注册触发器
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="trigger"></param>
        public void regTrigBfr(string eventName, SyncTrigger trigger)
        {
            EventContext context = null;
            //检查已存在列表中是否有对应事件，如果没有，创建一个
            if (!eventList.Any( e => e.name == eventName))
            {
                context = new EventContext(eventName);
                eventList.AddLast(context);
            }
            else
            {
                context = eventList.Where(e => e.name == eventName).LastOrDefault();
            }

            List<SyncTrigger> triggers = null;
            //往事件的“before”注册触发器
            if (!context.hasVar(EventContext.BEFORE))
            {
                triggers = new List<SyncTrigger>();
                triggers.Add(trigger);
                context.Add(EventContext.BEFORE, triggers);
            }
            else
            {
                triggers = context.getVar<List<SyncTrigger>>(EventContext.BEFORE);
                triggers.Add(trigger);
            }
        }

        /// <summary>
        /// 获取一个事件的“在事件之前”所有的触发器
        /// </summary>
        /// <param name="eventName"></param>
        /// <returns></returns>
        public IEnumerable<SyncTrigger> getTrigBfr(string eventName)
        {
            EventContext context = null;
            //检查已存在列表中是否有对应事件，如果没有，那就没了
            if (!eventList.Any(e => e.name == eventName))
            {
                return null;
            }
            else
            {
                context = eventList.Where(e => e.name == eventName).LastOrDefault();
            }

            if (!context.hasVar(EventContext.BEFORE))
            {
                return null;
            }

            IEnumerable<SyncTrigger> list = context.getVar<List<SyncTrigger>>(EventContext.BEFORE);
            return list;

        }

        /// <summary>
        /// 删除指定事件“在事件之前”的指定触发器
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="trigger"></param>
        /// <returns></returns>
        public bool unregTrigBfr(string eventName, SyncTrigger trigger)
        {
            EventContext context = null;
            //检查已存在列表中是否有对应事件，如果没有，那返回false
            if (!eventList.Any(e => e.name == eventName))
            {
                return false;
            }
            else
            {
                context = eventList.Where(e => e.name == eventName).LastOrDefault();
            }

            if (context.hasVar(EventContext.BEFORE) &&
                context.getVar<List<SyncTrigger>>(EventContext.BEFORE) != null &&
                context.getVar<List<SyncTrigger>>(EventContext.BEFORE).Any(e => e == trigger))
            {
                context.getVar<List<SyncTrigger>>(EventContext.BEFORE).Remove(trigger);
                return true;
            }
            else
            {
                return false;
            }
        }
        public void regTrigAft(string eventName, SyncTrigger trigger)
        {
            EventContext context = null;
            //检查已存在列表中是否有对应事件，如果没有，创建一个
            if (!eventList.Any(e => e.name == eventName))
            {
                context = new EventContext(eventName);
                eventList.AddLast(context);
            }
            else
            {
                context = eventList.Where(e => e.name == eventName).LastOrDefault();
            }

            List<SyncTrigger> triggers = null;
            //往事件的“before”注册触发器
            if (!context.hasVar(EventContext.AFTER))
            {
                triggers = new List<SyncTrigger>();
                triggers.Add(trigger);
                context.Add(EventContext.AFTER, triggers);
            }
            else
            {
                triggers = context.getVar<List<SyncTrigger>>(EventContext.AFTER);
                triggers.Add(trigger);
            }
        }
        public IEnumerable<SyncTrigger> getTrigAft(string eventName)
        {
            EventContext context = null;
            //检查已存在列表中是否有对应事件，如果没有，那就没了
            if (!eventList.Any(e => e.name == eventName))
            {
                return null;
            }
            else
            {
                context = eventList.Where(e => e.name == eventName).LastOrDefault();
            }

            if (!context.hasVar(EventContext.AFTER))
            {
                return null;
            }

            IEnumerable<SyncTrigger> list = context.getVar<List<SyncTrigger>>(EventContext.AFTER);
            return list;
        }
        public bool unregTrigAft(string eventName, SyncTrigger trigger)
        {
            EventContext context = null;
            //检查已存在列表中是否有对应事件，如果没有，那返回false
            if (!eventList.Any(e => e.name == eventName))
            {
                return false;
            }
            else
            {
                context = eventList.Where(e => e.name == eventName).LastOrDefault();
            }

            if (context.hasVar(EventContext.AFTER) &&
                context.getVar<List<SyncTrigger>>(EventContext.AFTER) != null &&
                context.getVar<List<SyncTrigger>>(EventContext.AFTER).Any(e => e == trigger))
            {
                context.getVar<List<SyncTrigger>>(EventContext.AFTER).Remove(trigger);
                return true;
            }
            else
            {
                return false;
            }
        }
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

        #region 常量
        /// <summary>
        /// IEnumerable<SyncTrigger>,“事件发生之前”
        /// </summary>
        public const string BEFORE = "before";
        /// <summary>
        /// IEnumerable<SyncTrigger>,“事件发生之后”
        /// </summary>
        public const string AFTER = "after";
        /// <summary>
        /// bool,是否被取消
        /// </summary>
        public const string IS_CANCEL = "isCancel";
        /// <summary>
        /// Action<CardEngine>[],用来替代事件原效果的新效果
        /// </summary>
        public const string NEW_ACTIONS = "newActions";
        #endregion

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
        SyncFunc<int> getPrior { get; set; }
        SyncFunc<bool> condition { get; set; }
        ActionCollection actions { get; set; }
        public SyncTrigger(SyncFunc<int> getPrior, SyncFunc<bool> condition, ActionCollection actions)
        {
            this.getPrior = getPrior;
            this.condition = condition;
            this.actions = actions;
        }
        public SyncTrigger(Func<CardEngine, int> getPrior = null, Func<CardEngine, bool> condition = null, params Action<CardEngine>[] actions) : this(null, null, new ActionCollection(actions))
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
    public class SyncFunc<T>
    {
        public virtual T evaluate(CardEngine game)
        {
            return default;
        }
    }

    //[Serializable]
    //public class RepeatRegistrationException_Sync : Exception
    //{
    //    public RepeatRegistrationException_Sync() { }
    //    public RepeatRegistrationException_Sync(string eventName, SyncTrigger trigger) : base(trigger + "重复注册事件" + eventName)
    //    { }
    //    public RepeatRegistrationException_Sync(string message) : base(message) { }
    //    public RepeatRegistrationException_Sync(string message, Exception inner) : base(message, inner) { }
    //    protected RepeatRegistrationException_Sync(
    //      System.Runtime.Serialization.SerializationInfo info,
    //      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    //}
}