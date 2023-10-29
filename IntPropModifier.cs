using TouhouCardEngine.Interfaces;
namespace TouhouCardEngine
{
    public class IntPropModifier : PropModifier<int>
    {
        #region 公有方法
        #region 构造方法
        public IntPropModifier(string propName, int value)
        {
            propertyName = propName;
            this.value = value;
        }
        public IntPropModifier(string propName, int value, bool isSet)
        {
            propertyName = propName;
            this.value = value;
            this.isSet = isSet;
        }
        public IntPropModifier(string propName, string relatedPropName, bool isSet)
        {
            propertyName = propName;
            this.relatedPropName = relatedPropName;
            this.isSet = isSet;
        }
        protected IntPropModifier(IntPropModifier origin)
        {
            propertyName = origin.getPropName();
            value = origin.value;
            isSet = origin.isSet;
            relatedPropName = origin.relatedPropName;
        }
        #endregion
        public override int calcGeneric(IGame game, Card card, int value)
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
            if (!string.IsNullOrEmpty(relatedPropName))
            {
                return $"{getPropName()}=<{relatedPropName}>";
            }
            else if (isSet)
                return $"{getPropName()}={value}";
            else
                return $"{getPropName()}{(value < 0 ? value.ToString() : "+" + value)}";
        }
        #endregion
        //#region 动作定义
        ///// <summary>
        ///// 修改整数属性修正器的修正值
        ///// </summary>
        ///// <param name="game"></param>
        ///// <param name="card"></param>
        ///// <param name="modifier"></param>
        ///// <param name="value"></param>
        ///// <returns></returns>
        //[ActionNodeMethod("SetIntPropModifierValue", "Modifier")]
        //[return: ActionNodeParam("PropertyChangeEvent")]
        //public static Task<IPropChangeEventArg> setIntPropModifierValue(IGame game, [ActionNodeParam("Card")] Card card, [ActionNodeParam("Modifier")] IntPropModifier modifier, [ActionNodeParam("Value")] int value)
        //{
        //    return modifier.setValue(game, card, value);
        //}
        //#endregion
        #region 属性字段
        public bool isSet = false;
        public int minValue = int.MinValue;
        public int maxValue = int.MaxValue;
        #endregion
    }
}