namespace TouhouCardEngine.Histories
{
    public interface IChangeableBuff : IChangeable
    {
        void setInfo(Card card, int instanceId);
        void setProp(string propName, object value);
    }
    public abstract class BuffChange : Change<IChangeableBuff>
    {
        public BuffChange(IChangeableBuff target) : base(target) { }
    }
}
