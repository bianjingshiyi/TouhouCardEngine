namespace TouhouCardEngine.Histories
{
    public interface IChangeableBuff
    {
        void setProp(string propName, object value);
        void setPropInvisible(string propName, Player player, bool invisible);
    }
    public interface IChangeableCard : IChangeable
    {
        int id { get; }
        void addBuff(Buff buff);
        void removeBuff(int buffInstanceId);
        IChangeableBuff getBuff(int id);
        void setProp(string propName, object value);
        void setPropInvisible(string propName, Player player, bool invisible);
        void setVisible(Player player, bool visible);
        void setDefine(CardDefine define);
        void moveTo(Pile to, int position);
    }
    public abstract class CardChange : Change<IChangeableCard>
    {
        public CardChange(IChangeableCard target) : base(target) { }
        public override bool compareTarget(IChangeableCard other)
        {
            return targetGeneric.id == other.id;
        }
    }
}
