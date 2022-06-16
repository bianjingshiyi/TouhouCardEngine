using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using TouhouCardEngine;

namespace Tests
{
    public class CardEngineActionTests
    {
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
            }, new object[] { IntegerOperator.add }, null, null);
            EventArg eventArg = new EventArg();
            _ = engine.doActionAsync(null, null, eventArg, action);
            Assert.AreEqual(4, eventArg.getVar("@IntegerBinaryOperator_3_0"));
        }
    }
}