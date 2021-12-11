using TouhouCardEngine.Interfaces;
using System.Threading.Tasks;
using System;

namespace TouhouCardEngine
{
    [Serializable]
    public abstract class PropModifier : IPropModifier
    {
        public abstract string getPropName();
        public virtual bool checkCondition(IGame game, Card card)
        {
            return true;
        }
        public virtual Task beforeAdd(IGame game, Card card)
        {
            return Task.CompletedTask;
        }
        public virtual Task afterAdd(IGame game, Card card)
        {
            return Task.CompletedTask;
        }
        public virtual Task beforeRemove(IGame game, Card card)
        {
            return Task.CompletedTask;
        }
        public virtual Task afterRemove(IGame game, Card card)
        {
            return Task.CompletedTask;
        }
        public abstract object calc(IGame game, Card card, object value);
        public abstract PropModifier clone();
    }
    [Serializable]
    public abstract class PropModifier<T> : PropModifier
    {
        public override string getPropName()
        { return propertyName; }
        /// <summary>
        /// 设置修改器的修改值。
        /// </summary>
        /// <param name="value"></param>
        public virtual Task<IPropChangeEventArg> setValue(IGame game, Card card, T value)
        {
            if (Equals(this.value, value))//泛型需要用Equals来比较值
            {
                return Task.FromResult(default(IPropChangeEventArg));
            }
            object beforeValue = card.getProp(game, getPropName());
            this.value = value;
            return game.triggers.doEvent<IPropChangeEventArg>(new Card.PropChangeEventArg()
            {
                game = game,
                card = card,
                propName = getPropName(),
                beforeValue = beforeValue,
                value = card.getProp(game, getPropName())
            }, arg => Task.CompletedTask);
        }
        public sealed override object calc(IGame game, Card card, object value)
        {
            if (value is T t)
                return calc(game, card, t);
            else
                return value;
        }
        public abstract T calc(IGame game, Card card, T value);
        #region 属性字段
        public string propertyName;
        public T value;
        #endregion
    }
}