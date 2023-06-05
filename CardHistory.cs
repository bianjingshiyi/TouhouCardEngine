using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public interface ITrackableCard
    {
        void setProp(string propName, object value);
        void setDefine(CardDefine define);
        void moveTo(Pile to, int position);
    }
    public abstract class CardHistory
    {
        public IEventArg eventArg;
        public CardHistory(IEventArg eventArg)
        {
            this.eventArg = eventArg;
        }
        public abstract void apply(ITrackableCard trackable);
        public abstract void revert(ITrackableCard trackable);

    }
    public class CardPropHistory : CardHistory
    {
        public string propName;
        public object beforeValue;
        public object value;
        public CardPropHistory(string propName, object beforeValue, object value, IEventArg eventArg) : base(eventArg)
        {
            this.propName = propName;
            this.beforeValue = beforeValue;
            this.value = value;
        }

        public override void apply(ITrackableCard trackable)
        {
            trackable.setProp(propName, value);
        }
        public override void revert(ITrackableCard trackable)
        {
            trackable.setProp(propName, beforeValue);
        }
    }
    public class CardSetDefineHistory : CardHistory
    {
        public CardDefine beforeDefine;
        public CardDefine define;
        public CardSetDefineHistory(CardDefine beforeDefine, CardDefine define, IEventArg eventArg) : base(eventArg)
        {
            this.beforeDefine = beforeDefine;
            this.define = define;
        }

        public override void apply(ITrackableCard trackable)
        {
            trackable.setDefine(define);
        }
        public override void revert(ITrackableCard trackable)
        {
            trackable.setDefine(beforeDefine);
        }
    }
    public class CardMoveHistory : CardHistory
    {
        public Pile from;
        public int fromPos;
        public Pile to;
        public int toPos;
        public CardMoveHistory(Pile from, Pile to, int fromPos, int toPos, IEventArg eventArg) : base(eventArg)
        {
            this.from = from;
            this.to = to;
            this.fromPos = fromPos;
            this.toPos = toPos;
        }

        public override void apply(ITrackableCard trackable)
        {
            trackable.moveTo(to, toPos);
        }
        public override void revert(ITrackableCard trackable)
        {
            trackable.moveTo(from, fromPos);
        }
    }
}
