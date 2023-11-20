using System.Linq;
namespace TouhouCardEngine
{
    public class GeneratedBuff : Buff
    {
        #region 公有方法

        #region 构造器
        public GeneratedBuff(BuffDefine buffDefine) : base()
        {
            _modifiers.AddRange(buffDefine.propModifierList);
            _existLimits.AddRange(buffDefine.existLimitList.Select(e => new BuffExistLimit(e)));
            _effects.AddRange(buffDefine.getEffects());
            defineRef = new DefineReference(buffDefine.cardPoolId, buffDefine.id);
        }
        #endregion

        public override Buff clone()
        {
            return new GeneratedBuff(this);
        }
        #endregion

        #region 私有方法
        private GeneratedBuff(GeneratedBuff originBuff) : base(originBuff)
        {
            defineRef = originBuff.defineRef;
        }
        #endregion

        #region 属性字段
        public override int id => defineRef.defineId;
        /// <summary>
        /// 增益类型Id
        /// </summary>
        public DefineReference defineRef;
        #endregion
    }
}