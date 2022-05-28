
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
            id = data.id;
            category = data.category;
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
            if (data.returnList != null)
                returnValueRefList.AddRange(data.returnList);
        }
        public GeneratedActionDefine(int id, string category, string defineName, ValueDefine[] inputs, ValueDefine[] consts, ValueDefine[] outputs, ReturnValueRef[] returnValueRefs, ActionNode action) : base(defineName, null)
        {
            this.id = id;
            this.category = category;
            if (inputs != null)
                inputList.AddRange(inputs);
            if (consts != null)
                constList.AddRange(consts);
            if (outputs != null)
                outputList.AddRange(outputs);
            this.action = action;
            if (returnValueRefs != null)
                returnValueRefList.AddRange(returnValueRefs);
        }
        #endregion
        public override Task<object[]> execute(IGame game, ICard card, IBuff buff, IEventArg eventArg, Scope scope, object[] args, object[] constValues)
        {
            Scope invokeScope = new Scope() { parentScope = scope, args = args, consts = constValues };
            return (game as CardEngine).getActionsReturnValueAsync(card as Card, buff as Buff, eventArg as EventArg, action, invokeScope, returnValueRefList.ToArray());
        }
        #endregion
        #region 属性字段
        public int id { get; }
        public string category;
        public override ValueDefine[] inputs => inputList.ToArray();
        public override ValueDefine[] consts => constList.ToArray();
        public override ValueDefine[] outputs => outputList.ToArray();
        public List<ValueDefine> inputList = new List<ValueDefine>();
        public List<ValueDefine> constList = new List<ValueDefine>();
        public List<ValueDefine> outputList = new List<ValueDefine>();
        public ActionNode action;
        public List<ReturnValueRef> returnValueRefList = new List<ReturnValueRef>();
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
    public class SerializableActionDefine
    {
        #region 公有方法
        #region 构造函数
        public SerializableActionDefine(GeneratedActionDefine generatedActionDefine)
        {
            id = generatedActionDefine.id;
            name = generatedActionDefine.defineName;
            inputList = new List<SerializableValueDefine>(Array.ConvertAll(generatedActionDefine.inputs, v => new SerializableValueDefine(v)));
            constList = new List<SerializableValueDefine>(Array.ConvertAll(generatedActionDefine.consts, v => new SerializableValueDefine(v)));
            outputList = new List<SerializableValueDefine>(Array.ConvertAll(generatedActionDefine.outputs, v => new SerializableValueDefine(v)));
            returnList = new List<ReturnValueRef>(generatedActionDefine.returnValueRefList);
            action = generatedActionDefine.action;
        }
        #endregion
        #endregion
        #region 属性字段
        public int id;
        public string name;
        public string category;
        public List<SerializableValueDefine> inputList = new List<SerializableValueDefine>();
        public List<SerializableValueDefine> constList = new List<SerializableValueDefine>();
        public List<SerializableValueDefine> outputList = new List<SerializableValueDefine>();
        public List<ReturnValueRef> returnList = new List<ReturnValueRef>();
        public ActionNode action;
        #endregion
    }
    [Serializable]
    public class SerializableValueDefine
    {
        #region 公有方法
        #region 构造函数
        public SerializableValueDefine(ValueDefine valueDefine)
        {
            typeName = valueDefine.type.IsArray ? valueDefine.type.GetElementType().FullName : valueDefine.type.FullName;
            name = valueDefine.name;
            isParams = valueDefine.isParams;
            isArray = valueDefine.type.IsArray;
        }
        #endregion
        public ValueDefine toValueDefine(Func<string, Type> typeFinder = null)
        {
            Type type;
            if (typeFinder != null)
            {
                type = typeFinder(typeName.EndsWith("[]") ? typeName.Substring(0, typeName.Length - 2) : typeName);
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
        #endregion
        #region 属性字段
        public string typeName;
        public string name;
        public bool isParams;
        public bool isArray;
        #endregion
    }
}