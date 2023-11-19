using System;
using System.Linq;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;
namespace TouhouCardEngine
{
    public class GeneratedBuff : Buff
    {
        #region 公有方法
        public GeneratedBuff(BuffDefine buffDefine) : base()
        {
            defineRef = new DefineReference(buffDefine.cardPoolId, buffDefine.id);
        }
        public Task onEnable(CardEngine game, Card card)
        {
            BuffDefine buffDefine = game.getBuffDefine(defineRef.cardPoolId, defineRef.defineId);
            return buffDefine.onEnable(game, card, this);
        }
        public Task onDisable(CardEngine game, Card card)
        {
            BuffDefine buffDefine = game.getBuffDefine(defineRef.cardPoolId, defineRef.defineId);
            return buffDefine.onDisable(game, card, this);
        }
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

        public override PropModifier[] getModifiers(CardEngine game)
        {
            if (_modifiers == null)
            {
                GeneratedBuffDefine define = game.getBuffDefine(defineRef.cardPoolId, defineRef.defineId) as GeneratedBuffDefine;
                _modifiers = define.propModifierList.ToArray();
            }
            return _modifiers;
        }
        public override BuffExistLimit[] getExistLimits(CardEngine game)
        {
            if (_existLimits == null)
            {
                GeneratedBuffDefine define = game.getBuffDefine(defineRef.cardPoolId, defineRef.defineId) as GeneratedBuffDefine;
                _existLimits = define.existLimitList.Select(e => new BuffExistLimit(e)).ToArray();
            }
            return _existLimits;
        }
        public override IEffect[] getEffects(CardEngine game)
        {
            GeneratedBuffDefine buffDefine = game.getBuffDefine(defineRef.cardPoolId, defineRef.defineId) as GeneratedBuffDefine;
            return buffDefine.effectList.ToArray();
        }
        /// <summary>
        /// 增益类型Id
        /// </summary>
        public DefineReference defineRef;
        private PropModifier[] _modifiers;
        private BuffExistLimit[] _existLimits;
        #endregion
    }
}