namespace TouhouCardEngine.Histories
{
    public class RandomChange : GameChange
    {
        private uint beforeState;
        private uint afterState;
        public RandomChange(IChangeableGame target, uint beforeState, uint afterState) : base(target)
        {
            this.beforeState = beforeState;
            this.afterState = afterState;
        }

        public override void applyFor(IChangeableGame changeable)
        {
            changeable.setRandomState(afterState);
        }

        public override void revertFor(IChangeableGame changeable)
        {
            changeable.setRandomState(beforeState);
        }
    }
}
