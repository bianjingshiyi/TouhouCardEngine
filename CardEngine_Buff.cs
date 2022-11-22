namespace TouhouCardEngine
{
    partial class CardEngine
    {
        #region 公有方法
        public BuffDefine getBuffDefine(long cardPoolId, int id)
        {
            if (rule.buffDict.TryGetValue(cardPoolId, out var buffDict))
            {
                if (buffDict.TryGetValue(id, out BuffDefine buffDefine))
                    return buffDefine;
            }
            return null;
        }
        #endregion
    }
}