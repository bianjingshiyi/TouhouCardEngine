using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;
namespace TouhouCardEngine
{
    [Serializable]
    public abstract class Buff : IBuff
    {
        #region 公有方法
        public Buff()
        {
        }
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
                    //当Buff属性发生改变的时候，如果有属性修正器的属性和Buff关联，则改变它的值
                    foreach (PropModifier propModifier in getPropertyModifiers(game))
                    {
                        if (propModifier.relatedPropName == arg.propName)
                        {
                            propModifier.setValue(game, card, arg.value);
                        }
                    }
                    game.logger?.logTrace("Game", string.Format("{0}的属性{1}=>{2}",
                        arg.getVar<Buff>(PropertyChangeEventArg.VAR_BUFF),
                        arg.getVar<string>(PropertyChangeEventArg.VAR_PROPERTY_NAME),
                        StringHelper.propToString(arg.getVar(PropertyChangeEventArg.VAR_VALUE))));
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
        public Card card;
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
            public Buff buff
            {
                get { return getVar<Buff>(VAR_BUFF); }
                set { setVar(VAR_BUFF, value); }
            }
            public string propName
            {
                get { return getVar<string>(VAR_PROPERTY_NAME); }
                set { setVar(VAR_PROPERTY_NAME, value); }
            }
            public object value
            {
                get { return getVar(VAR_VALUE); }
                set { setVar(VAR_VALUE, value); }
            }
            public object valueBeforeChanged
            {
                get { return getVar(VAR_VALUE_BEFORE_CHANGED); }
                set { setVar(VAR_VALUE_BEFORE_CHANGED, value); }
            }
            public const string VAR_BUFF = "Buff";
            public const string VAR_PROPERTY_NAME = "PropertyName";
            public const string VAR_VALUE = "Value";
            public const string VAR_VALUE_BEFORE_CHANGED = "ValueBeforeChanged";
        }
        #endregion
    }
}