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
            engine.addActionDefine("IntegerBinaryOperation", new IntegerBinaryOperationActionDefine());
            engine.addActionDefine("IntegerConst", new IntegerConstActionDefine());
            ActionNode action = new ActionNode("IntegerBinaryOperation", new ActionNode[0], new ActionValueRef[]
            {
                new ActionValueRef(new ActionNode("IntegerConst", new ActionNode[0], new ActionValueRef[0], new object[] { 2 })),
                new ActionValueRef(new ActionNode("IntegerConst", new ActionNode[0], new ActionValueRef[0], new object[] { 2 }))
            }, new object[] { BinaryOperator.add });
            EventArg eventArg = new EventArg();
            _ = engine.doActionAsync(null, null, eventArg, action);
            Assert.AreEqual(4, eventArg.getVar("@IntegerBinaryOperator_3_0"));
        }
    }
}