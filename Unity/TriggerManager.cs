using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using TouhouCardEngine.Interfaces;
using TouhouCardEngine.Histories;

namespace TouhouCardEngine
{
    public class TriggerManager : MonoBehaviour, ITriggerManager, IDisposable
    {
        #region 公共方法

        #region 注册触发器
        /// <summary>
        /// 注册触发器
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="trigger"></param>
        public void register(string eventName, ITrigger trigger)
        {
            //传入不能为空
            if (string.IsNullOrEmpty(eventName))
                throw new ArgumentException($"{nameof(eventName)}不能为空字符串", nameof(eventName));
            if (trigger == null)
                throw new ArgumentNullException(nameof(trigger));
            // 添加触发器。
            addTrigger(eventName, trigger);
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

        #region 延迟注册触发器
        /// <summary>
        /// 注册触发器。如果在执行流程中，并且处于目标事件内部，则改为延迟注册直到该事件结束。用来防止某些效果因为其外部的效果而触发。
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="trigger"></param>
        public void registerDelayed(string eventName, ITrigger trigger)
        {
            //传入不能为空
            if (string.IsNullOrEmpty(eventName))
                throw new ArgumentException($"{nameof(eventName)}不能为空字符串", nameof(eventName));
            if (trigger == null)
                throw new ArgumentNullException(nameof(trigger));

            // 如果正在执行事件，并且是“XX后”事件，将触发器加入到队列中，并在之后刷新队列。
            if (_executingEvents.Count > 0)
            {
                var argItem = _executingEvents.LastOrDefault(e => eventName == getNameAfter(e.eventArg));
                if (argItem != null)
                {
                    argItem.triggerList.Add(trigger);
                    return;
                }
            }

            // 正常添加触发器。
            addTrigger(eventName, trigger);
        }
        #endregion

        #region 移除触发器
        public bool remove(string eventName, ITrigger trigger)
        {
            EventListItem eventItem = _triggerList.FirstOrDefault(ei => ei.eventName == eventName);
            if (eventItem != null && eventItem.triggerList.RemoveAll(ti => ti == trigger) > 0)
            {
                logger?.logTrace("Trigger", $"注销触发器{trigger}");
                return true;
            }
            else
            {
                var argItem = _executingEvents.FirstOrDefault(e => getNameAfter(e.eventArg) == eventName);
                if (argItem != null && argItem.triggerList.RemoveAll(ti => ti == trigger) > 0)
                {
                    logger?.logTrace("Trigger", $"注销延迟队列中的触发器{trigger}");
                    return true;
                }
            }
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

        #region 获取触发器
        public ITrigger[] getTriggers(string eventName)
        {
            EventListItem eventItem = _triggerList.FirstOrDefault(ei => ei.eventName == eventName);
            if (eventItem == null)
                return new ITrigger[0];
            return eventItem.triggerList.ToArray();
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

        #region 事件名称
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
        #endregion

        #region 执行事件

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
            // 如果该事件在一次事件中执行次数超过上限，停止执行新的动作。
            if (eventOutOfLimit<T>())
                return default;
            EventArgItem eventArgItem = new EventArgItem(eventArg);
            if (currentEvent != null)
                eventArg.setParent(currentEvent);
            var record = new EventRecord(eventArg);
            eventArg.record = record;
            _executingEvents.Add(eventArgItem);
            _eventArgList.Add(eventArg);
            _eventRecordList.Add(record);
            eventArg.isCanceled = false;
            eventArg.repeatTime = 0;

            // 为事件保存当前执行流的节点ID，以得知是哪个节点创建了这个事件。
            if (game is CardEngine engine)
            {
                eventArg.flowNodeId = engine.currentFlow?.currentNode?.id ?? -1;
            }


            // 事件前
            // 获取事件前触发器。
            eventArg.state = EventState.Before;
            var beforeNames = eventArg.beforeNames;
            if (beforeNames == null)
                beforeNames = new string[] { getNameBefore<T>() };
            else
                beforeNames = beforeNames.Concat(new string[] { getNameBefore<T>() }).Distinct().ToArray();
            IEnumerable<ITrigger> beforeTriggers = getEventTriggerList(beforeNames, eventArg);
            // 执行事件前触发器。
            await doEventBefore<T>(beforeTriggers, eventArg);

            // 执行事件。
            eventArg.state = EventState.Logic;
            await doEventFunc(eventArg, record);

            // 事件后
            // 获取事件后触发器。
            eventArg.state = EventState.After;
            var afterNames = eventArg.afterNames;
            if (afterNames == null)
                afterNames = new string[] { getNameAfter<T>() };
            else
                afterNames = afterNames.Concat(new string[] { getNameAfter<T>() }).Distinct().ToArray();
            IEnumerable<ITrigger> afterTriggers = getEventTriggerList(afterNames, eventArg);
            // 执行事件后触发器。
            await doEventAfter<T>(afterTriggers, eventArg);

            flushTriggersAfterEvent(eventArgItem, eventArg);

            _executingEvents.Remove(eventArgItem);
            if (eventArg.isCanceled)
            {
                eventArg.state = EventState.Canceled;
                return default;
            }
            else
            {
                eventArg.state = EventState.Completed;
                return eventArg;
            }
        }

        public async Task<T> doEvent<T>(T eventArg, Func<T, Task> action) where T : IEventArg
        {
            eventArg.action = arg =>
            {
                return action?.Invoke((T)arg);
            };
            return await doEvent(eventArg);
        }
        #endregion

        #region 获取事件

        public IEventArg[] getEventChain()
        {
            return _executingEvents.Select(ei => ei.eventArg).ToArray();
        }
        public IEventArg[] getRecordedEvents(bool includeCanceled = false, bool includeUncompleted = true)
        {
            return getEvents(includeCanceled, includeUncompleted).ToArray();
        }
        public EventRecord[] getEventRecords(bool includeCanceled = false, bool includeUncompleted = true)
        {
            return getRecords(includeCanceled, includeUncompleted).ToArray();
        }
        public EventRecord getEventRecord(IEventArg eventArg, bool includeCanceled = false, bool includeUncompleted = true)
        {
            var record = eventArg.record;
            if (record == null)
                return null;
            if (!includeCanceled && record.isCanceled)
                return null;
            if (!includeUncompleted && !record.isCompleted)
                return null;
            return record;
        }
        public IEventArg getEventArg(string[] eventNames, object[] args)
        {
            return new DynamicEventArg(eventNames, args);
        }

        #endregion

        public void addChange(Change change)
        {
            var eventArg = currentEvent;
            if (eventArg == null)
                return;
            var eventRecord = getEventRecord(eventArg);
            eventRecord.addChange(change);
        }
        public void Dispose()
        {
            Destroy(gameObject);
        }
        #endregion

        #region 私有方法

        #region 触发器
        private void addTrigger(string eventName, ITrigger trigger)
        {
            EventListItem eventItem = _triggerList.FirstOrDefault(ei => ei.eventName == eventName);
            if (eventItem == null)
            {
                eventItem = new EventListItem(eventName);
                _triggerList.Add(eventItem);
            }

            if (!eventItem.triggerList.Any(ti => ti == trigger))
            {
                eventItem.triggerList.Add(trigger);
                logger?.logTrace("Trigger", $"注册触发器{trigger}");
            }
            else
                throw new RepeatRegistrationException(eventName, trigger);
        }
        private async Task runTrigger<T>(ITrigger trigger, IEventArg eventArg) where T : IEventArg
        {
            if (trigger is ITrigger<T> triggerT)
            {
                logger?.logTrace("Trigger", $"运行触发器{triggerT}");
                try
                {
                    await triggerT.invoke(eventArg);
                }
                catch (Exception e)
                {
                    logger?.logError("Trigger", $"运行触发器{triggerT}引发异常：{e}");
                }
            }
            else
            {
                logger?.logTrace("Trigger", $"运行触发器{trigger}");
                try
                {
                    await trigger.invoke(eventArg);
                }
                catch (Exception e)
                {
                    logger?.logError("Trigger", $"运行触发器{trigger}引发异常：{e}");
                }
            }
        }
        private ITrigger[] getEventTriggerList(IEnumerable<string> names, IEventArg eventArg)
        {
            List<ITrigger> triggerList = new List<ITrigger>();
            foreach (string eventName in names)
            {
                EventListItem eventItem = _triggerList.FirstOrDefault(ei => ei.eventName == eventName);
                if (eventItem == null)
                    continue;
                eventItem.triggerList.Sort((a, b) => a.compare(b, eventArg));
                foreach (ITrigger item in eventItem.triggerList)
                {
                    if (item.checkCondition(eventArg))
                        triggerList.Add(item);
                }
            }
            triggerList.Sort((a, b) => a.compare(b, eventArg));
            return triggerList.ToArray();
        }
        /// <summary>
        /// 刷新事件项中的所有触发器队列。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="eventArg"></param>
        private void flushTriggersAfterEvent<T>(EventArgItem argItem, T eventArg) where T : IEventArg
        {
            var eventName = getNameAfter<T>();
            var triggers = argItem.triggerList;
            foreach (var item in triggers)
            {
                if (item == null)
                    continue;
                addTrigger(eventName, item);
            }
            argItem.triggerList.Clear();
        }
        #endregion

        #region 执行事件
        private async Task doEventBefore<T>(IEnumerable<ITrigger> triggers, IEventArg eventArg) where T: IEventArg
        {
            //Callback
            try
            {
                onEventBefore?.Invoke(eventArg);
            }
            catch (Exception e)
            {
                logger?.logTrace("Trigger", $"执行{eventArg}发生前回调引发异常：{e}");
            }
            foreach (var trigger in triggers)
            {
                if (eventArg.isCanceled)
                    break;
                await runTrigger<T>(trigger, eventArg);
            }
        }
        private async Task doEventFunc(IEventArg eventArg, EventRecord record)
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
                    logger?.logError("Error", $"执行{eventArg}逻辑发生异常：{e}");
                }
                repeatTime++;
            }
            while (repeatTime <= eventArg.repeatTime);

