using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine.Histories
{
    public class SetNextRandomChange : GameChange
    {
        private int[] beforeRandom;
        private int[] afterRandom;
        public SetNextRandomChange(IChangeableGame target, int[] beforeRandom, int[] afterRandom) : base(target)
        {
            this.beforeRandom = beforeRandom.Clone() as int[];
            this.afterRandom = afterRandom.Clone() as int[];
        }

        public override void applyFor(IChangeableGame changeable)
        {
            changeable.setNextRandomInt(afterRandom);
        }

        public override void revertFor(IChangeableGame changeable)
        {
            changeable.setNextRandomInt(beforeRandom);
        }
    }
}
