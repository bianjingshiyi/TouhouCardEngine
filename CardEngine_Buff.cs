namespace TouhouCardEngine
{
    partial class CardEngine
    {
        #region 公有方法
        public BuffDefine getBuffDefine(int id)
        {
            return rule.getBuffDefine(id);
        }
        #region 动作定义
        [ActionNodeMethod("GetBuffDefine", "Game")]
        [return: ActionNodeParam("BuffDefine")]
        public static BuffDefine getBuffDefine(CardEngine game, [ActionNodeParam("Id", isConst: true)]int id)
        {
            return game.getBuffDefine(id);
        }
        #endregion
        #endregion
    }
}