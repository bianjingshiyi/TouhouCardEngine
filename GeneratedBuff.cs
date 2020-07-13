namespace TouhouCardEngine
{
    public class GeneratedBuff : Buff
    {
        public override int id { get; } = 0;
        public override PropModifier[] modifiers { get; }
        public GeneratedBuff(int id, params PropModifier[] modifiers)
        {
            this.id = id;
            this.modifiers = modifiers;
        }
        GeneratedBuff(GeneratedBuff origin)
        {
            id = origin.id;
            modifiers = origin.modifiers;
        }
        public override Buff clone()
        {
            return new GeneratedBuff(this);
        }
    }
}