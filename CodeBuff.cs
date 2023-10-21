using System;
using TouhouCardEngine.Interfaces;
namespace TouhouCardEngine
{
    public class CodeBuff : Buff
    {
        #region 公有方法
        #region 构造方法
        public CodeBuff(int id, params PropModifier[] modifiers) : base()
        {
            this.id = id;
            this.modifiers = modifiers;
        }
        public CodeBuff(int id, params IPassiveEffect[] effects) : base()
        {
            this.id = id;
            this.effects = effects;
        }
        public CodeBuff(int id, PropModifier modifier, params IPassiveEffect[] effects) : base()
        {
            this.id = id;
            modifiers = (new PropModifier[] { modifier });
            this.effects = effects;
        }
        private CodeBuff(CodeBuff origin)
        {
            id = origin.id;
            modifiers = origin.modifiers;
            effects = origin.effects;
        }
        #endregion
        public override Buff clone()
        {
            return new CodeBuff(this);
        }
        public override string ToString()
        {
            string s = $"Buff({id})";
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
        #endregion
        #region 属性字段
        [Obsolete("使用效果中的buff替代")]
        public override int id { get; } = 0;
        public override int instanceID { get; set; } = -1;

        private readonly PropModifier[] modifiers = new PropModifier[0];

        public override PropModifier[] getPropertyModifiers(CardEngine game)
        {
            return modifiers;
        }

        private readonly IPassiveEffect[] effects = new IPassiveEffect[0];

        public override IEffect[] getEffects(CardEngine game)
        {
            return effects;
        }
        #endregion
    }
}