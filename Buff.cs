using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;
namespace TouhouCardEngine
{
    [Serializable]
    public abstract class BuffDefine
    {
        #region 公有方法
        public abstract Task onEnable(CardEngine game, Card card, Buff buff);
        public abstract Task onDisable(CardEngine game, Card card, Buff buff);
        public abstract int getId();
        #endregion
    }
    [Serializable]
    public class GeneratedBuffDefine : BuffDefine
    {
        #region 公有方法
        public GeneratedBuffDefine(int id, IEnumerable<PropModifier> propModifiers = null, IEnumerable<GeneratedEffect> effects = null)
        {
            this.id = id;
            if (propModifiers != null)
                propModifierList.AddRange(propModifiers);
            if (effects != null)
                effectList.AddRange(effects);
        }
        public override async Task onEnable(CardEngine game, Card card, Buff buff)
        {
            for (int i = 0; i < effectList.Count; i++)
            {
                await effectList[i].onEnable(game, card, buff);
            }
        }
        public override async Task onDisable(CardEngine game, Card card, Buff buff)
        {
            for (int i = 0; i < effectList.Count; i++)
            {
                await effectList[i].onEnable(game, card, buff);
            }
        }
        public override int getId()
        {
            return id;
        }
        public override string ToString()
        {
            return string.Intern(string.Format("Buff<{0}>", id));
        }

        #endregion
        #region 属性字段
        #endregion
        #region 属性字段
        public int id = 0;
        public List<PropModifier> propModifierList = new List<PropModifier>();
        public List<GeneratedEffect> effectList = new List<GeneratedEffect>();
        #endregion
    }
    [Serializable]
    public class PropertyModifyInfo
    {
        public string propName;
        public PropertyModifyType modifyType;
        public object value;
    }
    public enum PropertyModifyType
    {
        set,
        //数字运算
        add,
        sub,
        mul,
        div,
    }
    [Serializable]
    public class GeneratedBuff : Buff
    {
        #region 公有方法
        public GeneratedBuff(BuffDefine buffDefine)
        {
            defineId = buffDefine.getId();
        }
        GeneratedBuff(GeneratedBuff originBuff)
        {
            defineId = originBuff.defineId;
            instanceId = originBuff.instanceId;
        }
        public Task onEnable(CardEngine game, Card card)
        {
            BuffDefine buffDefine = game.getBuffDefine(defineId);
            return buffDefine.onEnable(game, card, this);
        }
        public Task onDisable(CardEngine game, Card card)
        {
            BuffDefine buffDefine = game.getBuffDefine(defineId);
            return buffDefine.onDisable(game, card, this);
        }
        public override Buff clone()
        {
            return new GeneratedBuff(this);
        }
        #endregion
        #region 属性字段
        public override int id => defineId;

        public override int instanceID
        {
            get { return instanceId; }
            set { instanceId = value; }
        }

        public override PropModifier[] getPropertyModifiers(CardEngine game)
        {
            GeneratedBuffDefine buffDefine = game.getBuffDefine(defineId) as GeneratedBuffDefine;
            return buffDefine.propModifierList.ToArray();
        }
        public override IPassiveEffect[] getEffects(CardEngine game)
        {
            GeneratedBuffDefine buffDefine = game.getBuffDefine(defineId) as GeneratedBuffDefine;
            return buffDefine.effectList.ToArray();
        }
        /// <summary>
        /// 增益类型Id
        /// </summary>
        public int defineId = 0;
        public int instanceId = 0;
        #endregion
    }
    [Serializable]
    public abstract class Buff : IBuff
    {
        #region 公有方法
        public object getProp(CardEngine game, string propName)
        {
            if (propDict.TryGetValue(propName, out object value))
            {
                return value;
            }
            return null;
        }
        public T getProp<T>(CardEngine game, string propName)
        {
            if (propDict.TryGetValue(propName, out object value) && value is T t)
                return t;
            else
                return default;
        }
        public Task<PropertyChangeEventArg> setProp(CardEngine game, string propName, object value)
        {
            if (game != null && game.triggers != null)
            {
                return game.triggers.doEvent(new PropertyChangeEventArg(this, propName, value, getProp(game, propName)), arg =>
                {
                    arg.getVar<Buff>(PropertyChangeEventArg.VAR_BUFF).propDict[arg.getVar<string>(PropertyChangeEventArg.VAR_PROPERTY_NAME)] = arg.getVar(PropertyChangeEventArg.VAR_VALUE);
                    game.logger?.logTrace("Game", arg.getVar<Buff>(PropertyChangeEventArg.VAR_BUFF) + "的属性" + arg.getVar<string>(PropertyChangeEventArg.VAR_PROPERTY_NAME) + "=>" + StringHelper.propToString(arg.getVar(PropertyChangeEventArg.VAR_VALUE)));
                    return Task.CompletedTask;
                });
            }
            else
            {
                propDict[propName] = value;
                return Task.FromResult<PropertyChangeEventArg>(default);
            }
        }
        public abstract PropModifier[] getPropertyModifiers(CardEngine game);
        public abstract IPassiveEffect[] getEffects(CardEngine game);
        public abstract Buff clone();
        #endregion
        #region 属性字段
        public abstract int id { get; }
        public abstract int instanceID { get; set; }
        public Dictionary<string, object> propDict = new Dictionary<string, object>();
        #endregion
        #region 嵌套类型
        public class PropertyChangeEventArg : EventArg
        {
            public PropertyChangeEventArg(Buff buff, string propName, object value, object valueBeforeChanged)
            {
                setVar(VAR_BUFF, buff);
                setVar(VAR_PROPERTY_NAME, propName);
                setVar(VAR_VALUE, value);
                setVar(VAR_VALUE_BEFORE_CHANGED, valueBeforeChanged);
            }
            public const string VAR_BUFF = "Buff";
            public const string VAR_PROPERTY_NAME = "PropertyName";
            public const string VAR_VALUE = "Value";
            public const string VAR_VALUE_BEFORE_CHANGED = "ValueBeforeChanged";
        }
        #endregion
    }
}