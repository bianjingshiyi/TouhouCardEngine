using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using TouhouCardEngine.Interfaces;
using MongoDB.Bson;

namespace TouhouCardEngine
{
    public class TriggerManager : MonoBehaviour, ITriggerManager, IDisposable
    {
        public Interfaces.ILogger logger { get; set; } = null;
        [Serializable]
        public class EventListItem
        {
            [SerializeField]
            string _eventName;
            public string eventName
            {
                get { return _eventName; }
            }
            [SerializeField]
            List<TriggerListItem> _triggerList = new List<TriggerListItem>();
            public List<TriggerListItem> triggerList
            {
                get { return _triggerList; }
            }
            public EventListItem(string eventName)
            {
                _eventName = eventName;
            }
        }
        [Serializable]
        public class TriggerListItem
        {
            public ITrigger trigger { get; }
            public TriggerListItem(ITrigger trigger)
            {
                this.trigger = trigger;
            }
        }
        [SerializeField]
        List<EventListItem> _eventList = new List<EventListItem>();
        /// <summary>
        /// 注册触发器
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="trigger"></param>
        public void register(string eventName, ITrigger trigger)
        {
            EventListItem eventItem = _eventList.FirstOrDefault(ei => ei.eventName == eventName);
            if (eventItem == null)
            {
                eventItem = new EventListItem(eventName);
                _eventList.Add(eventItem);
            }
            if (!eventItem.triggerList.Any(ti => ti.trigger == trigger))
            {
                TriggerListItem item = new TriggerListItem(trigger);
                eventItem.triggerList.Add(item);
                logger?.log("Trigger", "注册触发器" + trigger);
                if (doEventNames != null && doEventNames.Contains(eventName))
                {
                    EventListItem insertEventItem = _insertEventList.FirstOrDefault(ei => ei.eventName == eventName);
                    if (insertEventItem == null)
                    {
                        insertEventItem = new EventListItem(eventName);
                        _insertEventList.Add(insertEventItem);
                    }
                    insertEventItem.triggerList.Add(item);
                    logger?.log("Trigger", "注册插入触发器" + trigger);
                }
            }
            else
                throw new RepeatRegistrationException(eventName, trigger);
        }
        [SerializeField]
        List<EventListItem> _insertEventList = new List<EventListItem>();
        public bool remove(string eventName, ITrigger trigger)
        {
            EventListItem eventItem = _eventList.FirstOrDefault(ei => ei.eventName == eventName);
            if (eventItem == null)
                return false;
            else
                return eventItem.triggerList.RemoveAll(ti => ti.trigger == trigger) > 0;
        }
        public ITrigger[] getTriggers(string eventName)
        {
            EventListItem eventItem = _eventList.FirstOrDefault(ei => ei.eventName == eventName);
            if (eventItem == null)
                return new ITrigger[0];
            return eventItem.triggerList.Select(ti => ti.trigger).ToArray();
        }
        public IEventArg getEventArg(string[] eventNames, object[] args)
        {
            return new GeneratedEventArg(eventNames, args);
        }
        public string getName<T>() where T : IEventArg
        {
            return typeof(T).FullName;
        }
        public string getName(IEventArg eventArg)
        {
            return eventArg.GetType().FullName;
        }
        public void register<T>(ITrigger<T> trigger) where T : IEventArg
        {
            register(getName<T>(), trigger);
        }
        public bool remove<T>(ITrigger<T> trigger) where T : IEventArg
        {
            return remove(getName<T>(), trigger);
        }
        public ITrigger<T>[] getTriggers<T>() where T : IEventArg
        {
            return getTriggers(getName<T>()).Where(t => t is ITrigger<T>).Cast<ITrigger<T>>().ToArray();
        }
        public string getNameBefore<T>() where T : IEventArg
        {
            return "Before" + getName<T>();
        }
        public string getNameBefore(IEventArg eventArg)
        {
            return "Before" + getName(eventArg);
        }
        public string getNameAfter<T>() where T : IEventArg
        {
            return "After" + getName<T>();
        }
        public string getNameAfter(IEventArg eventArg)
        {
            return "After" + getName(eventArg);
        }
        public void registerBefore<T>(ITrigger<T> trigger) where T : IEventArg
        {
            register(getNameBefore<T>(), trigger);
        }
        public void registerAfter<T>(ITrigger<T> trigger) where T : IEventArg
        {
            register(getNameAfter<T>(), trigger);
        }
        public void registerAfter<T>(Trigger<T> trigger) where T : IEventArg
        {
            registerAfter(trigger as ITrigger<T>);
        }
        public bool removeBefore<T>(ITrigger<T> trigger) where T : IEventArg
        {
            return remove(getNameBefore<T>(), trigger);
        }
        public bool removeAfter<T>(ITrigger<T> trigger) where T : IEventArg
        {
            return remove(getNameAfter<T>(), trigger);
        }
        public ITrigger<T>[] getTriggersBefore<T>() where T : IEventArg
        {
            return getTriggers(getNameBefore<T>()).Where(t => t is ITrigger<T>).Cast<ITrigger<T>>().ToArray();
        }
        public ITrigger<T>[] getTriggersAfter<T>() where T : IEventArg
        {
            return getTriggers(getNameAfter<T>()).Where(t => t is ITrigger<T>).Cast<ITrigger<T>>().ToArray();
        }
        public Task<T> doEvent<T>(string[] eventNames, T eventArg, params object[] args) where T : IEventArg
        {
            eventArg.afterNames = eventNames;
            eventArg.args = args;
            return doEvent(eventArg);
        }
        public Task doEvent(string[] eventNames, object[] args)
        {
            return doEvent(getEventArg(eventNames, args));
        }
        public Task doEvent(string eventName, params object[] args)
        {
            return doEvent(new string[] { eventName }, args);
        }
        public Task<T> doEvent<T>(string[] beforeNames, string[] afterNames, T eventArg, Func<T, Task> action, params object[] args) where T : IEventArg
        {
            eventArg.beforeNames = beforeNames;
            eventArg.afterNames = afterNames;
            eventArg.args = args;
            return doEvent(eventArg, action);
        }
        public async Task<T> doEvent<T>(T eventArg) where T : IEventArg
        {
            if (eventArg == null)
                throw new ArgumentNullException(nameof(eventArg));
            //加入事件链
            EventArgItem eventArgItem = new EventArgItem(eventArg);
            if (currentEvent != null)
                eventArg.parent = currentEvent;
            _eventChainList.Add(eventArgItem);
            _eventRecordList.Add(eventArgItem);
            try
            {
                onEventBefore?.Invoke(eventArg);
            }
            catch (Exception e)
            {
                logger?.logError("Trigger", "执行" + eventArg + "发生前回调引发异常：" + e);
            }
            //获取事件名
            doEventNames = eventArg.afterNames;
            if (doEventNames == null)
                doEventNames = new string[] { getName<T>() };
            else
                doEventNames = doEventNames.Concat(new string[] { getName<T>() }).Distinct().ToArray();
            //对注册事件进行排序
            List<ITrigger> triggerList = new List<ITrigger>();
            foreach (string eventName in doEventNames)
            {
                EventListItem eventItem = _eventList.FirstOrDefault(ei => ei.eventName == eventName);
                if (eventItem == null)
                    continue;
                eventItem.triggerList.Sort((a, b) => a.trigger.compare(b.trigger, eventArg));
                triggerList.AddRange(eventItem.triggerList.Select(ti => ti.trigger));
            }
            triggerList.Sort((a, b) => a.compare(b, eventArg));
            //执行注册事件
            while (triggerList.Count > 0)
            {
                ITrigger trigger = triggerList[0];
                triggerList.RemoveAt(0);
                if (trigger is ITrigger<T> triggerT)
                {
                    logger?.log("Trigger", "运行触发器" + triggerT);
                    try
                    {
                        await triggerT.invoke(eventArg);
                    }
                    catch (Exception e)
                    {
                        logger?.logError("Trigger", "运行触发器" + triggerT + "引发异常：" + e);
                    }
                }
                else
                {
                    logger?.log("Trigger", "运行触发器" + trigger);
                    try
                    {
                        await trigger.invoke(eventArg);
                    }
                    catch (Exception e)
                    {
                        logger?.logError("Trigger", "运行触发器" + trigger + "引发异常：" + e);
                    }
                }
                if (_insertEventList.Count > 0)
                {
                    foreach (string eventName in doEventNames)
                    {
                        EventListItem insertEventItem = _insertEventList.FirstOrDefault(ei => ei.eventName == eventName);
                        if (insertEventItem == null)
                            continue;
                        if (insertEventItem.triggerList.Count > 0)
                        {
                            EventListItem eventItem = _eventList.FirstOrDefault(ei => ei.eventName == eventName);
                            if (eventItem != null)
                                eventItem.triggerList.Sort((a, b) => a.trigger.compare(b.trigger, eventArg));
                            triggerList.AddRange(insertEventItem.triggerList.Select(ti => ti.trigger));
                            logger?.log("运行中插入触发器" + string.Join("，", insertEventItem.triggerList.Select(ti => ti.trigger)));
                            triggerList.Sort((a, b) => a.compare(b, eventArg));
                            _insertEventList.Remove(insertEventItem);
                        }
                    }
                    _insertEventList.Clear();
                }
            }
            doEventNames = null;
            try
            {
                onEventAfter?.Invoke(eventArg);
            }
            catch (Exception e)
            {
                logger?.logError("Trigger", "执行" + eventArg + "发生后回调引发异常：" + e);
            }
            //移出事件链
            _eventChainList.Remove(eventArgItem);
            return eventArg;
        }
        string[] doEventNames { get; set; } = null;
        public async Task<T> doEvent<T>(T eventArg, Func<T, Task> action) where T : IEventArg
        {
            if (eventArg == null)
                throw new ArgumentNullException(nameof(eventArg));
            EventArgItem eventArgItem = new EventArgItem(eventArg);
            if (currentEvent != null)
                eventArg.parent = currentEvent;
            _eventChainList.Add(eventArgItem);
            _eventRecordList.Add(eventArgItem);
            eventArg.isCanceled = false;
            eventArg.repeatTime = 0;
            eventArg.action = arg =>
            {
                return action.Invoke((T)arg);
            };
            try
            {
                onEventBefore?.Invoke(eventArg);
            }
            catch (Exception e)
            {
                logger?.log("Trigger", "执行" + eventArg + "发生前回调引发异常：" + e);
            }
            //Before
            doEventNames = eventArg.beforeNames;
            if (doEventNames == null)
                doEventNames = new string[] { getNameBefore<T>() };
            else
                doEventNames = doEventNames.Concat(new string[] { getNameBefore<T>() }).Distinct().ToArray();
            List<ITrigger> triggerList = new List<ITrigger>();
            foreach (string eventName in doEventNames)
            {
                EventListItem eventItem = _eventList.FirstOrDefault(ei => ei.eventName == eventName);
                if (eventItem == null)
                    continue;
                eventItem.triggerList.Sort((a, b) => a.trigger.compare(b.trigger, eventArg));
                triggerList.AddRange(eventItem.triggerList.Select(ti => ti.trigger));
            }
            triggerList.Sort((a, b) => a.compare(b, eventArg));
            while (triggerList.Count > 0)
            {
                ITrigger trigger = triggerList[0];
                triggerList.RemoveAt(0);
                if (eventArg.isCanceled)
                    break;
                if (trigger is ITrigger<T> triggerT)
                {
                    logger?.log("Trigger", "运行触发器" + triggerT);
                    try
                    {
                        await triggerT.invoke(eventArg);
                    }
                    catch (Exception e)
                    {
                        logger?.logError("Trigger", "运行触发器" + triggerT + "引发异常：" + e);
                    }
                }
                else
                {
                    logger?.log("Trigger", "运行触发器" + trigger);
                    try
                    {
                        await trigger.invoke(eventArg);
                    }
                    catch (Exception e)
                    {
                        logger?.logError("Trigger", "运行触发器" + trigger + "引发异常：" + e);
                    }
                }
                if (_insertEventList.Count > 0)
                {
                    foreach (string beforeName in doEventNames)
                    {
                        EventListItem insertEventItem = _insertEventList.FirstOrDefault(ei => ei.eventName == beforeName);
                        if (insertEventItem == null)
                            continue;
                        if (insertEventItem.triggerList.Count > 0)
                        {
                            EventListItem eventItem = _eventList.FirstOrDefault(ei => ei.eventName == beforeName);
                            if (eventItem != null)
                                eventItem.triggerList.Sort((a, b) => a.trigger.compare(b.trigger, eventArg));
                            triggerList.AddRange(insertEventItem.triggerList.Select(ti => ti.trigger));
                            logger?.log("运行中插入触发器" + string.Join("，", insertEventItem.triggerList.Select(ti => ti.trigger)));
                            triggerList.Sort((a, b) => a.compare(b, eventArg));
                            _insertEventList.Remove(insertEventItem);
                        }
                    }
                    _insertEventList.Clear();
                }
            }
            doEventNames = null;
            //Event
            int repeatTime = 0;
            do
            {
                if (eventArg.isCanceled)
                    break;
                if (eventArg.action != null)
                {
                    try
                    {
                        await eventArg.action.Invoke(eventArg);
                    }
                    catch (Exception e)
                    {
                        logger?.log("Error", "执行" + eventArg + "逻辑发生异常：" + e);
                    }
                }
                repeatTime++;
            }
            while (repeatTime <= eventArg.repeatTime);
            //After
            doEventNames = eventArg.afterNames;
            if (doEventNames == null)
                doEventNames = new string[] { getNameAfter<T>() };
            else
                doEventNames = doEventNames.Concat(new string[] { getNameAfter<T>() }).Distinct().ToArray();
            triggerList.Clear();
            foreach (string eventName in doEventNames)
            {
                EventListItem eventItem = _eventList.FirstOrDefault(ei => ei.eventName == eventName);
                if (eventItem == null)
                    continue;
                eventItem.triggerList.Sort((a, b) => a.trigger.compare(b.trigger, eventArg));
                triggerList.AddRange(eventItem.triggerList.Select(ti => ti.trigger));
            }
            triggerList.Sort((a, b) => a.compare(b, eventArg));
            while (triggerList.Count > 0)
            {
                ITrigger trigger = triggerList[0];
                triggerList.RemoveAt(0);
                if (eventArg.isCanceled)
                    break;
                if (trigger is ITrigger<T> triggerT)
                {
                    logger?.log("Trigger", "运行触发器" + triggerT);
                    try
                    {
                        await triggerT.invoke(eventArg);
                    }
                    catch (Exception e)
                    {
                        logger?.logError("Trigger", "运行触发器" + triggerT + "引发异常：" + e);
                    }
                }
                else
                {
                    logger?.log("Trigger", "运行触发器" + trigger);
                    try
                    {
                        await trigger.invoke(eventArg);
                    }
                    catch (Exception e)
                    {
                        logger?.logError("Trigger", "运行触发器" + trigger + "引发异常：" + e);
                    }
                }
                if (_insertEventList.Count > 0)
                {
                    foreach (string afterName in doEventNames)
                    {
                        EventListItem insertEventItem = _insertEventList.FirstOrDefault(ei => ei.eventName == afterName);
                        if (insertEventItem == null)
                            continue;
                        if (insertEventItem.triggerList.Count > 0)
                        {
                            EventListItem eventItem = _eventList.FirstOrDefault(ei => ei.eventName == afterName);
                            if (eventItem != null)
                                eventItem.triggerList.Sort((a, b) => a.trigger.compare(b.trigger, eventArg));
                            logger?.log("Trigger", "插入触发器" + string.Join("，", insertEventItem.triggerList.Select(ti => ti.trigger)));
                            triggerList.AddRange(insertEventItem.triggerList.Select(ti => ti.trigger));
                            triggerList.Sort((a, b) => a.compare(b, eventArg));
                            _insertEventList.Remove(insertEventItem);
                        }
                    }
                    _insertEventList.Clear();
                }
            }
            doEventNames = null;
            try
            {
                onEventAfter?.Invoke(eventArg);
            }
            catch (Exception e)
            {
                logger?.logError("Trigger", "执行" + eventArg + "发生后回调引发异常：" + e);
            }
            _eventChainList.Remove(eventArgItem);
            return eventArg;
        }
        public event Action<IEventArg> onEventBefore;
        public event Action<IEventArg> onEventAfter;
        [Serializable]
        public class EventArgItem
        {
#if UNITY_EDITOR
            [SerializeField]
            string _eventArg;
            [Multiline]
            [SerializeField]
            string _string;
#endif
            public IEventArg eventArg { get; }
            public EventArgItem(IEventArg eventArg)
            {
                this.eventArg = eventArg;
#if UNITY_EDITOR
                _eventArg = eventArg.GetType().Name;
                _string = eventArg.ToString();
#endif
            }
        }
        [SerializeField]
        List<EventArgItem> _eventChainList = new List<EventArgItem>();
        public IEventArg currentEvent => _eventChainList.Count > 0 ? _eventChainList[_eventChainList.Count - 1].eventArg : null;
        public IEventArg[] getEventChain()
        {
            return _eventChainList.Select(ei => ei.eventArg).ToArray();
        }
        [SerializeField]
        List<EventArgItem> _eventRecordList = new List<EventArgItem>();
        public IEventArg[] getRecordedEvents()
        {
            return _eventRecordList.Select(ei => ei.eventArg).ToArray();
        }
        public void Dispose()
        {
            Destroy(gameObject);
        }
    }
    public class Trigger : Trigger<IEventArg>
    {
        public Trigger(Func<object[], Task> action = null, Func<ITrigger, ITrigger, IEventArg, int> comparsion = null) : base(arg =>
        {
            if (action != null)
                return action.Invoke(arg.args);
            else
                return Task.CompletedTask;
        }, comparsion)
        {
        }
    }
    public class Trigger<T> : ITrigger<T> where T : IEventArg
    {
        public Func<ITrigger, ITrigger, IEventArg, int> comparsion { get; set; }
        public Func<T, bool> condition { get; set; }
        public Func<T, Task> action { get; set; }
        public Trigger(Func<T, Task> action = null, Func<ITrigger, ITrigger, IEventArg, int> comparsion = null)
        {
            this.action = action;
            this.comparsion = comparsion;
        }
        public int compare(ITrigger<T> other, T arg)
        {
            return compare(other, arg);
        }
        public int compare(ITrigger other, IEventArg arg)
        {
            if (comparsion != null)
                return comparsion.Invoke(this, other, arg);
            else
                return 0;
        }
        public bool checkCondition(T arg)
        {
            if (condition != null)
                return condition.Invoke(arg);
            else
                return true;
        }
        public bool checkCondition(IEventArg arg)
        {
            return checkCondition((T)arg);
        }
        public Task invoke(T arg)
        {
            if (action != null)
                return action.Invoke(arg);
            else
                return Task.CompletedTask;
        }
        public Task invoke(IEventArg arg)
        {
            if (arg is T t)
                return invoke(t);
            else
                return Task.CompletedTask;
        }
    }
    public class GeneratedEventArg : IEventArg
    {
        public string[] beforeNames { get; set; } = new string[0];
        public string[] afterNames { get; set; } = new string[0];
        public object[] args { get; set; }
        public bool isCanceled { get; set; } = false;
        public int repeatTime { get; set; } = 0;
        public Func<IEventArg, Task> action { get; set; }

