namespace TouhouCardEngine
{
    public abstract class Buff
    {
        public abstract int id { get; }
        public abstract PropModifier[] modifiers { get; }
    }
}