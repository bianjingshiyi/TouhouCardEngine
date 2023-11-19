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
            defaultValue = value;
        }
        public IntPropModifier(string propName, int value, bool isSet)
        {
            propertyName = propName;
            defaultValue = value;
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
            defaultValue = origin.defaultValue;
            isSet = origin.isSet;
            relatedPropName = origin.relatedPropName;
        }
        #endregion
        public override int calcGeneric(IGame game, ICardData card, int prop, int value)
        {
            if (isSet)
                prop = value;
            else
                prop += value;

            if (prop < minValue)
                prop = minValue;
            else if (prop > maxValue)
                prop = maxValue;
            return prop;
        }
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(relatedPropName))
            {
                return $"{getPropName()}=<{relatedPropName}>";
            }
            else
            {
                var value = defaultValue;
                if (isSet)
                    return $"{getPropName()}={value}";
                else
                    return $"{getPropName()}{(value < 0 ? value.ToString() : "+" + value)}";
            }
        }
        #endregion

        #region 属性字段
        public bool isSet = false;
        public int minValue = int.MinValue;
        public int maxValue = int.MaxValue;
        #endregion
    }
}