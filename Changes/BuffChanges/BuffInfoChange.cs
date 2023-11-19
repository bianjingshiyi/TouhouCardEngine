namespace TouhouCardEngine.Histories
{
    public class BuffInfoChange : BuffChange
    {
        public Card beforeCard;
        public Card afterCard;
        public int beforeInstanceId;
        public int afterInstanceId;
        public BuffInfoChange(IChangeableBuff target, Card beforeCard, Card afterCard, int beforeInstanceId, int afterInstanceId) : base(target)
        {
            this.beforeCard = beforeCard;
            this.afterCard = afterCard;
            this.beforeInstanceId = beforeInstanceId;
            this.afterInstanceId = afterInstanceId;
        }

        public override void applyFor(IChangeableBuff trackable)
        {
            trackable.setInfo(afterCard, afterInstanceId);
        }
        public override void revertFor(IChangeableBuff trackable)
        {
            trackable.setInfo(beforeCard, beforeInstanceId);
        }
    }
}
