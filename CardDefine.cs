namespace TouhouCardEngine
{
    /// <summary>
    /// 卡片定义，包含了一张卡的静态数据和效果逻辑。
    /// </summary>
    public abstract class CardDefine
    {
        public abstract int id { get; set; }
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
    }
}