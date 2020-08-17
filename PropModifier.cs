using TouhouCardEngine.Interfaces;
using System.Threading.Tasks;
namespace TouhouCardEngine
{
    public abstract class PropModifier : IPropModifier
    {
        public abstract string propName { get; }
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
    public abstract class PropModifier<T> : PropModifier
    {
        public sealed override object calc(IGame game, Card card, object value)
        {
            if (value is T t)
                return calc(game, card, t);
            else
                return value;
        }
        public abstract T calc(IGame game, Card card, T value);
    }
}