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
        /// 设置修改器的修改值。注意对于一些修改器来说，这次修改并不会修改它的beforeAdd,afterAdd等方法，请确定你明白你在做什么再调用这个方法。
        /// </summary>
        /// <param name="value"></param>
        public void setValue(int value)
        {
            this.value = value;
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