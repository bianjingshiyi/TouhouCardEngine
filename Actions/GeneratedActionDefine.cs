
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
        public GeneratedActionDefine(int id, ValueDefine[] inputs, ValueDefine[] consts, ValueDefine[] outputs, ActionNode action) :
            this(id, string.Empty, string.Empty, inputs, consts, outputs, null, action)
        {

        }
        #endregion
        public override Task<object[]> execute(IGame game, ICard card, IBuff buff, IEventArg eventArg, Scope scope, object[] args, object[] constValues)
        {
            Scope invokeScope = new Scope() { args = args, consts = constValues };
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
    public class SerializableActionDefine
    {
        #region 公有方法
        #region 构造函数
        public SerializableActionDefine(GeneratedActionDefine generatedActionDefine)
        {
            if (generatedActionDefine == null)
                throw new ArgumentNullException(nameof(generatedActionDefine));
            id = generatedActionDefine.id;
            name = generatedActionDefine.defineName;
            inputList = generatedActionDefine.inputs != null ?
                new List<SerializableValueDefine>(Array.ConvertAll(generatedActionDefine.inputs, v => v != null ?
                    new SerializableValueDefine(v) :
                    null)) :
                new List<SerializableValueDefine>();
            constList = generatedActionDefine.consts != null ?
                new List<SerializableValueDefine>(Array.ConvertAll(generatedActionDefine.consts, v => v != null ?
                    new SerializableValueDefine(v) :
                    null)) :
                new List<SerializableValueDefine>();
            outputList = generatedActionDefine.outputs != null ?
                new List<SerializableValueDefine>(Array.ConvertAll(generatedActionDefine.outputs, v => v != null ?
                    new SerializableValueDefine(v) :
                    null)) :
                new List<SerializableValueDefine>();
            //returnList = new List<ReturnValueRef>(generatedActionDefine.returnValueRefList);
            seriReturnList = generatedActionDefine.returnValueRefList != null ?
                generatedActionDefine.returnValueRefList.ConvertAll(r => r != null ?
                    new SerializableReturnValueRef(r) :
                    null) :
                new List<SerializableReturnValueRef>();
            //action = generatedActionDefine.action;
            if (generatedActionDefine.action != null)
            {
                rootActionId = generatedActionDefine.action.id;
                generatedActionDefine.action.traverse(a =>
                {
                    if (a != null)
                        actionNodeList.Add(new SerializableActionNode(a));
                });
            }
            else
                rootActionId = 0;
        }
        #endregion
        public GeneratedActionDefine toGeneratedActionDefine(Func<string, Type> typeFinder)
        {
            //action
            Dictionary<int, ActionNode> actionNodeDict = new Dictionary<int, ActionNode>();
            ActionNode rootActionNode = action != null ? action :
                SerializableActionNode.toActionNodeGraph(rootActionId, actionNodeList, actionNodeDict);
            //return
            ReturnValueRef[] returnValueRefs;
            if (returnList != null)
                returnValueRefs = returnList.ToArray();
            else
            {
                returnValueRefs = new ReturnValueRef[seriReturnList.Count];
                for (int i = 0; i < returnValueRefs.Length; i++)
                {
                    returnValueRefs[i] = seriReturnList[i].toReturnValueRef(actionNodeList, actionNodeDict);
                }
            }
            return new GeneratedActionDefine(id, category, name,
                inputList.ConvertAll(s => s.toValueDefine(typeFinder)).ToArray(),
                constList.ConvertAll(s => s.toValueDefine(typeFinder)).ToArray(),
                outputList.ConvertAll(s => s.toValueDefine(typeFinder)).ToArray(),
                returnValueRefs, rootActionNode);
        }
        #endregion
        #region 属性字段
        public int id;
        public string name;
        public string category;
        public List<SerializableValueDefine> inputList = new List<SerializableValueDefine>();
        public List<SerializableValueDefine> constList = new List<SerializableValueDefine>();
        public List<SerializableValueDefine> outputList = new List<SerializableValueDefine>();
        public List<ReturnValueRef> returnList = null;
        public List<SerializableReturnValueRef> seriReturnList = new List<SerializableReturnValueRef>();
        public ActionNode action;
        public int rootActionId;
        public List<SerializableActionNode> actionNodeList = new List<SerializableActionNode>();
        #endregion
    }
}