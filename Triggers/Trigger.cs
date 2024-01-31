using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public abstract class Trigger : ITrigger
    {
        public Trigger()
        {
        }
        public abstract bool checkCondition(IEventArg arg);
        public abstract Task invoke(IEventArg arg);
        public abstract int getPriority();
    }
}
