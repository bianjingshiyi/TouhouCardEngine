using System;

namespace TouhouCardEngine
{
    public class BuffExistLimit
    {
        #region 公有方法
        public BuffExistLimit(BuffExistLimitDefine define)
        {
            this.define = define;
        }
        public void apply(CardEngine game, Card card, Buff buff)
        {
            var triggerTime = define.triggerTime;
            string triggerName = getEffectName(buff, triggerTime);
            game.logger.logTrace("BuffExistLimit", $"{card}注册触发器{triggerName}");
            Trigger trigger = new BuffExistLimitTrigger(game, card, buff, this);
            this.trigger = trigger;
            game.triggers.registerDelayed(triggerTime, trigger);
        }
        public void remove(CardEngine game, Card card, Buff buff)
        {
            var triggerTime = define.triggerTime;
            string triggerName = getEffectName(buff, triggerTime);
            game.logger.logTrace("BuffExistLimit", $"{card}注销触发器{triggerName}");
            game.triggers.remove(triggerTime, trigger);
            trigger = null;
        }
        public void addCounter(CardEngine game, Card card, Buff buff)
        {
            counter = counter + 1;
            update(game, card, buff);
        }
        public bool isFinished()
        {
            return counter >= define.count;
        }
        public BuffExistLimit clone()
        {
            return new BuffExistLimit(this);
        }
        #endregion

        #region 私有方法
        private BuffExistLimit(BuffExistLimit other)
        {
            define = other.define;
            counter = other.counter;
        }
        private void update(CardEngine game, Card card, Buff buff)
        {
            if (isFinished())
            {
                RemoveBuffEventDefine.doEvent(game, card, buff);
            }
        }
        private string getEffectName(Buff buff, EventTriggerTime triggerTime)
        {
            var buffPrefix = buff != null ? buff.instanceID.ToString() : string.Empty;
            var limits = buff.getExistLimits();
            var limitIndex = -1;
            if (limits != null)
            {
                limitIndex = Array.IndexOf(limits, this);
            }
            return $"{buffPrefix}-Limit{limitIndex}-{triggerTime}";
        }
        #endregion

        public BuffExistLimitDefine define;
        private Trigger trigger;
        private int counter;
    }
}
