namespace TouhouCardEngine.Interfaces
{
    public interface IEffectEventDefine
    {
        Card getCard(EventArg arg);
        Effect getEffect(EventArg arg);
    }
}
