using System;
using System.Collections.Generic;

namespace TouhouCardEngine
{
    [Serializable]
    public sealed class SerializableFunctionNode
    {
        #region 属性字段
        public int actionNodeId;
        public SerializableReturnValueRef[] returns;
        public string functionName;
        public object[] consts;
        public List<SerializableActionNode> actionNodeList;
        #endregion
    }
}