using System;
using System.Collections.Generic;
namespace TouhouCardEngine
{
    [Serializable]
    public class ReturnValueRef
    {
        #region 公有方法
        #region 构造方法
        public ReturnValueRef(ActionNode actionNode, int index, int returnIndex)
        {
            valueRef = new ActionValueRef(actionNode, index);
            this.returnIndex = returnIndex;
        }
        public ReturnValueRef(int actionNodeId, int index, int returnIndex)
        {
            valueRef = new ActionValueRef(actionNodeId, index);
            this.returnIndex = returnIndex;
        }
        public ReturnValueRef(int argIndex, int returnIndex)
        {
            valueRef = new ActionValueRef(argIndex);
            this.returnIndex = returnIndex;
        }
        public ReturnValueRef() : this(0, 0, 0)
        {
        }
        #endregion
        #endregion
        public ActionValueRef valueRef;
        public int returnIndex;
    }
    [Serializable]
    public class SerializableReturnValueRef
    {
        #region 公有方法
        #region 构造方法
        public SerializableReturnValueRef(ReturnValueRef returnValueRef)
        {
            valueRef = new SerializableActionValueRef(returnValueRef.valueRef);
            returnIndex = returnValueRef.returnIndex;
        }
        #endregion
        public ReturnValueRef toReturnValueRef(List<SerializableActionNode> actionNodeList, Dictionary<int, ActionNode> actionNodeDict)
        {
            if (valueRef.actionNodeId != 0)
            {
                if (actionNodeDict.TryGetValue(valueRef.actionNodeId, out ActionNode childNode))
                    return new ReturnValueRef(childNode, valueRef.index, returnIndex);
                else
                    return new ReturnValueRef(
                        SerializableActionNode.toActionNodeGraph(valueRef.actionNodeId, actionNodeList, actionNodeDict),
                        valueRef.index, returnIndex);
            }
            else
                return new ReturnValueRef(valueRef.argIndex, returnIndex);
        }
        #endregion
        public SerializableActionValueRef valueRef;
        public int returnIndex;
    }
}