using TouhouCardEngine.Interfaces;
using System.Threading.Tasks;
namespace TouhouCardEngine
{
    public class IntPropModifier : PropModifier<int>
    {
        public bool isSet { get; set; }
        public int minValue { get; set; } = 0;
        public int maxValue { get; set; } = int.MaxValue;
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
                value = this.value;
            else
                value += this.value;
            if (value < minValue)
                value = minValue;
            else if (value > maxValue)
                value = maxValue;
            return value;
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
        #region 动作定义
        /// <summary>
        /// 修改整数属性修正器的修正值
        /// </summary>
        /// <param name="game"></param>
        /// <param name="card"></param>
        /// <param name="modifier"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [ActionNodeMethod("SetIntPropModifierValue")]
        [return: ActionNodeParam("PropertyChangeEvent")]
        public static Task<IPropChangeEventArg> setIntPropModifierValue(IGame game, [ActionNodeParam("Card")] Card card, [ActionNodeParam("Modifier")] IntPropModifier modifier, [ActionNodeParam("Value")] int value)
        {
            return modifier.setValue(game, card, value);
        }
        #endregion
    }
}