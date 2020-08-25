using TouhouCardEngine.Interfaces;
using System.Threading.Tasks;
namespace TouhouCardEngine
{
    public class IntPropModifier : PropModifier<int>
    {
        public bool isSet { get; }
        public IntPropModifier(string propName, int value) : base(propName, value)
        {
            isSet = false;
        }
        public IntPropModifier(string propName, int value, bool isSet) : base(propName, value)
        {
            this.isSet = isSet;
        }
        protected IntPropModifier(IntPropModifier origin) : base(origin)
        {
            isSet = origin.isSet;
        }
        public override int calc(IGame game, Card card, int value)
        {
            if (isSet)
                return this.value;
            else
                return value + this.value;
        }
        public override PropModifier clone()
        {
            return new IntPropModifier(this);
        }
        public override string ToString()
        {
            if (isSet)
                return propName + "=" + value;
            else
                return propName + (value < 0 ? value.ToString() : "+" + value);
        }
    }
}