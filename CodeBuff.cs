using System;
using System.Linq;
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
            _modifiers.AddRange(modifiers);
        }
        public CodeBuff(int id, params IPassiveEffect[] effects) : base()
        {
            this.id = id;
            _effects.AddRange(effects);
        }
        public CodeBuff(int id, PropModifier modifier, params IPassiveEffect[] effects) : base()
        {
            this.id = id;
            _modifiers.Add(modifier);
            _effects.AddRange(effects);
        }
        private CodeBuff(CodeBuff origin) : base(origin)
        {
            id = origin.id;
        }
        #endregion
        public override Buff clone()
        {
            return new CodeBuff(this);
        }
        public override string ToString()
        {
            string s = $"Buff({id})";
            var modifiers = _modifiers;
            var effects = _effects;
            if (modifiers.Count > 0 || effects.Count > 0)
            {
                s += "{";
                if (modifiers.Count > 0)
                {
                    s += "modifier:" + string.Join(",", modifiers) + ";";
                }
                if (modifiers.Count > 0)
                {
                    s += "effects:" + string.Join(",", effects) + ";";
                }
                s += "}";
            }
            return s;
        }
        #endregion
        #region 属性字段
        [Obsolete("使用效果中的buff替代")]
        public override int id { get; } = 0;
        #endregion
    }
}