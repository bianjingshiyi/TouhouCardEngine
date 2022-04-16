
using TouhouCardEngine;
using TouhouCardEngine.Interfaces;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace TouhouCardEngine
{
    public class GeneratedActionDefine : ActionDefine
    {
        #region 公有方法
        #region 构造方法
        public GeneratedActionDefine(SerializableActionDefine data, Func<string, Type> typeFinder = null) : base(data.name, null)
        {
            if (data.inputList != null)
            {
                inputs = new ValueDefine[data.inputList.Count];
                for (int i = 0; i < data.inputList.Count; i++)
                {
                    inputs[i] = data.inputList[i].toValueDefine(typeFinder);
                }
            }
            else
            {
                inputs = new ValueDefine[0];
            }
            if (data.constList != null)
            {
                consts = new ValueDefine[data.constList.Count];
                for (int i = 0; i < data.constList.Count; i++)
                {
                    consts[i] = data.constList[i].toValueDefine(typeFinder);
                }
            }
            else
            {
                consts = new ValueDefine[0];
            }
            if (data.outputList != null)
            {
                outputs = new ValueDefine[data.outputList.Count];
                for (int i = 0; i < data.outputList.Count; i++)
                {
                    outputs[i] = data.outputList[i].toValueDefine(typeFinder);
                }
            }
            else
            {
                outputs = new ValueDefine[0];
            }
            action = data.action;
        }
        public GeneratedActionDefine(string defineName, ValueDefine[] inputs, ValueDefine[] consts, ValueDefine[] outputs, ReturnValueRef[] returnValueRefs, ActionNode action) : base(defineName, null)
        {
            this.inputs = inputs;
            this.consts = consts;
            this.outputs = outputs;
            this.action = action;
            this.returnValueRefs = returnValueRefs;
        }
        #endregion
        public override Task<object[]> execute(IGame game, ICard card, IBuff buff, IEventArg eventArg, Scope scope, object[] args, object[] constValues)
        {
            Scope invokeScope = new Scope() { parentScope = scope, args = args, consts = constValues };
            return (game as CardEngine).getActionsReturnValueAsync(card as Card, buff as Buff, eventArg as EventArg, action, invokeScope, returnValueRefs);
        }
        #endregion
        #region 属性字段
        public override ValueDefine[] inputs { get; }
        public override ValueDefine[] consts { get; }
        public override ValueDefine[] outputs { get; }
        public ActionNode action;
        public ReturnValueRef[] returnValueRefs;
        #endregion
    }
    [Serializable]
    public class ReturnValueRef
    {
        #region 公有方法
        #region 构造方法
        public ReturnValueRef(int actionNodeId, int index, int returnIndex)
        {
            valueRef = new ActionValueRef(actionNodeId, index);
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
    public class SerializableActionDefine
    {
        public int id;
        public string name;
        public List<SerializableValueDefine> inputList = new List<SerializableValueDefine>();
        public List<SerializableValueDefine> constList = new List<SerializableValueDefine>();
        public List<SerializableValueDefine> outputList = new List<SerializableValueDefine>();
        public ActionNode action;
    }
    [Serializable]
    public class SerializableValueDefine
    {
        public ValueDefine toValueDefine(Func<string, Type> typeFinder = null)
        {
            Type type;
            if (typeFinder != null)
            {
                type = typeFinder(typeName);
            }
            else
            {
                type = null;
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = assembly.GetType(typeName);
                    if (type != null)
                        break;
                }
            }
            return new ValueDefine(type, name, isParams, false);
        }
        public string typeName;
        public string name;
        public bool isParams;
    }
}