using System;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;
namespace TouhouCardEngine
{
    [Serializable]
    public class GeneratedBuff : Buff
    {
        #region 公有方法
        public GeneratedBuff(BuffDefine buffDefine) : base()
        {
            defineId = buffDefine.getId();
        }
        public Task onEnable(CardEngine game, Card card)
        {
            BuffDefine buffDefine = game.getBuffDefine(defineId);
            return buffDefine.onEnable(game, card, this);
        }
        public Task onDisable(CardEngine game, Card card)
        {
            BuffDefine buffDefine = game.getBuffDefine(defineId);
            return buffDefine.onDisable(game, card, this);
        }
        public override Buff clone()
        {
            return new GeneratedBuff(this);
        }
        #endregion
        #region 私有方法
        private GeneratedBuff(GeneratedBuff originBuff) : base()
        {
            defineId = originBuff.defineId;
            instanceId = originBuff.instanceId;
        }
        #endregion
        #region 属性字段
        public override int id => defineId;

        public override int instanceID
        {
            get { return instanceId; }
            set { instanceId = value; }
        }

        public override PropModifier[] getPropertyModifiers(CardEngine game)
        {
            GeneratedBuffDefine buffDefine = game.getBuffDefine(defineId) as GeneratedBuffDefine;
            return buffDefine.propModifierList.ToArray();
        }
        public override IPassiveEffect[] getEffects(CardEngine game)
        {
            GeneratedBuffDefine buffDefine = game.getBuffDefine(defineId) as GeneratedBuffDefine;
            return buffDefine.effectList.ToArray();
        }
        /// <summary>
        /// 增益类型Id
        /// </summary>
        public int defineId = 0;
        public int instanceId = 0;
        #endregion
    }
}