using TouhouCardEngine.Interfaces;
using System.Threading.Tasks;
namespace TouhouCardEngine
{
    public class IntPropModifier : PropModifier<int>
    {
        public sealed override string propName { get; }
        public int value { get; private set; }
        public bool isSet { get; }
        public IntPropModifier(string propName, int value)
        {
            this.propName = propName;
            this.value = value;
            isSet = false;
        }
        public IntPropModifier(string propName, int value, bool isSet)
        {
            this.propName = propName;
            this.value = value;
            this.isSet = isSet;
        }
        protected IntPropModifier(IntPropModifier origin)
        {
            propName = origin.propName;
            value = origin.value;
            isSet = origin.isSet;
        }
        /// <summary>
        /// 设置修改器的修改值。
        /// </summary>
        /// <param name="value"></param>
        public virtual Task setValue(IGame game, Card card, int value)
        {
            this.value = value;
            return Task.CompletedTask;
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