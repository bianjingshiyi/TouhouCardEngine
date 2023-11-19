namespace TouhouCardEngine.Histories
{
    public class ResponseRNGChange : GameChange
    {
        private uint beforeState;
        private uint afterState;
        public ResponseRNGChange(IChangeableGame target, uint beforeState, uint afterState) : base(target)
        {
            this.beforeState = beforeState;
            this.afterState = afterState;
        }

        public override void applyFor(IChangeableGame changeable)
        {
            changeable.setResponseRNGState(afterState);
        }

        public override void revertFor(IChangeableGame changeable)
        {
            changeable.setResponseRNGState(beforeState);
        }
    }
}
