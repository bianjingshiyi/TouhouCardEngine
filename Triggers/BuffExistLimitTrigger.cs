using System;
using System.Linq;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public class BuffExistLimitTrigger : Trigger
    {
        public BuffExistLimitTrigger(CardEngine game, Card card, Buff buff, BuffExistLimit limit) : base()
        {
            this.game = game;
            this.card = card;
            this.buff = buff;
            this.limit = limit;
        }
        public override bool checkCondition(IEventArg arg)
        {
            return true;
        }
        public override Task invoke(IEventArg arg)
        {
            limit.addCounter(game, card, buff);
            return Task.CompletedTask;
        }
        public Card card;
        public Buff buff;
        public BuffExistLimit limit;
        public CardEngine game;
    }
    [Serializable]
    public class SerializableBuffExistLimitTrigger
    {
        public SerializableBuffExistLimitTrigger(BuffExistLimitTrigger trigger)
        {
            cardId = trigger.card.id;
            buffInstanceId = trigger.buff.instanceID;
            limitIndex = Array.IndexOf(trigger.buff.getExistLimits(), trigger.limit);
        }
        public BuffExistLimitTrigger toTrigger(CardEngine game)
        {
            var card = game.getCard(cardId);
            var buff = card.getBuffs().FirstOrDefault(b => b.instanceID == buffInstanceId);
            var limit = buff.getExistLimits()[limitIndex];
            return new BuffExistLimitTrigger(game, card, buff, limit);
        }
        private int limitIndex;
        private int cardId;
        private int buffInstanceId;
    }
}
