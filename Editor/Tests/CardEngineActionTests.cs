using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using TouhouCardEngine;
using UnityEngine;

namespace Tests
{
    public class CardEngineActionTests
    {
        /// <summary>
        /// case ZMCS-1167 节点的输出值连接多个输入值，会导致无法正确获取到值并报错
        /// </summary>
        [Test]
        public void multipleInputRefToOutputThroughScopeTest()
        {
            CardEngine game = new CardEngine();
            game.addActionDefine("IntegerBinaryOperation", new IntegerOperationActionDefine());
            game.addActionDefine("IntegerConst", new IntegerConstActionDefine());
            //动作节点1的输出值会被多次引用，获取节点的返回值应该可以得到正确答案。
            ActionNode actionNode1 = new ActionNode(1, "IntegerConst", consts: new object[] { 2 }, regVar: new bool[] { true });
            Scope scope = new Scope();
            Assert.AreEqual(2, game.getActionReturnValueAsync<int>(null, null, null, actionNode1, scope: scope).Result);
            Assert.AreEqual(2, scope.getLocalVar(1, 0));
            ActionNode actionNode2 = new ActionNode(2, "IntegerBinaryOperation",
                inputs: new ActionValueRef[] { new ActionValueRef(actionNode1), new ActionValueRef(actionNode1) },
                consts: new object[] { IntegerOperator.add });
            Assert.AreEqual(4, game.getActionReturnValueAsync<int>(null, null, null, actionNode2, scope: scope).Result);
            ActionNode actionNode3 = new ActionNode(3, "IntegerBinaryOperation",
                inputs: new ActionValueRef[] { new ActionValueRef(actionNode2), new ActionValueRef(actionNode1) },
                consts: new object[] { IntegerOperator.sub });
            Assert.AreEqual(2, game.getActionReturnValueAsync<int>(null, null, null, actionNode3, scope: scope).Result);
        }
        [Test]
        public void doActionWithGeneratedActionDefineTest()
        {
            CardEngine game = new CardEngine();
            game.addActionDefine("IntegerBinaryOperation", new IntegerOperationActionDefine());
            game.addActionDefine("IntegerConst", new IntegerConstActionDefine());
            ActionNode actionNode1 = new ActionNode(1, "IntegerBinaryOperation", new ActionValueRef[]
            {
                new ActionValueRef(0),
                new ActionValueRef(1)
            }, new object[] { IntegerOperator.add }, new bool[] { true });
            ActionNode actionNode2 = new ActionNode(2, "IntegerBinaryOperation", new ActionValueRef[]
            {
                new ActionValueRef(actionNode1,0),
                new ActionValueRef(2)
            }, new object[] { IntegerOperator.mul }, null, null);
            actionNode1.branches = new ActionNode[] { actionNode2 };
            game.addActionDefine("GeneratedActionDefine", new GeneratedActionDefine(1, null, "GeneratedActionDefine",
                new ValueDefine[]
                {
                    new ValueDefine(typeof(int),"A",false,false),
                    new ValueDefine(typeof(int),"B",false,false)
                },
                new ValueDefine[]
                {
                    new ValueDefine(typeof(int),"C",false,false)
                },
                new ValueDefine[]
                {
                    new ValueDefine(typeof(int),"Result",false,false)
                },
                new ReturnValueRef[] { new ReturnValueRef(actionNode2, 0, 0) },
                actionNode1));
            ActionNode action = new ActionNode(1, "GeneratedActionDefine", new ActionValueRef[]
            {
                new ActionValueRef(new ActionNode(2, "IntegerConst", new ActionValueRef[0], new object[] { 2 }, null, null)),
                new ActionValueRef(new ActionNode(3, "IntegerConst", new ActionValueRef[0], new object[] { 2 }, null, null))
            }, new object[] { 2 }, null, null);
            var task = game.doActionAsync(null, null, null, action);
            Assert.AreEqual(8, task.Result[0]);
        }
        [Test]
        public void doActionTest()
        {
            CardEngine engine = new CardEngine();
            engine.addActionDefine("IntegerBinaryOperation", new IntegerOperationActionDefine());
            engine.addActionDefine("IntegerConst", new IntegerConstActionDefine());
            ActionNode action = new ActionNode(1, "IntegerBinaryOperation", new ActionValueRef[]
            {
                new ActionValueRef(new ActionNode(2, "IntegerConst", new ActionValueRef[0], new object[] { 2 }, null, null)),
                new ActionValueRef(new ActionNode(3, "IntegerConst", new ActionValueRef[0], new object[] { 2 }, null, null))
            }, new object[] { IntegerOperator.add }, new bool[] { true }, null);
            Scope scope = new Scope();
            _ = engine.doActionAsync(null, null, null, action, scope);
            Assert.AreEqual(4, scope.getLocalVar(1, 0));
        }
    }
}