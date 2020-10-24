using TouhouCardEngine.Interfaces;
namespace TouhouCardEngine
{
    public class GeneratedBuff : Buff
    {
        public override int id { get; } = 0;
        public override int instanceID { get; set; } = -1;
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
        public GeneratedBuff(int id, PropModifier modifier, params IPassiveEffect[] effects)
        {
            this.id = id;
            modifiers = new PropModifier[] { modifier };
            this.effects = effects;
        }
        GeneratedBuff(GeneratedBuff origin)
        {
            id = origin.id;
            modifiers = origin.modifiers;
            effects = origin.effects;
        }
        public override Buff clone()
        {
            return new GeneratedBuff(this);
        }
        public override string ToString()
        {
            string s = "Buff(" + id + ")";
            if ((modifiers != null && modifiers.Length > 0) || (effects != null && effects.Length > 0))
            {
                s += "{";
                if (modifiers != null && modifiers.Length > 0)
                {
                    s += "modifier:" + string.Join<PropModifier>(",", modifiers) + ";";
                }
                if (effects != null && effects.Length > 0)
                {
                    s += "effects:" + string.Join<IPassiveEffect>(",", effects) + ";";
                }
                s += "}";
            }
            return s;
        }
    }
}