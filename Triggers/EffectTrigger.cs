using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public class EffectTrigger : Trigger
    {
        public EffectTrigger(CardEngine game, Card card, Buff buff, IEventEffect effect)
        {
            this.game = game;
            this.card = card;
            this.buff = buff;
            this.effect = effect;
        }
        public override bool checkCondition(IEventArg arg)
        {
            var effectEnv = new EffectEnv(game, card, card.define, buff, arg as EventArg, effect);
            return effect.checkCondition(effectEnv);
        }
        public override Task invoke(IEventArg arg)
        {
            var effectEnv = new EffectEnv(game, card, card.define, buff, arg as EventArg, effect);
            return effect.onTrigger(effectEnv);
        }
        public CardEngine game;
        public Card card;
        public Buff buff;
        public IEventEffect effect;
    }
}
