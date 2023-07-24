
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public class EffectTriggerEventArg : EventArg
    {
        public EffectTriggerEventArg(Card card, Buff buff, IEffect effect) 
        {
            this.card = card;
            this.buff = buff;
            this.effect = effect;
        }
        public override void Record(IGame game, EventRecord record)
        {
            record.setCardState(VAR_CARD, card);
            record.setVar(VAR_BUFF, buff);
            record.setVar(VAR_EFFECT, effect);
        }
        public Card card
        {
            get => getVar<Card>(VAR_CARD);
            set => setVar(VAR_CARD, value);
        }
        public Buff buff
        {
            get => getVar<Buff>(VAR_BUFF);
            set => setVar(VAR_BUFF, value);
        }
        public IEffect effect
        {
            get => getVar<IEffect>(VAR_EFFECT);
            set => setVar(VAR_EFFECT, value);
        }

        public const string VAR_CARD = "Card";
        public const string VAR_BUFF = "Buff";
        public const string VAR_EFFECT = "Effect";
    }
}
