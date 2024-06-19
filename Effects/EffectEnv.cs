using System.Collections.Generic;
using System.Linq;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public class EffectEnv
    {
        public EffectEnv(CardEngine game, Card card, Buff buff, EventArg eventArg, Effect effect)
        {
            this.game = game;
            this.card = card;
            this.buff = buff;
            this.eventArg = eventArg;
            this.effect = effect;
        }
        public void setArgument(string name, object value)
        {
            arguments[name] = value;
        }
        public object getArgument(string name)
        {
            if (arguments.TryGetValue(name, out var value))
                return value;
            return null;
        }
        public FlowEnv toFlowEnv()
        {
            var env = new FlowEnv(game, card, buff, eventArg, effect);
            foreach (var pair in arguments)
            {
                env.SetArgument(pair.Key, pair.Value);
            }
            return env;
        }
        public CardEngine game;
        public Card card;
        public Buff buff;
        public EventArg eventArg;
        public Effect effect;
        private Dictionary<string, object> arguments = new Dictionary<string, object>();
    }
}
