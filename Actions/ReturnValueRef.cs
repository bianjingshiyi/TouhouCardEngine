using System;
namespace TouhouCardEngine
{
    [Obsolete]
    public class ReturnValueRef
    {
        #region 公有方法
        #region 构造方法
        public ReturnValueRef(ActionNode actionNode, int index, int returnIndex)
        {
            valueRef = new ActionValueRef(actionNode, index);
            this.returnIndex = returnIndex;
        }
        public ReturnValueRef(int argIndex, int returnIndex)
        {
            valueRef = new ActionValueRef(argIndex);
            this.returnIndex = returnIndex;
        }
        public ReturnValueRef() : this(null, 0, 0)
        {
        }
        #endregion
        #endregion
        public ActionValueRef valueRef;
        public int returnIndex;
    }
    [Obsolete]
    [Serializable]
    public class SerializableReturnValueRef
    {
        public SerializableActionValueRef valueRef;
        public int returnIndex;
    }
}