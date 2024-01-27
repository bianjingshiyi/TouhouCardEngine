using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public abstract class Effect
    {
        public async Task enable(CardEngine game, Card card, Buff buff)
        {
            if (!isDisabled(game, card, buff))
                return;

            await EffectActivationEventDefine.doEvent(game, card, buff, this, true);
        }
        public async Task disable(CardEngine game, Card card, Buff buff)
        {
            if (isDisabled(game, card, buff))
                return;

            await EffectActivationEventDefine.doEvent(game, card, buff, this, false);
        }
        public virtual bool isDisabled(IGame game, ICard card, IBuff buff)
        {
            return !card.isEffectEnabled(buff, this);
        }
        public abstract bool checkCondition(EffectEnv env);
        public abstract Task execute(EffectEnv env);
        internal Task onEnableInternal(EffectEnv env) => onEnable(env);
        internal Task onDisableInternal(EffectEnv env) => onDisable(env);
        protected virtual Task onEnable(EffectEnv env)
        {
            return Task.CompletedTask;
        }
        protected virtual Task onDisable(EffectEnv env)
        {
            return Task.CompletedTask;
        }
        public DefineReference cardDefineRef { get; set; }
        public DefineReference buffDefineRef { get; set; }
    }
}
