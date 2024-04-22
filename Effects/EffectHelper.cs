using System;
using System.Linq;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public static class EffectHelper
    {
        /// <summary>
        /// 启用一张卡牌的某个效果。如果是被牌堆限制住的效果，还会根据该卡牌目前的牌堆判断是否应该启用。
        /// </summary>
        /// <param name="effect">要启用的效果。</param>
        /// <param name="game">游戏实例。</param>
        /// <param name="card">效果所依附的卡牌。</param>
        /// <param name="buff">效果所属增益。</param>
        /// <returns></returns>
        public static async Task updateEnable(this Effect effect, CardEngine game, Card card, Buff buff)
        {
            if (effect is IPileRangedEffect pileEffect)
            {
                if (pileEffect.getPiles().ContainsPileOrAny(card.pile?.name))
                    await effect.enable(game, card, buff);
            }
            else
            {
                await effect.enable(game, card, buff);
            }
        }
    }
}
