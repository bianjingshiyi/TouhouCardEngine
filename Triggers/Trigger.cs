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
    }
    public abstract class Trigger<T> : Trigger, ITrigger<T> where T : IEventArg
    {
        public Trigger()
        {
        }
        public override sealed bool checkCondition(IEventArg arg)
        {
            if (arg is T tArg)
            {
                return checkCondition(tArg);
            }
            return false;
        }
        public override sealed Task invoke(IEventArg arg)
        {
            if (arg is T tArg)
            {
                return invoke(tArg);
            }
            return Task.CompletedTask;
        }
        public abstract bool checkCondition(T arg);
        public abstract Task invoke(T arg);
    }
}
