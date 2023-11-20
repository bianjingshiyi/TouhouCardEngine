﻿namespace TouhouCardEngine.Histories
{
    public interface IChangeableBuff
    {
        void setProp(string propName, object value);
    }
    public interface IChangeableCard : IChangeable
    {
        void addBuff(Buff buff);
        void removeBuff(Buff buff);
        IChangeableBuff getBuff(int id);
        void setProp(string propName, object value);
        void setDefine(CardDefine define);
        void moveTo(Pile to, int position);
    }
    public abstract class CardChange : Change<IChangeableCard>
    {
        public CardChange(IChangeableCard target) : base(target) { }
    }
}
