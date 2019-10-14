namespace TouhouCardEngine
{
    /// <summary>
    /// 卡片定义，包含了一张卡的静态数据和效果逻辑。
    /// </summary>
    public abstract class CardDefine
    {
        public static EmptyCardDefine empty { get; } = new EmptyCardDefine();
        public abstract int id { get; }
        public abstract CardDefineType type { get; }
        public abstract Effect[] effects { get; }
        public object this[string propName]
        {
            get { return getProp<object>(propName); }
        }
        public virtual T getProp<T>(string propName)
        {
            if (propName == nameof(id))
                return (T)(object)id;
            else
                return default(T);
        }
        public abstract string isUsable(CardEngine engine, Player player, Card card);
        public class EmptyCardDefine : CardDefine
        {
            int _id = 0;
            public override int id
            {
                get { return _id; }
            }
            CardDefineType _type = CardDefineType.unknow;
            public override CardDefineType type
            {
                get { return _type; }
            }
            Effect[] _effects = new Effect[0];
            public override Effect[] effects
            {
                get { return _effects; }
            }
            public EmptyCardDefine setID(int id)
            {
                EmptyCardDefine clone = this.clone();
                clone._id = id;
                return clone;
            }
            EmptyCardDefine clone()
            {
                EmptyCardDefine clone = new EmptyCardDefine();
                clone._id = _id;
                clone._type = _type;
                clone._effects = _effects;
                return clone;
            }
            public override string isUsable(CardEngine engine, Player player, Card card)
            {
                return null;
            }
        }
    }
}