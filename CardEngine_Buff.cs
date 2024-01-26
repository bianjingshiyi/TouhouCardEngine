using System.Collections.Generic;
using System.Linq;

namespace TouhouCardEngine
{
    partial class CardEngine
    {
        #region 公有方法
        public BuffDefine getBuffDefine(long cardPoolId, int id)
        {
            return buffs.FirstOrDefault(b => b.cardPoolId == cardPoolId && b.id == id);
        }
        public BuffDefine getBuffDefine(DefineReference defRef)
        {
            if (defRef == null)
                return null;
            return getBuffDefine(defRef.cardPoolId, defRef.defineId);
        }
        public void addBuffDefine(BuffDefine buffDefine)
        {
            buffs.Add(buffDefine);
        }
        #endregion

        #region 属性字段
        private List<BuffDefine> buffs = new List<BuffDefine>();
        #endregion
    }
}