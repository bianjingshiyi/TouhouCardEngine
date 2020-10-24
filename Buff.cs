using TouhouCardEngine.Interfaces;
namespace TouhouCardEngine
{
    public abstract class Buff : IBuff
    {
        public abstract int id { get; }
        public abstract int instanceID { get; set; }
        public abstract PropModifier[] modifiers { get; }
        public abstract IPassiveEffect[] effects { get; }
        public abstract Buff clone();
    }
}