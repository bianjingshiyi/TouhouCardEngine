using System.Threading.Tasks;

namespace TouhouCardEngine.Interfaces
{
    public interface IEffect
    {
        string[] events { get; }
        string[] piles { get; }
        bool checkCondition(IGame game, IPlayer player, ICard card, object[] vars);
        bool checkTarget(IGame game, IPlayer player, ICard card, object[] targets);
        Task execute(IGame game, IPlayer player, ICard card, object[] vars, object[] targets);
    }
}
