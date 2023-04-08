using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public class TriggerManager : MonoBehaviour, ITriggerManager, IDisposable
    {
        #region 公共方法
        public void Dispose()
        {
            Destroy(gameObject);
        }

        #region 触发器
        #region 注册
        /// <summary>
        /// 注册触发器
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="trigger"></param>
        public void register(string eventName, ITrigger trigger)
        {
            //传入不能为空
            if (string.IsNullOrEmpty(eventName))
                throw new ArgumentException(nameof(eventName) + "不能为空字符串", nameof(eventName));
            if (trigger == null)
                throw new ArgumentNullException(nameof(trigger));

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
            }
            else
                throw new RepeatRegistrationException(eventName, trigger);
        }

        public void register<T>(ITrigger<T> trigger) where T : IEventArg
        {
            register(getName<T>(), trigger);
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
        #endregion

        #region 移除
        public bool remove(string eventName, ITrigger trigger)
        {
            EventListItem eventItem = _eventList.FirstOrDefault(ei => ei.eventName == eventName);
            if (eventItem == null)
                return false;
            else if (eventItem.triggerList.RemoveAll(ti => ti.trigger == trigger) > 0)
            {
                logger?.log("Trigger", "注销触发器" + trigger);
                return true;
            }
            else
                return false;
        }
        public bool remove<T>(ITrigger<T> trigger) where T : IEventArg
        {
            return remove(getName<T>(), trigger);
        }

        public bool removeBefore<T>(ITrigger<T> trigger) where T : IEventArg
        {
            return remove(getNameBefore<T>(), trigger);
        }

        public bool removeAfter<T>(ITrigger<T> trigger) where T : IEventArg
        {
            return remove(getNameAfter<T>(), trigger);
        }
        #endregion 移除

        #region 获取
        public ITrigger[] getTriggers(string eventName)
        {
            EventListItem eventItem = _eventList.FirstOrDefault(ei => ei.eventName == eventName);
            if (eventItem == null)
                return new ITrigger[0];
            return eventItem.triggerList.Select(ti => ti.trigger).ToArray();
        }
        public ITrigger<T>[] getTriggers<T>() where T : IEventArg
        {
            return getTriggers(getName<T>()).OfType<ITrigger<T>>().ToArray();
        }
        public ITrigger<T>[] getTriggersBefore<T>() where T : IEventArg
        {
            return getTriggers(getNameBefore<T>()).Where(t => t is ITrigger<T>).Cast<ITrigger<T>>().ToArray();
        }

        public ITrigger<T>[] getTriggersAfter<T>() where T : IEventArg
        {
            return getTriggers(getNameAfter<T>()).Where(t => t is ITrigger<T>).Cast<ITrigger<T>>().ToArray();
        }
        #endregion
        #endregion

        #region 事件

        public IEventArg[] getEventChain()
        {
            return _eventChainList.Select(ei => ei.eventArg).ToArray();
        }

        public IEventArg[] getRecordedEvents()
        {
            return _eventRecordList.Select(ei => ei.eventArg).ToArray();
        }

        public IEventArg getEventArg(string[] eventNames, object[] args)
        {
            return new GeneratedEventArg(eventNames, args);
        }

        public string getName<T>() where T : IEventArg
        {
            return getName(typeof(T));
        }

        public string getName(IEventArg eventArg)
        {
            return getName(eventArg.GetType());
        }

        public string getName(Type type)
        {
            string name = type.Name;
            if (name.EndsWith("EventArg"))
                name = string.Intern(name.Substring(0, name.Length - 3));
            return name;
        }

        public string getNameBefore<T>() where T : IEventArg
        {
            return getNameBefore(getName<T>());
        }

        public string getNameBefore(IEventArg eventArg)
        {
            return getNameBefore(getName(eventArg));
        }

        public string getNameBefore(string eventName)
        {
            return string.Intern("Before" + eventName);
        }

        public string getNameAfter<T>() where T : IEventArg
        {
            return getNameAfter(getName<T>());
        }

        public string getNameAfter(IEventArg eventArg)
        {
            return getNameAfter(getName(eventArg));
        }

        public string getNameAfter(string eventName)
        {
            return string.Intern("After" + eventName);
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
            return await doEvent(eventArg, null);
        }

        public async Task<T> doEvent<T>(T eventArg, Func<T, Task> action) where T : IEventArg
        {
            if (eventArg == null)
                throw new ArgumentNullException(nameof(eventArg));
            // 如果该事件在一次事件中执行次数超过上限，停止执行新的动作。
            if (eventOutOfLimit<T>())
                return default;
            EventArgItem eventArgItem = new EventArgItem(eventArg);
            if (currentEvent != null)
                eventArg.parent = currentEvent;
            _eventChainList.Add(eventArgItem);
            _eventRecordList.Add(eventArgItem);
            eventArg.isCanceled = false;
            eventArg.repeatTime = 0;
            eventArg.action = arg =>
            {
                return action?.Invoke((T)arg);
            };

            // 在执行事件前，先获取所有符合条件的触发器。
            // 获取事件前触发器。
            var beforeNames = eventArg.beforeNames;
            if (beforeNames == null)
                beforeNames = new string[] { getNameBefore<T>() };
            else
                beforeNames = beforeNames.Concat(new string[] { getNameBefore<T>() }).Distinct().ToArray();
            IEnumerable<ITrigger> beforeTriggers = getEventTriggerList(beforeNames, eventArg);

            // 获取事件后触发器。
            var afterNames = eventArg.afterNames;
            if (afterNames == null)
                afterNames = new string[] { getNameAfter<T>() };
            else
                afterNames = afterNames.Concat(new string[] { getNameAfter<T>() }).Distinct().ToArray();
            IEnumerable<ITrigger> afterTriggers = getEventTriggerList(afterNames, eventArg);

            // 事件前
            await doEventBefore<T>(beforeTriggers, eventArg);

            // 执行事件
            await doEventFunc(eventArg);

            // 事件后
            // 只有事件前和事件后都合法的“事件后”触发器才会执行。
            var newAfterTriggers = getEventTriggerList(afterNames, eventArg);
            afterTriggers = afterTriggers.Intersect(newAfterTriggers);
            await doEventAfter<T>(afterTriggers, eventArg);

            beforeNames = null;
            _eventChainList.Remove(eventArgItem);
            if (eventArg.isCanceled)
                return default;
            else
                return eventArg;
        }
        #endregion

        #endregion
        #region 私有方法
        private async Task doEventBefore<T>(IEnumerable<ITrigger> triggers, IEventArg eventArg) where T: IEventArg
        {
            foreach (var trigger in triggers)
            {
                if (eventArg.isCanceled)
                    break;
                await runTrigger<T>(trigger, eventArg);
            }
            //Callback
            try
            {
                onEventBefore?.Invoke(eventArg);
            }
            catch (Exception e)
            {
                logger?.log("Trigger", "执行" + eventArg + "发生前回调引发异常：" + e);
            }
        }
        private async Task doEventFunc(IEventArg eventArg)
        {
            int repeatTime = 0;
            do
            {
                if (eventArg.isCanceled)
                    break;
                if (eventArg.action == null)
                    break;
                try
                {
                    await eventArg.action.Invoke(eventArg);
                }
                catch (Exception e)
                {
                    logger?.log("Error", "执行" + eventArg + "逻辑发生异常：" + e);
                }
                repeatTime++;
            }
            while (repeatTime <= eventArg.repeatTime);
        }
        private async Task doEventAfter<T>(IEnumerable<ITrigger> triggers, IEventArg eventArg) where T : IEventArg
        {
            //Callback
            try
            {
                onEventAfter?.Invoke(eventArg);
            }
            catch (Exception e)
            {
                logger?.logError("Trigger", "执行" + eventArg + "发生后回调引发异常：" + e);
            }
            // Event.
            foreach (var trigger in triggers)
            {
                if (eventArg.isCanceled)
                    break;
                await runTrigger<T>(trigger, eventArg);
            }
        }
        private bool eventOutOfLimit<T>() where T : IEventArg
        {
            if (_eventChainList.Count(i => i.eventArg is T) >= MAX_EVENT_TIMES)
            {
                string eventName = getName<T>();
                Debug.LogError($"事件{eventName}的执行次数超出上限！不再执行新的事件。");
                logger?.logError("Trigger", $"事件{eventName}的执行次数超出上限！不再执行新的事件。");
                return true;
            }
            return false;
        }
        private async Task runTrigger<T>(ITrigger trigger, IEventArg eventArg) where T:IEventArg
        {
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
        }
        private ITrigger[] getEventTriggerList(IEnumerable<string> names, IEventArg eventArg)
        {
            List<ITrigger> triggerList = new List<ITrigger>();
            foreach (string eventName in names)
            {
                EventListItem eventItem = _eventList.FirstOrDefault(ei => ei.eventName == eventName);
                if (eventItem == null)
                    continue;
                eventItem.triggerList.Sort((a, b) => a.trigger.compare(b.trigger, eventArg));
                foreach (TriggerListItem item in eventItem.triggerList)
                {
                    if (item.trigger.checkCondition(eventArg))
                        triggerList.Add(item.trigger);
                }
            }
            triggerList.Sort((a, b) => a.compare(b, eventArg));
            return triggerList.ToArray();
        }
        #endregion
        #region 内置类
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

        #endregion
        #region 事件
        public event Action<IEventArg> onEventBefore;
        public event Action<IEventArg> onEventAfter;
        #endregion
        #region 属性字段
        public Shared.ILogger logger { get; set; } = null;
        public IEventArg currentEvent => _eventChainList.Count > 0 ? _eventChainList[_eventChainList.Count - 1].eventArg : null;
        [SerializeField]
        List<EventListItem> _eventList = new List<EventListItem>();
        [SerializeField]
        List<EventArgItem> _eventChainList = new List<EventArgItem>();
        [SerializeField]
        List<EventArgItem> _eventRecordList = new List<EventArgItem>();
        private const int MAX_EVENT_TIMES = 30;
        #endregion
    }
    public class Trigger : Trigger<IEventArg>
    {
        public Trigger(Func<object[], bool> condition = null, Func<object[], Task> action = null, Func<ITrigger, ITrigger, IEventArg, int> comparsion = null, string name = null) : base(
            arg =>
            {
                if (condition != null)
                    return condition.Invoke(new object[] { arg });
                else
                    return true;
            },
            arg =>
            {
                if (action != null)
                    return action.Invoke(new object[] { arg });
                else
                    return Task.CompletedTask;
            }, comparsion, name)
        {
        }
    }
    public class Trigger<T> : ITrigger<T> where T : IEventArg
    {
        public Func<ITrigger, ITrigger, IEventArg, int> comparsion { get; set; }
        public Func<T, bool> condition { get; set; }
        public Func<T, Task> action { get; set; }
        string _name;
        public Trigger(Func<T, bool> condition = null, Func<T, Task> action = null, Func<ITrigger, ITrigger, IEventArg, int> comparsion = null, string name = null)
        {
            this.condition = condition;
            this.action = action;
            this.comparsion = comparsion;
            _name = name;
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
        public override string ToString()
        {
            return string.IsNullOrEmpty(_name) ? base.ToString() : _name;
        }
    }
    public class GeneratedEventArg : IEventArg
    {
        public GeneratedEventArg(string[] eventNames, object[] args)
        {
            afterNames = eventNames;
            this.args = args;
        }
        public IEventArg[] getChildEvents()
        {
            return childEventList.ToArray();
        }
        public object getVar(string varName)
        {
            if (varDict.TryGetValue(varName, out object value))
                return value;
            else
                return null;
        }
        public void setVar(string varName, object value)
        {
            varDict[varName] = value;
        }
        public string[] beforeNames { get; set; } = new string[0];
        public string[] afterNames { get; set; } = new string[0];
        public object[] args { get; set; }
        public bool isCanceled { get; set; } = false;
        public int repeatTime { get; set; } = 0;
        public Func<IEventArg, Task> action { get; set; }
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
        IEventArg _parnet;
        public IEventArg[] children
        {
            get { return childEventList.ToArray(); }
        }
        public List<IEventArg> childEventList { get; } = new List<IEventArg>();
        Dictionary<string, object> varDict { get; } = new Dictionary<string, object>();
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
