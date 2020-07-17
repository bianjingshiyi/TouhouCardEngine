using TouhouCardEngine.Interfaces;
namespace TouhouCardEngine
{
    public class GeneratedBuff : Buff
    {
        public override int id { get; } = 0;
        public override PropModifier[] modifiers { get; } = new PropModifier[0];
        public override IPassiveEffect[] effects { get; } = new IPassiveEffect[0];
        public GeneratedBuff(int id, params PropModifier[] modifiers)
        {
            this.id = id;
            this.modifiers = modifiers;
        }
        public GeneratedBuff(int id, params IPassiveEffect[] effects)
        {
            this.id = id;
            this.effects = effects;
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