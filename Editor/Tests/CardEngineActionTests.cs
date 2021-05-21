using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TouhouCardEngine;

namespace Tests
{
    public class CardEngineActionTests
    {
        [Test]
        public void doActionTest()
        {
            CardEngine engine = new CardEngine();
            engine.addActionDefine("IntegerBinaryOperation", new IntegerOperationActionDefine());
            engine.addActionDefine("IntegerConst", new IntegerConstActionDefine());
            ActionNode action = new ActionNode("IntegerBinaryOperation", new ActionValueRef[]
            {
                new ActionValueRef(new ActionNode("IntegerConst", new ActionValueRef[0], new object[] { 2 }, new ActionNode[0])),
                new ActionValueRef(new ActionNode("IntegerConst", new ActionValueRef[0], new object[] { 2 }, new ActionNode[0]))
            }, new object[] { IntegerOperator.add }, new ActionNode[0]);
            EventArg eventArg = new EventArg();
            _ = engine.doActionAsync(null, null, eventArg, action);
            Assert.AreEqual(4, eventArg.getVar("@IntegerBinaryOperator_3_0"));
        }
    }
}