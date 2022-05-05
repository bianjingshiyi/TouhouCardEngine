
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
                for (int i = 0; i < data.inputList.Count; i++)
                {
                    inputList.Add(data.inputList[i].toValueDefine(typeFinder));
                }
            }
            if (data.constList != null)
            {
                for (int i = 0; i < data.constList.Count; i++)
                {
                    constList.Add(data.constList[i].toValueDefine(typeFinder));
                }
            }
            if (data.outputList != null)
            {
                for (int i = 0; i < data.outputList.Count; i++)
                {
                    outputList.Add(data.outputList[i].toValueDefine(typeFinder));
                }
            }
            action = data.action;
        }
        public GeneratedActionDefine(string defineName, ValueDefine[] inputs, ValueDefine[] consts, ValueDefine[] outputs, ReturnValueRef[] returnValueRefs, ActionNode action) : base(defineName, null)
        {
            if (inputs != null)
                inputList.AddRange(inputs);
            if (consts != null)
                constList.AddRange(consts);
            if (outputs != null)
                outputList.AddRange(outputs);
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
        public override ValueDefine[] inputs => inputList.ToArray();
        public override ValueDefine[] consts => constList.ToArray();
        public override ValueDefine[] outputs => outputList.ToArray();
        public List<ValueDefine> inputList = new List<ValueDefine>();
        public List<ValueDefine> constList = new List<ValueDefine>();
        public List<ValueDefine> outputList = new List<ValueDefine>();
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
        public List<ReturnValueRef> returnList = new List<ReturnValueRef>();
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
            if (isArray)
                type = type.MakeArrayType();
            return new ValueDefine(type, name, isParams, false);
        }
        public string typeName;
        public string name;
        public bool isParams;
        public bool isArray;
    }
}