            record.isCanceled = eventArg.isCanceled;
            eventArg.Record(game, record);
            eventArg.isCompleted = true;
            record.isCompleted = true;
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
                logger?.logError("Trigger", $"执行{eventArg}发生后回调引发异常：{e}");
            }
            // Event.
            foreach (var trigger in triggers)
            {
                if (eventArg.isCanceled)
                    break;
                await runTrigger<T>(trigger, eventArg);
            }
        }
        #endregion

        private bool eventOutOfLimit<T>() where T : IEventArg
        {
            if (_executingEvents.Count(i => i.eventArg is T) >= MAX_EVENT_TIMES)
            {
                string eventName = getName<T>();
                Debug.LogError($"事件{eventName}的执行次数超出上限！不再执行新的事件。");
                logger?.logError("Trigger", $"事件{eventName}的执行次数超出上限！不再执行新的事件。");
                return true;
            }
            return false;
        }
        private IEnumerable<IEventArg> getEvents(bool includeCanceled, bool includeUncompleted)
        {
            return _eventArgList.Where(e => (includeCanceled || !e.isCanceled) && (includeUncompleted || e.isCompleted));
        }
        private IEnumerable<EventRecord> getRecords(bool includeCanceled, bool includeUncompleted)
        {
            return _eventRecordList.Where(r => (includeCanceled || !r.isCanceled) && (includeUncompleted || r.isCompleted));
        }
        #endregion

        #region 事件
        public event Action<IEventArg> onEventBefore;
        public event Action<IEventArg> onEventAfter;
        #endregion

        #region 属性字段
        public IGame game { get; set; }
        public Shared.ILogger logger { get; set; } = null;
        public IEventArg currentEvent => _executingEvents.LastOrDefault()?.eventArg;
        private List<EventListItem> _triggerList = new List<EventListItem>();
        private List<EventArgItem> _executingEvents = new List<EventArgItem>();
        private List<IEventArg> _eventArgList = new List<IEventArg>();
        private List<EventRecord> _eventRecordList = new List<EventRecord>();
        private const int MAX_EVENT_TIMES = 30;
        #endregion

        #region 内置类
        public class EventArgItem
        {
            public EventArgItem(IEventArg eventArg)
            {
                this.eventArg = eventArg;
            }
            public IEventArg eventArg { get; }
            public List<ITrigger> triggerList { get; } = new List<ITrigger>();
        }

        public class EventListItem
        {
            public EventListItem(string eventName)
            {
                this.eventName = eventName;
            }
            public string eventName { get; }
            public List<ITrigger> triggerList { get; } = new List<ITrigger>();
        }

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
    public class DynamicEventArg : IEventArg
    {
        public DynamicEventArg(string[] eventNames, object[] args)
        {
            afterNames = eventNames;
            this.args = args;
        }
        public IEventArg[] getAllChildEvents()
        {
            return childEventList.ToArray();
        }
        public IEventArg[] getChildEvents(EventState state)
        {
            return getAllChildEvents();
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
        public string[] getVarNames()
        {
            return varDict.Keys.ToArray();
        }
        public void Record(IGame game, EventRecord record)
        {
        }
        public void setParent(IEventArg value)
        {
            parent = value;
            if (value is DynamicEventArg gea)
                gea.childEventList.Add(this);
        }
        public string[] beforeNames { get; set; } = new string[0];
        public string[] afterNames { get; set; } = new string[0];
        public object[] args { get; set; }
        public bool isCompleted { get; set; } = false;
        public bool isCanceled { get; set; } = false;
        public EventRecord record { get; set; }
        public int repeatTime { get; set; } = 0;
        public int flowNodeId { get; set; } = 0;
        public Func<IEventArg, Task> action { get; set; }
        public IEventArg parent { get; private set; }
        public List<IEventArg> childEventList { get; } = new List<IEventArg>();
        public EventState state { get; set; }
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
        public RepeatRegistrationException(string eventName, ITrigger trigger) : base($"{trigger}重复注册事件{eventName}")
        { }
        public RepeatRegistrationException(string message) : base(message) { }
        public RepeatRegistrationException(string message, Exception inner) : base(message, inner) { }
        protected RepeatRegistrationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
