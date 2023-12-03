using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public class EffectTrigger : Trigger
    {
        public EffectTrigger(IGame game, ICard card, IBuff buff, IEventEffect effect)
        {
            this.game = game;
            this.card = card;
            this.buff = buff;
            this.effect = effect;
        }
        public override bool checkCondition(IEventArg arg)
        {
            return effect.checkCondition(game, card, buff, arg);
        }
        public override Task invoke(IEventArg arg)
        {
            return effect.execute(game, card, buff, arg);
        }
        public IGame game;
        public ICard card;
        public IBuff buff;
        public IEventEffect effect;
    }
}
