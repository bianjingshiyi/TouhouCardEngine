using System.Threading.Tasks;
namespace TouhouCardEngine.Interfaces
{
    public interface IGame
    {
        ITriggerManager triggers { get; }
        IAnswerManager answers { get; }
        ITimeManager time { get; }
        ILogger logger { get; }
        int randomInt(int min, int max);
    }
    public interface IPlayer
    {
        int id { get; }
    }
    public interface ICard
    {
        int id { get; }
        ICardDefine define { get; }
        Task addModifier(IGame game, PropModifier modifier);
        Task<bool> removeModifier(IGame game, PropModifier modifier);
        T getProp<T>(string propName);
        void setProp<T>(string propName, T value);
    }
    public interface ICardDefine
    {
        int id { get; }
        IEffect[] effects { get; }
    }
}
