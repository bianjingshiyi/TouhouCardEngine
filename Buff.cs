using TouhouCardEngine.Interfaces;
namespace TouhouCardEngine
{
    public abstract class Buff
    {
        public abstract int id { get; }
        public abstract PropModifier[] modifiers { get; }
        public abstract IPileEffect[] effects { get; }
        public abstract Buff clone();
    }
}