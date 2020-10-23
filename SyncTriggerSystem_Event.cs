using System;
using System.Collections;
using System.Collections.Generic;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public partial class SyncTriggerSystem
    {
        #region 公共成员
        public SyncTask doEvent(EventContext context, params Action<CardEngine>[] actions)
        {
            throw new NotImplementedException();
        }
        public void regTrigBfr(string eventName, SyncTrigger trigger)
        {
            throw new NotImplementedException();
        }
        public IEnumerable<SyncTrigger> getTrigBfr(string eventName)
        {
            throw new NotImplementedException();
        }
        public bool unregTrigBfr(string eventName, SyncTrigger trigger)
        {
            throw new NotImplementedException();
        }
        public void regTrigAft(string eventName, SyncTrigger trigger)
        {
            throw new NotImplementedException();
        }
        public IEnumerable<SyncTrigger> getTrigAft(string eventName)
        {
            throw new NotImplementedException();
        }
        public bool unregTrigAft(string eventName, SyncTrigger trigger)
        {
            throw new NotImplementedException();
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
        public SyncTrigger(SyncFunc<int> getPrior, SyncFunc<bool> condition, ActionCollection actions)
        {
            throw new NotImplementedException();
        }
        public SyncTrigger(Func<CardEngine, int> getPrior = null, Func<CardEngine, bool> condition = null, params Action<CardEngine>[] actions) : this(null, null, new ActionCollection(actions))
        {
            throw new NotImplementedException();
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
}