        public GeneratedEventArg(string[] eventNames, object[] args)
        {
            afterNames = eventNames;
            this.args = args;
        }
        public List<IEventArg> childEventList { get; } = new List<IEventArg>();
        public IEventArg[] getChildEvents()
        {
            return childEventList.ToArray();
        }
        IEventArg _parnet;
        public IEventArg parent
        {
            get => _parnet;
            set
            {
                _parnet = value;
                if (value is GeneratedEventArg gea)
                    gea.childEventList.Add(this);
            }
        }
        public IEventArg[] children
        {
            get { return childEventList.ToArray(); }
        }
    }
    public static class EventArgExtension
    {
        public static void replaceAction<T>(this T eventArg, Func<T, Task> action) where T : IEventArg
        {
            eventArg.action = arg =>
            {
                if (action != null)
                    return action.Invoke(eventArg);
                else
                    return Task.CompletedTask;
            };
        }
    }
    [Serializable]
    public class RepeatRegistrationException : Exception
    {
        public RepeatRegistrationException() { }
        public RepeatRegistrationException(string eventName, ITrigger trigger) : base(trigger + "重复注册事件" + eventName)
        { }
        public RepeatRegistrationException(string message) : base(message) { }
        public RepeatRegistrationException(string message, Exception inner) : base(message, inner) { }
        protected RepeatRegistrationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
