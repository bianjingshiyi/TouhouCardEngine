using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;
namespace TouhouCardEngine
{
    public abstract class GeneratedEffect : IEffect
    {
        #region 公有方法
        #region 构造方法
        public GeneratedEffect(ActionGraph graph)
        {
            this.graph = graph;
        }
        public GeneratedEffect()
        {
        }
        #endregion
        public virtual async Task onEnable(IGame game, ICard card, IBuff buff)
        {
            if (!isDisabled(game, card, buff))
                return;

            if (onEnableAction != null)
            {
                var flow = new Flow(game, card, buff, null, this);
                await game.runActions(flow, onEnableAction);
            }

            // 设置该Effect已被启用。
            card.enableEffect(buff, this);
        }
        public virtual async Task onDisable(IGame game, ICard card, IBuff buff)
        {
            if (isDisabled(game, card, buff))
                return;

            // 设置该Effect已被禁用。
            card.disableEffect(buff, this);

            if (onDisableAction != null)
            {
                var flow = new Flow(game, card, buff, null, this);
                await game.runActions(flow, onDisableAction);
            }
        }
        public virtual bool isDisabled(IGame game, ICard card, IBuff buff)
        {
            return !card.isEffectEnabled(buff, this);
        }
        public T getProp<T>(string name)
        {
            var value = getProp(name);
            if (value is T result)
            {
                return result;
            }
            return default;
        }
        public virtual object getProp(string name)
        {
            if (propDict.TryGetValue(name, out object value))
                return value;
            else
                return null;
        }
        public virtual void setProp(string name, object value)
        {
            propDict[name] = value;
        }
        public virtual bool hasProp(string name)
        {
            return propDict.ContainsKey(name);
        }
        public virtual bool removeProp(string name)
        {
            return propDict.Remove(name);
        }
        public virtual string[] getPropNames()
        {
            return propDict.Keys.ToArray();
        }
        /// <summary>
        /// 遍历效果中的动作节点
        /// </summary>
        /// <param name="action"></param>
        public void traverseActionNode(Action<Node> action)
        {
            if (action == null)
                return;
            foreach (var value in getTraversableProps())
            {
                if (value == null)
                    continue;
                if (value is ITraversable actionNode)
                {
                    actionNode.traverse(action);
                }
                else if (value is IEnumerable<ITraversable> actionNodeCol)
                {
                    foreach (var actionNodeEle in actionNodeCol)
                    {
                        if (actionNodeEle == null)
                            continue;
                        actionNodeEle.traverse(action);
                    }
                }
            }
        }
        public virtual void Init()
        {
        }
        public abstract void setTags(params string[] tags);
        public abstract string[] getTags();
        public abstract bool hasTag(string tag);
        public abstract bool checkCondition(IGame game, ICard card, IBuff buff, IEventArg eventArg);
        public abstract Task execute(IGame game, ICard card, IBuff buff, IEventArg eventArg);
        public abstract SerializableEffect Serialize();
        #endregion
        #region 私有方法
        protected virtual IEnumerable<ITraversable> getTraversableProps()
        {
            if (onEnableAction != null)
                yield return onEnableAction;
            if (onDisableAction != null)
                yield return onDisableAction;
        }
        protected string getEffectName(ICard card, IBuff buff, string eventName)
        {
            return $"{(buff != null ? buff.instanceID.ToString() : string.Empty)}Effect{Array.IndexOf(card.define.getEffects(), this)}{eventName}";
        }
        #endregion
        #region 属性字段
        public string name;
        public ActionGraph graph { get; set; }
        public Dictionary<string, object> propDict = new Dictionary<string, object>();
        public virtual ControlOutput onEnableAction => null;
        public virtual ControlOutput onDisableAction => null;
        #endregion
    }

    [Serializable]
    public abstract class SerializableEffect
    {
        public abstract GeneratedEffect Deserialize(INodeDefiner definer);

        public string name;
        public Dictionary<string, object> propDict;
        public SerializableActionNodeGraph graph;

    }
    /// <summary>
    /// 用于兼容老卡池的数据类。
    /// </summary>
    [Obsolete]
    public class SerializableGeneratedEffect
    {
        public string name;
        public string typeName;
        public Dictionary<string, object> propDict;
        public PileNameCollection pileList;
        public int onEnableRootActionNodeId;
        public int onDisableRootActionNodeId;
        public List<SerializableActionNode> onEnableActionList = new List<SerializableActionNode>();
        public List<SerializableActionNode> onDisalbeActionList = new List<SerializableActionNode>();
        public List<SerializableTrigger> triggerList;
        public EffectTagCollection tagList;
    }
}