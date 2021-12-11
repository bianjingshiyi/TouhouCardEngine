using System.Collections.Generic;
namespace TouhouCardEngine
{
    partial class Rule
    {
        #region 公有方法
        public void addBuffDefine(BuffDefine buffDefine)
        {
            _buffDefineDict.Add(buffDefine.getId(), buffDefine);
        }
        public BuffDefine getBuffDefine(int id)
        {
            if (_buffDefineDict.TryGetValue(id, out BuffDefine buffDefine))
                return buffDefine;
            return null;
        }
        #endregion
        #region 属性字段
        Dictionary<int, BuffDefine> _buffDefineDict = new Dictionary<int, BuffDefine>();
        #endregion
    }
}