namespace TouhouCardEngine.Interfaces
{
    public interface ISnapshoter
    {
        CardSnapshot snapshot(IGame game, Card card);
    }
}
