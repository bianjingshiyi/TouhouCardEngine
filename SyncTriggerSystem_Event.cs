using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TouhouCardEngine
{
    public partial class SyncTriggerSystem
    {
        #region 公共成员
        public SyncTask doEvent(EventContext context, params Action<CardEngine>[] actions)
        {
            ActionCollection returnActions = new ActionCollection();
            string eventName = context.name;
            IEnumerable<SyncTrigger> bfrTriggers = getTrigBfr(eventName);
            IEnumerable<SyncTrigger> aftTriggers = getTrigAft(eventName);
            ActionCollection bfrEventActions = new ActionCollection();
            ActionCollection aftEventActions = new ActionCollection();
            if (bfrTriggers!=null)
            {
                foreach (SyncTrigger trigger in bfrTriggers)
                {
                    if (trigger.condition.evaluate(game))
                    {
                        bfrEventActions.Concat(trigger.actions);
                    }
                }
            }

            if (aftTriggers!=null)
            {
                foreach (SyncTrigger trigger in aftTriggers)
                {
                    if (trigger.condition.evaluate(game))
                    {
                        aftEventActions.Concat(trigger.actions);
                    }
                }
            }
            returnActions.Add(bfrEventActions);
            returnActions.Add(new ActionCollection(actions));
            returnActions.Add(aftEventActions);
            return doTask(context, returnActions);
        }
        public void regTrigBfr(string eventName, SyncTrigger trigger)
        {
            if (_bfrEventTriggerDic.ContainsKey(eventName))
            {
                _bfrEventTriggerDic[eventName].Add(trigger);
            }
            else
            {
                _bfrEventTriggerDic.Add(eventName,new List<SyncTrigger>{trigger});
            }
        }
        public IEnumerable<SyncTrigger> getTrigBfr(string eventName)
        {
            return _bfrEventTriggerDic.ContainsKey(eventName)?_bfrEventTriggerDic[eventName]:null;
        }
        public bool unregTrigBfr(string eventName, SyncTrigger trigger)
        {
            return _bfrEventTriggerDic.ContainsKey(eventName)?_bfrEventTriggerDic[eventName].Remove(trigger):false;
        }
        public void regTrigAft(string eventName, SyncTrigger trigger)
        {
            if (_aftEventTriggerDic.ContainsKey(eventName))
            {
                _aftEventTriggerDic[eventName].Add(trigger);
            }
            else
            {
                _aftEventTriggerDic.Add(eventName,new List<SyncTrigger>{trigger});
            }
        }
        public IEnumerable<SyncTrigger> getTrigAft(string eventName)
        {
            return _aftEventTriggerDic.ContainsKey(eventName)?_aftEventTriggerDic[eventName]:null;
        }
        public bool unregTrigAft(string eventName, SyncTrigger trigger)
        {
            return _aftEventTriggerDic.ContainsKey(eventName)?_aftEventTriggerDic[eventName].Remove(trigger):false;
        }
        Dictionary<string,List<SyncTrigger>> _aftEventTriggerDic = new Dictionary<string,List<SyncTrigger>>();
        Dictionary<string,List<SyncTrigger>> _bfrEventTriggerDic = new Dictionary<string, List<SyncTrigger>>();
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
        public SyncTrigger(Func<CardEngine, int> getPrior = null, Func<CardEngine, bool> condition = null, params Action<CardEngine>[] actions) : this(new SyncFunc<int>(getPrior), new SyncFunc<bool>(condition), new ActionCollection(actions))
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
        public Func<CardEngine,T> func;
        public SyncFunc(Func<CardEngine,T> func)
        {
            this.func = func;
        }
        public virtual T evaluate(CardEngine game)
        {
            if (func == null) return default;
            return func(game);
        }
    }
}