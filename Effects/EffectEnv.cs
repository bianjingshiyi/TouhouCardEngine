using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public class EffectEnv
    {
        public EffectEnv(CardEngine game, Card card, CardDefine sourceCardDefine, Buff buff, EventArg eventArg, IEffect effect)
        {
            this.game = game;
            this.card = card;
            this.sourceCardDefine = sourceCardDefine;
            this.buff = buff;
            this.eventArg = eventArg;
            this.effect = effect;
        }
        public FlowEnv toFlowEnv()
        {
            return new FlowEnv(game, card, buff, eventArg, effect);
        }
        public CardEngine game;
        public Card card;
        public CardDefine sourceCardDefine;
        public Buff buff;
        public EventArg eventArg;
        public IEffect effect;
    }
}
