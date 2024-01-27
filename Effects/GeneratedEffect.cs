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
        #endregion

        public async Task enable(CardEngine game, Card card, Buff buff)
        {
            if (!isDisabled(game, card, buff))
                return;

            await EffectActivationEventDefine.doEvent(game, card, buff, this, true);
        }
        public async Task disable(CardEngine game, Card card, Buff buff)
        {
            if (isDisabled(game, card, buff))
                return;

            await EffectActivationEventDefine.doEvent(game, card, buff, this, false);
            // 设置该Effect已被禁用。
            card.disableEffect(buff, this);

            if (onDisableAction != null)
            {
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
        public abstract Task execute(EffectEnv env);
        public abstract void setTags(params string[] tags);
        public abstract string[] getTags();
        public abstract bool hasTag(string tag);
        public abstract bool checkCondition(EffectEnv env);
        public abstract SerializableEffect Serialize();
        #endregion

        #region 私有方法

        #region 接口实现
        Task IEffect.onEnable(EffectEnv env) => onEnable(env);
        Task IEffect.onDisable(EffectEnv env) => onDisable(env);
        #endregion

        protected virtual Task onEnable(EffectEnv env)
        {
            if (onEnableAction != null)
            {
                var flowEnv = env.toFlowEnv();
                var flow = new Flow(flowEnv);
                return env.game.runActions(flow, onEnableAction);
            }
            return Task.CompletedTask;
        }
        protected virtual Task onDisable(EffectEnv env)
        {
            if (onDisableAction != null)
            {
                var flowEnv = env.toFlowEnv();
                var flow = new Flow(flowEnv);
                return env.game.runActions(flow, onDisableAction);
            }
            return Task.CompletedTask;
        }
        protected virtual IEnumerable<ITraversable> getTraversableProps()
        {
            if (onEnableAction != null)
                yield return onEnableAction;
            if (onDisableAction != null)
                yield return onDisableAction;
        }
        #endregion
        #region 属性字段
        public string name;
        public DefineReference buffDefineRef { get; set;  }
        public DefineReference cardDefineRef { get; set; }
        public ActionGraph graph { get; set; }
        public Dictionary<string, object> propDict = new Dictionary<string, object>();
        public virtual ControlOutput onEnableAction => null;
        public virtual ControlOutput onDisableAction => null;
        public abstract ControlOutput executePort { get; }
        #endregion
    }

    [Serializable]
    public abstract class SerializableEffect
    {
        public SerializableEffect(GeneratedEffect effect)
        {
            name = effect.name;
            propDict = effect.propDict;
            graph = new SerializableActionNodeGraph(effect.graph);
        }
        public abstract GeneratedEffect Deserialize(INodeDefiner definer);
        protected void apply(GeneratedEffect effect)
        {
            effect.name = name;
            effect.propDict = propDict;
        }

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