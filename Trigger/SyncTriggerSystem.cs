namespace TouhouCardEngine
{
    public partial class SyncTriggerSystem
    {
        public SyncTriggerSystem(CardEngine game)
        {
            this.game = game;
            initDoEventActions();
        }
    }
}