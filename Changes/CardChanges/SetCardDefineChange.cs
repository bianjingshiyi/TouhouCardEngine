namespace TouhouCardEngine.Histories
{
    public class SetCardDefineChange : CardChange
    {
        public CardDefine beforeDefine;
        public CardDefine define;
        public SetCardDefineChange(IChangeableCard target, CardDefine beforeDefine, CardDefine define) : base(target)
        {
            this.beforeDefine = beforeDefine;
            this.define = define;
        }

        public override void applyFor(IChangeableCard trackable)
        {
            trackable.setDefine(define);
        }
        public override void revertFor(IChangeableCard trackable)
        {
            trackable.setDefine(beforeDefine);
        }
    }
}
