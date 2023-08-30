namespace TouhouCardEngine
{
    public class ActionReference : DefineReference
    {
        public ActionReference(long cardPoolId, int defineId)
        {
            this.cardPoolId = cardPoolId;
            this.defineId = defineId;
        }
    }
}
