using System;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine.Histories
{
    public interface IChangeablePlayer : IChangeable
    {
        int id { get; }
        void setProp(string name, object value);
        void addPile(Pile pile);
        void removePile(Pile pile);
    }
    public abstract class PlayerChange : Change<IChangeablePlayer>
    {
        public PlayerChange(IChangeablePlayer target) : base(target) { }
        public override bool compareTarget(IChangeablePlayer other)
        {
            return targetGeneric.id == other.id;
        }
    }
}
