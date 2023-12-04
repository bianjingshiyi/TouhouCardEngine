using System;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;
using TouhouCardEngine.Interfaces;
using UnityEngine;

namespace TouhouCardEngine
{
    public abstract class PropModifier : IPropModifier
    {
        #region 公有方法
        public async Task updateValue(IGame game, Card card, object beforeCardProp, object beforeValue, object value)
        {
            await updateCardProp(game, card, beforeCardProp);
            await updateModifierValue(game, card, beforeValue, value);
        }
        public object calcProp(IGame game, ICardData card, Buff buff, string propName, object prop)
        {
            if (getPropName() != propName)
                return prop;
            var value = getValue(buff);
            return calc(game, card, prop, value);
        }
        public object getValue(Buff buff)
        {
            if (relatedPropName != null && buff != null)
            {
                return buff.getProp(relatedPropName);
            }
            return getDefaultValue();
        }
        public abstract SerializablePropModifier serialize();

        #region 添加/移除回调
        public virtual Task beforeAdd(IGame game, Card card, Buff buff)
        {
            return Task.CompletedTask;
        }
        public virtual Task afterAdd(IGame game, Card card, Buff buff)
        {
            return Task.CompletedTask;
        }
        public virtual Task beforeRemove(IGame game, Card card, Buff buff)
        {
            return Task.CompletedTask;
        }
        public virtual Task afterRemove(IGame game, Card card, Buff buff)
        {
            return Task.CompletedTask;
        }
        #endregion

        #region 抽象方法
        public abstract object getDefaultValue();
        public abstract object calc(IGame game, ICardData card, object prop, object value);
        #endregion
        public string getPropName()
        {
            return propertyName;
        }

        #endregion

        #region 私有方法
        protected abstract Task updateModifierValue(IGame game, Card card, object beforeValue, object value);
        private Task<Card.PropChangeEventArg> updateCardProp(IGame game, Card card, object beforeCardProp)
        {
            var propName = getPropName();
            var afterCardProp = card.getProp(game, propName);
            if (Equals(beforeCardProp, afterCardProp))
                return Task.FromResult<Card.PropChangeEventArg>(null);
            return game.triggers.doEvent(new Card.PropChangeEventArg()
            {
                card = card,
                propName = propName,
                beforeValue = beforeCardProp,
                value = afterCardProp
            });
        }
        #endregion

        #region 属性字段
        public string relatedPropName = null;
        public string propertyName;
        #endregion
    }
    public abstract class PropModifier<T> : PropModifier
    {
        #region 公有方法

        #region 替换为泛型的重写
        public override sealed object calc(IGame game, ICardData card, object prop, object value)
        {
            T tValue = toGenericValue(value);
            if (prop == null)
                return calcGeneric(game, card, default, tValue);
            else if (prop is T tProp)
                return calcGeneric(game, card, tProp, tValue);
            else
                return prop;
        }
        protected override sealed Task updateModifierValue(IGame game, Card card, object beforeValue, object value)
        {
            return updateModifierValue(game, card, toGenericValue(beforeValue), toGenericValue(value));
        }
        #endregion

        #region 泛型方法
        public T calcProp(IGame game, ICardData card, Buff buff, string propName, T prop)
        {
            if (getPropName() != propName)
                return prop;
            var value = getValueGeneric(buff);
            return calcGeneric(game, card, prop, value);
        }
        public virtual Task beforeAdd(IGame game, Card card, T value) => Task.CompletedTask;
        public virtual Task afterAdd(IGame game, Card card, T value) => Task.CompletedTask;
        public virtual Task beforeRemove(IGame game, Card card, T value) => Task.CompletedTask;
        public virtual Task afterRemove(IGame game, Card card, T value) => Task.CompletedTask;
        public virtual Task updateModifierValue(IGame game, Card card, T beforeValue, T value) => Task.CompletedTask;
        public abstract T calcGeneric(IGame game, ICardData card, T prop, T value);
        #endregion

        public override object getDefaultValue()
        {
            return defaultValue;
        }
        private T getValueGeneric(Buff buff)
        {
            if (relatedPropName != null && buff != null)
            {
                return buff.getProp<T>(relatedPropName);
            }
            return defaultValue;
        }
        #endregion

        #region 私有方法
        private T toGenericValue(object value)
        {
            if (value is null)
                return default;
            if (value is T t)
                return t;
            throw new InvalidCastException($"属性修整器{this}的值类型必须是{typeof(T).Name}");
        }
        #endregion

        #region 属性字段
        public T defaultValue;
        #endregion
    }
    [Serializable]
    public abstract class SerializablePropModifier
    {
        public SerializablePropModifier(PropModifier modifier)
        {
            relatedPropName = modifier.relatedPropName;
            propertyName = modifier.propertyName;
        }
        public abstract PropModifier deserialize();
        public string relatedPropName;
        public string propertyName;
    }
    [Serializable]
    public abstract class SerializablePropModifier<T> : SerializablePropModifier
    {
        public SerializablePropModifier(PropModifier<T> modifier) : base(modifier)
        {
            defaultValue = modifier.defaultValue;
        }
        protected T getDefaultValue()
        {
            var defValue = default(T);
            if (Equals(defaultValue, defValue) && !Equals(value, defValue))
            {
                return value;
            }
            return defaultValue;
        }
        public T defaultValue;
        [Obsolete]
        [BsonIgnoreIfDefault]
        public T value;
    }
}