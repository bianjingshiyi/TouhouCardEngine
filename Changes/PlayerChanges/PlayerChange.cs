using System;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine.Histories
{
    public interface IChangeablePlayer : IChangeable
    {
        void setProp(string name, object value);
        void addPile(Pile pile);
        void removePile(Pile pile);
    }
    public abstract class PlayerChange : Change<IChangeablePlayer>
    {
        public PlayerChange(IChangeablePlayer target) : base(target) { }
    }
}
